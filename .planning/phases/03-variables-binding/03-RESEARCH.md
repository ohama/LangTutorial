# Phase 3: Variables & Binding - Research

**Researched:** 2026-01-30
**Domain:** Variable binding, lexical scoping, environment management
**Confidence:** HIGH

## Summary

This research investigates implementing variables and let bindings in FunLang, building on the existing fslex/fsyacc-based arithmetic expression parser. The implementation requires:

1. **AST extension**: Add `Var` for variable references and `Let` for let-in expressions
2. **Lexer tokens**: Add `LET`, `IN`, `EQUALS`, and `IDENT` tokens with proper keyword/identifier distinction
3. **Parser grammar**: Add grammar rules for let-in with correct precedence (lowest, below arithmetic)
4. **Environment**: Use F#'s immutable `Map<string, int>` to track variable bindings
5. **Error handling**: Return clear error messages for undefined variables using F# Result type or exceptions

**Primary recommendation:** Use the established ML/OCaml pattern: `let x = expr1 in expr2` parsed as `Let(name, binding, body)`, evaluated by extending the environment and recursively evaluating the body.

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| FsLexYacc | 11.3.0 | Lexer/parser generation | Already in use, well-documented |
| FSharp.Core Map | built-in | Immutable environment | Standard F# collection, persistent |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| F# Result type | built-in | Error handling | Returning undefined variable errors |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Map<string, int> | Dictionary | Map is immutable, perfect for scoped environments |
| Result type | Exceptions | Exceptions simpler initially, Result better long-term |

## Architecture Patterns

### Recommended AST Extension

```fsharp
// Ast.fs - Add to existing type
type Expr =
    | Number of int
    | Add of Expr * Expr
    | Subtract of Expr * Expr
    | Multiply of Expr * Expr
    | Divide of Expr * Expr
    | Negate of Expr
    // Phase 3: Variables
    | Var of string           // Variable reference
    | Let of string * Expr * Expr  // let name = expr1 in expr2
```

### Pattern 1: Environment-Passing Evaluation

**What:** Thread an immutable environment through recursive evaluation
**When to use:** Always for let-in expressions with lexical scoping
**Example:**

```fsharp
// Eval.fs - Environment-based evaluation
type Env = Map<string, int>

let rec eval (env: Env) (expr: Expr) : int =
    match expr with
    | Number n -> n
    | Var name ->
        match Map.tryFind name env with
        | Some value -> value
        | None -> failwithf "Undefined variable: %s" name
    | Let (name, binding, body) ->
        let value = eval env binding      // Evaluate binding in current env
        let newEnv = Map.add name value env  // Extend environment
        eval newEnv body                  // Evaluate body in extended env
    | Add (left, right) -> eval env left + eval env right
    // ... other cases pass env through
```

### Pattern 2: Keyword vs Identifier Lexing

**What:** Check if an identifier matches a keyword, return appropriate token
**When to use:** When language has reserved keywords (let, in)
**Example:**

```fsl
// Lexer.fsl pattern
let letter = ['a'-'z' 'A'-'Z']
let digit = ['0'-'9']
let ident = letter (letter | digit | '_')*

rule tokenize = parse
    | "let"         { LET }
    | "in"          { IN }
    | ident         { IDENT (lexeme lexbuf) }
```

### Pattern 3: Let-In Grammar with Precedence

**What:** Parse let-in expressions at lowest precedence level
**When to use:** Standard approach for ML-family languages
**Example:**

```fsy
// Parser.fsy - Precedence matters!
%nonassoc IN        // IN has lowest precedence
%left PLUS MINUS    // Then additive
%left STAR SLASH    // Then multiplicative

// This ensures: let x = 1 in x + 2 parses as: let x = 1 in (x + 2)
// NOT as: (let x = 1 in x) + 2
```

### Anti-Patterns to Avoid

- **Mutable environment:** Don't use `Dictionary` or mutable state; breaks when you need nested scopes
- **Separate keyword tokens in lexer rules:** Define keywords explicitly BEFORE the general identifier rule, or use keyword map lookup
- **Wrong precedence for IN:** If IN has higher precedence than operators, expressions parse incorrectly

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Immutable environment | Custom linked list of scopes | F# Map | Map.add returns new map, perfect for scoping |
| String comparison | Custom identifier equality | F# string = | Built-in handles all edge cases |
| Error result type | Custom error union | F# Result<'T, 'E> or failwith | Standard, well-understood |

**Key insight:** F#'s `Map` is already an immutable persistent data structure. When you `Map.add`, you get a new map that shares structure with the old one. This is exactly what you need for let-in scoping where the extended environment only exists for the body evaluation.

## Common Pitfalls

### Pitfall 1: Lexer Rule Order for Keywords

**What goes wrong:** Identifiers match before keywords, so `let` becomes `IDENT("let")`
**Why it happens:** fslex tries rules in order; if identifier pattern matches first, keywords never match
**How to avoid:**
1. Define keyword patterns explicitly BEFORE identifier pattern, OR
2. Match identifier then check against keyword map
**Warning signs:** Parser syntax errors on keyword usage

### Pitfall 2: Let-In Precedence

