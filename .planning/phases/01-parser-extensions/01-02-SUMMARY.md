---
phase: 01-parser-extensions
plan: 02
subsystem: parser
tags: [parser, grammar, type-expressions, fsyacc, v6.0]

# Dependency graph
requires:
  - phase: 01-01
    provides: Type annotation tokens (COLON, TYPE_INT, TYPE_BOOL, TYPE_STRING, TYPE_LIST, TYPE_VAR) and TypeExpr AST
provides:
  - TypeExpr grammar with 5 non-terminals for parsing type expressions
  - Right-associative arrow types (int -> int -> int)
  - Tuple types with correct precedence (int * bool)
  - Postfix list syntax (int list)
  - Parenthesized type grouping ((int -> int) list)
affects: [01-03-annotation-syntax, 02-inference-foundation]

# Tech tracking
tech-stack:
  added: []
  patterns: [grammar-structure-for-precedence, three-level-type-hierarchy]

key-files:
  created: []
  modified:
    - path: FunLang/Parser.fsy
      reason: Added token declarations and TypeExpr grammar with 5 non-terminals

key-decisions:
  - "No precedence declarations for type operators - use grammar structure instead"
  - "Three-level hierarchy (Arrow > Tuple > Atomic) encodes precedence without conflicts"
  - "Right-associative arrow via recursive ArrowType rule"
  - "Postfix TYPE_LIST in AtomicType for ML-style syntax"
  - "TypeExpr has no Span fields (errors come from annotated expressions)"

patterns-established:
  - "Grammar hierarchy pattern: Use nested non-terminals to encode operator precedence"
  - "Right-associativity pattern: Recursive rule at same level (ArrowType -> TupleType ARROW ArrowType)"

# Metrics
duration: 12m 3s
completed: 2026-02-03
---

# Phase 01 Plan 02: Parser Grammar Rules Summary

**Parser declares 6 type annotation tokens and TypeExpr grammar with 5 non-terminals for parsing ML-style type expressions with correct precedence.**

## Performance

- **Duration:** 12 min 3 sec
- **Started:** 2026-02-03T09:55:08Z
- **Completed:** 2026-02-03T10:07:11Z
- **Tasks:** 3
- **Files modified:** 3

## Accomplishments
- Token declarations for COLON and 6 type tokens integrated into Parser.fsy
- Complete TypeExpr grammar with right-associative arrows and correct tuple precedence
- Clean build with no fsyacc conflicts (114 states, 17 non-terminals)
- All existing tests pass - backward compatibility maintained

## Task Commits

Each task was committed atomically:

1. **Task 1: Add token declarations to Parser.fsy** - `9c507b6` (feat)
   - Added COLON, TYPE_INT, TYPE_BOOL, TYPE_STRING, TYPE_LIST, TYPE_VAR tokens
   - Build succeeds with generated parser files

2. **Task 2: Add TypeExpr grammar rules to Parser.fsy** - `4e4851e` (feat)
   - Added TypeExpr, ArrowType, TupleType, TupleTypeList, AtomicType non-terminals
   - Three-level hierarchy encodes precedence (Arrow > Tuple > Atomic)
   - Grammar compiles without conflicts

3. **Task 3: Verify build and run existing tests** - (verification only, no commit)
   - Full build succeeded
   - Expecto tests passed (378/378)
   - Individual test categories verified

## Files Created/Modified

- `FunLang/Parser.fsy` - Added token declarations and TypeExpr grammar
- `FunLang/Parser.fs` - Generated parser implementation
- `FunLang/Parser.fsi` - Generated parser interface

## Decisions Made

1. **No precedence declarations for type operators**
   - Use grammar structure hierarchy instead of %left/%right
   - Avoids potential shift/reduce conflicts
   - Makes precedence explicit in grammar structure

2. **Three-level grammar hierarchy**
   - TypeExpr → ArrowType → TupleType → AtomicType
   - Arrow has lowest precedence (int * bool -> string)
   - Tuple has higher precedence than arrow
   - Atomic types have highest precedence

3. **Right-associativity via recursive rule**
   - ArrowType: TupleType ARROW ArrowType
   - Makes int -> int -> int parse as int -> (int -> int)
   - Matches ML-style function type convention

4. **Postfix list syntax**
   - AtomicType TYPE_LIST (not TYPE_LIST AtomicType)
   - Enables int list, int list list, (int -> int) list
   - Matches F# and OCaml list type syntax

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None - grammar compiled cleanly with no conflicts.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

**Ready for 01-03 (Annotation Syntax):**
- TypeExpr grammar fully implemented and tested
- All 5 non-terminals compile without conflicts
- Token declarations ready for integration into expression rules
- Parser can now be extended with annotation syntax (e : T), (x: T) -> e

**No blockers or concerns.**

---
*Phase: 01-parser-extensions*
*Completed: 2026-02-03*
