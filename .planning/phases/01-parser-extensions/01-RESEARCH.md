# Phase 1: Parser Extensions - Research

**Researched:** 2026-02-03
**Domain:** FsLex/FsYacc Parser Extension with Type Annotation Syntax
**Confidence:** HIGH

## Summary

This phase extends the existing FunLang lexer and parser to support ML-style type annotation syntax. The goal is to parse type expressions (like `int`, `'a -> int`, `int * bool`) and annotated expressions (like `fun (x: int) -> x + 1`) without breaking backward compatibility with existing unannotated code.

**Key challenges identified:**
1. **Token ordering conflict**: Type keywords (`int`, `bool`, `string`, `list`) can conflict with identifiers if lexer patterns are not ordered correctly
2. **Type variable ambiguity**: The `'a` syntax for type variables needs special lexing since `'` is not typically part of identifiers
3. **Colon operator conflict**: The `:` token for annotations conflicts with the `::` cons operator - must be ordered carefully
4. **Parser precedence**: Type expressions have their own precedence rules (arrow is right-associative, tuple has higher precedence than arrow)
5. **Backward compatibility**: All existing tests (98 fslit + 362 Expecto) must continue passing

**Primary recommendation:** Follow the established fslex/fsyacc pattern of (1) keywords before identifiers in lexer, (2) use a separate `TypeExpr` non-terminal with proper precedence, (3) make annotations optional to preserve backward compatibility, (4) add comprehensive tests incrementally.

## Standard Stack

The project already uses the standard F# parser toolchain. No new libraries are needed for Phase 1.

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| FsLexYacc | (current) | Lexer/Parser generator | Industry standard for F# language implementations |
| FSharp.Text.Lexing | Built-in | Lexer runtime support | Part of F# standard library |
| FSharp.Text.Parsing | Built-in | Parser runtime support | Part of F# standard library |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Expecto | (current) | Unit testing | Already in use for parser tests |
| fslit | (current) | CLI integration tests | Already in use for end-to-end tests |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| FsLexYacc | FParsec | FParsec offers parser combinators (more flexible) but requires rewriting entire parser. FsLexYacc maintains consistency with tutorial's educational approach. |
| Hand-written lexer | Custom F# code | More control but loses automatic token generation, position tracking, and maintainability |

**Installation:**
No new packages needed. The project already has FsLexYacc configured in FunLang.fsproj.

## Architecture Patterns

### Recommended Project Structure
Current structure is appropriate - extensions will be in-place:
```
FunLang/
├── Lexer.fsl        # Add: COLON, TYPE_INT/BOOL/STRING/LIST, TYPE_VAR tokens
├── Parser.fsy       # Add: TypeExpr non-terminal, Annot/LambdaAnnot rules
└── Ast.fs           # Add: TypeExpr, Annot, LambdaAnnot AST nodes
```

### Pattern 1: Token Declaration Order in Lexer

**What:** In fslex, pattern matching is first-match, so order matters critically.

**When to use:** Always when adding keywords that could conflict with identifiers.

**Example:**
```fsharp
// Source: Existing Lexer.fsl (lines 35-52)
rule tokenize = parse
    // Keywords MUST come before identifier pattern
    | "true"        { TRUE }
    | "false"       { FALSE }
    | "if"          { IF }
    | "let"         { LET }
    // NEW: Type keywords before identifier
    | "int"         { TYPE_INT }
    | "bool"        { TYPE_BOOL }
    | "string"      { TYPE_STRING }
    | "list"        { TYPE_LIST }
    // Identifier: starts with letter or underscore
    | ident_start ident_char* { IDENT (lexeme lexbuf) }
```

**Critical insight:** If `| ident_start ident_char*` comes before `| "int"`, then "int" will always be lexed as IDENT "int", never as TYPE_INT. Keywords must be listed first.

### Pattern 2: Type Variable Token with Apostrophe

**What:** ML-style type variables use `'a`, `'b` syntax. The apostrophe must be recognized as part of the token.

**When to use:** For polymorphic type syntax.

