# Phase 4: Control Flow - Research

**Researched:** 2026-01-30
**Domain:** Parser generators (FsLex/FsYacc), discriminated unions, expression evaluators with heterogeneous types
**Confidence:** HIGH

## Summary

Control flow implementation for FunLang requires adding if-then-else expressions, boolean types, comparison operators, and logical operators to the existing expression evaluator. The research focused on three key technical areas: extending the AST with discriminated unions to handle heterogeneous types (int and bool), implementing operator precedence correctly in fsyacc grammar, and avoiding common pitfalls like the dangling else problem and incorrect type handling.

F# discriminated unions provide the standard solution for representing expression ASTs with multiple value types. The evaluator must return a discriminated union value type rather than a fixed type, enabling expressions to evaluate to either int or bool. FsYacc precedence declarations (%left, %right, %nonassoc) resolve operator precedence and associativity, with the standard hierarchy: arithmetic > comparison > logical AND > logical OR.

The most critical design decision is resolving the "dangling else" shift-reduce conflict in the parser grammar. FsYacc's default behavior (shift over reduce) correctly implements the standard convention of binding else to the nearest if, matching expectations from languages like C, Java, and Python.

**Primary recommendation:** Introduce a Value discriminated union (Int of int | Bool of bool) as the evaluator's return type, use %left/%right precedence declarations for all operators with standard precedence ordering, and rely on fsyacc's default shift behavior to resolve dangling else correctly.

## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| FsLexYacc | Latest | Lexer/parser generation for F# | Official F# port of lex/yacc, MSBuild integrated, maintained by fsprojects |
| F# discriminated unions | Built-in | AST representation with heterogeneous types | Type-safe, exhaustive pattern matching, compiler-enforced case handling |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Map<string, Value> | Built-in | Environment for variable storage | Already in use (Phase 3), extend to store Value instead of int |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Value discriminated union | obj with runtime type checks | Loses type safety, runtime errors instead of compile-time checks |
| %left/%right precedence | Rewrite grammar to be unambiguous | More complex grammar, harder to maintain, no practical benefit |
| fsyacc | FParsec parser combinator | Overkill for simple expression language, steeper learning curve |

**Installation:**
```bash
# Already installed from Phase 1
# FsLexYacc is in project via NuGet
```

## Architecture Patterns

### Recommended Project Structure
```
FunLang/
├── Ast.fs              # Extend Expr with: Bool, If, comparison ops, logical ops
├── Eval.fs             # Change return type from int to Value, add type checking
├── Lexer.fsl           # Add tokens: TRUE, FALSE, IF, THEN, ELSE, LT, GT, LE, GE, NE, AND, OR
├── Parser.fsy          # Add precedence declarations and grammar rules for new constructs
└── Program.fs          # Update to handle Value results instead of int
```

### Pattern 1: Value Type for Heterogeneous Results

**What:** Introduce a discriminated union to represent values of different types that expressions can evaluate to.

**When to use:** When your expression evaluator needs to return different types (int, bool, etc.) from the same eval function.

**Example:**
```fsharp
// Source: Microsoft Learn - Discriminated Unions
// https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/discriminated-unions

// In Ast.fs
type Value =
    | IntValue of int
    | BoolValue of bool

// In Eval.fs
let rec eval (env: Map<string, Value>) (expr: Expr) : Value =
    match expr with
    | Number n -> IntValue n
    | Bool b -> BoolValue b
    | Add (left, right) ->
        match (eval env left, eval env right) with
        | (IntValue a, IntValue b) -> IntValue (a + b)
        | _ -> failwithf "Type error: + expects int operands"
    | If (cond, thenBranch, elseBranch) ->
        match eval env cond with
        | BoolValue true -> eval env thenBranch
        | BoolValue false -> eval env elseBranch
        | _ -> failwithf "Type error: if condition must be bool"
```

### Pattern 2: Operator Precedence Declarations

**What:** Use %left, %right, %nonassoc in fsyacc to declare operator precedence and associativity.

**When to use:** Always - for all binary operators. Prevents shift-reduce conflicts and implements correct precedence.

