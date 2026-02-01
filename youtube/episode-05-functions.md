# Episode 5: 함수와 클로저 - Turing-complete 달성!

**예상 길이:** 20-25분
**난이도:** 고급

---

## 썸네일 텍스트

```
F#으로 언어 만들기
Episode 5: 함수 & 클로저
Turing-complete 달성!
```

---

## 인트로 (0:00 - 2:00)

[화면: 시리즈 회고]

안녕하세요! 드디어 마지막 에피소드입니다.

지금까지 우리는:
- 사칙연산 ✓
- 변수 ✓
- 조건문 ✓

하지만 아직 **함수**가 없어요.

```
"반복되는 로직을 재사용하고 싶다"
"재귀로 복잡한 계산을 하고 싶다"
```

오늘 함수를 추가하면 FunLang은 **Turing-complete**가 됩니다!

Turing-complete란? 이론적으로 **모든 계산 가능한 것을 계산**할 수 있다는 뜻이에요.

[화면: 팩토리얼 예시]

```bash
$ funlang --expr "let rec fact n = if n <= 1 then 1 else n * fact (n - 1) in fact 5"
120
```

이걸 만들어 봅시다!

---

## 함수란 무엇인가? (2:00 - 4:00)

[화면: 함수 구성 요소]

함수에는 세 가지가 필요해요:

1. **매개변수** (Parameter) - 입력을 받는 이름
2. **본문** (Body) - 실행할 코드
3. **클로저** (Closure) - 정의 시점의 환경!

```fsharp
let x = 10 in
let f = fun y -> x + y in
f 5  // 15
```

`f`가 정의될 때 `x = 10`이에요.
`f`는 이 환경을 **기억**합니다.

나중에 `f 5`를 호출하면:
- `y = 5`를 추가
- `x + y = 10 + 5 = 15`

이게 **클로저**의 핵심이에요!

---

## AST 확장 (4:00 - 6:00)

[화면: Ast.fs 편집]

세 가지 노드를 추가합니다.

```fsharp
type Expr =
    // ... 기존 노드들 ...
    | Lambda of param: string * body: Expr
    | App of func: Expr * arg: Expr
    | LetRec of name: string * param: string * body: Expr * inExpr: Expr
```

| 노드 | 구문 | 예시 |
|------|------|------|
| Lambda | `fun x -> body` | `fun x -> x + 1` |
| App | `f arg` | `f 5` |
| LetRec | `let rec f x = body in expr` | `let rec fact n = ... in fact 5` |

[Value에도 추가]

```fsharp
type Value =
    | IntValue of int
    | BoolValue of bool
    | FunctionValue of param: string * body: Expr * closure: Env
```

함수도 **값**이에요! 변수에 저장하고 전달할 수 있죠.

---

## 문법 충돌 해결 (6:00 - 9:00)

[화면: 문제 상황]

여기서 **재미있는 문제**가 생겨요.

```
f - 1
```

이게 뭘까요?

1. **뺄셈**: `f`에서 `1`을 뺀다
2. **함수 호출**: `f`에 `-1`을 전달한다

[해결책: Atom 비단말]

함수 인자에서 **단항 마이너스를 제외**합니다.

```fsharp
Factor:
    | MINUS Factor       { Negate($2) }
    | AppExpr            { $1 }

AppExpr:
    | AppExpr Atom       { App($1, $2) }  // Atom만!
    | Atom               { $1 }

Atom:
    | NUMBER             { Number($1) }
    | IDENT              { Var($1) }
    | LPAREN Expr RPAREN { $2 }
```

이제:
- `f - 1` → 뺄셈
- `f (-1)` → 함수 호출 (괄호로 명시)

[커링도 자연스럽게]

```
f 1 2 → App(App(f, 1), 2)
```

좌결합으로 파싱됩니다.

---

## Lambda 평가 (9:00 - 11:00)

[화면: Eval.fs - Lambda]

람다를 평가하면 **클로저를 생성**합니다.

```fsharp
| Lambda (param, body) ->
    FunctionValue (param, body, env)  // 현재 환경 캡처!
```

[시각화]

```
let x = 10 in fun y -> x + y

1. env = {x: 10}
2. Lambda 평가 → FunctionValue("y", Add(Var "x", Var "y"), {x: 10})
```

환경 `{x: 10}`이 함수 안에 저장됐어요.

이게 바로 **클로저**입니다!

---

## App 평가 (11:00 - 14:00)

[화면: Eval.fs - App]

함수 호출은 이렇게 동작해요:

```fsharp
| App (funcExpr, argExpr) ->
    match eval env funcExpr with
    | FunctionValue (param, body, closureEnv) ->
        let argValue = eval env argExpr       // 1. 인자 평가
        let callEnv = Map.add param argValue closureEnv  // 2. 클로저 환경 확장
        eval callEnv body                     // 3. 본문 평가
    | _ -> failwith "Type error: not a function"
```

[핵심 포인트]

