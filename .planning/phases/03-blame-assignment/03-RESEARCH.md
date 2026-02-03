# Phase 3: Blame Assignment - Research

**Researched:** 2026-02-03
**Domain:** Type error localization and blame assignment in Hindley-Milner type inference
**Confidence:** MEDIUM

## Summary

Blame assignment determines which expressions in a type error are most directly responsible for the failure, distinguishing between the primary cause and contributing factors. Research reveals this is a well-studied problem in type systems with two main approaches: (1) heuristic-based selection using "innermost expression" and unification trace analysis, and (2) constraint-solving approaches (like SHErrLoc) that analyze unsatisfiable constraint paths.

For FunLang's Phase 3, the heuristic approach is appropriate: use the Term field in TypeError as the primary blame location (already captured at inference site), and extract secondary spans from ContextStack (related expressions) and Trace (type structure locations). The key insight from Rust and Elm compilers is that primary spans should be self-contained (understandable alone in an IDE), while secondary spans provide "why" context.

**Primary recommendation:** Populate SecondarySpans by extracting spans from ContextStack entries (to show inference path through code) and optionally from Terms referenced in Trace (to show related type origins). Primary span remains TypeError.Span (already the most direct cause).

## Standard Stack

FunLang already has the necessary infrastructure from Phase 2. No external libraries needed.

### Core
| Component | Location | Purpose | Why Standard |
|-----------|----------|---------|--------------|
| TypeError.Term | Diagnostic.fs | Stores the problematic expression | Captures innermost expression at error site |
| TypeError.ContextStack | Diagnostic.fs | InferContext list (14 cases) | Tracks inference path through code |
| TypeError.Trace | Diagnostic.fs | UnifyPath list (4 cases) | Tracks structural location in types |
| Span extraction | Ast.fs | spanOf helper | Every Expr/Pattern has span |

### Supporting
| Component | Location | Purpose | When to Use |
|-----------|----------|---------|-------------|
| Diagnostic.SecondarySpans | Diagnostic.fs | (Span * string) list | Currently empty, Phase 3 populates this |
| formatContextStack | Diagnostic.fs | Format context to strings | Already reverses inner-first to outer-first |
| formatTrace | Diagnostic.fs | Format trace to strings | Already reverses inner-first to outer-first |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Heuristic (ContextStack) | SHErrLoc constraint solver | SMT solver is overkill for FunLang's size; adds complexity |
| Multiple secondary spans | Only primary span | Misses "why" context; users see error without understanding cause |
| Span-only labels | Include type info in labels | Spans show location, types show expectation - both needed |

**Installation:**
No additional packages required.

## Architecture Patterns

### Current Error Flow (Phase 2)
```
inferWithContext → unifyWithContext → TypeException(TypeError)
                                          ↓
                                   typeErrorToDiagnostic
                                          ↓
                                   Diagnostic { SecondarySpans = [] }
```

### Pattern 1: Primary Span Selection
**What:** Primary span is already correct - it's the span passed to unifyWithContext or the expression span where inference fails.

**When to use:** Always. TypeError.Span is set at the innermost failure point.

**Example:**
```fsharp
// From Infer.fs line 137 - If expression
let s4 = unifyWithContext ctx [] span (apply (compose s3 (compose s2 s1)) condTy) TBool
// If unification fails, span is the entire if-expression span
// This is correct as primary span
```

**Why this works:** The span parameter to unifyWithContext is always the "current expression being checked" - the innermost relevant expression.

### Pattern 2: Secondary Span Extraction from ContextStack
**What:** Extract spans from InferContext list to show inference path.

**When to use:** When ContextStack is non-empty (most errors in nested expressions).

**Example:**
```fsharp
// TypeError with context:
// ContextStack = [InAppArg span1; InIfThen span2]  // inner-first order
//
// Extract secondary spans:
let secondaryFromContext (ctx: InferContext list) : (Span * string) list =
    ctx
    |> List.rev  // Reverse to outer-first (same as formatContextStack)
    |> List.choose (function
        | InIfCond span -> Some (span, "in if condition")
        | InIfThen span -> Some (span, "in then-branch")
        | InIfElse span -> Some (span, "in else-branch")
        | InAppFun span -> Some (span, "in function position")
        | InAppArg span -> Some (span, "in argument position")
        | InLetRhs (name, span) -> Some (span, sprintf "in let %s binding" name)
        // ... etc for all 14 InferContext cases
        | _ -> None  // Skip if span equals primary span
    )
```

**Why this works:** ContextStack already captures the inference path through code. Each entry has the span of the enclosing expression.

### Pattern 3: Secondary Span Extraction from Trace
**What:** Extract type origins from UnifyPath to show "expected X here" context.

**When to use:** For UnifyMismatch errors where knowing the origin of expected/actual types helps.