**Example:**
```fsharp
// Define character class for type variables
let type_var = '\'' letter+   // 'a, 'b, 'result, etc.

rule tokenize = parse
    // Type variable: must come AFTER keywords but BEFORE identifiers
    // to avoid conflicts with string literals or identifiers
    | type_var      { TYPE_VAR (lexeme lexbuf) }  // includes the '
```

**Rationale:** The apostrophe `'` is used for character/string literals in some contexts, so the type variable pattern must be specific: `'` followed immediately by letters, no whitespace.

### Pattern 3: Multi-Character Operator Ordering

**What:** The lexer already demonstrates proper handling of multi-char operators vs single-char.

**When to use:** When adding `:` for annotations alongside existing `::` for cons.

**Example:**
```fsharp
// Source: Existing Lexer.fsl (lines 55-64) - already correct pattern
rule tokenize = parse
    // Multi-char operators MUST come before single-char
    | "::"          { CONS }    // Cons operator (existing)
    | "->"          { ARROW }   // Arrow (existing)
    // Single-char operators
    | ':'           { COLON }   // NEW: Type annotation
```

**Critical:** If `| ':'` comes before `| "::"`, the lexer will tokenize `:` `:` (two COLON tokens) instead of a single CONS token. The existing lexer already follows this pattern correctly.

### Pattern 4: TypeExpr Grammar with Precedence

**What:** Type expressions have their own operator precedence separate from value expressions.

**When to use:** When parsing type syntax like `int -> bool -> string` or `int * bool`.

**Example:**
```fsharp
// Source: OCaml type expression grammar (OCaml Manual 5.4, types.html)
// Adapted for FsYacc

// Precedence (highest to lowest):
// 1. Type constructor application (TEList T)
// 2. Tuple (*) - non-associative
// 3. Arrow (->) - right associative

TypeExpr:
    | ArrowType                { $1 }

// Arrow is right-associative: int -> int -> int = int -> (int -> int)
ArrowType:
    | TupleType ARROW ArrowType  { TEArrow($1, $3) }
    | TupleType                  { $1 }

// Tuple types: int * bool * string
TupleType:
    | AtomicType STAR TupleTypeList  { TETuple($1 :: $3) }
    | AtomicType                      { $1 }

TupleTypeList:
    | AtomicType STAR TupleTypeList  { $1 :: $3 }
    | AtomicType                     { [$1] }

// Atomic types: base types, type variables, list types, parenthesized
AtomicType:
    | TYPE_INT                    { TEInt }
    | TYPE_BOOL                   { TEBool }
    | TYPE_STRING                 { TEString }
    | TYPE_VAR                    { TEVar($1) }  // 'a, 'b
    | AtomicType TYPE_LIST        { TEList($1) }
    | LPAREN TypeExpr RPAREN      { $2 }
```

**Rationale:** This three-tier structure (Arrow → Tuple → Atomic) ensures correct precedence without requiring `%left`/`%right` declarations for type operators, avoiding potential conflicts with existing value expression precedence.

### Pattern 5: Optional Annotations for Backward Compatibility

**What:** Type annotations should be optional so all existing unannotated code continues to parse.

**When to use:** When extending existing syntax.

**Example:**
```fsharp
// Existing lambda (no annotation) - MUST still work
Expr:
    | FUN IDENT ARROW Expr                     { Lambda($2, $4, ...) }

// NEW: Annotated lambda - add as separate rule
    | FUN LPAREN IDENT COLON TypeExpr RPAREN ARROW Expr
        { LambdaAnnot($3, $5, $8, ...) }

// Existing expression in parens - MUST still work
Atom:
    | LPAREN Expr RPAREN                       { $2 }

// NEW: Annotated expression - requires different syntax to avoid ambiguity
    | LPAREN Expr COLON TypeExpr RPAREN        { Annot($2, $4, ...) }
```

**Critical:** Adding new rules should not break existing rules. The grammar must remain non-ambiguous. Annotated forms require additional tokens (like `LPAREN` + `COLON`) to disambiguate from unannotated forms.

### Pattern 6: Curried Multi-Parameter Annotations

**What:** Support `fun (x: int) (y: int) -> e` for consistency with FunLang's curried function style.

**When to use:** To match the existing currying semantics.

