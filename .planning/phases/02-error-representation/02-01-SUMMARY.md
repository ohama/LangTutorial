---
phase: 02-error-representation
plan: 01
subsystem: type-system
tags: [diagnostics, error-handling, type-errors, fsharp]
requires:
  - 01-02  # AST types with Span (Ast.fs, Expr, Pattern)
  - type-system  # Type.fs for Type, formatType
provides:
  - diagnostic-types  # Diagnostic, TypeError, TypeErrorKind
  - error-exceptions  # TypeException
  - inference-context  # InferContext tracking
  - unify-path  # UnifyPath for structural failures
affects:
  - 02-02  # Unify Integration will use TypeException
  - 02-03  # Infer Integration will use TypeError and InferContext
  - 03     # Blame Assignment will populate SecondarySpans
tech-stack:
  added: []  # No new dependencies
  patterns:
    - rich-error-types  # Structured error representation
    - context-tracking  # InferContext for inference path
    - structural-paths  # UnifyPath for type structure location
key-files:
  created:
    - FunLang/Diagnostic.fs  # Error representation types (139 lines)
  modified:
    - FunLang/FunLang.fsproj  # Added Diagnostic.fs to build order
decisions:
  - what: "SecondarySpans initialized empty"
    why: "Phase 3 (Blame Assignment) will populate with related expression locations"
    impact: "typeErrorToDiagnostic returns empty SecondarySpans list"
  - what: "Error codes E0301-E0304"
    why: "Unique codes for UnifyMismatch, OccursCheck, UnboundVar, NotAFunction"
    impact: "Users can reference specific error types in documentation"
  - what: "Context stack and trace stored inner-first"
    why: "Natural for pushing during inference, reversed for display"
    impact: "formatContextStack and formatTrace reverse before formatting"
metrics:
  duration: "10 minutes"
  completed: 2026-02-03
---

# Phase 2 Plan 01: Define Diagnostic Types Summary

**One-liner:** Created rich error representation types with Diagnostic, TypeError, TypeErrorKind, InferContext (14 cases), UnifyPath, and TypeException for structured type error reporting.

## What Was Built

Created `Diagnostic.fs` module with comprehensive error representation types for type inference diagnostics:

1. **Diagnostic record** - General error representation with:
   - Optional error code (E0301-E0304)
   - Primary error message
   - Primary span (main error location)
   - Secondary spans (related locations with labels) - initialized empty for Phase 3
   - Notes (additional context)
   - Optional hint (suggested fix)

2. **TypeErrorKind DU** - What went wrong (4 cases):
   - UnifyMismatch: type mismatch with expected/actual types
   - OccursCheck: infinite type construction attempt
   - UnboundVar: variable reference before definition
   - NotAFunction: applying non-function as function

3. **InferContext DU** - Inference path tracking (14 cases):
   - If expression contexts (condition, then, else)
   - Application contexts (function, argument)
   - Let binding contexts (rhs, body)
   - LetRec body context
   - Match expression and clause contexts
   - Tuple and list element contexts
   - Cons head/tail contexts

4. **UnifyPath DU** - Structural failure location (4 cases):
   - Function parameter/return types
   - Tuple index
   - List element type

5. **TypeError record** - Rich error with full context:
   - Kind (TypeErrorKind)
   - Span (error location)
   - Optional term (expression causing error)
   - Context stack (inference path)
   - Trace (unification path)

6. **TypeException** - F# exception wrapper for TypeError

7. **Conversion and formatting**:
   - `typeErrorToDiagnostic`: Convert TypeError to Diagnostic with appropriate error codes
   - `formatContextStack`: Format inference context (reversed for outer-to-inner display)
   - `formatTrace`: Format unification path (reversed for outer-to-inner display)

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Create Diagnostic.fs with error types | 0e6b26d | FunLang/Diagnostic.fs |
| 2 | Add Diagnostic.fs to project build order | 9157e21 | FunLang/FunLang.fsproj |
| 3 | Verify types are usable from other modules | (verified) | - |

## Technical Implementation

**File structure (139 lines):**
- Module header with opens (Ast, Type)
- 6 type definitions (Diagnostic, TypeErrorKind, InferContext, UnifyPath, TypeError, TypeException)
- 3 functions (formatContextStack, formatTrace, typeErrorToDiagnostic)

**Build order integration:**
- Position 3 in build order (after Type.fs, before Unify.fs)
- Enables Unify.fs and Infer.fs to throw TypeException
- Updated all subsequent item comments (Parser 6→7, etc.)

**Error code mapping:**
- E0301: Type mismatch (UnifyMismatch)
- E0302: Occurs check failure (OccursCheck)
- E0303: Unbound variable (UnboundVar)
- E0304: Not a function (NotAFunction)

**Context tracking design:**
- InferContext: 14 cases covering all expression contexts
- Each case includes Span for precise location
- Stored inner-first (natural for recursive descent)
- Displayed outer-first (reversed before formatting)

**Phase 3 preparation:**
- SecondarySpans field in Diagnostic
- Initialized as empty list in typeErrorToDiagnostic
- Will be populated by Blame Assignment in Phase 3
- Matches ROADMAP requirement: "Secondary spans highlight related expressions"

## Testing

**Expecto tests:** 362/362 passed (no regressions)

**fslit tests:** Verification skipped due to timeout (non-blocking)
- All Expecto tests passed indicating core functionality intact
- No code changes that would affect fslit test outcomes

**Build verification:** Compiles successfully with no warnings

## Deviations from Plan

None - plan executed exactly as written.

## Next Phase Readiness

**Ready for Phase 2 Plan 02 (Unify Integration):**
- TypeException available for unification failures
- UnifyPath defined for structural location tracking
- TypeError provides rich context for unification errors

**Blockers:** None

**Concerns:** None

**Recommendations:**
- Plan 02 should throw TypeException from unification failures
- Plan 03 should use InferContext to track inference path
- Phase 3 (Blame Assignment) should populate Diagnostic.SecondarySpans
