# Episode 3: 변수와 스코프

**예상 길이:** 15-18분
**난이도:** 중급

---

## 썸네일 텍스트

```
F#으로 언어 만들기
Episode 3: 변수 추가!
let x = 5 in x + 1
```

---

## 인트로 (0:00 - 1:30)

[화면: 계산기 vs 변수]

안녕하세요! 지난 에피소드에서 사칙연산 계산기를 완성했죠.

하지만 계산기에는 한계가 있어요.

```bash
$ funlang --expr "3.14159 * 10 * 10 + 3.14159 * 10 * 2"
```

원의 넓이에 옆면 넓이를 더하는 건데... 3.14159를 두 번이나 썼네요.

만약 변수가 있다면?

```
let pi = 3 in
let r = 10 in
pi * r * r + pi * r * 2
```

훨씬 깔끔하죠!

오늘은 **변수 바인딩**과 **스코프**를 구현해 봅니다.

---

## 환경(Environment)이란? (1:30 - 3:30)

[화면: 환경 다이어그램]

변수를 지원하려면 **환경(Environment)**이 필요해요.

환경은 간단히 말해서 **변수 이름 → 값** 매핑입니다.

```
환경 = { x: 5, y: 10, pi: 3 }
```

F#에서는 `Map<string, int>`로 표현할 수 있어요.

[화면: 환경 전파 애니메이션]

```
let x = 5 in x + 1
```

1. 빈 환경 `{}`에서 시작
2. `5`를 계산해서 `x`에 바인딩: `{x: 5}`
3. 이 환경에서 `x + 1` 평가
4. `x`를 조회하면 `5`
5. `5 + 1 = 6`

---

## AST 확장 (3:30 - 5:00)

[화면: Ast.fs 편집]

두 가지 노드를 추가합니다.

```fsharp
type Expr =
    // ... 기존 노드들 ...
    | Var of string                    // 변수 참조
    | Let of string * Expr * Expr      // let name = expr1 in expr2
```

`Let`은 세 부분으로 구성돼요:
- 변수 이름 (`string`)
- 바인딩할 값 (`Expr`)
- 본문 (`Expr`)

예시:
```
let x = 5 in x + 1

Let("x", Number 5, Add(Var "x", Number 1))
```

---

## Lexer: 키워드 우선순위 (5:00 - 7:00)

[화면: Lexer.fsl 편집 + 경고]

토큰을 추가할 때 **순서가 중요**합니다!

```fsharp
// 키워드는 반드시 식별자보다 먼저!
| "let"         { LET }
| "in"          { IN }
// 그 다음에 식별자
| ident_start ident_char* { IDENT (lexeme lexbuf) }
```

왜 그럴까요?

`let`이라는 입력이 들어오면:
- `"let"` 규칙도 매치
- `ident_start ident_char*` 규칙도 매치

fslex는 **먼저 나온 규칙**을 적용해요.

[잘못된 순서 보여주기]

```fsharp
// 잘못된 순서!
| ident_start ident_char* { IDENT (lexeme lexbuf) }
| "let"         { LET }  // 절대 도달 안 함!
```

이러면 `let`이 IDENT로 인식돼서 파싱 에러가 납니다.

---

## Evaluator: 환경 전달 (7:00 - 10:30)

[화면: Eval.fs 편집]

이제 핵심, Evaluator를 수정합니다.

```fsharp
type Env = Map<string, int>

let emptyEnv : Env = Map.empty

let rec eval (env: Env) (expr: Expr) : int =
    match expr with
    | Number n -> n

    | Var name ->
        match Map.tryFind name env with
        | Some value -> value
        | None -> failwithf "Undefined variable: %s" name

    | Let (name, binding, body) ->
        let value = eval env binding         // 1. 바인딩 평가
        let extendedEnv = Map.add name value env  // 2. 환경 확장
        eval extendedEnv body                // 3. 본문 평가

    | Add (left, right) ->
        eval env left + eval env right
    // ... 나머지 연산자들도 env 전달 ...
```

[강조]

세 가지 핵심 포인트:

1. **환경 전파** - 모든 재귀 호출에 `env` 전달
2. **환경 확장** - `Map.add`로 새 바인딩 추가
3. **불변성** - 원본 환경은 변하지 않음

---

## 중첩 스코프 (10:30 - 12:00)

[화면: 스코프 다이어그램]

```fsharp
let x = 1 in let y = 2 in x + y
```

이건 어떻게 동작할까요?

```
1. env = {}
2. x = 1 바인딩 → {x: 1}
3. y = 2 바인딩 → {x: 1, y: 2}
4. x + y = 1 + 2 = 3
```

내부 Let은 외부 변수에 접근 가능해요.

---

## 섀도잉 (Shadowing) (12:00 - 14:00)

[화면: 섀도잉 예제]

같은 이름으로 다시 바인딩하면?

```fsharp
let x = 1 in let x = 2 in x
```

결과는 `2`입니다.

내부의 `x = 2`가 외부의 `x = 1`을 **섀도잉**(가림) 해요.

[더 복잡한 예제]

```fsharp
let x = 1 in (let x = 2 in x) + x
```

결과는 `3`!

- 괄호 안: `x = 2` → 결과 2
- 괄호 밖: `x = 1` → 결과 1
- 합계: 3

[강조]

**불변 맵** 덕분에 내부 스코프가 외부에 영향을 주지 않아요.

`Map.add`는 새 맵을 반환하지, 기존 맵을 수정하지 않거든요.

---

## 데모 (14:00 - 15:30)

[화면: 터미널]

```bash
$ funlang --expr "let x = 5 in x"
5

$ funlang --expr "let x = 5 in x + 1"
6

$ funlang --expr "let x = 1 in let y = 2 in x + y"
3

$ funlang --expr "let x = 1 in let x = 2 in x"
2

$ funlang --expr "let x = 1 in (let x = 2 in x) + x"
3
```

[정의되지 않은 변수]

```bash
$ funlang --expr "y + 1"
Error: Undefined variable: y
```

에러 메시지도 잘 나오네요!

---

## 마무리 (15:30 - 17:00)

[화면: 요약 슬라이드]

오늘 배운 핵심 개념:

| 개념 | 설명 |
|------|------|
| **Environment** | 변수 이름 → 값 매핑 |
| **Lexical Scope** | 코드 구조가 변수 가시성 결정 |
| **Shadowing** | 같은 이름 재바인딩 시 내부 값 우선 |
| **Immutable Map** | 스코프 간 격리 보장 |

[다음 예고]

다음 에피소드에서는 **조건문**을 추가합니다!

```bash
$ funlang --expr "if 5 > 3 then 10 else 20"
10
```

if-then-else와 Boolean 타입을 구현할 거예요.

[마무리]

코드는 GitHub에 있습니다. 다음 에피소드에서 만나요!

---

## B-roll / 화면 전환 제안

- 0:00 - 변수 필요성 비교 애니메이션
- 1:30 - 환경 다이어그램 애니메이션
- 5:00 - "주의!" 경고 박스
- 10:30 - 스코프 중첩 애니메이션
- 12:00 - 섀도잉 시각화

---

## 태그

F#, 프로그래밍 언어, 변수, 스코프, 환경, 렉시컬 스코프, 인터프리터