**Example:**
```fsharp
// Single parameter (base case)
Expr:
    | FUN AnnotParam ARROW Expr
        { LambdaAnnot(fst $2, snd $2, $4, ...) }

// Multiple parameters via recursion (currying)
    | FUN AnnotParam Expr
        { LambdaAnnot(fst $2, snd $2, $3, ...) }

AnnotParam:
    | LPAREN IDENT COLON TypeExpr RPAREN
        { ($2, $4) }  // returns (param_name, param_type)
```

**Rationale:** This allows `fun (x: int) (y: int) -> x + y` to parse as nested lambdas, consistent with how `fun x y -> x + y` already works. The recursive structure matches the existing application pattern.

### Anti-Patterns to Avoid

- **Don't use `%token COLON` without checking existing operators**: The single-colon token must come AFTER multi-char operators like `::` in lexer rules to avoid breaking cons operator.
- **Don't reuse existing expression precedence for types**: Type expressions (`int -> bool`) have different associativity than value expressions (`2 + 3`). Use a separate non-terminal hierarchy.
- **Don't make annotations mandatory**: This breaks backward compatibility. All existing code must parse without annotations.
- **Don't use keyword-style tokens for type constructors without lexer precedence**: If `list` is both a keyword (TYPE_LIST) and a valid identifier, the keyword pattern must come first in the lexer.

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Parser precedence conflicts | Manual conflict resolution, grammar rewriting | FsYacc's `%left`, `%right`, `%nonassoc` declarations | FsYacc automatically resolves shift/reduce conflicts based on precedence. Manual resolution is error-prone. However, for type expressions, use non-terminal hierarchy instead of precedence declarations to avoid conflicts with existing value operators. |
| Position tracking for error messages | Custom span calculation | FsYacc's `parseState.InputStartPosition`, `parseState.InputEndPosition` | Already implemented in `Parser.fsy` via `ruleSpan` and `symSpan` helper functions. These use FsYacc's built-in position tracking. |
| Token generation | Manual token union type | FsYacc generates token types automatically | Parser.fsy's `%token` declarations generate a union type that Lexer.fsl imports via `open Parser`. Hand-rolling breaks this integration. |
| Keyword vs identifier disambiguation | Runtime keyword map lookup | Lexer pattern ordering | For simple keyword sets, pattern ordering (keywords before identifier) is simpler than `match keywords.TryFind(lexeme) ...` runtime lookup. Current lexer uses pattern ordering successfully. |

**Key insight:** FsLexYacc's design requires parser-first workflow: `Parser.fsy` generates token types → `Lexer.fsl` imports them. Don't try to generate tokens independently.

## Common Pitfalls

### Pitfall 1: Lexer Pattern Ordering

**What goes wrong:** Adding type keywords (`int`, `bool`, `string`) after the identifier rule causes them to always lex as identifiers, breaking the grammar.

**Why it happens:** FsLex uses first-match semantics. Once `| ident_start ident_char*` matches "int", it returns IDENT "int" and never tries subsequent patterns.

**How to avoid:**
1. Always place keyword patterns before the identifier pattern in the lexer
2. Add comments marking the critical ordering: `// Keywords MUST come before identifier`
3. Test lexer output explicitly: `dotnet run --project FunLang -- --emit-tokens %input`

**Warning signs:**
- Parser syntax errors on valid input like `fun (x: int) -> x`
- `--emit-tokens` shows `IDENT "int"` instead of `TYPE_INT`
- Parser conflicts reported during `fsyacc` generation

### Pitfall 2: Colon vs Cons Operator Conflict

**What goes wrong:** If single `| ':'` comes before multi-char `| "::"` in the lexer, the cons operator `::` is lexed as two separate COLON tokens.

**Why it happens:** First-match semantics: lexer sees `:` at position 0, matches `':'` pattern, returns COLON, then sees `:` at position 1, matches again, returns second COLON.

**How to avoid:**
1. The existing lexer already follows the correct pattern: multi-char operators before single-char (lines 55-77)
2. When adding `| ':'  { COLON }`, place it AFTER `| "::"  { CONS }`
3. Verify with tests: `1 :: []` should tokenize as `NUMBER CONS LBRACKET RBRACKET`, not `NUMBER COLON COLON ...`