**Example:**
```fsharp
// UnifyMismatch (expected: int, actual: bool)
// Trace = [AtFunctionReturn TInt]
//
// This means: "expected int because function return type is int"
// Could extract span from the function definition if we store it in Trace
```

**Challenge:** Current UnifyPath only stores types, not spans. Would need to enhance UnifyPath to track origin spans.

**Decision:** Defer this to later. Focus on ContextStack extraction first (simpler, high value).

### Pattern 4: De-duplication of Spans
**What:** Avoid showing same span as both primary and secondary.

**When to use:** Always. Primary span should not appear in secondary list.

**Example:**
```fsharp
let secondarySpans =
    extractedSpans
    |> List.filter (fun (s, _) -> s <> primarySpan)
```

### Pattern 5: Limit Secondary Spans
**What:** Show most relevant 2-3 secondary spans, not entire context.

**When to use:** When context stack is deep (>3 entries).

**Rationale:** Rust compiler limits context to avoid clutter. Too many spans confuse rather than clarify.

**Example:**
```fsharp
let secondarySpans =
    extractedSpans
    |> List.filter (fun (s, _) -> s <> primarySpan)
    |> List.truncate 3  // Limit to 3 most relevant
```

### Anti-Patterns to Avoid
- **Changing primary span selection:** TypeError.Span is already correct (innermost expression). Don't try to "improve" it by choosing parent expressions.
- **Including type details in span labels:** Keep labels location-focused ("in then-branch"), not type-focused ("expected int"). Types go in Message and Notes.
- **Reversing context order:** ContextStack is stored inner-first. Always reverse to outer-first for display (same as formatContextStack does).
- **Enhancing UnifyPath prematurely:** Don't add span tracking to UnifyPath until we see test failures that need it. Start simple.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Constraint-based error localization | Custom SMT solver integration | Heuristic extraction from ContextStack | SHErrLoc is research-grade; overkill for FunLang's language size and scope |
| Span deduplication | Custom equality checking | F# structural equality on Span record | Span is record type with structural equality |
| Context reversal | Manual list reversal logic | List.rev (built-in) | Standard library, well-tested |
| Span filtering | Nested loops and mutable state | List.choose, List.filter (functional) | Idiomatic F#, composable |

**Key insight:** Phase 2 did the hard work. Phase 3 is mostly data extraction and transformation.

## Common Pitfalls

### Pitfall 1: Left-to-Right Bias in Blame
**What goes wrong:** OCaml's type checker famously reports errors "before the actual error" due to left-to-right evaluation order.

**Why it happens:** Algorithm W processes subexpressions left-to-right. If `e1 + e2` fails, unification might blame e1 even if e2 is wrong.

**How to avoid:** FunLang already avoids this. The span passed to unifyWithContext is the parent expression span (the entire `e1 + e2`), not a subexpression span. This is correct.

**Warning signs:** Test failures where error points to wrong subexpression. Example: `[1, true]` should blame `true` (element 1), not `1` (element 0).

**Current status:** Check existing tests. In `inferBinaryOpWithContext`, spans use `spanOf e1` and `spanOf e2` for unification. This is correct - each subexpression is blamed independently.

### Pitfall 2: Duplicate Spans in Output
**What goes wrong:** Primary span appears again in secondary spans list, cluttering output.

**Why it happens:** ContextStack includes the current expression context, which might have same span as primary.

**How to avoid:** Filter secondary spans to exclude primary span (Pattern 4 above).

**Warning signs:** Test output shows same location twice with different labels.

### Pitfall 3: Too Many Secondary Spans
**What goes wrong:** Deep expression nesting creates 5-10 secondary spans, overwhelming the user.

**Why it happens:** Every nested expression adds a ContextStack entry.

**How to avoid:** Limit to 2-3 most relevant spans. Rust compiler shows ~3 spans typically.

**Warning signs:** User feedback that errors are "too noisy". Test output with >4 secondary locations.

### Pitfall 4: Empty or Uninformative Labels
**What goes wrong:** Secondary span labels like "related location" don't explain relevance.

**Why it happens:** Generic label generation without context-specific information.

**How to avoid:** Use InferContext case information to create specific labels. "in then-branch" is better than "related location".

**Warning signs:** Labels that don't help user understand why span is relevant.

### Pitfall 5: Assuming Term is Always Present
**What goes wrong:** TypeError.Term is `Expr option`. Unify.fs sets it to None (only Infer.fs sets it to Some).

**Why it happens:** Unification failures don't have direct expression context (they work on types).

**How to avoid:** Pattern match on Term and handle None case. Don't use Term.Value.

**Warning signs:** Runtime exceptions: "Option.get called on None".

**Current code:** Unify.fs line 28, 58, 62 - all set `Term = None`. Infer.fs line 88 - sets `Term = Some expr`.

## Code Examples

Verified patterns from existing codebase:

### Example 1: Current Primary Span Selection (Correct)
```fsharp
// Source: Infer.fs lines 124-129 (App case)
| App (func, arg, span) ->
    let s1, funcTy = inferWithContext (InAppFun span :: ctx) env func
    let s2, argTy = inferWithContext (InAppArg span :: ctx) (applyEnv s1 env) arg
    let resultTy = freshVar()
    let s3 = unifyWithContext ctx [] span (apply s2 funcTy) (TArrow (argTy, resultTy))
    (compose s3 (compose s2 s1), apply s3 resultTy)
// Primary span is 'span' (entire application), correct
// ContextStack includes InAppFun and InAppArg for subexpressions
```

### Example 2: Current Context Stack Building (Correct)
```fsharp
// Source: Infer.fs lines 132-141 (If case)
| If (cond, thenExpr, elseExpr, span) ->
    let s1, condTy = inferWithContext (InIfCond span :: ctx) env cond
    let s2, thenTy = inferWithContext (InIfThen span :: ctx) (applyEnv s1 env) thenExpr
    let s3, elseTy = inferWithContext (InIfElse span :: ctx) (applyEnv (compose s2 s1) env) elseExpr
    // ... unification
// Each subexpression gets its own InferContext entry
// These are available in TypeError.ContextStack for secondary spans
```

### Example 3: Extract Secondary Spans (To Implement)
```fsharp
// Source: To be added in Diagnostic.fs

/// Extract secondary spans from context stack for related expression locations
let contextToSecondarySpans (ctx: InferContext list) (primarySpan: Span) : (Span * string) list =
    ctx
    |> List.rev  // Reverse from inner-first to outer-first
    |> List.choose (fun context ->
        let span, label =
            match context with
            | InIfCond span -> (span, "in if condition")
            | InIfThen span -> (span, "in then-branch")
            | InIfElse span -> (span, "in else-branch")
            | InAppFun span -> (span, "in function position")
            | InAppArg span -> (span, "in argument position")
            | InLetRhs (name, span) -> (span, sprintf "in let %s binding" name)
            | InLetBody (name, span) -> (span, sprintf "in let %s body" name)
            | InLetRecBody (name, span) -> (span, sprintf "in let rec %s body" name)
            | InMatch span -> (span, "in match expression")
            | InMatchClause (idx, span) -> (span, sprintf "in match clause %d" idx)
            | InTupleElement (idx, span) -> (span, sprintf "in tuple element %d" idx)
            | InListElement (idx, span) -> (span, sprintf "in list element %d" idx)
            | InConsHead span -> (span, "in cons head")
            | InConsTail span -> (span, "in cons tail")
        // Filter out primary span to avoid duplication
        if span = primarySpan then None else Some (span, label)
    )
    |> List.truncate 3  // Limit to 3 most relevant spans
```

