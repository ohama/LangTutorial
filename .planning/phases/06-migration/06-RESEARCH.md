# Phase 6: Migration - Research

**Researched:** 2026-02-04
**Domain:** Code migration / type system transition / documentation
**Confidence:** HIGH

## Summary

Phase 6 is the final phase of the v6.0 Bidirectional Type System milestone. The research examines the current state of the codebase and identifies what remains to complete the migration from Algorithm W to bidirectional type checking.

The key finding is that **most of the migration work is already complete**. TypeCheck.fs already uses `Bidir.synthTop` instead of `Infer.infer`, which means CLI and most integrations are already using the bidirectional system. The REPL does not perform type checking (it only evaluates), so no changes are needed there. The remaining work is verification, cleanup, and documentation.

The requirements break down as:
- **MIG-01** (Test verification): Run all tests to confirm Bidir handles everything Algorithm W did
- **MIG-02** (CLI/REPL transition): Already partially done - verify and document the completed transition
- **MIG-03** (Tutorial chapter): Write a new chapter explaining bidirectional type checking and type annotations

**Primary recommendation:** Verify test coverage, clean up unused Infer code paths, and write the tutorial chapter documenting the bidirectional type system.

## Standard Stack

### Core

This phase involves no new libraries - it is primarily verification and documentation.

| Component | Current State | Purpose | Action Needed |
|-----------|--------------|---------|---------------|
| TypeCheck.fs | Uses `Bidir.synthTop` | Type checking entry point | Verify it covers all cases |
| Infer.fs | Provides helpers (freshVar, instantiate, generalize, inferPattern) | Reused by Bidir | Keep - not deprecated |
| Bidir.fs | Full implementation | Bidirectional type checker | Verify test coverage |

### File Dependencies

```
Bidir.fs
  └── imports Infer (freshVar, instantiate, generalize, inferPattern)
  └── imports Elaborate (elaborateTypeExpr)
  └── imports Unify (unifyWithContext)
  └── imports Diagnostic (TypeError, InferContext)

TypeCheck.fs
  └── imports Bidir (synthTop)
  └── imports Infer (unused now - only for historical reference)
```

### What Gets Deprecated

| Function | Module | Replacement | Notes |
|----------|--------|-------------|-------|
| `infer` | Infer.fs | `Bidir.synth` | Keep for reference, mark as internal |
| `inferWithContext` | Infer.fs | `Bidir.synth` | Keep for reference, mark as internal |
| Direct `Infer.infer` calls | Various | `Bidir.synthTop` | Already migrated in TypeCheck.fs |

**Important:** The Infer module MUST NOT be fully deprecated because:
- `freshVar`, `instantiate`, `generalize` are used by Bidir.fs
- `inferPattern` is used by Bidir.fs for pattern matching
- Only the main `infer`/`inferWithContext` functions are superseded

## Architecture Patterns

### Current Integration Pattern (Already Implemented)

```fsharp
// TypeCheck.fs - CURRENT STATE
module TypeCheck

open Type
open Unify
open Infer  // For initialTypeEnv definition (uses Scheme)
open Bidir  // For synthTop
open Ast
open Diagnostic

let typecheck (expr: Expr): Result<Type, string> =
    try
        let ty = synthTop initialTypeEnv expr  // Using Bidir!
        Ok(ty)
    with
    | TypeException err ->
        let diag = typeErrorToDiagnostic err
        Error(diag.Message)
```

### REPL Pattern (No Type Checking)

```fsharp
// Repl.fs - CURRENT STATE
let rec private replLoop (env: Env) : unit =
    // ...
    let ast = parse line
    let result = eval env ast  // Direct evaluation, no typecheck
    printfn "%s" (formatValue result)
```

The REPL intentionally skips type checking for interactive exploration. This is a design choice, not a bug.

### Pattern: Optional Type Checking in REPL

If adding type checking to REPL is desired (future enhancement, not MIG-02):

```fsharp
// Possible future pattern - NOT required for MIG-02
let rec private replLoop (env: Env) (typeEnv: TypeEnv) : unit =
    let ast = parse line
    match typecheckWithDiagnostic ast with
    | Error diag ->
        eprintfn "%s" (formatDiagnostic diag)
        replLoop env typeEnv
    | Ok _ ->
        let result = eval env ast
        printfn "%s" (formatValue result)
        replLoop env typeEnv
```

### Anti-Patterns to Avoid

- **Removing Infer.fs entirely:** Would break Bidir which depends on its helpers
- **Duplicating helper functions:** Keep single source in Infer.fs
- **Adding type checking to REPL without user request:** Current behavior is intentional

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Test verification | Manual test runs | `make -C tests && dotnet run --project FunLang.Tests` | Existing test infrastructure is comprehensive |
| Type formatting | Custom formatters | `Type.formatTypeNormalized` | Already handles all edge cases |
| Error messages | Ad-hoc strings | `Diagnostic.typeErrorToDiagnostic` | Consistent, localized format |

**Key insight:** All infrastructure for bidirectional type checking is already in place. The migration is verification and documentation, not new implementation.

## Common Pitfalls

### Pitfall 1: Thinking REPL needs migration
**What goes wrong:** Assuming MIG-02 requires adding type checking to REPL
**Why it happens:** MIG-02 says "Infer -> Bidir transition (CLI, REPL)"
**How to avoid:** Check current code - REPL never used Infer for type checking
**Warning signs:** Modifying Repl.fs when it already works correctly

### Pitfall 2: Removing Infer module entirely
**What goes wrong:** Breaking Bidir.fs which imports Infer helpers
**Why it happens:** Misunderstanding "deprecate old Infer module"
**How to avoid:** Only deprecate the `infer`/`inferWithContext` entry points, not the module
**Warning signs:** Compilation errors in Bidir.fs after modifying Infer.fs