**Warning signs:**
- Existing list tests (`tests/control/`, `tests/functions/`) start failing
- `--emit-tokens` shows `COLON COLON` for input `1 :: []`
- Parser reports "unexpected COLON" in cons expressions

### Pitfall 3: Type Expression Ambiguity

**What goes wrong:** Using the same precedence declarations for type expressions and value expressions causes shift/reduce conflicts or incorrect parsing.

**Why it happens:** Type arrow `->` is right-associative (int -> int -> int), but in value expressions, operators like `+` are left-associative. FsYacc's precedence declarations are global.

**How to avoid:**
1. Use a separate non-terminal hierarchy for types (`ArrowType → TupleType → AtomicType`)
2. DO NOT use `%right ARROW` for type expressions if ARROW is already used in value expressions
3. Encode precedence structurally through grammar rules, not via `%left`/`%right`

**Warning signs:**
- FsYacc reports shift/reduce conflicts when adding type rules
- Type `int -> int -> int` parses incorrectly as left-associative
- Existing function expression parsing changes unexpectedly

### Pitfall 4: Annotation Ambiguity

**What goes wrong:** Adding annotation syntax `(e : T)` creates ambiguity with existing parenthesized expressions `(e)` in the grammar.

**Why it happens:** Both start with `LPAREN Expr`, and the parser must decide whether to reduce or shift when it sees `)` vs `:`.

**How to avoid:**
1. Keep `LPAREN Expr RPAREN` as the existing atom rule (no annotation)
2. Add `LPAREN Expr COLON TypeExpr RPAREN` as a separate annotation rule
3. The extra `COLON` token disambiguates the two forms
4. Test both: `(1 + 2)` should parse as before, `(1 + 2 : int)` as annotation

**Warning signs:**
- FsYacc reports shift/reduce conflicts in `Atom` or `Factor` rules
- Simple parenthesized expressions like `(1 + 2)` fail to parse
- Reduce/reduce conflicts between multiple parenthesized forms

### Pitfall 5: Backward Compatibility Breakage

**What goes wrong:** Existing tests fail after parser changes, even though no syntax was "removed."

**Why it happens:**
- Changing precedence affects existing expression parsing
- Adding tokens changes the generated parser state machine
- Grammar ambiguities introduce non-deterministic parsing

**How to avoid:**
1. Run full test suite BEFORE changes: `make -C tests && dotnet run --project FunLang.Tests`
2. Make changes incrementally: tokens first, then simple type grammar, then annotations
3. Run test suite after EACH change
4. Keep annotations optional - don't modify existing rules unnecessarily

**Warning signs:**
- Even one existing test fails: this is a breaking change
- Parser.fs has different line count (indicates state machine changed)
- Test suite shows different error messages for previously working code

### Pitfall 6: Type Variable Lexing

**What goes wrong:** Type variable `'a` is not recognized, or conflicts with character literal syntax `'x'`.

