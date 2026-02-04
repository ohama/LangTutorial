---
created: 2026-02-04
description: Algorithm W를 양방향 타입 체킹으로 확장하여 타입 어노테이션을 지원하는 방법
---

# Bidirectional Type Checking 구현

Algorithm W (bottom-up only)를 양방향 타입 체킹으로 확장하여 `(e : T)`, `fun (x: int) -> e` 같은 타입 어노테이션을 지원한다.

## The Insight

**타입 추론에는 두 방향이 있다:**

- **Synthesis (⇒)**: 표현식에서 타입을 추론 (bottom-up) — "이 표현식의 타입은 무엇인가?"
- **Checking (⇐)**: 예상 타입에 대해 표현식을 검증 (top-down) — "이 표현식이 이 타입을 가지는가?"

Algorithm W는 synthesis만 사용하지만, 타입 어노테이션이 있으면 **checking으로 전환**할 수 있다. 어노테이션이 "예상 타입"을 제공하기 때문이다.

핵심 규칙:
```
synth: Γ ⊢ e ⇒ T      (e의 타입을 추론)
check: Γ ⊢ e ⇐ T      (e가 T 타입인지 검증)
```

## Why This Matters

Algorithm W만으로는:
- 타입 어노테이션을 처리할 수 없다
- 에러 메시지가 "추론된 타입"만 표시한다
- "expected T but got U" 같은 명확한 메시지를 만들기 어렵다

양방향 체킹을 추가하면:
- `(e : T)` 어노테이션이 checking 모드를 유발
- 에러 메시지에 "expected T due to annotation"을 포함할 수 있다
- 람다 파라미터 타입을 명시할 수 있다: `fun (x: int) -> x + 1`

## Recognition Pattern

다음 상황에서 양방향 체킹이 필요하다:

1. **타입 어노테이션 지원**: `(expr : Type)` 문법
2. **어노테이션된 람다**: `fun (x: T) -> e`
3. **더 나은 에러 메시지**: 예상 타입이 어디서 왔는지 추적
4. **점진적 타이핑**: 일부만 어노테이션하고 나머지는 추론

## The Approach

### Step 1: synth/check 분리

두 함수를 정의한다:

```fsharp
// Synthesis: expr → type
let rec synth (env: TypeEnv) (expr: Expr): Subst * Type

// Checking: expr × expected → unit (substitution)
and check (env: TypeEnv) (expr: Expr) (expected: Type): Subst
```

대부분의 표현식은 **synthesis**한다:
- 리터럴: `42` ⇒ `int`
- 변수: 환경에서 조회
- 함수 적용: 함수 타입에서 결과 추출

일부 표현식은 **checking**이 더 자연스럽다:
- 람다 + 예상 화살표 타입: 파라미터 타입을 예상에서 가져온다

### Step 2: Subsumption 규칙 (BIDIR-06)

**핵심**: check가 모든 표현식을 직접 처리할 필요가 없다. 실패하면 synth로 fallback하고 결과를 unify한다.

```fsharp
and check ctx env expr expected =
    match expr with
    // 특별 처리할 표현식들
    | Lambda (param, body, _) ->
        match expected with
        | TArrow (paramTy, resultTy) ->
            // 파라미터 타입을 expected에서 가져와 body를 check
            let bodyEnv = Map.add param (Scheme ([], paramTy)) env
            check ctx bodyEnv body resultTy
        | _ ->
            // 화살표 아님 → subsumption으로 fallback
            subsume ctx env expr expected

    // 기본: subsumption
    | _ -> subsume ctx env expr expected

// Subsumption: synth 후 unify
and subsume ctx env expr expected =
    let s, actual = synth ctx env expr
    let s' = unify (apply s expected) actual
    compose s' s
```

이렇게 하면 모든 표현식에 대해 check를 구현할 필요가 없다.

### Step 3: Annot 표현식 처리 (BIDIR-01)

`(e : T)` 어노테이션을 만나면:

1. TypeExpr `T`를 내부 Type으로 변환 (elaboration)
2. checking 모드로 전환: `check env e T`
3. 결과 타입은 `T`

```fsharp
| Annot (e, tyExpr, span) ->
    let expectedTy = elaborateTypeExpr tyExpr
    let s = check ctx env e expectedTy
    (s, apply s expectedTy)
```

### Step 4: 어노테이션된 람다 (BIDIR-02)

`fun (x: T) -> e`를 만나면:

1. 파라미터 타입 `T`를 elaborate
2. 환경에 `x: T` 추가
3. body를 synth (checking이 아님!)

```fsharp
| LambdaAnnot (param, paramTyExpr, body, span) ->
    let paramTy = elaborateTypeExpr paramTyExpr
    let bodyEnv = Map.add param (Scheme ([], paramTy)) env
    let s, bodyTy = synth ctx bodyEnv body
    (s, TArrow (apply s paramTy, bodyTy))
```

### Step 5: Hybrid Approach (BIDIR-05)

어노테이션이 없는 람다는 어떻게?

**Hybrid**: fresh type variable로 synth한다:

```fsharp
| Lambda (param, body, _) ->
    let paramTy = freshVar()  // 'a
    let bodyEnv = Map.add param (Scheme ([], paramTy)) env
    let s, bodyTy = synth ctx bodyEnv body
    (s, TArrow (apply s paramTy, bodyTy))
```

checking 모드에서 람다를 만나면 예상 타입에서 파라미터 타입을 가져온다:

```fsharp
// check 내에서
| Lambda (param, body, _) ->
    match expected with
    | TArrow (paramTy, resultTy) ->
        // expected에서 paramTy 사용
        let bodyEnv = Map.add param (Scheme ([], paramTy)) env
        check ctx bodyEnv body resultTy
```

이 hybrid 접근법 덕분에:
- 어노테이션 없는 기존 코드가 그대로 동작 (backward compatible)
- 어노테이션이 있으면 그 정보를 활용

## Example

```fsharp
// Algorithm W와 동일하게 동작
let id = fun x -> x       // 'a -> 'a

// 어노테이션으로 타입 명시
let idInt = fun (x: int) -> x  // int -> int

// 표현식 어노테이션
let x = (42 : int)        // int

// 잘못된 어노테이션 → 타입 에러
let bad = (true : int)
// Error: Type mismatch - expected int but got bool
//        The type annotation expects int
```

## 체크리스트

- [ ] synth/check 함수 분리
- [ ] 리터럴, 변수, 적용은 synth
- [ ] 람다의 check: expected에서 파라미터 타입 추출
- [ ] Subsumption: check 실패 시 synth + unify fallback
- [ ] Annot: elaborate 후 check 호출
- [ ] LambdaAnnot: elaborate 후 synth 호출
- [ ] 어노테이션 없는 람다: freshVar로 hybrid synth
- [ ] Let-polymorphism 유지 (generalize at let)

## 관련 문서

- `implement-hindley-milner-algorithm-w.md` - 기반 알고리즘
- `avoid-type-variable-collision.md` - 타입 변수 인덱스 분리
- `design-type-expression-grammar-fsyacc.md` - TypeExpr 파싱
