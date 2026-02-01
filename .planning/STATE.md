# Project State: LangTutorial

## Current Status

**Milestone:** v4.0 타입 시스템
**Status:** Defining requirements
**Started:** 2026-02-01

---

## Project Reference

**See:** .planning/PROJECT.md (updated 2026-02-01)

**Core value:** 각 챕터가 독립적으로 동작하는 완전한 예제를 제공하여, 독자가 언어 구현의 각 단계를 직접 따라하고 실행해볼 수 있어야 한다.

**Current focus:** Hindley-Milner 타입 추론 구현

**Tech stack:**
- F# (.NET 10)
- FsLexYacc 11.3.0 (fslex + fsyacc)
- Argu 6.2.5 (CLI argument parsing)
- Value type (IntValue | BoolValue | FunctionValue | StringValue | TupleValue | ListValue)
- Expecto + FsCheck + fslit for testing

---

## Milestone History

| Milestone | Shipped | Phases | Key Achievement |
|-----------|---------|--------|-----------------|
| v1.0 MVP | 2026-01-31 | 1-5, 7 | Turing-complete 언어 |
| v2.0 실용성 | 2026-02-01 | 1-3 | REPL, 문자열, 주석 |
| v3.0 데이터 구조 | 2026-02-01 | 1-4 | Tuples, Lists, Pattern Matching, Prelude |

**Archives:** `.planning/milestones/`

---

## Accumulated Decisions

Key decisions from previous milestones (see PROJECT.md for full table):

- Expr/Term/Factor 문법 (v1.0)
- Environment as Map (v1.0)
- 클로저 환경 캡처 (v1.0)
- Self-hosted Prelude (v3.0)
- evalToEnv pattern (v3.0)
- First-match semantics (v3.0)

---

## Session Continuity

**Last session:** 2026-02-01 - v4.0 milestone started
**What happened:** Started v4.0 타입 시스템 milestone. Updated PROJECT.md with target features.
**What's next:** Define requirements, create roadmap
**Resume command:** Continue with `/gsd:new-milestone`

---

*Last updated: 2026-02-01*
*Status: Defining requirements*
