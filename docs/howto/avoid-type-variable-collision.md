---
created: 2026-02-01
description: Prelude 타입 스킴과 fresh 타입 변수 간 충돌 방지 패턴
---

# 타입 변수 충돌 방지 패턴

Prelude 함수의 다형성 타입을 정의할 때, bound variable과 fresh variable이 충돌하지 않도록 번호 범위를 분리한다.

## The Insight

타입 변수는 정수로 표현된다. Prelude에서 `Scheme([0], TArrow(TVar 0, TVar 0))`처럼 작은 정수를 사용하고, freshVar가 0부터 시작하면 충돌한다. **서로 다른 출처의 변수는 번호 범위를 분리해야 한다.**

## Why This Matters

충돌하면 이런 일이 벌어진다:

```fsharp
// Prelude: id = forall 0. 0 -> 0
// freshVar가 0부터 시작하면...

// 추론 중: let x = 5 in id x
// x: 'a (freshVar = TVar 0)
// id 인스턴스화: TVar 0 -> TVar 0

// 문제: scheme의 TVar 0과 fresh TVar 0이 같은 변수로 취급됨!
// id가 x의 타입에 의존하게 됨
```

증상:
- Prelude 함수가 예상과 다른 타입으로 추론됨
- 같은 Prelude 함수를 두 번 사용할 때 충돌
- 타입 오류가 없어야 할 곳에서 발생

## Recognition Pattern

다음 상황에서 이 패턴이 필요하다:

- 표준 라이브러리 함수에 다형성 타입 부여
- 타입 환경에 미리 정의된 scheme 추가
- 여러 출처의 타입 변수가 한 시스템에서 공존

## The Approach

### Step 1: Prelude 변수 범위 예약

Prelude scheme에는 0-9 또는 0-99 같은 작은 범위를 사용한다.

```fsharp
// TypeCheck.fs
let initialTypeEnv: TypeEnv =
    Map.ofList [
        // 0, 1 사용
        "map", Scheme([0; 1], TArrow(TArrow(TVar 0, TVar 1), ...))

        // 0만 사용
        "filter", Scheme([0], TArrow(TArrow(TVar 0, TBool), ...))

        // 0, 1, 2 사용
        "compose", Scheme([0; 1; 2], ...)
    ]
```

### Step 2: freshVar 시작점 이동

freshVar가 예약 범위 밖에서 시작하도록 한다.

```fsharp
// Infer.fs
let freshVar =
    let counter = ref 1000  // ← 0-999 예약, 1000부터 시작
    fun () ->
        let n = !counter
        counter := n + 1
        TVar n
```

### Step 3: 충돌 없음 확인

```fsharp
// Prelude bound vars: 0, 1, 2, ...
// Fresh vars: 1000, 1001, 1002, ...
// 겹치지 않음 ✓
```

## Example

```fsharp
// Prelude 정의
"id", Scheme([0], TArrow(TVar 0, TVar 0))  // forall 0. 0 -> 0

// 추론 시작
let result = infer initialTypeEnv (parse "let x = 5 in id x")

// 과정:
// 1. x: 'a (TVar 1000) — fresh
// 2. id 인스턴스화: TVar 0 → TVar 1001 (fresh로 교체)
//    결과: TVar 1001 -> TVar 1001
// 3. id x 단일화: TVar 1001 = TVar 1000 = int
// 4. 최종: int

// TVar 0 (Prelude)과 TVar 1000 (fresh)은 다른 변수 ✓
```

## 대안적 접근

### Named Variables 사용

```fsharp
type Type =
    | TVar of string  // "a", "b" 대신 문자열

// Prelude
"id", Scheme(["a"], TArrow(TVar "a", TVar "a"))

// freshVar
let freshVar =
    let counter = ref 0
    fun () -> TVar (sprintf "_t%d" (!counter |> fun n -> counter := n + 1; n))
```

장점: 충돌 가능성 없음 (이름 패턴이 다름)
단점: 문자열 비교/해싱 오버헤드

### 음수 사용

```fsharp
// Prelude: -1, -2, ...
"id", Scheme([-1], TArrow(TVar -1, TVar -1))

// Fresh: 0, 1, 2, ...
let freshVar = let c = ref 0 in fun () -> incr c; TVar !c
```

## 체크리스트

- [ ] Prelude scheme의 bound var 범위 확인
- [ ] freshVar 시작점이 범위 밖인가?
- [ ] 모든 Prelude 함수가 같은 범위를 사용하는가?
- [ ] 테스트: Prelude 함수 + 로컬 변수 혼용 시 정상 동작?

## 관련 문서

- `implement-hindley-milner-algorithm-w.md` - 전체 구조
- `implement-let-polymorphism.md` - instantiate가 fresh var 사용
