# Phase 3: Unification - Research

**Researched:** 2026-02-01
**Domain:** Type unification in Hindley-Milner type inference
**Confidence:** HIGH

## Summary

Researched the unification algorithm for Hindley-Milner type inference, focusing on how to implement the most general unifier (MGU), occurs check, and type error handling. The unification algorithm is the core mechanism that finds substitutions to make two types equal, enabling type inference to work.

The standard approach follows Robinson's 1965 unification algorithm adapted for typed lambda calculus. The algorithm recursively matches type structures (primitives, variables, arrows, tuples, lists) and composes substitutions while preventing infinite types through the occurs check. Error handling uses custom exception types with clear diagnostic messages.

This phase builds directly on Phase 2's substitution operations. The `unify` function returns a `Subst` that can be composed with other substitutions and applied using the existing `apply` and `compose` functions. Phase 4 (Inference) will consume `unify` extensively.

**Primary recommendation:** Implement a recursive `unify` function with explicit pattern matching for each type constructor, perform occurs check before binding type variables, and use F# `exception` for TypeError with format strings showing both types that failed to unify.

## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| F# pattern matching | Built-in | Match type constructors | Native F# feature, no library needed |
| F# exception | Built-in | TypeError definition | Standard F# error handling |
| Map module | FSharp.Core | Substitution operations | Already used in Phase 2 |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| sprintf | FSharp.Core | Format error messages | Build descriptive TypeError messages |
| Set module | FSharp.Core | Occurs check (via freeVars) | Leverage Phase 2's freeVars function |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| exception | Result<Subst, string> | Exception is simpler for this case; Result would require threading through all unify calls |
| Recursive function | Mutable union-find | Mutable approach (Algorithm J) is faster but adds complexity; recursive is clearer for tutorial |

**Installation:**
No installation needed - all standard F# library features.

## Architecture Patterns

### Recommended Project Structure
```
FunLang/
├── Type.fs           # Already exists with Type, Subst, substitution ops
├── Unify.fs          # NEW: occurs check, unify function, TypeError
└── (Infer.fs)        # Phase 4: will import Unify module
```

### Pattern 1: Recursive Unification with Type Constructor Matching
**What:** Match on (type1, type2) pairs, handle each constructor combination
**When to use:** Core unify function structure
**Example:**
```fsharp
// Source: https://github.com/wh5a/Algorithm-W-Step-By-Step
let rec unify (t1: Type) (t2: Type): Subst =
    match t1, t2 with
    | TInt, TInt -> empty
    | TBool, TBool -> empty
    | TString, TString -> empty

    | TVar n, t | t, TVar n ->
        if t = TVar n then empty
        elif occurs n t then
            raise (TypeError (sprintf "Infinite type: %s = %s"
                (formatType (TVar n)) (formatType t)))
        else
            singleton n t

    | TArrow (a1, b1), TArrow (a2, b2) ->
        let s1 = unify a1 a2
        let s2 = unify (apply s1 b1) (apply s1 b2)
        compose s2 s1

    | TTuple ts1, TTuple ts2 when List.length ts1 = List.length ts2 ->
        List.fold2 (fun s t1 t2 ->
            let s' = unify (apply s t1) (apply s t2)
            compose s' s
        ) empty ts1 ts2

    | TList t1, TList t2 ->
        unify t1 t2

    | _ ->
        raise (TypeError (sprintf "Cannot unify %s with %s"
            (formatType t1) (formatType t2)))
```

### Pattern 2: Occurs Check Using Free Variables
**What:** Check if type variable appears in target type before binding
**When to use:** Before creating singleton substitution for TVar case
**Example:**
```fsharp
// Source: https://eli.thegreenplace.net/2018/unification/
let occurs (v: int) (t: Type): bool =
    Set.contains v (freeVars t)
```
**Why:** Prevents infinite types like `'a = 'a -> int` which would expand forever during `apply`.

### Pattern 3: Substitution Threading in Recursive Cases
**What:** Apply accumulated substitution before unifying next component
**When to use:** Arrow types, tuple types (multi-component constructors)
**Example:**
```fsharp
// Arrow: unify left sides, apply s1, then unify right sides
| TArrow (a1, b1), TArrow (a2, b2) ->
    let s1 = unify a1 a2
    let s2 = unify (apply s1 b1) (apply s1 b2)  // Apply s1 first!
    compose s2 s1

// Tuple: fold with threading
| TTuple ts1, TTuple ts2 ->
    List.fold2 (fun s t1 t2 ->
        let s' = unify (apply s t1) (apply s t2)  // Apply s first!
        compose s' s
    ) empty ts1 ts2
```
**Why:** Earlier unifications constrain type variables; later unifications must see those constraints.

### Pattern 4: Custom Exception Type with Arguments
**What:** Define TypeError exception with string argument for message
**When to use:** Type system errors (unification failure, occurs check failure)
**Example:**
```fsharp
// Source: https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/exception-handling/exception-types
exception TypeError of string

// Raise with formatted message
raise (TypeError (sprintf "Cannot unify %s with %s"
    (formatType t1) (formatType t2)))
```
**Why:** F# exception with arguments enables pattern matching in try...with and clear error propagation.