**Example:**
```fsharp
// Source: FsYacc documentation and Bison manual
// https://www.gnu.org/software/bison/manual/html_node/Precedence-Decl.html

// In Parser.fsy, after token declarations:
// Precedence: lower precedence first, higher later
// Associativity: %left (left-to-right), %right (right-to-left), %nonassoc (non-associative)

%left OR           // Lowest precedence: ||
%left AND          // Logical AND: &&
%nonassoc LT GT LE GE EQUALS NE  // Comparison: =, <, >, <=, >=, <>
%left PLUS MINUS   // Addition/Subtraction
%left STAR SLASH   // Multiplication/Division (highest precedence)

// Grammar rules can then be simple without explicit precedence handling:
Expr:
    | Expr OR Expr      { Or($1, $3) }
    | Expr AND Expr     { And($1, $3) }
    | Expr LT Expr      { LessThan($1, $3) }
    | Expr PLUS Expr    { Add($1, $3) }
```

### Pattern 3: If-Then-Else Grammar Rule

**What:** Add if-then-else as a low-precedence expression form.

**When to use:** For conditional expressions in the language.

**Example:**
```fsharp
// Source: F# Compiler Guide - Expressions
// https://fsharp.github.io/fsharp-compiler-docs/fcs/untypedtree.html

// In Parser.fsy
Expr:
    | IF Expr THEN Expr ELSE Expr  { If($2, $4, $6) }
    | Expr OR Expr                  { Or($1, $3) }
    | LogicalAnd                    { $1 }

// Place if-then-else early in the grammar (low precedence)
// This allows the branches to contain any expression
```

### Pattern 4: Short-Circuit Evaluation (Optional Enhancement)

**What:** Logical operators && and || should skip evaluating the right operand when the result is determined by the left operand.

**When to use:** For correctness and efficiency with boolean operators.

**Example:**
```fsharp
// Source: Microsoft Learn - Boolean Operators
// https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/symbol-and-operator-reference/boolean-operators

// In Eval.fs
| And (left, right) ->
    match eval env left with
    | BoolValue false -> BoolValue false  // Short-circuit: don't eval right
    | BoolValue true ->
        match eval env right with
        | BoolValue b -> BoolValue b
        | _ -> failwithf "Type error: && expects bool operands"
    | _ -> failwithf "Type error: && expects bool operands"

| Or (left, right) ->
    match eval env left with
    | BoolValue true -> BoolValue true  // Short-circuit: don't eval right
    | BoolValue false ->
        match eval env right with
        | BoolValue b -> BoolValue b
        | _ -> failwithf "Type error: || expects bool operands"
    | _ -> failwithf "Type error: || expects bool operands"
```

### Anti-Patterns to Avoid

- **Returning obj from evaluator:** Using F# obj type loses type safety and forces runtime type checks everywhere. Discriminated unions provide compile-time type safety.

- **Hand-rolled precedence in grammar:** Writing explicit grammar levels without %left/%right leads to more complex, harder to maintain grammars with no benefit.

- **Comparing precedence levels incorrectly:** Declaring %left PLUS before %left STAR makes addition higher precedence than multiplication - opposite of mathematical convention. Order matters: first declaration is lowest precedence.

- **Forgetting %nonassoc for comparisons:** Using %left for comparison operators like < and > allows chains like "1 < 2 < 3" which don't make semantic sense (would parse as "(1 < 2) < 3"). Use %nonassoc to make them non-associative.

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Operator precedence | Manual grammar stratification | %left/%right/%nonassoc | Precedence declarations are standard, clearer, less error-prone than multi-level grammar |
| Short-circuit evaluation | Always eval both operands | Pattern match left, conditionally eval right | Short-circuit is expected behavior, prevents unnecessary computation and errors |
| Dangling else resolution | Rewrite grammar to be unambiguous | Rely on fsyacc default shift behavior | Default shift implements standard binding (else to nearest if), matches all major languages |
| Type checking in AST | Runtime checks with exceptions | F# discriminated unions with exhaustive matching | Compile-time safety, impossible to forget cases, clear error messages |

**Key insight:** Parser generators like fsyacc have decades of established patterns for these problems. The precedence declaration mechanism is specifically designed to handle operator precedence elegantly. Discriminated unions are F#'s idiomatic way to represent heterogeneous data with type safety.

## Common Pitfalls

### Pitfall 1: Dangling Else Ambiguity
**What goes wrong:** Nested if statements create shift-reduce conflicts when parsing else clauses. Example: "if c1 then if c2 then e1 else e2" - does else bind to inner or outer if?

**Why it happens:** Grammar is ambiguous - both interpretations are valid parses. The parser doesn't know whether to shift (continue building inner if-then-else) or reduce (complete inner if-then, bind else to outer).

**How to avoid:** Rely on fsyacc's default behavior (shift over reduce), which binds else to the nearest if. This matches C, Java, Python, and other languages. Do not try to "fix" this with manual grammar rewrites.

