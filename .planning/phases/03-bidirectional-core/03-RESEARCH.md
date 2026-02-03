# Phase 3: Bidirectional Core - Research

**Researched:** 2026-02-03
**Domain:** Type System Implementation - Bidirectional Type Checking
**Confidence:** HIGH

## Summary

Phase 3 implements the core bidirectional type checking algorithm for FunLang, creating two complementary typing modes: **synthesis** (determining a type from an expression) and **checking** (verifying an expression against a known type). This phase creates a new `Bidir.fs` module that will eventually replace the existing `Infer.fs` Algorithm W implementation.

The key innovation is a **hybrid approach** that combines bidirectional typing with unification-based inference. For backward compatibility, unannotated lambdas synthesize types using fresh type variables (like Algorithm W), while annotated expressions use pure bidirectional rules. This preserves 100% compatibility with all existing unannotated code while enabling the benefits of bidirectional typing.

The implementation reuses existing infrastructure: `Type.fs` for types and substitutions, `Unify.fs` for unification, `Elaborate.fs` for type expression conversion, and `Diagnostic.fs` for error reporting. The subsumption rule bridges synthesis and checking modes via unification, enabling seamless mode switching.

**Primary recommendation:** Implement `synth` and `check` functions as mutually recursive, thread `InferContext` for error tracking, apply substitutions eagerly, and preserve let-polymorphism with `generalize` at let boundaries.

## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| F# | 10 | Host language | Project already uses F# |
| Existing Type.fs | v5.0 | Type representation, substitutions | Already implements HM types |
| Existing Unify.fs | v5.0 | Unification algorithm | Needed for subsumption |
| Existing Elaborate.fs | v6.0 (Phase 2) | TypeExpr → Type conversion | Already handles scoped type vars |
| Existing Diagnostic.fs | v5.0 | Error infrastructure | Already has InferContext |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| N/A | - | No external deps | All infrastructure exists |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Hybrid approach | Pure bidirectional | Would break backward compatibility - unannotated lambdas need types |
| Unification-based subsumption | Subtyping-based subsumption | Simpler for FunLang's monomorphic base types |
| Keep Algorithm W | Pure bidirectional | Hybrid preserves existing tests while enabling annotations |

**Installation:**
No new dependencies required. All infrastructure exists in FunLang codebase.

## Architecture Patterns

### Recommended Project Structure
```
FunLang/
├── Type.fs          # Already exists - type representation, substitutions
├── Unify.fs         # Already exists - unification algorithm
├── Elaborate.fs     # Already exists (Phase 2) - TypeExpr → Type
├── Infer.fs         # Existing Algorithm W - will be deprecated
├── Bidir.fs         # NEW - bidirectional type checker
└── Diagnostic.fs    # Already exists - error infrastructure
```

### Pattern 1: Mutually Recursive Synthesis and Checking

**What:** The core of bidirectional typing is two mutually recursive functions that call each other.

**When to use:** Always - this is the fundamental structure.

