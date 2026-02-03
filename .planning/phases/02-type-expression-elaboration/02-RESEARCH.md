# Phase 2: Type Expression Elaboration - Research

**Researched:** 2026-02-03
**Domain:** Type System Elaboration (Surface Syntax to Internal Representation)
**Confidence:** HIGH

## Summary

Type expression elaboration is the process of converting user-written type syntax (TypeExpr) into the internal type representation (Type) used by the type checker. This is a standard compiler phase in ML-family languages, sitting between parsing and type checking.

For FunLang Phase 2, the task is straightforward: implement a recursive transformation from the 7 TypeExpr variants (TEInt, TEBool, TEString, TEList, TEArrow, TETuple, TEVar) to corresponding Type variants. The main complexity is **type variable scoping** - ensuring that user-written type variables like `'a` in `let f (x: 'a) : 'a = x` refer to the same internal type variable within a binding's scope.

The standard approach uses a **scoped elaboration environment** that maps type variable names to internal indices during elaboration. F# itself uses this pattern, as do OCaml and Standard ML implementations.

**Primary recommendation:** Create a new `Elaborate.fs` module between Type.fs and Infer.fs in the build order, implementing `elaborateTypeExpr` with a simple Map-based scoping environment.

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| F# Map | Built-in | Type variable scoping environment | Standard functional dictionary, immutable, perfect for scope tracking |
| Pattern matching | Built-in | Recursive elaboration logic | F# discriminated unions make transformation clean and exhaustive |

### Supporting
None needed - this is a pure transformation using standard library facilities.

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Map<string, int> | Dictionary<string, int> | Mutable dictionary is overkill; Map is idiomatic and safer |
| Separate module | Extend Type.fs | Separate module better for future bidirectional integration |
| Global counter | Context threading | Context threading is more flexible for multi-binding scopes |

**Installation:**
No external dependencies required.

## Architecture Patterns

### Recommended Project Structure
```
FunLang/
├── Ast.fs              # TypeExpr type (already exists from Phase 1)
├── Type.fs             # Type type (already exists)
├── Elaborate.fs        # NEW - elaborateTypeExpr function
├── Diagnostic.fs       # (existing)
├── Unify.fs            # (existing)
├── Infer.fs            # (existing - will use elaborateTypeExpr)
└── FunLang.fsproj      # Add Elaborate.fs after Type.fs
```

### Pattern 1: Simple Elaboration (No Scoping)
**What:** Direct structural transformation without variable tracking.
**When to use:** For testing Phase 2 in isolation before full scoping.
**Example:**
```fsharp
module Elaborate

open Ast
open Type

/// Convert TypeExpr to Type (simple version - no scoping)
let rec elaborateTypeExpr (te: TypeExpr): Type =
    match te with
    | TEInt -> TInt
    | TEBool -> TBool
    | TEString -> TString
    | TEList t -> TList (elaborateTypeExpr t)
    | TEArrow (t1, t2) -> TArrow (elaborateTypeExpr t1, elaborateTypeExpr t2)
    | TETuple ts -> TTuple (List.map elaborateTypeExpr ts)
    | TEVar name ->
        // Simple: map 'a -> 0, 'b -> 1, etc.
        let idx = int name.[1] - int 'a'
        TVar idx
```

