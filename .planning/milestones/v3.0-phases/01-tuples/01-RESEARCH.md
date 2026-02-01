# Phase 1: Tuples - Research

**Researched:** 2026-02-01
**Domain:** Tuple implementation in functional language interpreters
**Confidence:** HIGH

## Summary

Tuples are fixed-size heterogeneous data collections found in all ML-family languages (OCaml, SML, F#). In interpreter implementation, tuples require three major components: (1) AST representation using expression lists, (2) parser grammar with comma-separated expressions in parentheses, and (3) pattern matching evaluation that destructures tuples and binds variables in the environment. The standard approach represents tuples as `Tuple of Expr list` in the AST and `TupleValue of Value list` at runtime, with pattern matching handled through recursive destructuring that validates arity and binds each element.

The critical challenge is grammar design: comma has low precedence (lower than all operators) and parentheses are already used for grouping, creating potential ambiguity between `(expr)` (grouped expression) and `(expr,)` (1-tuple). The established solution from ML-family languages is to require at least 2 elements for tuples, making `(1)` a grouped expression and `(1, 2)` a tuple. This avoids the trailing comma requirement found in Python.

The implementation follows the existing FunLang architecture: extend Ast.fs with Tuple/TupleValue constructors, add COMMA token to lexer, extend parser grammar with tuple expression rule at appropriate precedence level, and extend evaluator with tuple evaluation and pattern matching logic. Testing should cover nested tuples `((1, 2), 3)`, heterogeneous types `(1, true, "hello")`, and pattern matching in let bindings `let (x, y) = pair`.

**Primary recommendation:** Follow OCaml/SML tuple semantics: minimum 2 elements, comma-separated in parentheses, structural equality, pattern matching with arity checking. Extend existing Value type with `TupleValue of Value list` and Expr with `Tuple of Expr list`. Add pattern matching AST node `LetPat of Pattern * Expr * Expr` where Pattern is a new discriminated union supporting variable, tuple, and wildcard patterns.

## Standard Stack

The established libraries/tools for tuple implementation in FunLang:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| FsLexYacc | 11.3.0 | Parser generator | Already in use, handles comma-separated lists with %left declarations |
| F# List | .NET 10 | Tuple element storage | Built-in, immutable, matches AST pattern (Expr list) |
| F# Map | .NET 10 | Environment bindings | Already used for variables, extends naturally to pattern bindings |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Expecto | 10.x | Unit tests | Test tuple evaluation, pattern matching, arity errors |
| fslit | current | Integration tests | E2E validation of tuple syntax and output formatting |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| `TupleValue of Value list` | `TupleValue of Value * Value * Value list` | Latter enforces arity >= 2 at type level but complicates code |
| New Pattern AST | Extend Expr with patterns | Cleaner separation (patterns vs expressions) vs fewer AST types |
| Comma as operator | Tuple constructor function | Functional purity vs complexity (comma is not an operator in ML) |

**Installation:**
```bash
# No new packages needed - extends existing FsLexYacc implementation
# Already installed: FsLexYacc 11.3.0, F# .NET 10
```

## Architecture Patterns

### Recommended Project Structure
```
FunLang/
├── Ast.fs              # Add: Tuple expr, TupleValue, Pattern DU
├── Parser.fsy          # Add: COMMA token, tuple expression grammar
├── Lexer.fsl          # Add: ',' -> COMMA token
├── Eval.fs            # Add: tuple eval, pattern matching eval
└── Format.fs          # Add: tuple formatting "(1, 2, 3)"
```

### Pattern 1: AST Representation

**What:** Discriminated union cases for tuple expressions and values

**When to use:** Extending Ast.fs for tuple support

**Example:**
```fsharp
// Source: OCaml/SML interpreter pattern
// In Ast.fs
type Expr =
    // ... existing cases ...
    | Tuple of Expr list                    // (e1, e2, ..., en)

type Pattern =
    | VarPat of string                      // x
    | TuplePat of Pattern list              // (p1, p2, ..., pn)
    | WildcardPat                           // _ (for TUP-02 completeness)

type Expr =
    // ... existing cases ...
    | LetPat of Pattern * Expr * Expr       // let pat = expr in body

type Value =
    // ... existing cases ...
    | TupleValue of Value list              // Runtime tuple value
```

**Key design choice:** `Expr list` (not `Expr * Expr * Expr list`) allows uniform handling of n-ary tuples. Runtime arity checking (minimum 2 elements) happens in parser or evaluator, not type system.

### Pattern 2: Parser Grammar for Tuples

**What:** Grammar rule for comma-separated expressions with correct precedence

**When to use:** Extending Parser.fsy for tuple syntax

**Example:**
```fsharp
// Source: FsYacc comma-separated list pattern
// In Parser.fsy

// Token declaration
%token COMMA

// Precedence: comma is lower than all binary operators
// (does NOT use %left COMMA - handled in grammar structure)

// Grammar rules
Expr:
    // ... existing expression rules ...
    | TupleExpr              { $1 }

TupleExpr:
    | LPAREN TupleElements RPAREN  { Tuple($2) }
    | Atom                         { $1 }  // fallthrough to lower precedence

TupleElements:
    | Expr COMMA Expr                      { [$1; $3] }
    | Expr COMMA TupleElements             { $1 :: $3 }

Atom:
    // ... existing atoms (NUMBER, IDENT, etc.) ...
    | LPAREN Expr RPAREN     { $2 }  // Grouped expression (higher precedence)
```

**Anti-pattern to avoid:** Do NOT make comma a binary operator with `%left COMMA`. This creates precedence conflicts with function application and let bindings. Instead, handle commas in dedicated grammar rules.

**Critical insight:** `LPAREN Expr RPAREN` (grouped expression) is in Atom, while `LPAREN TupleElements RPAREN` (tuple) is in TupleExpr. Parser uses lookahead to distinguish: after `Expr COMMA`, it must be a tuple.

### Pattern 3: Tuple Pattern Matching Evaluation

**What:** Recursive pattern matching that validates arity and binds variables

**When to use:** Extending Eval.fs for pattern destructuring

**Example:**
```fsharp
// Source: ML pattern matching semantics
// In Eval.fs

// Evaluate tuple expression
| Tuple exprs ->
    let values = List.map (eval env) exprs
    TupleValue values

// Pattern matching against a value
let rec matchPattern (pat: Pattern) (value: Value) : (string * Value) list option =
    match pat, value with
    | VarPat name, v -> Some [(name, v)]
    | WildcardPat, _ -> Some []
    | TuplePat pats, TupleValue vals ->
        if List.length pats <> List.length vals then
            None  // Arity mismatch
        else
            // Recursively match each element
            let bindings = List.map2 matchPattern pats vals
            if List.forall Option.isSome bindings then
                Some (List.collect Option.get bindings)
            else
                None
    | _ -> None  // Type mismatch

// Evaluate let-pattern binding
| LetPat (pat, bindingExpr, bodyExpr) ->
    let value = eval env bindingExpr
    match matchPattern pat value with
    | Some bindings ->
        let extendedEnv = List.fold (fun e (n, v) -> Map.add n v e) env bindings
        eval extendedEnv bodyExpr
    | None ->
        failwith "Pattern match failed: arity or type mismatch"
```

**Arity validation:** Pattern matching checks `List.length pats = List.length vals` and fails with runtime error on mismatch. This is standard ML behavior.

### Pattern 4: Tuple Formatting

**What:** User-friendly output representation `(1, 2, 3)`

**When to use:** Extending Format.fs and Eval.formatValue

**Example:**
```fsharp
// Source: Standard ML/OCaml REPL output format
// In Eval.fs formatValue function

| TupleValue values ->
    let formattedElements = List.map formatValue values
    sprintf "(%s)" (String.concat ", " formattedElements)

// Example outputs:
// (1, 2) -> "(1, 2)"
// (true, 42, "hello") -> "(true, 42, \"hello\")"
// ((1, 2), 3) -> "((1, 2), 3)"
```

### Anti-Patterns to Avoid

- **Comma as binary operator:** Treating comma like `+` or `*` creates precedence nightmares. Commas are delimiters, not operators.
- **Allowing 1-tuples or 0-tuples:** Creates ambiguity with grouped expressions `(x)` and empty parens `()`. Standard ML avoids this; Python requires trailing comma `(x,)` which is ugly.
- **Pattern matching without arity checking:** Runtime must validate `let (x, y) = (1, 2, 3)` fails with clear error message.
- **Mutable tuple implementation:** Tuples are immutable values in functional languages. No `tuple[0] = x` assignment.

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Comma-separated list parsing | Custom recursive parsing | FsYacc grammar with left recursion | Parser handles shift/reduce conflicts, generates efficient code |
| Pattern matching algorithm | Ad-hoc if/else chains | Recursive pattern match function | Handles nested patterns, arity checking, extensible to lists/etc |
| Tuple equality | Custom comparison | F# structural equality `=` | Built-in, handles nested tuples, matches ML semantics |
| Formatting nested structures | String concatenation loops | List.map + String.concat | Compositional, handles arbitrary nesting |

**Key insight:** FsYacc already handles comma-separated lists perfectly with left-recursive grammar rules. The challenge is NOT parsing commas, it's disambiguating `(expr)` from `(expr, expr)` through grammar structure.

## Common Pitfalls

### Pitfall 1: Shift/Reduce Conflict with Parentheses

**What goes wrong:** Parser can't decide whether `(expr` starts a grouped expression or a tuple

**Why it happens:** Both rules begin with `LPAREN Expr`, parser needs lookahead to see COMMA

**How to avoid:** Use separate grammar rules for Atom (grouped) and TupleExpr (tuple). Place tuple rule at higher level than Atom to allow parser lookahead to distinguish them.

**Warning signs:** FsYacc reports "shift/reduce conflict" with LPAREN token. Parser may incorrectly parse `(1, 2)` as application of `1` to `2`.

**Resolution:**
```fsharp
// GOOD: Separate rules with clear lookahead
TupleExpr:
    | LPAREN TupleElements RPAREN  { Tuple($2) }
    | Atom                         { $1 }

TupleElements:
    | Expr COMMA Expr              { [$1; $3] }

Atom:
    | LPAREN Expr RPAREN           { $2 }

// BAD: Ambiguous rules
Atom:
    | LPAREN Expr RPAREN           { $2 }
    | LPAREN ExprList RPAREN       { Tuple($2) }  // Conflict!
```

### Pitfall 2: Comma Precedence Confusion

**What goes wrong:** `let x = 1, 2` might parse as `let x = (1, 2)` or `let x = 1` followed by syntax error

**Why it happens:** Unclear whether comma is part of the let-binding or a tuple constructor

**How to avoid:** Require parentheses for tuple literals when ambiguous. In `let pat = expr`, if `expr` is a tuple, require `let x = (1, 2)` not `let x = 1, 2`.

**Warning signs:** Confusing syntax errors involving commas. Test cases with `let` and tuples fail to parse.

**Resolution:** Grammar should parse `let x = 1, 2` as a syntax error or enforce parentheses. Most ML languages require `let x = (1, 2)` for clarity. Only in pattern position (left of `=`) are bare commas allowed: `let (x, y) = pair`.

### Pitfall 3: Pattern Arity Mismatch Error Quality

**What goes wrong:** Runtime error `"pattern match failed"` without indicating expected vs actual arity

**Why it happens:** Simple boolean check without diagnostic information

**How to avoid:** Provide informative error messages: `"Tuple pattern expects 2 elements but value has 3"`

**Warning signs:** Users confused by cryptic pattern match failures

**Resolution:**
```fsharp
// GOOD: Informative error
| TuplePat pats, TupleValue vals ->
    if List.length pats <> List.length vals then
        failwithf "Tuple pattern expects %d elements but value has %d"
                  (List.length pats) (List.length vals)

// BAD: Cryptic error
| TuplePat pats, TupleValue vals ->
    if List.length pats <> List.length vals then
        failwith "Pattern match failed"
```

### Pitfall 4: Nested Tuple Formatting

**What goes wrong:** Nested tuples print as `(1, (2, 3))` instead of correct `((1, 2), 3)` or vice versa

**Why it happens:** Incorrect parenthesization in recursive formatting

**How to avoid:** Always wrap tuple values in parentheses, regardless of context. Nested tuples inherit correct formatting through recursion.

**Warning signs:** Success criterion #3 fails (nested tuples don't print correctly)

**Resolution:**
```fsharp
// GOOD: Always parenthesize tuples
| TupleValue values ->
    sprintf "(%s)" (values |> List.map formatValue |> String.concat ", ")
// Produces: ((1, 2), 3) for Tuple([Tuple([1; 2]); 3])

// BAD: Conditional parentheses
| TupleValue values ->
    if isNested then sprintf "(%s)" ... else sprintf "%s" ...
```

### Pitfall 5: Forgetting Existing Grammar Conflicts

**What goes wrong:** Adding tuple grammar breaks existing let/if/lambda parsing

**Why it happens:** Tuple rules interact with existing expression-level grammar

**How to avoid:** Run full test suite (fslit + Expecto) after parser changes. Verify existing phases still work.

**Warning signs:** Previously passing tests fail after adding tuple support

**Resolution:** Incremental testing approach - add tuple grammar, verify old tests pass, add new tuple tests. If old tests break, refine grammar to avoid interactions.

## Code Examples

Verified patterns from FunLang codebase and ML languages:

### Tuple Literal Syntax
```fsharp
// Source: Success criteria from ROADMAP.md
(1, 2)                    // 2-tuple of integers
(1, true, "hello")        // 3-tuple of mixed types
((1, 2), 3)              // Nested tuple: 2-tuple of (2-tuple, int)
```

### Pattern Matching in Let Bindings
```fsharp
// Source: TUP-02 requirement
let (x, y) = (1, 2) in x + y        // Result: 3
let ((a, b), c) = ((1, 2), 3) in a + b + c  // Result: 6

// With wildcards (TUP-02 implies)
let (x, _) = (1, 2) in x            // Ignore second element: 1
```

### Heterogeneous Tuples
```fsharp
// Source: Success criterion #4
let t = (1, true, "hello") in t     // Result: (1, true, "hello")

// Mixed with existing features
let (n, b, s) = (42, false, "test") in
    if b then n else 0              // Result: 0
```

### Nested Evaluation
```fsharp
// Source: TUP-03 nested tuple requirement
let pair = (1, 2) in
let triple = (pair, 3) in
    triple                          // Result: ((1, 2), 3)

// Nested destructuring
let ((x, y), z) = ((5, 10), 15) in x * y * z  // Result: 750
```

### Tuple Construction from Expressions
```fsharp
// Tuples can contain arbitrary expressions
(1 + 2, 3 * 4)                     // Result: (3, 12)

let x = 5 in
let y = 10 in
    (x, y, x + y)                  // Result: (5, 10, 15)

// With function calls (Phase 5 integration)
let id = fun x -> x in
    (id 1, id true)                // Result: (1, true)
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Homogeneous tuples (Python typing) | Heterogeneous tuples (ML family) | ML tradition (1970s) | Tuples can mix types: `(1, "hello")` |
| Trailing comma for 1-tuples (Python) | Minimum 2 elements (ML) | ML tradition | No `(x,)` syntax, cleaner |
| Comma as operator | Comma as delimiter | Always in ML | No precedence issues, cleaner grammar |
| Runtime-only checking | Static types (production ML) | N/A - out of scope | FunLang has runtime checking only (no static types yet) |

**Deprecated/outdated:**
- **0-tuples/unit type:** FunLang v3.0 doesn't include unit type `()`. Wait for later phase if needed.
- **1-tuples:** Not standard in ML. Avoid confusion with grouped expressions.
- **Mutable tuples:** Never existed in ML; Python tuples are immutable too.

**Current best practice (2026):** Follow OCaml/SML semantics exactly - tuples are immutable, heterogeneous, minimum arity 2, structural equality, pattern matching with arity validation. This matches learner expectations from F# and aligns with functional programming pedagogy.

## Open Questions

Things that couldn't be fully resolved:

1. **Should wildcard pattern `_` be implemented in Phase 1?**
   - What we know: Success criteria don't explicitly require it, but TUP-02 implies pattern matching
   - What's unclear: Whether `let (x, _) = pair` should work in Phase 1 or wait for Phase 3 (Pattern Matching)
   - Recommendation: Implement basic wildcard in Phase 1 (minimal: just ignore binding), full pattern matching in Phase 3. This enables `let (x, _) = pair` which is pedagogically useful.

2. **What about singleton values that aren't tuples?**
   - What we know: `(1)` should parse as grouped expression, not 1-tuple
   - What's unclear: Should evaluator enforce minimum arity or should parser prevent 1-tuples?
   - Recommendation: Parser prevents 1-tuples through grammar (TupleElements requires at least 2). Evaluator doesn't need to check arity of tuple expressions, only pattern matching.

3. **Should tuple equality be structural or referential?**
   - What we know: FunLang uses F# `=` for IntValue, BoolValue equality
   - What's unclear: Should `(1, 2) = (1, 2)` be true (structural) or false (different instances)?
   - Recommendation: Structural equality (ML standard). When `Equal of Expr * Expr` evaluates tuples, use F#'s built-in `=` which provides structural equality for `Value list`.

4. **Integration with Phase 2 (Lists) - are tuples and lists distinct?**
   - What we know: Phase 1 and 2 are independent per dependency graph
   - What's unclear: Should `[1, 2]` (list of 2 elements) be different from `(1, 2)` (tuple)?
   - Recommendation: Yes, distinct types. Lists are homogeneous and variable-length, tuples are heterogeneous and fixed-length. This is standard ML design.

## Sources

### Primary (HIGH confidence)
- [How to represent tuples in AST? - OCaml Discussion](https://discuss.ocaml.org/t/how-to-represent-tuples-in-ast/14095) - AST design patterns
- [CS 251: Interpretive Dance - Wellesley](https://cs.wellesley.edu/~cs251/f15/assignments/smile/smile.html) - SML interpreter tuple implementation
- [F# Pattern Matching - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/pattern-matching) - Official F# pattern matching semantics
- [F# Tuples - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/tuples) - Official F# tuple syntax and destructuring
- [F# Wikibooks: Lexing and Parsing](https://en.wikibooks.org/wiki/F_Sharp_Programming/Lexing_and_Parsing) - FsYacc comma-separated lists
- [YACC Conflicts Documentation - Arizona](https://www2.cs.arizona.edu/~debray/Teaching/CSc453/DOCS/conflicts.pdf) - Shift/reduce resolution for parentheses

### Secondary (MEDIUM confidence)
- [Crafting Interpreters: Parsing Expressions](https://craftinginterpreters.com/parsing-expressions.html) - Precedence and grammar design
- [UCSD CSE 131: Tuples](https://ucsd-progsys.github.io/131-web/lectures/08-fer-de-lance.html) - Tuple arity mismatch errors in compiler course
- [Flow Tuple Types](https://flow.org/en/docs/types/tuples/) - Arity enforcement and type checking
- [Python PEP 634: Structural Pattern Matching](https://peps.python.org/pep-0634/) - Pattern matching semantics (different from ML but informative)
- [Python Literal Syntax Reference](https://python-reference.readthedocs.io/en/latest/docs/tuple/literals.html) - Tuple syntax comparison

### Tertiary (LOW confidence)
- WebSearch: "fsyacc tuple parsing" - Community discussions, no authoritative source
- WebSearch: "nested tuple interpreter" - No specific results, general knowledge applied

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All existing dependencies, no new packages
- Architecture: HIGH - Well-established ML interpreter patterns, verified in OCaml/SML courses
- Pitfalls: HIGH - Documented in yacc conflict resolution and ML implementation guides
- Grammar design: MEDIUM - Some trial-and-error expected for fsyacc-specific shift/reduce conflicts

**Research date:** 2026-02-01
**Valid until:** 2026-07-01 (6 months - stable domain, ML tuple semantics unchanged since 1970s)

**Key unknowns requiring planning-time decisions:**
- Exact grammar rule structure (may need refinement based on existing Expr/Term/Factor)
- Whether to add Pattern as new DU or extend Expr
- Wildcard pattern scope (Phase 1 vs Phase 3)
- COMMA token precedence declaration (if any)

**Planner should investigate:**
- Integration test: verify existing tests pass after adding tuple grammar
- Parser conflict resolution: may need to adjust Expr/Term/Factor hierarchy
- Pattern vs Expr separation: cleaner architecture but more code
