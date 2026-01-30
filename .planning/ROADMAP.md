# Roadmap: LangTutorial

## Overview

F# 언어 구현 튜토리얼 로드맵. fslex/fsyacc를 사용하여 점진적으로 기능을 추가하며 완전한 인터프리터를 구축합니다. 각 단계는 독립적으로 실행 가능한 코드를 생성합니다.

| Phase | Name | Goal | Requirements |
|-------|------|------|--------------|
| 1 | Foundation & Pipeline | 개발자가 fslex/fsyacc 기반 프로젝트를 설정하고 기본 파이프라인을 이해한다 | 4 |
| 2 | Arithmetic Expressions | 사용자가 사칙연산 계산기를 실행하여 즉각적인 결과를 얻는다 | 4 |
| 3 | Variables & Binding | 사용자가 변수에 값을 바인딩하고 재사용할 수 있다 | 3 |
| 4 | Control Flow | 사용자가 조건 분기로 논리를 표현할 수 있다 | 4 |
| 5 | Functions & Abstraction | 사용자가 함수를 정의하고 호출하여 코드를 재사용할 수 있다 | 4 |
| 6 | Quality & Polish | 사용자가 친화적인 오류 메시지와 대화형 REPL을 경험한다 | 3 |
| 7 | CLI Options & File-Based Tests | emit 옵션과 파일 기반 테스트 지원 | 5 |

**Total phases:** 7
**Total requirements:** 27
**Depth calibration:** Standard (6 phases fits 5-8 range)

---

## Phases

### Phase 1: Foundation & Pipeline

**Goal:** 개발자가 fslex/fsyacc 기반 프로젝트를 설정하고 기본 파이프라인을 이해한다

**Requirements:**
- **FOUND-01**: .NET 10 + FsLexYacc 프로젝트 구성 설명
- **FOUND-02**: fslex로 토큰 생성 (Lexer 기초)
- **FOUND-03**: fsyacc로 AST 생성 (Parser 기초)
- **FOUND-04**: Discriminated Union으로 AST 타입 정의

**Success Criteria:**
1. 개발자가 .NET 10 프로젝트를 생성하고 FsLexYacc 패키지를 설치할 수 있다
2. fslex가 Lexer.fsl 파일에서 Lexer.fs를 생성한다
3. fsyacc가 Parser.fsy 파일에서 Parser.fs와 토큰 타입을 생성한다
4. F# 코드에서 AST 타입 (Discriminated Union)을 정의하고 컴파일된다
5. 빌드 순서 (Parser → Lexer → AST)가 문서화되고 .fsproj에 올바르게 설정된다

**Depends on:** None (foundation phase)

**Plans:** 3 plans

Plans:
- [x] 01-01-PLAN.md — Project setup + AST types
- [x] 01-02-PLAN.md — Parser.fsy + Lexer.fsl + build configuration
- [x] 01-03-PLAN.md — Program.fs wiring + pipeline verification

**Notes:**
- 이 단계에서는 아직 실행 가능한 인터프리터를 만들지 않음
- Lexer와 Parser의 구조만 설정하고, 다음 단계에서 평가 로직 추가
- Build order dependency (Parser.fsy → Lexer.fsl) 해결이 핵심

---

### Phase 2: Arithmetic Expressions

**Goal:** 사용자가 사칙연산 계산기를 실행하여 즉각적인 결과를 얻는다

**Requirements:**
- **EXPR-01**: 사칙연산 (+, -, *, /) 구현
- **EXPR-02**: 연산자 우선순위 처리 (*, /가 +, -보다 먼저)
- **EXPR-03**: 괄호로 우선순위 변경
- **EXPR-04**: 단항 마이너스 (음수) 지원

**Success Criteria:**
1. 사용자가 "2 + 3 * 4"를 입력하면 14를 출력한다 (우선순위 준수)
2. 사용자가 "(2 + 3) * 4"를 입력하면 20을 출력한다 (괄호 우선순위)
3. 사용자가 "-5 + 3"을 입력하면 -2를 출력한다 (단항 마이너스)
4. 사용자가 "10 / 2 - 3"을 입력하면 2를 출력한다 (좌결합 연산)

**Depends on:** Phase 1 (Lexer와 Parser가 존재해야 평가 가능)

**Plans:** 2 plans

Plans:
- [x] 02-01-PLAN.md — AST expansion + Evaluator
- [x] 02-02-PLAN.md — Parser grammar + Lexer tokens + Pipeline wiring

**Notes:**
- 첫 번째 실행 가능한 인터프리터 (즉각적인 만족감 제공)
- 이 단계에서 Evaluator 컴포넌트 도입
- Expr/Term/Factor grammar pattern for precedence (avoid FsYacc %left/%right bugs)
- Chapter 1 튜토리얼 문서 작성 (tutorial/chapter-01-arithmetic.md)

---

### Phase 3: Variables & Binding

