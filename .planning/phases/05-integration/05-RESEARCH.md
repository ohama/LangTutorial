# Phase 5: Integration - Research

**Researched:** 2026-02-01
**Domain:** Type System CLI Integration and Prelude Type Environment
**Confidence:** HIGH

## Summary

This research covers integrating the Hindley-Milner type inference system (Phases 1-4) with the FunLang CLI and defining the initial type environment for Prelude functions. The integration phase connects the type inference module to the existing CLI infrastructure (Argu-based) and creates a `typecheck` entry point that validates expressions before evaluation.

The core work involves three components: (1) defining `initialTypeEnv` with polymorphic type schemes for all 11 Prelude functions (map, filter, fold, length, reverse, append, id, const, compose, hd, tl), (2) creating a `typecheck` function that runs inference with the initial environment and catches `TypeError` exceptions, and (3) modifying Program.fs to use `--emit-type` for type display and to exit with code 1 on type errors.

**Primary recommendation:** Create TypeCheck.fs module with `initialTypeEnv` (hardcoded Prelude types) and `typecheck` function. Modify Program.fs to implement `--emit-type` and add optional type checking before evaluation. Use exit code 1 for type errors with clear error messages.

## Standard Stack

The established approach for this domain:

### Core Functions
| Function | Signature | Purpose | Why Standard |
|----------|-----------|---------|--------------|
| initialTypeEnv | `TypeEnv` | Prelude function type schemes | Mirrors Prelude.loadPrelude() for types |
| typecheck | `Expr -> Result<Type, string>` | Entry point for type inference | Catches TypeError, returns Result |
| inferAndFormat | `TypeEnv -> Expr -> string` | Infer and format type | CLI display helper |

### Supporting
| Function | Purpose | When to Use |
|----------|---------|-------------|
| preludeScheme | Create polymorphic scheme for Prelude function | Building initialTypeEnv |
| formatTypeError | User-friendly error message | CLI error output |

### Prelude Function Types (INTEG-01)
| Function | Type Scheme | In FunLang notation |
|----------|-------------|---------------------|
| map | `forall a b. (a -> b) -> a list -> b list` | `('a -> 'b) -> 'a list -> 'b list` |
| filter | `forall a. (a -> bool) -> a list -> a list` | `('a -> bool) -> 'a list -> 'a list` |
| fold | `forall a b. (b -> a -> b) -> b -> a list -> b` | `('b -> 'a -> 'b) -> 'b -> 'a list -> 'b` |
| length | `forall a. a list -> int` | `'a list -> int` |
| reverse | `forall a. a list -> a list` | `'a list -> 'a list` |
| append | `forall a. a list -> a list -> a list` | `'a list -> 'a list -> 'a list` |
| id | `forall a. a -> a` | `'a -> 'a` |
| const | `forall a b. a -> b -> a` | `'a -> 'b -> 'a` |
| compose | `forall a b c. (b -> c) -> (a -> b) -> a -> c` | `('b -> 'c) -> ('a -> 'b) -> 'a -> 'c` |
| hd | `forall a. a list -> a` | `'a list -> 'a` |
| tl | `forall a. a list -> a list` | `'a list -> 'a list` |

## Architecture Patterns

### Module Structure
```
FunLang/
├── Type.fs          # Types, schemes, substitution, freeVars
├── Unify.fs         # TypeError, occurs, unify
├── Infer.fs         # freshVar, instantiate, generalize, infer
├── TypeCheck.fs     # initialTypeEnv, typecheck (NEW)
├── Cli.fs           # CLI argument definitions (already exists)
└── Program.fs       # CLI handler (modify for --emit-type)
```

### Pattern 1: Hardcoded Initial Type Environment
**What:** Define Prelude function types as Scheme values in TypeCheck.fs
**When to use:** Type checking needs to know Prelude function types before inference
**Example:**
```fsharp
// Source: Standard approach for typed interpreters
module TypeCheck

open Type
open Infer

/// Create type scheme with given bound variables and type
let private scheme vars ty = Scheme (vars, ty)

/// Initial type environment with Prelude function types
let initialTypeEnv: TypeEnv =
    Map.ofList [
        // id: forall a. a -> a
        ("id", scheme [0] (TArrow (TVar 0, TVar 0)))

        // const: forall a b. a -> b -> a
        ("const", scheme [0; 1] (TArrow (TVar 0, TArrow (TVar 1, TVar 0))))

        // map: forall a b. (a -> b) -> a list -> b list
        ("map", scheme [0; 1] (TArrow (TArrow (TVar 0, TVar 1),
                                TArrow (TList (TVar 0), TList (TVar 1)))))
        // ... etc
    ]
```

