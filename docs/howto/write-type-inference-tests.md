---
created: 2026-02-01
description: Hindley-Milner 타입 추론 테스트 작성 패턴 (Expecto + F#)
---

# 타입 추론 테스트 작성

Hindley-Milner 타입 추론을 테스트하는 패턴. 다형성, 에러 케이스, substitution 검증을 Expecto로 작성한다.

## The Insight

타입 추론 테스트는 일반 단위 테스트와 다르다. **구조적 동등성**을 확인해야 한다. `'a -> 'a`와 `'b -> 'b`는 변수 이름만 다를 뿐 같은 타입이다. Fresh variable 때문에 정확한 숫자 매칭은 불가능하다.

## Why This Matters

잘못된 테스트:
```fsharp
// ❌ 특정 변수 번호에 의존
Expect.equal result (TArrow(TVar 1000, TVar 1000))  // 깨지기 쉬움
```

올바른 테스트:
```fsharp
// ✓ 구조만 확인
match result with
| TArrow(TVar a, TVar b) -> Expect.equal a b  // 같은 변수인지만 확인
```

## Recognition Pattern

다음을 테스트할 때 이 패턴을 사용한다:

- 다형성 함수 타입 (`'a -> 'a`, `'a -> 'b -> 'a`)
- Fresh variable 생성 (instantiate, freshVar)
- Scheme generalization

## The Approach

### Step 1: 테스트 헬퍼 정의

```fsharp
module InferTests

open Expecto
open Type
open Infer

/// 빈 환경에서 추론 (순수 추론 테스트용)
let inferEmpty expr =
    let s, ty = infer Map.empty expr
    apply s ty

/// Prelude 포함 환경에서 추론
let inferWithPrelude expr =
    let s, ty = infer TypeCheck.initialTypeEnv expr
    apply s ty
```

### Step 2: 기본 타입 테스트 (Exact Match)

primitive 타입은 정확히 비교한다.

```fsharp
testList "Literal inference" [
    test "Number infers to int" {
        let result = inferEmpty (Number 42)
        Expect.equal result TInt "Number should be int"
    }

    test "Bool infers to bool" {
        let result = inferEmpty (Bool true)
        Expect.equal result TBool "Bool should be bool"
    }

    test "String infers to string" {
        let result = inferEmpty (String "hello")
        Expect.equal result TString "String should be string"
    }
]
```

### Step 3: 다형성 테스트 (Pattern Match)

Fresh variable은 구조로 확인한다.

```fsharp
testList "Polymorphism" [
    test "Identity function has 'a -> 'a structure" {
        let result = inferEmpty (parse "fun x -> x")
        match result with
        | TArrow(TVar a, TVar b) ->
            Expect.equal a b "domain and range should be same var"
            Expect.isGreaterThanOrEqual a 1000 "should be fresh var"
        | _ -> failtest "should be arrow type"
    }

    test "Constant function has 'a -> 'b -> 'a structure" {
        let result = inferEmpty (parse "fun x -> fun y -> x")
        match result with
        | TArrow(TVar a, TArrow(TVar b, TVar c)) ->
            Expect.equal a c "first and result should be same"
            Expect.notEqual a b "params should differ"
        | _ -> failtest "should be curried arrow"
    }
]
```

### Step 4: Let-Polymorphism 테스트

같은 함수가 다른 타입으로 사용되는지 확인한다.

```fsharp
test "Let-polymorphism: id at multiple types" {
    let result = inferEmpty (parse "let id = fun x -> x in (id 5, id true)")
    match result with
    | TTuple [TInt; TBool] -> ()  // ✓
    | _ -> failtest "should be (int, bool)"
}

test "Lambda param is monomorphic" {
    // 이건 실패해야 함
    Expect.throws (fun () ->
        inferEmpty (parse "fun f -> (f 1, f true)") |> ignore
    ) "lambda param cannot be polymorphic"
}
```

### Step 5: 에러 테스트

TypeError가 발생하는지 확인한다.

```fsharp
testList "Type errors" [
    test "Unbound variable raises TypeError" {
        Expect.throws (fun () ->
            inferEmpty (Var "x") |> ignore
        ) "should raise for unbound var"
    }

    test "Type mismatch in arithmetic" {
        Expect.throws (fun () ->
            inferEmpty (Add(Bool true, Number 1)) |> ignore
        ) "cannot add bool and int"
    }

    test "Infinite type detected" {
        Expect.throws (fun () ->
            // let rec f x = f in f — f: 'a = 'a -> 'b
            inferEmpty (parse "let rec f x = f in f") |> ignore
        ) "should detect infinite type"
    }

    test "If branch type mismatch" {
        Expect.throws (fun () ->
            inferEmpty (parse "if true then 1 else false") |> ignore
        ) "branches must have same type"
    }
]
```

### Step 6: Prelude 함수 타입 테스트

Prelude 함수가 올바른 다형성 타입을 갖는지 확인한다.

```fsharp
testList "Prelude types" [
    test "map has ('a -> 'b) -> 'a list -> 'b list structure" {
        let result = inferWithPrelude (Var "map")
        match result with
        | TArrow(TArrow(TVar a, TVar b), TArrow(TList(TVar c), TList(TVar d))) ->
            Expect.equal a c "input type should match list element"
            Expect.equal b d "output type should match result element"
            Expect.notEqual a b "input and output can differ"
        | _ -> failtest "map should have correct structure"
    }

    test "map partial application" {
        let result = inferWithPrelude (parse "map (fun x -> x + 1)")
        match result with
        | TArrow(TList TInt, TList TInt) -> ()
        | _ -> failtest "should be int list -> int list"
    }
]
```

## Example: 전체 테스트 구조

```fsharp
[<Tests>]
let inferTests = testList "Type Inference" [
    testList "Core functions" [ ... ]
    testList "Literal inference (INFER-04)" [ ... ]
    testList "Variable inference (INFER-06)" [ ... ]
    testList "Arithmetic operators (INFER-05)" [ ... ]
    testList "Lambda (INFER-08)" [ ... ]
    testList "Let-polymorphism (INFER-07)" [ ... ]
    testList "Type errors" [ ... ]
]
```

## 체크리스트

- [ ] Primitive 타입은 `Expect.equal`로 정확히 비교
- [ ] 다형성 타입은 `match`로 구조만 확인
- [ ] Fresh var는 `>= 1000` 확인 (Prelude 범위 밖)
- [ ] 같은 변수는 `Expect.equal a b`로 동일성 확인
- [ ] 에러 케이스는 `Expect.throws`로 예외 확인
- [ ] Let-polymorphism vs Lambda-monomorphism 둘 다 테스트

## 관련 문서

- `implement-hindley-milner-algorithm-w.md` - 테스트 대상 이해
- `implement-let-polymorphism.md` - polymorphism 테스트 이유
- `setup-expecto-test-project.md` - Expecto 설정
