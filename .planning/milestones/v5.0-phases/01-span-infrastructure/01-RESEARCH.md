# Phase 1: Span Infrastructure - Research

**Researched:** 2026-02-02
**Domain:** Source location tracking in lexer/parser/AST for F# compiler (fslex/fsyacc)
**Confidence:** HIGH

## Summary

This research investigates how to add source location tracking (spans) to a FunLang compiler built with fslex/fsyacc. The goal is to capture file, line, and column information for every AST node to enable quality error messages and diagnostics.

FsLexYacc follows the OCamlLex model where position information is stored in the `LexBuffer` state and propagated through parser actions using the `parseState` interface. The standard approach uses F#'s `Position` record type (containing `pos_fname`, `pos_lnum`, `pos_bol`, `pos_cnum`) to track locations, with manual updates for newlines in the lexer. Parser actions access position data via helper functions (`rhs`, `lhs`, `rhs2`) to construct AST nodes with span information.

The F# compiler itself uses a `range` type (representing start/end positions) stored in AST nodes, with a special `range0` value for synthetic/built-in definitions. Modern best practices emphasize storing byte offsets during lexing and computing line/column numbers on-demand for error reporting, though for educational compilers the simpler eager tracking is acceptable.

**Primary recommendation:** Define a `Span` record type containing filename, start Position, and end Position. Add a `span: Span` field to every `Expr` variant. Initialize `lexbuf.EndPos` in the lexer, update it on newlines, and propagate positions in parser actions using `parseState` helpers.

## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| FsLexYacc | 11.3.0+ | Lexer/parser generator for F# | Official F# tooling, follows OCamlLex model with built-in position tracking |
| FSharp.Text.Lexing | (runtime) | Position tracking types and LexBuffer | Provides `Position` record and `LexBuffer<'char>` with `.StartPos` and `.EndPos` |
| FSharp.Text.Parsing | (runtime) | Parser state with position access | Provides `IParseState` interface for accessing symbol positions in parser actions |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| N/A | - | No additional libraries needed | Built-in FsLexYacc runtime is sufficient |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| FsLexYacc Position | Custom position tracking | FsLexYacc Position is well-tested and integrates with parseState; custom tracking requires manual threading |
| Eager line/column | Lazy byte-offset only | Lazy is faster but more complex; for educational compiler, eager is simpler and sufficient |

**Installation:**
```bash
# Already installed in project
dotnet add package FsLexYacc --version 11.3.0
```

## Architecture Patterns

### Recommended Project Structure
```
FunLang/
├── Ast.fs           # Add Span type and span fields to Expr
├── Lexer.fsl        # Initialize lexbuf.EndPos, update on newlines
├── Parser.fsy       # Use parseState helpers to propagate spans
└── Eval.fs          # (no changes needed for Phase 1)
```

### Pattern 1: Span Type Definition
**What:** Define a `Span` record to hold source location information
**When to use:** Every AST node should carry span information
**Example:**
```fsharp
// Source: F# compiler Range type pattern
// https://fsharp.github.io/fsharp-compiler-docs/reference/fsharp-compiler-text-rangemodule.html

/// Represents a source location span (file, start position, end position)
type Span = {
    FileName: string
    StartLine: int
    StartColumn: int
    EndLine: int
    EndColumn: int
}

/// Create a span from FsLexYacc Position records
let mkSpan (fileName: string) (startPos: Position) (endPos: Position) : Span =
    {
        FileName = fileName
        StartLine = startPos.Line
        StartColumn = startPos.Column
        EndLine = endPos.Line
        EndColumn = endPos.Column
    }

/// Special span for unknown/built-in locations (like F# compiler's range0)
let unknownSpan : Span =
    {
        FileName = "<unknown>"
        StartLine = 0
        StartColumn = 0
        EndLine = 0
        EndColumn = 0
    }
```

### Pattern 2: AST Node with Span Field
**What:** Add span field to every Expr variant
**When to use:** All AST constructors should include span parameter
**Example:**
```fsharp
// Source: F# compiler AST pattern
// https://fsharp.github.io/fsharp-compiler-docs/fcs/untypedtree.html

type Expr =
    | Number of int * span: Span
    | Add of Expr * Expr * span: Span
    | Var of string * span: Span
    | Let of string * Expr * Expr * span: Span
    // ... other variants
```

