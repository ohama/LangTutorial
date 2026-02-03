# Phase 4: Output & Testing - Research

**Researched:** 2026-02-03
**Domain:** Compiler error message formatting, type pretty-printing, golden testing
**Confidence:** HIGH

## Summary

Phase 4 transforms the rich diagnostic infrastructure built in Phases 1-3 into user-friendly error messages and validates the entire v5.0 diagnostic system through comprehensive testing. The phase has two main components: (1) formatting diagnostics for human consumption with proper type variable normalization, location display, and helpful hints; and (2) establishing a golden test framework to ensure diagnostic output stability.

The current implementation already has the Diagnostic type with all necessary fields (Code, Message, PrimarySpan, SecondarySpans, Notes, Hint), error codes E0301-E0304, and typeErrorToDiagnostic conversion. What remains is: improving the CLI output to render the full diagnostic structure, normalizing type variables for readable display, and creating comprehensive tests for each error scenario.

**Primary recommendation:** Implement a `formatDiagnostic` function that renders the full Diagnostic structure in Rust/Elm-inspired format, add type variable normalization to `formatType`, update CLI to use `typecheckWithDiagnostic`, and add golden tests using fslit's existing `--- Output:` sections.

## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| FSharp.Core | 9.0 | String formatting, StringBuilder | Built-in, sufficient for text generation |
| Existing Diagnostic.fs | N/A | Diagnostic type, typeErrorToDiagnostic | Already implemented in Phase 2-3 |
| fslit | Current | Golden test framework | Already used for 98 tests in project |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| System.Console | N/A | ANSI color output (optional) | Future enhancement for colored errors |
| Argu | Current | CLI argument parsing | Already integrated, use for potential --verbose flag |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Custom formatter | sprintf chains | StringBuilder more efficient for multi-line output |
| Plain text errors | ANSI colored output | Simpler first, add colors later |
| Expecto golden | fslit golden | fslit already covers CLI E2E, Expecto for unit tests |

**No new packages needed** - all formatting can be done with F# standard library.

## Architecture Patterns

### Recommended Project Structure
```
FunLang/
├── Diagnostic.fs     # Existing: types + typeErrorToDiagnostic (Phase 2-3)
│                     # Add: formatDiagnostic, normalizeTypeVars
├── Type.fs           # Existing: formatType
│                     # Modify: formatTypeNormalized for a,b,c output
├── TypeCheck.fs      # Existing: typecheck, typecheckWithDiagnostic
├── Program.fs        # Modify: use typecheckWithDiagnostic, formatDiagnostic
└── Cli.fs            # No changes needed

tests/
├── type-errors/      # Existing: 10 tests
│                     # Add: golden tests for new format (~15 more)
└── diagnostics/      # NEW: comprehensive diagnostic output tests
```

### Pattern 1: Diagnostic Formatter Function
**What:** Single function that renders Diagnostic to multi-line string
**When to use:** In CLI when displaying type errors
**Example:**
```fsharp
// Diagnostic.fs
let formatDiagnostic (diag: Diagnostic) : string =
    let sb = System.Text.StringBuilder()

    // Error header: error[E0301]: Type mismatch
    match diag.Code with
    | Some code -> sb.AppendLine(sprintf "error[%s]: %s" code diag.Message) |> ignore
    | None -> sb.AppendLine(sprintf "error: %s" diag.Message) |> ignore

    // Primary location: --> file.fun:2:5
    sb.AppendLine(sprintf " --> %s" (formatSpan diag.PrimarySpan)) |> ignore

    // Secondary spans (related locations)
    for (span, label) in diag.SecondarySpans do
        sb.AppendLine(sprintf "   = %s: %s" label (formatSpan span)) |> ignore

    // Notes (context stack, trace)
    for note in diag.Notes do
        sb.AppendLine(sprintf "   = note: %s" note) |> ignore

    // Hint
    match diag.Hint with
    | Some hint -> sb.AppendLine(sprintf "   = hint: %s" hint) |> ignore
    | None -> ()

    sb.ToString().TrimEnd()
```

