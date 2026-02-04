---
created: 2026-01-30
description: 인터프리터의 evalExpr가 int에서 Value 타입으로 진화할 때 기존 테스트 호환성 유지
---

# Evaluator 반환 타입 진화 시 테스트 적응

인터프리터가 단일 타입(int)에서 다중 타입(Value)으로 진화할 때, 기존 테스트를 깨뜨리지 않으면서 새 타입을 지원하는 패턴.

## The Insight

**타입 진화는 점진적이어야 한다.** 기존 테스트의 의도(int 결과 검증)는 유효하다. 새 타입 시스템이 이를 무효화하는 게 아니라, 어댑터 레이어가 호환성을 유지해야 한다.

멘탈 모델:
```
기존: evalExpr → int → 테스트 검증
진화: evalExpr → Value → 어댑터 → int → 테스트 검증 (기존 유지)
                      → Value → 새 테스트 (신규 추가)
```

## Why This Matters

증상:
```
error FS0001: Type mismatch.
Expecting 'Expr -> int' but given 'Expr -> Value'
```

`evalExpr`의 반환 타입이 `int`에서 `Value` (discriminated union)로 변경되면:
- 모든 기존 테스트가 컴파일 에러
- `int` 비교하던 `Expect.equal` 전부 실패
- Property 테스트의 `evalExpr (Number n) = n` 같은 패턴 전멸

잘못된 대응:
- 기존 테스트 전부 삭제 → 회귀 테스트 손실
- 모든 테스트에 match 추가 → 코드 중복, 가독성 저하

## Recognition Pattern

이 패턴이 필요한 시점:
- evalExpr가 `int`에서 `IntValue of int | BoolValue of bool` 같은 discriminated union으로 변경
- 새로운 타입(bool, string, function 등)을 언어에 추가
- 기존 int 전용 테스트가 갑자기 전부 빨간색

## The Approach

**핵심: 어댑터 함수로 호환성 레이어 구축**

### Step 1: Value 반환 함수 추가

기존 `evaluate` 함수는 그대로 두고, Value를 반환하는 새 함수 추가:

```fsharp
/// Parse and evaluate, returning Value (새 진입점)
let evaluateToValue (input: string) : Value =
    input |> parse |> evalExpr
```

### Step 2: 기존 함수를 어댑터로 전환

기존 `evaluate`를 Value에서 int를 추출하는 어댑터로 변경:

```fsharp
/// Parse and evaluate, extracting int (하위 호환성)
let evaluate (input: string) : int =
    match evaluateToValue input with
    | IntValue n -> n
    | BoolValue _ -> failwith "Expected int but got bool"
```

### Step 3: 새 타입용 어댑터 추가

새로 추가된 타입마다 어댑터 추가:

```fsharp
/// Parse and evaluate, extracting bool (새 타입용)
let evaluateToBool (input: string) : bool =
    match evaluateToValue input with
    | BoolValue b -> b
    | IntValue _ -> failwith "Expected bool but got int"
```

### Step 4: Property 테스트용 헬퍼 추가

`evalExpr`을 직접 호출하는 property 테스트용:

```fsharp
/// Extract int from Value (property 테스트용)
let asInt (v: Value) : int =
    match v with
    | IntValue n -> n
    | BoolValue _ -> failwith "Expected int"
```

## Example

**Before (컴파일 에러):**

```fsharp
// evalExpr가 Value를 반환하므로 타입 불일치
test "addition" {
    Expect.equal (evalExpr (Add(Number 2, Number 3))) 5 ""
}

testProperty "number evaluates to itself" <| fun (n: int) ->
    evalExpr (Number n) = n  // Value = int 비교 불가
```

**After (호환성 유지):**

```fsharp
// 기존 테스트 - evaluate 어댑터 사용 (변경 없음)
test "addition" {
    Expect.equal (evaluate "2 + 3") 5 ""
}

// Property 테스트 - asInt 헬퍼 사용
testProperty "number evaluates to itself" <| fun (n: int) ->
    asInt (evalExpr (Number n)) = n

// 새 타입 테스트 - evaluateToBool 사용
test "true evaluates to true" {
    Expect.isTrue (evaluateToBool "true") ""
}
```

## 체크리스트

- [ ] `evaluateToValue` 함수 추가 (Value 반환)
- [ ] 기존 `evaluate` 함수를 int 추출 어댑터로 수정
- [ ] 새 타입마다 어댑터 함수 추가 (`evaluateToBool` 등)
- [ ] Property 테스트용 `asInt` 헬퍼 추가
- [ ] 기존 테스트 컴파일 확인 (변경 없어야 함)
- [ ] 새 타입 테스트에서 적절한 어댑터 사용

## 관련 문서

- `setup-expecto-test-project.md` - Expecto 테스트 프로젝트 설정
- `write-fscheck-property-tests.md` - FsCheck 속성 테스트 작성
