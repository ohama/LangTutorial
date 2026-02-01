# Project State: LangTutorial

## Current Status

**Milestone:** v3.0 데이터 구조 — **SHIPPED**
**Status:** Milestone complete and archived
**Shipped:** 2026-02-01

---

## Project Reference

**See:** .planning/PROJECT.md (updated 2026-02-01)

**Core value:** 각 챕터가 독립적으로 동작하는 완전한 예제를 제공하여, 독자가 언어 구현의 각 단계를 직접 따라하고 실행해볼 수 있어야 한다.

**Current focus:** Planning next milestone or project complete

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

**Last session:** 2026-02-01 - v3.0 milestone archived
**What happened:** Completed and archived v3.0 milestone. All phase artifacts moved to milestones/v3.0-phases/. ROADMAP.md and REQUIREMENTS.md deleted (fresh for next milestone).
**What's next:** `/gsd:new-milestone` to start next milestone or project complete
**Resume command:** `/gsd:new-milestone`

---

*Last updated: 2026-02-01*
*Status: Ready for next milestone*
