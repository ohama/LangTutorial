# Phase 7: CLI Options & File-Based Tests - Research

**Researched:** 2026-01-30
**Domain:** F# CLI argument parsing, file I/O, and compiler emit options
**Confidence:** HIGH

## Summary

This phase extends the FunLang interpreter with multiple input methods (file and expression) and compiler emit options for inspecting intermediate representations (tokens and AST). The research focused on three primary domains: F# CLI argument parsing patterns, file-based testing with fslit, and pretty-printing for diagnostic output.

For CLI parsing, the standard approach for simple CLIs is array pattern matching on `argv`, which F# handles elegantly with `[|pattern|]` syntax. For more complex needs, Argu (declarative discriminated union-based parser) or System.CommandLine (.NET's official library) are the standard choices. Since this phase requires simple flag parsing (`--expr`, `--emit-tokens`, `--emit-ast`, positional file arguments), pattern matching is sufficient and keeps dependencies minimal.

For file-based testing, fslit is a dedicated F# test runner modeled after LLVM's lit tool, designed specifically for compiler/interpreter testing. It uses `.flt` files with three sections: Command, Input, and Output. This aligns perfectly with the Phase 7 requirement to test emit options against expected outputs.

Pretty-printing discriminated unions (tokens and AST) is handled by F#'s built-in `%A` formatter with `sprintf` or `printfn`, which automatically produces human-readable structured output with proper indentation. For custom formatting, pattern matching over union cases provides complete control.

**Primary recommendation:** Use array pattern matching for CLI parsing, fslit for file-based testing, and `%A` formatter for emit output. Keep it simple—no external CLI parsing libraries needed for this phase.

## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| System.IO | .NET 10 BCL | File reading | Built-in, `File.ReadAllText` is idiomatic |
| sprintf/printfn | F# core | Formatted output | `%A` formatter handles discriminated unions perfectly |
| fslit | 0.2.0 | File-based testing | Purpose-built for compiler/interpreter testing |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Argu | latest (6.x) | CLI parsing | When CLI grows beyond 5-6 flags |
| System.CommandLine | .NET library | CLI parsing | When need tab completion or .NET tooling consistency |
| Expecto | existing | Unit testing | Already in project (Phase 6), continue using |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Pattern matching | Argu | Argu adds dependency, overkill for <10 flags |
| Pattern matching | System.CommandLine | Better for complex CLIs, but heavier than needed |
| fslit | Manual golden files | fslit automates comparison, variables like `%input` |
| `%A` formatter | Custom toString | Custom gives control but loses automatic indentation |

**Installation:**
```bash
# For future use (not needed yet for simple pattern matching)
dotnet add package Argu
dotnet add package System.CommandLine

# For fslit (file-based testing)
git clone https://github.com/ohama/fslit.git
cd fslit
dotnet build FsLit/FsLit.fsproj
# Or: make build
```

## Architecture Patterns

### Recommended CLI Structure
```
FunLang/Program.fs
├── CLI parsing (match argv)
├── Input loading (file vs --expr)
├── Emit flags processing
└── Pipeline execution (lex → parse → eval)
```

### Pattern 1: Array Pattern Matching for CLI Arguments
**What:** Use F#'s array pattern matching with `[|...|]` syntax to destructure `argv` into recognized argument patterns.

**When to use:** Simple CLIs with <10 flags and straightforward argument combinations.

**Example:**
```fsharp
// Source: https://fsharpforfunandprofit.com/posts/pattern-matching-command-line/
[<EntryPoint>]
let main argv =
    match argv with
    | [| "--expr"; expr |] ->
        // Evaluate expression directly

    | [| "--emit-tokens"; "--expr"; expr |] ->
        // Parse and emit tokens

    | [| "--emit-ast"; "--expr"; expr |] ->
        // Parse and emit AST

    | [| filename |] when File.Exists filename ->
        // Read and evaluate file

    | [| "--emit-tokens"; filename |] when File.Exists filename ->
        // Read file, emit tokens

    | _ ->
        eprintfn "Usage: funlang [--emit-tokens|--emit-ast] (--expr <expr> | <file>)"
        1
```