### Pattern 2: Typecheck with Result Type
**What:** Wrap inference in try-catch, return Result<Type, string>
**When to use:** CLI needs to handle both success and type error gracefully
**Example:**
```fsharp
// Source: Standard F# error handling pattern
/// Type check expression and return result
let typecheck (expr: Expr): Result<Type, string> =
    try
        let subst, ty = infer initialTypeEnv expr
        Ok (apply subst ty)
    with
    | TypeError msg -> Error msg
```

### Pattern 3: CLI Flag Integration (INTEG-03)
**What:** Add `--emit-type` branch to Program.fs matching existing `--emit-ast` pattern
**When to use:** CLI flag for type display
**Example:**
```fsharp
// Source: Existing Program.fs pattern, adapted
elif results.Contains Emit_Type && results.Contains Expr then
    let expr = results.GetResult Expr
    try
        let ast = parse expr
        match typecheck ast with
        | Ok ty -> printfn "%s" (formatType ty); 0
        | Error msg -> eprintfn "Type error: %s" msg; 1
    with ex ->
        eprintfn "Error: %s" ex.Message
        1
```

### Pattern 4: Type Check Before Eval (INTEG-04)
**What:** Run typecheck before eval, exit 1 on type error
**When to use:** Normal evaluation mode (without --emit-type)
**Example:**
```fsharp
// Source: Standard static type checking integration
elif results.Contains Expr then
    let expr = results.GetResult Expr
    try
        let ast = parse expr
        match typecheck ast with
        | Error msg ->
            eprintfn "Type error: %s" msg
            1
        | Ok _ ->
            // Type check passed, now evaluate
            let result = eval initialEnv ast
            printfn "%s" (formatValue result)
            0
    with ex ->
        eprintfn "Error: %s" ex.Message
        1
```

### Anti-Patterns to Avoid
- **Parsing Prelude.fun for types:** Don't infer types from Prelude.fun at runtime; hardcode schemes instead
- **Mutable type variable counter pollution:** Reset or isolate freshVar counter for typecheck calls
- **Missing env apply in typecheck:** Must apply final substitution to get concrete type
- **Inconsistent error formatting:** TypeError message should match runtime error style
- **Skipping type check for --emit-type:** Must typecheck even when only displaying type

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Prelude type parsing | Parse Prelude.fun and infer | Hardcoded Scheme values | Simpler, no circular dependency, more reliable |
| Type error display | Custom error formatting | TypeError exception message | Already formatted in Unify.fs |
| Result type | Custom union | F# Result<'T, 'E> | Standard library type |
| CLI exit code | Print and continue | Return exit code from main | Proper process exit semantics |

**Key insight:** The Prelude types are fixed at language design time. Inferring them dynamically from Prelude.fun adds complexity and potential for bootstrap issues. Hardcoding is the standard approach.

## Common Pitfalls

### Pitfall 1: Type Variable Index Collisions
**What goes wrong:** Prelude type variables (0, 1, 2) collide with freshVar() output
**Why it happens:** freshVar counter starts at 0, same as scheme bound vars
**How to avoid:** freshVar already starts at 1000 (from 04-01), so no collision with 0-99 scheme vars
**Warning signs:** Weird type inference results for Prelude function applications

### Pitfall 2: Forgetting to Apply Final Substitution
**What goes wrong:** Type displayed has unresolved type variables
**Why it happens:** infer returns (subst, ty) but ty not fully resolved
**How to avoid:** Always `apply subst ty` before formatting/returning
**Warning signs:** Output like `'hq -> int` instead of `int -> int`

### Pitfall 3: Inconsistent Type Variable Naming in Output
**What goes wrong:** formatType shows 'a, 'b based on raw TVar int
**Why it happens:** TVar 1000 displays as `char(1000 % 26)` = weird character
**How to avoid:** Consider normalizing type variables for display (optional enhancement)
**Warning signs:** Output shows `'w` instead of `'a` for first polymorphic var

