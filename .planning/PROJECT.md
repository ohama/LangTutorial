# LangTutorial

## What This Is

F# 개발자를 위한 프로그래밍 언어 구현 튜토리얼. fslex와 fsyacc를 사용하여 Turing-complete 인터프리터를 단계별로 만들어가는 과정을 문서화한다. 사칙연산에서 시작해 변수, 조건문, 함수까지 확장하여 완전한 함수형 언어를 구현한다.

## Core Value

각 챕터가 독립적으로 동작하는 완전한 예제를 제공하여, 독자가 언어 구현의 각 단계를 직접 따라하고 실행해볼 수 있어야 한다.

## Current Milestone: v3.0 데이터 구조

**Goal:** 복합 데이터 타입, 패턴 매칭, 표준 라이브러리를 추가하여 FunLang을 실용적인 함수형 언어로 발전

**Target features:**
- 튜플 (Tuples) — 고정 크기 이종 데이터 컬렉션, 패턴 분해
- 리스트 (Lists) — 동종 데이터의 가변 길이 컬렉션, cons 연산자
- 패턴 매칭 (Pattern Matching) — 구조적 분해와 조건 분기 통합
- Prelude (표준 라이브러리) — map, filter, fold 등 기본 함수

## Current State (v2.0 Shipped)

**v2.0 Shipped:** 2026-02-01 (2 days after v1.0)

**What's implemented:**
- Foundation & Pipeline - .NET 10 + FsLexYacc 파이프라인
- Arithmetic - 사칙연산, 연산자 우선순위, 단항 마이너스
- Variables - let 바인딩, 환경 기반 스코프
- Control Flow - Boolean, if-then-else, 비교/논리 연산자
- Functions - 람다, 함수 호출, 재귀, 클로저
- **Comments** - `//` 단일행, `(* *)` 다중행 중첩 주석 (v2.0)
- **Strings** - 리터럴, 이스케이프, 연결, 비교 (v2.0)
- **REPL** - 대화형 루프, 오류 복구, #quit (v2.0)
- **Argu CLI** - 선언적 CLI 파싱 (v2.0)

**Codebase:**
- ~2,317 lines F#
- 73 files added/modified in v2.0
- 100 fslit tests + 175 Expecto tests = 275 total

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

### Validated (v2.0)

- **CMT-01**: 단일행 주석 `//` — v2.0
- **CMT-02**: 다중행 주석 `(* *)` — v2.0
- **CMT-03**: 중첩 주석 지원 — v2.0
- **CMT-04**: 미종료 주석 오류 — v2.0
- **STR-01~12**: 문자열 타입 전체 — v2.0 (리터럴, 이스케이프, 연결, 비교, 오류 처리)
- **REPL-01~08**: REPL 전체 — v2.0 (루프, 프롬프트, 환경, 오류 복구, 종료)

### On Hold (v2.1+)

- **QUAL-01**: 사용자 친화적 에러 메시지
- **QUAL-02**: REPL 히스토리 (readline)
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
| `//` 주석 (C-style) | 개발자 친숙함 | Good |
| `(* *)` 주석 (ML-style) | F# 일관성, 중첩 지원 | Good |
| 주석은 Lexer에서 처리 | AST 오염 방지 | Good |
| 이스케이프는 Lexer에서 처리 | 관심사 분리 | Good |
| Argu CLI | 선언적, 120 LOC 대체 | Good |
| #quit 명령 | F# Interactive 관례 | Good |
| no-args → REPL | 더 나은 UX | Good |

---
*Last updated: 2026-02-01 after v3.0 milestone start*
