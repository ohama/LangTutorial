---
created: 2026-01-30
description: fsyacc에서 함수 호출과 뺄셈 연산자 문법 충돌을 Atom 비단말로 해결
---

# fsyacc 함수 호출 vs 뺄셈 문법 충돌 해결

함수 호출(juxtaposition)과 단항 마이너스가 공존할 때 발생하는 문법 모호성을 Atom 비단말로 해결한다.

## The Insight

`f - 1`은 두 가지로 해석될 수 있다:
- **뺄셈**: f에서 1을 뺀다 (`Subtract(Var "f", Number 1)`)
- **함수 호출**: f에 -1을 전달한다 (`App(Var "f", Negate(Number 1))`)

파서는 공백을 무시하므로 `f - 1`과 `f -1`은 동일한 토큰 스트림이다. 문법 규칙 자체로 구분해야 한다.

## Why This Matters

이 모호성을 해결하지 않으면:
- **Shift/reduce 충돌**: fsyacc가 경고를 출력하고 기본 동작(shift)을 선택
- **예상과 다른 파싱**: `add 3 - 1`이 `add 3`에서 1을 빼는 대신 `add 3 (-1)`로 파싱될 수 있음
- **사용자 혼란**: 같은 표현식이 문맥에 따라 다르게 동작

## Recognition Pattern

다음 조건이 동시에 충족될 때 이 문제가 발생한다:
1. **함수 호출이 juxtaposition**: `f x` 형태 (ML, Haskell, F# 스타일)
2. **단항 마이너스 존재**: `-x` 형태
3. **이항 마이너스 존재**: `x - y` 형태

C-style 함수 호출 `f(x)`를 사용하면 이 문제가 없다.

## The Approach

**핵심 전략**: 함수 인자로 허용되는 표현식에서 단항 마이너스를 제외한다.

```
Factor → MINUS Factor | AppExpr     // Factor는 단항 마이너스 포함
AppExpr → AppExpr Atom | Atom       // 함수 호출은 Atom만 인자로
Atom → NUMBER | IDENT | (Expr)      // Atom은 단항 마이너스 제외
```

이렇게 하면:
- `f - 1` → Factor에서 `-`를 만나면 이항 연산자로 처리 (뺄셈)
- `f (-1)` → 괄호로 감싸면 Atom이 되어 함수 인자로 처리

### Step 1: 기존 문법 구조 파악

일반적인 산술 문법:
```
Expr → Expr + Term | Expr - Term | Term
Term → Term * Factor | Term / Factor | Factor
Factor → MINUS Factor | NUMBER | IDENT | (Expr)
```

여기에 함수 호출 `AppExpr → AppExpr Factor`를 추가하면 충돌 발생.

### Step 2: Atom 비단말 도입

Factor에서 단항 마이너스를 분리:
```fsharp
Factor:
    | MINUS Factor       { Negate($2) }
    | AppExpr            { $1 }

AppExpr:
    | AppExpr Atom       { App($1, $2) }
    | Atom               { $1 }

Atom:
    | NUMBER             { Number($1) }
    | IDENT              { Var($1) }
    | TRUE               { Bool(true) }
    | FALSE              { Bool(false) }
    | LPAREN Expr RPAREN { $2 }
```

### Step 3: 우선순위 확인

결과적인 우선순위 (낮음 → 높음):
1. `+ -` (이항)
2. `* /`
3. `-` (단항)
4. 함수 호출
5. Atom (리터럴, 변수, 괄호)

## Example

```fsharp
// 입력: f - 1
// 토큰: IDENT(f) MINUS NUMBER(1)
// 파싱: Subtract(Var "f", Number 1)

// 입력: f (-1)
// 토큰: IDENT(f) LPAREN MINUS NUMBER(1) RPAREN
// 파싱: App(Var "f", Negate(Number 1))

// 입력: add 3 - 1
// 파싱: Subtract(App(Var "add", Number 3), Number 1)
// 즉, (add 3) - 1

// 입력: add 3 (-1)
// 파싱: App(App(Var "add", Number 3), Negate(Number 1))
// 즉, (add 3) (-1) → add에 3과 -1을 커링 적용
```

## 체크리스트

- [ ] Factor가 MINUS Factor와 AppExpr로 분리되어 있는가
- [ ] AppExpr가 Atom만 인자로 받는가 (Factor가 아님)
- [ ] Atom에 단항 연산자가 없는가
- [ ] `f - 1` 테스트: 뺄셈으로 파싱되는가
- [ ] `f (-1)` 테스트: 함수 호출로 파싱되는가
- [ ] shift/reduce 충돌 경고가 없는가

## 관련 문서

- `implement-unary-minus.md` - 단항 마이너스 구현 전략
- `fsyacc-operator-precedence-methods.md` - fsyacc 우선순위 설정 방법