**Goal:** 사용자가 변수에 값을 바인딩하고 재사용할 수 있다

**Requirements:**
- **VAR-01**: let 바인딩 (let x = 5)
- **VAR-02**: 식에서 변수 참조
- **VAR-03**: let-in 식으로 지역 스코프

**Success Criteria:**
1. 사용자가 "let x = 10"을 입력하면 변수 x에 10이 저장된다
2. 사용자가 "x + 5"를 입력하면 15를 출력한다 (변수 참조)
3. 사용자가 "let y = x * 2"를 입력하면 기존 변수 x를 사용하여 y에 20이 저장된다
4. 사용자가 "let x = 3 in x + 5"를 입력하면 8을 출력하고 외부 x는 여전히 10이다 (지역 스코프)

**Depends on:** Phase 2 (표현식 평가 인프라 필요)

**Notes:**
- Environment 컴포넌트 도입 (immutable Map 기반)
- 스코프 관리 (global vs local)
- Chapter 2 튜토리얼 문서 작성 (tutorial/chapter-02-variables.md)

---

### Phase 4: Control Flow

**Goal:** 사용자가 조건 분기로 논리를 표현할 수 있다

**Requirements:**
- **CTRL-01**: if-then-else 조건 분기
- **CTRL-02**: Boolean 타입 (true, false 리터럴)
- **CTRL-03**: 비교 연산자 (=, <, >, <=, >=, <>)
- **CTRL-04**: 논리 연산자 (&&, ||)

**Success Criteria:**
1. 사용자가 "if true then 1 else 2"를 입력하면 1을 출력한다
2. 사용자가 "if 5 > 3 then 10 else 20"을 입력하면 10을 출력한다 (비교 연산)
3. 사용자가 "if x = 10 && y = 20 then 1 else 0"을 입력하면 논리 연산이 평가된다
4. 사용자가 "if 5 < 3 then 99 else 100"을 입력하면 100을 출력한다 (false 분기)

**Depends on:** Phase 3 (변수와 표현식 평가 필요)

**Notes:**
- Value 타입에 Boolean 추가
- if-then-else는 표현식 (값 반환)
- Chapter 3 튜토리얼 문서 작성 (tutorial/chapter-03-conditionals.md)

---

### Phase 5: Functions & Abstraction

**Goal:** 사용자가 함수를 정의하고 호출하여 코드를 재사용할 수 있다

**Requirements:**
- **FUNC-01**: 함수 정의 (let f x = x + 1)
- **FUNC-02**: 함수 호출 (f 5)
- **FUNC-03**: 재귀 함수 지원
- **FUNC-04**: 클로저 (외부 변수 캡처)

**Success Criteria:**
1. 사용자가 "let f x = x + 1"을 정의하고 "f 5"를 호출하면 6을 출력한다
2. 사용자가 "let add x y = x + y"를 정의하고 "add 3 4"를 호출하면 7을 출력한다 (다중 파라미터)
3. 사용자가 "let rec fib n = if n <= 1 then n else fib (n-1) + fib (n-2)"를 정의하고 "fib 6"을 호출하면 8을 출력한다 (재귀)
4. 사용자가 "let makeAdder x = (let add y = x + y in add)"를 정의하고 "let add5 = makeAdder 5"로 클로저를 생성한 후 "add5 3"을 호출하면 8을 출력한다 (클로저)

**Depends on:** Phase 4 (조건문이 재귀 함수에 필요)

**Notes:**
- Function 값 타입 도입 (파라미터, 본문, 캡처된 환경)
- 클로저는 환경을 캡처하여 first-class function 지원
- Chapter 4 튜토리얼 문서 작성 (tutorial/chapter-04-functions.md)
- 이 단계 완료 시 Turing-complete 언어 달성

---

### Phase 6: Quality & Polish

**Goal:** 사용자가 친화적인 오류 메시지와 대화형 REPL을 경험한다

**Requirements:**
- **QUAL-01**: 사용자 친화적 에러 메시지
- **QUAL-02**: 대화형 REPL 셸
- **QUAL-03**: Expecto, FsCheck, fslit으로 테스트

**Success Criteria:**
1. 사용자가 잘못된 문법 "let x ="을 입력하면 위치와 예상 토큰을 포함한 명확한 에러 메시지를 표시한다
2. 사용자가 정의되지 않은 변수 "y"를 참조하면 "Variable 'y' not found. Did you mean 'x'?"와 같은 제안을 받는다
3. 사용자가 REPL을 실행하면 프롬프트가 표시되고 여러 줄의 명령을 순차적으로 입력할 수 있다
4. Expecto 단위 테스트, FsCheck 속성 테스트, fslit 파일 기반 테스트가 모두 통과한다

**Depends on:** Phase 5 (모든 기능이 구현되어야 종합 테스트 가능)

**Notes:**
- Error 타입 정의 (LexError, ParseError, RuntimeError)
- Position tracking (line, column)
- REPL은 세션 상태 유지 (환경이 누적됨)
- Expecto (단위), FsCheck (속성), fslit (파일 기반) 테스트로 회귀 방지

