# Requirements: v6.0 Bidirectional Type System

**Defined:** 2026-02-03
**Core Value:** 각 챕터가 독립적으로 동작하는 완전한 예제를 제공

## v6.0 Requirements

Bidirectional type system으로 완전 전환. Algorithm W를 synthesis/checking 모드로 대체하고 ML 스타일 타입 어노테이션 지원.

### Parser Extensions ✓

- [x] **PARSE-01**: COLON 토큰 추가 (타입 어노테이션용)
- [x] **PARSE-02**: 타입 키워드 토큰 추가 (TYPE_INT, TYPE_BOOL, TYPE_STRING, TYPE_LIST)
- [x] **PARSE-03**: TYPE_VAR 토큰 추가 ('a, 'b 등)
- [x] **PARSE-04**: TypeExpr 비터미널 정의 (TEInt, TEBool, TEString, TEArrow, TETuple, TEList, TEVar)
- [x] **PARSE-05**: Annot AST 노드 추가 (표현식 어노테이션: `(e : T)`)
- [x] **PARSE-06**: LambdaAnnot AST 노드 추가 (파라미터 어노테이션: `fun (x: int) -> e`)
- [x] **PARSE-07**: 커리 스타일 다중 파라미터 지원 (`fun (x: int) (y: int) -> e`)

### Type Expression Elaboration ✓

- [x] **ELAB-01**: TypeExpr → Type 변환 함수 (elaborateTypeExpr)
- [x] **ELAB-02**: 타입 변수 스코핑 (같은 바인딩 내 'a는 같은 타입)
- [x] **ELAB-03**: 다형 어노테이션 지원 (`let id (x: 'a) : 'a = x`)

### Bidirectional Core ✓

- [x] **BIDIR-01**: synth 함수 구현 (synthesis mode: expr → type)
- [x] **BIDIR-02**: check 함수 구현 (checking mode: expr × type → ())
- [x] **BIDIR-03**: 리터럴/변수/어플리케이션 synthesis 규칙
- [x] **BIDIR-04**: 람다 checking 규칙 (화살표 타입 분해)
- [x] **BIDIR-05**: 어노테이션 없는 람다 hybrid 처리 (fresh var로 synthesis)
- [x] **BIDIR-06**: subsumption 규칙 (synthesis → checking 전환)
- [x] **BIDIR-07**: Let-polymorphism 유지 (generalize at let)

### Annotation Checking ✓

- [x] **ANNOT-01**: Annot 표현식 처리 (checking 후 synthesis)
- [x] **ANNOT-02**: LambdaAnnot 표현식 처리 (파라미터 타입 사용)
- [x] **ANNOT-03**: 어노테이션 타입과 추론 타입 검증
- [x] **ANNOT-04**: 잘못된 어노테이션 에러 메시지

### Error Integration ✓

- [x] **ERR-01**: Mode-aware context (InCheckMode, InSynthMode)
- [x] **ERR-02**: 예상 타입 포함 에러 메시지 ("expected int due to annotation")
- [x] **ERR-03**: 기존 Diagnostic 인프라 재사용

### Migration

- [ ] **MIG-01**: Bidir 모듈이 모든 기존 테스트 통과
- [ ] **MIG-02**: Infer → Bidir 전환 (CLI, REPL)
- [ ] **MIG-03**: 튜토리얼 챕터 작성

## Future Requirements

### Higher-Rank Polymorphism (v7.0+)

- **RANK-01**: forall 양화자 문법
- **RANK-02**: Dunfield-Krishnaswami 알고리즘 완전 구현

## Out of Scope

| Feature | Reason |
|---------|--------|
| Higher-rank polymorphism | v6.0은 rank-1 유지 |
| 타입 클래스/트레이트 | 별도 마일스톤 |
| Row polymorphism | 레코드 시스템 필요 |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| PARSE-01 | Phase 1 | Complete |
| PARSE-02 | Phase 1 | Complete |
| PARSE-03 | Phase 1 | Complete |
| PARSE-04 | Phase 1 | Complete |
| PARSE-05 | Phase 1 | Complete |
| PARSE-06 | Phase 1 | Complete |
| PARSE-07 | Phase 1 | Complete |
| ELAB-01 | Phase 2 | Complete |
| ELAB-02 | Phase 2 | Complete |
| ELAB-03 | Phase 2 | Complete |
| BIDIR-01 | Phase 3 | Complete |
| BIDIR-02 | Phase 3 | Complete |
| BIDIR-03 | Phase 3 | Complete |
| BIDIR-04 | Phase 3 | Complete |
| BIDIR-05 | Phase 3 | Complete |
| BIDIR-06 | Phase 3 | Complete |
| BIDIR-07 | Phase 3 | Complete |
| ANNOT-01 | Phase 4 | Complete |
| ANNOT-02 | Phase 4 | Complete |
| ANNOT-03 | Phase 4 | Complete |
| ANNOT-04 | Phase 4 | Complete |
| ERR-01 | Phase 5 | Complete |
| ERR-02 | Phase 5 | Complete |
| ERR-03 | Phase 5 | Complete |
| MIG-01 | Phase 6 | Pending |
| MIG-02 | Phase 6 | Pending |
| MIG-03 | Phase 6 | Pending |

**Coverage:**
- v6.0 requirements: 27 total
- Complete: 24 (89%)
- Pending: 3