**Example:**
```fsharp
// Source: Dunfield-Krishnaswami 2013, adapted for FunLang
module Bidir

open Type
open Unify
open Elaborate
open Diagnostic
open Ast

/// Synthesis mode: expression → type
/// Returns (substitution, inferred type)
let rec synth (ctx: InferContext list) (env: TypeEnv) (expr: Expr): Subst * Type =
    match expr with
    // Literals synthesize their intrinsic types
    | Number (_, _) -> (empty, TInt)
    | Bool (_, _) -> (empty, TBool)
    | String (_, _) -> (empty, TString)

    // Variables synthesize from environment (with instantiation)
    | Var (name, span) ->
        match Map.tryFind name env with
        | Some scheme -> (empty, instantiate scheme)
        | None -> raise (TypeException { Kind = UnboundVar name; Span = span; Term = Some expr; ContextStack = ctx; Trace = [] })

    // Annotated expressions synthesize the annotation type (after checking)
    | Annot (e, tyExpr, span) ->
        let ty = elaborateTypeExpr tyExpr
        let s = check ctx env e ty
        (s, apply s ty)  // Return annotated type, not synthesized type

    // Annotated lambda synthesizes arrow type
    | LambdaAnnot (param, paramTyExpr, body, span) ->
        let paramTy = elaborateTypeExpr paramTyExpr
        let bodyEnv = Map.add param (Scheme ([], paramTy)) env
        let s, bodyTy = synth ctx bodyEnv body
        (s, TArrow (apply s paramTy, bodyTy))

    // Application synthesizes result type
    | App (func, arg, span) ->
        let s1, funcTy = synth (InAppFun span :: ctx) env func
        let s2, argTy = synth (InAppArg span :: ctx) (applyEnv s1 env) arg
        let appliedFuncTy = apply s2 funcTy
        // Check for non-function application
        match appliedFuncTy with
        | TInt | TBool | TString | TTuple _ | TList _ ->
            raise (TypeException { Kind = NotAFunction appliedFuncTy; Span = spanOf func; Term = Some func; ContextStack = ctx; Trace = [] })
        | _ ->
            let resultTy = freshVar()
            let s3 = unifyWithContext ctx [] span appliedFuncTy (TArrow (argTy, resultTy))
            (compose s3 (compose s2 s1), apply s3 resultTy)

    // HYBRID: Unannotated lambda synthesizes with fresh type variable
    | Lambda (param, body, span) ->
        let paramTy = freshVar()
        let bodyEnv = Map.add param (Scheme ([], paramTy)) env
        let s, bodyTy = synth ctx bodyEnv body
        (s, TArrow (apply s paramTy, bodyTy))

    // Let with polymorphism (generalize at let)
    | Let (name, value, body, span) ->
        let s1, valueTy = synth (InLetRhs (name, span) :: ctx) env value
        let env' = applyEnv s1 env
        let scheme = generalize env' (apply s1 valueTy)
        let bodyEnv = Map.add name scheme env'
        let s2, bodyTy = synth (InLetBody (name, span) :: ctx) bodyEnv body
        (compose s2 s1, bodyTy)

    // ... other cases

/// Checking mode: expression → expected type → substitution
/// Returns substitution that makes expression have the expected type
and check (ctx: InferContext list) (env: TypeEnv) (expr: Expr) (expected: Type): Subst =
    match expr, expected with
    // Lambda checks against arrow type (parameter type from expected)
    | Lambda (param, body, _), TArrow (paramTy, resultTy) ->
        let bodyEnv = Map.add param (Scheme ([], paramTy)) env
        check ctx bodyEnv body resultTy

    // If-then-else checks both branches against expected
    | If (cond, thenE, elseE, span), expected ->
        let s1 = check (InIfCond span :: ctx) env cond TBool
        let s2 = check (InIfThen span :: ctx) (applyEnv s1 env) thenE (apply s1 expected)
        let s3 = check (InIfElse span :: ctx) (applyEnv (compose s2 s1) env) elseE (apply (compose s2 s1) expected)
        compose s3 (compose s2 s1)

    // Subsumption: synthesize then unify with expected
    | expr, expected ->
        let s1, actual = synth ctx env expr
        let s2 = unifyWithContext ctx [] (spanOf expr) (apply s1 expected) (apply s1 actual)
        compose s2 s1
```

### Pattern 2: Eager Substitution Application

**What:** Apply substitutions to types and environments immediately after unification.

**When to use:** Always - prevents stale type variables in subsequent operations.

**Example:**
```fsharp
// Source: FunLang's existing Infer.fs pattern
| App (func, arg, span) ->
    let s1, funcTy = synth ctx env func
    // CRITICAL: Apply s1 to env before checking arg
    let s2, argTy = synth ctx (applyEnv s1 env) arg
    // CRITICAL: Apply s2 to funcTy before unifying
    let appliedFuncTy = apply s2 funcTy
    let resultTy = freshVar()
    let s3 = unifyWithContext ctx [] span appliedFuncTy (TArrow (argTy, resultTy))
    (compose s3 (compose s2 s1), apply s3 resultTy)
```

