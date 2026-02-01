# Phase 4: Inference - Research

**Researched:** 2026-02-01
**Domain:** Hindley-Milner Type Inference (Algorithm W)
**Confidence:** HIGH

## Summary

This research covers Algorithm W implementation for complete type inference in FunLang. Algorithm W is the standard approach for Hindley-Milner type inference, providing principal type inference without annotations. The implementation builds on the existing Type.fs (types, substitution, free variables) and Unify.fs (unification, occurs check) modules from Phases 1-3.

The core mechanism involves three helper functions (freshVar, instantiate, generalize) and a recursive infer function that walks the AST generating type constraints. Key insight: substitutions must be threaded through recursive calls, and let-polymorphism requires careful handling of when to generalize vs instantiate.

**Primary recommendation:** Implement Algorithm W with explicit substitution threading (not constraint-based). Create Infer.fs with freshVar counter, instantiate, generalize, and a comprehensive infer function covering all 15 AST expression types plus patterns.

## Standard Stack

The established approach for this domain:

### Core Functions
| Function | Signature | Purpose | Why Standard |
|----------|-----------|---------|--------------|
| freshVar | `unit -> Type` | Generate unique type variables | Counter-based, monotonic, prevents collisions |
| instantiate | `Scheme -> Type` | Replace bound vars with fresh vars | Enables polymorphic reuse |
| generalize | `TypeEnv -> Type -> Scheme` | Abstract over free vars not in env | Creates polymorphic types at let boundaries |
| infer | `TypeEnv -> Expr -> Subst * Type` | Infer type for expression | Returns substitution + inferred type pair |

### Supporting
| Function | Signature | Purpose | When to Use |
|----------|-----------|---------|-------------|
| inferPattern | `Pattern -> TypeEnv * Type` | Infer pattern type and bindings | Match expressions, LetPat |
| typecheck | `Expr -> Type` | Entry point with initial env | CLI integration |

### Implementation Decisions (from prior phases)
| Decision | Value | Reason |
|----------|-------|--------|
| TVar representation | `int` | Simplifies substitution, set operations |
| Substitution threading | Explicit | Algorithm W style, not constraint generation |
| Compose order | `compose s2 s1` (s2 after s1) | Function composition semantics |
| Apply transitive | `apply s t` recursive | Handles TVar chains correctly |

## Architecture Patterns

### Module Structure
```
FunLang/
├── Type.fs          # Types, schemes, substitution, freeVars
├── Unify.fs         # TypeError, occurs, unify
└── Infer.fs         # freshVar, instantiate, generalize, infer (NEW)
```

### Pattern 1: Stateful Fresh Variable Counter
**What:** Mutable ref cell for generating unique type variable IDs
**When to use:** Called whenever a new unknown type is needed
**Example:**
```fsharp
// Source: praeclarum/AlgorithmW.fs, adapted to int-based TVar
let freshVar =
    let counter = ref 0
    fun () ->
        let n = !counter
        counter := n + 1
        TVar n
```
**Note:** Must reset counter between typecheck calls if needed for consistent output.

### Pattern 2: Instantiate with Fresh Variables
**What:** Replace all bound (forall) variables with fresh type variables
**When to use:** When looking up a polymorphic variable from environment
**Example:**
```fsharp
// Source: Bernstein "Damas-Hindley-Milner inference two ways"
let instantiate (Scheme (vars, ty)) =
    let freshVars = List.map (fun _ -> freshVar()) vars
    let subst = List.zip vars freshVars |> Map.ofList
    apply subst ty
```

### Pattern 3: Generalize at Let Boundaries
**What:** Abstract type over variables not free in environment
**When to use:** After inferring let-bound expression, before adding to env
**Example:**
```fsharp
// Source: Stephen Diehl "Write You a Haskell"
let generalize (env: TypeEnv) (ty: Type): Scheme =
    let envFreeVars = freeVarsEnv env
    let tyFreeVars = freeVars ty
    let quantifiedVars = Set.difference tyFreeVars envFreeVars |> Set.toList
    Scheme (quantifiedVars, ty)
```

### Pattern 4: Substitution Threading in Infer
**What:** Apply accumulated substitutions before recursive calls
**When to use:** After each unification, apply result to subsequent types/env
**Example:**
```fsharp
// Source: NEU CS4410 Type Inference lecture
// For If expression:
let s1, t1 = infer env e1        // condition
let s2, t2 = infer (applyEnv s1 env) e2  // then branch
let s3, t3 = infer (applyEnv (compose s2 s1) env) e3  // else branch
let s4 = unify (apply (compose s3 (compose s2 s1)) t1) TBool
let s5 = unify (apply s4 t2) (apply s4 t3)
// Compose all: s5 after s4 after s3 after s2 after s1
```