### Pattern 2: Scoped Elaboration (Standard Approach)
**What:** Thread a scoping environment through elaboration to track type variable bindings.
**When to use:** Production implementation supporting polymorphic annotations like `(x: 'a) : 'a`.
**Example:**
```fsharp
module Elaborate

open Ast
open Type

/// Type variable scoping environment
/// Maps user type variable names to internal TVar indices
type TypeVarEnv = Map<string, int>

/// Generate fresh type variable index
let freshTypeVar =
    let counter = ref 0
    fun () ->
        let n = !counter
        counter := n + 1
        TVar n

/// Elaborate with scoping environment
let rec elaborateWithEnv (env: TypeVarEnv) (te: TypeExpr): Type * TypeVarEnv =
    match te with
    | TEInt -> (TInt, env)
    | TEBool -> (TBool, env)
    | TEString -> (TString, env)
    | TEList t ->
        let ty, env' = elaborateWithEnv env t
        (TList ty, env')
    | TEArrow (t1, t2) ->
        let ty1, env1 = elaborateWithEnv env t1
        let ty2, env2 = elaborateWithEnv env1 t2
        (TArrow (ty1, ty2), env2)
    | TETuple ts ->
        let tys, env' = List.fold (fun (acc, e) t ->
            let ty, e' = elaborateWithEnv e t
            (acc @ [ty], e')
        ) ([], env) ts
        (TTuple tys, env')
    | TEVar name ->
        match Map.tryFind name env with
        | Some (TVar idx) -> (TVar idx, env)  // Already bound
        | _ ->
            // First occurrence - create fresh binding
            let ty = freshTypeVar()
            (ty, Map.add name ty env)

/// Public API: elaborate single type expression with fresh scope
let elaborateTypeExpr (te: TypeExpr): Type =
    let ty, _ = elaborateWithEnv Map.empty te
    ty

/// Elaborate multiple type expressions in shared scope
/// Used for: fun (x: 'a) : 'a -> ... (both 'a refer to same var)
let elaborateTypeExprsScoped (tes: TypeExpr list): Type list =
    let rec loop env acc = function
        | [] -> List.rev acc
        | te :: rest ->
            let ty, env' = elaborateWithEnv env te
            loop env' (ty :: acc) rest
    loop Map.empty [] tes
```

### Pattern 3: Integration with Annotation Nodes
**What:** Call elaboration when handling Annot and LambdaAnnot expressions.
**When to use:** Phase 3/4 when bidirectional typing is implemented.
**Example:**
```fsharp
// In Infer.fs or future Bidir.fs
| Annot (e, tyExpr, span) ->
    let annotTy = Elaborate.elaborateTypeExpr tyExpr
    let s = check ctx env e annotTy
    (s, annotTy)

| LambdaAnnot (param, paramTyExpr, body, span) ->
    let paramTy = Elaborate.elaborateTypeExpr paramTyExpr
    let bodyEnv = Map.add param (Scheme ([], paramTy)) env
    let s, bodyTy = synth ctx bodyEnv body
    (s, TArrow (apply s paramTy, bodyTy))
```

