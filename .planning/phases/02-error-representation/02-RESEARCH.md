# Phase 2: Error Representation - Research

**Researched:** 2026-02-03
**Domain:** Compiler diagnostics, type error representation
**Confidence:** HIGH

## Summary

Rich diagnostic types for type errors require a multi-layered structure inspired by modern compilers (Rust, Elm) and research on Hindley-Milner error reporting. The standard approach separates structural concerns (Diagnostic type for general compiler errors) from domain-specific concerns (TypeError for type inference failures), with explicit tracking of inference context and unification traces.

**Key findings:**
- F# discriminated unions are the natural fit for error hierarchies with exhaustive pattern matching
- Modern compilers (Rust, Elm) use primary/secondary span separation for blame assignment
- Context stacks track "where we are" in inference (InIfCond, InAppFun) for better error messages
- Unification traces track "what failed" structurally (AtFunctionReturn, AtTupleIndex) for precise diagnostics
- Exception vs Result type: use exceptions for type errors (exceptional, propagate through inference stack)

**Primary recommendation:** Define TypeError as a discriminated union with rich context, convert to Diagnostic for rendering. Keep exceptions for type errors (Algorithm W standard), add context/trace during propagation.

## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| F# Discriminated Unions | F# 10 (.NET 10) | Error type hierarchy | Native F# feature, exhaustive pattern matching, domain modeling |
| F# exception handling | F# 10 | Error propagation | Standard for Algorithm W, maintains call stack, simple flow control |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| None needed | - | - | Built-in F# features sufficient |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Exceptions | Result<'T, TypeError> | Result forces explicit handling at every level, incompatible with Algorithm W pattern. Exceptions better for type errors (truly exceptional, should halt inference). |
| Simple strings | Rich error types | Strings lose structure, prevent programmatic error analysis, can't attach multiple spans or build context stacks. Rich types essential. |
| codespan-reporting (Rust) | Custom F# rendering | Rust library requires FFI or rewrite. Custom F# solution simpler for this project, can be inspired by codespan/ariadne patterns. |