### Pattern 3: Lexer Position Initialization
**What:** Set initial position when creating lexbuf and update on newlines
**When to use:** At lexer entry point and in newline rule
**Example:**
```fsharp
// Source: FsLexYacc documentation
// https://fsprojects.github.io/FsLexYacc/content/fslex.html

// In lexer preamble or initialization code:
let setInitialPos (lexbuf: LexBuffer<_>) (filename: string) =
    lexbuf.EndPos <- {
        pos_fname = filename
        pos_lnum = 1
        pos_bol = 0
        pos_cnum = 0
    }

// In lexer rules:
rule tokenize = parse
    | newline  { lexbuf.EndPos <- lexbuf.EndPos.AsNewLinePos()
                 tokenize lexbuf }
    | digit+   { NUMBER (Int32.Parse(lexeme lexbuf)) }
    // ... other rules
```

### Pattern 4: Parser Position Propagation
**What:** Use parseState helpers to get symbol positions and construct spans
**When to use:** In every parser action that builds AST nodes
**Example:**
```fsharp
// Source: FsLexYacc documentation and F# compiler pattern
// https://github.com/fsprojects/FsLexYacc/blob/master/docs/content/fsyacc.md

// In parser actions:
Expr:
    | NUMBER
        { let startPos = (parseState.InputStartPosition 1)
          let endPos = (parseState.InputEndPosition 1)
          let span = mkSpan startPos.FileName startPos endPos
          Number($1, span) }

    | Expr PLUS Term
        { let startPos = (parseState.InputStartPosition 1)
          let endPos = (parseState.InputEndPosition 3)
          let span = mkSpan startPos.FileName startPos endPos
          Add($1, $3, span) }

// Using helper functions (shorter):
Expr:
    | NUMBER
        { Number($1, rhs parseState 1 |> toSpan) }

    | Expr PLUS Term
        { Add($1, $3, lhs parseState |> toSpan) }
```

### Pattern 5: Unknown Span for Built-ins
**What:** Use special "unknown" span for compiler-generated or built-in values
**When to use:** Standard library functions, implicit conversions, etc.
**Example:**
```fsharp
// Source: F# compiler range0 pattern
// https://fsharp.github.io/fsharp-compiler-docs/reference/fsharp-compiler-text-rangemodule.html

// For built-in/synthetic nodes:
let builtInFunction = Lambda("x", Var("x", unknownSpan), unknownSpan)

// Check if span is unknown:
let isUnknownSpan (span: Span) : bool =
    span.FileName = "<unknown>" && span.StartLine = 0
```

### Anti-Patterns to Avoid
- **Forgetting newline updates:** Position tracking breaks if `lexbuf.EndPos.AsNewLinePos()` isn't called on newlines, causing all line numbers to be 1
- **Inconsistent span construction:** Always use start position from first symbol and end position from last symbol; don't mix up order
- **Zero-based vs one-based indexing:** FsLexYacc uses 1-based line numbers internally; be consistent and document clearly
- **Ignoring multi-line tokens:** String literals and block comments must update positions correctly for all internal newlines

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Position tracking in lexer | Manual line/column counting in every token rule | `lexbuf.StartPos` and `lexbuf.EndPos` from FsLexYacc | FsLexYacc maintains positions automatically for `pos_cnum` and `pos_bol`; you only need to call `AsNewLinePos()` on newlines |
| Getting symbol ranges in parser | Manually threading positions through grammar | `parseState.InputStartPosition`, `InputEndPosition`, `InputRange` helpers | Parser state already tracks positions for all symbols; helpers like `rhs parseState 1` and `lhs parseState` extract them correctly |
| Zero span for built-ins | `null` or `option<Span>` | Dedicated `unknownSpan` constant with sentinel values | F# compiler uses `range0`; sentinel values avoid null checks and option unwrapping |

**Key insight:** FsLexYacc already does most position tracking automatically in the `LexBuffer` state. Your job is to initialize it correctly, update it on newlines, and propagate it to AST nodes—not to reimplement position tracking from scratch.

## Common Pitfalls

### Pitfall 1: Forgetting to Initialize lexbuf.EndPos
**What goes wrong:** All tokens report position as line 0, column 0, or throw exceptions
**Why it happens:** LexBuffer doesn't automatically know the filename or starting position
**How to avoid:** Always call initialization function before tokenizing:
```fsharp
let lexbuf = LexBuffer<char>.FromString input
setInitialPos lexbuf "example.fun"  // REQUIRED
let tokens = tokenize lexbuf
```
**Warning signs:** Error messages show "line 0" or "<unknown>" file even for valid source

