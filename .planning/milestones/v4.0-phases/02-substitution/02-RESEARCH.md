# Phase 2: Substitution - Research

**Researched:** 2026-02-01
**Domain:** Hindley-Milner Type System Substitution Operations
**Confidence:** HIGH

## Summary

Substitution is the foundational operation in Hindley-Milner type inference. It manages type variable replacement through three core operations: apply (substituting variables in types), compose (chaining substitutions), and freeVars (tracking unbound variables). These operations must respect polymorphic type schemes, where bound (quantified) variables should never be substituted, only free variables.

The phase builds on Phase 1's type definitions (Type, Scheme, TypeEnv, Subst) by implementing operations that transform these structures. The key challenge is getting composition order correct: compose s2 s1 means "apply s1 first, then s2" which is implemented by applying s2 to all values in s1, then merging. Free variable tracking enables let-polymorphism by identifying which variables can be generalized.

In F#, these operations follow standard patterns: recursive pattern matching on discriminated unions, Map.fold for composition, and Set operations for free variable tracking. The reference implementation in docs/todo.md provides verified F# code.

**Primary recommendation:** Implement substitution operations as pure functions using F#'s built-in Map and Set types, following the reference code exactly to avoid subtle composition order bugs.

## Standard Stack

This phase uses only F# standard library features, no external packages.

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| F# Map | .NET 8.0 | Type variable substitution mapping | Immutable dictionary for pure functional operations |
| F# Set | .NET 8.0 | Free variable tracking | Efficient set operations for variable collections |
| F# Pattern Matching | .NET 8.0 | Type AST traversal | Exhaustive matching on discriminated unions |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| F# List module | .NET 8.0 | Processing variable lists in Schemes | List.fold, List.map for bound variable handling |
| F# Seq module | .NET 8.0 | Environment value traversal | Map.values returns seq, used in freeVarsEnv |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Map<int, Type> | Dictionary<int, Type> | Mutable dict would break purity, Map is correct choice |
| Set<int> | int list | List would allow duplicates and need manual union logic |
| Recursive functions | Mutable loops | Recursion is idiomatic F#, matches type structure |

**Installation:**
No installation required, all features are built into F# .NET 8.0 standard library.

## Architecture Patterns

### Code Organization
Place all substitution operations in Type.fs module after type definitions.

```fsharp
// Type.fs structure:
// 1. Type definitions (TInt, TBool, etc.)
// 2. Scheme, TypeEnv, Subst aliases
// 3. formatType function (from Phase 1)
// 4. Substitution operations (Phase 2)
//    - apply
//    - compose
//    - applyScheme
//    - applyEnv
// 5. Free variable functions (Phase 2)
//    - freeVars
//    - freeVarsScheme
//    - freeVarsEnv
```

No separate Subst.fs module needed. Keep all type system operations together for discoverability.

### Pattern 1: Recursive Apply with Transitive Substitution
**What:** Apply substitution recursively to handle chains of substitutions (TVar n -> TVar m -> TInt)
**When to use:** Always in apply function
**Example:**
```fsharp
// Source: docs/todo.md (reference implementation)
let rec apply (s: Subst) = function
    | TInt -> TInt
    | TBool -> TBool
    | TString -> TString
    | TVar n ->
        match Map.tryFind n s with
        | Some t -> apply s t  // CRITICAL: recursive apply for transitive substitution
        | None -> TVar n
    | TArrow (t1, t2) -> TArrow (apply s t1, apply s t2)
    | TTuple ts -> TTuple (List.map (apply s) ts)
    | TList t -> TList (apply s t)
```

**Why recursive apply:** If s = {0 -> TVar 1, 1 -> TInt}, applying to TVar 0 must transitively resolve to TInt, not TVar 1.

### Pattern 2: Compose via Map.map then Map.fold
**What:** Composition chains substitutions by applying newer substitution to older one's values, then merging
**When to use:** Always when composing two substitutions
**Example:**
```fsharp
// Source: docs/todo.md (reference implementation)
let compose (s2: Subst) (s1: Subst): Subst =
    let s1' = Map.map (fun _ t -> apply s2 t) s1
    Map.fold (fun acc k v -> Map.add k v acc) s1' s2
```