### Pattern 2: Guard Clauses with File.Exists
**What:** Use `when` guards in pattern matching to validate file existence before processing.

**When to use:** When distinguishing between file arguments and invalid input.

**Example:**
```fsharp
// Source: https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/match-expressions
match argv with
| [| path |] when File.Exists path ->
    let content = File.ReadAllText path
    // process content
| [| path |] ->
    eprintfn "File not found: %s" path
    1
```

### Pattern 3: OR Patterns for Multiple Flags
**What:** Use `|` to combine multiple patterns that should execute the same code path.

**When to use:** When flags have aliases (like `--help` and `-h`) or multiple flags trigger the same behavior.

**Example:**
```fsharp
// Source: https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/pattern-matching
match argv with
| [| "--help" |] | [| "-h" |] ->
    printUsage()
    0
| [| "--emit-tokens" |] | [| "--emit-ast" |] | [| "--emit-type" |] ->
    eprintfn "Emit option requires input (--expr or filename)"
    1
```

### Pattern 4: fslit Test File Structure
**What:** Create `.flt` files with Command, Input, and Output sections for file-based testing.

**When to use:** Testing compiler/interpreter behavior with expected output validation.

**Example:**
```flt
// Source: https://github.com/ohama/fslit
// --- Command: funlang --emit-tokens --expr "2 + 3"
// --- Output:
NUMBER(2) PLUS NUMBER(3) EOF
```

### Pattern 5: Pretty-Printing with %A Formatter
**What:** Use `sprintf "%A"` or `printfn "%A"` to automatically format discriminated unions.

**When to use:** Default for emit output; provides structured formatting with indentation.

**Example:**
```fsharp
// Source: https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/plaintext-formatting
type Token =
    | NUMBER of int
    | PLUS
    | EOF

let tokens = [NUMBER 2; PLUS; NUMBER 3; EOF]
printfn "%A" tokens
// Output: [NUMBER 2; PLUS; NUMBER 3; EOF]

// For custom formatting:
let rec formatTokens tokens =
    tokens
    |> List.map (function
        | NUMBER n -> sprintf "NUMBER(%d)" n
        | PLUS -> "PLUS"
        | EOF -> "EOF")
    |> String.concat " "
```

### Anti-Patterns to Avoid

- **Over-engineering CLI parsing:** Don't add Argu or System.CommandLine until you have >10 flags or need subcommands. Pattern matching is sufficient and has zero dependencies.

- **Ignoring file existence:** Don't attempt `File.ReadAllText` without checking existence first. Use `when File.Exists` guards to provide clear error messages.

- **Manual string building for discriminated unions:** Don't hand-write formatters when `%A` already produces readable output. Only customize when specific format is required.

- **Hardcoded test expectations in unit tests:** Don't embed expected emit output as string literals. Use fslit's file-based approach to keep tests maintainable.

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| CLI flag parsing | Recursive list parser | Pattern matching on argv array | Built-in syntax, exhaustiveness checking, no dependencies |
| File reading | StreamReader/char arrays | `File.ReadAllText(path)` | Handles encoding detection, closes handles, single line |
| Pretty-printing unions | Custom toString() | `sprintf "%A"` / `printfn "%A"` | Automatic indentation, handles nesting, compile-time safety |
| Golden file testing | Manual diff scripts | fslit | Variables like `%input`, automatic comparison, LLVM lit compatibility |
| Test output comparison | String.Equals line-by-line | fslit's Output section | Handles whitespace, clear diffs, tooling support |

**Key insight:** F#'s pattern matching and formatted printing are powerful enough for this phase's needs. External libraries add complexity without benefit until the CLI grows significantly more complex. fslit is the exception—it's purpose-built for exactly this use case.

## Common Pitfalls

