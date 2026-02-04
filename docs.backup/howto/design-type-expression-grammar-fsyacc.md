---
created: 2026-02-03
description: FsYacc에서 %left/%right 없이 문법 계층 구조로 타입 표현식 우선순위 구현
---

# FsYacc 타입 표현식 문법 설계

파서에 타입 표현식(int -> bool, int * string 등)을 추가할 때, 기존 값 표현식의 %left/%right 선언과 충돌 없이 우선순위를 처리하는 방법.

## The Insight

**타입 표현식은 값 표현식과 다른 우선순위 체계가 필요하다.**

값 표현식에서 `*`는 곱셈이고 `+/-`보다 높은 우선순위를 가진다. 타입 표현식에서 `*`는 튜플이고 `->`보다 높은 우선순위를 가진다. 같은 토큰(`STAR`, `ARROW`)이 다른 컨텍스트에서 다른 의미를 가지므로, %left/%right 전역 선언이 아닌 **문법 구조 자체로 우선순위를 인코딩**해야 한다.

## Why This Matters

잘못된 접근:
```
// ❌ 전역 precedence 추가 시도
%left ARROW
%left STAR  // 이미 곱셈용으로 사용 중!

TypeExpr:
    | TypeExpr ARROW TypeExpr  // shift/reduce conflict
    | TypeExpr STAR TypeExpr   // 곱셈과 충돌
```

결과:
- 기존 산술 연산 파싱이 깨짐
- Shift/reduce conflict 발생
- 타입 `int * bool`이 값 `3 * 4`와 혼동

## Recognition Pattern

이 패턴이 필요한 상황:
- 기존 파서에 새로운 "언어" 추가 (타입 표현식, 패턴 문법 등)
- 같은 토큰이 다른 컨텍스트에서 다른 의미를 가짐
- 새 문법의 우선순위가 기존과 독립적

## The Approach

**3단계 계층 구조로 우선순위 인코딩:**

```
TypeExpr (진입점)
    └── ArrowType (가장 낮은 우선순위: ->)
            └── TupleType (중간 우선순위: *)
                    └── AtomicType (가장 높은 우선순위: int, bool, ...)
```

하위 비터미널만 참조함으로써 자연스럽게 우선순위가 결정된다.

### Step 1: 진입점 정의

```
TypeExpr:
    | ArrowType                     { $1 }
```

TypeExpr은 단순히 ArrowType을 감싸는 wrapper. 확장성을 위해 별도 비터미널로 분리.

### Step 2: 화살표 타입 (가장 낮은 우선순위)

```
// 오른쪽 결합: int -> int -> int = int -> (int -> int)
ArrowType:
    | TupleType ARROW ArrowType     { TEArrow($1, $3) }
    | TupleType                     { $1 }
```

핵심: 왼쪽에는 TupleType(더 높은 우선순위), 오른쪽에는 ArrowType(같은 레벨)을 참조.
- 왼쪽 TupleType: `int * bool`이 화살표보다 먼저 파싱됨
- 오른쪽 ArrowType: 재귀로 오른쪽 결합성 구현

### Step 3: 튜플 타입 (중간 우선순위)

```
TupleType:
    | AtomicType STAR TupleTypeList { TETuple($1 :: $3) }
    | AtomicType                    { $1 }

TupleTypeList:
    | AtomicType STAR TupleTypeList { $1 :: $3 }
    | AtomicType                    { [$1] }
```

양쪽 모두 AtomicType만 참조하여 `*`가 `->`보다 강하게 바인딩.

### Step 4: 원자 타입 (가장 높은 우선순위)

```
AtomicType:
    | TYPE_INT                      { TEInt }
    | TYPE_BOOL                     { TEBool }
    | TYPE_STRING                   { TEString }
    | TYPE_VAR                      { TEVar($1) }  // 'a, 'b
    | AtomicType TYPE_LIST          { TEList($1) } // int list
    | LPAREN TypeExpr RPAREN        { $2 }         // 괄호로 우선순위 오버라이드
```

`list`는 postfix로 처리: `int list list` = `(int list) list`

## Example

입력: `int -> int * bool -> string`

파싱 과정:
```
1. ArrowType 시작
2. TupleType 매칭 시도 → int (AtomicType)
3. ARROW 발견 → ArrowType 재귀
4. TupleType 매칭 → int * bool
   - AtomicType: int
   - STAR
   - TupleTypeList: bool
   → TETuple([TEInt; TEBool])
5. ARROW 발견 → ArrowType 재귀
6. TupleType → string (AtomicType)
7. 최종: TEArrow(TEInt, TEArrow(TETuple([TEInt; TEBool]), TEString))
```

결과: `int -> ((int * bool) -> string)` (의도한 대로)

## 체크리스트

- [ ] 새 문법이 기존 토큰 재사용 시 컨텍스트 분리 확인
- [ ] 각 비터미널이 하위 레벨만 참조하는지 확인
- [ ] 결합성 방향 확인 (왼쪽 재귀 vs 오른쪽 재귀)
- [ ] 괄호로 우선순위 오버라이드 가능한지 확인
- [ ] 기존 테스트 모두 통과하는지 확인

## 관련 문서

- `fsyacc-precedence-without-declarations.md` - 값 표현식에서 문법 구조로 우선순위 처리
- `fsyacc-operator-precedence-methods.md` - %left/%right 선언 방식과 비교