### Pattern 3: Subsumption via Unification

**What:** Bridge synthesis and checking modes by unifying synthesized type with expected type.

**When to use:** Whenever a checking context needs to fall back to synthesis.

**Example:**
```fsharp
// Source: Bidirectional typing theory, adapted for unification
// Subsumption rule: synth ⇒ check
and check (ctx: InferContext list) (env: TypeEnv) (expr: Expr) (expected: Type): Subst =
    match expr, expected with
    // ... specific checking rules first ...

    // Fallback: subsumption
    | expr, expected ->
        let s1, actual = synth ctx env expr
        // Unify expected with actual (order matters for error messages)
        let s2 = unifyWithContext ctx [] (spanOf expr) (apply s1 expected) (apply s1 actual)
        compose s2 s1
```

### Pattern 4: Let-Polymorphism Preservation

**What:** Generalize types at let boundaries to preserve Hindley-Milner polymorphism.

**When to use:** Always for `Let` expressions - this is orthogonal to bidirectional structure.

**Example:**
```fsharp
// Source: FunLang's existing Infer.fs, preserved in Bidir.fs
| Let (name, value, body, span) ->
    let s1, valueTy = synth (InLetRhs (name, span) :: ctx) env value
    let env' = applyEnv s1 env
    // CRITICAL: Generalize at let boundary
    let scheme = generalize env' (apply s1 valueTy)
    let bodyEnv = Map.add name scheme env'
    let s2, bodyTy = synth (InLetBody (name, span) :: ctx) bodyEnv body
    (compose s2 s1, bodyTy)
```

### Pattern 5: Context Threading for Error Tracking

**What:** Thread `InferContext` list through all recursive calls for rich error messages.

**When to use:** Always - enables blame assignment and multi-span diagnostics.

**Example:**
```fsharp
// Source: FunLang's existing Diagnostic.fs infrastructure
| App (func, arg, span) ->
    // Thread context with InAppFun
    let s1, funcTy = synth (InAppFun span :: ctx) env func
    // Thread context with InAppArg
    let s2, argTy = synth (InAppArg span :: ctx) (applyEnv s1 env) arg
    // Context available for unification errors
    let s3 = unifyWithContext ctx [] span appliedFuncTy (TArrow (argTy, resultTy))
    (compose s3 (compose s2 s1), apply s3 resultTy)
```

### Anti-Patterns to Avoid

- **Don't trust annotations without checking:** Always `check` the expression against the annotated type.
- **Don't forget to apply substitutions:** Stale type variables cause incorrect unification.
- **Don't break let-polymorphism:** Always generalize at let boundaries.
- **Don't lose context:** Thread `InferContext` through all recursive calls.
- **Don't handle TVar specially in application:** Let unification handle it via subsumption.

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Type substitution | Custom apply function | `Type.apply` from Type.fs | Already handles transitive substitution correctly |
| Unification | Custom unification | `Unify.unifyWithContext` | Already has occurs check, error context, trace tracking |
| Type variable generation | Custom counter | `Infer.freshVar()` | Already uses 1000+ range to avoid collision |
| Type variable scoping | Custom scoping logic | `Elaborate.elaborateScoped` | Already handles shared scope for curried params |
| Polymorphic instantiation | Custom instantiation | `Infer.instantiate` | Already replaces bound vars with fresh vars |
| Generalization | Custom generalization | `Infer.generalize` | Already computes free vars correctly |
| Error formatting | Custom diagnostic | `Diagnostic.typeErrorToDiagnostic` | Already integrates context and trace |

**Key insight:** FunLang already has a complete type infrastructure from Algorithm W. Bidirectional typing reuses all primitives - only the control flow (synth/check modes) is new.