**What goes wrong:** `let x = 1 in x + 2` parses as `(let x = 1 in x) + 2 = error`
**Why it happens:** Missing or incorrect precedence declaration for IN
**How to avoid:** Declare `%nonassoc IN` at lowest precedence (before %left operators)
**Warning signs:** Type errors or unexpected parse results with let-in in expressions

### Pitfall 3: Environment Not Threaded Through

**What goes wrong:** Variable lookup fails even when variable is defined
**Why it happens:** Some eval branches don't pass the environment parameter
**How to avoid:** Every recursive `eval` call must pass `env` (or extended env for Let)
**Warning signs:** "Undefined variable" errors for defined variables

### Pitfall 4: Shadowing vs Undefined

**What goes wrong:** Confusion between variable not found and shadowing
**Why it happens:** Not distinguishing between "new binding" and "lookup"
**How to avoid:**
- Let always uses `Map.add` (shadowing is fine, creates new binding)
- Var always uses `Map.tryFind` (lookup only, never creates)
**Warning signs:** Unexpected values from shadowed variables

### Pitfall 5: Token Type for IDENT

**What goes wrong:** Parser can't access the identifier string
**Why it happens:** Forgetting to declare `%token <string> IDENT`
**How to avoid:** Always declare tokens with payload types: `%token <string> IDENT`
**Warning signs:** Compilation errors in parser or can't construct Var node

## Code Examples

### Complete Lexer Extension

```fsl
// Lexer.fsl - Add to existing file
{
open System
open FSharp.Text.Lexing
open Parser

let lexeme (lexbuf: LexBuffer<_>) =
    LexBuffer<_>.LexemeString lexbuf
}

// Character classes
let digit = ['0'-'9']
let letter = ['a'-'z' 'A'-'Z']
let ident_start = letter | '_'
let ident_char = letter | digit | '_'
let whitespace = [' ' '\t']
let newline = ('\n' | '\r' '\n')

rule tokenize = parse
    | whitespace+   { tokenize lexbuf }
    | newline       { tokenize lexbuf }
    | digit+        { NUMBER (Int32.Parse(lexeme lexbuf)) }
    // Keywords MUST come before identifier pattern
    | "let"         { LET }
    | "in"          { IN }
    // Identifier: starts with letter or underscore
    | ident_start ident_char* { IDENT (lexeme lexbuf) }
    | '+'           { PLUS }
    | '-'           { MINUS }
    | '*'           { STAR }
    | '/'           { SLASH }
    | '='           { EQUALS }
    | '('           { LPAREN }
    | ')'           { RPAREN }
    | eof           { EOF }
```

### Complete Parser Extension

```fsy
// Parser.fsy - Full file with let-in
%{
open Ast
%}

%token <int> NUMBER
%token <string> IDENT
%token PLUS MINUS STAR SLASH EQUALS
%token LET IN
%token LPAREN RPAREN
%token EOF

%start start
%type <Ast.Expr> start

// Precedence: lowest to highest (top to bottom)
%nonassoc IN           // let-in binds loosest
%left PLUS MINUS       // then additive
%left STAR SLASH       // then multiplicative

%%

start:
    | Expr EOF           { $1 }

Expr:
    // Let expression - lowest precedence due to %nonassoc IN
    | LET IDENT EQUALS Expr IN Expr  { Let($2, $4, $6) }
    | Expr PLUS Term     { Add($1, $3) }
    | Expr MINUS Term    { Subtract($1, $3) }
    | Term               { $1 }

Term:
    | Term STAR Factor   { Multiply($1, $3) }
    | Term SLASH Factor  { Divide($1, $3) }
    | Factor             { $1 }

Factor:
    | NUMBER             { Number($1) }
    | IDENT              { Var($1) }
    | LPAREN Expr RPAREN { $2 }
    | MINUS Factor       { Negate($2) }
```

### Complete Evaluator Extension

```fsharp
// Eval.fs - Environment-based evaluation
module Eval

open Ast

/// Environment mapping variable names to values
type Env = Map<string, int>

/// Empty environment for top-level evaluation
let emptyEnv : Env = Map.empty

/// Evaluate an expression in an environment
/// Raises exception for undefined variables
let rec eval (env: Env) (expr: Expr) : int =
    match expr with
    | Number n -> n

    | Var name ->
        match Map.tryFind name env with
        | Some value -> value
        | None -> failwithf "Undefined variable: %s" name

    | Let (name, binding, body) ->
        // Evaluate binding in current environment
        let value = eval env binding
        // Extend environment with new binding
        let extendedEnv = Map.add name value env
        // Evaluate body in extended environment
        eval extendedEnv body

    | Add (left, right) ->
        eval env left + eval env right

    | Subtract (left, right) ->
        eval env left - eval env right

    | Multiply (left, right) ->
        eval env left * eval env right

    | Divide (left, right) ->
        eval env left / eval env right

    | Negate e ->
        -(eval env e)

/// Convenience function for top-level evaluation
let evalExpr (expr: Expr) : int =
    eval emptyEnv expr
```

### Format.fs Token Formatting Extension