**Installation:**
None required (using built-in F# features)

## Architecture Patterns

### Recommended Project Structure
```
FunLang/
├── Ast.fs               # Span type (Phase 1 - complete)
├── Type.fs              # Type definitions (existing)
├── Diagnostic.fs        # NEW: Diagnostic, TypeError, InferContext, UnifyPath types
├── Unify.fs             # Modified: throw TypeError with context
├── Infer.fs             # Modified: maintain context stack during inference
└── TypeCheck.fs         # Modified: catch TypeError, convert to Diagnostic, format
```

### Pattern 1: Layered Error Types (Diagnostic wraps TypeError)

**What:** Separate general diagnostic structure from domain-specific type errors

**When to use:** When you need both structured error representation (for rendering) and domain-specific information (for debugging)

**Example:**
```fsharp
// Diagnostic.fs

/// General diagnostic structure (inspired by Rust/Elm compilers)
type Diagnostic = {
    Code: string option              // e.g. Some "E0301" or None
    Message: string                  // Primary error message
    PrimarySpan: Span                // Main error location
    SecondarySpans: (Span * string) list  // Related locations with labels
    Notes: string list               // Additional context
    Hint: string option              // Suggested fix
}

/// Type error kind - what went wrong
type TypeErrorKind =
    | UnifyMismatch of expected: Type * actual: Type
    | OccursCheck of var: int * ty: Type
    | UnboundVar of name: string
    | NotAFunction of ty: Type

/// Rich type error with context and trace
type TypeError = {
    Kind: TypeErrorKind
    Span: Span                       // Where the error occurred
    Term: Expr option                // The problematic expression (if available)
    ContextStack: InferContext list  // Inference path (outer to inner)
    Trace: UnifyPath list            // Structural failure location
}

/// Inference context - where are we in type inference?
type InferContext =
    | InIfCond of Span               // Checking if-condition
    | InIfThen of Span               // Checking then-branch
    | InIfElse of Span               // Checking else-branch
    | InAppFun of Span               // Checking function in application
    | InAppArg of Span               // Checking argument in application
    | InLetRhs of name: string * Span // Checking let binding RHS
    | InLetBody of name: string * Span // Checking let body
    | InMatch of Span                // Checking match expression
    | InTupleElement of index: int * Span // Checking tuple element

/// Unification path - where did unification structurally fail?
type UnifyPath =
    | AtFunctionParam of Type        // Failed at function parameter
    | AtFunctionReturn of Type       // Failed at function return
    | AtTupleIndex of index: int * Type // Failed at tuple element
    | AtListElement of Type          // Failed at list element

/// Convert TypeError to Diagnostic for rendering
let typeErrorToDiagnostic (err: TypeError): Diagnostic =
    // Convert based on Kind, include context summary in notes
    match err.Kind with
    | UnifyMismatch (expected, actual) ->
        { Code = Some "E0301"
          Message = sprintf "Type mismatch: expected %s but got %s"
                        (formatType expected) (formatType actual)
          PrimarySpan = err.Span
          SecondarySpans = []  // TODO: extract from context/trace
          Notes = formatContextStack err.ContextStack
          Hint = Some "Check the type of this expression" }
    | OccursCheck (v, ty) ->
        { Code = Some "E0302"
          Message = sprintf "Infinite type: %s = %s"
                        (formatType (TVar v)) (formatType ty)
          PrimarySpan = err.Span
          SecondarySpans = []
          Notes = ["This occurs when a type variable appears inside its own definition"]
          Hint = Some "Consider using explicit type annotations to clarify intent" }
    | UnboundVar name ->
        { Code = Some "E0303"
          Message = sprintf "Unbound variable: %s" name
          PrimarySpan = err.Span
          SecondarySpans = []
          Notes = []
          Hint = Some (sprintf "Did you mean to define '%s' with a let binding?" name) }
    | NotAFunction ty ->
        { Code = Some "E0304"
          Message = sprintf "Cannot call non-function type: %s" (formatType ty)
          PrimarySpan = err.Span
          SecondarySpans = []
          Notes = formatContextStack err.ContextStack
          Hint = Some "Only function types can be applied to arguments" }

/// Format context stack for diagnostic notes
let formatContextStack (ctx: InferContext list): string list =
    ctx |> List.rev |> List.map (function
        | InIfCond _ -> "While checking if-condition"
        | InIfThen _ -> "While checking then-branch"
        | InIfElse _ -> "While checking else-branch"
        | InAppFun _ -> "While checking function in application"
        | InAppArg _ -> "While checking function argument"
        | InLetRhs (name, _) -> sprintf "While checking RHS of let %s = ..." name
        | InLetBody (name, _) -> sprintf "In body of let %s = ..." name
        | InMatch _ -> "While checking match expression"
        | InTupleElement (i, _) -> sprintf "In tuple element %d" i)
```

**Why this pattern:** Separates concerns - Diagnostic is pure data for rendering, TypeError carries domain knowledge. F# compiler uses similar layering (FSharpDiagnostic wraps internal error types).

### Pattern 2: Exception with Rich Payload

**What:** Throw exceptions carrying structured TypeError, not just strings

**When to use:** Type errors in Algorithm W (exceptional condition, should abort inference)

**Example:**
```fsharp
// Unify.fs

exception TypeException of TypeError

/// Modified unify with context tracking
let rec unifyWithContext (ctx: InferContext list) (trace: UnifyPath list)
                         (span: Span) (t1: Type) (t2: Type): Subst =
    match t1, t2 with
    | TInt, TInt -> empty
    | TBool, TBool -> empty
    // ... other cases ...

    | TArrow (a1, b1), TArrow (a2, b2) ->
        // Track where we are in the arrow structure
        let s1 = unifyWithContext ctx (AtFunctionParam a1 :: trace) span a1 a2
        let s2 = unifyWithContext ctx (AtFunctionReturn b1 :: trace) span
                                  (apply s1 b1) (apply s1 b2)
        compose s2 s1

    | TVar n, t | t, TVar n ->
        if t = TVar n then empty
        elif occurs n t then
            raise (TypeException {
                Kind = OccursCheck (n, t)
                Span = span
                Term = None
                ContextStack = ctx
                Trace = trace
            })
        else singleton n t

    | _ ->
        raise (TypeException {
            Kind = UnifyMismatch (t1, t2)
            Span = span
            Term = None
            ContextStack = ctx
            Trace = trace
        })
```

**Why exceptions:** Algorithm W naturally uses exceptions for type errors. Modern research confirms: "using result types as a general-purpose error handling mechanism for F# applications should be considered harmful" for compiler-like applications. Exceptions preserve stack, simplify control flow.

### Pattern 3: Context Stack Threading

**What:** Thread context stack through all inference functions

**When to use:** Everywhere in Algorithm W to track inference path

**Example:**
```fsharp
// Infer.fs

/// Modified infer with context tracking
let rec inferWithContext (ctx: InferContext list) (env: TypeEnv)
                         (expr: Expr): Subst * Type =
    let span = spanOf expr
    try
        match expr with
        | Number (_, _) -> (empty, TInt)
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

        | If (cond, thenExpr, elseExpr, span) ->
            let s1, condTy = inferWithContext (InIfCond span :: ctx) env cond
            let s2 = unifyWithContext ctx [] span (apply s1 condTy) TBool
            let s = compose s2 s1
            let env' = applyEnv s env
            let s3, thenTy = inferWithContext (InIfThen span :: ctx) env' thenExpr
            let s4, elseTy = inferWithContext (InIfElse span :: ctx)
                                              (applyEnv s3 env') elseExpr
            let s5 = unifyWithContext ctx [] span
                                      (apply s4 thenTy) (apply s4 elseTy)
            (compose s5 (compose s4 (compose s3 s)), apply s5 thenTy)

        | App (func, arg, span) ->
            let s1, funcTy = inferWithContext (InAppFun span :: ctx) env func
            let s2, argTy = inferWithContext (InAppArg span :: ctx)
                                             (applyEnv s1 env) arg
            let resultTy = freshVar()
            let s3 = unifyWithContext ctx [] span
                                      (apply s2 funcTy) (TArrow (argTy, resultTy))
            (compose s3 (compose s2 s1), apply s3 resultTy)

        // ... other cases ...
    with
    | TypeException err ->
        // Enrich with current expression if not already set
        let err' =
            match err.Term with
            | None -> { err with Term = Some expr }
            | Some _ -> err
        raise (TypeException err')
```

**Why context threading:** Elm showed that "no significant changes to the type inference algorithm" are needed - just add context hints during traversal. Minimal performance cost, huge diagnostic improvement.

### Anti-Patterns to Avoid

- **Don't use string exceptions:** `raise (TypeError "type mismatch")` loses all structure. Always use discriminated unions.
- **Don't ignore spans:** Every error needs a primary span. Unknown span is better than no span tracking.
- **Don't build error messages during inference:** Build structured errors, convert to messages at the end. Separation of concerns.
- **Don't catch and rethrow without enrichment:** If catching TypeError, add context before rethrowing. Otherwise, just propagate.

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| String formatting for types | Custom concatenation | formatType from Type.fs | Already handles precedence, arrow associativity correctly |
| Position tracking | Custom line/column math | Span from Ast.fs (Phase 1) | Already integrated with FsLexYacc Position API |
| Error message templates | String interpolation scattered everywhere | Centralized conversion functions | Maintainability, consistency, i18n future-proofing |
| Color output for CLI | ANSI code manual insertion | Defer to Phase 4 (Output & Testing) | Rendering concern, separate from error representation |

**Key insight:** Phase 2 is about *structure* (types, data), not *presentation* (formatting, colors). Keep concerns separate.

## Common Pitfalls

### Pitfall 1: Confusing Type and TypeError
**What goes wrong:** Beginners try to embed error information directly in the Type discriminated union (e.g., `TError of string`).

**Why it happens:** Seems convenient to mark "bad types" in the type system itself.

**How to avoid:** Keep Type pure (represents valid types only). TypeError is a separate exception/result type. Type errors abort inference, they don't propagate as "error types."

**Warning signs:**
- Pattern matching on `TError` in unify/infer functions
- Type environment containing error markers
- Trying to unify error types with other types

### Pitfall 2: Context Stack Direction Confusion
**What goes wrong:** Building context stack in wrong order (inner to outer instead of outer to inner).

**Why it happens:** Natural to cons onto the front of a list, but context reads better outer-to-inner.

**How to avoid:**
- Store context outer-to-inner: `[InLetRhs; InIfCond]` means "in a let RHS, inside an if condition"
- Cons new context at the HEAD: `inferWithContext (InIfCond span :: ctx) ...`
- Reverse when formatting: `ctx |> List.rev |> List.map format`

**Warning signs:** Error messages read backwards ("While checking if-condition... in let RHS" instead of "In let RHS... while checking if-condition")

### Pitfall 3: Over-Collecting Secondary Spans
**What goes wrong:** Adding every expression span to secondary spans, creating noise instead of clarity.

**Why it happens:** Want to be maximally helpful, but too much information overwhelms.

**How to avoid:**
- Primary span: the MOST direct cause (innermost expression that failed)
- Secondary spans: only spans that EXPLAIN the primary (e.g., function definition when call fails)
- Usually 0-2 secondary spans, rarely more than 3
- Phase 2 can defer this - Phase 3 (Blame Assignment) determines which spans matter

**Warning signs:**
- Every error has 5+ secondary spans
- Secondary spans just repeat the AST structure
- Users complain errors are "too noisy"

### Pitfall 4: Trace Without Context (or vice versa)
**What goes wrong:** UnifyPath shows "AtFunctionReturn" but not WHICH function call caused the unification.

**Why it happens:** Trace and context are orthogonal - trace is structural (inside types), context is semantic (inside inference).

**How to avoid:**
- Always populate BOTH ContextStack and Trace
- ContextStack: inference path (InAppFun, InLetRhs)
- Trace: structural path (AtFunctionReturn, AtTupleIndex)
- Together they form complete picture: "In function application (context), the return type (trace) doesn't match"

**Warning signs:**
- Errors say "type mismatch at function return" but don't say which function
- Errors say "while checking function argument" but don't say which type constructor failed

### Pitfall 5: Exception Type vs Exception Instance
**What goes wrong:** Throwing `TypeError` directly instead of wrapping in an exception.

**Why it happens:** F# exceptions must inherit from `System.Exception` or be defined with `exception` keyword.

**How to avoid:**
```fsharp
// ✅ CORRECT: Define exception that wraps TypeError
exception TypeException of TypeError

// Then throw:
raise (TypeException { Kind = ...; Span = ...; ... })

// NOT:
// raise TypeError { ... }  // ❌ WRONG: TypeError is not an exception type
```

**Warning signs:** Compiler error "TypeError is not an exception type"

## Code Examples

Verified patterns from official sources and research:

### Example 1: Basic Diagnostic Types
```fsharp
// Source: Based on Rust rustc diagnostic structs and Ariadne report model
// See: https://rustc-dev-guide.rust-lang.org/diagnostics/diagnostic-structs.html

type Diagnostic = {
    Code: string option
    Message: string
    PrimarySpan: Span
    SecondarySpans: (Span * string) list
    Notes: string list
    Hint: string option
}

type TypeErrorKind =
    | UnifyMismatch of expected: Type * actual: Type
    | OccursCheck of var: int * ty: Type
    | UnboundVar of name: string
    | NotAFunction of ty: Type

type TypeError = {
    Kind: TypeErrorKind
    Span: Span
    Term: Expr option
    ContextStack: InferContext list
    Trace: UnifyPath list
}

exception TypeException of TypeError
```

### Example 2: Context and Trace Types
```fsharp
// Source: Inspired by GHC's error context and research on type error slicing
// Pattern: Tag each inference context with the span where that context begins

type InferContext =
    | InIfCond of Span
    | InIfThen of Span
    | InIfElse of Span
    | InAppFun of Span
    | InAppArg of Span
    | InLetRhs of name: string * Span
    | InLetBody of name: string * Span
    | InLetRecBody of name: string * Span
    | InMatch of Span
    | InMatchClause of index: int * Span
    | InTupleElement of index: int * Span
    | InListElement of index: int * Span
    | InConsHead of Span
    | InConsTail of Span

type UnifyPath =
    | AtFunctionParam of Type
    | AtFunctionReturn of Type
    | AtTupleIndex of index: int * Type
    | AtListElement of Type
```

### Example 3: Converting TypeError to Diagnostic
```fsharp
// Source: Pattern inspired by Elm's error message philosophy
// See: https://elm-lang.org/news/compiler-errors-for-humans

let typeErrorToDiagnostic (err: TypeError): Diagnostic =
    match err.Kind with
    | UnifyMismatch (expected, actual) ->
        let contextNotes = formatContextStack err.ContextStack
        let traceNotes = formatTrace err.Trace
        { Code = Some "E0301"
          Message = sprintf "Type mismatch: expected %s, but got %s"
                        (formatType expected) (formatType actual)
          PrimarySpan = err.Span
          SecondarySpans = extractSecondarySpans err.ContextStack
          Notes = contextNotes @ traceNotes
          Hint = inferHint err }

    | OccursCheck (v, ty) ->
        { Code = Some "E0302"
          Message = sprintf "Infinite type detected: %s = %s"
                        (formatType (TVar v)) (formatType ty)
          PrimarySpan = err.Span
          SecondarySpans = []
          Notes = ["This usually happens when a function is applied to itself"
                   "or when recursive types are inferred without explicit annotation"]
          Hint = Some "Consider adding a type annotation to break the cycle" }

    | UnboundVar name ->
        { Code = Some "E0303"
          Message = sprintf "Unbound variable: %s" name
          PrimarySpan = err.Span
          SecondarySpans = []
          Notes = []
          Hint = Some (sprintf "Did you mean to define '%s' before using it?" name) }

    | NotAFunction ty ->
        { Code = Some "E0304"
          Message = sprintf "Cannot apply arguments to non-function type %s"
                        (formatType ty)
          PrimarySpan = err.Span
          SecondarySpans = []
          Notes = formatContextStack err.ContextStack
          Hint = Some "Only function types (a -> b) can be called" }

let formatContextStack (ctx: InferContext list): string list =
    // Reverse to read outer-to-inner
    ctx
    |> List.rev
    |> List.mapi (fun i c ->
        let indent = String.replicate i "  "
        match c with
        | InIfCond _ -> indent + "While checking if-condition"
        | InIfThen _ -> indent + "In then-branch"
        | InIfElse _ -> indent + "In else-branch"
        | InAppFun _ -> indent + "While checking function in application"
        | InAppArg _ -> indent + "While checking argument"
        | InLetRhs (name, _) -> indent + sprintf "In right-hand side of: let %s = ..." name
        | InLetBody (name, _) -> indent + sprintf "In body of: let %s = ... in" name
        | InMatch _ -> indent + "While checking match expression"
        | InTupleElement (i, _) -> indent + sprintf "In tuple element at index %d" i
        | _ -> indent + "In expression")

let formatTrace (trace: UnifyPath list): string list =
    match trace with
    | [] -> []
    | _ ->
        ["Unification failed at:"]
        @ (trace |> List.rev |> List.map (function
            | AtFunctionParam ty ->
                sprintf "  - Function parameter (expected %s)" (formatType ty)
            | AtFunctionReturn ty ->
                sprintf "  - Function return type (expected %s)" (formatType ty)
            | AtTupleIndex (i, ty) ->
                sprintf "  - Tuple element %d (expected %s)" i (formatType ty)
            | AtListElement ty ->
                sprintf "  - List element (expected %s)" (formatType ty)))

let extractSecondarySpans (ctx: InferContext list): (Span * string) list =
    // Phase 3 will implement blame assignment logic
    // For Phase 2, just return empty - structure is ready
    []

let inferHint (err: TypeError): string option =
    // Phase 4 will implement smart hints based on error patterns
    // For Phase 2, basic hints based on kind
    match err.Kind with
    | UnifyMismatch (TBool, _) ->
        Some "Did you mean to use a boolean expression here?"
    | UnifyMismatch (TInt, _) ->
        Some "Did you mean to use an integer expression here?"
    | UnifyMismatch (TArrow _, _) ->
        Some "Expected a function type here"
    | _ -> None
```

### Example 4: Unify with Context and Trace
```fsharp
// Source: Standard Robinson unification augmented with context tracking
// Pattern: Thread context and trace through recursion, build trace as we descend

let rec unifyWithContext (ctx: InferContext list) (trace: UnifyPath list)
                         (span: Span) (t1: Type) (t2: Type): Subst =
    match t1, t2 with
    | TInt, TInt -> empty
    | TBool, TBool -> empty
    | TString, TString -> empty

    | TVar n, t | t, TVar n ->
        if t = TVar n then empty
        elif occurs n t then
            raise (TypeException {
                Kind = OccursCheck (n, t)
                Span = span
                Term = None
                ContextStack = ctx
                Trace = trace
            })
        else singleton n t

    | TArrow (a1, b1), TArrow (a2, b2) ->
        // Build trace as we descend into arrow structure
        let s1 = unifyWithContext ctx (AtFunctionParam a2 :: trace) span a1 a2
        let s2 = unifyWithContext ctx (AtFunctionReturn b2 :: trace) span
                                  (apply s1 b1) (apply s1 b2)
        compose s2 s1

    | TTuple ts1, TTuple ts2 when List.length ts1 = List.length ts2 ->
        List.fold2 (fun (s, idx) t1 t2 ->
            let trace' = AtTupleIndex (idx, t2) :: trace
            let s' = unifyWithContext ctx trace' span (apply s t1) (apply s t2)
            (compose s' s, idx + 1)
        ) (empty, 0) ts1 ts2
        |> fst

    | TList t1, TList t2 ->
        unifyWithContext ctx (AtListElement t2 :: trace) span t1 t2

    | _ ->
        raise (TypeException {
            Kind = UnifyMismatch (t1, t2)
            Span = span
            Term = None
            ContextStack = ctx
            Trace = trace
        })
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| String exceptions | Rich discriminated unions | F# from start | Type-safe error handling, exhaustive pattern matching |
| Single error location | Primary + secondary spans | Rust 1.12 (2016), GCC 6.0 (2016) | Users can see related code that explains the error |
| Generic "type error" | Specific error kinds | Elm 0.16 (2015), TypeScript 2.0 (2016) | Targeted hints, better error recovery |
| Implicit context | Explicit context stacks | GHC 8.0 (2016), research papers 2015+ | Clear inference paths in errors |
| No unification trace | Structural failure paths | Recent research (2020+) | Pinpoint exact type constructor mismatch |

**Deprecated/outdated:**
- **String-based error messages in type checkers:** Modern approach uses structured errors (discriminated unions) that convert to strings only for display. Enables testing, refactoring, IDE integration.
- **Single-span errors:** Multi-span diagnostics are now standard (Rust, Clang, GCC). Users need to see related code.
- **Error messages that blame the user:** Elm showed that compiler messages should educate, not scold. Use "I found" not "You made a mistake."

## Open Questions

Things that couldn't be fully resolved:

1. **Error codes vs no codes**
   - What we know: Rust uses E0301-style codes, F# uses FS0001-style, Elm uses no codes
   - What's unclear: Whether error codes are worth maintaining for a tutorial project
   - Recommendation: Use codes (E0301, E0302, etc.) for Phase 2-4, makes testing easier ("expect error E0301"), optional can be removed later

2. **How much trace detail is too much?**
   - What we know: UnifyPath can record every step of unification descent
   - What's unclear: Whether full trace helps or overwhelms users
   - Recommendation: Collect full trace in Phase 2, Phase 4 testing will reveal if it needs filtering

3. **Secondary span selection heuristics**
   - What we know: Rust/Clang use sophisticated heuristics to pick which related spans to show
   - What's unclear: What heuristics work best for Hindley-Milner errors specifically
   - Recommendation: Defer to Phase 3 (Blame Assignment), Phase 2 just provides the structure (empty list is fine)

4. **Normalization of type variables in errors**
   - What we know: TVar 1000 should display as 'a, TVar 1001 as 'b, etc.
   - What's unclear: Whether to normalize at error construction or display time
   - Recommendation: Display time (Phase 4). Keep TVar numbers in TypeError for accuracy, normalize in formatType when rendering Diagnostic.

## Sources

### Primary (HIGH confidence)
- [Rust Compiler Diagnostic Structs](https://rustc-dev-guide.rust-lang.org/diagnostics/diagnostic-structs.html) - primary_span, labels, notes structure
- [Elm Compiler Errors for Humans](https://elm-lang.org/news/compiler-errors-for-humans) - philosophy and message structure
- [F# Discriminated Unions - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/discriminated-unions) - official F# DU documentation
- [F# Compiler Diagnostics Guide](https://fsharp.github.io/fsharp-compiler-docs/diagnostics.html) - F# compiler diagnostic structure
- [GCC Guidelines for Diagnostics](https://gcc.gnu.org/onlinedocs/gccint/Guidelines-for-Diagnostics.html) - rich_location and multi-span patterns

### Secondary (MEDIUM confidence)
- [Ariadne Diagnostic Crate (Rust)](https://github.com/zesterer/ariadne) - modern diagnostic reporting library (Rust, but patterns apply)
- [Writing Good Compiler Error Messages](https://calebmer.com/2019/07/01/writing-good-compiler-error-messages.html) - principles and examples
- [F# Exception vs Result Pattern](https://dev.to/k_ribaric/net-error-handling-balancing-exceptions-and-the-result-pattern-ljo) - when to use each
- [Hindley-Milner Type Inference Lecture Notes](https://course.ccs.neu.edu/cs4410sp19/lec_type-inference_notes.html) - unification algorithm context

### Tertiary (LOW confidence)
- [Local Contextual Type Inference POPL 2026](https://popl26.sigplan.org/details/POPL-2026-popl-research-papers/13/Local-Contextual-Type-Inference) - cutting-edge research on contextual typing (may inform future work)
- WebSearch results for type error patterns - various compiler error message examples

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - F# discriminated unions are well-documented, standard pattern
- Architecture: HIGH - Layered error types proven in Rust/F#/Elm compilers
- Context/trace tracking: MEDIUM - Pattern exists in research/GHC, but Hindley-Milner specifics less documented
- Error codes: MEDIUM - Conventions vary (Rust yes, Elm no), tutorial context makes it optional
- Secondary span heuristics: LOW - Deferred to Phase 3, needs experimentation

**Research date:** 2026-02-03
**Valid until:** 60 days (stable domain - compiler diagnostic patterns change slowly)