### Pitfall 4: Missing Exit Code 1 for Type Errors
**What goes wrong:** Type errors print message but return 0
**Why it happens:** Forgot to return 1 after error message
**How to avoid:** Match branches must return appropriate exit code
**Warning signs:** Shell scripts can't detect type errors from exit code

### Pitfall 5: Type Check in REPL Changes Environment
**What goes wrong:** REPL type environment accumulates across expressions
**Why it happens:** REPL uses single persistent TypeEnv
**How to avoid:** For Phase 5, REPL type checking is out of scope; focus on CLI
**Warning signs:** Earlier REPL expressions affect later type inference

### Pitfall 6: Incorrect Curried Function Types
**What goes wrong:** `map` typed as `(a -> b) * a list -> b list` (tuple) instead of curried
**Why it happens:** Confusing F# tuple syntax with curried arrows
**How to avoid:** Use nested TArrow: `TArrow(f, TArrow(xs, result))`
**Warning signs:** Application like `map f xs` fails to type check

## Code Examples

Verified patterns from project context and standard approaches:

### Initial Type Environment Definition
```fsharp
// Source: Standard typed interpreter pattern
module TypeCheck

open Type
open Infer
open Unify
open Ast

/// Initial type environment with Prelude function types
/// Types use bound variables 0-9 for polymorphism
let initialTypeEnv: TypeEnv =
    Map.ofList [
        // id: forall a. a -> a
        ("id", Scheme ([0], TArrow (TVar 0, TVar 0)))

        // const: forall a b. a -> b -> a
        ("const", Scheme ([0; 1], TArrow (TVar 0, TArrow (TVar 1, TVar 0))))

        // compose: forall a b c. (b -> c) -> (a -> b) -> a -> c
        ("compose", Scheme ([0; 1; 2],
            TArrow (TArrow (TVar 1, TVar 2),
                TArrow (TArrow (TVar 0, TVar 1),
                    TArrow (TVar 0, TVar 2)))))

        // map: forall a b. (a -> b) -> a list -> b list
        ("map", Scheme ([0; 1],
            TArrow (TArrow (TVar 0, TVar 1),
                TArrow (TList (TVar 0), TList (TVar 1)))))

        // filter: forall a. (a -> bool) -> a list -> a list
        ("filter", Scheme ([0],
            TArrow (TArrow (TVar 0, TBool),
                TArrow (TList (TVar 0), TList (TVar 0)))))

        // fold: forall a b. (b -> a -> b) -> b -> a list -> b
        ("fold", Scheme ([0; 1],
            TArrow (TArrow (TVar 1, TArrow (TVar 0, TVar 1)),
                TArrow (TVar 1,
                    TArrow (TList (TVar 0), TVar 1)))))

        // length: forall a. a list -> int
        ("length", Scheme ([0], TArrow (TList (TVar 0), TInt)))

        // reverse: forall a. a list -> a list
        ("reverse", Scheme ([0], TArrow (TList (TVar 0), TList (TVar 0))))

        // append: forall a. a list -> a list -> a list
        ("append", Scheme ([0],
            TArrow (TList (TVar 0),
                TArrow (TList (TVar 0), TList (TVar 0)))))

        // hd: forall a. a list -> a
        ("hd", Scheme ([0], TArrow (TList (TVar 0), TVar 0)))

        // tl: forall a. a list -> a list
        ("tl", Scheme ([0], TArrow (TList (TVar 0), TList (TVar 0))))
    ]
```

### Typecheck Function
```fsharp
// Source: Standard type checking entry point
/// Type check an expression with initial environment
/// Returns Ok(type) on success, Error(message) on type error
let typecheck (expr: Expr): Result<Type, string> =
    try
        let subst, ty = infer initialTypeEnv expr
        Ok (apply subst ty)
    with
    | TypeError msg -> Error msg
```

### Typecheck With Custom Environment
```fsharp
// Source: For future REPL integration
/// Type check with custom type environment (for REPL)
let typecheckWith (env: TypeEnv) (expr: Expr): Result<Type, string> =
    try
        let subst, ty = infer env expr
        Ok (apply subst ty)
    with
    | TypeError msg -> Error msg
```

