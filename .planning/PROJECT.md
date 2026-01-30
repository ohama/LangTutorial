# LangTutorial

## What This Is

F# 개발자를 위한 프로그래밍 언어 구현 튜토리얼. fslex와 fsyacc를 사용하여 Turing-complete 인터프리터를 단계별로 만들어가는 과정을 문서화한다. 사칙연산에서 시작해 변수, 조건문, 함수까지 확장하여 완전한 함수형 언어를 구현한다.

## Core Value

각 챕터가 독립적으로 동작하는 완전한 예제를 제공하여, 독자가 언어 구현의 각 단계를 직접 따라하고 실행해볼 수 있어야 한다.

## Current State (v1.0 Shipped)

**Shipped:** 2026-01-31

**What's implemented:**
- Foundation & Pipeline - .NET 10 + FsLexYacc 파이프라인
- Arithmetic - 사칙연산, 연산자 우선순위, 단항 마이너스
- Variables - let 바인딩, 환경 기반 스코프
- Control Flow - Boolean, if-then-else, 비교/논리 연산자
- Functions - 람다, 함수 호출, 재귀, 클로저
- CLI & Testing - emit-tokens/ast, 파일 입력, 195개 테스트

**Codebase:**
- 2,117 lines F#
- 147 project files
- 66 fslit tests + 129 Expecto tests

## Requirements

### Validated

- **FOUND-01**: .NET 10 + FsLexYacc 프로젝트 구성 — v1.0
- **FOUND-02**: fslex로 토큰 생성 — v1.0
- **FOUND-03**: fsyacc로 AST 생성 — v1.0
- **FOUND-04**: Discriminated Union AST 타입 — v1.0
- **EXPR-01**: 사칙연산 (+, -, *, /) — v1.0
- **EXPR-02**: 연산자 우선순위 — v1.0
- **EXPR-03**: 괄호 우선순위 — v1.0
- **EXPR-04**: 단항 마이너스 — v1.0
- **VAR-01**: let 바인딩 — v1.0
- **VAR-02**: 변수 참조 — v1.0
- **VAR-03**: let-in 지역 스코프 — v1.0
- **CTRL-01**: if-then-else — v1.0
- **CTRL-02**: Boolean 타입 — v1.0
- **CTRL-03**: 비교 연산자 — v1.0
- **CTRL-04**: 논리 연산자 — v1.0
- **FUNC-01**: 함수 정의 — v1.0
- **FUNC-02**: 함수 호출 — v1.0
- **FUNC-03**: 재귀 함수 — v1.0
- **FUNC-04**: 클로저 — v1.0
- **CLI-01~05**: CLI 옵션 및 테스트 — v1.0

### On Hold (Phase 6)

- **QUAL-01**: 사용자 친화적 에러 메시지
- **QUAL-02**: 대화형 REPL 셸
- **QUAL-03**: 테스트 프레임워크 통합 (부분 완료)

### Out of Scope

- 실수 (float/double) — 정수만 지원, 파싱/연산 단순화
- 컴파일러 (바이트코드/네이티브 코드 생성) — 인터프리터에 집중
- 타입 시스템 — v1 범위 초과, 추후 확장 가능
- 표준 라이브러리 — 언어 코어에 집중

## Context

**Tech stack:**
- F# (.NET 10)
- FsLexYacc 11.3.0 — 렉서/파서 생성기
- Expecto — 단위 테스트
- FsCheck — 속성 기반 테스트
- fslit — 파일 기반 CLI 테스트

**Directory structure:**
- `tutorial/` — 튜토리얼 문서 (5 chapters)
- `FunLang/` — 언어 구현 (Ast, Lexer, Parser, Eval, Format, Program)
- `FunLang.Tests/` — Expecto 단위 테스트
- `tests/` — fslit CLI 테스트
- `docs/howto/` — 개발 지식 문서 (13개)

## Constraints

- **Language**: F# — 함수형 언어로 인터프리터 구현에 적합
- **Tools**: fslex, fsyacc — F#용 전통적 렉서/파서 생성기
- **Target Audience**: F# 개발자 — F# 문법 설명 불필요

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| 인터프리터 방식 | 컴파일러보다 단순, 즉각적 결과 확인 | Good |
| fslex/fsyacc 사용 | F# 생태계 표준 도구, 학습 가치 | Good |
| Expr/Term/Factor 문법 | %left/%right 대신 문법으로 우선순위 | Good |
| Environment as Map | O(log n) 조회, 불변, 함수형 스타일 | Good |
| Value 타입 (Int\|Bool\|Function) | 다형적 평가 결과 | Good |
| 클로저 환경 캡처 | First-class function 지원 | Good |
| Phase 6 보류 | MVP 달성, REPL/에러 메시지는 선택적 | Acceptable |

---
*Last updated: 2026-01-31 after v1.0 milestone*
