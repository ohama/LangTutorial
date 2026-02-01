# Phase 2: Lists (리스트) - Research

**Researched:** 2026-02-01
**Domain:** List implementation in functional language interpreters with cons-based structure
**Confidence:** HIGH

## Summary

List support in functional languages requires three core components: cons operator (::), empty list literal ([]), and syntactic sugar for list literals ([1, 2, 3]). The research focused on standard patterns for implementing these in F# interpreters using FsLexYacc.

Lists are canonically represented as singly-linked cons cells, where `[1, 2, 3]` desugars to `1 :: 2 :: 3 :: []`. The cons operator is right-associative and has specific precedence requirements. The primary implementation challenge is handling both bracket notation and cons operator syntax while avoiding parser conflicts.

F#'s own list implementation provides the reference model: lists use `::` (cons), `[]` (empty), and `[e1; e2; e3]` (literal with semicolons). However, FunLang currently uses commas for tuples, so the choice is between `[1, 2, 3]` (comma) vs `[1; 2; 3]` (semicolon). Given existing tuple syntax uses commas, consistency favors comma-separated list literals.

**Primary recommendation:** Implement cons operator (::) with %right precedence, empty list literal ([]), list literal syntactic sugar ([1, 2, 3]), and ListValue type. Follow ML-family cons/nil pattern with right-associative cons operator.

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| FsLexYacc | 11.3.0 | Parser generator | Already in project, handles grammar |
| F# list type | .NET 10 | Reference implementation | Built-in cons operator and pattern matching |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| N/A | - | No additional libraries | Lists are built-in AST types |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Cons operator :: | Array indexing | :: is functional standard, arrays are imperative |
| Right-associative :: | Left-associative | Right-association matches ML/F#/OCaml/Haskell |
| Comma separator [1, 2, 3] | Semicolon [1; 2; 3] | Comma matches existing tuple syntax, semicolon matches F# |

**Installation:**
No additional packages needed. Lists are AST-level constructs.

## Architecture Patterns

### Recommended AST Structure
```fsharp
// Ast.fs
type Expr =
    // ... existing cases ...
    | List of Expr list              // List literal: [e1, e2, e3] (syntactic sugar)
    | EmptyList                      // Empty list: []
    | Cons of Expr * Expr            // Cons operator: head :: tail

and Value =
    // ... existing cases ...
    | ListValue of Value list        // Runtime list representation
```

### Pattern 1: Cons Operator as Right-Associative Infix

**What:** The cons operator (::) prepends an element to a list, associating right-to-left.

**When to use:** Always for list construction in functional languages.

**Example:**
```fsharp
// Source: https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/lists
// 1 :: 2 :: 3 :: [] parses as 1 :: (2 :: (3 :: []))
// NOT as ((1 :: 2) :: 3) :: []

// In Parser.fsy:
%right CONS    // Right-associative cons operator

// Grammar rule:
Expr:
    | Expr CONS Expr    { Cons($1, $3) }
```

### Pattern 2: List Literal as Syntactic Sugar

**What:** `[1, 2, 3]` desugars to `1 :: 2 :: 3 :: []` during parsing.

**When to use:** To provide convenient list syntax while keeping AST simple.

**Example:**
```fsharp
// Source: https://cs3110.github.io/textbook/chapters/data/lists.html
// Any list [e1; e2; ...; en] could instead be written with the more primitive
// nil and cons syntax: e1 :: e2 :: ... :: en :: []

// Two approaches:

// APPROACH A: Desugar in parser (recommended)
Atom:
    | LBRACKET RBRACKET                  { EmptyList }
    | LBRACKET ExprList RBRACKET         { desugarList $2 }  // Desugar to nested Cons

// APPROACH B: Keep as separate AST node, desugar in evaluator
Atom:
    | LBRACKET RBRACKET                  { EmptyList }
    | LBRACKET ExprList RBRACKET         { List($2) }  // Keep as List node

// Eval.fs approach B:
| List exprs ->
    let values = List.map (eval env) exprs
    ListValue values
```

