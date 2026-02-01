# EP02: Arithmetic Expressions - 사칙연산 인터프리터 만들기

## 영상 정보
- **예상 길이**: 15-18분
- **난이도**: 입문
- **필요 사전 지식**: EP01 시청 (Lexer/Parser 기본 개념)

## 인트로 (0:00)

안녕하세요! 지난 영상에서 우리는 숫자 하나만 파싱하는 아주 간단한 파이프라인을 만들었죠. 사실 좀 심심했죠? 오늘은 진짜 계산기를 만들어볼 겁니다.

[화면: "2 + 3 * 4" 입력 → "14" 출력 데모]

여러분, 이 간단해 보이는 계산식에는 컴파일러가 다뤄야 할 핵심 개념들이 다 들어있습니다. 연산자 우선순위, 괄호 처리, 재귀 구조. 오늘 이걸 다 해결할 겁니다.

그리고 많은 튜토리얼에서 대충 넘어가는 부분인데요, "왜 Expr, Term, Factor로 나누는가?" 이 질문에 제대로 된 답을 드리겠습니다.

시작하죠!

## 본문

### 섹션 1: 우리가 만들 것 미리보기 (1:00)

먼저 오늘 완성할 인터프리터가 어떤 걸 할 수 있는지 보시죠.

[화면: 터미널에서 여러 예제 실행]

```bash
$ dotnet run -- --expr "2 + 3"
5

$ dotnet run -- --expr "2 + 3 * 4"
14                    # 곱셈이 먼저!

$ dotnet run -- --expr "(2 + 3) * 4"
20                    # 괄호로 우선순위 변경

$ dotnet run -- --expr "--5"
5                     # 이중 부정도 가능
```

기본 사칙연산은 물론이고, 연산자 우선순위가 제대로 동작하고, 괄호도 지원하고, 심지어 단항 마이너스까지 처리합니다.

이 모든 걸 100줄도 안 되는 코드로 구현할 수 있어요. F#과 fsyacc의 힘입니다.

### 섹션 2: AST부터 확장하기 (2:30)

인터프리터를 만들 때는 항상 데이터 구조부터 시작합니다. 우리가 표현하려는 개념을 타입으로 먼저 정의하는 거죠.

[화면: Ast.fs 파일]

```fsharp
type Expr =
    | Number of int
    | Add of Expr * Expr
    | Subtract of Expr * Expr
    | Multiply of Expr * Expr
    | Divide of Expr * Expr
    | Negate of Expr
```

여기서 중요한 포인트가 세 가지 있습니다.

첫째, 각 연산자마다 별도의 케이스를 만들었어요. `Add`는 `Add`, `Multiply`는 `Multiply`. 왜 그냥 `BinOp of Operator * Expr * Expr` 같은 범용 케이스 하나로 안 만들었을까요?

[화면: 두 가지 접근법 비교]

타입 안전성 때문입니다. F#의 패턴 매칭은 모든 케이스를 처리했는지 컴파일 타임에 검사해줍니다. 나중에 새 연산자를 추가하면 컴파일러가 "어이, Evaluator에서 이거 처리 안 했는데?" 하고 알려주는 거죠.

둘째, 재귀 구조입니다. `Add of Expr * Expr` — Add의 왼쪽과 오른쪽이 또 Expr이에요. 이게 중첩 표현식을 가능하게 만듭니다.

[화면: AST 트리 시각화 - "2 + 3 * 4"]

```
Add
├── Number 2
└── Multiply
    ├── Number 3
    └── Number 4
```

셋째, `Negate`는 특별합니다. 단항 연산자거든요. 표현식 하나만 받죠. `-5`, `--5`, `-(2+3)` 같은 걸 표현할 수 있어요.

### 섹션 3: 연산자 우선순위의 비밀 - Expr/Term/Factor 패턴 (5:00)

자, 이제 핵심으로 갑니다. 많은 분들이 궁금해하는 부분이죠.

"2 + 3 * 4를 입력하면 어떻게 곱셈이 먼저 계산되나요?"

[화면: Parser.fsy 파일]

```fsharp
Expr:
    | Expr PLUS Term     { Add($1, $3) }
    | Expr MINUS Term    { Subtract($1, $3) }
    | Term               { $1 }

Term:
    | Term STAR Factor   { Multiply($1, $3) }
    | Term SLASH Factor  { Divide($1, $3) }
    | Factor             { $1 }

Factor:
    | NUMBER             { Number($1) }
    | LPAREN Expr RPAREN { $2 }
    | MINUS Factor       { Negate($2) }
```

Expr, Term, Factor 세 단계로 나눴어요. 왜일까요?

핵심 원리는 이겁니다: **낮은 우선순위일수록 높은 문법 레벨에 배치한다.**

[화면: 다이어그램으로 설명]

```
Expr (낮은 우선순위: +, -)
 └─ Term (높은 우선순위: *, /)
     └─ Factor (가장 높음: 숫자, 괄호, 단항연산자)
```

