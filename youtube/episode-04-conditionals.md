# Episode 4: 조건문과 Boolean

**예상 길이:** 18-20분
**난이도:** 중급

---

## 썸네일 텍스트

```
F#으로 언어 만들기
Episode 4: if-then-else!
타입 검사까지
```

---

## 인트로 (0:00 - 1:30)

[화면: 코드 예시]

안녕하세요! 지금까지 우리 언어 FunLang은:
- 사칙연산 ✓
- 변수 바인딩 ✓

하지만 뭔가 부족하죠. **분기**가 없어요!

```
"점수가 60점 이상이면 합격"
```

이런 로직을 표현할 수 없습니다.

오늘은 **if-then-else**와 **Boolean 타입**을 추가합니다.

그리고 여기서 중요한 개념이 등장해요.
바로 **타입 검사(Type Checking)**입니다.

`5 + true`는 말이 안 되잖아요? 이런 걸 잡아내야 해요.

---

## Value 타입 도입 (1:30 - 4:00)

[화면: 문제 제기]

지금까지 `eval`은 `int`를 반환했어요.

```fsharp
let rec eval (env: Env) (expr: Expr) : int = ...
```

그런데 Boolean을 추가하면?

```fsharp
eval "5 > 3"  // true를 반환해야 함
eval "5 + 1"  // 6을 반환해야 함
```

반환 타입이 int일 수도, bool일 수도 있어요.

[해결책]

**Discriminated Union**으로 해결합니다!

```fsharp
type Value =
    | IntValue of int
    | BoolValue of bool
```

이제 `eval`은 `Value`를 반환해요.

```fsharp
let rec eval (env: Env) (expr: Expr) : Value = ...
```

[강조]

이게 바로 F#의 강점이에요.

다른 언어에서는 `Object`를 반환하고 캐스팅해야 할 텐데,
F#에서는 **가능한 타입을 명시적으로 열거**합니다.

패턴 매칭으로 안전하게 처리할 수 있죠.

---

## AST 확장 (4:00 - 6:00)

[화면: Ast.fs 편집]

제어 흐름 노드들을 추가합니다.

```fsharp
type Expr =
    // ... 기존 노드들 ...
    // Phase 4: Control flow
    | Bool of bool            // true, false
    | If of Expr * Expr * Expr  // if cond then e1 else e2
    // 비교 연산자
    | Equal of Expr * Expr       // =
    | NotEqual of Expr * Expr    // <>
    | LessThan of Expr * Expr    // <
    | GreaterThan of Expr * Expr // >
    | LessEqual of Expr * Expr   // <=
    | GreaterEqual of Expr * Expr // >=
    // 논리 연산자
    | And of Expr * Expr  // &&
    | Or of Expr * Expr   // ||
```

많아 보이지만 패턴은 비슷해요.

---

## 연산자 우선순위 (6:00 - 8:30)

[화면: 우선순위 표]

새 연산자들의 우선순위를 정해야 해요.

```
낮음 ←――――――――――――――――――――――→ 높음

  ||    &&    비교    +,-    *,/    단항-
```

`5 > 3 && 2 < 4`를 파싱하면:
- 먼저 `5 > 3` → `true`
- 그 다음 `2 < 4` → `true`
- 마지막으로 `true && true` → `true`

[Parser 코드]

```fsharp
// Parser.fsy
%left OR
%left AND
%nonassoc EQUALS LT GT LE GE NE
```

`%nonassoc`은 **비연관성**이에요.

`1 < 2 < 3` 같은 표현을 **에러**로 만듭니다.

왜? 수학에서는 "1 < 2이고 2 < 3"을 의미하지만,
대부분 언어에서는 `(1 < 2) < 3` → `true < 3` → 타입 에러!

혼란을 막기 위해 아예 금지하는 거예요.

---

## 타입 검사 Evaluator (8:30 - 12:30)

[화면: Eval.fs 편집]

이제 Evaluator에서 **타입을 검사**합니다.

```fsharp
| Add (left, right) ->
    match eval env left, eval env right with
    | IntValue l, IntValue r -> IntValue (l + r)
    | _ -> failwith "Type error: + requires integer operands"
```

[강조]

