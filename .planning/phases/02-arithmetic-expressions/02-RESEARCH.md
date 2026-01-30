# Phase 2: Arithmetic Expressions - Research

**Researched:** 2026-01-30
**Domain:** FsYacc arithmetic expression parsing with operator precedence and evaluation
**Confidence:** HIGH

## Summary

Phase 2 extends the Phase 1 foundation to support arithmetic expressions with four operators (+, -, *, /), operator precedence, parentheses, and unary minus. The implementation builds on the existing FsLexYacc 11.3.0 infrastructure, expanding the AST discriminated union, grammar rules, and lexer patterns.

There are two standard approaches for handling operator precedence in FsYacc: (1) using `%left` and `%right` precedence declarations, or (2) using the classic Expr/Term/Factor grammar stratification from compiler textbooks. Given FsYacc's known bugs with precedence declarations (issues #39, #40), the **Expr/Term/Factor approach is more reliable** for this tutorial. This pattern encodes precedence directly in the grammar structure and avoids shift-reduce conflicts without relying on precedence declarations.

For unary minus (EXPR-04), the cleanest approach is to handle it in the Factor rule, giving it highest precedence naturally. An alternative is using `%prec` with a pseudo-token, but FsYacc's precedence handling bugs make this unreliable. The grammar-based approach is safer and more educational.

**Primary recommendation:** Use the classic Expr/Term/Factor grammar pattern to encode operator precedence directly in the grammar structure, avoiding reliance on FsYacc's `%left`/`%right` declarations which have known bugs.

## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| FsLexYacc | 11.3.0 | Lexer/parser generation | Already established in Phase 1, continues |
| FsLexYacc.Runtime | 11.3.0 | Runtime support | Required for LexBuffer and parsing |
| F# Discriminated Unions | Built-in | AST representation | Perfect for representing expression trees |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| FSharp.Core | 8.0+ | Map for environments | Phase 3+ for variable bindings |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Expr/Term/Factor grammar | %left/%right declarations | Declarations are simpler but FsYacc has bugs (issues #39, #40) |
| Unary minus in grammar | Negative number in lexer | Grammar approach more flexible for expressions like `-(1+2)` |

**Installation:**
```bash
# Already installed in Phase 1
dotnet add package FsLexYacc --version 11.3.0
```

## Architecture Patterns

### Recommended Project Structure
```
FunLang/
├── Ast.fs           # Extended with Add, Subtract, Multiply, Divide, Negate
├── Parser.fsy       # Expr/Term/Factor grammar with operator rules
├── Lexer.fsl        # New tokens: PLUS, MINUS, STAR, SLASH, LPAREN, RPAREN
├── Eval.fs          # NEW: Evaluator using pattern matching
├── Program.fs       # Updated to call evaluator
└── FunLang.fsproj   # Add Eval.fs to compilation order
```

### Pattern 1: Expr/Term/Factor Grammar for Precedence
**What:** Encode operator precedence through grammar stratification rather than precedence declarations.
**When to use:** Always in FsYacc, due to known bugs with `%left`/`%right`.
**Example:**
```fsharp
// Source: Microsoft Learn (Jomo Fisher tutorial)
// Lower precedence operators at higher (outer) grammar level
// Higher precedence operators at lower (inner) grammar level

Expr:
    | Expr PLUS Term      { Add($1, $3) }
    | Expr MINUS Term     { Subtract($1, $3) }
    | Term                { $1 }

Term:
    | Term STAR Factor    { Multiply($1, $3) }
    | Term SLASH Factor   { Divide($1, $3) }
    | Factor              { $1 }

Factor:
    | NUMBER              { Number($1) }
    | LPAREN Expr RPAREN  { $2 }
    | MINUS Factor        { Negate($2) }  // Unary minus
```