**Why it happens:** Apostrophe `'` is overloaded: in type syntax it prefixes type variables, in expression syntax it delimits character literals (not yet implemented in FunLang, but standard F#).

**How to avoid:**
1. Define type variable pattern specifically: `'\'' letter+` (apostrophe followed by letters)
2. Place type variable pattern BEFORE identifier pattern but AFTER keywords
3. If character literals are added later, they use different pattern: `'\'' any_char '\''` (apostrophe-char-apostrophe)
4. FunLang doesn't currently have character literals, so type variables can safely claim `'letter` syntax

**Warning signs:**
- Parser fails on `'a -> 'a` type expressions
- Lexer reports unexpected character `'`
- Type variable token not generated by lexer

## Code Examples

Verified patterns from the existing codebase and ML language standards:

### Example 1: Lexer Token Additions

```fsharp
// Source: Lexer.fsl, adapted from existing pattern (lines 22-86)

// Character class definitions (existing)
let digit = ['0'-'9']
let letter = ['a'-'z' 'A'-'Z']
let ident_start = letter | '_'
let ident_char = letter | digit | '_'

// NEW: Type variable definition
let type_var = '\'' letter (letter | digit | '_')*

// Lexer rules
rule tokenize = parse
    | whitespace+   { tokenize lexbuf }
    | newline       { lexbuf.EndPos <- lexbuf.EndPos.NextLine; tokenize lexbuf }
    | digit+        { NUMBER (Int32.Parse(lexeme lexbuf)) }

    // Keywords MUST come before identifier pattern
    | "true"        { TRUE }
    | "false"       { FALSE }
    | "if"          { IF }
    | "then"        { THEN }
    | "else"        { ELSE }
    | "let"         { LET }
    | "in"          { IN }
    | "fun"         { FUN }
    | "rec"         { REC }
    | "match"       { MATCH }
    | "with"        { WITH }

    // NEW: Type keywords (BEFORE identifier)
    | "int"         { TYPE_INT }
    | "bool"        { TYPE_BOOL }
    | "string"      { TYPE_STRING }
    | "list"        { TYPE_LIST }

    // Wildcard pattern (BEFORE identifier)
    | '_'           { UNDERSCORE }

    // NEW: Type variables (BEFORE identifier)
    | type_var      { TYPE_VAR (lexeme lexbuf) }

    // Identifier: starts with letter or underscore (but _ alone is UNDERSCORE)
    | ident_start ident_char* { IDENT (lexeme lexbuf) }

    // String literals
    | '"'           { read_string (System.Text.StringBuilder()) lexbuf }

    // Multi-char operators MUST come before single-char
    | "<="          { LE }
    | ">="          { GE }
    | "<>"          { NE }
    | "&&"          { AND }
    | "||"          { OR }
    | "::"          { CONS }
    | "->"          { ARROW }

    // Single-char operators
    | '+'           { PLUS }
    | '-'           { MINUS }
    | '*'           { STAR }
    | '/'           { SLASH }
    | '<'           { LT }
    | '>'           { GT }
    | '('           { LPAREN }
    | ')'           { RPAREN }
    | '='           { EQUALS }
    | ','           { COMMA }
    | '['           { LBRACKET }
    | ']'           { RBRACKET }
    | '|'           { PIPE }
    | ':'           { COLON }      // NEW: After "::"
    | eof           { EOF }
```

**Key points:**
- Type keywords after `"match"`, before `"_"` wildcard
- Type variable pattern before identifier
- COLON token after multi-char operators

### Example 2: Parser Token Declarations

```fsharp
// Source: Parser.fsy, token section (lines 15-34), with additions

%{
open Ast
open FSharp.Text.Lexing
open FSharp.Text.Parsing

let ruleSpan (parseState: IParseState) (firstSym: int) (lastSym: int) : Span =
    mkSpan (parseState.InputStartPosition firstSym) (parseState.InputEndPosition lastSym)

let symSpan (parseState: IParseState) (n: int) : Span =
    mkSpan (parseState.InputStartPosition n) (parseState.InputEndPosition n)
%}

// Token declarations
%token <int> NUMBER
%token <string> IDENT
%token <string> STRING
%token PLUS MINUS STAR SLASH
%token LPAREN RPAREN
%token LET IN EQUALS
%token TRUE FALSE IF THEN ELSE
%token LT GT LE GE NE
%token AND OR
%token FUN REC ARROW
%token COMMA UNDERSCORE
%token LBRACKET RBRACKET CONS
%token MATCH WITH PIPE

// NEW: Type annotation tokens
%token COLON
%token TYPE_INT TYPE_BOOL TYPE_STRING TYPE_LIST
%token <string> TYPE_VAR

%token EOF

// Precedence declarations (unchanged - don't use for type expressions)
%left OR
%left AND
%nonassoc EQUALS LT GT LE GE NE
%right CONS

%start start
%type <Ast.Expr> start
```

**Key points:**
- COLON is a simple token (no value)
- Type keywords are simple tokens
- TYPE_VAR carries the string value (including the apostrophe)

### Example 3: AST Node Additions

```fsharp
// Source: Ast.fs, Expr type (lines 49-87), with additions

type Expr =
    // Existing variants (abbreviated)
    | Number of int * span: Span
    | Var of string * span: Span
    | Lambda of param: string * body: Expr * span: Span
    | App of func: Expr * arg: Expr * span: Span
    // ... other variants ...

    // NEW: Annotated expression: (e : T)
    | Annot of expr: Expr * typeExpr: TypeExpr * span: Span

    // NEW: Annotated lambda: fun (x: T) -> e
    | LambdaAnnot of param: string * paramType: TypeExpr * body: Expr * span: Span

// NEW: Type expression AST
and TypeExpr =
    | TEInt
    | TEBool
    | TEString
    | TEList of TypeExpr
    | TEArrow of TypeExpr * TypeExpr      // T1 -> T2 (right-associative)
    | TETuple of TypeExpr list            // T1 * T2 * ... (n >= 2)
    | TEVar of string                     // 'a, 'b (includes apostrophe)

// Update spanOf helper
let spanOf (expr: Expr) : Span =
    match expr with
    | Number(_, s) | Var(_, s) | Lambda(_, _, s) | App(_, _, s) -> s
    // ... existing cases ...
    | Annot(_, _, s) | LambdaAnnot(_, _, _, s) -> s
```

**Key points:**
- `TypeExpr` is separate from `Expr` (different AST layers)
- `Annot` wraps an expression with a type
- `LambdaAnnot` extends `Lambda` with parameter type
- All variants carry `Span` for error messages

### Example 4: Type Expression Grammar

```fsharp
// Source: Parser.fsy, new type expression rules
// Based on OCaml type grammar (OCaml Manual 5.4, Chapter 7.4)

// Start at ArrowType (outermost type expression)
TypeExpr:
    | ArrowType             { $1 }

// Arrow is right-associative: int -> int -> int = int -> (int -> int)
ArrowType:
    | TupleType ARROW ArrowType     { TEArrow($1, $3, ruleSpan parseState 1 3) }
    | TupleType                      { $1 }

// Tuple types: int * bool * string
TupleType:
    | AtomicType STAR TupleTypeList { TETuple($1 :: $3, ruleSpan parseState 1 3) }
    | AtomicType                     { $1 }

TupleTypeList:
    | AtomicType STAR TupleTypeList { $1 :: $3 }
    | AtomicType                     { [$1] }

// Atomic types: base types, type variables, list types, parenthesized
AtomicType:
    | TYPE_INT                       { TEInt }
    | TYPE_BOOL                      { TEBool }
    | TYPE_STRING                    { TEString }
    | TYPE_VAR                       { TEVar($1) }
    | AtomicType TYPE_LIST           { TEList($1, ruleSpan parseState 1 2) }
    | LPAREN TypeExpr RPAREN         { $2 }
```

**Key points:**
- Three-level hierarchy: Arrow > Tuple > Atomic
- Right-associativity of arrow encoded structurally
- Tuple requires at least 2 elements (via STAR separator)
- List type uses postfix syntax: `int list`

### Example 5: Annotated Expression Grammar

```fsharp
// Source: Parser.fsy, existing Expr and Atom rules with additions

Expr:
    // Existing expression rules (unchanged)
    | MATCH Expr WITH MatchClauses              { Match($2, $4, ...) }
    | LET IDENT EQUALS Expr IN Expr             { Let($2, $4, $6, ...) }
    | IF Expr THEN Expr ELSE Expr               { If($2, $4, $6, ...) }
    | LET REC IDENT IDENT EQUALS Expr IN Expr   { LetRec($3, $4, $6, $8, ...) }

    // NEW: Annotated lambda
    | FUN LPAREN IDENT COLON TypeExpr RPAREN ARROW Expr
        { LambdaAnnot($3, $5, $8, ruleSpan parseState 1 8) }

    // Existing lambda (unannotated) - UNCHANGED
    | FUN IDENT ARROW Expr                      { Lambda($2, $4, ...) }

    // Existing operators (unchanged)
    | Expr OR Expr                              { Or($1, $3, ...) }
    | Expr AND Expr                             { And($1, $3, ...) }
    // ... other operators ...
    | Term                                      { $1 }

Term:
    | Term STAR Factor   { Multiply($1, $3, ...) }
    | Term SLASH Factor  { Divide($1, $3, ...) }
    | Factor             { $1 }

Factor:
    | MINUS Factor       { Negate($2, ...) }
    | AppExpr            { $1 }

AppExpr:
    | AppExpr Atom       { App($1, $2, ...) }
    | Atom               { $1 }

Atom:
    | NUMBER             { Number($1, symSpan parseState 1) }
    | IDENT              { Var($1, symSpan parseState 1) }
    | TRUE               { Bool(true, symSpan parseState 1) }
    | FALSE              { Bool(false, symSpan parseState 1) }
    | STRING             { String($1, symSpan parseState 1) }

    // Existing parenthesized expression - UNCHANGED
    | LPAREN Expr RPAREN                        { $2 }

    // NEW: Annotated expression (requires COLON to disambiguate)
    | LPAREN Expr COLON TypeExpr RPAREN         { Annot($2, $4, ruleSpan parseState 1 5) }

    // Existing tuple/list - UNCHANGED
    | LPAREN Expr COMMA ExprList RPAREN         { Tuple($2 :: $4, ...) }
    | LBRACKET RBRACKET                          { EmptyList(...) }
    | LBRACKET Expr RBRACKET                    { List([$2], ...) }
    | LBRACKET Expr COMMA ExprList RBRACKET     { List($2 :: $4, ...) }
```

**Key points:**
- Annotated lambda (`fun (x: T) -> e`) added as separate rule
- Unannotated lambda (`fun x -> e`) unchanged
- Annotated expression (`(e : T)`) disambiguated by COLON
- Plain parenthesized expression (`(e)`) unchanged
- No modifications to existing operator rules

### Example 6: Curried Multi-Parameter Annotations

```fsharp
// Source: Parser.fsy, curried parameter handling
// Pattern: fun (x: int) (y: bool) -> e
// Desugars to: fun (x: int) -> (fun (y: bool) -> e)

// Option 1: Iterative approach (extend base lambda rule)
Expr:
    // Base case: single annotated parameter
    | FUN AnnotParamList ARROW Expr
        { desugarAnnotParams $2 $4 (ruleSpan parseState 1 4) }

AnnotParamList:
    | AnnotParam                        { [$1] }
    | AnnotParam AnnotParamList         { $1 :: $2 }

AnnotParam:
    | LPAREN IDENT COLON TypeExpr RPAREN
        { ($2, $4) }  // returns (param_name, param_type)

// Desugaring helper (to add in Ast.fs or Parser.fsy preamble):
// let rec desugarAnnotParams params body span =
//     match params with
//     | [] -> failwith "empty param list"
//     | [(name, ty)] -> LambdaAnnot(name, ty, body, span)
//     | (name, ty) :: rest -> LambdaAnnot(name, ty, desugarAnnotParams rest body span, span)
```

**Key points:**
- `AnnotParamList` captures one or more `(x: T)` parameters
- Desugaring function converts to nested `LambdaAnnot` nodes
- Maintains consistency with existing curried function semantics
- Right-associative nesting: `fun (x: A) (y: B) -> e` = `fun (x: A) -> (fun (y: B) -> e)`

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Algorithm W (pure inference) | Bidirectional typing with annotations | 2020+ in modern type checkers | Enables explicit type annotations, better error messages, supports advanced types (higher-rank, GADTs) |
| Hand-written lexer/parser | Parser generators (fslex/fsyacc) | Established since 1970s (lex/yacc), F# port ~2010 | Automatic token generation, position tracking, reduced manual error handling |
| Runtime keyword maps | Lexer pattern ordering | Long-standing best practice | Simpler for small keyword sets, no runtime overhead |
| Global precedence for all operators | Separate non-terminal hierarchies | Established yacc practice | Avoids precedence conflicts between different operator classes |

**Deprecated/outdated:**
- **FsYacc conflict resolution with `%prec`**: While available, better to use grammar refactoring (separate non-terminals) for clearer intent and easier maintenance
- **Mixing value and type operator precedence**: Modern language implementations separate type syntax grammar from expression grammar to avoid cross-contamination

## Open Questions

Things that couldn't be fully resolved:

1. **Should `list` be a postfix type constructor (`int list`) or prefix (`list int`)?**
   - What we know: ML languages use postfix (`int list`), Haskell uses prefix (`[Int]`)
   - What's unclear: Current FunLang syntax uses `[1, 2, 3]` for list literals, no type syntax yet
   - Recommendation: Use ML-style postfix (`int list`) for consistency with ML heritage and syntax like `int option`, `int ref` in F#. This requires `AtomicType TYPE_LIST` pattern in grammar.

2. **How to handle type annotation in `let rec` bindings?**
   - What we know: Need to support `let rec f (x: int) : int = ...` eventually
   - What's unclear: Should this be Phase 1 or deferred to later phases?
   - Recommendation: Defer to Phase 2 or later. Phase 1 focuses on lambda annotations. Let-binding annotations can follow same pattern but need careful interaction with type inference/checking logic.

3. **Should curried parameters allow mixing annotated and unannotated?**
   - What we know: `fun x (y: int) -> e` is legal in OCaml (partial annotation)
   - What's unclear: Does this add complexity to Phase 1 parser, or defer to type checking phase?
   - Recommendation: Phase 1 can support it by having both `IDENT` and `AnnotParam` in parameter lists, but type checking logic (Phase 3-4) will need to handle mixed cases. For simplicity, Phase 1 could restrict to "all annotated" or "all unannotated" per lambda, document as future extension.

## Sources

### Primary (HIGH confidence)
- [FsLexYacc GitHub Repository](https://github.com/fsprojects/FsLexYacc) - Official FsLexYacc documentation and examples
- [FsLexYacc Official Docs](https://fsprojects.github.io/FsLexYacc/) - FsLex and FsYacc reference
- [OCaml Manual 5.4: Type Expressions](https://ocaml.org/manual/5.4/types.html) - Authoritative type expression grammar
- [F# Programming Wikibooks: Lexing and Parsing](https://en.wikibooks.org/wiki/F_Sharp_Programming/Lexing_and_Parsing) - Comprehensive F# parser tutorial
- Existing codebase files: `Lexer.fsl`, `Parser.fsy`, `Ast.fs` - Current implementation patterns

### Secondary (MEDIUM confidence)
- [CS3110 OCaml Textbook: Functions](https://cs3110.github.io/textbook/chapters/basics/functions.html) - Type annotation syntax examples
- [Real World OCaml: Variables and Functions](https://dev.realworldocaml.org/variables-and-functions.html) - Type annotation patterns in practice
- [FsLexYacc JSON Parser Example](https://github.com/fsprojects/FsLexYacc/blob/master/docs/content/jsonParserExample.md) - Practical parser example
- [Using FSLexYacc Blog Post](https://thanos.codes/blog/using-fslexyacc-the-fsharp-lexer-and-parser/) - Tutorial on FsLexYacc usage

### Tertiary (LOW confidence)
- [ML Dialects Comparison](https://hyperpolyglot.org/ml) - Cross-language syntax comparison (useful but not authoritative for FunLang decisions)
- Web search results on parser conflict resolution - General principles, verify against FsYacc documentation

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Project already uses FsLexYacc successfully through Phase 1-7
- Architecture patterns: HIGH - Based on existing working codebase and official OCaml grammar
- Pitfalls: HIGH - Derived from existing codebase patterns and common lexer/parser errors
- Type expression grammar: MEDIUM-HIGH - Based on OCaml standard but needs adaptation to FsYacc syntax
- Currying support: MEDIUM - Pattern is clear but desugaring implementation details need verification

**Research date:** 2026-02-03
**Valid until:** 60 days (stable domain - lexer/parser tools change slowly)

**Next phase dependencies:**
- Phase 2 (Elaboration) will need the `TypeExpr` AST nodes from this phase
- Phase 3 (Bidirectional) will consume elaborated types for synthesis/checking
- Phase 4 (Annotation checking) will validate `Annot` and `LambdaAnnot` nodes

**Test coverage requirements for Phase 1:**
- All 98 existing fslit tests must pass (backward compatibility)
- All 362 existing Expecto tests must pass (backward compatibility)
- New lexer tests: TYPE_INT, TYPE_BOOL, TYPE_STRING, TYPE_LIST, TYPE_VAR, COLON tokens
- New parser tests: TypeExpr parsing (atoms, arrows, tuples, lists), Annot, LambdaAnnot, curried params
- Emit tests: `--emit-tokens` and `--emit-ast` should show new tokens and AST nodes
