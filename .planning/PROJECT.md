# LangTutorial

## What This Is

F# 개발자를 위한 프로그래밍 언어 구현 튜토리얼. fslex와 fsyacc를 사용하여 Turing-complete 인터프리터를 단계별로 만들어가는 과정을 문서화한다. 사칙연산에서 시작해 변수, 조건문, 함수, 데이터 구조까지 확장하여 완전한 함수형 언어를 구현한다.

## Core Value

각 챕터가 독립적으로 동작하는 완전한 예제를 제공하여, 독자가 언어 구현의 각 단계를 직접 따라하고 실행해볼 수 있어야 한다.

## Previous State (v4.0 Shipped)

**v4.0 Shipped:** 2026-02-01

**What's implemented:**
- Foundation & Pipeline - .NET 10 + FsLexYacc 파이프라인
- Arithmetic - 사칙연산, 연산자 우선순위, 단항 마이너스
- Variables - let 바인딩, 환경 기반 스코프
- Control Flow - Boolean, if-then-else, 비교/논리 연산자
- Functions - 람다, 함수 호출, 재귀, 클로저
- Comments - `//` 단일행, `(* *)` 다중행 중첩 주석
- Strings - 리터럴, 이스케이프, 연결, 비교
- REPL - 대화형 루프, 오류 복구, #quit
- Argu CLI - 선언적 CLI 파싱
- Tuples - 고정 크기 이종 데이터, 패턴 분해 (v3.0)
- Lists - 가변 길이 컬렉션, cons 연산자 (v3.0)
- Pattern Matching - match 표현식, 7가지 패턴 타입 (v3.0)
- Prelude - 자체 호스팅 표준 라이브러리 11개 함수 (v3.0)
- **Type System** - Hindley-Milner 타입 추론, Algorithm W (v4.0)
- **Let-polymorphism** - 다형성 let 바인딩 (v4.0)
- **--emit-type** - CLI 타입 표시 플래그 (v4.0)

**Codebase:**
- ~4,805 lines F# (2,608 core + 2,197 tests)
- 460 total tests (98 fslit + 362 Expecto)

## Requirements

### Validated (v1.0)

- ✓ FOUND-01~04: .NET 10 + FsLexYacc 프로젝트 구성
- ✓ EXPR-01~04: 사칙연산, 우선순위, 괄호, 단항 마이너스
- ✓ VAR-01~03: let 바인딩, 변수 참조, 스코프
- ✓ CTRL-01~04: if-then-else, Boolean, 비교/논리 연산자
- ✓ FUNC-01~04: 함수 정의/호출, 재귀, 클로저
- ✓ CLI-01~05: CLI 옵션 및 테스트

### Validated (v2.0)

- ✓ CMT-01~04: 주석 (단일행, 다중행, 중첩)
- ✓ STR-01~12: 문자열 타입 (리터럴, 이스케이프, 연결, 비교)
- ✓ REPL-01~08: REPL (루프, 프롬프트, 환경, 오류 복구, 종료)

### Validated (v3.0)

- ✓ TUP-01~04: 튜플 (리터럴, 패턴 분해, 중첩, TupleValue)
- ✓ LIST-01~04: 리스트 (빈 리스트, 리터럴, cons, ListValue)
- ✓ PAT-01~08: 패턴 매칭 (match, 변수, 와일드카드, 상수, cons, 튜플, 빈 리스트, 완전성)
- ✓ PRE-01~09: Prelude (map, filter, fold, length, reverse, append, id, const, compose, hd, tl, 자동 로드)

### Validated (v4.0)

- ✓ TYPE-01~04: 타입 정의 (Type AST, Scheme, TypeEnv, formatType)
- ✓ SUBST-01~03: 대체 연산 (apply, compose, freeVars)
- ✓ UNIFY-01~03: 단일화 (occurs check, unify, TypeError)
- ✓ INFER-01~15: 타입 추론 (Algorithm W, let-polymorphism, 패턴)
- ✓ INTEG-01~04: 통합 (Prelude 타입, typecheck, --emit-type, 오류 처리)
- ✓ TEST-01~07: 테스트 (Type, Subst, Unify, Infer, TypeCheck, fslit)

### Active (v5.0)

## Current Milestone: v5.0 타입 에러 진단

**Goal:** Algorithm W의 에러 위치/원인을 정확히 표현하는 Diagnostic 인프라 구축

**Target features:**
- Span/Range 기반 위치 추적
- Diagnostic 타입 시스템 (에러 코드, 위치, 기대/실제 타입)
- Context Stack (추론 경로 추적)
- Unification Trace (단일화 실패 경로)
- 정확한 에러 메시지 포맷
- Bidirectional Typing 확장 준비

### On Hold (v5.0+)

- **ADT-01**: 대수적 데이터 타입 (Sum types)
- **ADT-02**: 사용자 정의 타입 생성자
- **TCO-01**: 꼬리 호출 최적화
- **EXC-01**: 예외 처리
- **MOD-01**: 모듈 시스템

### Out of Scope

- 실수 (float/double) — 정수만 지원, 파싱/연산 단순화
- 컴파일러 (바이트코드/네이티브 코드 생성) — 인터프리터에 집중
- 타입 주석 구문 — 타입 추론만 지원, 명시적 주석 불필요

## Context

**Tech stack:**
- F# (.NET 10)
- FsLexYacc 11.3.0 — 렉서/파서 생성기
- Expecto — 단위 테스트
- FsCheck — 속성 기반 테스트
- fslit — 파일 기반 CLI 테스트

**Directory structure:**
- `tutorial/` — 튜토리얼 문서 (9 chapters + 1 appendix)
- `youtube/` — YouTube 스크립트 (10개)
- `FunLang/` — 언어 구현 (Ast, Lexer, Parser, Eval, Format, Prelude, Program)
- `FunLang.Tests/` — Expecto 단위 테스트
- `tests/` — fslit CLI 테스트
- `docs/` — 문서 (grammar.md, howto/ 17개)
- `Prelude.fun` — 자체 호스팅 표준 라이브러리

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
| Value 타입 다형성 | Int\|Bool\|Function\|String\|Tuple\|List | Good |
| 클로저 환경 캡처 | First-class function 지원 | Good |
| Self-hosted Prelude | FunLang으로 표준 라이브러리 작성 (dogfooding) | Good |
| evalToEnv pattern | 중첩 let 바인딩을 환경으로 수집 | Good |
| First-match semantics | 패턴 매칭에서 첫 번째 매치 사용 | Good |
| Right-associative cons | `1 :: 2 :: []` = `1 :: (2 :: [])` | Good |

| Hindley-Milner 타입 추론 | 타입 클래스 없이 완전한 추론, 교육 목적에 적합 | Good |

---
*Last updated: 2026-02-02 after v5.0 milestone start*