### Pitfall 1: Argument Order Dependency
**What goes wrong:** Pattern matching is order-sensitive. Placing `[| filename |]` before `[| "--expr"; expr |]` causes flags to be treated as filenames.

**Why it happens:** Pattern matching evaluates top-to-bottom and takes the first match. Generic patterns shadow specific ones.

**How to avoid:** Order patterns from most specific to least specific. Put flag combinations before positional arguments.

**Warning signs:** Flags like `--expr` result in "File not found: --expr" errors.

**Example:**
```fsharp
// WRONG - positional argument shadows flags
match argv with
| [| arg |] -> (* This matches "--expr" as filename! *)
| [| "--expr"; expr |] -> (* Never reached *)

// RIGHT - flags before positional
match argv with
| [| "--expr"; expr |] ->
| [| "--emit-tokens"; "--expr"; expr |] ->
| [| filename |] when File.Exists filename ->
```

### Pitfall 2: File.Exists Race Condition
**What goes wrong:** Checking `File.Exists` in `when` guard, then reading file later allows file deletion between check and read.

**Why it happens:** File system is mutable; time-of-check-time-of-use (TOCTOU) race condition.

**How to avoid:** For production code, catch `FileNotFoundException` instead of pre-checking. For tutorial code, accept the race as rare.

**Warning signs:** Works in testing but fails intermittently in real use.

**Example:**
```fsharp
// Tutorial-acceptable approach (simple, rare race)
| [| filename |] when File.Exists filename ->
    let content = File.ReadAllText filename  // Could fail if deleted

// Production approach (robust)
| [| filename |] ->
    try
        let content = File.ReadAllText filename
        // process content
    with
    | :? FileNotFoundException ->
        eprintfn "File not found: %s" filename
        1
```

### Pitfall 3: Guard Applies to All OR Patterns
**What goes wrong:** Writing `| A | B when condition` applies `when condition` to both A and B, not just B.

**Why it happens:** F#'s syntax design—guards apply to the entire OR pattern group.

**How to avoid:** Be explicit about what the guard covers. Use separate match cases if guard should only apply to one pattern.

**Warning signs:** Unexpected filtering of earlier patterns in OR group.

**Example:**
```fsharp
// This guard applies to BOTH patterns!
| [| "--expr"; expr |] | [| filename |] when File.Exists filename ->
    // This requires File.Exists for BOTH "--expr" AND filename cases
    // NOT what you want!

// Separate them:
| [| "--expr"; expr |] ->
    // Process expr
| [| filename |] when File.Exists filename ->
    // Process file
```

### Pitfall 4: %A Formatter Width Default
**What goes wrong:** `%A` defaults to 80-character width, which may wrap AST output unexpectedly.

**Why it happens:** F# pretty-printer uses block indentation with 80-char limit for readability.

**How to avoid:** Use `%0A` for single-line output (width=0) or `%200A` for wider output when needed.

**Warning signs:** AST output has unexpected line breaks in the middle of expressions.

**Example:**
```fsharp
// Default 80-char width might wrap
printfn "%A" largeAST  // May have line breaks

// Single-line output
printfn "%0A" largeAST  // All on one line

// Custom width
printfn "%200A" largeAST  // Up to 200 chars per line
```

### Pitfall 5: fslit Test Isolation
**What goes wrong:** Tests that depend on external files or state may fail when run in different directories or orders.

**Why it happens:** fslit runs each test independently but doesn't sandbox file system access.

**How to avoid:** Use fslit's `%input` variable for temporary input files. Keep tests self-contained within the `.flt` file.

**Warning signs:** Tests pass individually but fail when run as suite, or pass locally but fail in CI.

**Example:**
```flt
// WRONG - depends on external file
// --- Command: funlang external-file.fun
// --- Output:
5

// RIGHT - self-contained with %input
// --- Command: funlang %input
// --- Input:
2 + 3
// --- Output:
5
```

## Code Examples

Verified patterns from official sources:

### CLI Parsing with Pattern Matching
```fsharp
// Source: https://fsharpforfunandprofit.com/posts/pattern-matching-command-line/
open System
open System.IO

[<EntryPoint>]
let main argv =
    match argv with
    // Emit options with --expr
    | [| "--emit-tokens"; "--expr"; expr |] ->
        let tokens = expr |> lex
        printfn "%s" (formatTokens tokens)
        0

    | [| "--emit-ast"; "--expr"; expr |] ->
        let ast = expr |> parse
        printfn "%A" ast  // %A formatter handles discriminated unions
        0

    // Emit options with file
    | [| "--emit-tokens"; filename |] when File.Exists filename ->
        let content = File.ReadAllText filename
        let tokens = content |> lex
        printfn "%s" (formatTokens tokens)
        0

    | [| "--emit-ast"; filename |] when File.Exists filename ->
        let content = File.ReadAllText filename
        let ast = content |> parse
        printfn "%A" ast
        0

    // Evaluate --expr (existing behavior)
    | [| "--expr"; expr |] ->
        try
            let result = expr |> parse |> eval
            printfn "%d" result
            0
        with ex ->
            eprintfn "Error: %s" ex.Message
            1

    // Evaluate file
    | [| filename |] when File.Exists filename ->
        try
            let content = File.ReadAllText filename
            let result = content |> parse |> eval
            printfn "%d" result
            0
        with ex ->
            eprintfn "Error: %s" ex.Message
            1

    // File not found
    | [| filename |] ->
        eprintfn "File not found: %s" filename
        1

    // Help
    | [| "--help" |] | [| "-h" |] ->
        printUsage()
        0

    // Unrecognized
    | _ ->
        eprintfn "Usage: funlang [--emit-tokens|--emit-ast] (--expr <expr> | <file>)"
        1
```

### File Reading
```fsharp
// Source: https://learn.microsoft.com/en-us/dotnet/api/system.io.file.readalltext
open System.IO

let readProgram filename =
    if File.Exists filename then
        Ok (File.ReadAllText filename)
    else
        Error (sprintf "File not found: %s" filename)
```

### Token Formatting
```fsharp
// Source: https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/plaintext-formatting
// Custom formatter (if %A doesn't match expected format)
let formatToken = function
    | NUMBER n -> sprintf "NUMBER(%d)" n
    | PLUS -> "PLUS"
    | MINUS -> "MINUS"
    | STAR -> "STAR"
    | SLASH -> "SLASH"
    | LPAREN -> "LPAREN"
    | RPAREN -> "RPAREN"
    | EOF -> "EOF"

let formatTokens tokens =
    tokens
    |> List.map formatToken
    |> String.concat " "

// Or use built-in %A for default formatting
let formatTokensSimple tokens =
    sprintf "%A" tokens
```

