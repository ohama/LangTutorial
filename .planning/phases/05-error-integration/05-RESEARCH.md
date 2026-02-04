# Phase 5: Error Integration - Research

**Researched:** 2026-02-04
**Domain:** Compiler Error Messages - Bidirectional Type Checking
**Confidence:** HIGH

## Summary

Phase 5 enhances FunLang's error messages to be mode-aware and include expected type information from annotations. The existing `Diagnostic.fs` infrastructure already provides excellent error formatting with context tracking, secondary spans, notes, and hints. The key enhancement is adding bidirectional mode context (checking vs synthesis) and incorporating annotation-derived expected types into error messages.

The requirements are focused on three areas: (1) adding mode-aware context types (`InCheckMode`, `InSynthMode`) to track whether an error occurred during checking or synthesis, (2) enhancing error messages to include "expected int due to annotation" style explanations when an annotation provided the expected type, and (3) reusing the existing `Diagnostic` infrastructure rather than building new error handling.

Current error messages already show "expected X but got Y" format (error E0301). The enhancement adds explanation of WHY that type was expected - specifically, when the expected type comes from a user annotation vs. being inferred. This follows the principle from bidirectional typing that "one of the benefits of this method of typechecking is that it can provide quite specific error messages."

**Primary recommendation:** Add `InCheckMode of Type * Span` and `InSynthMode of Span` to `InferContext`, pass expected types through check calls, and enhance `typeErrorToDiagnostic` to extract annotation-derived types from context for richer error messages.

## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| F# | 10 | Host language | Project already uses F# |
| Diagnostic.fs | v5.0 | Error infrastructure | Already has InferContext, Diagnostic, formatDiagnostic |
| Bidir.fs | v6.0 | Bidirectional checker | Source of type errors, passes InferContext |
| Unify.fs | v5.0 | Unification errors | Already raises TypeException with context |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Type.fs | v5.0 | Type formatting | formatType, formatTypeNormalized for error messages |
| Ast.fs | v4.0 | Span information | Error location, expression spans |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Extend InferContext | Separate error context type | InferContext already threaded everywhere, simpler to extend |
| Add new error codes | Reuse E0301 with richer messages | E0301 already handles type mismatch, just needs more context |
| Custom annotation tracking | Leverage existing TypeExpr spans | Spans already captured in AST |

**Installation:**
No new dependencies required. All infrastructure exists in FunLang codebase.

## Architecture Patterns

### Recommended Project Structure
```
FunLang/
├── Diagnostic.fs    # MODIFY - Add InCheckMode, InSynthMode to InferContext
├── Bidir.fs         # MODIFY - Pass expected types through check calls
├── Type.fs          # NO CHANGE - Type formatting already works
└── Unify.fs         # NO CHANGE - Already raises TypeException with context
```

### Pattern 1: Mode-Aware InferContext

**What:** Add context variants that track whether we're in checking or synthesis mode, and what expected type triggered checking mode.

**When to use:** When entering check mode with an annotation-derived expected type.

**Example:**
```fsharp
// Source: Diagnostic.fs extension
type InferContext =
    // Existing contexts
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
    // NEW: Mode-aware contexts (ERR-01)
    | InCheckMode of expected: Type * source: string * Span  // "expected int due to annotation"
    | InSynthMode of Span  // "inferring type"
```

### Pattern 2: Push Expected Type Context in Check

**What:** When entering check mode with an annotation-derived type, push a context that records the expected type and its source (annotation, if-branch, etc.).

**When to use:** In `check` function when processing annotated expressions.

**Example:**
```fsharp
// Source: Bidir.fs check function enhancement
| Annot (e, tyExpr, span) ->
    let expectedTy = elaborateTypeExpr tyExpr
    // Push context explaining where expected type came from
    let ctx' = InCheckMode (expectedTy, "annotation", span) :: ctx
    let s = check ctx' env e expectedTy
    (s, apply s expectedTy)
```

### Pattern 3: Extract Annotation Source from Context

**What:** When formatting error messages, scan context stack for InCheckMode to explain why a type was expected.

**When to use:** In `typeErrorToDiagnostic` when generating notes.

**Example:**
```fsharp
// Source: Diagnostic.fs enhancement
let findExpectedTypeSource (contexts: InferContext list) : (Type * string * Span) option =
    contexts
    |> List.tryPick (function
        | InCheckMode (ty, source, span) -> Some (ty, source, span)
        | _ -> None)

let typeErrorToDiagnostic (err: TypeError) : Diagnostic =
    let code, message, hint =
        match err.Kind with
        | UnifyMismatch (expected, actual) ->
            // Check if expected type came from annotation
            let source = findExpectedTypeSource err.ContextStack
            let msg =
                match source with
                | Some (ty, "annotation", span) ->
                    sprintf "Type mismatch: expected %s (due to annotation at %s) but got %s"
                        (formatType expected) (formatSpan span) (formatType actual)
                | Some (ty, reason, _) ->
                    sprintf "Type mismatch: expected %s (%s) but got %s"
                        (formatType expected) reason (formatType actual)
                | None ->
                    sprintf "Type mismatch: expected %s but got %s"
                        (formatType expected) (formatType actual)
            Some "E0301", msg, Some "Check that the expression type matches the annotation"
        // ... other cases
```