### Anti-Patterns to Avoid
- **Don't skip occurs check for "performance":** Prolog does this, but it breaks soundness (can prove false theorems). Always include it.
- **Don't use wildcard match for type variables:** `| TVar n, t ->` misses the symmetric case; use `| TVar n, t | t, TVar n ->` pattern.
- **Don't forget to apply substitution before recursive unify:** `unify b1 b2` instead of `unify (apply s1 b1) (apply s1 b2)` will fail to see constraints.
- **Don't compose substitutions in wrong order:** `compose s1 s2` vs `compose s2 s1` matters; always `compose newer older`.

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Checking variable occurrence | Custom tree walk per type constructor | `freeVars` from Phase 2 | Already implemented, tested, handles all constructors |
| Applying constraints | Custom per-case application | `apply` from Phase 2 | Handles transitive chains, all constructors |
| Combining substitutions | Manual Map merge | `compose` from Phase 2 | Correct ordering, applies s2 to s1's values |
| Formatting types for errors | Custom string builder | `formatType` from Phase 1 | Already handles precedence, all constructors |

**Key insight:** Phase 2 already provides all the machinery for substitution operations. Unification's job is only to decide *which* substitutions to create, not how to manipulate them.

## Common Pitfalls

### Pitfall 1: Forgetting Symmetric Type Variable Case
**What goes wrong:** Pattern `| TVar n, t ->` only matches when TVar is on the left, missing `| t, TVar n ->`.
**Why it happens:** Natural to think "variable on left", but unification is symmetric.
**How to avoid:** Use combined pattern `| TVar n, t | t, TVar n ->` to handle both orders.
**Warning signs:** Type errors like "Cannot unify int with 'a" when "'a = int" should succeed.

### Pitfall 2: Omitting Occurs Check
**What goes wrong:** Infinite types accepted (e.g., `'a = 'a list`), causing stack overflow in `apply`.
**Why it happens:** Occurs check seems like "extra work"; many tutorials skip it for brevity.
**How to avoid:** Always check `occurs n t` before `singleton n t`. Test with `fun f -> f f` (classic failure case).
**Warning signs:** Stack overflow during type inference for self-application.

### Pitfall 3: Wrong Substitution Composition Order
**What goes wrong:** `compose s1 s2` instead of `compose s2 s1` loses constraints.
**Why it happens:** Composition order is opposite to intuition (like function composition).
**How to avoid:** Remember `compose s2 s1` means "s1 first, then s2"; newer substitution on left.
**Warning signs:** Variables not getting unified, wrong types inferred.

### Pitfall 4: Not Applying Substitution Before Recursive Unify
**What goes wrong:** `unify (TArrow (TVar 0, TVar 1)) (TArrow (TInt, TBool))` followed by `unify b1 b2` instead of `unify (apply s1 b1) (apply s1 b2)` fails to see `TVar 0 = TInt`.
**Why it happens:** Looks redundant - "why apply if we're about to unify?"
**How to avoid:** Always apply accumulated substitution before recursive unify calls.
**Warning signs:** Type variables not getting resolved, unification failures on simple expressions.

### Pitfall 5: Tuple Length Mismatch Without Guard
**What goes wrong:** Pattern `| TTuple ts1, TTuple ts2 ->` without length check calls `List.fold2` on unequal lists, raising exception.
**Why it happens:** Assuming all tuples unify; forgetting different arities are different types.
**How to avoid:** Add guard `when List.length ts1 = List.length ts2`, fall through to error case otherwise.
**Warning signs:** ArgumentException from List.fold2 instead of clear TypeError.

### Pitfall 6: Unclear Error Messages
**What goes wrong:** `raise (TypeError "type mismatch")` doesn't show which types failed.
**Why it happens:** Lazy error message construction.
**How to avoid:** Always use `sprintf "Cannot unify %s with %s" (formatType t1) (formatType t2)` in mismatch case.
**Warning signs:** Users can't debug type errors; have to add prints to find problem.

## Code Examples

Verified patterns from official sources:

### Complete Unify Function
```fsharp
// Source: docs/todo.md + https://github.com/wh5a/Algorithm-W-Step-By-Step
module Unify

open Type

/// Type error exception
exception TypeError of string

/// Occurs check: prevents infinite types
let occurs (v: int) (t: Type): bool =
    Set.contains v (freeVars t)

/// Find most general unifier for two types
let rec unify (t1: Type) (t2: Type): Subst =
    match t1, t2 with
    // Primitives: equal types unify with empty substitution
    | TInt, TInt -> empty
    | TBool, TBool -> empty
    | TString, TString -> empty

    // Type variables: bind to other type (with occurs check)
    | TVar n, t | t, TVar n ->
        if t = TVar n then empty
        elif occurs n t then
            raise (TypeError (sprintf "Infinite type: %s = %s"
                (formatType (TVar n)) (formatType t)))
        else
            singleton n t

    // Arrow types: unify domain and codomain with substitution threading
    | TArrow (a1, b1), TArrow (a2, b2) ->
        let s1 = unify a1 a2
        let s2 = unify (apply s1 b1) (apply s1 b2)
        compose s2 s1

    // Tuple types: unify componentwise with length check
    | TTuple ts1, TTuple ts2 when List.length ts1 = List.length ts2 ->
        List.fold2 (fun s t1 t2 ->
            let s' = unify (apply s t1) (apply s t2)
            compose s' s
        ) empty ts1 ts2

    // List types: unify element types
    | TList t1, TList t2 ->
        unify t1 t2

    // Default: incompatible types
    | _ ->
        raise (TypeError (sprintf "Cannot unify %s with %s"
            (formatType t1) (formatType t2)))
```