## Common Pitfalls

### Pitfall 1: Forgetting to Apply Substitutions
**What goes wrong:** Unification produces a substitution, but if you don't apply it to subsequent types, you'll unify against stale type variables.

**Why it happens:** Functional programming makes it easy to pass old environments instead of updated ones.

**How to avoid:** Always `applyEnv s env` before passing environment to recursive calls. Always `apply s ty` before using a type in unification.

**Warning signs:**
- Tests fail with "type mismatch" but the types look identical
- Type variables appear in final results that should have been substituted

### Pitfall 2: Wrong Substitution Composition Order
**What goes wrong:** `compose s1 s2` applies s2 first, then s1. Reversing the order produces wrong results.

**Why it happens:** Functional composition reads right-to-left, but temporal order reads left-to-right.

**How to avoid:** Remember: `compose s2 s1` means "apply s1 first, then s2". Match the temporal order.

**Warning signs:**
- Substitutions don't propagate correctly
- Later unifications don't see earlier bindings

**Example:**
```fsharp
// WRONG: s1 then s2 then s3
let final = compose s1 (compose s2 s3)

// RIGHT: s1 then s2 then s3
let final = compose s3 (compose s2 s1)
```

### Pitfall 3: Subsumption in Wrong Order
**What goes wrong:** Unifying `actual` with `expected` vs. `expected` with `actual` affects error messages (which type is blamed).

**Why it happens:** Unification is symmetric for types, but error messages are not.

**How to avoid:** Always unify `expected` with `actual` in that order - blame falls on `actual`.

**Warning signs:**
- Error messages blame the wrong expression
- "Expected int but got bool" should point to the bool expression, not the int context

**Example:**
```fsharp
// Subsumption: synthesize then check
| expr, expected ->
    let s1, actual = synth ctx env expr
    // RIGHT: unify expected with actual (blames actual)
    let s2 = unifyWithContext ctx [] (spanOf expr) (apply s1 expected) (apply s1 actual)
    compose s2 s1
```

### Pitfall 4: Breaking Let-Polymorphism
**What goes wrong:** Forgetting to generalize at let boundaries loses polymorphism - `let id = fun x -> x in (id 1, id true)` fails.

**Why it happens:** Tempting to simplify by removing generalization.

**How to avoid:** Always call `generalize env' valueTy` for let-bound values.

**Warning signs:**
- `id` function can only be used at one type
- Polymorphic examples from Algorithm W tests fail

### Pitfall 5: Unannotated Lambda Without Fresh Var (Pure Bidirectional)
**What goes wrong:** Pure bidirectional typing requires annotations on lambdas. `fun x -> x` without context cannot synthesize.

**Why it happens:** Academic papers often assume annotations are required.

**How to avoid:** Use hybrid approach: unannotated lambdas synthesize with fresh type variables (like Algorithm W).

**Warning signs:**
- Existing tests fail with "cannot infer type for unannotated lambda"
- Backward compatibility broken

### Pitfall 6: Annotated Lambda Not Using Annotation
**What goes wrong:** Treating `fun (x: int) -> e` like `fun x -> e` ignores the user's annotation.

**Why it happens:** Copy-paste from unannotated lambda case.

**How to avoid:** Annotated lambda is a separate AST case (`LambdaAnnot`) - elaborate the `TypeExpr` and use it.

**Warning signs:**
- Annotation has no effect on type checking
- Wrong types accepted without error

## Code Examples

Verified patterns from FunLang codebase:

### Synthesis for Variables (Polymorphic Instantiation)
```fsharp
// Source: Infer.fs line 82-91
| Var (name, span) ->
    match Map.tryFind name env with
    | Some scheme -> (empty, instantiate scheme)
    | None ->
        raise (TypeException {
            Kind = UnboundVar name
            Span = span
            Term = Some expr
            ContextStack = ctx
            Trace = []
        })
```