### Pattern 4: Preserve Existing Error Flow

**What:** The existing error flow (TypeException raised, caught at top level, converted to Diagnostic) remains unchanged. Only the context information is enriched.

**When to use:** Always - ERR-03 requires reusing existing infrastructure.

**Example:**
```fsharp
// Source: Existing flow preserved
// 1. Bidir.fs raises TypeException with enriched context
raise (TypeException {
    Kind = UnifyMismatch (expected, actual)
    Span = span
    Term = Some expr
    ContextStack = ctx  // Now includes InCheckMode when relevant
    Trace = trace
})

// 2. Top-level catches and formats (unchanged flow)
try
    let ty = Bidir.synthTop env expr
    // ...
with
| TypeException err ->
    let diag = typeErrorToDiagnostic err  // Enhanced formatting
    printfn "%s" (formatDiagnostic diag)
```

### Anti-Patterns to Avoid

- **Don't duplicate error handling:** Reuse existing TypeException/Diagnostic flow (ERR-03)
- **Don't lose context:** Always push InCheckMode/InSynthMode when entering those modes
- **Don't over-annotate:** Only add "due to annotation" when the expected type actually came from user annotation
- **Don't break existing tests:** Error message enhancements should be additive, not breaking

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Error formatting | Custom error formatter | `Diagnostic.formatDiagnostic` | Already produces Rust-style multi-line errors |
| Context tracking | Custom context type | `InferContext` extension | Already threaded through all inference |
| Type display | Custom type printer | `Type.formatType` | Already handles all type variants |
| Span formatting | Custom location printer | `Ast.formatSpan` | Already produces file:line:col format |
| Context notes | Custom note builder | `formatContextStack` | Already converts context to readable notes |

**Key insight:** FunLang already has production-quality error infrastructure. Phase 5 only needs to enrich the context information being passed through it, not rebuild the infrastructure.

## Common Pitfalls

### Pitfall 1: Forgetting to Push Mode Context
**What goes wrong:** Error messages don't include "due to annotation" even when the type came from an annotation.

**Why it happens:** Easy to forget to push InCheckMode when entering check with annotation-derived type.

**How to avoid:** Systematically add InCheckMode push in all check entry points that have annotation-derived types.

**Warning signs:**
- Error messages for annotation mismatches look identical to inference mismatches
- "due to annotation" never appears in any error

### Pitfall 2: Wrong Expected/Actual Order in Messages
**What goes wrong:** "expected bool but got int" when user wrote `(1 : bool)` - confusing because user annotated bool but expression is int.

**Why it happens:** Unification is symmetric but error messages are not.

**How to avoid:** In subsumption, always unify `expected` with `actual` where `expected` is the annotation type and `actual` is the synthesized type.

**Warning signs:**
- Error message blames annotation when it should blame expression
- Expected/actual appear reversed from user's mental model

### Pitfall 3: Context Stack Pollution
**What goes wrong:** Too many contexts pushed, error messages become verbose with redundant information.

**Why it happens:** Pushing context in every check/synth call.

**How to avoid:** Only push InCheckMode when entering check from a significant boundary (annotation, if-branch, etc.). Don't push for every recursive call.

**Warning signs:**
- Error messages have many repetitive notes
- Same span appears multiple times in different contexts

### Pitfall 4: Breaking Existing Golden Tests
**What goes wrong:** Existing fslit type-error tests fail because error message format changed.

**Why it happens:** Adding "due to annotation" changes expected output.

**How to avoid:**
- Update affected golden tests with new expected output
- Add new tests for annotation-specific errors
- Keep non-annotation errors unchanged

**Warning signs:**
- `make -C tests` failures in type-errors directory
- Tests fail with "expected X but got Y" in test output diff

### Pitfall 5: Losing Original Error Location
**What goes wrong:** Error points to annotation location instead of the actual expression causing the mismatch.

**Why it happens:** InCheckMode span is annotation span, error uses that instead of expression span.

**How to avoid:** Keep error span as expression span (where the actual type mismatch is), use InCheckMode span only in explanatory notes.

**Warning signs:**
- Error arrow points to `: int` instead of the mismatched expression
- User can't see which expression is wrong

## Code Examples

Verified patterns from FunLang codebase:

### Current InferContext (to extend)
```fsharp
// Source: Diagnostic.fs lines 25-39
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
    // Add: InCheckMode, InSynthMode
```

### Current Error Message Format
```fsharp
// Source: Diagnostic.fs lines 127-167
let typeErrorToDiagnostic (err: TypeError) : Diagnostic =
    let code, message, hint =
        match err.Kind with
        | UnifyMismatch (expected, actual) ->
            Some "E0301",
            sprintf "Type mismatch: expected %s but got %s"
                (formatType expected) (formatType actual),
            Some "Check that all branches of your expression return the same type"
        // ... other cases
```