**Semantics:** compose s2 s1 means "apply s1 first, then s2" (like function composition). This is implemented by:
1. Apply s2 to all values in s1 (gets s1')
2. Add all bindings from s2 to s1' (s2 bindings take precedence)

**Critical order:** The parameter order compose s2 s1 represents right-to-left application, matching mathematical composition notation.

### Pattern 3: Bound Variable Exclusion in applyScheme
**What:** Remove bound variables from substitution before applying to scheme body
**When to use:** Always when applying substitution to Scheme
**Example:**
```fsharp
// Source: docs/todo.md (reference implementation)
let applyScheme (s: Subst) (Scheme (vars, ty)): Scheme =
    let s' = List.fold (fun acc v -> Map.remove v acc) s vars
    Scheme (vars, apply s' ty)
```

**Why:** Scheme (vars, ty) means "forall vars. ty". The vars are bound (quantified) and should not be substituted. Only free type variables in ty can be substituted.

### Pattern 4: Set Operations for Free Variables
**What:** Recursively collect variables in types using Set.union, then subtract bound variables for schemes
**When to use:** Always for free variable calculation
**Example:**
```fsharp
// Source: docs/todo.md (reference implementation)
let rec freeVars = function
    | TInt | TBool | TString -> Set.empty
    | TVar n -> Set.singleton n
    | TArrow (t1, t2) -> Set.union (freeVars t1) (freeVars t2)
    | TTuple ts -> ts |> List.map freeVars |> Set.unionMany
    | TList t -> freeVars t

let freeVarsScheme (Scheme (vars, ty)) =
    Set.difference (freeVars ty) (Set.ofList vars)

let freeVarsEnv (env: TypeEnv) =
    env |> Map.values |> Seq.map freeVarsScheme |> Set.unionMany
```

**Pattern:** Use Set combinators (union, difference, unionMany) rather than manual set building.

### Anti-Patterns to Avoid
- **Non-recursive apply on TVar:** Fails to transitively resolve substitution chains, causes unification bugs later
- **Wrong compose order:** Map.fold (fun acc k v -> Map.add k v acc) s2 s1 would make s1 take precedence over s2, breaking invariant
- **Substituting bound variables:** Applying s to Scheme without removing bound vars violates forall semantics
- **List instead of Set for freeVars:** Allows duplicates, breaks Set.difference semantics in freeVarsScheme

## Don't Hand-Roll

This phase requires custom implementations of all operations, as they're specific to the type system. However, leverage standard library:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Immutable map operations | Custom tree-based map | F# Map module | Built-in, optimized, well-tested |
| Set union of multiple sets | Manual fold with Set.union | Set.unionMany | One-liner, intention-revealing |
| Safe map lookup | Exception-throwing lookup | Map.tryFind with pattern match | Idiomatic F#, forces None handling |

**Key insight:** The operations themselves must be custom (apply, compose, freeVars), but the data structures (Map, Set) should use standard library.

## Common Pitfalls

### Pitfall 1: Non-Transitive Apply
**What goes wrong:** TVar n lookup finds Some t, but returns t directly without recursing
**Why it happens:** Looks like simple dictionary lookup, easy to forget transitive case
**How to avoid:** Always use `apply s t` not just `t` in the TVar case
**Warning signs:** Unification failures later with partially-substituted types like TVar 1 when expecting TInt

**Concrete example:**
```fsharp
// WRONG:
| TVar n ->
    match Map.tryFind n s with
    | Some t -> t  // BUG: doesn't handle chains
    | None -> TVar n

// CORRECT:
| TVar n ->
    match Map.tryFind n s with
    | Some t -> apply s t  // Recursively resolve chains
    | None -> TVar n
```

**Test case that catches this:**
```fsharp
let s = Map.ofList [(0, TVar 1); (1, TInt)]
let result = apply s (TVar 0)
// Should be TInt, not TVar 1
```

### Pitfall 2: Compose Parameter Order Confusion
**What goes wrong:** Implementing compose s1 s2 to mean "s1 then s2" when it should mean "s2 then s1"
**Why it happens:** Parameter order is opposite of temporal application order (like function composition)
**How to avoid:** Remember compose s2 s1 is like (s2 ∘ s1) in math, apply right argument first
**Warning signs:** Type inference produces wrong types, substitutions don't chain correctly

**Mnemonic:** compose s2 s1 reads right-to-left like function application: (s2 . s1)(x) = s2(s1(x))

### Pitfall 3: Forgetting to Remove Bound Variables
**What goes wrong:** applyScheme substitutes quantified variables, breaking polymorphism
**Why it happens:** Easy to forget that Scheme's vars list represents bound variables
**How to avoid:** Always List.fold Map.remove over vars before applying
**Warning signs:** Polymorphic functions fail to work with different types, "let id = fun x -> x" can't be used as both int->int and bool->bool

**Example of bug:**
```fsharp
// WRONG:
let applyScheme (s: Subst) (Scheme (vars, ty)) =
    Scheme (vars, apply s ty)  // BUG: s might contain vars!

// CORRECT:
let applyScheme (s: Subst) (Scheme (vars, ty)) =
    let s' = List.fold (fun acc v -> Map.remove v acc) s vars
    Scheme (vars, apply s' ty)
```

### Pitfall 4: Using List for Free Variables
**What goes wrong:** freeVars returns int list with duplicates, Set.difference fails
**Why it happens:** Recursive traversal naturally produces duplicates (same variable appears multiple times)
**How to avoid:** Always use Set<int> for variable collections
**Warning signs:** Type errors about Set vs list in freeVarsScheme

## Code Examples

All examples verified from docs/todo.md reference implementation.

### Empty Substitution and Environment Operations
```fsharp
// Source: docs/todo.md Phase 2.1
/// Empty substitution
let empty: Subst = Map.empty

/// Single substitution creation
let singleton (v: int) (t: Type): Subst = Map.ofList [(v, t)]

/// Apply substitution to environment
let applyEnv (s: Subst) (env: TypeEnv): TypeEnv =
    Map.map (fun _ scheme -> applyScheme s scheme) env
```

### Complete Apply Function
```fsharp
// Source: docs/todo.md Phase 2.1
/// Apply substitution to type
let rec apply (s: Subst) = function
    | TInt -> TInt
    | TBool -> TBool
    | TString -> TString
    | TVar n ->
        match Map.tryFind n s with
        | Some t -> apply s t  // Recursive for transitive substitution
        | None -> TVar n
    | TArrow (t1, t2) -> TArrow (apply s t1, apply s t2)
    | TTuple ts -> TTuple (List.map (apply s) ts)
    | TList t -> TList (apply s t)
```

### Complete Compose Function
```fsharp
// Source: docs/todo.md Phase 2.1
/// Compose substitutions: s2 ∘ s1 (s1 first, then s2)
let compose (s2: Subst) (s1: Subst): Subst =
    let s1' = Map.map (fun _ t -> apply s2 t) s1
    Map.fold (fun acc k v -> Map.add k v acc) s1' s2
```

### Complete Free Variable Functions
```fsharp
// Source: docs/todo.md Phase 2.2
/// Free type variables in type
let rec freeVars = function
    | TInt | TBool | TString -> Set.empty
    | TVar n -> Set.singleton n
    | TArrow (t1, t2) -> Set.union (freeVars t1) (freeVars t2)
    | TTuple ts -> ts |> List.map freeVars |> Set.unionMany
    | TList t -> freeVars t

/// Free type variables in scheme (excluding bound variables)
let freeVarsScheme (Scheme (vars, ty)) =
    Set.difference (freeVars ty) (Set.ofList vars)

/// Free type variables in environment
let freeVarsEnv (env: TypeEnv) =
    env |> Map.values |> Seq.map freeVarsScheme |> Set.unionMany
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Explicit occurs check in substitution | Occurs check in unification only | Classic HM papers (1978-1982) | Substitution is simpler, unification handles circularity |
| String type variable names | Integer type variable IDs | Modern implementations (2000s+) | Faster comparison, simpler fresh variable generation |
| Separate Subst module | Operations in Type module | Project decision (01-01) | Better discoverability, fewer files |

**Deprecated/outdated:**
- Manual tree-based maps: F#'s Map is efficient enough, no need for custom implementation
- Imperative substitution tracking: Pure functional approach is simpler and safer

## Open Questions

None. The substitution domain is well-established with canonical implementations in literature and reference code in docs/todo.md.

## Sources

### Primary (HIGH confidence)
- docs/todo.md - Complete reference implementation with verified F# code
- Phase 1 implementation - Type.fs establishes Type, Scheme, TypeEnv, Subst definitions
- [Course.ccs.neu.edu Type Inference Lecture](https://course.ccs.neu.edu/cs4410sp19/lec_type-inference_notes.html) - Defines subst_var_scheme semantics, compose as left-to-right application
- [Max Bernstein: Damas-Hindley-Milner inference](https://bernsteinbear.com/blog/type-inference/) - Explains compose is not simple dict merge, demonstrates transitive resolution

### Secondary (MEDIUM confidence)
- [Wikipedia: Hindley-Milner type system](https://en.wikipedia.org/wiki/Hindley%E2%80%93Milner_type_system) - General overview of substitution in HM
- [GitHub: 7sharp9/write-you-an-inference-in-fsharp](https://github.com/7sharp9/write-you-an-inference-in-fsharp) - F# HM implementations (code not directly inspected)

### Tertiary (LOW confidence)
- [F# for Fun and Profit: Type Inference](https://fsharpforfunandprofit.com/posts/conciseness-type-inference/) - General F# type inference, not HM-specific
- [Microsoft Learn: F# Type Inference](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/type-inference) - F# compiler behavior, not HM algorithm details

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Only F# standard library Map/Set, well-documented
- Architecture: HIGH - Reference implementation in docs/todo.md provides exact code
- Pitfalls: HIGH - Well-known issues documented in academic sources and verified by reference code

**Research date:** 2026-02-01
**Valid until:** 180 days (2026-07-30) - Core HM substitution theory is stable, F# 9.0 unlikely to change Map/Set APIs