**Warning signs:** Fsyacc reports "shift/reduce conflict on ELSE". This is expected and benign - verify the default shift resolves it correctly.

### Pitfall 2: Incorrect Operator Precedence Ordering
**What goes wrong:** Declaring precedence in wrong order causes incorrect evaluation. Example: %left STAR before %left PLUS makes "2 + 3 * 4" evaluate as "(2 + 3) * 4 = 20" instead of "2 + (3 * 4) = 14".

**Why it happens:** Precedence declarations are ordered from lowest to highest. First declaration has lowest precedence. Intuition often gets this backwards.

**How to avoid:** Remember mnemonic "Low First, High Last". Start with OR (lowest), end with STAR/SLASH (highest). Follow standard precedence: logical OR < logical AND < comparison < addition/subtraction < multiplication/division.

**Warning signs:** Arithmetic expressions evaluate incorrectly. Test "2 + 3 * 4" and "10 - 2 - 3" to verify precedence and associativity.

### Pitfall 3: Forgetting Type Checks in Evaluator
**What goes wrong:** Operations get wrong types: "Add (Bool true, Bool false)" or "If (Number 42, ...)". This causes runtime exceptions or wrong results.

**Why it happens:** After adding Value type, eval returns Value but operations expect specific types. Pattern matching extracts values but doesn't enforce types.

**How to avoid:** Add exhaustive type checking with clear error messages for every operation. Use nested pattern matching: "match (eval env left, eval env right) with | (IntValue a, IntValue b) -> ... | _ -> failwithf 'type error'".

**Warning signs:** Runtime "type error" messages or strange results like treating true as 1. Write tests for type errors.

### Pitfall 4: Using = for Comparison Instead of ==
**What goes wrong:** F# uses = for equality comparison, but many languages use ==. Using = in both lexer (for token) and semantics (for comparison operator) can be confusing.

**Why it happens:** Lexical collision - EQUALS token is used for both "let x = 5" (binding) and "x = 5" (comparison). Parser context disambiguates but it's error-prone.

**How to avoid:** The requirements specify = for comparison, which is consistent with let bindings. Accept this design. Be clear in tests which = is being used.

**Warning signs:** Parser confusion between let bindings and comparisons. Ensure "let x = 5 in x = 5" parses correctly as "let x = 5 in (x = 5)".

### Pitfall 5: Comparison Operators with Wrong Associativity
**What goes wrong:** Allowing "1 < 2 < 3" to parse (as it does in Python) but it evaluates as "(1 < 2) < 3" = "true < 3" which is a type error in typed languages.

**Why it happens:** Using %left for comparison operators makes them left-associative, allowing chains.

**How to avoid:** Use %nonassoc for comparison operators (LT, GT, LE, GE, EQUALS, NE). This makes "1 < 2 < 3" a parse error, forcing users to write "1 < 2 && 2 < 3".

**Warning signs:** Parser accepts "a < b < c" expressions. This should be a parse error, not a type error.

## Code Examples

Verified patterns from official sources:

### AST Extension with If and Boolean Operations
```fsharp
// Source: Microsoft Learn - Discriminated Unions
// https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/discriminated-unions

// Ast.fs
module Ast

type Value =
    | IntValue of int
    | BoolValue of bool

type Expr =
    // Phase 2: Arithmetic
    | Number of int
    | Add of Expr * Expr
    | Subtract of Expr * Expr
    | Multiply of Expr * Expr
    | Divide of Expr * Expr
    | Negate of Expr
    // Phase 3: Variables
    | Var of string
    | Let of string * Expr * Expr
    // Phase 4: Control Flow
    | Bool of bool                           // Boolean literals
    | If of Expr * Expr * Expr              // if-then-else
    | LessThan of Expr * Expr               // <
    | GreaterThan of Expr * Expr            // >
    | LessOrEqual of Expr * Expr            // <=
    | GreaterOrEqual of Expr * Expr         // >=
    | Equal of Expr * Expr                  // =
    | NotEqual of Expr * Expr               // <>
    | And of Expr * Expr                    // &&
    | Or of Expr * Expr                     // ||
```

