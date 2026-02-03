---
phase: 02-type-expression-elaboration
plan: 01
subsystem: type-system
tags: [elaboration, type-conversion, bidirectional]
requires:
  - 01-parser-extensions (TypeExpr AST available)
provides:
  - elaborateTypeExpr function (converts TypeExpr to Type)
  - elaborateScoped function (shares type variable scope)
  - FunLang/Elaborate.fs module
affects:
  - 02-02 (will use Elaborate in bidirectional checker)
  - 02-03 (integration with inference)
tech-stack:
  added: []
  patterns:
    - "Type variable environment threading"
    - "Fresh counter for type variable indices"
key-files:
  created:
    - FunLang/Elaborate.fs
  modified:
    - FunLang/FunLang.fsproj
decisions:
  - decision: "Type variable indices start at 0 (separate from inference's 1000+)"
    rationale: "Avoids collision with inference-generated type variables"
    phase: "02"
    plan: "01"
  - decision: "Thread type variable environment through elaboration"
    rationale: "Ensures same 'a in scope maps to same TVar index"
    phase: "02"
    plan: "01"
metrics:
  duration: 5 minutes
  completed: 2026-02-03
---

# Phase 02 Plan 01: Type Expression Elaboration Summary

**One-liner:** Convert surface type syntax (TypeExpr) to internal type representation (Type) with scoped type variable tracking

## What Was Built

Created the Elaborate module that translates user-written type annotations into the internal Type representation used by the type checker. This is the bridge between parser output (TypeExpr) and type inference (Type).

### Core Components

**1. Type Variable Environment (Map<string, int>)**
- Maps type variable names ('a, 'b) to TVar indices
- Threaded through elaboration to ensure consistency
- Example: First 'a seen gets TVar 0, reused for all 'a in scope

**2. elaborateWithVars Function**
- Recursive elaboration with environment threading
- Handles all 7 TypeExpr variants:
  - TEInt → TInt
  - TEBool → TBool
  - TEString → TString
  - TEList t → TList (elaborate t)
  - TEArrow (t1, t2) → TArrow (elaborate t1, elaborate t2)
  - TETuple ts → TTuple (elaborate each)
  - TEVar name → TVar (lookup or fresh index)

**3. Public API Functions**
- `elaborateTypeExpr`: Fresh scope for single type
- `elaborateScoped`: Shared scope for multiple types (curried parameters)

### Implementation Details

**Fresh Type Variable Indices:**
```fsharp
let freshTypeVarIndex =
    let counter = ref 0
    fun () ->
        let n = !counter
        counter := n + 1
        n
```
- Start at 0 (vs inference's 1000+)
- Prevents collision between user-written and inferred type variables

**TEVar Handling (Critical):**
```fsharp
| TEVar name ->
    match Map.tryFind name vars with
    | Some idx -> (TVar idx, vars)        // Reuse existing
    | None ->
        let idx = freshTypeVarIndex()
        let vars' = Map.add name idx vars
        (TVar idx, vars')                 // Allocate fresh
```

**Environment Threading:**
- TETuple and TEArrow thread environment left-to-right
- Preserves order of type variable first appearance
- Example: `'a * 'b -> 'a` → TVar 0 * TVar 1 -> TVar 0

## Files Changed

### Created
- **FunLang/Elaborate.fs** (69 lines)
  - Module structure with Ast and Type dependencies
  - Type variable environment and fresh counter
  - elaborateWithVars recursive function
  - Public API (elaborateTypeExpr, elaborateScoped)

### Modified
- **FunLang/FunLang.fsproj**
  - Added Elaborate.fs after Type.fs in build order
  - Before Diagnostic.fs and Infer.fs
  - Correct dependency order maintained

## Verification Results

1. ✅ Project builds successfully (dotnet build)
2. ✅ All 378 existing tests pass (no regressions)
3. ✅ Elaborate module accessible from F# interactive
4. ✅ Functions exported: elaborateTypeExpr, elaborateScoped
5. ✅ Correct build order: Ast.fs → Type.fs → Elaborate.fs → Diagnostic.fs

## Decisions Made

**1. Separate index ranges for user vs inferred type variables**
- User-written type variables (from elaboration): 0-999
- Inference-generated type variables: 1000+
- **Rationale:** Prevents collision, easier debugging (can identify source)

**2. Environment threading pattern**
- Thread Map<string, int> through elaboration
- Accumulate bindings left-to-right
- **Rationale:** Ensures 'a in `fun (x: 'a) (y: 'a) -> ...` maps to same TVar

**3. Two public APIs (elaborateTypeExpr vs elaborateScoped)**
- elaborateTypeExpr: Fresh scope each call
- elaborateScoped: Shared scope across list
- **Rationale:** Supports both isolated annotations `(e : int)` and curried parameters `fun (x: 'a) (y: 'a) -> ...`

## Testing Strategy

Currently no dedicated tests for Elaborate module (not in plan scope). Verification through:
- Compilation success (type correctness)
- No test regressions (378 tests still pass)

**Future testing (Phase 2 Plan 2):**
- Unit tests for each TypeExpr variant
- Tests for type variable scoping
- Integration tests with bidirectional checker

## Deviations from Plan

None - plan executed exactly as written.

## Next Phase Readiness

**Blocks removed:**
- ✅ Type expression elaboration available for bidirectional checker
- ✅ Build order ready for Infer.fs to open Elaborate (future)

**Readiness for 02-02 (Bidirectional Type Checker):**
- ✅ elaborateTypeExpr ready for use in checking mode
- ✅ elaborateScoped ready for annotated lambda parameters
- ✅ No compilation errors or test failures

**Outstanding work:**
- Need to integrate Elaborate into bidirectional checker (02-02)
- Need to update Infer.fs to handle Annot/LambdaAnnot (02-03)

## Performance Considerations

- Fresh counter: O(1) per allocation
- Map lookup: O(log n) per type variable reference
- Overall elaboration: O(n) in type expression size
- No performance concerns for typical type annotations

## Commits

| Task | Commit | Files | Description |
|------|--------|-------|-------------|
| 1 | 52dbc1b | FunLang/Elaborate.fs | Create Elaborate module with elaborateTypeExpr and elaborateScoped |
| 2 | 72d26f6 | FunLang/FunLang.fsproj | Add Elaborate.fs to build order after Type.fs |

## Execution Time

- Start: 2026-02-03T10:50:45Z
- End: 2026-02-03T10:56:17Z
- Duration: 5 minutes 32 seconds

## Key Learnings

1. **Type variable environment threading is critical** - Without it, each 'a would get a fresh TVar index
2. **Separate index ranges prevent bugs** - Clear separation between user-written and inferred type variables
3. **Two APIs cover different use cases** - Single annotation vs shared scope for curried parameters
4. **Build order matters in F#** - Elaborate.fs must come after Type.fs but before modules that use it
