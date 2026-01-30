---
created: 2026-01-30
description: FsCheck로 속성 기반 테스트 작성 - 수학적 불변식 검증
---

# FsCheck 속성 기반 테스트

무작위 입력으로 코드의 수학적 성질을 검증하는 방법.

## The Insight

단위 테스트는 "이 입력에 이 출력"을 검증한다. 속성 테스트는 "모든 입력에 이 성질이 유지됨"을 검증한다. 테스트 케이스를 나열하는 대신 **불변식(invariant)**을 선언하면, FsCheck가 반례를 찾아준다.

```
단위 테스트: add(2, 3) == 5
속성 테스트: ∀a,b: add(a, b) == add(b, a)  // 교환법칙
```

## Why This Matters

- **엣지 케이스 발견**: 사람이 생각 못한 입력 조합을 자동 생성
- **명세 검증**: "이 함수는 무엇을 보장하는가?"를 코드로 표현
- **회귀 방지**: 100개의 무작위 케이스가 1개의 하드코딩 케이스보다 강력

## Recognition Pattern

- 수학적 성질이 있는 코드 (덧셈의 교환법칙, 역연산 등)
- "모든 X에 대해 Y가 성립"을 검증하고 싶을 때
- 엣지 케이스를 놓칠 걱정이 될 때

## The Approach

### Step 1: Expecto + FsCheck 설정

```bash
cd MyProject.Tests
dotnet add package FsCheck
dotnet add package Expecto.FsCheck
```

### Step 2: 검증할 속성 식별

코드가 보장하는 수학적 성질을 찾는다:

| 속성 | 설명 | 예시 |
|------|------|------|
| 항등원 | 특정 값과 연산해도 변하지 않음 | `x + 0 = x`, `x * 1 = x` |
| 교환법칙 | 순서 바꿔도 결과 동일 | `a + b = b + a` |
| 결합법칙 | 그룹핑 바꿔도 결과 동일 | `(a + b) + c = a + (b + c)` |
| 역연산 | 연산 후 역연산하면 원래 값 | `parse(print(x)) = x` |
| 멱등성 | 여러 번 적용해도 결과 동일 | `sort(sort(x)) = sort(x)` |

### Step 3: testProperty로 속성 작성

```fsharp
open Expecto
open FsCheck

[<Tests>]
let propertyTests =
    testList "Properties" [
        testProperty "addition is commutative" <| fun (a: int) (b: int) ->
            a + b = b + a

        testProperty "zero is additive identity" <| fun (n: int) ->
            n + 0 = n
    ]
```

### Step 4: 조건부 속성 (==>)

특정 조건에서만 성립하는 속성:

```fsharp
testProperty "division and multiplication are inverse" <| fun (a: int) (b: int) ->
    b <> 0 ==> lazy (a / b * b + a % b = a)
```

`==>` 연산자: 왼쪽이 false면 테스트 스킵, true면 오른쪽 검증.

## Example

**인터프리터 평가기 속성 테스트:**

```fsharp
module EvalProperties

open Expecto
open FsCheck
open Ast
open Eval

[<Tests>]
let evalProperties =
    testList "Evaluator Properties" [
        // 숫자 리터럴은 그대로 평가됨
        testProperty "number evaluates to itself" <| fun (n: int) ->
            eval (Number n) = n

        // 덧셈은 교환법칙
        testProperty "addition is commutative" <| fun (a: int) (b: int) ->
            let left = eval (Add(Number a, Number b))
            let right = eval (Add(Number b, Number a))
            left = right

        // 곱셈은 결합법칙
        testProperty "multiplication is associative" <| fun (a: int) (b: int) (c: int) ->
            let left = eval (Multiply(Multiply(Number a, Number b), Number c))
            let right = eval (Multiply(Number a, Multiply(Number b, Number c)))
            left = right

        // 0을 더해도 변하지 않음 (항등원)
        testProperty "zero is additive identity" <| fun (n: int) ->
            eval (Add(Number n, Number 0)) = n

        // 1을 곱해도 변하지 않음 (항등원)
        testProperty "one is multiplicative identity" <| fun (n: int) ->
            eval (Multiply(Number n, Number 1)) = n

        // 부정의 부정은 원래 값
        testProperty "double negation is identity" <| fun (n: int) ->
            eval (Negate(Negate(Number n))) = n

        // 뺄셈은 덧셈 + 부정
        testProperty "subtraction is addition of negation" <| fun (a: int) (b: int) ->
            let sub = eval (Subtract(Number a, Number b))
            let addNeg = eval (Add(Number a, Negate(Number b)))
            sub = addNeg
    ]
```

## 커스텀 Generator

복잡한 타입을 자동 생성:

```fsharp
open FsCheck

// AST를 무작위 생성하는 Generator
let exprGen =
    let rec expr' depth =
        if depth <= 0 then
            Gen.map Number Arb.generate<int>
        else
            let smaller = expr' (depth - 1)
            Gen.oneof [
                Gen.map Number Arb.generate<int>
                Gen.map2 (fun a b -> Add(a, b)) smaller smaller
                Gen.map2 (fun a b -> Multiply(a, b)) smaller smaller
                Gen.map Negate smaller
            ]
    Gen.sized (fun s -> expr' (min s 5))

type ExprArbitrary =
    static member Expr() = Arb.fromGen exprGen

// 등록 (테스트 시작 전)
Arb.register<ExprArbitrary>() |> ignore

[<Tests>]
let astTests =
    testList "AST Properties" [
        testProperty "eval never throws on valid AST" <| fun (expr: Expr) ->
            try
                eval expr |> ignore
                true
            with _ -> false
    ]
```

## 실행

```bash
# 전체 테스트
dotnet run --project MyProject.Tests

# 속성 테스트만
dotnet run --project MyProject.Tests -- --filter "Properties"

# 더 많은 케이스 (기본 100개)
# FsCheck.Config에서 MaxTest 설정
```

## 실패 시 출력

FsCheck는 실패 시 **최소 반례(shrunk)**를 보여준다:

```
Falsifiable, after 42 tests (3 shrinks):
Original: (1073741824, 1073741824)
Shrunk: (1, 2147483647)
```

overflow 같은 엣지 케이스를 자동 발견.

## 체크리스트

- [ ] FsCheck, Expecto.FsCheck 패키지 추가됨
- [ ] 검증할 수학적 속성을 식별함
- [ ] testProperty로 속성 작성함
- [ ] 조건부 속성은 ==> 사용
- [ ] 복잡한 타입은 커스텀 Generator 작성

## 관련 문서

- [setup-expecto-test-project](setup-expecto-test-project.md) - Expecto 프로젝트 설정
- [testing-strategies](testing-strategies.md) - 전체 테스트 전략
- [FsCheck Documentation](https://fscheck.github.io/FsCheck/) - 공식 문서