### Pitfall 2: Not Updating Position on Newlines
**What goes wrong:** Line numbers are always 1; column numbers become huge
**Why it happens:** Lexer doesn't automatically detect newlines (could be in strings, comments, etc.)
**How to avoid:** Call `lexbuf.EndPos <- lexbuf.EndPos.AsNewLinePos()` in every newline rule:
```fsharp
rule tokenize = parse
    | newline       { lexbuf.EndPos <- lexbuf.EndPos.AsNewLinePos()
                      tokenize lexbuf }
    | "//" [^ '\n']* newline  { lexbuf.EndPos <- lexbuf.EndPos.AsNewLinePos()
                                tokenize lexbuf }
```
**Warning signs:** Multi-line programs report errors all on line 1 with large column numbers

### Pitfall 3: Multi-line Tokens Don't Update Positions
**What goes wrong:** String literals or block comments spanning multiple lines show wrong end position
**Why it happens:** Sub-rules don't automatically call `AsNewLinePos()`
**How to avoid:** Update position in sub-rules that consume newlines:
```fsharp
and read_string buf = parse
    | '"'      { STRING (buf.ToString()) }
    | "\\n"    { buf.Append('\n') |> ignore; read_string buf lexbuf }
    | newline  { lexbuf.EndPos <- lexbuf.EndPos.AsNewLinePos()
                 failwith "Newline in string literal" }

and block_comment depth = parse
    | "(*"    { block_comment (depth + 1) lexbuf }
    | "*)"    { if depth = 1 then tokenize lexbuf
                else block_comment (depth - 1) lexbuf }
    | newline { lexbuf.EndPos <- lexbuf.EndPos.AsNewLinePos()  // ADD THIS
                block_comment depth lexbuf }
```
**Warning signs:** Errors in multi-line strings/comments report wrong line numbers

### Pitfall 4: Off-by-One in Parser Position Indices
**What goes wrong:** Span points to wrong token or throws index out of range exception
**Why it happens:** Confusing 1-based parser indices ($1, $2) with 0-based array indexing
**How to avoid:** Use `parseState.InputStartPosition n` where `n` matches the `$n` symbol (1-based):
```fsharp
// For rule: Expr PLUS Term
// $1 = Expr, $2 = PLUS, $3 = Term
| Expr PLUS Term
    { let startPos = parseState.InputStartPosition 1  // First symbol (Expr)
      let endPos = parseState.InputEndPosition 3       // Last symbol (Term)
      Add($1, $3, mkSpan startPos.FileName startPos endPos) }
```
**Warning signs:** Parser crashes with "index out of range" or spans cover wrong tokens

### Pitfall 5: Span Construction Order Reversed
**What goes wrong:** Start position comes after end position, causing negative ranges
**Why it happens:** Accidentally swapping start/end when combining positions
**How to avoid:** Always use leftmost symbol's start and rightmost symbol's end:
```fsharp
// CORRECT: start from $1, end from $3
let span = mkSpan (parseState.InputStartPosition 1) (parseState.InputEndPosition 3)

// WRONG: swapped
let span = mkSpan (parseState.InputEndPosition 3) (parseState.InputStartPosition 1)
```
**Warning signs:** Span shows "line 5 column 10 to line 3 column 2" (end before start)

### Pitfall 6: Breaking Changes to All AST Constructors
**What goes wrong:** Adding span parameter to Expr breaks all existing code that constructs AST nodes
**Why it happens:** Every Expr variant now requires an additional span argument
**How to avoid:**
1. Add span as last parameter for consistency
2. Update all construction sites systematically (lexer, parser, tests)
3. Create helper functions for tests that use `unknownSpan` by default
```fsharp
// Test helper to avoid threading unknownSpan everywhere:
module TestHelpers =
    let num n = Number(n, unknownSpan)
    let add e1 e2 = Add(e1, e2, unknownSpan)
    let var x = Var(x, unknownSpan)
```
**Warning signs:** Compilation errors in hundreds of locations after adding span field

## Code Examples

Verified patterns from official sources:

### Complete Span Type Module
```fsharp
// Source: Based on F# compiler Range type
// https://fsharp.github.io/fsharp-compiler-docs/reference/fsharp-compiler-text-rangemodule.html

module Span =
    open FSharp.Text.Lexing

    /// Represents a source location span
    type Span = {
        FileName: string
        StartLine: int
        StartColumn: int
        EndLine: int
        EndColumn: int
    }

    /// Create span from FsLexYacc positions
    let mkSpan (fileName: string) (startPos: Position) (endPos: Position) : Span =
        {
            FileName = fileName
            StartLine = startPos.Line
            StartColumn = startPos.Column
            EndLine = endPos.Line
            EndColumn = endPos.Column
        }

    /// Unknown span for built-in definitions
    let unknownSpan : Span =
        {
            FileName = "<unknown>"
            StartLine = 0
            StartColumn = 0
            EndLine = 0
            EndColumn = 0
        }

    /// Format span for error messages
    let formatSpan (span: Span) : string =
        if span = unknownSpan then
            "<unknown location>"
        else
            sprintf "%s:%d:%d-%d:%d"
                span.FileName
                span.StartLine span.StartColumn
                span.EndLine span.EndColumn

    /// Check if span is unknown/synthetic
    let isUnknown (span: Span) : bool =
        span = unknownSpan
```

### Lexer with Position Tracking
```fsharp
// Source: FsLexYacc documentation
// https://fsprojects.github.io/FsLexYacc/content/fslex.html

{
open FSharp.Text.Lexing
open Parser

let lexeme (lexbuf: LexBuffer<_>) =
    LexBuffer<_>.LexemeString lexbuf

// Initialize position tracking
let setInitialPos (lexbuf: LexBuffer<_>) (filename: string) =
    lexbuf.EndPos <- {
        pos_fname = filename
        pos_lnum = 1
        pos_bol = 0
        pos_cnum = 0
    }
}

let digit = ['0'-'9']
let whitespace = [' ' '\t']
let newline = ('\n' | '\r' '\n')

rule tokenize = parse
    | whitespace+   { tokenize lexbuf }
    | newline       { lexbuf.EndPos <- lexbuf.EndPos.AsNewLinePos()
                      tokenize lexbuf }
    | digit+        { NUMBER (Int32.Parse(lexeme lexbuf)) }
    | '+'           { PLUS }
    | '-'           { MINUS }
    | eof           { EOF }

// Multi-line comment with position updates
and block_comment depth = parse
    | "(*"    { block_comment (depth + 1) lexbuf }
    | "*)"    { if depth = 1 then tokenize lexbuf
                else block_comment (depth - 1) lexbuf }
    | newline { lexbuf.EndPos <- lexbuf.EndPos.AsNewLinePos()
                block_comment depth lexbuf }
    | eof     { failwith "Unterminated comment" }
    | _       { block_comment depth lexbuf }
```

### Parser with Position Propagation
```fsharp
// Source: FsLexYacc documentation and F# compiler pattern
// https://github.com/fsprojects/FsLexYacc/blob/master/docs/content/fsyacc.md

%{
open Ast
open FSharp.Text.Lexing

// Helper to create span from parser positions
let spanFromPos (startPos: Position) (endPos: Position) : Span =
    Span.mkSpan startPos.FileName startPos endPos

// Helper to get span from single symbol
let spanOf (parseState: IParseState) (n: int) : Span =
    spanFromPos (parseState.InputStartPosition n) (parseState.InputEndPosition n)

// Helper to get span covering entire rule
let spanOfRule (parseState: IParseState) : Span =
    let (startPos, endPos) = parseState.ResultRange
    spanFromPos startPos endPos
%}

%token <int> NUMBER
%token PLUS MINUS
%token EOF

%start start
%type <Ast.Expr> start

%%

start:
    | Expr EOF  { $1 }

Expr:
    | Expr PLUS Term
        { Add($1, $3, spanOfRule parseState) }
    | Expr MINUS Term
        { Subtract($1, $3, spanOfRule parseState) }
    | Term
        { $1 }

Term:
    | NUMBER
        { Number($1, spanOf parseState 1) }
    | LPAREN Expr RPAREN
        { $2 }  // Use inner expr's span, not parentheses
```