### Pattern 5: LetRec with Pre-bound Fresh Variable
**What:** Add function name to env with fresh var BEFORE inferring body
**When to use:** Recursive function definitions
**Example:**
```fsharp
// Source: Multiple (NEU, Bernstein, Cambridge lectures)
// LetRec: let rec f x = body in expr
let funcTy = freshVar()  // Fresh type for f
let paramTy = freshVar() // Fresh type for x
let bodyEnv = Map.add f (Scheme ([], funcTy)) (Map.add x (Scheme ([], paramTy)) env)
let s1, bodyTy = infer bodyEnv body
let s2 = unify (apply s1 funcTy) (TArrow (apply s1 paramTy, bodyTy))
// Now generalize and add to env for expr
```

### Anti-Patterns to Avoid
- **Forgetting to apply substitution before recursive infer:** Leads to lost type information
- **Wrong compose order:** `compose s1 s2` instead of `compose s2 s1` breaks transitivity
- **Generalizing lambda parameters:** Only let-bound expressions get polymorphism
- **Instantiating monomorphic schemes:** Works but wasteful (no-op)
- **Not threading env through substitutions:** Types become inconsistent

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Fresh variables | Random/hash-based | Counter with ref cell | Monotonic, debuggable, deterministic |
| Scheme application | Manual variable renaming | instantiate + apply | Fresh vars prevent capture |
| Polymorphism check | Scanning for TVar | generalize (env difference) | Correct handling of env constraints |
| Recursive types | Manual pre-binding | freshVar + unify pattern | Algorithm W standard technique |
| Pattern bindings | Inline in infer | Separate inferPattern | Reusable for Match, LetPat |

**Key insight:** Algorithm W is well-studied since 1978. Every "clever optimization" has likely been tried and has subtle bugs. Stick to the standard formulation.

## Common Pitfalls

### Pitfall 1: Substitution Not Applied Before Recursive Infer
**What goes wrong:** Type constraints from earlier expressions don't propagate
**Why it happens:** Forgetting `applyEnv s env` before next infer call
**How to avoid:** Always apply accumulated substitution to env: `infer (applyEnv s env) expr`
**Warning signs:** Polymorphic functions infer to concrete types incorrectly

### Pitfall 2: Let-Polymorphism Leaking to Lambda Parameters
**What goes wrong:** Lambda parameters become polymorphic (unsound)
**Why it happens:** Generalizing at wrong place (inside lambda instead of at let)
**How to avoid:** Only generalize in Let/LetRec/LetPat, never for lambda-bound vars
**Warning signs:** `fun x -> (x 1, x true)` typechecks when it shouldn't

### Pitfall 3: LetRec Without Pre-Binding
**What goes wrong:** Recursive calls have "unbound variable" error
**Why it happens:** Not adding function name to env before inferring body
**How to avoid:** Add `(name, Scheme([], freshVar()))` to env before body inference
**Warning signs:** TypeError on valid recursive functions

### Pitfall 4: Pattern Binding Creates Wrong Environment
**What goes wrong:** Pattern variables not in scope or wrong types
**Why it happens:** inferPattern returns incorrect bindings
**How to avoid:** inferPattern must return `TypeEnv * Type` with all bound vars
**Warning signs:** Match branches can't access pattern variables

### Pitfall 5: EmptyList Without Fresh Variable
**What goes wrong:** Empty list has concrete type like `'a list` with fixed 'a
**Why it happens:** Returning `TList (TVar constant)` instead of fresh
**How to avoid:** `let tv = freshVar() in (empty, TList tv)`
**Warning signs:** `[] : int list` or polymorphism breaks with empty lists

### Pitfall 6: Compose Order Inverted
**What goes wrong:** Substitution chains break, types become wrong
**Why it happens:** Writing `compose s1 s2` instead of `compose s2 s1`
**How to avoid:** Remember: compose semantics is "s2 after s1" (function composition)
**Warning signs:** Transitive type variable chains don't resolve

## Code Examples

Verified patterns from official sources:

### Fresh Variable Generator
```fsharp
// Source: praeclarum/AlgorithmW.fs, adapted
let freshVar =
    let counter = ref 0
    fun () ->
        let n = !counter
        counter := n + 1
        TVar n
```

### Instantiate
```fsharp
// Source: Bernstein, Stephen Diehl
let instantiate (Scheme (vars, ty)): Type =
    match vars with
    | [] -> ty  // Optimization: monomorphic scheme, no substitution needed
    | _ ->
        let freshVars = List.map (fun _ -> freshVar()) vars
        let subst = List.zip vars freshVars |> Map.ofList
        apply subst ty
```