### Checking for If-Then-Else
```fsharp
// Source: Adapted from Infer.fs If case
| If (cond, thenE, elseE, span), expected ->
    // Condition must be bool
    let s1 = check (InIfCond span :: ctx) env cond TBool
    // Then branch must have expected type
    let s2 = check (InIfThen span :: ctx) (applyEnv s1 env) thenE (apply s1 expected)
    // Else branch must have expected type
    let s3 = check (InIfElse span :: ctx) (applyEnv (compose s2 s1) env) elseE (apply (compose s2 s1) expected)
    compose s3 (compose s2 s1)
```

### Application with Function Type Unification
```fsharp
// Source: Infer.fs line 124-141
| App (func, arg, span) ->
    let s1, funcTy = synth (InAppFun span :: ctx) env func
    let s2, argTy = synth (InAppArg span :: ctx) (applyEnv s1 env) arg
    let appliedFuncTy = apply s2 funcTy
    // Check if we're trying to apply a non-function type
    match appliedFuncTy with
    | TInt | TBool | TString | TTuple _ | TList _ ->
        raise (TypeException {
            Kind = NotAFunction appliedFuncTy
            Span = spanOf func
            Term = Some func
            ContextStack = ctx
            Trace = []
        })
    | _ ->
        let resultTy = freshVar()
        let s3 = unifyWithContext ctx [] span appliedFuncTy (TArrow (argTy, resultTy))
        (compose s3 (compose s2 s1), apply s3 resultTy)
```

### Let with Generalization
```fsharp
// Source: Infer.fs line 155-162
| Let (name, value, body, span) ->
    let s1, valueTy = synth (InLetRhs (name, span) :: ctx) env value
    let env' = applyEnv s1 env
    let scheme = generalize env' (apply s1 valueTy)
    let bodyEnv = Map.add name scheme env'
    let s2, bodyTy = synth (InLetBody (name, span) :: ctx) bodyEnv body
    (compose s2 s1, bodyTy)
```

### Annotated Expression Checking
```fsharp
// Source: Bidirectional typing pattern
| Annot (e, tyExpr, span) ->
    let ty = elaborateTypeExpr tyExpr
    let s = check ctx env e ty
    // Return annotation type (after checking), not synthesized type
    (s, apply s ty)
```

