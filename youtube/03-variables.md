# EP03: Variables & Binding - 변수와 스코프 구현하기

## 영상 정보
- **예상 길이**: 15-18분
- **난이도**: 초급
- **필요 사전 지식**: EP01-02 시청 (Lexer/Parser 기초)

## 인트로 (0:00)

안녕하세요! 지난 시간에는 사칙연산이 가능한 계산기를 만들어봤죠. 그런데 계산 결과를 저장하고 재사용할 수 있다면 훨씬 편리하지 않을까요?

[화면: 코드 비교 - 왼쪽에 "2+3 * 4" vs 오른쪽에 "let x = 2+3 in x * 4"]

오늘은 변수 바인딩을 추가해서 우리 언어에 메모리를 부여해보겠습니다. 단순한 계산기에서 진짜 프로그래밍 언어로 한 걸음 더 나아가는 순간이에요.

이번 에피소드에서 배울 내용:
- Let 바인딩으로 값 저장하기
- 환경(Environment) 개념
- 중첩 스코프와 섀도잉
- fslex에서 키워드 처리하는 방법

그럼 시작해볼까요!

## 본문

### 섹션 1: 무엇을 만들 것인가? (1:00)

먼저 우리가 만들 기능이 어떤 모습일지 보겠습니다.

[화면: 터미널에서 실행 예시들 차례로 보여주기]

```bash
$ dotnet run -- --expr "let x = 5 in x"
5

$ dotnet run -- --expr "let x = 2 + 3 in x * 4"
20

$ dotnet run -- --expr "let x = 1 in let y = 2 in x + y"
3
```

보시는 것처럼 `let 변수명 = 값 in 표현식` 형태로 변수에 값을 바인딩하고, 그 값을 표현식에서 사용할 수 있습니다.

[화면: AST 구조 다이어그램]

이걸 구현하려면 두 가지가 필요합니다:
1. **문법 확장** - Var와 Let 노드를 AST에 추가
2. **환경(Environment)** - 변수 이름과 값을 저장하는 공간

하나씩 구현해봅시다!

### 섹션 2: AST 확장하기 (2:30)

[화면: Ast.fs 파일 열기]

먼저 AST에 두 가지 새로운 노드를 추가합니다.

```fsharp
type Expr =
    | Number of int
    | Add of Expr * Expr
    // ... 기존 연산자들
    // Phase 3: Variables
    | Var of string           // 변수 참조
    | Let of string * Expr * Expr  // let 바인딩
```

[화면: Var 노드 강조]

`Var`는 간단합니다. 변수 이름만 저장하죠. 실제 값은 나중에 평가할 때 환경에서 찾을 겁니다.

[화면: Let 노드 강조, 세 부분을 화살표로 표시]

`Let`은 세 부분으로 구성됩니다:
1. 변수 이름 (string)
2. 바인딩할 표현식 (Expr)
3. 본문 표현식 (Expr)

예를 들어 "let x = 5 in x + 1"은 이렇게 변환됩니다:

[화면: 코드와 AST를 나란히 보여주기]

```
Let("x", Number 5, Add(Var "x", Number 1))
```

### 섹션 3: Lexer 확장 - 키워드 vs 식별자 (4:00)

[화면: Lexer.fsl 파일]

이제 렉서에서 키워드와 변수 이름을 인식해야 합니다. 여기서 중요한 함정이 하나 있어요.

```fsharp
let letter = ['a'-'z' 'A'-'Z']
let ident_start = letter | '_'
let ident_char = letter | digit | '_'

rule tokenize = parse
    // 키워드는 반드시 식별자보다 먼저!
    | "let"         { LET }
    | "in"          { IN }
    | ident_start ident_char* { IDENT (lexeme lexbuf) }
```

[화면: 규칙 순서를 강조 표시]

보이시죠? `"let"`과 `"in"` 규칙이 `ident_start` 규칙보다 위에 있습니다. 이게 엄청 중요한데요.

[화면: 잘못된 순서 예시로 전환]

만약 순서를 바꾸면 어떻게 될까요?

```fsharp
// 잘못된 순서!
| ident_start ident_char* { IDENT (lexeme lexbuf) }
| "let"         { LET }  // 절대 매칭되지 않음!
```

[화면: "let" 문자열이 입력으로 들어오는 애니메이션]

"let"이 들어오면 fslex는 위에서부터 순서대로 확인합니다. `ident_start ident_char*` 패턴이 먼저 있으니 "let"을 IDENT 토큰으로 인식해버리고, 그 밑의 `"let"` 규칙은 절대 실행되지 않습니다.