### Example 4: Update typeErrorToDiagnostic (To Implement)
```fsharp
// Source: Diagnostic.fs line 102, to be modified

/// Convert TypeError to Diagnostic for display
let typeErrorToDiagnostic (err: TypeError) : Diagnostic =
    let code, message, hint =
        match err.Kind with
        | UnifyMismatch (expected, actual) ->
            Some "E0301",
            sprintf "Type mismatch: expected %s but got %s" (formatType expected) (formatType actual),
            Some "Check that all branches of your expression return the same type"
        | OccursCheck (var, ty) ->
            Some "E0302",
            sprintf "Occurs check: cannot construct infinite type '%c = %s"
                (char (97 + var % 26))
                (formatType ty),
            Some "This usually means you're trying to define a recursive type without a base case"
        | UnboundVar name ->
            Some "E0303",
            sprintf "Unbound variable: %s" name,
            Some "Make sure the variable is defined before use"
        | NotAFunction ty ->
            Some "E0304",
            sprintf "Type %s is not a function and cannot be applied" (formatType ty),
            Some "Check that you're calling a function, not a value"

    // Build notes from context stack and trace
    let contextNotes = formatContextStack err.ContextStack
    let traceNotes = formatTrace err.Trace
    let notes = contextNotes @ traceNotes

    // NEW: Extract secondary spans from context stack
    let secondarySpans = contextToSecondarySpans err.ContextStack err.Span

    {
        Code = code
        Message = message
        PrimarySpan = err.Span
        SecondarySpans = secondarySpans  // Changed from []
        Notes = notes
        Hint = hint
    }
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Single error location (OCaml pre-4.08) | Primary + secondary spans (OCaml 4.08+, Rust, Elm) | 2018-2019 | Users can see "why" errors occur, not just "where" |
| Left-to-right blame bias | Innermost expression + context stack | Research: 2014 (SHErrLoc), Practice: 2016+ (Rust) | More accurate blame assignment |
| String-based error messages | Structured diagnostics with spans | 2016+ (Rust RFC 1644, Elm 0.18) | IDEs can highlight multiple locations |
| Single error per compilation | Error recovery with multiple diagnostics | 2015+ (most modern compilers) | FunLang has basic recovery in REPL |

**Deprecated/outdated:**
- **OCaml's original left-to-right error reporting:** Charguéraud (2015) paper showed this confuses beginners. Modern OCaml improved but still has issues.
- **Generic "type error" messages:** Elm (2016) pioneered helpful, specific messages. Now industry standard.
- **Single-location error reporting:** Rust's multi-span diagnostics (2016) set new standard.

**Current best practices (2026):**
- Primary span = most direct cause (innermost relevant expression)
- Secondary spans = contributing context (2-3 related locations)
- Labels = location-aware, concise explanations
- Notes = additional textual context (traces, suggestions)

## Open Questions

1. **Should we enhance UnifyPath with span tracking?**
   - What we know: Current UnifyPath stores types (AtFunctionParam TInt), not spans
   - What's unclear: Would "function parameter expected int here [span]" significantly help users?
   - Recommendation: Defer. Implement ContextStack extraction first, evaluate from test feedback. If test cases show need for type-origin spans, revisit.

2. **How many secondary spans is optimal?**
   - What we know: Rust shows ~2-3, Elm shows variable count, too many = clutter
   - What's unclear: FunLang's expression nesting depth in typical errors
   - Recommendation: Start with 3-span limit (Pattern 5), adjust based on test output readability.

3. **Should we deduplicate spans with same location but different labels?**
   - What we know: If-expression span might appear as both "in if condition" and "in if expression"
   - What's unclear: Does this happen in practice? Is it helpful or confusing?
   - Recommendation: Implement simple span equality check first, enhance if tests show label conflicts.

4. **Should UnboundVar errors include secondary spans?**
   - What we know: UnboundVar has empty ContextStack (fails at Var lookup, line 86)
   - What's unclear: Would showing enclosing expression help? ("undefined x in function position")
   - Recommendation: ContextStack might be non-empty (e.g., InAppFun :: ctx). Extract normally; if empty, no secondary spans shown.

## Sources

### Primary (HIGH confidence)
- FunLang/Diagnostic.fs - TypeError and Diagnostic types defined in Phase 2
- FunLang/Infer.fs - inferWithContext shows context stack building pattern
- FunLang/Unify.fs - unifyWithContext shows trace building pattern
- FunLang/Ast.fs - Span type and spanOf helper
- [Rust Compiler Development Guide - Diagnostics](https://rustc-dev-guide.rust-lang.org/diagnostics.html) - Primary/secondary span design philosophy

### Secondary (MEDIUM confidence)
- [SHErrLoc: A static holistic error locator](https://www.cs.cornell.edu/projects/SHErrLoc/) - Constraint-based approach (not used, but informs understanding)
- [Understanding Elm's Type Mismatch Error](https://thoughtbot.com/blog/understanding-elms-type-mismatch-error) - Elm's approach to helpful errors
- [Common Error Messages · OCaml Documentation](https://ocaml.org/docs/common-errors) - OCaml error patterns
- [Shape of errors to come | Rust Blog](https://blog.rust-lang.org/2016/08/10/Shape-of-errors-to-come/) - Rust's error design principles (2016)

### Tertiary (LOW confidence)
- [Learning to Blame: Localizing Novice Type Errors](https://arxiv.org/pdf/1708.07583) - Academic paper on ML error localization (PDF was corrupted, couldn't extract details)
- [Improving Type Error Messages in OCaml](https://www.chargueraud.org/research/2015/ocaml_errors/ocaml_errors.pdf) - Charguéraud paper (PDF was corrupted, but title and abstract available)
- Various GitHub issues and discussions - Anecdotal evidence of error message pain points

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All components exist in Phase 2, well-documented
- Architecture: HIGH - Patterns are extrapolations from existing code in Infer.fs and Diagnostic.fs
- Pitfalls: MEDIUM - Based on research literature and Rust/Elm best practices, not FunLang-specific testing
- Implementation approach: MEDIUM - Extraction logic is straightforward, but optimal span count/labels need testing

**Research date:** 2026-02-03
**Valid until:** ~60 days (2026-04-03) - Stable domain, techniques are mature (2016-2019 era best practices still current)

**Key assumption:** Phase 2 correctly captures context and trace during inference. Research assumes ContextStack and Trace already contain the right data; Phase 3 just extracts and formats it. If Phase 2 data is incomplete, Phase 3 blame assignment will be limited.

**Success indicator for planning:** Planner can create tasks that:
1. Add contextToSecondarySpans helper function
2. Modify typeErrorToDiagnostic to call it
3. Write tests showing secondary spans for nested errors
4. Verify span deduplication works
5. Adjust span limit based on output readability
