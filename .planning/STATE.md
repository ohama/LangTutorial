# Project State: LangTutorial

## Current Status

**Milestone:** v3.0 데이터 구조
**Status:** Phase 4 in progress (1/2 plans done)
**Started:** 2026-02-01

```
v3.0 데이터 구조 - IN PROGRESS
├── Phase 1 [██████████] ● Tuples (튜플) ✓
├── Phase 2 [██████████] ● Lists (리스트) ✓
├── Phase 3 [██████████] ● Pattern Matching (패턴 매칭) ✓
└── Phase 4 [█████░░░░░] ◐ Prelude (표준 라이브러리)
```

---

## Project Reference

**See:** .planning/PROJECT.md (updated 2026-02-01)

**Core value:** 각 챕터가 독립적으로 동작하는 완전한 예제를 제공하여, 독자가 언어 구현의 각 단계를 직접 따라하고 실행해볼 수 있어야 한다.

**Current focus:** v3.0 데이터 구조 - 튜플, 리스트, 패턴 매칭, Prelude

**Tech stack:**
- F# (.NET 10)
- FsLexYacc 11.3.0 (fslex + fsyacc)
- Argu 6.2.5 (CLI argument parsing)
- Value type (IntValue | BoolValue | FunctionValue | StringValue | TupleValue | ListValue)
- Expecto + FsCheck + fslit for testing
- State machine pattern for complex lexing

---

## Milestone History

| Milestone | Shipped | Phases | Key Achievement |
|-----------|---------|--------|-----------------|
| v1.0 MVP | 2026-01-31 | 1-5, 7 | Turing-complete 언어 |
| v2.0 실용성 | 2026-02-01 | 1-3 | REPL, 문자열, 주석 |

**Archives:** `.planning/milestones/`

---

## Accumulated Decisions

Key decisions from previous milestones:

| Phase | Decision | Rationale |
|-------|----------|-----------|
| v1.0 | Expr/Term/Factor 문법 | %left/%right 대신 문법으로 우선순위 |
| v1.0 | Environment as Map | O(log n) 조회, 불변, 함수형 스타일 |
| v1.0 | 클로저 환경 캡처 | First-class function 지원 |
| v2.0 | `//` 주석 (C-style) | Developer familiarity |
| v2.0 | `(* *)` 주석 (ML-style) | F# 일관성, 중첩 지원 |
| v2.0 | 주석은 Lexer에서 처리 | AST 오염 방지 |
| v2.0 | Argu CLI | 선언적, 120 LOC 대체 |
| v3.0 | ExprList 재사용 (리스트/튜플) | 코드 중복 제거, 일관성 |
| v3.0 | Cons 연산자 우선순위 | 비교와 산술 사이 배치 (F# 일관성) |
| v3.0 | F# 구조적 동등성 사용 (리스트) | 커스텀 동등성 함수 불필요, 튜플 패턴 따름 |
| v3.0 | 리스트 포매팅 [1, 2, 3] | F# 스타일 일관성 |
| v3.0 | MatchClause as tuple | Pattern * Expr - 간단하고 충분 |
| v3.0 | Constant type for patterns | 향후 문자열 패턴 확장 가능 |
| v3.0 | Leading PIPE required | F# 스타일 match 문법 |
| v3.0 | First-match semantics | 패턴 순차 평가, 첫 매치 승리 |
| v3.0 | Runtime Match failure | 비완전 매치는 런타임 에러 |
| v3.0 | evalToEnv recursive accumulation | 중첩된 let-in 표현식 처리, 환경 축적 |
| v3.0 | Self-hosted standard library | Prelude.fun in FunLang source (dogfooding) |
| v3.0 | Graceful prelude degradation | loadPrelude returns emptyEnv on error |

---

## Session Continuity

**Last session:** 2026-02-01 - Phase 4 Plan 01 complete
**What happened:** Created Prelude infrastructure with evalToEnv and loadPrelude in Prelude.fs. Implemented 11 standard library functions in Prelude.fun (map, filter, fold, length, reverse, append, hd, tl, id, const, compose). Self-hosted stdlib approach established.
**What's next:** Execute Phase 4 Plan 02 (integrate Prelude loading into REPL/CLI)
**Stopped at:** 04-01 complete - Prelude infrastructure ready
**Resume command:** `/gsd:execute-phase` for 04-02

---

*Last updated: 2026-02-01*
*Status: Phase 4 in progress (1/2 plans done)*
