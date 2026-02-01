---
phase: 02-substitution
plan: 01
subsystem: type-system
tags: [substitution, free-variables, hindley-milner]
dependency-graph:
  requires: [01-type-definition]
  provides: [substitution-operations, free-variable-tracking]
  affects: [03-unification, 04-inference]
tech-stack:
  added: []
  patterns: [recursive-apply, composition-semantics, bound-variable-protection]
key-files:
  created: []
  modified: [FunLang/Type.fs]
decisions:
  - id: 02-01-01
    context: "Transitive substitution handling"
    decision: "apply recursively calls itself when TVar maps to another type"
    rationale: "Handles chains like {0->TVar 1, 1->TInt} correctly"
  - id: 02-01-02
    context: "Compose semantics"
    decision: "compose s2 s1 = s2 after s1 (apply s2 to s1 values, merge s2 bindings)"
    rationale: "Standard function composition order for substitutions"
metrics:
  duration: 1 min
  completed: 2026-02-01
---

# Phase 2 Plan 1: Substitution Operations Summary

**One-liner:** Substitution (apply/compose) and free variable tracking (freeVars/freeVarsScheme/freeVarsEnv) for Hindley-Milner type inference foundation.

## What Was Built

### Substitution Operations
1. **empty** - Empty substitution (Map.empty)
2. **singleton** - Single variable substitution {v -> t}
3. **apply** - Apply substitution to type with recursive TVar handling for transitive chains
4. **compose** - Compose s2 after s1 with correct semantics
5. **applyScheme** - Apply to scheme, protecting bound variables
6. **applyEnv** - Apply to all schemes in environment

### Free Variable Operations
7. **freeVars** - Collect free type variables in a type (returns Set<int>)
8. **freeVarsScheme** - Free vars excluding bound variables
9. **freeVarsEnv** - Union of free vars across entire environment

## Key Implementation Details

### Transitive Substitution
```fsharp
| TVar n ->
    match Map.tryFind n s with
    | Some t -> apply s t  // Recursive - handles chains
    | None -> TVar n
```
This handles: `{0 -> TVar 1, 1 -> TInt}` applied to `TVar 0` yields `TInt`.

### Bound Variable Protection
```fsharp
let applyScheme (s: Subst) (Scheme (vars, ty)): Scheme =
    let s' = List.fold (fun acc v -> Map.remove v acc) s vars
    Scheme (vars, apply s' ty)
```
Removes bound vars from substitution before applying, preventing accidental capture.

## Verification Results

```
empty: map []
singleton: map [(0, TInt)]
apply transitive: TInt
compose: TInt
applyScheme (bound var protected): Scheme ([0], TArrow (TVar 0, TBool))
freeVars: set [0; 1]
freeVarsScheme: set [1]
```

All critical behaviors verified:
- Transitive chains resolved correctly
- Compose follows correct order
- Bound variables protected in schemes
- Free variable sets exclude bound vars

## Commits

| Hash | Type | Description |
|------|------|-------------|
| 476bb0b | feat | Implement substitution operations (empty, singleton, apply, compose, applyScheme, applyEnv) |
| 12097b1 | feat | Implement free variable functions (freeVars, freeVarsScheme, freeVarsEnv) |

## Deviations from Plan

None - plan executed exactly as written.

## Next Phase Readiness

**Phase 3 (Unification) prerequisites met:**
- [x] Substitution operations available
- [x] Free variable tracking available
- [x] Composition semantics verified
- [x] Bound variable protection verified

**Ready for:** Unification algorithm implementation (occurs check, unify function)
