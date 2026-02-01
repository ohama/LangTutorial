# Episode 2: 사칙연산 계산기 만들기

**예상 길이:** 15-18분
**난이도:** 초급

---

## 썸네일 텍스트

```
F#으로 언어 만들기
Episode 2: 계산기 완성!
2 + 3 * 4 = ?
```

---

## 인트로 (0:00 - 1:00)

[화면: 터미널에서 계산기 실행]

```bash
$ funlang --expr "2 + 3 * 4"
14
```

안녕하세요! 지난 에피소드에서 우리는 숫자 하나를 파싱하는 파이프라인을 만들었죠.

오늘은 드디어 **사칙연산**을 추가해서 실제로 계산하는 인터프리터를 완성합니다!

그런데요, 계산기 만들기... 쉬워 보이죠?

`2 + 3 * 4`

이게 11일까요, 14일까요?

네, **연산자 우선순위** 때문에 14가 맞습니다. 곱셈이 덧셈보다 먼저니까요.

이걸 어떻게 처리하는지, 오늘 함께 알아봅시다.

---

## AST 확장 (1:00 - 3:00)

[화면: Ast.fs 편집]

먼저 AST에 연산자들을 추가합니다.

```fsharp
type Expr =
    | Number of int
    | Add of Expr * Expr
    | Subtract of Expr * Expr
    | Multiply of Expr * Expr
    | Divide of Expr * Expr
    | Negate of Expr  // 단항 마이너스 (-5)
```

[손으로 그리면서]

`2 + 3 * 4`의 AST는 이렇게 됩니다:

```
    Add
   /   \
  2   Multiply
      /    \
     3      4
```

**곱셈이 덧셈의 자식**이에요. 그래서 곱셈이 먼저 계산되는 거죠.

---

## Evaluator 만들기 (3:00 - 5:30)

[화면: Eval.fs 생성]

이제 AST를 실제로 계산하는 Evaluator를 만들어요.

```fsharp
// Eval.fs
module Eval

open Ast

let rec eval (expr: Expr) : int =
    match expr with
    | Number n -> n
    | Add (left, right) -> eval left + eval right
    | Subtract (left, right) -> eval left - eval right
    | Multiply (left, right) -> eval left * eval right
    | Divide (left, right) -> eval left / eval right
    | Negate e -> -(eval e)
```

[강조하면서]

보세요, **패턴 매칭**이 얼마나 깔끔한가요!

각 케이스별로 어떻게 계산할지 명확하게 표현됩니다.

`Add (left, right)` → 왼쪽 평가하고, 오른쪽 평가하고, 더하기.

재귀적으로 트리를 타고 내려가면서 계산하는 거예요.

---

## 핵심! Expr/Term/Factor 패턴 (5:30 - 9:00)

[화면: 문법 다이어그램]

자, 이제 **가장 중요한 부분**입니다.

Parser에서 연산자 우선순위를 어떻게 처리할까요?

fsyacc에는 `%left`, `%right` 같은 선언이 있는데요...
솔직히 말하면 **버그가 좀 있어요**.

그래서 우리는 더 안전한 방법을 쓸 겁니다.
바로 **Expr / Term / Factor** 패턴!

[화면: 문법 규칙]

```
낮은 우선순위 ←――――――――――→ 높은 우선순위

    Expr          Term          Factor
    (+, -)        (*, /)        (숫자, 괄호)
```

코드로 보면:

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

[애니메이션으로 파싱 과정 보여주기]

`2 + 3 * 4`를 파싱해 봅시다:

1. `2`는 Factor → Term → Expr
2. `+`를 만남, Expr + Term 규칙 적용
3. `3 * 4`는 Term * Factor 규칙으로 먼저 묶임
4. 결과: `Add(2, Multiply(3, 4))`

**문법 구조 자체가 우선순위를 표현**하는 거예요.
%left, %right 없이도 완벽하게 동작합니다!

---

## Lexer 확장 (9:00 - 10:30)

[화면: Lexer.fsl 편집]

토큰을 추가해 줍니다.