[화면: 올바른 순서로 다시 돌아가기]

그래서 **키워드는 항상 식별자 규칙보다 먼저** 와야 합니다. 이건 fslex뿐만 아니라 대부분의 렉서 생성기에서 지켜야 하는 황금 규칙이에요.

### 섹션 4: Parser 확장 (6:00)

[화면: Parser.fsy 파일]

파서에도 새로운 토큰과 문법 규칙을 추가합니다.

```fsharp
%token <string> IDENT
%token LET IN EQUALS

Expr:
    | LET IDENT EQUALS Expr IN Expr  { Let($2, $4, $6) }
    | Expr PLUS Term     { Add($1, $3) }
    | Expr MINUS Term    { Subtract($1, $3) }
    | Term               { $1 }

Factor:
    | NUMBER             { Number($1) }
    | IDENT              { Var($1) }
    | LPAREN Expr RPAREN { $2 }
    | MINUS Factor       { Negate($2) }
```

[화면: Expr 규칙의 첫 번째 줄 강조]

Let 표현식이 `Expr` 레벨에 있다는 게 중요합니다. 이는 Let이 가장 낮은 우선순위를 가진다는 의미예요.

[화면: 파싱 예시 애니메이션]

```
let x = 2 + 3 in x * 4
```

이 코드가 어떻게 파싱될까요?

```
Let("x", Add(2, 3), Multiply(Var "x", 4))
```

[화면: 파싱 트리 시각화]

`2 + 3`이 먼저 파싱되고, 그 다음 `x * 4`가 파싱되고, 마지막에 Let으로 감싸집니다. 우선순위가 제대로 작동하네요!

### 섹션 5: 환경(Environment) 개념 (8:00)

자, 이제 가장 중요한 부분입니다. 변수를 어떻게 평가할까요?

[화면: 빈 화면에 질문 표시]

```
let x = 5 in x + 1
```

이 코드를 평가할 때, `x + 1`을 계산하는 시점에 `x`가 5라는 걸 어떻게 알 수 있을까요?

[화면: Environment 개념 등장 - Map 자료구조 시각화]

답은 **환경(Environment)**입니다. 환경은 변수 이름에서 값으로의 매핑이에요. F#의 `Map` 타입을 사용합니다.

```fsharp
type Env = Map<string, int>

let emptyEnv : Env = Map.empty
```

[화면: 테이블 형태로 시각화]

```
Environment:
┌──────┬───────┐
│ 이름 │  값   │
├──────┼───────┤
│  x   │   5   │
│  y   │  10   │
└──────┴───────┘
```

이제 모든 eval 함수에 이 환경을 전달하면 됩니다!

### 섹션 6: Evaluator 구현 (9:30)

[화면: Eval.fs 파일]

```fsharp
let rec eval (env: Env) (expr: Expr) : int =
    match expr with
    | Number n -> n

    | Var name ->
        match Map.tryFind name env with
        | Some value -> value
        | None -> failwithf "Undefined variable: %s" name

    | Let (name, binding, body) ->
        // 1. 현재 환경에서 바인딩 평가
        let value = eval env binding
        // 2. 환경 확장
        let extendedEnv = Map.add name value env
        // 3. 확장된 환경에서 본문 평가
        eval extendedEnv body
```

[화면: 코드를 섹션별로 강조하며 설명]

`Var`를 만나면 환경에서 이름을 찾습니다. 없으면 에러를 던지고요.

`Let`이 핵심인데요, 3단계로 진행됩니다:

[화면: 단계별 애니메이션]

```
let x = 2 + 3 in x * 4
```

1단계: 현재 환경 `{}`에서 `2 + 3`을 평가 → 5

[화면: env = {} → value = 5 표시]

2단계: 환경에 x를 추가 → `{x: 5}`

[화면: extendedEnv = {x: 5} 표시]

3단계: 확장된 환경에서 `x * 4`를 평가
- `x`를 환경에서 찾으면 5
- `5 * 4 = 20`

[화면: 최종 결과 20 표시]

### 섹션 7: 중첩 스코프 (11:30)

이제 재미있는 부분입니다. Let을 여러 번 사용하면 어떻게 될까요?

[화면: 코드 예시]

```
let x = 5 in let y = x + 1 in y
```

[화면: 단계별 환경 변화 애니메이션]

```
1. env = {}
2. {x: 5}에서 x + 1 평가 → 6
3. {x: 5, y: 6}에서 y 평가 → 6
결과: 6
```

