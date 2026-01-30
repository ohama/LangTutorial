# Requirements: LangTutorial

**Defined:** 2025-01-30
**Core Value:** 각 챕터가 독립적으로 동작하는 완전한 예제를 제공하여, 독자가 언어 구현의 각 단계를 직접 따라하고 실행해볼 수 있어야 한다.

## v1 Requirements

### Foundation

- [ ] **FOUND-01**: .NET 10 + FsLexYacc 프로젝트 구성 설명
- [ ] **FOUND-02**: fslex로 토큰 생성 (Lexer 기초)
- [ ] **FOUND-03**: fsyacc로 AST 생성 (Parser 기초)
- [ ] **FOUND-04**: Discriminated Union으로 AST 타입 정의

### Expressions

- [ ] **EXPR-01**: 사칙연산 (+, -, *, /) 구현
- [ ] **EXPR-02**: 연산자 우선순위 처리 (*, /가 +, -보다 먼저)
- [ ] **EXPR-03**: 괄호로 우선순위 변경
- [ ] **EXPR-04**: 단항 마이너스 (음수) 지원

### Variables

- [ ] **VAR-01**: let 바인딩 (let x = 5)
- [ ] **VAR-02**: 식에서 변수 참조
- [ ] **VAR-03**: let-in 식으로 지역 스코프

### Control Flow

- [ ] **CTRL-01**: if-then-else 조건 분기
- [ ] **CTRL-02**: Boolean 타입 (true, false 리터럴)
- [ ] **CTRL-03**: 비교 연산자 (=, <, >, <=, >=, <>)
- [ ] **CTRL-04**: 논리 연산자 (&&, ||)

### Functions

- [ ] **FUNC-01**: 함수 정의 (let f x = x + 1)
- [ ] **FUNC-02**: 함수 호출 (f 5)
- [ ] **FUNC-03**: 재귀 함수 지원
- [ ] **FUNC-04**: 클로저 (외부 변수 캡처)

### Quality

- [ ] **QUAL-01**: 사용자 친화적 에러 메시지
- [ ] **QUAL-02**: 대화형 REPL 셸
- [ ] **QUAL-03**: Expecto, FsCheck, fslit으로 테스트

## v2 Requirements

### Advanced Features

- **ADV-01**: 정적 타입 시스템
- **ADV-02**: 리스트/배열 데이터 타입
- **ADV-03**: 패턴 매칭 (match 식)
- **ADV-04**: 모듈 시스템

## Out of Scope

| Feature | Reason |
|---------|--------|
| 실수 (float/double) | 정수만 지원, 파싱/연산 단순화 |
| 컴파일러 (바이트코드/네이티브) | 인터프리터에 집중, 복잡도 증가 |
| 표준 라이브러리 | 언어 코어에 집중 |
| 멀티라인 문자열 | v1 범위 초과 |
| 파일 I/O | 언어 자체 구현에 집중 |
| 동시성/병렬성 | 고급 주제, 추후 확장 |

## Traceability

| Requirement | Phase | Phase Name | Status |
|-------------|-------|------------|--------|
| FOUND-01 | Phase 1 | Foundation & Pipeline | Pending |
| FOUND-02 | Phase 1 | Foundation & Pipeline | Pending |
| FOUND-03 | Phase 1 | Foundation & Pipeline | Pending |
| FOUND-04 | Phase 1 | Foundation & Pipeline | Pending |
| EXPR-01 | Phase 2 | Arithmetic Expressions | Pending |
| EXPR-02 | Phase 2 | Arithmetic Expressions | Pending |
| EXPR-03 | Phase 2 | Arithmetic Expressions | Pending |
| EXPR-04 | Phase 2 | Arithmetic Expressions | Pending |
| VAR-01 | Phase 3 | Variables & Binding | Pending |
| VAR-02 | Phase 3 | Variables & Binding | Pending |
| VAR-03 | Phase 3 | Variables & Binding | Pending |
| CTRL-01 | Phase 4 | Control Flow | Pending |
| CTRL-02 | Phase 4 | Control Flow | Pending |
| CTRL-03 | Phase 4 | Control Flow | Pending |
| CTRL-04 | Phase 4 | Control Flow | Pending |
| FUNC-01 | Phase 5 | Functions & Abstraction | Pending |
| FUNC-02 | Phase 5 | Functions & Abstraction | Pending |
| FUNC-03 | Phase 5 | Functions & Abstraction | Pending |
| FUNC-04 | Phase 5 | Functions & Abstraction | Pending |
| QUAL-01 | Phase 6 | Quality & Polish | Pending |
| QUAL-02 | Phase 6 | Quality & Polish | Pending |
| QUAL-03 | Phase 6 | Quality & Polish | Pending |

**Coverage:**
- v1 requirements: 22 total
- Mapped to phases: 22/22 ✓
- Unmapped: 0 ✓

**Phase Distribution:**
- Phase 1 (Foundation & Pipeline): 4 requirements
- Phase 2 (Arithmetic Expressions): 4 requirements
- Phase 3 (Variables & Binding): 3 requirements
- Phase 4 (Control Flow): 4 requirements
- Phase 5 (Functions & Abstraction): 4 requirements
- Phase 6 (Quality & Polish): 3 requirements

---
*Requirements defined: 2025-01-30*
*Last updated: 2025-01-30 after roadmap creation*
