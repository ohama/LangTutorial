---
phase: 03-blame-assignment
plan: 01
subsystem: diagnostics
tags: [type-error, spans, context-stack, blame-assignment]

# Dependency graph
requires:
  - phase: 02-error-representation
    provides: TypeError with ContextStack, InferContext types
provides:
  - contextToSecondarySpans helper function
  - SecondarySpans populated in Diagnostic
  - Tests for secondary span extraction
affects: [04-output-testing, error-display, diagnostic-formatting]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Context stack processed outer-to-inner for display"
    - "Primary span excluded from secondary spans"
    - "Secondary spans limited to 3 for clarity"

key-files:
  created: []
  modified:
    - FunLang/Diagnostic.fs
    - FunLang.Tests/InferTests.fs

key-decisions:
  - "Secondary spans extracted in display order (outer-to-inner)"
  - "Primary span filtered to prevent duplication"
  - "Limit of 3 secondary spans to avoid clutter"

patterns-established:
  - "contextToSecondarySpans pattern: reverse, map, filter, distinctBy, truncate"

# Metrics
duration: 23min
completed: 2026-02-03
---

# Phase 3 Plan 1: Secondary Spans from Context Stack Summary

**SecondarySpans populated from TypeError.ContextStack with deduplication and limit of 3, enabling multi-location diagnostic display**

## Performance

- **Duration:** 23 min
- **Started:** 2026-02-03T02:16:45Z
- **Completed:** 2026-02-03T02:40:02Z
- **Tasks:** 3
- **Files modified:** 2

## Accomplishments
- contextToSecondarySpans function handles all 14 InferContext cases
- typeErrorToDiagnostic now populates SecondarySpans (no longer empty)
- Primary span excluded from secondary spans to prevent duplication
- Secondary spans limited to 3 for clarity
- 4 new tests verify secondary span extraction behavior

## Task Commits

Each task was committed atomically:

1. **Task 1: Add contextToSecondarySpans helper** - `78c5835` (feat)
2. **Task 2: Update typeErrorToDiagnostic** - `fbb2aa6` (feat)
3. **Task 3: Add tests for secondary spans** - `218deb1` (test)

## Files Created/Modified
- `FunLang/Diagnostic.fs` - Added contextToSecondarySpans, updated typeErrorToDiagnostic
- `FunLang.Tests/InferTests.fs` - Added getDiagnostic helper and 4 secondary span tests

## Decisions Made
- Secondary spans processed in display order (outer-to-inner) by reversing context list
- Primary span filtered out using equality check to prevent duplication
- Limit of 3 secondary spans chosen to match research findings (avoid clutter)
- Labels kept concise (e.g., "in then branch" not "in if then-branch at 1:14-1:23")

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- SecondarySpans now populated with related locations
- Phase 4 (Output & Testing) can format these spans in error output
- Diagnostic structure complete for rendering

---
*Phase: 03-blame-assignment*
*Completed: 2026-02-03*