### Pattern 2: Type Variable Normalization
**What:** Rename type variables to sequential a, b, c for readable output
**When to use:** When formatting types for error messages
**Example:**
```fsharp
// Type.fs
let formatTypeNormalized (ty: Type) : string =
    // Collect all type variables in order of first appearance
    let rec collectVars acc = function
        | TVar n -> if List.contains n acc then acc else acc @ [n]
        | TArrow(t1, t2) -> collectVars (collectVars acc t1) t2
        | TTuple ts -> List.fold collectVars acc ts
        | TList t -> collectVars acc t
        | TInt | TBool | TString -> acc

    let vars = collectVars [] ty
    // Map: original var -> normalized index (0='a, 1='b, ...)
    let varMap = vars |> List.mapi (fun i v -> (v, i)) |> Map.ofList

    let rec format = function
        | TInt -> "int"
        | TBool -> "bool"
        | TString -> "string"
        | TVar n ->
            match Map.tryFind n varMap with
            | Some idx -> sprintf "'%c" (char (97 + idx % 26))
            | None -> sprintf "'%c" (char (97 + n % 26))
        | TArrow(t1, t2) ->
            let left = match t1 with TArrow _ -> sprintf "(%s)" (format t1) | _ -> format t1
            sprintf "%s -> %s" left (format t2)
        | TTuple ts -> ts |> List.map format |> String.concat " * "
        | TList t -> sprintf "%s list" (format t)

    format ty
```

### Pattern 3: Error Output Format (Rust-inspired)
**What:** Multi-line diagnostic display format
**When to use:** CLI error output
**Example output:**
```
error[E0301]: Type mismatch: expected int but got bool
 --> test.fun:3:10-14
   = in if condition: test.fun:3:4-20
   = note: in if condition at test.fun:3:4
   = hint: Check that all branches of your expression return the same type
```

### Anti-Patterns to Avoid
- **Single-line cramming:** Don't put all info on one line - hard to scan
- **Raw type variable IDs:** Don't show TVar 1003 - users see 'a, 'b, 'c
- **Missing context:** Don't show "Type mismatch" without expected/actual
- **Unicode box drawing:** Avoid fancy chars that break in some terminals

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Type var collection | Manual recursion per use | Single collectVars helper | Consistent ordering, reusable |
| Multi-line strings | sprintf + "\n" chains | StringBuilder | Performance, cleaner code |
| Golden test comparison | Custom diff tool | fslit's built-in comparison | Already handles newlines, whitespace |
| Error code registry | Hardcoded strings | Pattern match in typeErrorToDiagnostic | Single source of truth |

**Key insight:** The existing typeErrorToDiagnostic function already maps error kinds to codes/messages. Phase 4 extends this with proper formatting, not replacement.

## Common Pitfalls

### Pitfall 1: Type Variable ID Leakage
**What goes wrong:** Error messages show TVar 1003 instead of 'a
**Why it happens:** formatType uses raw variable index, inference starts at 1000
**How to avoid:** Use formatTypeNormalized for all user-facing output, normalize before display
**Warning signs:** Test output shows 'u, 'v, 'w instead of 'a, 'b, 'c

### Pitfall 2: Span Format Inconsistency
**What goes wrong:** Some spans show "file:1:1-5", others show "file:1:1-1:5"
**Why it happens:** formatSpan logic for single-line vs multi-line differs
**How to avoid:** Existing formatSpan handles this; ensure all paths use it
**Warning signs:** Golden tests fail due to format differences

### Pitfall 3: Golden Test Brittleness
**What goes wrong:** Tests break on minor whitespace/wording changes
**Why it happens:** Exact string matching is sensitive
**How to avoid:**
  - Use `--- Output:` sections carefully (fslit is exact-match)
  - Consider separate tests for format vs content
  - Document expected format explicitly
**Warning signs:** Many test failures after minor message tweaks

### Pitfall 4: Context Stack Overflow in Display
**What goes wrong:** Deep nesting produces 20+ "note:" lines
**Why it happens:** Context stack captures every inference step
**How to avoid:** Already limited to 3 secondary spans (Phase 3 decision)
**Warning signs:** Error output exceeds terminal height

### Pitfall 5: Missing Error Cases
**What goes wrong:** Some TypeError kinds not tested
**Why it happens:** Test coverage focuses on common cases
**How to avoid:** Systematic test for each TypeErrorKind: UnifyMismatch, OccursCheck, UnboundVar, NotAFunction
**Warning signs:** Code paths without test coverage

## Code Examples

Verified patterns for Phase 4 implementation:

### CLI Integration Pattern
```fsharp
// Program.fs - Replace existing typecheck usage
elif results.Contains Emit_Type && results.Contains Expr then
    let expr = results.GetResult Expr
    try
        let ast = parse expr "<expr>"
        match typecheckWithDiagnostic ast with
        | Ok ty ->
            printfn "%s" (formatTypeNormalized ty)
            0
        | Error diag ->
            eprintfn "%s" (formatDiagnostic diag)
            1
    with ex ->
        eprintfn "Error: %s" ex.Message
        1
```