```fsharp
rule tokenize = parse
    | whitespace+   { tokenize lexbuf }
    | digit+        { NUMBER (Int32.Parse(lexeme lexbuf)) }
    | '+'           { PLUS }
    | '-'           { MINUS }
    | '*'           { STAR }
    | '/'           { SLASH }
    | '('           { LPAREN }
    | ')'           { RPAREN }
    | eof           { EOF }
```

간단하죠? 한 글자씩 토큰으로 변환하면 됩니다.

참고로 `MINUS`는 뺄셈에도 쓰이고, 단항 마이너스에도 쓰여요.
둘을 구분하는 건 **Parser의 역할**입니다.

---

## CLI 만들기 (10:30 - 12:00)

[화면: Program.fs 편집]

이제 커맨드 라인에서 사용할 수 있게 만들어 봅시다.

```fsharp
[<EntryPoint>]
let main argv =
    match argv with
    | [| "--expr"; expr |] ->
        let result = expr |> parse |> eval
        printfn "%d" result
        0
    | _ ->
        eprintfn "Usage: funlang --expr <expression>"
        1
```

`--expr` 뒤에 표현식을 받아서 계산 결과를 출력합니다.

---

## 데모 타임! (12:00 - 14:30)

[화면: 터미널 전체 화면]

자, 이제 실행해 볼까요?

```bash
$ dotnet run --project FunLang -- --expr "2 + 3"
5

$ dotnet run --project FunLang -- --expr "10 - 4"
6

$ dotnet run --project FunLang -- --expr "3 * 4"
12

$ dotnet run --project FunLang -- --expr "20 / 4"
5
```

[우선순위 테스트]

```bash
$ dotnet run --project FunLang -- --expr "2 + 3 * 4"
14
```

14! 곱셈이 먼저 계산됐죠.

[괄호 테스트]

```bash
$ dotnet run --project FunLang -- --expr "(2 + 3) * 4"
20
```

괄호로 우선순위 바꾸기도 됩니다.

[단항 마이너스]

```bash
$ dotnet run --project FunLang -- --expr "-5 + 3"
-2

$ dotnet run --project FunLang -- --expr "--5"
5

$ dotnet run --project FunLang -- --expr "-(2 + 3)"
-5
```

단항 마이너스도 완벽하게 동작해요!

---

## 좌결합성 (14:30 - 15:30)

[화면: 수식 분석]

하나 더 볼게요.

```bash
$ dotnet run --project FunLang -- --expr "10 - 3 - 2"
5
```

`(10 - 3) - 2 = 5`죠.

만약 오른쪽부터 계산했다면?
`10 - (3 - 2) = 9`가 됐을 거예요.

우리 문법이 **좌결합(left-associative)**으로 동작한다는 증거입니다.

```fsharp
Expr:
    | Expr PLUS Term   // Expr가 왼쪽에 있으니까 좌결합!
```

왼쪽 재귀(left recursion) 덕분이에요.

---

## 마무리 (15:30 - 17:00)

[화면: 요약 슬라이드]

오늘 만든 것:

1. **AST 확장** - 사칙연산 노드 추가
2. **Evaluator** - 패턴 매칭으로 계산
3. **Expr/Term/Factor 패턴** - 우선순위 처리
4. **CLI** - `--expr` 옵션으로 실행

[화면: 다음 예고]

다음 에피소드에서는 **변수**를 추가합니다!

```bash
$ funlang --expr "let x = 5 in x + 1"
6
```

이렇게 값을 저장하고 재사용할 수 있게 되는 거죠.

[화면: 구독/좋아요]

코드는 GitHub에 있습니다. 설명란 확인하세요!

질문은 댓글로 남겨주시고, 다음 에피소드에서 만나요!

---

## B-roll / 화면 전환 제안

- 0:00 - 계산 결과 애니메이션
- 1:00 - AST 트리 다이어그램 애니메이션
- 5:30 - "핵심!" 강조 효과
- 9:00 - 코드 하이라이트
- 12:00 - 터미널 데모 (실시간 타이핑)

---

## 태그

F#, 프로그래밍 언어, 계산기, 파서, 연산자 우선순위, 인터프리터, 튜토리얼
