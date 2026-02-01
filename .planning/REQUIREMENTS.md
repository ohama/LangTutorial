# Requirements: LangTutorial v4.0

**Defined:** 2026-02-01
**Core Value:** 각 챕터가 독립적으로 동작하는 완전한 예제를 제공하여, 독자가 언어 구현의 각 단계를 직접 따라하고 실행해볼 수 있어야 한다.

## v4.0 Requirements

Requirements for v4.0 타입 시스템. Each maps to roadmap phases.

### Type Definition (타입 정의)

- [ ] **TYPE-01**: Type AST 정의 (TInt, TBool, TString, TVar, TArrow, TTuple, TList)
- [ ] **TYPE-02**: Scheme 타입 정의 (forall 변수 리스트 + 타입)
- [ ] **TYPE-03**: TypeEnv 정의 (변수 이름 → Scheme 매핑)
- [ ] **TYPE-04**: formatType 함수 (타입을 문자열로 변환)

### Substitution (대체 연산)

- [ ] **SUBST-01**: apply 함수 (타입에 대체 적용)
- [ ] **SUBST-02**: compose 함수 (대체 합성)
- [ ] **SUBST-03**: freeVars 함수 (타입/스킴/환경의 자유 타입 변수)

### Unification (단일화)

- [ ] **UNIFY-01**: occurs check (무한 타입 방지)
- [ ] **UNIFY-02**: unify 함수 (두 타입을 같게 만드는 대체 찾기)
- [ ] **UNIFY-03**: TypeError 예외 (타입 오류 메시지)

### Inference (타입 추론)

- [ ] **INFER-01**: freshVar 함수 (새 타입 변수 생성)
- [ ] **INFER-02**: instantiate 함수 (스킴을 타입으로 인스턴스화)
- [ ] **INFER-03**: generalize 함수 (타입을 스킴으로 일반화)
- [ ] **INFER-04**: infer 함수 — 리터럴 (Number, Bool, String)
- [ ] **INFER-05**: infer 함수 — 산술/비교/논리 연산자
- [ ] **INFER-06**: infer 함수 — 변수 참조 (Var)
- [ ] **INFER-07**: infer 함수 — Let 바인딩 (let-polymorphism)
- [ ] **INFER-08**: infer 함수 — Lambda와 App
- [ ] **INFER-09**: infer 함수 — LetRec
- [ ] **INFER-10**: infer 함수 — If 표현식
- [ ] **INFER-11**: infer 함수 — Tuple
- [ ] **INFER-12**: infer 함수 — List, EmptyList, Cons
- [ ] **INFER-13**: infer 함수 — Match 표현식
- [ ] **INFER-14**: infer 함수 — LetPat
- [ ] **INFER-15**: inferPattern 함수 (패턴 타입 추론)

### Integration (통합)

- [ ] **INTEG-01**: initialEnv 정의 (Prelude 함수 타입)
- [ ] **INTEG-02**: typecheck 함수 (표현식 타입 검사)
- [ ] **INTEG-03**: --emit-type CLI 옵션
- [ ] **INTEG-04**: 타입 오류 시 프로그램 종료 (exit 1)

### Testing (테스트)

- [ ] **TEST-01**: Type 모듈 단위 테스트
- [ ] **TEST-02**: Subst 모듈 단위 테스트
- [ ] **TEST-03**: Unify 모듈 단위 테스트
- [ ] **TEST-04**: Infer 모듈 단위 테스트
- [ ] **TEST-05**: TypeCheck 통합 테스트
- [ ] **TEST-06**: fslit CLI 테스트 (--emit-type)
- [ ] **TEST-07**: 타입 오류 케이스 테스트

## Out of Scope

| Feature | Reason |
|---------|--------|
| 타입 주석 구문 | 타입 추론만 지원, 명시적 주석 불필요 |
| 타입 클래스 | 복잡도 높음, v5.0+ 범위 |
| 레코드 타입 | ADT와 함께 v5.0+ 범위 |
| 재귀 타입 | 복잡도 높음, 기본 구현에 불필요 |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| TYPE-01 | Phase 1 | Complete |
| TYPE-02 | Phase 1 | Complete |
| TYPE-03 | Phase 1 | Complete |
| TYPE-04 | Phase 1 | Complete |
| SUBST-01 | Phase 2 | Complete |
| SUBST-02 | Phase 2 | Complete |
| SUBST-03 | Phase 2 | Complete |
| UNIFY-01 | Phase 3 | Complete |
| UNIFY-02 | Phase 3 | Complete |
| UNIFY-03 | Phase 3 | Complete |
| INFER-01 | Phase 4 | Complete |
| INFER-02 | Phase 4 | Complete |
| INFER-03 | Phase 4 | Complete |
| INFER-04 | Phase 4 | Complete |
| INFER-05 | Phase 4 | Complete |
| INFER-06 | Phase 4 | Complete |
| INFER-07 | Phase 4 | Complete |
| INFER-08 | Phase 4 | Complete |
| INFER-09 | Phase 4 | Complete |
| INFER-10 | Phase 4 | Complete |
| INFER-11 | Phase 4 | Complete |
| INFER-12 | Phase 4 | Complete |
| INFER-13 | Phase 4 | Complete |
| INFER-14 | Phase 4 | Complete |
| INFER-15 | Phase 4 | Complete |
| INTEG-01 | Phase 5 | Complete |
| INTEG-02 | Phase 5 | Complete |
| INTEG-03 | Phase 5 | Complete |
| INTEG-04 | Phase 5 | Complete |
| TEST-01 | Phase 6 | Pending |
| TEST-02 | Phase 6 | Pending |
| TEST-03 | Phase 6 | Pending |
| TEST-04 | Phase 6 | Pending |
| TEST-05 | Phase 6 | Pending |
| TEST-06 | Phase 6 | Pending |
| TEST-07 | Phase 6 | Pending |

**Coverage:**
- v4.0 requirements: 33 total
- Mapped to phases: 33
- Unmapped: 0 ✓

---
*Requirements defined: 2026-02-01*