### Type Variable Normalization Test
```fsharp
// Expected behavior:
// Input: TArrow(TVar 1000, TArrow(TVar 1001, TVar 1000))
// Output: "'a -> 'b -> 'a"
// NOT: "'u -> 'v -> 'u" (raw high indices)

test "normalize type variables to a,b,c" {
    let ty = TArrow(TVar 1000, TArrow(TVar 1001, TVar 1000))
    let formatted = formatTypeNormalized ty
    Expect.equal formatted "'a -> 'b -> 'a" "should use normalized names"
}
```

### Diagnostic Format Test
```fsharp
// fslit golden test format
// tests/diagnostics/01-type-mismatch-format.flt
// Type mismatch error with full diagnostic format
// --- Command: dotnet run --project FunLang -- --emit-type %input 2>&1
// --- ExitCode: 1
// --- Input:
if true then 1 else "hello"
// --- Output:
error[E0301]: Type mismatch: expected int but got string
 --> <input>:1:20-27
   = note: in if else-branch at <input>:1:1-27
   = hint: Check that all branches of your expression return the same type
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Simple "Type mismatch" | Full diagnostic with location, context, hints | v5.0 Phase 2-4 | Much better UX |
| Raw type var indices | Normalized 'a, 'b, 'c | Industry standard | Readable output |
| Single error line | Multi-line Rust/Elm style | Rust 1.12 (2016), widespread 2020+ | Better scannability |

**Current best practices (from Rust/Elm):**
- Error code for searchability (E0301)
- Primary span with arrow indicator (-->)
- Secondary spans for related context (=)
- Notes for additional explanation
- Hints for suggested fixes

**Deprecated/outdated:**
- Single-line error messages: Rust moved away in 2016
- No location info: F# always shows locations
- Generic examples only: Elm pioneered mixing with user code

## Open Questions

Things that couldn't be fully resolved:

1. **Color support in error output**
   - What we know: ANSI colors improve readability
   - What's unclear: Should Phase 4 include colors or defer?
   - Recommendation: Defer colors to future enhancement, focus on structure first

2. **Source code snippet display**
   - What we know: Rust shows actual source lines with underlines
   - What's unclear: Do we have source text available at error time?
   - Recommendation: Out of scope for Phase 4; would require storing source in AST or separate lookup

3. **Error message localization**
   - What we know: Messages are hardcoded in English
   - What's unclear: Is i18n needed for tutorial project?
   - Recommendation: Not needed for tutorial; document as future possibility

## Sources

### Primary (HIGH confidence)
- Existing codebase: Diagnostic.fs, Type.fs, Infer.fs, Unify.fs
- ROADMAP.md requirements OUT-01 through TEST-06
- Prior Phase decisions in STATE.md (context stack ordering, secondary span limits)

### Secondary (MEDIUM confidence)
- [Rust RFC 1644 - Default and expanded errors](https://rust-lang.github.io/rfcs/1644-default-and-expanded-rustc-errors.html) - Error format design principles
- [Shape of Errors to Come - Rust Blog](https://blog.rust-lang.org/2016/08/10/Shape-of-errors-to-come/) - Rust error message philosophy
- [Writing Good Compiler Error Messages](https://calebmer.com/2019/07/01/writing-good-compiler-error-messages.html) - Writing style guidelines
- [F# Compiler Diagnostics Guide](https://fsharp.github.io/fsharp-compiler-docs/diagnostics.html) - F# compiler message infrastructure

### Tertiary (LOW confidence)
- [Elm error messages style discussion](https://discourse.elm-lang.org/t/error-messages-style/7828) - Community discussion
- [Comparing Compiler Errors](https://www.amazingcto.com/developer-productivity-compiler-errors/) - Language comparison
- [Golden Testing with Hspec-golden](https://www.stackbuilders.com/insights/golden-testing-with-hspec-golden/) - Golden test patterns

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Using existing F# stdlib and project patterns
- Architecture: HIGH - Follows established Rust/Elm patterns, builds on Phase 2-3
- Pitfalls: HIGH - Based on existing codebase analysis and known issues

**Research date:** 2026-02-03
**Valid until:** 2026-03-03 (30 days - stable domain)