### Evaluator with Type Checking
```fsharp
// Source: Microsoft Learn - Discriminated Unions, Pattern Matching
// https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/discriminated-unions

// Eval.fs
module Eval

open Ast

type Env = Map<string, Value>
let emptyEnv : Env = Map.empty

let rec eval (env: Env) (expr: Expr) : Value =
    match expr with
    | Number n -> IntValue n
    | Bool b -> BoolValue b

    | Var name ->
        match Map.tryFind name env with
        | Some value -> value
        | None -> failwithf "Undefined variable: %s" name

    | Let (name, binding, body) ->
        let value = eval env binding
        let extendedEnv = Map.add name value env
        eval extendedEnv body

    | Add (left, right) ->
        match (eval env left, eval env right) with
        | (IntValue a, IntValue b) -> IntValue (a + b)
        | _ -> failwithf "Type error: + expects int operands"

    | LessThan (left, right) ->
        match (eval env left, eval env right) with
        | (IntValue a, IntValue b) -> BoolValue (a < b)
        | _ -> failwithf "Type error: < expects int operands"

    | And (left, right) ->
        match eval env left with
        | BoolValue false -> BoolValue false  // Short-circuit
        | BoolValue true ->
            match eval env right with
            | BoolValue b -> BoolValue b
            | _ -> failwithf "Type error: && expects bool operands"
        | _ -> failwithf "Type error: && expects bool operands"

    | If (cond, thenBranch, elseBranch) ->
        match eval env cond with
        | BoolValue true -> eval env thenBranch
        | BoolValue false -> eval env elseBranch
        | _ -> failwithf "Type error: if condition must be bool"

    // ... other cases

let evalExpr (expr: Expr) : Value =
    eval emptyEnv expr
```

### Lexer Extensions
```fsharp
// Source: FsLexYacc JSON Parser Example
// https://github.com/fsprojects/FsLexYacc/blob/master/docs/content/jsonParserExample.md

// Lexer.fsl additions
rule tokenize = parse
    // ... existing rules

    // Phase 4 keywords - MUST come before identifier pattern
    | "true"        { TRUE }
    | "false"       { FALSE }
    | "if"          { IF }
    | "then"        { THEN }
    | "else"        { ELSE }

    // Comparison operators
    | "<="          { LE }
    | ">="          { GE }
    | "<>"          { NE }
    | '<'           { LT }
    | '>'           { GT }
    // EQUALS already exists for let binding

    // Logical operators
    | "&&"          { AND }
    | "||"          { OR }
```

