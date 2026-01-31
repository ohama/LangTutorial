# Project State: LangTutorial

## Current Status

**Milestone:** v3.0 데이터 구조
**Status:** Ready to plan
**Started:** 2026-02-01

```
v3.0 데이터 구조 - IN PROGRESS
├── Phase 1 [░░░░░░░░░░] ○ Tuples (튜플)
├── Phase 2 [░░░░░░░░░░] ○ Lists (리스트)
├── Phase 3 [░░░░░░░░░░] ○ Pattern Matching (패턴 매칭)
└── Phase 4 [░░░░░░░░░░] ○ Prelude (표준 라이브러리)
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
- Value type (IntValue | BoolValue | FunctionValue | StringValue)
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

---

## Session Continuity

**Last session:** 2026-02-01 - v3.0 Roadmap created
**What happened:** Defined 25 requirements, created 4-phase roadmap
**What's next:** Plan and execute Phase 1 (Tuples)
**Stopped at:** Roadmap approval
**Resume command:** `/gsd:discuss-phase 1`

---

*Last updated: 2026-02-01*
*Status: v3.0 Ready to plan*