### Usage Example
```fsharp
// Source: Practical usage pattern

let parseFile (filename: string) (input: string) : Expr =
    let lexbuf = LexBuffer<char>.FromString input
    Lexer.setInitialPos lexbuf filename
    Parser.start Lexer.tokenize lexbuf

let reportError (span: Span) (message: string) : unit =
    if Span.isUnknown span then
        printfn "Error: %s" message
    else
        printfn "%s\nError: %s" (Span.formatSpan span) message

// Example error reporting:
try
    let expr = parseFile "test.fun" "1 + (2 * )"
    eval expr
with
| EvalError(span, msg) ->
    reportError span msg
    // Output: test.fun:1:8-1:9
    //         Error: Expected expression after '*'
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Null/option spans | Sentinel unknownSpan value | F# compiler tradition | Simpler pattern matching, no option unwrapping |
| Eager line/column | Lazy byte-offset only | 2020s performance focus | Faster lexing but complex error reporting |
| Manual position threading | LexBuffer automatic tracking | Since OCamlLex (1990s) | Less error-prone, cleaner code |
| Parser without positions | parseState with position methods | FsYacc inception | Enables quality diagnostics |

**Deprecated/outdated:**
- Manual line/column counting in lexer actions: FsLexYacc provides `Position` type with automatic `pos_cnum` and `pos_bol` tracking
- Using `option<Span>` for unknown locations: Sentinel value pattern (like F# compiler's `range0`) is cleaner

## Open Questions

Things that couldn't be fully resolved:

1. **Should Column be 0-based or 1-based?**
   - What we know: FsLexYacc uses 1-based line numbers (`pos_lnum`), but column calculation is `pos_cnum - pos_bol` which could be either
   - What's unclear: Whether Position.Column property returns 0-based or 1-based column numbers
   - Recommendation: Document convention clearly in code; use 1-based for consistency with line numbers and user expectations

2. **How to handle spans that cross file boundaries?**
   - What we know: Macros and file inclusion can create AST nodes from multiple files
   - What's unclear: Should Span support multiple filenames, or always use outermost file?
   - Recommendation: Not needed for Phase 1; defer until macro/import phase; use single filename (outermost) if needed

3. **Performance impact of eager position tracking?**
   - What we know: Modern compilers use lazy byte-offset-only tracking for performance
   - What's unclear: Is the performance difference significant for small educational compiler?
   - Recommendation: Use eager tracking (simpler); optimize only if profiling shows it's a bottleneck

## Sources

### Primary (HIGH confidence)
- [FsLexYacc Position type reference](https://fsprojects.github.io/FsLexYacc/reference/fsharp-text-lexing-position.html) - Official API documentation
- [FsLex Overview](https://fsprojects.github.io/FsLexYacc/content/fslex.html) - Position tracking initialization and updates
- [FsYacc documentation](https://github.com/fsprojects/FsLexYacc/blob/master/docs/content/fsyacc.md) - parseState and position propagation
- [F# Compiler Range type](https://fsharp.github.io/fsharp-compiler-docs/reference/fsharp-compiler-text-rangemodule.html) - Real-world span implementation
- [F# Compiler AST Tutorial](https://fsharp.github.io/fsharp-compiler-docs/fcs/untypedtree.html) - How F# compiler stores ranges in AST

### Secondary (MEDIUM confidence)
- [F# Programming Wikibook - Lexing and Parsing](https://en.wikibooks.org/wiki/F_Sharp_Programming/Lexing_and_Parsing) - General patterns, verified with official docs
- [Using FSLexYacc blog post](https://thanos.codes/blog/using-fslexyacc-the-fsharp-lexer-and-parser/) - Practical examples
- [Futhark compiler blog: Tracking source locations](https://futhark-lang.org/blog/2025-07-29-tracking-source-locations.html) - Lessons learned from adding spans to existing compiler

### Tertiary (LOW confidence)
- [Writing a Lexer in C and C++](https://copyprogramming.com/howto/writing-a-lexer-in-c) - General lexer position tracking advice, not F# specific
- [JSON.parse() position tracking bug](https://bugzilla.mozilla.org/show_bug.cgi?id=507998) - Off-by-one error examples

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - FsLexYacc is the established tool for F# parsers; Position type is well-documented
- Architecture: HIGH - F# compiler source code provides proven patterns; FsLexYacc docs show clear usage
- Pitfalls: MEDIUM - Common issues documented but some discovered through community discussions, not official docs

**Research date:** 2026-02-02
**Valid until:** 2026-03-04 (30 days - stable technology, no breaking changes expected)