### Parser Extensions with Precedence
```fsharp
// Source: Bison Manual - Precedence Declarations
// https://www.gnu.org/software/bison/manual/html_node/Precedence-Decl.html

// Parser.fsy

%{
open Ast
%}

// Token declarations
%token <int> NUMBER
%token <string> IDENT
%token PLUS MINUS STAR SLASH
%token LPAREN RPAREN
%token LET IN EQUALS
%token TRUE FALSE IF THEN ELSE
%token LT GT LE GE NE
%token AND OR
%token EOF

// Precedence declarations (lowest to highest)
%left OR
%left AND
%nonassoc LT GT LE GE EQUALS NE
%left PLUS MINUS
%left STAR SLASH

%start start
%type <Ast.Expr> start

%%

start:
    | Expr EOF           { $1 }

Expr:
    // Control flow - lowest precedence (placed first)
    | IF Expr THEN Expr ELSE Expr  { If($2, $4, $6) }
    // Let expression
    | LET IDENT EQUALS Expr IN Expr  { Let($2, $4, $6) }
    // Logical OR
    | Expr OR Expr       { Or($1, $3) }
    // Logical AND (higher precedence, but %left handles it)
    | Expr AND Expr      { And($1, $3) }
    // Comparisons
    | Expr LT Expr       { LessThan($1, $3) }
    | Expr GT Expr       { GreaterThan($1, $3) }
    | Expr LE Expr       { LessOrEqual($1, $3) }
    | Expr GE Expr       { GreaterOrEqual($1, $3) }
    | Expr EQUALS Expr   { Equal($1, $3) }
    | Expr NE Expr       { NotEqual($1, $3) }
    // Arithmetic (precedence handled by %left declarations)
    | Expr PLUS Expr     { Add($1, $3) }
    | Expr MINUS Expr    { Subtract($1, $3) }
    | Expr STAR Expr     { Multiply($1, $3) }
    | Expr SLASH Expr    { Divide($1, $3) }
    // Atoms and high-precedence unary
    | NUMBER             { Number($1) }
    | TRUE               { Bool(true) }
    | FALSE              { Bool(false) }
    | IDENT              { Var($1) }
    | LPAREN Expr RPAREN { $2 }
    | MINUS Expr         { Negate($2) }
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Manual grammar stratification for precedence | %left/%right declarations | 1970s (original yacc) | Simpler grammars, less error-prone, easier to modify |
| Unambiguous grammars for dangling else | Ambiguous grammar + default shift | 1970s (yacc/bison convention) | Simpler grammar, standard behavior across languages |
| obj/dynamic types for heterogeneous values | Discriminated unions | F# design (2000s) | Type safety, exhaustive matching, better error messages |
| Non-short-circuit logical operators | Short-circuit && and || | 1960s-70s (Algol, C) | Efficiency, prevents evaluation errors, matches expectations |

**Deprecated/outdated:**
- Using separate expression categories (BoolExpr, IntExpr) instead of unified Expr type: Modern approach uses single Expr type with Value result type
- Writing separate parsers for different expression types: Unified grammar with precedence declarations is standard
- FSharp.PowerPack fslex/fsyacc: Replaced by community FsLexYacc project (fsprojects/FsLexYacc)

## Open Questions

Things that couldn't be fully resolved:

1. **Should if-then-else be an expression or statement?**
   - What we know: F# if-then-else is an expression (returns a value). Requirements show "if c1 then e1 else e2" suggesting expression form.
   - What's unclear: Whether both branches must return same type (strong typing) or can differ (dynamic typing).
   - Recommendation: Make it an expression (returns Value). Both branches can return any Value, runtime type doesn't need to match. Defer proper type checking to Phase 6.

2. **How to format Value results for display?**
   - What we know: Current Program.fs prints int results. After Phase 4, results are Value (IntValue or BoolValue).
   - What's unclear: Display format - "IntValue(42)" vs "42", "BoolValue(true)" vs "true".
   - Recommendation: Add a simple formatter: "let formatValue = function | IntValue i -> string i | BoolValue b -> if b then 'true' else 'false'". Print user-friendly form.

3. **Should Environment store Value or continue with int?**
   - What we know: Phase 3 uses Map<string, int>. Phase 4 needs variables to hold bool values too.
   - What's unclear: Whether variables should be typed or dynamically typed.
   - Recommendation: Change to Map<string, Value>. Variables can hold any value type. This is simpler and defers typing decisions to Phase 6.

## Sources

### Primary (HIGH confidence)
- Microsoft Learn - Discriminated Unions: https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/discriminated-unions
- Microsoft Learn - Boolean Operators: https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/symbol-and-operator-reference/boolean-operators
- Microsoft Learn - Conditional Expressions: https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/conditional-expressions-if-then-else
- F# Compiler Guide - Expressions: https://fsharp.github.io/fsharp-compiler-docs/fcs/untypedtree.html
- FsLexYacc GitHub Documentation: https://github.com/fsprojects/FsLexYacc
- FsLexYacc JSON Parser Example: https://github.com/fsprojects/FsLexYacc/blob/master/docs/content/jsonParserExample.md
- GNU Bison Manual - Precedence Declarations: https://www.gnu.org/software/bison/manual/html_node/Precedence-Decl.html
- GNU Bison Manual - Shift/Reduce Conflicts: https://www.gnu.org/software/bison/manual/html_node/Shift_002fReduce.html

### Secondary (MEDIUM confidence)
- F# for Fun and Profit - Discriminated Unions: https://fsharpforfunandprofit.com/posts/discriminated-unions/
- F# Wikibooks - Lexing and Parsing: https://en.wikibooks.org/wiki/F_Sharp_Programming/Lexing_and_Parsing
- Wikipedia - Dangling Else: https://en.wikipedia.org/wiki/Dangling_else
- Wikipedia - Operator Precedence Parser: https://en.wikipedia.org/wiki/Operator-precedence_parser
- MDN - Operator Precedence: https://developer.mozilla.org/en-us/docs/Web/JavaScript/Reference/Operators/Operator_precedence (for general precedence patterns)
- GeeksforGeeks - Operator Grammar and Precedence Parser: https://www.geeksforgeeks.org/compiler-design/operator-grammar-and-precedence-parser-in-toc/

### Tertiary (LOW confidence)
- Various tutorials on operator precedence: Stanford, Princeton, javatpoint (general principles, not F# specific)
- Blog posts: thanos.codes, partario.com (older examples, FsLexYacc may have evolved)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - FsLexYacc is official, discriminated unions are F# core feature
- Architecture: HIGH - Patterns verified from Microsoft Learn and F# compiler source
- Pitfalls: HIGH - Dangling else is well-documented classic problem, precedence errors are common and well-understood

**Research date:** 2026-01-30
**Valid until:** 2026-02-28 (30 days - stable domain, F# language features don't change rapidly)
