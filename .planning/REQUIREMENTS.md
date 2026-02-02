# Requirements: LangTutorial

**Defined:** 2026-02-02
**Core Value:** 각 챕터가 독립적으로 동작하는 완전한 예제를 제공

## v5.0 Requirements

타입 에러 진단 시스템. Algorithm W의 에러 위치/원인을 정확히 표현.

### Span/Range

- [x] **SPAN-01**: Span 타입 정의 (파일, 시작/끝 라인·컬럼)
- [x] **SPAN-02**: Expr 노드에 span 필드 추가
- [x] **SPAN-03**: Lexer에서 위치 정보 생성
- [x] **SPAN-04**: Parser에서 AST에 span 전달

### Diagnostic

- [ ] **DIAG-01**: Diagnostic 타입 정의 (code, message, primarySpan, secondarySpans, notes, hint)
- [ ] **DIAG-02**: TypeError 타입 정의 (kind, span, expected, actual, term, contextStack, trace)
- [ ] **DIAG-03**: TypeError kind 정의 (UnifyMismatch, OccursCheck, UnboundVar, NotAFunction 등)
- [ ] **DIAG-04**: TypeError → Diagnostic 변환 함수

### Context Stack

- [ ] **CTX-01**: InferContext 타입 정의 (InIfCond, InIfThen, InIfElse, InAppFun, InAppArg, InLetRhs 등)
- [ ] **CTX-02**: 타입 추론 시 context stack 관리
- [ ] **CTX-03**: 에러 발생 시 context stack 포함

### Unification Trace

- [ ] **TRACE-01**: UnifyPath 타입 정의 (AtFunctionReturn, AtTupleIndex, AtListElement 등)
- [ ] **TRACE-02**: unify 실패 시 충돌 경로 기록
- [ ] **TRACE-03**: Trace 정보를 TypeError에 포함

### Blame Assignment

- [ ] **BLAME-01**: Primary span 선택 규칙 구현 (가장 직접적인 원인)
- [ ] **BLAME-02**: Secondary span 선택 규칙 구현 (관련 expr)
- [ ] **BLAME-03**: 가장 안쪽 expr 우선 규칙

### Error Output

- [ ] **OUT-01**: 에러 코드 체계 정의 (E0301 등)
- [ ] **OUT-02**: 에러 메시지 포맷 (위치, 기대/실제 타입, context 요약, 힌트)
- [ ] **OUT-03**: 타입 pretty-printer 개선 (타입 변수 a,b,c 정규화)
- [ ] **OUT-04**: CLI에서 새 에러 포맷 출력

### Testing

- [ ] **TEST-01**: if 조건이 int인 경우 테스트
- [ ] **TEST-02**: 함수가 아닌 값 호출 테스트
- [ ] **TEST-03**: 인자 타입 불일치 테스트
- [ ] **TEST-04**: let rhs 에러 테스트
- [ ] **TEST-05**: occurs check 테스트
- [ ] **TEST-06**: Diagnostic 골든 테스트 프레임워크

## Future Requirements

### Bidirectional Typing (v6.0+)

- **BIDIR-01**: expected type을 TypeError에 포함
- **BIDIR-02**: check mode 추가 (W의 infer + check)
- **BIDIR-03**: 같은 Diagnostic 렌더링 재사용

## Out of Scope

| Feature | Reason |
|---------|--------|
| Bidirectional Typing 구현 | v5.0은 인프라 구축, 실제 전환은 v6.0+ |
| IDE 통합 (LSP) | 에러 포맷 안정화 후 고려 |
| 에러 복구 (error recovery) | 첫 에러에서 중단하는 현재 방식 유지 |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| SPAN-01 | Phase 1 | Done |
| SPAN-02 | Phase 1 | Done |
| SPAN-03 | Phase 1 | Done |
| SPAN-04 | Phase 1 | Done |
| DIAG-01 | Phase 2 | Pending |
| DIAG-02 | Phase 2 | Pending |
| DIAG-03 | Phase 2 | Pending |
| DIAG-04 | Phase 2 | Pending |
| CTX-01 | Phase 2 | Pending |
| CTX-02 | Phase 2 | Pending |
| CTX-03 | Phase 2 | Pending |
| TRACE-01 | Phase 2 | Pending |
| TRACE-02 | Phase 2 | Pending |
| TRACE-03 | Phase 2 | Pending |
| BLAME-01 | Phase 3 | Pending |
| BLAME-02 | Phase 3 | Pending |
| BLAME-03 | Phase 3 | Pending |
| OUT-01 | Phase 4 | Pending |
| OUT-02 | Phase 4 | Pending |
| OUT-03 | Phase 4 | Pending |
| OUT-04 | Phase 4 | Pending |
| TEST-01 | Phase 4 | Pending |
| TEST-02 | Phase 4 | Pending |
| TEST-03 | Phase 4 | Pending |
| TEST-04 | Phase 4 | Pending |
| TEST-05 | Phase 4 | Pending |
| TEST-06 | Phase 4 | Pending |

**Coverage:**
- v5.0 requirements: 27 total
- Mapped to phases: 27 ✓
- Unmapped: 0

---
*Requirements defined: 2026-02-02*
*Last updated: 2026-02-02 after roadmap creation*
