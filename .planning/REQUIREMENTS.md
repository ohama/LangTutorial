# Requirements: LangTutorial

**Defined:** 2026-02-01
**Core Value:** 각 챕터가 독립적으로 동작하는 완전한 예제를 제공하여, 독자가 언어 구현의 각 단계를 직접 따라하고 실행해볼 수 있어야 한다.

## v3.0 Requirements

Requirements for v3.0 데이터 구조. Each maps to roadmap phases.

### Tuples (튜플)

- [ ] **TUP-01**: 튜플 리터럴 생성 `(1, true)`, `(1, 2, 3)`
- [ ] **TUP-02**: 튜플 패턴 분해 `let (x, y) = pair`
- [ ] **TUP-03**: 중첩 튜플 지원 `((1, 2), 3)`
- [ ] **TUP-04**: TupleValue 타입 추가

### Lists (리스트)

- [ ] **LIST-01**: 빈 리스트 리터럴 `[]`
- [ ] **LIST-02**: 리스트 리터럴 `[1, 2, 3]` (syntactic sugar)
- [ ] **LIST-03**: Cons 연산자 `0 :: xs`
- [ ] **LIST-04**: ListValue 타입 추가

### Pattern Matching (패턴 매칭)

- [ ] **PAT-01**: match 표현식 `match e with | p1 -> e1 | p2 -> e2`
- [ ] **PAT-02**: 변수 패턴 `x`
- [ ] **PAT-03**: 와일드카드 패턴 `_`
- [ ] **PAT-04**: 상수 패턴 (정수, 불리언)
- [ ] **PAT-05**: Cons 패턴 `h :: t`
- [ ] **PAT-06**: 튜플 패턴 `(x, y)`
- [ ] **PAT-07**: 빈 리스트 패턴 `[]`
- [ ] **PAT-08**: 완전성 검사 (exhaustiveness check)

### Prelude (표준 라이브러리)

- [ ] **PRE-01**: `map` 함수 구현
- [ ] **PRE-02**: `filter` 함수 구현
- [ ] **PRE-03**: `fold` 함수 구현
- [ ] **PRE-04**: `length` 함수 구현
- [ ] **PRE-05**: `reverse` 함수 구현
- [ ] **PRE-06**: `append` 함수 구현
- [ ] **PRE-07**: `id`, `const`, `compose` 유틸리티
- [ ] **PRE-08**: `hd`, `tl` 리스트 연산 함수
- [ ] **PRE-09**: 시작 시 Prelude.fun 자동 로드

## Future Requirements (v4.0+)

Deferred to future release. Tracked but not in current roadmap.

### Data Structures (추가)

- **ADT-01**: 대수적 데이터 타입 (Sum types)
- **ADT-02**: 사용자 정의 타입 생성자

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| 타입 시스템 | v5.0 범위, 복잡도 높음 |
| TCO (꼬리 호출 최적화) | v4.0 범위, 별도 구현 필요 |
| 예외 처리 | v4.0 범위 |
| 모듈 시스템 | v5.0 범위 |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| TUP-01 | Phase 1 | Pending |
| TUP-02 | Phase 1 | Pending |
| TUP-03 | Phase 1 | Pending |
| TUP-04 | Phase 1 | Pending |
| LIST-01 | Phase 2 | Pending |
| LIST-02 | Phase 2 | Pending |
| LIST-03 | Phase 2 | Pending |
| LIST-04 | Phase 2 | Pending |
| PAT-01 | Phase 3 | Pending |
| PAT-02 | Phase 3 | Pending |
| PAT-03 | Phase 3 | Pending |
| PAT-04 | Phase 3 | Pending |
| PAT-05 | Phase 3 | Pending |
| PAT-06 | Phase 3 | Pending |
| PAT-07 | Phase 3 | Pending |
| PAT-08 | Phase 3 | Pending |
| PRE-01 | Phase 4 | Pending |
| PRE-02 | Phase 4 | Pending |
| PRE-03 | Phase 4 | Pending |
| PRE-04 | Phase 4 | Pending |
| PRE-05 | Phase 4 | Pending |
| PRE-06 | Phase 4 | Pending |
| PRE-07 | Phase 4 | Pending |
| PRE-08 | Phase 4 | Pending |
| PRE-09 | Phase 4 | Pending |

**Coverage:**
- v3.0 requirements: 25 total
- Mapped to phases: 25
- Unmapped: 0 ✓

---
*Requirements defined: 2026-02-01*
*Last updated: 2026-02-01 after initial definition*