### CLI --emit-type Handler
```fsharp
// Source: Existing Program.fs pattern, adapted for types
// In Program.fs, modify the Emit_Type branch:
elif results.Contains Emit_Type && results.Contains Expr then
    let expr = results.GetResult Expr
    try
        let ast = parse expr
        match TypeCheck.typecheck ast with
        | Ok ty ->
            printfn "%s" (formatType ty)
            0
        | Error msg ->
            eprintfn "Type error: %s" msg
            1
    with ex ->
        eprintfn "Error: %s" ex.Message
        1

// Similarly for file input:
elif results.Contains Emit_Type && results.Contains File then
    let filename = results.GetResult File
    if File.Exists filename then
        try
            let input = File.ReadAllText filename
            let ast = parse input
            match TypeCheck.typecheck ast with
            | Ok ty ->
                printfn "%s" (formatType ty)
                0
            | Error msg ->
                eprintfn "Type error: %s" msg
                1
        with ex ->
            eprintfn "Error: %s" ex.Message
            1
    else
        eprintfn "File not found: %s" filename
        1
```

### Type Check Before Evaluation
```fsharp
// Source: Standard static typing integration
// In normal eval mode (--expr without --emit-type):
elif results.Contains Expr then
    let expr = results.GetResult Expr
    try
        let ast = parse expr
        // Type check first
        match TypeCheck.typecheck ast with
        | Error msg ->
            eprintfn "Type error: %s" msg
            1
        | Ok _ ->
            // Type check passed, evaluate
            let result = eval initialEnv ast
            printfn "%s" (formatValue result)
            0
    with ex ->
        eprintfn "Error: %s" ex.Message
        1
```

### Update Cli.fs Usage Text
```fsharp
// Source: Existing Cli.fs, update Emit_Type description
| Emit_Type -> "show inferred type"  // was: "show inferred types (reserved)"
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Reserved --emit-type | Functional --emit-type | Phase 5 | Type display works |
| Runtime-only type errors | Static type checking | Phase 5 | Errors before execution |
| Parse Prelude for types | Hardcoded type schemes | Standard practice | Simpler, reliable |

**Deprecated/outdated:**
- Parsing Prelude.fun to infer types: Works but adds complexity and potential bootstrap issues
- REPL type integration: Deferred to future phase (environment threading complexity)

## Open Questions

Things that couldn't be fully resolved:

1. **Type Variable Display Normalization**
   - What we know: formatType uses TVar int mod 26 for naming
   - What's unclear: Should output normalize to 'a, 'b, 'c for readability?
   - Recommendation: Keep current behavior for Phase 5; add normalization as enhancement

2. **REPL Type Integration**
   - What we know: REPL uses persistent runtime Env
   - What's unclear: Should REPL also show types? How to thread TypeEnv?
   - Recommendation: Out of scope for Phase 5; focus on CLI batch mode

3. **Type Check Performance**
   - What we know: Type inference runs before evaluation
   - What's unclear: Is double traversal (typecheck then eval) noticeable?
   - Recommendation: Accept for now; FunLang programs are small

4. **Partial Functions (hd, tl)**
   - What we know: hd/tl are partial - fail on empty list at runtime
   - What's unclear: Should type system warn about partiality?
   - Recommendation: No warnings for Phase 5; standard ML/Haskell don't warn either

## Sources

### Primary (HIGH confidence)
- FunLang/Program.fs - Existing CLI structure with Argu
- FunLang/Prelude.fs - Runtime Prelude loading pattern
- FunLang/Infer.fs - Type inference implementation
- docs/howto/setup-argu-cli.md - Argu CLI patterns

### Secondary (MEDIUM confidence)
- OCaml/Haskell standard library types - Reference for Prelude function signatures
- F# Result type documentation - Error handling pattern

### Tertiary (LOW confidence)
- Various typed interpreter tutorials - General patterns

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Builds on existing infrastructure, well-defined requirements
- Architecture: HIGH - Follows existing module patterns, clear separation
- Pitfalls: HIGH - Most issues identified from Phase 4 experience
- Code examples: HIGH - Derived from existing codebase patterns

**Research date:** 2026-02-01
**Valid until:** 60 days (stable requirements, no external dependencies)