**Recommendation:** Use Approach B (separate List AST node). Simpler parser, clearer AST, easier to implement. Desugar during evaluation, not parsing.

### Pattern 3: List Pattern Matching (Phase 3)

**What:** Destructure lists in pattern matching: `[] | h :: t`

**When to use:** Phase 3 Pattern Matching (not Phase 2).

**Example:**
```fsharp
// Source: https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/lists
// Phase 3 will add these patterns:
and Pattern =
    | VarPat of string
    | TuplePat of Pattern list
    | WildcardPat
    | EmptyListPat                    // Phase 3: [] pattern
    | ConsPat of Pattern * Pattern    // Phase 3: h :: t pattern
```

**Note:** Phase 2 focuses on list *construction* only. Pattern matching is Phase 3.

### Pattern 4: Bracket Token Handling

**What:** Square brackets for lists require LBRACKET and RBRACKET tokens.

**When to use:** Any language with list literals.

**Example:**
```fsharp
// Lexer.fsl
| '['           { LBRACKET }
| ']'           { RBRACKET }

// Parser.fsy
%token LBRACKET RBRACKET
%token CONS    // or COLONCOLON, DOUBLECOLON - :: token

// Grammar:
Atom:
    | LBRACKET RBRACKET                      { EmptyList }
    | LBRACKET Expr COMMA ExprList RBRACKET  { List($2 :: $4) }
```

### Pattern 5: Cons vs Comma Separator

**What:** OCaml/F# use semicolon in list literals `[1; 2; 3]`, but FunLang already uses comma for tuples.

**Decision:** Use comma for lists to match tuple syntax: `[1, 2, 3]`.

**Why:**
- FunLang tuples use `(1, 2, 3)` with commas
- Consistency: both compound structures use commas
- Fewer separator tokens to learn
- Different from F# but internally consistent

**Example:**
```fsharp
// FunLang (consistent comma):
(1, 2, 3)    // tuple
[1, 2, 3]    // list

// F# (different separators):
(1, 2, 3)    // tuple
[1; 2; 3]    // list
```

### Anti-Patterns to Avoid

- **Cons as left-associative:** Breaks ML-family convention, makes `1 :: 2 :: []` parse as `((1 :: 2)) :: []` instead of `1 :: (2 :: [])`.
- **1-element list as tuple:** Using `[1, 2, 3]` syntax but `[x]` creates tuple. OCaml pitfall: `[1, 2]` creates `[(1, 2)]` if commas are tuple operators.
- **Using %left for cons in FsYacc:** FsYacc has bugs with %left/%right. Use grammar stratification if possible, or test %right carefully.
- **Hand-rolling list value equality:** F#'s structural equality handles `ListValue of Value list` correctly. Don't write custom equality.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| List equality | Custom recursive equality function | F# structural equality (=) | F# discriminated unions have built-in structural equality |
| List reversal for parsing | Manual reverse loop | List.rev | Built-in, optimized, tail-recursive |
| ExprList accumulation | Complex parser rules | Left-recursive list accumulation | Standard parser pattern |

**Key insight:** F#'s type system handles list equality automatically. `ListValue [IntValue 1; IntValue 2] = ListValue [IntValue 1; IntValue 2]` works out of the box because discriminated unions use structural equality.

## Common Pitfalls

### Pitfall 1: Comma Creates Tuple Inside Brackets

**What goes wrong:** If comma is already the tuple operator, `[1, 2, 3]` might parse as a list containing one tuple `[(1, 2, 3)]`.

**Why it happens:** Operator precedence. If `COMMA` is an expression-level operator, `Expr COMMA Expr` produces tuples everywhere.

**How to avoid:**
- Use separate grammar rules for `ExprList` inside brackets
- OR ensure tuple grammar requires parentheses: `LPAREN Expr COMMA ExprList RPAREN`
- FunLang already requires parens for tuples, so this is safe