### Testing Occurs Check
```fsharp
// Source: https://course.ccs.neu.edu/cs4410sp19/lec_type-inference_notes.html
// Test case: fun f -> f f
// Should fail with occurs check error
// Type variable 'a cannot unify with 'a -> 'b

let testOccursCheck () =
    try
        // Attempt to unify 'a with 'a -> 'b
        let s = unify (TVar 0) (TArrow (TVar 0, TVar 1))
        failwith "Should have raised TypeError"
    with
    | TypeError msg when msg.Contains("Infinite type") ->
        printfn "Occurs check working: %s" msg
```

### Error Message Format Examples
```fsharp
// Source: https://blog.stimsina.com/post/implementing-a-hindley-milner-type-system-part-2
// Good error messages show both types in readable notation

// Type mismatch
"Cannot unify int with bool"

// Occurs check failure
"Infinite type: 'a = 'a -> int"

// Tuple arity mismatch (falls through to default case)
"Cannot unify int * bool with int * bool * string"
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Manual occurs check per constructor | Use freeVars from Phase 2 | Established pattern | Simpler, less error-prone |
| Result<Subst, string> return type | exception TypeError | Common in ML family | Cleaner code, natural F# idiom |
| Mutable union-find (Algorithm J) | Immutable recursive (Algorithm W) | Both valid | W is clearer for teaching, J faster for production |
| Wildcard pattern for TVar | Symmetric pattern `\| TVar n, t \| t, TVar n ->` | Best practice | Prevents missed cases |

**Deprecated/outdated:**
- **Skipping occurs check:** Prolog does this for speed, but type inference requires it for soundness
- **String-based error types:** `raise (Failure "...")` less structured than `exception TypeError of string`

## Open Questions

Things that couldn't be fully resolved:

1. **Should we report source location in TypeError?**
   - What we know: Phase 4 (Inference) will have expression context
   - What's unclear: How to thread source location through unify calls
   - Recommendation: Start with type-only messages; enhance in Phase 4 if needed

2. **Should empty tuple unify with unit type?**
   - What we know: FunLang has TTuple [], ML has TUnit
   - What's unclear: FunLang design decision on unit type
   - Recommendation: TTuple [] is already in Type.fs; treat as valid 0-ary tuple

3. **Performance optimization needed?**
   - What we know: Tutorial codebase, not production compiler
   - What's unclear: Whether substitution copying will be slow
   - Recommendation: Implement simple version first; optimize if tests are slow

## Sources

### Primary (HIGH confidence)
- [Algorithm W Step by Step](https://github.com/wh5a/Algorithm-W-Step-By-Step/blob/master/AlgorithmW.lhs) - Reference Haskell implementation
- [F# Exception Types - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/exception-handling/exception-types) - Official F# exception syntax
- docs/todo.md (local codebase) - Complete F# implementation example
- Type.fs (local codebase) - Existing substitution operations

### Secondary (MEDIUM confidence)
- [Lecture 11: Type Inference - Northeastern](https://course.ccs.neu.edu/cs4410sp19/lec_type-inference_notes.html) - Occurs check explanation, error handling advice
- [Implementing Hindley-Milner Type System Part 2](https://blog.stimsina.com/post/implementing-a-hindley-milner-type-system-part-2) - Error handling patterns
- [Damas-Hindley-Milner inference two ways](https://bernsteinbear.com/blog/type-inference/) - Common pitfalls
- [Unification - Eli Bendersky](https://eli.thegreenplace.net/2018/unification/) - Practical implementation advice

### Tertiary (LOW confidence)
- [Wikipedia: Occurs Check](https://en.wikipedia.org/wiki/Occurs_check) - Background theory
- [Wikipedia: Unification](https://en.wikipedia.org/wiki/Unification_(computer_science)) - General concepts
- [Cornell CS 3110 Type Inference](https://www.cs.cornell.edu/courses/cs3110/2011sp/Lectures/lec26-type-inference/type-inference.htm) - Algorithm structure (verified against primary sources)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All F# built-in features, no external libraries
- Architecture: HIGH - Unify.fs structure verified in multiple sources, docs/todo.md has complete code
- Pitfalls: HIGH - Occurs check omission, substitution ordering well-documented; tested in reference implementations

**Research date:** 2026-02-01
**Valid until:** 2026-03-01 (30 days - stable domain, core algorithm unchanged since Robinson 1965)