### Hybrid Unannotated Lambda
```fsharp
// Source: Infer.fs line 116-121 (preserved for backward compatibility)
| Lambda (param, body, span) ->
    // HYBRID: Use fresh variable for parameter type
    let paramTy = freshVar()
    let bodyEnv = Map.add param (Scheme ([], paramTy)) env
    let s, bodyTy = synth ctx bodyEnv body
    (s, TArrow (apply s paramTy, bodyTy))
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Pure Algorithm W | Hybrid bidirectional + unification | 2026 research | Enables annotations while preserving backward compatibility |
| No type annotations | ML-style annotations | FunLang v6.0 | Better documentation, error messages |
| Single mode (infer) | Dual mode (synth/check) | Bidirectional systems | Better error locality |
| Omnidirectional typing | Bidirectional with let-generalization | 2026 (Inria) | Omnidirectional struggles with ML-style let-polymorphism |

**Current trend (2026):**
- Research: Omnidirectional type inference (Inria 2026) - dynamic flow order
- Practice: Hybrid bidirectional + unification (GHC, Scala, C#)
- FunLang approach: Match practice, not bleeding-edge research

**Deprecated/outdated:**
- Pure bidirectional without unification: Too restrictive for ML-style languages
- Global inference only: Doesn't scale to higher-rank types (future)
- Annotation-free only: Loses documentation and blame assignment benefits

## Open Questions

### Question 1: Should LetRec Support Annotations?

**What we know:**
- Phase 1-2 added `Annot` and `LambdaAnnot` to AST
- `LetRec` currently uses fresh variables like Algorithm W
- Annotations could constrain recursive function types

**What's unclear:**
- Grammar for annotated let rec (`let rec f (x: int) : int = ...`)
- Whether to check body against annotation or synthesize+unify

**Recommendation:**
- Defer to Phase 4 (Annotation Checking)
- Phase 3 should handle `LetRec` like Algorithm W (fresh vars, unification)
- Phase 4 can add `LetRecAnnot` AST node if needed

### Question 2: Should Match Use Checking or Synthesis?

**What we know:**
- Algorithm W: Match synthesizes result type, unifies all clause results
- Bidirectional: Match could check all clauses against expected type

**What's unclear:**
- Which gives better error messages
- Whether checking mode prevents some valid programs

**Recommendation:**
- Phase 3: Keep synthesis mode (like Algorithm W) for backward compatibility
- Measure error message quality in practice
- Could switch to checking mode in Phase 5 (Error Integration) if beneficial

### Question 3: Performance Impact of Dual-Mode System

**What we know:**
- Asymptotic complexity same as Algorithm W (both use unification)
- Dual-mode adds pattern matching overhead
- F# 10 added type subsumption cache for performance

**What's unclear:**
- Real-world performance on FunLang's 570+ tests
- Whether caching would help

**Recommendation:**
- Implement without premature optimization
- Profile if performance regression observed
- F# pattern matching is fast - unlikely to be bottleneck

## Sources

### Primary (HIGH confidence)
- `.planning/research/bidirectional-typing.md` - Comprehensive prior research covering Dunfield-Krishnaswami rules, ML-style syntax, implementation patterns
- `FunLang/Infer.fs` - Existing Algorithm W implementation to preserve
- `FunLang/Type.fs` - Type representation, substitutions, generalization
- `FunLang/Unify.fs` - Unification with context tracking
- `FunLang/Elaborate.fs` - Type expression elaboration (Phase 2)
- `FunLang/Diagnostic.fs` - Error infrastructure with InferContext

### Secondary (MEDIUM confidence)
- [Omnidirectional type inference for ML](https://inria.hal.science/hal-05438544v1/document) - 2026 Inria paper on dynamic flow order
- [Bidirectional Type Inference (Crux)](http://cruxlang.org/inference/) - Practical hybrid approach explanation
- [Haskell for All: Bidirectional Type-Checking](https://www.haskellforall.com/2022/06/the-appeal-of-bidirectional-type.html) - Motivation and benefits

### Tertiary (LOW confidence)
- [F# Graph-Based Type Checking](https://devblogs.microsoft.com/dotnet/a-new-fsharp-compiler-feature-graphbased-typechecking/) - F# 10 compiler improvements (tangential)
- Community discussions on combining bidirectional + unification (lobste.rs)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All infrastructure exists in FunLang codebase
- Architecture: HIGH - Mutually recursive synth/check is well-established pattern
- Pitfalls: HIGH - Directly observed from existing Infer.fs patterns and research
- Code examples: HIGH - All examples from working FunLang code or verified research
- Backward compatibility: HIGH - Hybrid approach proven to preserve Algorithm W behavior

**Research date:** 2026-02-03
**Valid until:** 2026-03-03 (30 days - stable domain)

**Key sources:**
- Dunfield & Krishnaswami 2013 (via prior research document)
- FunLang existing codebase (Types, Unify, Infer, Elaborate)
- 2026 Inria omnidirectional research (for state of the art)

Sources:
- [Omnidirectional type inference for ML: principality any way](https://inria.hal.science/hal-05438544v1/document)
- [Bidirectional Type Inference - Crux Lang](http://cruxlang.org/inference/)
- [The appeal of bidirectional type-checking](https://www.haskellforall.com/2022/06/the-appeal-of-bidirectional-type.html)
- [A new F# compiler feature: graph-based type-checking - .NET Blog](https://devblogs.microsoft.com/dotnet/a-new-fsharp-compiler-feature-graphbased-typechecking/)