`+`는 **정수만** 받아요. Boolean이 들어오면 에러!

```fsharp
| LessThan (left, right) ->
    match eval env left, eval env right with
    | IntValue l, IntValue r -> BoolValue (l < r)
    | _ -> failwith "Type error: < requires integer operands"
```

비교 연산자는 정수를 받아서 **Boolean을 반환**해요.

[동등 비교는 특별]

```fsharp
| Equal (left, right) ->
    match eval env left, eval env right with
    | IntValue l, IntValue r -> BoolValue (l = r)
    | BoolValue l, BoolValue r -> BoolValue (l = r)
    | _ -> failwith "Type error: = requires operands of same type"
```

`=`는 **같은 타입끼리** 비교 가능해요.
`5 = 5` ✓
`true = false` ✓
`5 = true` ✗

---

## 단락 평가 (Short-circuit) (12:30 - 14:30)

[화면: 단락 평가 다이어그램]

논리 연산자에는 **단락 평가**가 있어요.

```fsharp
| And (left, right) ->
    match eval env left with
    | BoolValue false -> BoolValue false  // 여기서 끝!
    | BoolValue true ->
        match eval env right with
        | BoolValue b -> BoolValue b
        | _ -> failwith "Type error"
    | _ -> failwith "Type error"
```

`false && (뭐든)` → 오른쪽 **평가 안 함**!

[왜 중요한가?]

```
x <> 0 && 10 / x > 1
```

x가 0이면?
- `x <> 0`이 `false`
- 단락 평가로 `10 / x`는 **실행 안 됨**
- 0으로 나누기 에러 방지!

---

## If-Then-Else (14:30 - 15:30)

[화면: If 평가]

```fsharp
| If (condition, thenBranch, elseBranch) ->
    match eval env condition with
    | BoolValue true -> eval env thenBranch
    | BoolValue false -> eval env elseBranch
    | _ -> failwith "Type error: if condition must be boolean"
```

조건이 Boolean이 아니면 **타입 에러**!

`if 1 then 2 else 3` → 에러
`if true then 2 else 3` → 2

---

## 데모 (15:30 - 17:30)

[화면: 터미널]

```bash
$ funlang --expr "true"
true

$ funlang --expr "false"
false

$ funlang --expr "5 > 3"
true

$ funlang --expr "if true then 1 else 2"
1

$ funlang --expr "if 5 > 3 then 10 else 20"
10
```

[논리 연산자]

```bash
$ funlang --expr "if true && true then 1 else 0"
1

$ funlang --expr "if false || true then 1 else 0"
1
```

[타입 에러]

```bash
$ funlang --expr "if 1 then 2 else 3"
Error: Type error: if condition must be boolean

$ funlang --expr "true + 1"
Error: Type error: + requires integer operands

$ funlang --expr "5 = true"
Error: Type error: = requires operands of same type
```

타입 에러가 잘 잡히네요!

---

## 마무리 (17:30 - 19:00)

[화면: 요약 슬라이드]

오늘 배운 핵심:

| 개념 | 설명 |
|------|------|
| **Value Type** | IntValue \| BoolValue로 다형성 표현 |
| **Type Check** | 연산자마다 피연산자 타입 검증 |
| **Short-circuit** | && \|\|에서 필요할 때만 평가 |
| **Precedence** | || < && < 비교 < 산술 |

[다음 예고]

다음은 **마지막 에피소드**입니다!

**함수**를 추가해서 FunLang을 **Turing-complete**하게 만들어요.

```bash
$ funlang --expr "let rec fact n = if n <= 1 then 1 else n * fact (n - 1) in fact 5"
120
```

팩토리얼을 재귀로 계산할 수 있게 됩니다!

[마무리]

코드는 GitHub에 있어요. 다음 에피소드에서 만나요!

---

## B-roll / 화면 전환 제안

- 0:00 - 분기 필요성 인트로
- 1:30 - Value 타입 다이어그램
- 6:00 - 우선순위 표 애니메이션
- 12:30 - 단락 평가 시각화
- 15:30 - 터미널 데모 (실시간)

---

## 태그

F#, 프로그래밍 언어, 조건문, Boolean, 타입 검사, if-then-else, 단락 평가