### Generalize
```fsharp
// Source: Stephen Diehl "Write You a Haskell"
let generalize (env: TypeEnv) (ty: Type): Scheme =
    let envFree = freeVarsEnv env
    let tyFree = freeVars ty
    let vars = Set.difference tyFree envFree |> Set.toList
    Scheme (vars, ty)
```

### Infer Literal
```fsharp
// Source: NEU CS4410, Algorithm W standard
| Number _ -> (empty, TInt)
| Bool _ -> (empty, TBool)
| String _ -> (empty, TString)
```

### Infer Variable
```fsharp
// Source: Algorithm W standard
| Var name ->
    match Map.tryFind name env with
    | Some scheme -> (empty, instantiate scheme)
    | None -> raise (TypeError (sprintf "Unbound variable: %s" name))
```

### Infer Lambda
```fsharp
// Source: NEU CS4410, Stephen Diehl
| Lambda (param, body) ->
    let paramTy = freshVar()
    let bodyEnv = Map.add param (Scheme ([], paramTy)) env
    let s, bodyTy = infer bodyEnv body
    (s, TArrow (apply s paramTy, bodyTy))
```

### Infer Application
```fsharp
// Source: Algorithm W standard
| App (func, arg) ->
    let s1, funcTy = infer env func
    let s2, argTy = infer (applyEnv s1 env) arg
    let resultTy = freshVar()
    let s3 = unify (apply s2 funcTy) (TArrow (argTy, resultTy))
    (compose s3 (compose s2 s1), apply s3 resultTy)
```

### Infer Let (with polymorphism)
```fsharp
// Source: Algorithm W, NEU CS4410
| Let (name, value, body) ->
    let s1, valueTy = infer env value
    let env' = applyEnv s1 env
    let scheme = generalize env' (apply s1 valueTy)
    let bodyEnv = Map.add name scheme env'
    let s2, bodyTy = infer bodyEnv body
    (compose s2 s1, bodyTy)
```

### Infer LetRec
```fsharp
// Source: NEU CS4410, Cambridge lectures
| LetRec (name, param, body, expr) ->
    let funcTy = freshVar()
    let paramTy = freshVar()
    let recEnv = Map.add name (Scheme ([], funcTy)) env
    let bodyEnv = Map.add param (Scheme ([], paramTy)) recEnv
    let s1, bodyTy = infer bodyEnv body
    let s2 = unify (apply s1 funcTy) (TArrow (apply s1 paramTy, bodyTy))
    let s = compose s2 s1
    let env' = applyEnv s env
    let scheme = generalize env' (apply s funcTy)
    let exprEnv = Map.add name scheme env'
    let s3, exprTy = infer exprEnv expr
    (compose s3 s, exprTy)
```

### Infer If
```fsharp
// Source: NEU CS4410
| If (cond, thenExpr, elseExpr) ->
    let s1, condTy = infer env cond
    let s2, thenTy = infer (applyEnv s1 env) thenExpr
    let s3, elseTy = infer (applyEnv (compose s2 s1) env) elseExpr
    let s4 = unify (apply (compose s3 (compose s2 s1)) condTy) TBool
    let s5 = unify (apply s4 thenTy) (apply s4 elseTy)
    let finalSubst = compose s5 (compose s4 (compose s3 (compose s2 s1)))
    (finalSubst, apply s5 thenTy)
```

### Infer EmptyList
```fsharp
// Source: Parametric polymorphism standard
| EmptyList ->
    let elemTy = freshVar()
    (empty, TList elemTy)
```

### Infer List Literal
```fsharp
// Source: derived from Cons pattern
| List exprs ->
    match exprs with
    | [] -> let tv = freshVar() in (empty, TList tv)
    | first :: rest ->
        let s1, elemTy = infer env first
        let folder (s, ty) e =
            let s', eTy = infer (applyEnv s env) e
            let s'' = unify (apply s' ty) eTy
            (compose s'' (compose s' s), apply s'' eTy)
        let finalS, elemTy' = List.fold folder (s1, elemTy) rest
        (finalS, TList elemTy')
```

### Infer Cons
```fsharp
// Source: List type rules
| Cons (head, tail) ->
    let s1, headTy = infer env head
    let s2, tailTy = infer (applyEnv s1 env) tail
    let s3 = unify tailTy (TList (apply s2 headTy))
    (compose s3 (compose s2 s1), apply s3 tailTy)
```

### Infer Tuple
```fsharp
// Source: Product type rules
| Tuple exprs ->
    let folder (s, tys) e =
        let s', ty = infer (applyEnv s env) e
        (compose s' s, ty :: tys)
    let finalS, revTys = List.fold folder (empty, []) exprs
    (finalS, TTuple (List.rev revTys))
```