---

### Phase 7: CLI Options & File-Based Tests

**Goal:** 사용자가 다양한 입력 방식과 emit 옵션으로 언어의 각 단계를 검증한다

**Requirements:**
- **CLI-01**: 입력 방식 — `--expr <expr>` 또는 파일명 (positional argument)
- **CLI-02**: `--emit-tokens` — 렉서 단계에서 토큰 목록 출력
- **CLI-03**: `--emit-ast` — 파서 단계에서 AST 출력
- **CLI-04**: `--emit-type` — (예약) 타입 체킹 단계 출력
- **CLI-05**: emit 옵션을 활용한 파일 기반 테스트 (fslit)

**Success Criteria:**
1. `funlang --expr "2 + 3"` → `5` (현재 동작 유지)
2. `funlang program.fun` → 파일 내용 실행
3. `funlang --emit-tokens --expr "2 + 3"` → `NUMBER(2) PLUS NUMBER(3) EOF`
4. `funlang --emit-ast --expr "2 + 3"` → `Add(Number 2, Number 3)`
5. `funlang --emit-type --expr "..."` → 예약 (Phase 미구현 시 에러)
6. fslit으로 `.fun` 파일들의 expected output 검증

**Depends on:** Phase 2 (기본 인터프리터 필요, Phase 3-6과 병렬 가능)

**Plans:** 2 plans

Plans:
- [x] 07-01-PLAN.md — Token formatter + CLI expansion
- [x] 07-02-PLAN.md — fslit test suite

**Notes:**
- emit 옵션은 디버깅과 테스트에 유용
- `--emit-type`은 타입 시스템 추가 시 활성화
- fslit (https://github.com/ohama/fslit) 사용

---

## Progress Tracking

| Phase | Status | Progress |
|-------|--------|----------|
| 1 - Foundation & Pipeline | ● Complete | 4/4 requirements |
| 2 - Arithmetic Expressions | ● Complete | 4/4 requirements |
| 3 - Variables & Binding | ○ Pending | 0/3 requirements |
| 4 - Control Flow | ○ Pending | 0/4 requirements |
| 5 - Functions & Abstraction | ○ Pending | 0/4 requirements |
| 6 - Quality & Polish | ○ Pending | 0/3 requirements |
| 7 - CLI Options & File-Based Tests | ● Complete | 5/5 requirements |

**Overall:** 3/7 phases complete (43%)

**Legend:**
- ○ Pending: Not started
- ◐ In Progress: Active work
- ● Complete: All requirements met

---

## Coverage Validation

**All v1 requirements mapped:** Yes

| Category | Requirements | Phase | Mapped |
|----------|--------------|-------|--------|
| Foundation | FOUND-01, FOUND-02, FOUND-03, FOUND-04 | Phase 1 | 4/4 |
| Expressions | EXPR-01, EXPR-02, EXPR-03, EXPR-04 | Phase 2 | 4/4 |
| Variables | VAR-01, VAR-02, VAR-03 | Phase 3 | 3/3 |
| Control Flow | CTRL-01, CTRL-02, CTRL-03, CTRL-04 | Phase 4 | 4/4 |
| Functions | FUNC-01, FUNC-02, FUNC-03, FUNC-04 | Phase 5 | 4/4 |
| Quality | QUAL-01, QUAL-02, QUAL-03 | Phase 6 | 3/3 |
| CLI & Tests | CLI-01, CLI-02, CLI-03, CLI-04, CLI-05 | Phase 7 | 5/5 |

**Total mapped:** 27/27 requirements
**Orphaned:** 0
**Duplicates:** 0

---

## Dependencies

```
Phase 1 (Foundation)
    |
Phase 2 (Expressions) <- First runnable code
    |
    +---> Phase 7 (CLI & File Tests) <- Parallel track
    |
Phase 3 (Variables)
    |
Phase 4 (Control Flow)
    |
Phase 5 (Functions) <- Turing-complete
    |
Phase 6 (Quality)
```

**Critical path:** 1 -> 2 -> 3 -> 4 -> 5 -> 6 (main sequential)
**Parallel track:** 2 -> 7 (independent of 3-6)

**Rationale:**
- Phase 2 needs Phase 1's pipeline
- Phase 3 needs Phase 2's evaluation infrastructure
- Phase 4 needs Phase 3's variable support (conditions use variables)
- Phase 5 needs Phase 4's conditionals (recursive function termination)
- Phase 6 needs Phase 5's all features (integration testing)
- Phase 7 only needs Phase 2's basic interpreter (can run in parallel with 3-6)

---

## Next Steps

**Immediate:** `/gsd:plan-phase 3` to plan Phase 3 (Variables & Binding)

---

*Roadmap created: 2025-01-30*
*Last updated: 2026-01-30 (Phase 7 complete)*