```fsharp
// Format.fs - Add to formatToken
let formatToken (token: Parser.token) : string =
    match token with
    | Parser.NUMBER n -> sprintf "NUMBER(%d)" n
    | Parser.IDENT s -> sprintf "IDENT(%s)" s  // New
    | Parser.PLUS -> "PLUS"
    | Parser.MINUS -> "MINUS"
    | Parser.STAR -> "STAR"
    | Parser.SLASH -> "SLASH"
    | Parser.EQUALS -> "EQUALS"  // New
    | Parser.LET -> "LET"        // New
    | Parser.IN -> "IN"          // New
    | Parser.LPAREN -> "LPAREN"
    | Parser.RPAREN -> "RPAREN"
    | Parser.EOF -> "EOF"
```

### Program.fs Evaluator Call Update

```fsharp
// Program.fs - Update eval calls to use emptyEnv
// Before: let result = expr |> parse |> eval
// After:
let result = expr |> parse |> Eval.evalExpr
```

## Test Cases

### VAR-01: Let Binding

```fsharp
// Basic let binding
test "let x = 5 in x" {
    Expect.equal (evaluate "let x = 5 in x") 5 ""
}

test "let with expression binding" {
    Expect.equal (evaluate "let x = 2 + 3 in x") 5 ""
}

test "let with expression body" {
    Expect.equal (evaluate "let x = 5 in x + 1") 6 ""
}
```

### VAR-02: Variable References

```fsharp
test "variable in arithmetic" {
    Expect.equal (evaluate "let x = 3 in x * 4") 12 ""
}

test "multiple uses of variable" {
    Expect.equal (evaluate "let x = 2 in x + x") 4 ""
}
```

### VAR-03: Nested Let-In (Local Scope)

```fsharp
test "nested let" {
    Expect.equal (evaluate "let x = 1 in let y = 2 in x + y") 3 ""
}

test "shadowing" {
    Expect.equal (evaluate "let x = 1 in let x = 2 in x") 2 ""
}

test "outer scope after inner" {
    // let x = 1 in (let y = x + 1 in y) + x
    // Should be: 2 + 1 = 3
    Expect.equal (evaluate "let x = 1 in (let y = x + 1 in y) + x") 3 ""
}
```

### Error Cases

```fsharp
test "undefined variable" {
    Expect.throws (fun () -> evaluate "x" |> ignore) ""
}

test "undefined variable in expression" {
    Expect.throws (fun () -> evaluate "y + 1" |> ignore) ""
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Mutable symbol table | Immutable Map environment | Always for FP | Cleaner scoping semantics |
| Custom error handling | F# Result type | F# 4.1 (2017) | More explicit error flow |

**Deprecated/outdated:**
- Using mutable Dictionary for environment: Breaks with nested scopes
- Not using typed tokens: Loses payload information

## Open Questions

1. **Reserved word set**
   - What we know: `let` and `in` are reserved
   - What's unclear: Should `true`, `false`, `if`, etc. be reserved now for future phases?
   - Recommendation: Reserve only what's needed now, extend later

2. **Error message format**
   - What we know: Need clear "Undefined variable: x" message
   - What's unclear: Exact format, whether to include line/column
   - Recommendation: Simple message now, add location info in error handling phase

## Sources

### Primary (HIGH confidence)
- [FsLexYacc Documentation](https://github.com/fsprojects/FsLexYacc) - Token declarations, grammar rules
- [Cornell CS3110 OCaml Textbook](https://cs3110.github.io/textbook/chapters/interp/parsing.html) - Let-in grammar, precedence
- [Cornell SimPL Tutorial](https://courses.cs.cornell.edu/cs3110/2021sp/textbook/interp/simpl_frontend.html) - Complete lexer/parser examples

### Secondary (MEDIUM confidence)
- [Crafting Interpreters](https://craftinginterpreters.com/statements-and-state.html) - Environment implementation patterns
- [F# for Fun and Profit](https://fsharpforfunandprofit.com/posts/let-use-do/) - F# let binding semantics
- [Microsoft Learn F# Docs](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/results) - Result type, error handling

### Tertiary (LOW confidence)
- [Blog: Using FSLexYacc](https://thanos.codes/blog/using-fslexyacc-the-fsharp-lexer-and-parser/) - fslex/fsyacc setup examples

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - F# Map is well-established for this use case
- Architecture: HIGH - ML-family let-in is thoroughly documented
- Pitfalls: HIGH - Common issues are well-known from compiler courses

**Research date:** 2026-01-30
**Valid until:** 2026-03-01 (stable domain, patterns unchanged for decades)

## Implementation Order Recommendation

1. **AST first**: Add `Var` and `Let` to Ast.fs
2. **Lexer second**: Add tokens (LET, IN, EQUALS, IDENT) to Lexer.fsl
3. **Parser third**: Update token declarations and grammar in Parser.fsy
4. **Evaluator fourth**: Add environment-passing eval in Eval.fs
5. **Format fifth**: Update Format.fs for new tokens
6. **Program last**: Update Program.fs to use evalExpr
7. **Tests**: Add comprehensive test cases

This order ensures each component can be tested incrementally.