"2 + 3 * 4"를 파싱할 때 무슨 일이 일어나는지 천천히 따라가 보죠.

[화면: 파싱 과정 애니메이션]

1. 최상위는 Expr입니다
2. Expr은 "Expr PLUS Term"을 찾으려고 해요
3. 왼쪽 Expr은? 일단 Term으로 내려갑니다
4. Term은 또 Factor로 내려가고, Factor는 "2"를 찾습니다
5. 다시 올라와서 PLUS를 만납니다
6. 오른쪽 Term을 찾기 시작합니다
7. Term에서 "3 STAR 4"를 발견! Term이 "Term STAR Factor"를 먼저 처리합니다
8. 최종 결과: Add(Number 2, Multiply(Number 3, Number 4))

곱셈이 먼저 묶인 거죠!

왜 이렇게 복잡하게 하냐고요? 사실 fsyacc에는 `%left`, `%right` 같은 선언으로 우선순위를 지정하는 방법도 있어요. 하지만 알려진 버그가 있습니다.

[화면: 텍스트 강조]

**Expr/Term/Factor 패턴은 문법 구조 자체로 우선순위를 표현하므로 버그의 영향을 받지 않습니다.**

그리고 이 패턴은 yacc/bison 시절부터 내려온 검증된 방법이에요. 40년 넘게 쓰인 패턴입니다.

### 섹션 4: 괄호는 어떻게 처리되나? (8:30)

"(2 + 3) * 4"는 어떻게 될까요?

[화면: 파싱 규칙 하이라이트]

```fsharp
Factor:
    | LPAREN Expr RPAREN { $2 }
```

Factor에 이 규칙이 있습니다. 괄호 안에 Expr이 올 수 있다는 거죠.

핵심은 Expr로 다시 올라간다는 겁니다. 괄호 안에서는 모든 연산자를 다시 쓸 수 있어요. 그리고 Factor는 가장 높은 우선순위 레벨이니까, 괄호 안 내용이 제일 먼저 묶입니다.

[화면: "(2 + 3) * 4" 파싱 과정]

1. Term이 "Factor STAR Factor"를 찾습니다
2. 왼쪽 Factor는 "LPAREN Expr RPAREN"
3. Expr에서 "2 PLUS 3"을 처리 → Add(Number 2, Number 3)
4. 오른쪽 Factor는 "4"
5. 최종: Multiply(Add(Number 2, Number 3), Number 4)

완벽하죠? 재귀 문법의 위력입니다.

### 섹션 5: Lexer 확장은 간단 (10:30)

Lexer는 별거 없어요. 토큰 몇 개만 추가하면 됩니다.

[화면: Lexer.fsl 파일]

```fsharp
| '+'           { PLUS }
| '-'           { MINUS }
| '*'           { STAR }
| '/'           { SLASH }
| '('           { LPAREN }
| ')'           { RPAREN }
```

여기서 한 가지 재미있는 점은 MINUS입니다. `-`는 이항 연산자로도 쓰이고 단항 연산자로도 쓰이잖아요?

[화면: "5 - 3"과 "-5" 비교]

Lexer는 그냥 MINUS 토큰 하나만 만들어요. 이게 단항인지 이항인지는 Parser가 문맥을 보고 판단합니다.

```fsharp
Expr MINUS Term     // 이항: 5 - 3
MINUS Factor        // 단항: -5
```

이게 Lexer와 Parser의 역할 분담입니다. Lexer는 문맥 없이 토큰만 만들고, Parser가 문법 규칙으로 의미를 부여하는 거죠.

### 섹션 6: Evaluator - 패턴 매칭의 우아함 (12:00)

이제 AST를 실제로 계산하는 Evaluator를 만들 차례입니다. 새 파일 `Eval.fs`를 만들어요.

[화면: Eval.fs 파일]

```fsharp
let rec eval (expr: Expr) : int =
    match expr with
    | Number n -> n
    | Add (left, right) -> eval left + eval right
    | Subtract (left, right) -> eval left - eval right
    | Multiply (left, right) -> eval left * eval right
    | Divide (left, right) -> eval left / eval right
    | Negate e -> -(eval e)
```

이게 전부입니다. 믿기지 않죠?

`rec` 키워드가 보이시죠? 재귀 함수입니다. AST가 재귀 구조니까 Evaluator도 재귀로 짜는 게 자연스러워요.

[화면: Add(Number 2, Multiply(Number 3, Number 4)) 평가 과정]

```
eval (Add(Number 2, Multiply(Number 3, Number 4)))
= eval (Number 2) + eval (Multiply(Number 3, Number 4))
= 2 + (eval (Number 3) * eval (Number 4))
= 2 + (3 * 4)
= 2 + 12
= 14
```

재귀 호출이 트리를 타고 내려가면서 리프 노드(Number)부터 계산하고 올라옵니다. 자동으로 올바른 순서로 계산되는 거죠.