### Infer Match
```fsharp
// Source: Pattern matching type rules
| Match (scrutinee, clauses) ->
    let s1, scrutTy = infer env scrutinee
    let resultTy = freshVar()
    let folder s (pat, expr) =
        let patEnv, patTy = inferPattern pat
        let s' = unify (apply s scrutTy) patTy
        let clauseEnv = Map.fold (fun acc k v -> Map.add k v acc) (applyEnv s' (applyEnv s env)) patEnv
        let s'', exprTy = infer clauseEnv expr
        let s''' = unify (apply s'' resultTy) exprTy
        compose s''' (compose s'' (compose s' s))
    let finalS = List.fold folder s1 clauses
    (finalS, apply finalS resultTy)
```

### InferPattern
```fsharp
// Source: Pattern type inference standard
let rec inferPattern = function
    | VarPat name ->
        let ty = freshVar()
        (Map.ofList [(name, Scheme ([], ty))], ty)
    | WildcardPat ->
        (Map.empty, freshVar())
    | TuplePat pats ->
        let envTys = List.map inferPattern pats
        let env = envTys |> List.map fst |> List.fold (fun acc m -> Map.fold (fun a k v -> Map.add k v a) acc m) Map.empty
        let tys = envTys |> List.map snd
        (env, TTuple tys)
    | EmptyListPat ->
        (Map.empty, TList (freshVar()))
    | ConsPat (headPat, tailPat) ->
        let headEnv, headTy = inferPattern headPat
        let tailEnv, tailTy = inferPattern tailPat
        // tailTy should unify with TList headTy, but we return TList headTy
        // Unification happens in Match inference
        let env = Map.fold (fun acc k v -> Map.add k v acc) headEnv tailEnv
        (env, TList headTy)
    | ConstPat (IntConst _) -> (Map.empty, TInt)
    | ConstPat (BoolConst _) -> (Map.empty, TBool)
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Algorithm W (1978) | Still Algorithm W | Stable since 1982 | Proven complete, efficient in practice |
| Manual type annotations | Full inference | 1978 | No annotations required |
| Simple substitutions | Efficient ref-based | 1990s | Better performance on large programs |

**Deprecated/outdated:**
- None for basic HM inference. The algorithm is stable since Damas-Milner proof (1982).
- Higher-ranked types, GADTs, type classes extend HM but are out of scope for FunLang v4.0.

## Open Questions

Things that couldn't be fully resolved:

1. **Pattern unification in Match**
   - What we know: Each pattern gives a type, scrutinee must unify with all
   - What's unclear: Best order to unify (all at once vs fold)
   - Recommendation: Use fold, accumulate substitution, apply to subsequent patterns

2. **LetPat generalization**
   - What we know: Should behave like Let with polymorphism
   - What's unclear: When pattern has multiple bindings, which get generalized
   - Recommendation: Generalize the entire pattern type, all bindings get same level

3. **Binary operator type signatures**
   - What we know: Arithmetic ops are int -> int -> int, comparison ops return bool
   - What's unclear: Should overloading be supported (e.g., + for strings)
   - Recommendation: No overloading for v4.0; arithmetic = int only, string concat via separate function

## Sources

### Primary (HIGH confidence)
- [praeclarum/AlgorithmW.fs](https://gist.github.com/praeclarum/5fbef41ea9c296590f23) - F# reference implementation
- [NEU CS4410 Type Inference](https://course.ccs.neu.edu/cs4410sp19/lec_type-inference_notes.html) - Complete lecture notes with pseudocode
- [Stephen Diehl "Write You a Haskell"](https://smunix.github.io/dev.stephendiehl.com/fun/006_hindley_milner.html) - Haskell implementation guide

### Secondary (MEDIUM confidence)
- [Bernstein "Damas-Hindley-Milner inference two ways"](https://bernsteinbear.com/blog/type-inference/) - Algorithm J vs W comparison
- [7sharp9/write-you-an-inference-in-fsharp](https://github.com/7sharp9/write-you-an-inference-in-fsharp) - F# split-solver approach
- [Wikipedia Hindley-Milner](https://en.wikipedia.org/wiki/Hindley%E2%80%93Milner_type_system) - Theory overview

### Tertiary (LOW confidence)
- [Casper Andersson Type Inference](https://casan.se/blog/computer_science/type-inference-with-hindley-milner-w-algorithm/) - Blog walkthrough
- [Jeremy Mikkola Understanding Algorithm W](https://jeremymikkola.com/posts/2018_03_25_understanding_algorithm_w.html) - Conceptual explanation

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Algorithm W is well-documented with multiple implementations
- Architecture: HIGH - Module structure follows prior phases, infer pattern established
- Pitfalls: HIGH - Common mistakes well-documented in literature
- Code examples: MEDIUM - Adapted from multiple sources, need testing

**Research date:** 2026-02-01
**Valid until:** 90 days (algorithm stable since 1982)