내부 Let이 외부 변수에 접근할 수 있습니다. 이게 바로 **렉시컬 스코프**의 핵심이에요. 코드의 구조가 변수의 가시성을 결정하는 거죠.

### 섹션 8: 섀도잉(Shadowing) (12:40)

더 흥미로운 케이스를 봅시다.

[화면: 섀도잉 예시]

```
let x = 1 in let x = 2 in x
```

같은 이름 `x`를 두 번 바인딩했습니다. 결과는 어떻게 될까요?

[화면: 단계별 진행]

```
1. env = {}
2. env1 = {x: 1}
3. env2 = {x: 2}  (Map.add가 값을 덮어씀)
4. x 평가 → 2
```

내부의 `x`가 외부의 `x`를 가립니다. 이걸 **섀도잉**이라고 해요.

[화면: 더 복잡한 예시]

그런데 여기서 중요한 질문! 내부 스코프가 끝나면 어떻게 될까요?

```
let x = 1 in (let x = 2 in x) + x
```

[화면: 괄호 안과 밖을 강조]

```
1. env = {}
2. env1 = {x: 1}
3. (let x = 2 in x) 평가:
   - env2 = {x: 2}
   - 결과: 2
4. + 연산의 오른쪽 x는 env1에서 평가 → 1
5. 2 + 1 = 3
```

[화면: 불변 자료구조 개념 설명]

여기서 F#의 **불변 Map**이 빛을 발합니다. `Map.add`는 원본을 수정하지 않고 새 맵을 반환하기 때문에, 내부 스코프의 env2가 외부 스코프의 env1에 영향을 주지 않아요.

이게 바로 함수형 프로그래밍의 아름다움입니다!

### 섹션 9: 실행 데모 (14:30)

[화면: 터미널 전체 화면]

자, 직접 실행해볼까요?

```bash
$ dotnet run -- --expr "let x = 5 in x"
5

$ dotnet run -- --expr "let x = 2 + 3 in x * 4"
20

$ dotnet run -- --expr "let x = 1 in let y = 2 in x + y"
3

$ dotnet run -- --expr "let x = 1 in (let x = 2 in x) + x"
3
```

완벽하게 작동하네요!

[화면: 에러 케이스]

정의되지 않은 변수를 사용하면?

```bash
$ dotnet run -- --expr "y + 1"
Error: Undefined variable: y
```

제대로 에러를 잡아냅니다.

[화면: 디버깅 옵션]

디버깅 옵션도 사용해봅시다:

```bash
$ dotnet run -- --emit-ast --expr "let x = 1 in let y = 2 in x + y"
Let(x, Number(1), Let(y, Number(2), Add(Var(x), Var(y))))
```

AST가 우리가 의도한 대로 만들어졌어요!

### 섹션 10: 테스트 확인 (15:40)

[화면: 테스트 실행]

```bash
$ make -C tests variables
...
PASS: all 12 tests passed

$ dotnet run --project FunLang.Tests
[13:25:01 INF] EXPECTO! 58 tests run – 58 passed, 0 failed. Success!
```

모든 테스트가 통과합니다!

## 아웃트로 (16:30)

오늘 우리는 단순한 계산기에서 변수를 다룰 수 있는 언어로 진화시켰습니다.

[화면: 핵심 개념 요약 슬라이드]

핵심 개념을 정리하면:
- **Environment**: 변수 이름을 값으로 매핑하는 Map
- **Lexical Scope**: 코드 구조가 변수 가시성을 결정
- **Shadowing**: 같은 이름 재바인딩으로 내부 값 우선
- **Immutable Map**: 스코프 간 격리를 자동으로 보장

[화면: 키워드 우선순위 규칙 강조]

그리고 fslex의 중요한 실전 팁:
- **키워드는 항상 식별자 규칙보다 먼저!**

[화면: 다음 에피소드 미리보기]

다음 시간에는 Boolean 타입과 if-then-else 조건문을 추가해서, 우리 언어에 의사결정 능력을 부여하겠습니다.

```
if x > 0 then x else -x
```

절댓값을 계산하는 이런 코드를 작성할 수 있게 되는 거죠!

그럼 다음 에피소드에서 만나요. 구독과 좋아요 부탁드립니다!

## 핵심 키워드

- Environment (환경)
- Let binding (Let 바인딩)
- Lexical scope (렉시컬 스코프)
- Shadowing (섀도잉)
- Immutable Map (불변 맵)
- Keyword priority (키워드 우선순위)
- fslex
- fsyacc
- Variable reference (변수 참조)
- Scope isolation (스코프 격리)