### Anti-Patterns to Avoid
- **Hard-coding type variable indices:** Don't map 'a -> TVar 0 directly; use fresh var counter to avoid collision with inference-generated variables (which start at 1000 in Infer.fs).
- **Global scope for all annotations:** Each top-level binding should have independent type variable scope; don't carry env across let bindings.
- **Ignoring TEVar apostrophe:** TYPE_VAR lexeme includes apostrophe ('a), so name.[0] is `'` and name.[1] is `a`.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Type variable renaming | Alpha-conversion logic | Existing TVar indices | Internal Type already uses indices; no need to rename |
| Type pretty-printing with user names | Track 'a/'b names through inference | formatTypeNormalized in Type.fs | Existing formatter normalizes indices to letters |
| Scope tracking | Custom stack data structure | F# Map | Map is immutable, efficient, and idiomatic |

**Key insight:** FunLang's internal Type representation already uses integer indices for type variables. The elaboration phase only needs to assign consistent indices within a scope - no complex renaming or unification required at this stage.

## Common Pitfalls

### Pitfall 1: Type Variable Index Collision
**What goes wrong:** User annotation `'a` gets elaborated to TVar 0, but inference engine generates TVar 1000+, then unification creates substitution {0 -> TInt, 1000 -> Bool}, and the wrong type is applied.
**Why it happens:** Elaboration and inference use overlapping index ranges.
**How to avoid:** Use fresh var counter in elaboration that starts at same base as Infer.freshVar (1000+), OR use distinct range (e.g., elaboration uses 0-999, inference uses 1000+).
**Warning signs:** Tests pass individually but fail when annotations and inference mix.

### Pitfall 2: Broken Type Variable Scoping
**What goes wrong:** `fun (x: 'a) : 'a -> x` produces `'a -> 'b` (different variables) instead of `'a -> 'a`.
**Why it happens:** Each elaborateTypeExpr call creates independent scope instead of shared scope for the binding.
**How to avoid:** Use elaborateTypeExprsScoped for annotations within same binding, passing accumulated environment.
**Warning signs:** Error messages say "type 'b doesn't unify with 'a" for code that should trivially type check.

### Pitfall 3: TEVar Name Parsing
**What goes wrong:** Code tries `int name.[0] - int 'a'` and gets wrong index because name.[0] is apostrophe.
**Why it happens:** TYPE_VAR lexeme captures full "'a" string, not just "a".
**How to avoid:** Use `name.[1]` (second character) or strip apostrophe with `name.TrimStart('\''')`.
**Warning signs:** Index calculation produces garbage values or crashes with IndexOutOfRange.

### Pitfall 4: Build Order Violation
**What goes wrong:** Add Elaborate.fs but put it after Infer.fs in .fsproj, causing "module Elaborate not found".
**Why it happens:** F# requires strict dependency ordering; Infer.fs will call Elaborate.elaborateTypeExpr.
**How to avoid:** Insert Elaborate.fs compilation AFTER Type.fs but BEFORE Infer.fs in ItemGroup.
**Warning signs:** Build error "The namespace or module 'Elaborate' is not defined".

### Pitfall 5: Forgetting Recursive Cases
**What goes wrong:** Implement elaboration for primitives but forget TEList or TETuple contains nested TypeExpr.
**Why it happens:** Pattern matching incomplete or forgets to recursively elaborate children.
**How to avoid:** Use F# compiler warnings for incomplete patterns; test with nested types like `'a list list`.
**Warning signs:** Compiler warning FS0025 "Incomplete pattern matches"; runtime crashes on nested types.

## Code Examples

Verified patterns from FunLang codebase and ML-family language implementations:

### Basic Elaboration Function
```fsharp
// Source: .planning/research/bidirectional-typing.md Section 4.3
module Elaborate

open Ast
open Type

/// Convert surface type syntax to internal Type
let rec elaborateTypeExpr (te: TypeExpr): Type =
    match te with
    | TEInt -> TInt
    | TEBool -> TBool
    | TEString -> TString
    | TEArrow (t1, t2) -> TArrow (elaborateTypeExpr t1, elaborateTypeExpr t2)
    | TETuple ts -> TTuple (List.map elaborateTypeExpr ts)
    | TEList t -> TList (elaborateTypeExpr t)
    | TEVar name ->
        // Extract letter after apostrophe: 'a -> 'a', 'b -> 'b'
        let letter = name.[1]
        let idx = int letter - int 'a'  // 'a' -> 0, 'b' -> 1
        TVar idx
```

### Scoped Elaboration with Environment
```fsharp
// Source: .planning/research/bidirectional-typing.md Section 6.3
/// Type variable environment for scoping
type TypeVarEnv = Map<string, int>

/// Fresh type variable counter
let freshTypeVarIndex =
    let counter = ref 1000  // Same base as Infer.freshVar
    fun () ->
        let n = !counter
        counter := n + 1
        n

/// Elaborate with type variable environment
let rec elaborateWithVars (vars: TypeVarEnv) (te: TypeExpr): Type * TypeVarEnv =
    match te with
    | TEInt -> (TInt, vars)
    | TEBool -> (TBool, vars)
    | TEString -> (TString, vars)
    | TEList t ->
        let ty, vars' = elaborateWithVars vars t
        (TList ty, vars')
    | TEArrow (t1, t2) ->
        let ty1, vars1 = elaborateWithVars vars t1
        let ty2, vars2 = elaborateWithVars vars1 t2
        (TArrow (ty1, ty2), vars2)
    | TETuple ts ->
        let folder (accTypes, accVars) t =
            let ty, vars' = elaborateWithVars accVars t
            (accTypes @ [ty], vars')
        let tys, vars' = List.fold folder ([], vars) ts
        (TTuple tys, vars')
    | TEVar name ->
        match Map.tryFind name vars with
        | Some idx -> (TVar idx, vars)  // Already bound in scope
        | None ->
            let idx = freshTypeVarIndex()
            (TVar idx, Map.add name idx vars)

/// Elaborate list of types in shared scope (for multi-parameter annotations)
let elaborateScoped (tes: TypeExpr list): Type list =
    let rec loop env acc = function
        | [] -> List.rev acc
        | te :: rest ->
            let ty, env' = elaborateWithVars env te
            loop env' (ty :: acc) rest
    loop Map.empty [] tes
```

### Integration Example (Future Use)
```fsharp
// Usage in Infer.fs or Bidir.fs when handling annotations
| LambdaAnnot (param, paramTyExpr, body, span) ->
    // Annotated lambda: fun (x: T) -> e
    let paramTy = Elaborate.elaborateTypeExpr paramTyExpr
    let bodyEnv = Map.add param (Scheme ([], paramTy)) env
    let s, bodyTy = inferWithContext ctx bodyEnv body
    (s, TArrow (apply s paramTy, bodyTy))
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Parse types directly to internal representation | Parse to TypeExpr AST, elaborate separately | 1990s ML compilers | Better error messages, easier to extend type syntax |
| Global type variable numbering | Scoped elaboration with fresh vars | Standard ML '97 | Prevents variable capture, clearer scope semantics |
| String-based type variables | Integer indices | Hindley-Milner implementations | Efficient substitution, faster unification |

**Deprecated/outdated:**
- **Direct TVar construction in parser:** Modern compilers always go through elaboration phase for better modularity and error reporting.

## Open Questions

1. **Should elaboration share fresh var counter with inference?**
   - What we know: Infer.freshVar starts at 1000; elaboration could start at 0 or share counter.
   - What's unclear: Whether unified counter or separate ranges is better for FunLang.
   - Recommendation: Start with separate ranges (elab: 0-999, infer: 1000+), unify if needed later.

2. **Where should scoped elaboration be called from?**
   - What we know: Multi-parameter functions need shared scope: `fun (x: 'a) (y: 'a) -> ...`
   - What's unclear: Should parser desugar to nested LambdaAnnot, or should inference handle scoping?
   - Recommendation: Phase 1 already desugars to nested LambdaAnnot; elaborate each independently (scoping handled by outer context in future phases).

3. **Should TEVar validation happen during elaboration?**
   - What we know: TYPE_VAR lexeme ensures syntactic validity ('a format).
   - What's unclear: Should elaboration reject unused variables or invalid names?
   - Recommendation: No validation in Phase 2; keep elaboration pure. Phase 3+ can add checks if needed.

## Sources

### Primary (HIGH confidence)
- `.planning/research/bidirectional-typing.md` - FunLang milestone research (Sections 4.3, 6.3)
- [F# Compiler Guide - Overview](https://fsharp.github.io/fsharp-compiler-docs/overview.html) - Internal representations and elaboration phases
- [F# Language Specification - Types](https://fsharp.github.io/fslang-spec/types-and-type-constraints/) - Type syntax and semantics
- FunLang codebase - Ast.fs (TypeExpr), Type.fs (Type), Infer.fs (freshVar pattern)

### Secondary (MEDIUM confidence)
- [OCaml Type Inference](https://cs3110.github.io/textbook/chapters/interp/inference.html) - Type elaboration in ML-family languages
- [Lexically-scoped type variables](https://www.microsoft.com/en-us/research/wp-content/uploads/2016/02/scoped.pdf) - Type variable scoping research (Microsoft Research, 2004)
- [OCaml Manual - Types](https://ocaml.org/manual/5.4/types.html) - Type variable scoping rules
- [Efficient and Insightful Generalization](https://okmij.org/ftp/ML/generalization.html) - ML type generalization with levels

### Tertiary (LOW confidence)
- [SML Type Checking](https://www.smlnj.org/doc/Conversion/types.html) - Standard ML type system overview

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Standard F# features, no external dependencies
- Architecture: HIGH - Direct implementation based on existing FunLang patterns and ML tradition
- Pitfalls: HIGH - Based on known issues in type elaboration (index collision, scoping bugs)

**Research date:** 2026-02-03
**Valid until:** ~60 days (stable domain - type elaboration patterns haven't changed in decades)
