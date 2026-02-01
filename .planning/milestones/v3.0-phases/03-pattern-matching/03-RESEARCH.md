# Phase 3: Pattern Matching - Research

**Researched:** 2026-02-01
**Domain:** Pattern matching, match expressions, exhaustiveness checking
**Confidence:** HIGH

## Summary

Pattern matching combines structural destructuring with conditional branching in a single construct. The `match` expression evaluates a scrutinee (subject expression) against a sequence of pattern clauses, executing the first matching branch's expression. This phase extends FunLang's existing pattern infrastructure (VarPat, TuplePat, WildcardPat from Phase 1) to add:

1. **Match expression AST node** - New expression type with scrutinee and pattern clauses
2. **Additional pattern types** - Constant patterns (int, bool), cons patterns (h :: t), empty list patterns ([])
3. **Pattern matching evaluation** - Sequential pattern testing with first-match semantics
4. **Exhaustiveness checking** - Warning system for non-exhaustive matches (optional but recommended)

The standard approach follows ML-family languages (F#, OCaml, Haskell): match expressions are syntactic sugar for cascading pattern tests, patterns are tried sequentially from top to bottom, and the first match wins. Implementation complexity lies in parsing the `match...with...` syntax (new for FunLang) and extending the existing `matchPattern` function to handle new pattern types.

**Primary recommendation:** Implement match expression in two stages - (1) core match evaluation with all pattern types, (2) exhaustiveness checking as a separate analysis pass. Start with runtime-only matching (raise error on non-exhaustive match), then add compile-time warnings if time permits.

## Standard Stack

The established tools for implementing pattern matching in F# interpreters:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| FsLexYacc | 11.3.0 | Lexer/parser generation | Already in use, handles ML-style syntax well |
| F# (compiler) | .NET 10 | Host language with native pattern matching | Meta-circular advantage - F#'s match guides implementation |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Expecto | 5.1+ | Unit testing | Test individual pattern matching scenarios |
| FsCheck | 3.0+ | Property testing | Verify pattern matching laws (if applicable) |
| fslit | N/A | Integration testing | End-to-end match expression evaluation |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Sequential matching | Decision tree compilation | Decision trees optimize performance but add complexity; sequential is simpler and matches tutorial scope |
| Runtime-only checking | Compile-time exhaustiveness | Compile-time warnings are better UX but require implementing Maranget's algorithm; runtime is sufficient for v3.0 |

**Installation:**
No new dependencies required - existing stack sufficient.

## Architecture Patterns

### Recommended AST Extension

```fsharp
// In Ast.fs

type Expr =
    | ... // existing cases
    | Match of scrutinee: Expr * clauses: MatchClause list

and MatchClause = Pattern * Expr  // (pattern, result expression)

and Pattern =
    | VarPat of string           // x - already exists
    | WildcardPat                // _ - already exists
    | TuplePat of Pattern list   // (p1, p2) - already exists
    | ConsPat of Pattern * Pattern   // h :: t - NEW
    | EmptyListPat               // [] - NEW
    | ConstPat of Constant       // 1, true - NEW

and Constant =
    | IntConst of int
    | BoolConst of bool
```

**Why this structure:**
- Reuses existing Pattern type from Phase 1
- MatchClause as tuple is simple (no need for named record)
- Constant as separate type enables future extension (string, char constants)
- ConsPat uses two sub-patterns (not list) to enforce h :: t structure

### Pattern 1: Sequential Pattern Matching

**What:** Evaluate patterns in order, return first match, error if none match
**When to use:** All ML-family interpreters
**Example:**
```fsharp
// Source: F# pattern matching semantics
// In Eval.fs
let rec evalMatch (env: Env) (scrutinee: Value) (clauses: MatchClause list) : Value =
    match clauses with
    | [] -> failwith "Match failure: no pattern matched"
    | (pattern, expr) :: rest ->
        match matchPattern pattern scrutinee with
        | Some bindings ->
            let extendedEnv = List.fold (fun e (n, v) -> Map.add n v e) env bindings
            eval extendedEnv expr
        | None -> evalMatch env scrutinee rest
```

### Pattern 2: Extended matchPattern Function

**What:** Extend existing matchPattern to handle new pattern types
**When to use:** Maintaining consistency with Phase 1 implementation
**Example:**
```fsharp
// Source: Existing Eval.fs matchPattern + new cases
let rec matchPattern (pat: Pattern) (value: Value) : (string * Value) list option =
    match pat, value with
    // Existing cases
    | VarPat name, v -> Some [(name, v)]
    | WildcardPat, _ -> Some []
    | TuplePat pats, TupleValue vals -> (* existing logic *)

    // NEW cases for Phase 3
    | ConstPat (IntConst n), IntValue m when n = m -> Some []
    | ConstPat (BoolConst b1), BoolValue b2 when b1 = b2 -> Some []
    | EmptyListPat, ListValue [] -> Some []
    | ConsPat (headPat, tailPat), ListValue (h :: t) ->
        match matchPattern headPat h with
        | Some headBindings ->
            match matchPattern tailPat (ListValue t) with
            | Some tailBindings -> Some (headBindings @ tailBindings)
            | None -> None
        | None -> None

    | _ -> None  // Type/value mismatch
```

### Pattern 3: Parser Structure for Match Expression

**What:** Use fsyacc's list construction pattern for match clauses
**When to use:** Parsing `match e with | p1 -> e1 | p2 -> e2`
**Example:**
```fsharp
// Source: fsyacc documentation + existing Parser.fsy patterns
// In Parser.fsy

%token MATCH WITH PIPE

Expr:
    | MATCH Expr WITH MatchClauses  { Match($2, $4) }
    // ... existing cases

MatchClauses:
    | PIPE Pattern ARROW Expr                { [($2, $4)] }
    | PIPE Pattern ARROW Expr MatchClauses   { ($2, $4) :: $5 }

Pattern:
    // Existing patterns
    | LPAREN PatternList RPAREN   { TuplePat($2) }
    | IDENT                       { VarPat($1) }
    | UNDERSCORE                  { WildcardPat }

    // NEW patterns for Phase 3
    | NUMBER                      { ConstPat(IntConst($1)) }
    | TRUE                        { ConstPat(BoolConst(true)) }
    | FALSE                       { ConstPat(BoolConst(false)) }
    | LBRACKET RBRACKET           { EmptyListPat }
    | Pattern CONS Pattern        { ConsPat($1, $3) }
```

**Note:** PIPE token conflicts with potential future OR patterns (`p1 | p2`). For Phase 3, use leading `|` only (F# style allows omitting first `|`). Consider grammar carefully.

### Pattern 4: Exhaustiveness Checking (Optional Enhancement)

**What:** Warn when match doesn't cover all possible values
**When to use:** Compile-time analysis, separate pass after parsing
**Example:**
```fsharp
// Source: Simplified Maranget algorithm concept
// Conceptual - not required for v3.0 MVP

type Coverage =
    | Complete                    // All cases covered
    | Incomplete of string list   // Missing cases (example values)

let checkExhaustiveness (scrutineeType: Type) (patterns: Pattern list) : Coverage =
    // Simplified: check common cases
    // Full implementation requires Maranget's matrix algorithm
    match scrutineeType with
    | BoolType ->
        let hasTrue = patterns |> List.exists (fun p -> p = ConstPat(BoolConst true))
        let hasFalse = patterns |> List.exists (fun p -> p = ConstPat(BoolConst false))
        let hasWildcard = patterns |> List.exists (fun p -> p = WildcardPat || p = VarPat _)
        if (hasTrue && hasFalse) || hasWildcard then Complete
        else if hasTrue then Incomplete ["false"]
        else if hasFalse then Incomplete ["true"]
        else Incomplete ["true"; "false"]
    | _ -> Complete  // Conservative: assume exhaustive for other types
```

**Decision:** Defer exhaustiveness checking to post-v3.0. Runtime errors are sufficient for MVP.

### Anti-Patterns to Avoid

- **Don't use decision trees** - Too complex for tutorial scope; sequential matching is clearer
- **Don't check exhaustiveness in evaluation** - Separation of concerns: checking is analysis, evaluation is runtime
- **Don't parse optional leading `|`** - Requires lookahead, complicates grammar; mandate leading `|` (F# style)
- **Don't special-case nested patterns in parser** - Let recursion handle nesting naturally

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Exhaustiveness checking | Custom boolean logic | Maranget's algorithm (or defer) | Correct exhaustiveness checking requires matrix-based analysis; simple boolean checks miss corner cases (nested patterns, OR patterns) |
| Pattern overlap detection | Manual case comparison | Maranget's usefulness algorithm | Detecting redundant patterns (e.g., `| x -> ...` after `| _ -> ...`) requires sophisticated analysis |
| Pattern compilation | String-based code generation | Direct AST interpretation | Pattern matching compiles to decision trees in production compilers, but interpreters should evaluate patterns directly |

**Key insight:** Pattern matching theory is deep (Maranget's papers span decades). For a tutorial interpreter, runtime-only matching is sufficient. Exhaustiveness checking can be added post-v3.0 as an enhancement.

## Common Pitfalls

### Pitfall 1: Cons Pattern Right-Associativity

**What goes wrong:** Parsing `h :: t :: rest` incorrectly as `(h :: t) :: rest` instead of `h :: (t :: rest)`
**Why it happens:** Cons is right-associative in expressions but requires same associativity in patterns
**How to avoid:** Pattern grammar must mirror expression grammar - `Pattern CONS Pattern` uses right-recursion naturally
**Warning signs:** Test case `match [1, 2, 3] with | h :: t :: rest -> ...` fails to parse or matches incorrectly

### Pitfall 2: Match Expression Precedence

**What goes wrong:** `let x = match y with | 1 -> 2 | _ -> 3 in x + 1` parses incorrectly
**Why it happens:** Match body expressions extend as far right as possible without explicit terminator
**How to avoid:** Use lowest precedence for match in expression grammar (lower than let-in), document that match arms need parentheses in some contexts
**Warning signs:** Parser conflicts or runtime evaluation errors when match is nested in expressions

### Pitfall 3: Pattern Variable Shadowing

**What goes wrong:** `match (1, 2) with | (x, x) -> x` matches with duplicate variables
**Why it happens:** Parser doesn't track variable uniqueness within patterns
**How to avoid:** Add pattern validation pass OR rely on F# compiler's pattern checking (for development), document as limitation
**Warning signs:** Test case with duplicate variables compiles but has undefined behavior

### Pitfall 4: Empty Match Clauses

**What goes wrong:** `match x with` with zero clauses accepted by parser
**Why it happens:** Grammar uses list construction `MatchClauses: | clause | clause MatchClauses` which can be empty
**How to avoid:** Use non-empty list grammar: `MatchClauses: clause | clause MatchClauses`
**Warning signs:** Parser accepts `match x with` without error, runtime crashes

### Pitfall 5: Constant Pattern Type Mismatch

**What goes wrong:** `match true with | 1 -> "bad"` type-checks but always fails at runtime
**Why it happens:** Interpreter has no static type system to catch bool-vs-int mismatch
**How to avoid:** Document limitation, rely on runtime error "Match failure: no pattern matched"
**Warning signs:** User confusion when valid-looking patterns don't match

## Code Examples

Verified patterns from official sources:

### Match Expression Evaluation
```fsharp
// Source: F# semantics + existing FunLang eval style
// In Eval.fs

and eval (env: Env) (expr: Expr) : Value =
    match expr with
    // ... existing cases

    | Match (scrutinee, clauses) ->
        let value = eval env scrutinee
        evalMatchClauses env value clauses

and evalMatchClauses (env: Env) (scrutinee: Value) (clauses: MatchClause list) : Value =
    match clauses with
    | [] -> failwith "Match failure: no pattern matched"
    | (pattern, resultExpr) :: rest ->
        match matchPattern pattern scrutinee with
        | Some bindings ->
            let extendedEnv = List.fold (fun e (n, v) -> Map.add n v e) env bindings
            eval extendedEnv resultExpr
        | None ->
            evalMatchClauses env scrutinee rest
```

### Extended Pattern Matching
```fsharp
// Source: Existing matchPattern + new pattern types
// In Eval.fs

let rec matchPattern (pat: Pattern) (value: Value) : (string * Value) list option =
    match pat, value with
    // Existing patterns (Phase 1)
    | VarPat name, v -> Some [(name, v)]
    | WildcardPat, _ -> Some []
    | TuplePat pats, TupleValue vals ->
        if List.length pats <> List.length vals then None
        else
            let bindings = List.map2 matchPattern pats vals
            if List.forall Option.isSome bindings then
                Some (List.collect Option.get bindings)
            else None

    // NEW: Constant patterns
    | ConstPat (IntConst n), IntValue m ->
        if n = m then Some [] else None
    | ConstPat (BoolConst b1), BoolValue b2 ->
        if b1 = b2 then Some [] else None

    // NEW: Empty list pattern
    | EmptyListPat, ListValue [] -> Some []

    // NEW: Cons pattern
    | ConsPat (headPat, tailPat), ListValue (h :: t) ->
        match matchPattern headPat h with
        | Some headBindings ->
            match matchPattern tailPat (ListValue t) with
            | Some tailBindings -> Some (headBindings @ tailBindings)
            | None -> None
        | None -> None

    // Mismatch
    | _ -> None
```

### Lexer Tokens
```fsharp
// Source: F# match syntax
// In Lexer.fsl

// Add tokens:
| "match"       { MATCH }
| "with"        { WITH }
| '|'           { PIPE }
```

### Parser Grammar
```fsharp
// Source: fsyacc documentation + F# syntax
// In Parser.fsy

%token MATCH WITH PIPE

Expr:
    // Add match before let (lower precedence)
    | MATCH Expr WITH MatchClauses  { Match($2, $4) }
    | LET IDENT EQUALS Expr IN Expr  { Let($2, $4, $6) }
    // ... rest of expression cases

MatchClauses:
    | PIPE Pattern ARROW Expr                { [($2, $4)] }
    | PIPE Pattern ARROW Expr MatchClauses   { ($2, $4) :: $5 }

Pattern:
    // Existing
    | LPAREN PatternList RPAREN   { TuplePat($2) }
    | IDENT                       { VarPat($1) }
    | UNDERSCORE                  { WildcardPat }

    // NEW for Phase 3
    | NUMBER                      { ConstPat(IntConst($1)) }
    | TRUE                        { ConstPat(BoolConst(true)) }
    | FALSE                       { ConstPat(BoolConst(false)) }
    | LBRACKET RBRACKET           { EmptyListPat }
    | Pattern CONS Pattern        { ConsPat($1, $3) }  // Right-associative like expression
```

### Test Examples (fslit format)
```fsharp
// Test: constant pattern matching
// --- Command: dotnet run --project FunLang -- %input
// --- Input:
match 1 with
| 1 -> "one"
| 2 -> "two"
| _ -> "other"
// --- Output:
"one"
```

```fsharp
// Test: list pattern matching
// --- Command: dotnet run --project FunLang -- %input
// --- Input:
match [1, 2, 3] with
| [] -> 0
| h :: t -> h
// --- Output:
1
```

```fsharp
// Test: tuple pattern matching
// --- Command: dotnet run --project FunLang -- %input
// --- Input:
match (1, 2) with
| (x, y) -> x + y
// --- Output:
3
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Decision tree compilation | Backtracking automata | OCaml 4.x (2010s) | Better optimization, but tutorial interpreters still use sequential |
| Boolean exhaustiveness | Maranget's matrix algorithm | 2007 (JFP paper) | Precise warnings, handles OR patterns and guards |
| Runtime-only errors | Compile-time warnings | Most modern FP languages | Better DX, but adds implementation complexity |

**Deprecated/outdated:**
- Nested if-then-else as "pattern matching" - Modern languages use true algebraic pattern matching
- String-based pattern compilation - Direct AST interpretation is clearer for educational purposes

## Open Questions

Things that couldn't be fully resolved:

1. **Exhaustiveness checking implementation complexity**
   - What we know: Maranget's algorithm is standard, requires matrix-based analysis
   - What's unclear: Whether simplified heuristics (e.g., "bool type needs true/false/wildcard") are sufficient for v3.0
   - Recommendation: Defer to post-v3.0; runtime errors are acceptable for tutorial

2. **PIPE token conflict with OR patterns**
   - What we know: Using `|` for match clauses prevents future `p1 | p2` OR patterns (would need different token)
   - What's unclear: Whether OR patterns are in scope for FunLang roadmap
   - Recommendation: Use PIPE for match clauses; if OR patterns needed later, use different syntax (e.g., `p1 || p2`)

3. **Pattern variable uniqueness checking**
   - What we know: OCaml/F# reject `(x, x)` patterns at compile-time
   - What's unclear: Whether to implement this check or document as limitation
   - Recommendation: Document as limitation for v3.0; second occurrence shadows first (F# semantics)

## Sources

### Primary (HIGH confidence)
- [F# Pattern Matching Reference](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/pattern-matching) - Official F# docs, syntax and semantics
- [OCaml Advanced Pattern Matching](https://cs3110.github.io/textbook/chapters/data/pattern_matching_advanced.html) - CS3110 textbook, evaluation semantics
- [FsLexYacc GitHub Repository](https://github.com/fsprojects/FsLexYacc) - Parser generator documentation
- Existing FunLang codebase (Ast.fs, Eval.fs, Parser.fsy) - Phase 1 pattern infrastructure

### Secondary (MEDIUM confidence)
- [OCaml Pattern Matching Exhaustiveness Discussion](https://discuss.ocaml.org/t/how-does-the-compiler-check-for-exhaustive-pattern-matching/5013) - Community explanation of exhaustiveness checking
- [Warnings for Pattern Matching (Maranget 2007)](http://moscova.inria.fr/~maranget/papers/warn/warn.pdf) - Original algorithm paper
- [C# Pattern Matching (2026)](https://blog.ndepend.com/c-pattern-matching-explained/) - Modern language comparison
- [Elm Destructuring Cheatsheet](https://gist.github.com/yang-wei/4f563fbf81ff843e8b1e) - Pattern syntax examples
- [Rust Exhaustiveness Checking](https://rustc-dev-guide.rust-lang.org/pat-exhaustive-checking.html) - Modern implementation approach

### Tertiary (LOW confidence)
- [Pattern Matching vs Destructuring Blog](https://blog.fogus.me/2011/01/12/pattern-matching-vs-destructuring-to-the-death.html) - Conceptual distinction
- [TypeScript Exhaustiveness Checking](https://www.geeksforgeeks.org/typescript-exhaustiveness-checking/) - Non-FP language perspective

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - FsLexYacc confirmed suitable, existing infrastructure reusable
- Architecture: HIGH - F# semantics well-documented, existing matchPattern extensible
- Pitfalls: MEDIUM - Identified from F# experience, some specific to FunLang untested

**Research date:** 2026-02-01
**Valid until:** ~2026-04-01 (60 days - stable domain, no rapid changes expected)