### Annotation Handling in Bidir.fs (to enhance)
```fsharp
// Source: Bidir.fs lines 79-83
| Annot (e, tyExpr, span) ->
    let expectedTy = elaborateTypeExpr tyExpr
    let s = check ctx env e expectedTy  // Add InCheckMode here
    (s, apply s expectedTy)
```

### Context Stack Formatting (to extend)
```fsharp
// Source: Diagnostic.fs lines 66-84
let formatContextStack (stack: InferContext list) : string list =
    stack
    |> List.rev
    |> List.map (function
        | InIfCond span -> sprintf "in if condition at %s" (formatSpan span)
        | InIfThen span -> sprintf "in if then-branch at %s" (formatSpan span)
        // ... other cases
        // Add: InCheckMode, InSynthMode formatting
    )
```

### Expected Enhanced Error Output
```
// Before (current):
error[E0301]: Type mismatch: expected int but got bool
 --> <expr>:1:1-5
   = hint: Check that all branches of your expression return the same type

// After (enhanced):
error[E0301]: Type mismatch: expected int but got bool
 --> <expr>:1:1-5
   = note: expected int due to type annotation at <expr>:1:1-12
   = hint: Check that the expression type matches the annotation
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Generic "type mismatch" | "expected X due to annotation" | Elm, Rust pioneered | Users understand WHY type was expected |
| Single error span | Multi-span with context | Rust 1.0+ (2015) | Shows both error and related locations |
| No blame assignment | Bidirectional blame | 2013+ research | Errors point to actual culprit |
| Compiler-centric messages | User-centric explanations | Elm 0.16+ | "Here's what I expected and why" |

**Current best practice (2026):**
- Show expected vs. actual types
- Explain WHY that type was expected (annotation, function signature, etc.)
- Point to both error location and source of expectation
- Provide actionable hints

**FunLang already has:**
- Multi-span errors with secondary locations
- Context stack notes
- Hints for common errors

**Phase 5 adds:**
- Mode-aware context (checking vs synthesis)
- "due to annotation" explanations
- Enhanced hints for annotation errors

## Open Questions

### Question 1: How Verbose Should "Due To" Messages Be?

**What we know:**
- "expected int due to annotation" is clear
- Elm and Rust include detailed context
- Too much context can overwhelm

**What's unclear:**
- Should we always show annotation location?
- Should we distinguish user annotations from inferred expectations?

**Recommendation:**
- Show "due to annotation at X" only for explicit user annotations
- Keep other cases as "expected X" without extra explanation
- Test with real users if possible

### Question 2: Should InSynthMode Be Tracked?

**What we know:**
- ERR-01 mentions both InCheckMode and InSynthMode
- Most errors occur in synthesis mode already
- InSynthMode adds context without changing messages

**What's unclear:**
- Does tracking synthesis mode add value?
- Would "inferring type" notes help users?

**Recommendation:**
- Start with InCheckMode only (higher value)
- Add InSynthMode later if error messages are confusing
- Keep implementation simple

### Question 3: Which Existing Tests Need Updates?

**What we know:**
- 15 type-error fslit tests exist
- tests 13, 14, 15 involve annotations
- Error format will change for annotation cases

**What's unclear:**
- Exact new output format for each test
- Whether non-annotation tests are affected

**Recommendation:**
- Run tests first to see which fail
- Update annotation tests (13, 14, 15) with enhanced format
- Keep non-annotation tests unchanged if possible

## Sources

### Primary (HIGH confidence)
- `FunLang/Diagnostic.fs` - Existing error infrastructure, InferContext definition
- `FunLang/Bidir.fs` - Current bidirectional checker, where errors originate
- `FunLang/Type.fs` - Type formatting functions
- `.planning/phases/03-bidirectional-core/03-RESEARCH.md` - Prior research on bidirectional typing

### Secondary (MEDIUM confidence)
- [Reconstructing TypeScript, part 1](https://jaked.org/blog/2021-09-15-Reconstructing-TypeScript-part-1) - Bidirectional typing and error messages
- [Simple Bidirectional Type Inference](https://ettolrach.com/blog/bidirectional_inference.html) - Error reporting structure with CheckedWrongType
- [ty type context issue](https://github.com/astral-sh/ty/issues/168) - Modern bidirectional checking context

### Tertiary (LOW confidence)
- [Rust E0308 error format](https://users.rust-lang.org/t/solved-why-do-i-get-error-e0308-mismatched-types-note-expected-type-found-type-c-c/27563) - Expected/found type message style
- [Understanding Elm's Type Mismatch Error](https://thoughtbot.com/blog/understanding-elms-type-mismatch-error) - User-centric error explanations

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All infrastructure exists and is well-understood
- Architecture: HIGH - Simple extension of existing patterns
- Pitfalls: HIGH - Based on actual codebase and error handling experience
- Code examples: HIGH - All examples from working FunLang code

**Research date:** 2026-02-04
**Valid until:** 2026-03-04 (30 days - stable domain, implementation-focused)

**Key insight:** Phase 5 is an enhancement phase, not a feature phase. The core work is enriching context information, not building new infrastructure. The existing Diagnostic system is already well-designed; we're adding mode-awareness to make messages more helpful.