**클로저의 환경**을 확장해요! 호출 시점 환경이 아니라요.

```
let x = 10 in
let f = fun y -> x + y in
let x = 100 in
f 5
```

결과는 `15`! (115가 아님)

`f`의 클로저에는 `x = 10`이 저장돼 있으니까요.

---

## 재귀 함수 (14:00 - 17:00)

[화면: 재귀 문제]

일반 `let`으로는 재귀가 안 돼요.

```fsharp
let fact = fun n -> if n <= 1 then 1 else n * fact (n - 1) in ...
```

왜? `fact`를 정의할 때 `fact`가 아직 환경에 없거든요!

[LetRec 해결책]

```fsharp
| LetRec (name, param, funcBody, inExpr) ->
    let funcVal = FunctionValue (param, funcBody, env)
    let recEnv = Map.add name funcVal env
    eval recEnv inExpr
```

그런데 이것만으로는 부족해요.

[App에서 자기 참조 추가]

```fsharp
| App (funcExpr, argExpr) ->
    let funcVal = eval env funcExpr
    match funcVal with
    | FunctionValue (param, body, closureEnv) ->
        let argValue = eval env argExpr
        // 재귀: 자기 자신을 클로저에 추가!
        let augmentedClosureEnv =
            match funcExpr with
            | Var name -> Map.add name funcVal closureEnv
            | _ -> closureEnv
        let callEnv = Map.add param argValue augmentedClosureEnv
        eval callEnv body
```

[동작 원리]

```
let rec fact n = if n <= 1 then 1 else n * fact (n - 1) in fact 3

1. fact 3 호출
2. fact을 클로저에 추가
3. n = 3, 조건 false, n * fact (n - 1) 평가
4. fact 2 호출 (클로저에 fact 있음!)
5. ... 반복 ...
6. fact 1 → 1 반환
7. 1 * 2 * 3 = 6
```

---

## 데모! (17:00 - 20:00)

[화면: 터미널 전체 화면]

드디어 데모 시간!

```bash
# 기본 람다
$ funlang --expr "fun x -> x + 1"
<function>

# 함수 호출
$ funlang --expr "let f = fun x -> x + 1 in f 5"
6

# 커링
$ funlang --expr "let add = fun x -> fun y -> x + y in add 3 4"
7
```

[재귀 함수]

```bash
# 팩토리얼!
$ funlang --expr "let rec fact n = if n <= 1 then 1 else n * fact (n - 1) in fact 5"
120

# 피보나치!
$ funlang --expr "let rec fib n = if n <= 1 then n else fib (n - 1) + fib (n - 2) in fib 10"
55
```

[클로저]

```bash
# 클로저 테스트
$ funlang --expr "let x = 10 in let f = fun y -> x + y in f 5"
15

# 섀도잉 후에도 클로저 유지
$ funlang --expr "let x = 1 in let f = fun y -> x + y in let x = 100 in f 5"
6
```

x가 100으로 바뀌어도 f는 여전히 x = 1을 기억해요!

---

## 시리즈 마무리 (20:00 - 23:00)

[화면: 전체 회고]

여러분, 축하합니다!

5개 에피소드에 걸쳐 우리는 **Turing-complete 언어**를 만들었어요.

| 에피소드 | 기능 |
|----------|------|
| 1 | 프로젝트 설정, 파이프라인 |
| 2 | 사칙연산, 우선순위 |
| 3 | 변수, 스코프 |
| 4 | 조건문, Boolean, 타입 검사 |
| 5 | 함수, 재귀, 클로저 |

[코드 통계]

- 약 2,000줄 F#
- 195개 테스트
- 5개 튜토리얼 문서

[더 갈 수 있는 방향]

- **타입 시스템** - 정적 타입 검사
- **리스트** - 데이터 구조
- **패턴 매칭** - match 표현식
- **REPL** - 대화형 셸

이건 여러분의 숙제로 남겨 둘게요!

[마무리]

이 시리즈가 도움이 됐다면 **구독과 좋아요** 부탁드려요.

전체 코드는 GitHub에 있습니다. 설명란 확인하세요!

프로그래밍 언어 만들기, 생각보다 어렵지 않죠?
여러분도 자신만의 언어를 만들어 보세요!

감사합니다!

---

## B-roll / 화면 전환 제안

- 0:00 - 시리즈 회고 몽타주
- 2:00 - 클로저 개념 애니메이션
- 6:00 - 문법 충돌 시각화
- 9:00 - Lambda 평가 단계별 애니메이션
- 14:00 - 재귀 호출 스택 시각화
- 17:00 - 터미널 데모 (드라마틱한 음악)
- 20:00 - 전체 회고 슬라이드

---

## 엔딩 카드 텍스트

```
FunLang - Turing-complete!

GitHub: [링크]
다음 시리즈: 타입 시스템? REPL?

구독 & 좋아요!
```

---

## 태그

F#, 프로그래밍 언어, 함수, 클로저, 재귀, Turing-complete, 인터프리터, 람다