**Warning signs:** Test case `[1, 2]` evaluates to `[(1, 2)]` instead of `[1; 2]`.

**Example from existing code:**
```fsharp
// Parser.fsy (current):
// Tuples REQUIRE parentheses:
Atom:
    | LPAREN Expr COMMA ExprList RPAREN  { Tuple($2 :: $4) }

// So COMMA outside parens is free to use in lists:
Atom:
    | LBRACKET Expr COMMA ExprList RBRACKET  { List($2 :: $4) }
```

### Pitfall 2: Cons Operator Precedence

**What goes wrong:** `1 :: 2 :: []` parses as `(1 :: 2) :: []` (left-associative) instead of `1 :: (2 :: [])` (right-associative).

**Why it happens:** Default left-associativity in LALR parsers, or wrong %left/%right declaration.

**How to avoid:**
- Declare `%right CONS` for right-associativity
- Test that `1 :: 2 :: 3 :: []` works correctly
- FsYacc has bugs with %left/%right, so verify behavior

**Warning signs:** Error "cons requires list as second argument but got int" when using `1 :: 2 :: []`.

### Pitfall 3: Empty List Type

**What goes wrong:** Empty list `[]` has no type information, causing type errors in heterogeneous contexts.

**Why it happens:** Type systems need to know list element type, but `[]` has no elements.

**How to avoid:**
- FunLang is dynamically typed (no static types), so this isn't an issue
- Runtime values are `ListValue of Value list`, which can be empty
- No type annotations needed

**Warning signs:** Not applicable to FunLang (dynamically typed).

### Pitfall 4: Parser Conflicts with Brackets

**What goes wrong:** Shift/reduce conflicts between `[` for array indexing vs list literals.

**Why it happens:** If the language has array indexing `arr[0]`, brackets become ambiguous.

**How to avoid:**
- FunLang has no array indexing (yet)
- Brackets are unambiguous: only used for lists
- Check `dotnet build` output for shift/reduce conflicts

**Warning signs:** Parser reports shift/reduce conflicts involving LBRACKET.

### Pitfall 5: Cons Pattern in Let Bindings (Phase 3)

**What goes wrong:** Trying to implement `let h :: t = list` in Phase 2.

**Why it happens:** Cons patterns are part of pattern matching (Phase 3), not list construction (Phase 2).

**How to avoid:**
- Phase 2: Only implement list construction (List, EmptyList, Cons expressions)
- Phase 3: Add list patterns (EmptyListPat, ConsPat)
- Keep concerns separated

**Warning signs:** Trying to modify Pattern type in Phase 2.

## Code Examples

### List Literal Parsing

```fsharp
// Parser.fsy
// Source: Adapted from FsLexYacc JSON parser example
// https://github.com/fsprojects/FsLexYacc/blob/master/docs/content/jsonParserExample.md

Atom:
    | LBRACKET RBRACKET                      { EmptyList }
    | LBRACKET Expr COMMA ExprList RBRACKET  { List($2 :: $4) }

ExprList:
    | Expr                        { [$1] }
    | Expr COMMA ExprList         { $1 :: $3 }
```

### Cons Operator Parsing

```fsharp
// Parser.fsy
%token CONS
%right CONS    // Right-associative

// Add to Expr rule (NOT Factor/Atom - lower precedence than arithmetic):
Expr:
    // ... existing rules ...
    | Expr CONS Expr              { Cons($1, $3) }
```

### List Value Evaluation

```fsharp
// Eval.fs
// Source: Pattern from existing Tuple evaluation

| EmptyList ->
    ListValue []

| List exprs ->
    let values = List.map (eval env) exprs
    ListValue values

| Cons (headExpr, tailExpr) ->
    let headVal = eval env headExpr
    match eval env tailExpr with
    | ListValue tailVals -> ListValue (headVal :: tailVals)
    | _ -> failwith "Type error: cons (::) requires list as second argument"
```

### List Value Formatting