### fslit Test Examples
```flt
// Source: https://github.com/ohama/fslit

// Test 1: Basic arithmetic with --expr
// --- Command: funlang --expr "2 + 3"
// --- Output:
5

// Test 2: Token emission
// --- Command: funlang --emit-tokens --expr "2 + 3"
// --- Output:
NUMBER(2) PLUS NUMBER(3) EOF

// Test 3: AST emission
// --- Command: funlang --emit-ast --expr "2 + 3"
// --- Output:
Add (Number 2, Number 3)

// Test 4: File-based input using %input variable
// --- Command: funlang %input
// --- Input:
(2 + 3) * 4
// --- Output:
20

// Test 5: File with --emit-ast
// --- Command: funlang --emit-ast %input
// --- Input:
2 + 3 * 4
// --- Output:
Add (Number 2, Mul (Number 3, Number 4))
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Recursive list parsing | Array pattern matching | F# 2.0+ | Simpler, exhaustiveness checking, less boilerplate |
| StreamReader + char loops | `File.ReadAllText` | .NET 2.0+ | One-liner, handles encoding automatically |
| Manual diff scripts | Golden file testing (lit, fslit) | 2010s (LLVM lit) | Automated, variables, clear diff output |
| Custom ToString() | `%A` formatter | F# 1.0 | Automatic indentation, handles all types |
| CommandLineParser (C#) | Argu (F#-native) | 2013+ | Discriminated unions, type-safe, F# idioms |
| N/A | System.CommandLine | 2020+ (still preview) | Official .NET, tab completion, modern API |

**Deprecated/outdated:**
- **Recursive list parsing with cons pattern:** While still valid, array pattern matching is more direct for `argv` (which is an array, not a list)
- **Custom parser combinators for simple CLIs:** FParsec and similar are for parsing input languages, not CLI arguments. Use pattern matching or Argu.
- **System.CommandLine 2.0.0-beta4.x:** Still in preview as of 2026. For production, use stable Argu or simple pattern matching.

## Open Questions

Things that couldn't be fully resolved:

1. **fslit integration with dotnet test**
   - What we know: fslit is a standalone test runner (separate binary), modeled after LLVM lit
   - What's unclear: Whether fslit integrates with `dotnet test` or runs independently
   - Recommendation: Plan for standalone execution (`fslit test-dir/`). Can investigate test adapter integration in future phases if needed.

2. **Exact token/AST format for emit options**
   - What we know: `%A` formatter produces readable output like `[NUMBER 2; PLUS; NUMBER 3; EOF]`
   - What's unclear: Whether tutorial expects custom format like `NUMBER(2) PLUS NUMBER(3) EOF` or is fine with default `%A` output
   - Recommendation: Start with `%A` formatter for simplicity. If specific format needed, implement custom formatter using pattern matching. Document expected format in fslit test files as source of truth.

3. **Reserved --emit-type behavior**
   - What we know: Phase 7 reserves `--emit-type` for future phase (type checking not yet implemented)
   - What's unclear: Should it silently ignore, show placeholder, or error when used?
   - Recommendation: Return error with message "Type checking not yet implemented. Reserved for future phase." Clear feedback is better than silent success or confusion.

## Sources

### Primary (HIGH confidence)
- [Microsoft Learn - F# Plain Text Formatting](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/plaintext-formatting) - %A formatter, sprintf usage
- [Microsoft Learn - F# Pattern Matching](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/pattern-matching) - Array patterns, guards, OR patterns
- [Microsoft Learn - File.ReadAllText](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.readalltext) - File I/O
- [Microsoft Learn - System.CommandLine](https://learn.microsoft.com/en-us/dotnet/standard/commandline/) - Official .NET CLI parsing
- [GitHub - ohama/fslit](https://github.com/ohama/fslit) - File-based testing tool
- [Argu Documentation](https://fsprojects.github.io/Argu/) - F# CLI parser

### Secondary (MEDIUM confidence)
- [F# for Fun and Profit - Pattern Matching CLI](https://fsharpforfunandprofit.com/posts/pattern-matching-command-line/) - Verified against official docs
- [Rust Compiler Development Guide - MIR](https://rustc-dev-guide.rust-lang.org/mir/index.html) - Emit options design patterns (cross-language reference)
- [Introduction to Golden Testing](https://ro-che.info/articles/2017-12-04-golden-tests) - Golden file testing best practices

### Tertiary (LOW confidence)
- WebSearch results for golden file testing across languages - General patterns, not F#-specific
- Community blog posts on F# CLI parsing - Supplementary examples

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Built-in F# features (pattern matching, %A, File.ReadAllText) are well-documented and stable. fslit is verified from source repository.
- Architecture: HIGH - Official Microsoft docs for pattern matching, verified examples from F# for Fun and Profit cross-referenced.
- Pitfalls: MEDIUM - Common pitfalls derived from official docs (OR pattern guards, TOCTOU) and general software engineering knowledge. fslit pitfall inferred from LLVM lit behavior.

**Research date:** 2026-01-30
**Valid until:** 2026-03-01 (30 days, stable domain—F# core features unlikely to change)