### Pitfall 3: Incomplete test verification
**What goes wrong:** Missing edge cases where Bidir differs from Algorithm W
**Why it happens:** Not running full test suite
**How to avoid:** Run both fslit (66 tests) AND Expecto (419 tests)
**Warning signs:** Tests passing individually but failing in CI

### Pitfall 4: Outdated tutorial chapter references
**What goes wrong:** New chapter references chapter-10's Algorithm W as current
**Why it happens:** Not updating cross-references
**How to avoid:** Update chapter-10 to note bidirectional approach, link to new chapter
**Warning signs:** Tutorial chapters contradict each other

## Code Examples

### Verifying Test Coverage

```bash
# Full test suite verification
make -C tests && dotnet run --project FunLang.Tests -- --summary

# Expected output:
# fslit: 66 tests passed
# Expecto: 419 tests passed
```

### Tutorial Chapter Structure (MIG-03)

Based on existing chapters (10, 11), the new chapter should follow this pattern:

```markdown
# Chapter 12: Bidirectional Type Checking

이 장에서는 FunLang의 타입 시스템을 **양방향 타입 체킹**으로 확장하여
타입 어노테이션을 지원한다.

## 개요

양방향 타입 체킹은 다음 기능을 제공한다:

- **Synthesis (합성)**: 표현식에서 타입을 추론
- **Checking (검사)**: 예상 타입에 대해 표현식 검증
- **타입 어노테이션**: `(e : T)`, `fun (x: int) -> e`

## 기존 Algorithm W와의 차이

[comparison table]

## 구현

[code walkthrough]

## 예제

[usage examples with annotations]
```

### Deprecation Documentation Pattern

```fsharp
// In Infer.fs, add comment:
/// DEPRECATED: Use Bidir.synth instead for new code.
/// This function is kept for reference and backward compatibility.
/// Bidir uses freshVar, instantiate, generalize from this module.
let rec infer (env: TypeEnv) (expr: Expr): Subst * Type =
    inferWithContext [] env expr
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Algorithm W (Infer.infer) | Bidirectional (Bidir.synthTop) | v6.0 Phase 4 | TypeCheck.fs updated |
| No type annotations | ML-style annotations | v6.0 Phase 1 | Parser extended |
| Generic error messages | Mode-aware diagnostics | v6.0 Phase 5 | InCheckMode context |

**Already completed:**
- TypeCheck.fs uses Bidir.synthTop (Phase 4)
- Type annotations parse correctly (Phase 1)
- Error messages include annotation context (Phase 5)

## Open Questions

### 1. Should REPL support type checking?

- What we know: Current REPL does evaluation only, no type checking
- What's unclear: Whether MIG-02 requires adding `:type` command
- Recommendation: Interpret MIG-02 as "verify CLI uses Bidir" - REPL type checking is out of scope for v6.0

### 2. How much of Infer.fs to deprecate?

- What we know: `freshVar`, `instantiate`, `generalize`, `inferPattern` are used by Bidir
- What's unclear: Whether to keep `infer`/`inferWithContext` or remove them
- Recommendation: Keep all code, add deprecation comments to `infer`/`inferWithContext`

### 3. Should chapter-10 be updated?

- What we know: Chapter-10 documents Algorithm W
- What's unclear: Whether to rewrite or just add forward reference
- Recommendation: Add note at end of chapter-10 pointing to new chapter-12

## Sources

### Primary (HIGH confidence)

- **TypeCheck.fs analysis:** Direct code inspection shows `synthTop` usage
- **Bidir.fs analysis:** Confirms dependency on Infer helpers
- **Repl.fs analysis:** No type checking, only evaluation
- **Test suite:** 419 Expecto + 66 fslit tests all passing

### Secondary (MEDIUM confidence)

- **ROADMAP.md requirements:** MIG-01, MIG-02, MIG-03 definitions
- **STATE.md decisions:** Prior phase decisions affecting migration

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Direct code analysis, clear file structure
- Architecture: HIGH - Current patterns visible in TypeCheck.fs, Bidir.fs
- Pitfalls: HIGH - Based on actual code dependencies and prior phase decisions

**Research date:** 2026-02-04
**Valid until:** 2026-03-04 (stable - internal codebase, no external dependencies)

## Migration Verification Checklist

For planner reference, the verification should cover:

### Test Categories (all should pass with Bidir)

| Category | Test Count | Directory |
|----------|------------|-----------|
| Type inference | 27 | tests/type-inference/ |
| Type errors | 15 | tests/type-errors/ |
| Functions | - | tests/functions/ |
| Pattern matching | - | tests/pattern-matching/ |
| Lists | - | tests/lists/ |
| Tuples | - | tests/tuples/ |
| Prelude | - | tests/prelude/ |

### CLI Modes to Verify

| Mode | Command | Type Checking |
|------|---------|---------------|
| Expression eval | `--expr` | Uses typecheckWithDiagnostic |
| File eval | `<file>` | Uses typecheckWithDiagnostic |
| Emit type | `--emit-type` | Uses typecheckWithDiagnostic |
| Emit AST | `--emit-ast` | No type checking |
| Emit tokens | `--emit-tokens` | No type checking |
| REPL | `--repl` | No type checking (intentional) |

### Documentation Artifacts

| Artifact | Location | Purpose |
|----------|----------|---------|
| New tutorial chapter | tutorial/chapter-12-*.md | Document bidirectional system |
| Chapter-10 update | tutorial/chapter-10-type-system.md | Add forward reference |
| Infer.fs comments | FunLang/Infer.fs | Mark deprecated functions |