F#의 패턴 매칭은 여기서 빛을 발합니다. 모든 케이스를 처리했는지 컴파일러가 확인해주거든요. 나중에 AST에 `Modulo` 케이스를 추가하면 컴파일러가 "eval 함수에서 Modulo 처리 안 했어요!" 하고 경고해줍니다.

### 섹션 7: Program.fs와 CLI (13:30)

마지막으로 CLI 인터페이스를 붙입니다.

[화면: Program.fs 파일]

```fsharp
let parse (input: string) : Expr =
    let lexbuf = LexBuffer<char>.FromString input
    Parser.start Lexer.tokenize lexbuf

[<EntryPoint>]
let main argv =
    match argv with
    | [| "--expr"; expr |] ->
        try
            let result = expr |> parse |> eval
            printfn "%d" result
            0
        with ex ->
            eprintfn "Error: %s" ex.Message
            1
    | _ ->
        eprintfn "Usage: funlang --expr <expression>"
        1
```

파이프라인이 보이시죠? `expr |> parse |> eval`

입력 문자열을 parse해서 AST 만들고, eval로 계산하고, 결과 출력. 깔끔합니다.

### 섹션 8: 실행해보기 (14:30)

자, 실제로 돌려볼까요?

[화면: 터미널 전체 화면]

```bash
$ dotnet run -- --expr "2 + 3 * 4"
14

$ dotnet run -- --expr "(2 + 3) * 4"
20

$ dotnet run -- --expr "10 - 6 / 2"
7

$ dotnet run -- --expr "--5"
5

$ dotnet run -- --expr "((2 + 3) * (4 - 1))"
15
```

완벽하게 동작합니다!

좌결합성도 확인해볼까요?

```bash
$ dotnet run -- --expr "2 - 3 - 4"
-5                    # (2-3)-4 = -5, 맞습니다!

$ dotnet run -- --expr "24 / 4 / 2"
3                     # (24/4)/2 = 3, 맞습니다!
```

Expr/Term/Factor 패턴은 기본적으로 좌결합입니다. "Expr PLUS Term" 형태로 정의했기 때문이죠.

### 섹션 9: 핵심 정리 (16:00)

[화면: 표로 정리]

오늘 우리가 한 것:

| 파일 | 추가한 것 |
|------|-----------|
| Ast.fs | 5개 산술 연산자 + Negate |
| Parser.fsy | Expr/Term/Factor 3단계 문법 |
| Lexer.fsl | 6개 토큰 (+, -, *, /, (, )) |
| Eval.fs | 재귀 평가 함수 (새 파일) |
| Program.fs | --expr CLI 인터페이스 |

핵심 개념:

1. **Expr/Term/Factor 패턴** - 문법 구조로 우선순위 표현
2. **재귀 구조** - AST도 재귀, Evaluator도 재귀
3. **패턴 매칭** - 컴파일 타임 완전성 검사
4. **역할 분담** - Lexer는 토큰화, Parser는 문법, Evaluator는 의미

이제 여러분은 제대로 된 계산기를 만들 수 있습니다. 그리고 이 패턴은 앞으로도 계속 쓰일 거예요.

## 아웃트로 (17:00)

오늘 만든 인터프리터는 이미 꽤 강력합니다. 하지만 한계도 있죠. 한 번에 하나의 표현식만 계산할 수 있고, 결과를 저장할 방법이 없어요.

다음 영상에서는 변수를 추가합니다. `let x = 5`처럼 값을 이름에 바인딩하고, `let x = 1 in x + 1`처럼 스코프를 만드는 거죠.

이렇게 되면 진짜 프로그래밍 언어처럼 느껴지기 시작할 겁니다.

[화면: 다음 에피소드 프리뷰]

```fsharp
let x = 5 in
let y = 3 in
x * y + 10
// 결과: 25
```

재미있겠죠?

오늘 영상이 도움이 되셨다면 좋아요와 구독 부탁드립니다. 그리고 댓글로 질문이나 피드백 남겨주세요. 특히 "이 부분이 어려웠어요" 같은 피드백이 큰 도움이 됩니다.

GitHub에 전체 코드가 올라가 있으니 직접 실행해보시고, 궁금한 점 있으면 이슈로 남겨주세요.

다음 영상에서 뵙겠습니다!

## 핵심 키워드

- 연산자 우선순위 (Operator Precedence)
- Expr/Term/Factor 패턴
- 재귀 하강 파싱 (Recursive Descent)
- 문맥 자유 문법 (Context-Free Grammar)
- 추상 구문 트리 (Abstract Syntax Tree)
- 패턴 매칭 (Pattern Matching)
- F# 판별된 공용체 (Discriminated Union)
- 재귀 함수 (Recursive Function)
- 좌결합성 (Left Associativity)
- 단항 연산자 (Unary Operator)
- fsyacc, fslex
- 인터프리터 구현
