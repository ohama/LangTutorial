# Project State: LangTutorial

## Current Status

**Milestone:** v2.0 실용성 강화
**Status:** SHIPPED
**Shipped:** 2026-02-01

```
v2.0 실용성 강화 - SHIPPED
├── Phase 1 [██████████] ● Comments (주석) - SHIPPED
├── Phase 2 [██████████] ● Strings (문자열) - SHIPPED
└── Phase 3 [██████████] ● REPL (대화형 셸) - SHIPPED
```

---

## Project Reference

**See:** .planning/PROJECT.md (updated 2026-02-01)

**Core value:** 각 챕터가 독립적으로 동작하는 완전한 예제를 제공하여, 독자가 언어 구현의 각 단계를 직접 따라하고 실행해볼 수 있어야 한다.

**Current focus:** v2.0 완료 - 다음 마일스톤 계획 대기

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

## Performance Metrics

**v1.0 Stats:**
- 6 phases, 12 plans
- 97 commits
- 2,117 lines F#
- 195 tests (66 fslit + 129 Expecto)
- 2 days development

**v2.0 Stats:**
- 3 phases, 4 plans
- 26 commits
- ~200 lines F# added
- +80 tests (275 total)
- 2 days development

---

## Accumulated Decisions

Key decisions made during v2.0 development:

| Phase | Decision | Rationale |
|-------|----------|-----------|
| 01-comments | Single-line: `//` (C-style) | Developer familiarity, widely recognized |
| 01-comments | Block: `(* *)` (ML-style) | F# consistency, supports nesting |
| 01-comments | Comments in lexer, not parser | No AST pollution, truly invisible |
| 01-comments | Pattern order: comments before operators | Ensures `//` matches before `/`, `(*` before `(` |
| 02-strings | Escape sequences in lexer | Clean separation, lexer responsibility |
| 02-strings | Heredoc for Lexer.fsl | Avoids fslex escape parsing issues |
| 02-strings | Type-safe operator overloading | Add works for int+int and string+string |
| 02-strings | Specific error messages | "Type error: + requires operands of same type (int or string)" |
| 03-repl | Argu for CLI parsing | Declarative approach replaces 120 lines of pattern matching |
| 03-repl | Auto underscore-to-hyphen | Emit_Tokens becomes --emit-tokens automatically |
| 03-repl | ProcessExiter with colorizer | Red errors, uncolored help text |
| 03-repl | #quit command | F# Interactive convention instead of exit |
| 03-repl | No-args starts REPL | Better UX - default to interactive mode |
| 03-repl | Error recovery in REPL | Errors print, REPL continues with same environment |

---

## Session Continuity

**Last session:** 2026-02-01 - v2.0 Milestone SHIPPED
**What happened:** Archived v2.0 milestone (roadmap, requirements, phases, audit)
**What's next:** Start next milestone with `/gsd:new-milestone` or project complete
**Stopped at:** Milestone completion workflow - git commit and tag pending
**Resume command:** `/gsd:new-milestone` for next milestone

---

*Last updated: 2026-02-01*
*Status: v2.0 SHIPPED*