### Pattern 2: AST with Explicit Operations
**What:** Define separate discriminated union cases for each operator.
**When to use:** Always for clarity and exhaustive pattern matching.
**Example:**
```fsharp
// Source: Microsoft Learn (Discriminated Unions)
module Ast

type Expr =
    | Number of int
    | Add of Expr * Expr
    | Subtract of Expr * Expr
    | Multiply of Expr * Expr
    | Divide of Expr * Expr
    | Negate of Expr  // Unary minus
```

### Pattern 3: Recursive Evaluator with Pattern Matching
**What:** Use F# pattern matching for exhaustive, clean evaluation logic.
**When to use:** Always for interpreters.
**Example:**
```fsharp
// Source: Rosetta Code F# Arithmetic Evaluation
module Eval

let rec eval expr =
    match expr with
    | Number n -> n
    | Add (a, b) -> eval a + eval b
    | Subtract (a, b) -> eval a - eval b
    | Multiply (a, b) -> eval a * eval b
    | Divide (a, b) -> eval a / eval b  // Integer division
    | Negate e -> -(eval e)
```

### Anti-Patterns to Avoid
- **Using %left/%right in FsYacc:** Known bugs with precedence declarations (GitHub issues #39, #40). Use grammar stratification instead.
- **Single BinOp AST node with operator string:** Loses type safety, requires runtime operator dispatch.
- **Negative numbers in lexer only:** Pattern `['-']?digit+` can't handle `-(1+2)` or `--5`.
- **Mixing precedence approaches:** Either use grammar stratification OR precedence declarations, not both.

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Integer parsing | Manual digit accumulation | `Int32.Parse(lexeme)` | Handles edge cases, overflow detection |
| Operator precedence | Ad-hoc parsing logic | Grammar stratification (Expr/Term/Factor) | Compiler-verified correctness |
| AST traversal | Manual if/else chains | F# pattern matching with `match` | Exhaustiveness checking, cleaner code |
| Parentheses handling | Special-case logic | Grammar rule `LPAREN Expr RPAREN` | Naturally recursive, handles nesting |

**Key insight:** The Expr/Term/Factor grammar pattern has been used since the original yacc in the 1970s. It's proven, well-understood, and avoids the precedence declaration bugs in FsYacc. Don't try to simplify it with clever hacks.

## Common Pitfalls

### Pitfall 1: FsYacc Precedence Declaration Bugs
**What goes wrong:** Using `%left PLUS MINUS` and `%left STAR SLASH` causes unexpected parse results or shift-reduce conflicts that shouldn't occur.

**Why it happens:** FsYacc has known bugs where `%left` is sometimes treated as `%right`, and `%nonassoc` doesn't work correctly (GitHub issues #39, #40).

**How to avoid:** Use Expr/Term/Factor grammar stratification instead of precedence declarations.

**Warning signs:**
- Parser returns right-associative results for left-associative operators
- Shift-reduce conflicts despite correct precedence declarations
- `2 - 3 - 4` evaluates to `2 - (3 - 4) = 3` instead of `(2 - 3) - 4 = -5`

### Pitfall 2: Division by Zero
**What goes wrong:** `10 / 0` crashes the interpreter with an unhandled exception.

**Why it happens:** F# integer division throws `DivideByZeroException`.

**How to avoid:** Either: (a) let it fail with clear error message for Phase 2 simplicity, or (b) check divisor and return error value. Decision: Document that division by zero throws exception; error handling is Phase 6 concern.

**Warning signs:**
- Unexpected program termination
- Stack traces in output

### Pitfall 3: Unary Minus Ambiguity
**What goes wrong:** Grammar can't distinguish binary minus from unary minus, causing conflicts.

**Why it happens:** Same MINUS token used for both operations without clear grammar separation.

**How to avoid:** Handle unary minus in Factor rule only: `Factor: MINUS Factor { Negate($2) }`. This gives unary minus highest precedence and avoids ambiguity.

**Warning signs:**
- Reduce-reduce conflicts
- `1 - -2` parsing incorrectly
- `-5 + 3` giving wrong result

### Pitfall 4: Left Recursion Confusion
**What goes wrong:** Beginners think left recursion is bad (true for LL parsers) and try to eliminate it.

**Why it happens:** Confusion between LL (top-down) and LALR (bottom-up) parsing. FsYacc is LALR.

**How to avoid:** Keep left recursion in FsYacc grammars. It's required for correct left-associativity. `Expr: Expr PLUS Term` is correct.

**Warning signs:**
- Attempting to rewrite `Expr: Expr PLUS Term` as `Expr: Term PLUS Expr`
- Operators becoming right-associative

### Pitfall 5: Integer Overflow
**What goes wrong:** Large multiplication results overflow `int` silently.

**Why it happens:** F# integers wrap on overflow by default.

**How to avoid:** Accept this limitation for Phase 2 (tutorial simplicity). Document that FunLang uses 32-bit signed integers with wraparound semantics.

**Warning signs:**
- `1000000 * 1000000` returning negative number

## Code Examples

Verified patterns from official sources:

### Complete AST Definition
```fsharp
// Source: Jomo Fisher tutorial + Rosetta Code pattern
module Ast

/// Expression AST for arithmetic operations
/// Phase 2: Arithmetic expressions with precedence
type Expr =
    | Number of int
    | Add of Expr * Expr
    | Subtract of Expr * Expr
    | Multiply of Expr * Expr
    | Divide of Expr * Expr
    | Negate of Expr  // Unary minus
```

### Complete Parser Grammar
```fsharp
// Source: Jomo Fisher tutorial pattern
%{
open Ast
%}

// Token declarations
%token <int> NUMBER
%token PLUS MINUS STAR SLASH
%token LPAREN RPAREN
%token EOF

// Start symbol
%start start
%type <Ast.Expr> start

%%

start:
    | Expr EOF           { $1 }

Expr:
    | Expr PLUS Term     { Add($1, $3) }
    | Expr MINUS Term    { Subtract($1, $3) }
    | Term               { $1 }

Term:
    | Term STAR Factor   { Multiply($1, $3) }
    | Term SLASH Factor  { Divide($1, $3) }
    | Factor             { $1 }

Factor:
    | NUMBER             { Number($1) }
    | LPAREN Expr RPAREN { $2 }
    | MINUS Factor       { Negate($2) }
```

### Complete Lexer Specification
```fsharp
// Source: FsLexYacc documentation + thanos.codes tutorial
{
open System
open FSharp.Text.Lexing
open Parser

let lexeme (lexbuf: LexBuffer<_>) =
    LexBuffer<_>.LexemeString lexbuf
}

let digit = ['0'-'9']
let whitespace = [' ' '\t']
let newline = ('\n' | '\r' '\n')

rule tokenize = parse
    | whitespace+   { tokenize lexbuf }
    | newline       { tokenize lexbuf }
    | digit+        { NUMBER (Int32.Parse(lexeme lexbuf)) }
    | '+'           { PLUS }
    | '-'           { MINUS }
    | '*'           { STAR }
    | '/'           { SLASH }
    | '('           { LPAREN }
    | ')'           { RPAREN }
    | eof           { EOF }
```

### Complete Evaluator
```fsharp
// Source: Rosetta Code F# pattern
module Eval

open Ast

/// Evaluate an expression to an integer result
let rec eval (expr: Expr) : int =
    match expr with
    | Number n -> n
    | Add (left, right) -> eval left + eval right
    | Subtract (left, right) -> eval left - eval right
    | Multiply (left, right) -> eval left * eval right
    | Divide (left, right) -> eval left / eval right
    | Negate e -> -(eval e)
```

### Updated Program.fs
```fsharp
// Source: Phase 1 pattern extended
open System
open FSharp.Text.Lexing
open Ast
open Eval

let parse (input: string) : Expr =
    let lexbuf = LexBuffer<char>.FromString input
    Parser.start Lexer.tokenize lexbuf

[<EntryPoint>]
let main argv =
    let testCases = [
        "42", 42
        "2 + 3", 5
        "2 + 3 * 4", 14           // Precedence: * before +
        "(2 + 3) * 4", 20         // Parentheses override
        "10 / 2 - 3", 2           // Left associativity
        "-5 + 3", -2              // Unary minus
    ]

    printfn "FunLang Interpreter - Phase 2: Arithmetic Expressions"
    printfn "======================================================"

    for (input, expected) in testCases do
        let ast = parse input
        let result = eval ast
        let status = if result = expected then "PASS" else "FAIL"
        printfn "[%s] %s = %d (expected %d)" status input result expected

    0
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Mutable evaluator with ref cells | Pure recursive eval function | F# best practice | Simpler, testable, no side effects |
| Generic BinOp(string, Expr, Expr) | Explicit Add/Subtract/Multiply/Divide | F# idiom | Exhaustive matching, type safety |
| %left/%right precedence | Grammar stratification | Always for FsYacc | Avoids FsYacc bugs |

**Deprecated/outdated:**
- **FSPowerPack**: FsLexYacc supersedes it; mixing causes conflicts
- **%nonassoc in FsYacc**: Broken (issue #39), don't use
- **%prec for unary minus**: Unreliable in FsYacc, use grammar approach

## Open Questions

Things that couldn't be fully resolved:

1. **Division by zero handling**
   - What we know: F# throws `DivideByZeroException`
   - What's unclear: Should Phase 2 handle this or defer to Phase 6 (error handling)?
   - Recommendation: Let it throw for now; document behavior. Phase 6 adds proper error handling.

2. **Integer overflow behavior**
   - What we know: F# `int` wraps on overflow silently
   - What's unclear: Should FunLang detect overflow?
   - Recommendation: Accept wraparound semantics for simplicity. Document as language behavior.

3. **Negative number parsing in lexer**
   - What we know: Could parse `-42` as single NUMBER token
   - What's unclear: Whether this is simpler or more limiting
   - Recommendation: Use unary minus in grammar (Negate) for flexibility with expressions like `-(1+2)`.

## Sources

### Primary (HIGH confidence)
- [Use FsLex and FsYacc to make a parser in F#](https://learn.microsoft.com/en-us/archive/blogs/jomo_fisher/use-fslex-and-fsyacc-to-make-a-parser-in-f) - Expr/Term/Factor grammar pattern
- [Rosetta Code: Arithmetic Evaluation](https://rosettacode.org/wiki/Arithmetic_evaluation) - F# AST and evaluator pattern
- [Microsoft Learn: Discriminated Unions](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/discriminated-unions) - AST design patterns
- [FsLexYacc Issue #40](https://github.com/fsprojects/FsLexYacc/issues/40) - Precedence declaration bugs
- [FsLexYacc Issue #39](https://github.com/fsprojects/FsLexYacc/issues/39) - %nonassoc not handled correctly

### Secondary (MEDIUM confidence)
- [Using FSLexYacc Tutorial](https://thanos.codes/blog/using-fslexyacc-the-fsharp-lexer-and-parser/) - Lexer patterns, verified against Phase 1 working code
- [FsLexYacc JSON Parser Example](https://fsprojects.github.io/FsLexYacc/content/jsonParserExample.html) - Project configuration patterns
- [Let's Build A Simple Interpreter Part 8](https://ruslanspivak.com/lsbasi-part8/) - Unary operator grammar design

### Tertiary (LOW confidence)
- [Fixing Ambiguities in Grammars](https://journal.stuffwithstuff.com/2008/12/28/fixing-ambiguities-in-yacc/) - General yacc conflict resolution (not F#-specific)
- [Bison Precedence Documentation](https://www.gnu.org/software/bison/manual/html_node/Precedence-Decl.html) - %left/%right semantics (Bison, not FsYacc)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Continuing Phase 1 infrastructure, no new dependencies
- Architecture: HIGH - Expr/Term/Factor is textbook pattern with 50+ years of proven use
- Pitfalls: HIGH - FsYacc bugs documented in GitHub issues with reproducible cases

**Research date:** 2026-01-30
**Valid until:** 2026-03-01 (30 days - stable domain, FsYacc unlikely to change)