```fsharp
// Eval.fs formatValue function
// Source: Pattern from existing TupleValue formatting

let rec formatValue (v: Value) : string =
    match v with
    | IntValue n -> string n
    | BoolValue b -> if b then "true" else "false"
    | FunctionValue _ -> "<function>"
    | StringValue s -> sprintf "\"%s\"" s
    | TupleValue values ->
        let formattedElements = List.map formatValue values
        sprintf "(%s)" (String.concat ", " formattedElements)
    | ListValue values ->
        let formattedElements = List.map formatValue values
        sprintf "[%s]" (String.concat ", " formattedElements)
```

### List Equality in Eval

```fsharp
// Eval.fs Equal/NotEqual operators
// Source: Pattern from existing TupleValue equality

| Equal (left, right) ->
    match eval env left, eval env right with
    | IntValue l, IntValue r -> BoolValue (l = r)
    | BoolValue l, BoolValue r -> BoolValue (l = r)
    | StringValue l, StringValue r -> BoolValue (l = r)
    | TupleValue l, TupleValue r -> BoolValue (l = r)  // Existing
    | ListValue l, ListValue r -> BoolValue (l = r)    // Add this
    | _ -> failwith "Type error: = requires operands of same type"
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Separate nil constructor | Empty list as discriminated union case | F# 2.0 | Simpler pattern matching |
| Custom list type | Built-in list<'T> type | F# 1.0 | Idiomatic F# |
| %left/%right precedence | Grammar stratification | FsYacc bugs | Avoids parser bugs |

**Deprecated/outdated:**
- **FsYacc %left/%right declarations:** Known bugs (Issues #39, #40). Use grammar stratification or test thoroughly.

## Open Questions

1. **Should cons operator have higher or lower precedence than arithmetic?**
   - What we know: F# doesn't allow mixing cons and arithmetic without parens
   - What's unclear: Best precedence level for FunLang
   - Recommendation: Lower than arithmetic (Expr level), same as comparisons. This prevents `1 + 2 :: xs` ambiguity. Require parens: `(1 + 2) :: xs`.

2. **Should [1, 2, 3] desugar in parser or evaluator?**
   - What we know: Both approaches work
   - What's unclear: Which is cleaner for FunLang
   - Recommendation: Keep List as AST node, desugar in evaluator. Simpler parser, clearer AST structure, matches Tuple pattern.

3. **Nested list type checking?**
   - What we know: FunLang is dynamically typed
   - What's unclear: Should `[1, [2, 3]]` be allowed?
   - Recommendation: Allow it. Runtime error only if operations assume homogeneous types. Success criterion 4 requires nested lists work.

## Sources

### Primary (HIGH confidence)
- [F# Lists - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/lists) - F# list syntax and semantics
- [OCaml Lists](https://cs3110.github.io/textbook/chapters/data/lists.html) - Cons operator right-associativity, desugaring
- [FsLexYacc JSON Parser Example](https://github.com/fsprojects/FsLexYacc/blob/master/docs/content/jsonParserExample.md) - List accumulation pattern in parser

### Secondary (MEDIUM confidence)
- [F# Lists - C# Corner](https://www.c-sharpcorner.com/article/f-sharps-list/) - List implementation details
- [OCaml Lists Documentation](https://ocaml.org/docs/lists) - Semicolon vs comma separator pitfall
- [Operator Associativity - Wikipedia](https://en.wikipedia.org/wiki/Operator_associativity) - Right-associativity theory

### Tertiary (LOW confidence)
- [WebSearch] Functional programming language lists (general patterns, no specific implementation)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - FsLexYacc already in project, F# lists are reference
- Architecture: HIGH - Cons/nil pattern is ML-family standard, AST design follows existing Tuple pattern
- Pitfalls: MEDIUM - Some pitfalls are FunLang-specific (comma separator), others are general (cons associativity)

**Research date:** 2026-02-01
**Valid until:** 2026-03-01 (30 days - stable domain, F# and FsLexYacc rarely change)
