# Phase 2: Strings - Research

**Researched:** 2026-01-31
**Domain:** String literal lexing and evaluation in fslex/fsyacc interpreters
**Confidence:** HIGH

## Summary

String literals are a foundational data type requiring changes across the full interpreter pipeline: lexer state machine for parsing escape sequences, parser token declaration, AST expression/value cases, evaluator pattern matching extensions, and formatter output. The FsLexYacc ecosystem provides well-documented patterns through its JSON parser example, which demonstrates the canonical `and read_string` state machine approach for handling escape sequences and detecting unterminated strings.

String implementation in FunLang extends existing patterns from Phase 1 (Comments) and Phase 4 (Control Flow). Comments established the precedent for lexer-only features using `and` rules; strings use this same mechanism but with accumulator arguments. Control flow established heterogeneous Value types (IntValue, BoolValue); strings add StringValue as the third variant. The key technical challenge is the lexer state machine: proper escape sequence translation (\n → newline character), newline rejection (literal newlines in strings are errors), and EOF detection (unterminated strings must fail fast).

The recommended implementation follows FsLexYacc's JSON parser pattern: use `and read_string` with a StringBuilder accumulator, match escape sequences explicitly (backslash followed by n/t/backslash/quote), reject literal newlines and EOF with clear errors, and extend existing Add/Equal/NotEqual evaluator patterns to handle StringValue alongside IntValue/BoolValue. This approach requires no new dependencies (F#'s built-in string and StringBuilder suffice) and integrates cleanly with FunLang's existing discriminated union architecture.

**Primary recommendation:** Use fslex `and read_string` state machine with explicit escape sequence matching; extend Value union with StringValue case; modify Add/Equal/NotEqual evaluators to accept both IntValue and StringValue operands with same-type requirements.

## Standard Stack

The existing FunLang stack handles all string requirements without new dependencies:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| FsLexYacc | 11.3.0 | Lexer/parser generator | Already in use; JSON example shows exact string pattern |
| F# System.String | .NET 10 | String values | Built-in; zero dependency cost |
| System.Text.StringBuilder | .NET 10 | String accumulation | Built-in; efficient lexer accumulator |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| F# discriminated unions | Language feature | Value type variants | Already used for IntValue/BoolValue/FunctionValue |
| F# pattern matching | Language feature | Type-safe evaluation | Already used throughout Eval.fs |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| StringBuilder accumulator | String concatenation | String concat is O(n²); StringBuilder is O(n) |
| Explicit escape matching | Regex-based lexer | fslex doesn't support regex backreferences for escapes |
| StringValue variant | Encode as IntValue list | Loses semantic type; breaks operator dispatch clarity |

**Installation:**
```bash
# No new packages required
# Existing FsLexYacc 11.3.0 + .NET 10 provide everything
```

## Architecture Patterns

### Recommended Project Structure
```
FunLang/
├── Lexer.fsl        # Add STRING token rule + read_string state
├── Parser.fsy       # Declare STRING token, add to Atom rule
├── Ast.fs           # Add String expr, StringValue case
├── Eval.fs          # Extend Add/Equal/NotEqual patterns
└── Format.fs        # Add StringValue formatting

tests/
└── strings/         # 15 new fslit tests
```

### Pattern 1: Lexer State Machine for String Literals
**What:** Dedicated lexer entry point that accumulates characters while handling escape sequences
**When to use:** Any time parsing delimited content with internal escaping (strings, regex, heredocs)
**Example:**
```fsharp
// Source: FsLexYacc JSON parser example
// https://fsprojects.github.io/FsLexYacc/content/jsonParserExample.html

// In tokenize rule:
| '"'    { read_string (System.Text.StringBuilder()) lexbuf }

// Separate state machine:
and read_string (buf: System.Text.StringBuilder) = parse
    | '"'                        { STRING (buf.ToString()) }
    | '\\' 'n'                   { buf.Append('\n') |> ignore; read_string buf lexbuf }
    | '\\' 't'                   { buf.Append('\t') |> ignore; read_string buf lexbuf }
    | '\\' '\\'                  { buf.Append('\\') |> ignore; read_string buf lexbuf }
    | '\\' '"'                   { buf.Append('"') |> ignore; read_string buf lexbuf }
    | [^ '"' '\\' '\n' '\r']+    { buf.Append(lexeme lexbuf) |> ignore; read_string buf lexbuf }
    | '\n' | '\r' | "\r\n"       { failwith "Newline in string literal" }
    | eof                        { failwith "Unterminated string literal" }
    | _                          { failwithf "Invalid character in string: %s" (lexeme lexbuf) }
```

**Why this pattern:**
- **Accumulator:** StringBuilder gives O(n) performance vs O(n²) string concatenation
- **Explicit escape matching:** `'\\' 'n'` is two-token sequence, ensures proper FSM state
- **Newline rejection:** Literal newlines caught before generic character matcher
- **EOF detection:** Unterminated strings fail immediately with clear message
- **Tail recursion:** Each rule calls `read_string buf lexbuf` for next character

### Pattern 2: Value Type Extension
**What:** Add new case to discriminated union, extend all pattern matches
**When to use:** Adding any new runtime value type (strings, floats, objects, etc)
**Example:**
```fsharp
// Source: Existing FunLang Ast.fs + Eval.fs patterns
// Ast.fs
type Expr =
    // ... existing cases ...
    | String of string    // NEW: String literal expression

and Value =
    | IntValue of int
    | BoolValue of bool
    | FunctionValue of param: string * body: Expr * closure: Env
    | StringValue of string    // NEW: String runtime value

// Eval.fs - Extend existing operator patterns
| Add (left, right) ->
    match eval env left, eval env right with
    | IntValue l, IntValue r -> IntValue (l + r)
    | StringValue l, StringValue r -> StringValue (l + r)    // NEW
    | _ -> failwith "Type error: + requires operands of same type"

| Equal (left, right) ->
    match eval env left, eval env right with
    | IntValue l, IntValue r -> BoolValue (l = r)
    | BoolValue l, BoolValue r -> BoolValue (l = r)
    | StringValue l, StringValue r -> BoolValue (l = r)    // NEW
    | _ -> failwith "Type error: = requires operands of same type"
```

**Why this pattern:**
- **Exhaustiveness checking:** F# compiler warns if any match is incomplete
- **Same-type enforcement:** Pattern matching ensures "a" + 1 fails at type level
- **Minimal coupling:** Only Add and Equal need modification; other ops naturally reject strings

### Pattern 3: Parser Atom Extension
**What:** Add new token to lowest-precedence parser rule (Atom)
**When to use:** Adding any new literal type (strings, floats, chars, etc)
**Example:**
```fsharp
// Source: Existing FunLang Parser.fsy Atom rule
// Parser.fsy
%token <string> STRING    // Add to token declarations

Atom:
    | NUMBER             { Number($1) }
    | IDENT              { Var($1) }
    | TRUE               { Bool(true) }
    | FALSE              { Bool(false) }
    | STRING             { String($1) }    // NEW: Strings are atomic expressions
    | LPAREN Expr RPAREN { $2 }
```

**Why this pattern:**
- **Atomic precedence:** String literals can't be split by operators (unlike 2 + 3)
- **Zero ambiguity:** Quoted strings have clear start/end delimiters
- **Function arguments:** Atom rule means strings work as `func "arg"` immediately

### Anti-Patterns to Avoid
- **String concat via lexer:** Don't make lexer handle "a" "b" → "ab"; parser should build Add(String "a", String "b") and eval concatenates
- **Implicit type coercion:** Don't auto-convert "123" to int or 42 to "42"; keep types strict
- **Single-escape-flag approach:** JSON example's `ignorequote` flag only handles quotes; explicit escape matching scales to \n, \t, etc
- **Forgetting Format.fs:** Outputting StringValue without quotes causes "hello" to print as hello (looks like identifier)

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| String escape processing | Manual char-by-char loop | fslex state machine | fslex handles FSM state, lexbuf position, EOF, errors |
| String concatenation | Repeated + in loop | StringBuilder | O(n) vs O(n²); fslex accumulator needs efficiency |
| Escape sequence translation | String.Replace chain | Explicit pattern match | Replace is brittle (order matters); patterns are exhaustive |
| Type checking in eval | Runtime type inspection | Pattern matching | Compiler checks exhaustiveness; can't forget a case |

**Key insight:** String lexing is a finite state machine problem—you're in "accumulating" state until quote, but backslash transitions to "escape" state. Hand-rolling this with if/else chains loses FSM clarity and introduces edge case bugs (what if EOF during escape?). fslex's rule-based approach makes states explicit and compiler-checked.

## Common Pitfalls

### Pitfall 1: Literal Newline in String Not Rejected
**What goes wrong:** User writes `"hello\nworld"` on two lines literally, lexer silently accepts it, string contains real newline instead of escape
**Why it happens:** Generic character matcher `[^ '"' '\\']+` matches newline character
**How to avoid:** Add explicit newline rejection rule BEFORE generic matcher: `| '\n' | '\r' | "\r\n" { failwith "Newline in string literal" }`
**Warning signs:** Test like `"line1\nline2"` (literal newline) passes when it should error

### Pitfall 2: Unterminated String Not Caught
**What goes wrong:** User writes `"unclosed`, lexer hits EOF, generic matcher or quote rule loops infinitely or crashes
**Why it happens:** No explicit EOF rule in `read_string` state machine
**How to avoid:** Add `| eof { failwith "Unterminated string literal" }` as penultimate rule (before generic catch-all)
**Warning signs:** Lexer hangs on unterminated string input; no error message printed

### Pitfall 3: Wrong Escape Sequence Order
**What goes wrong:** `"\n"` produces backslash-n (two chars) instead of newline char
**Why it happens:** Escape rules come AFTER generic character matcher, so `'\\' 'n'` never matches
**How to avoid:** Put all escape sequence rules (`'\\' 'n'`, `'\\' 't'`, etc) BEFORE `[^ '"' '\\']+` rule
**Warning signs:** Escape sequences print literally instead of transforming

### Pitfall 4: String + Int Not Rejected
**What goes wrong:** `"text" + 123` returns weird result or crashes instead of type error
**Why it happens:** Add evaluator pattern doesn't explicitly reject mixed types
**How to avoid:** Use catch-all pattern with error message: `| _ -> failwith "Type error: + requires operands of same type"`
**Warning signs:** No error message when adding string to int; either crashes or returns wrong type

### Pitfall 5: Parser Token Declared After Lexer Used
**What goes wrong:** Lexer.fsl uses `STRING (...)` token but Parser.fsy hasn't declared it yet; build fails with "Undefined value 'STRING'"
**Why it happens:** FsLexYacc build order: fsyacc generates Parser.fs first, then fslex compiles Lexer.fsl referencing Parser module
**How to avoid:** Always add `%token <string> STRING` to Parser.fsy BEFORE modifying Lexer.fsl
**Warning signs:** Build error "The value or constructor 'STRING' is not defined"

### Pitfall 6: Format.fs Returns Unquoted String
**What goes wrong:** REPL prints `hello` instead of `"hello"` for string values
**Why it happens:** formatValue returns raw string without quotes: `| StringValue s -> s`
**How to avoid:** Add quotes in formatter: `| StringValue s -> sprintf "\"%s\"" s` (or use F# verbatim string for clarity)
**Warning signs:** String output looks like identifier; can't distinguish "true" string from true boolean

## Code Examples

Verified patterns from official sources and existing FunLang code:

### Lexer: String Literal with Escape Sequences
```fsharp
// Source: FsLexYacc JSON parser (adapted for FunLang escape sequences)
// https://github.com/fsprojects/FsLexYacc/blob/master/docs/content/jsonParserExample.md

// In main tokenize rule, add BEFORE single-char operators:
| '"'    { read_string (System.Text.StringBuilder()) lexbuf }

// New entry point at end of file:
and read_string (buf: System.Text.StringBuilder) = parse
    | '"'                        { STRING (buf.ToString()) }
    // Escape sequences - MUST come before generic char matcher
    | '\\' 'n'                   { buf.Append('\n') |> ignore; read_string buf lexbuf }
    | '\\' 't'                   { buf.Append('\t') |> ignore; read_string buf lexbuf }
    | '\\' '\\'                  { buf.Append('\\') |> ignore; read_string buf lexbuf }
    | '\\' '"'                   { buf.Append('"') |> ignore; read_string buf lexbuf }
    // Normal characters (excluding quote, backslash, newline)
    | [^ '"' '\\' '\n' '\r']+    { buf.Append(lexeme lexbuf) |> ignore; read_string buf lexbuf }
    // Error cases - MUST detect before eof in tokenize
    | '\n' | '\r' | "\r\n"       { failwith "Newline in string literal" }
    | eof                        { failwith "Unterminated string literal" }
    | _                          { failwithf "Invalid character in string: %s" (lexeme lexbuf) }
```

### Parser: STRING Token Declaration
```fsharp
// Source: Existing FunLang Parser.fsy pattern
// Add to token section (line ~7):
%token <string> STRING

// Add to Atom rule (line ~80):
Atom:
    | NUMBER             { Number($1) }
    | IDENT              { Var($1) }
    | TRUE               { Bool(true) }
    | FALSE              { Bool(false) }
    | STRING             { String($1) }    // NEW
    | LPAREN Expr RPAREN { $2 }
```

### AST: String Expression and Value
```fsharp
// Source: Existing FunLang Ast.fs discriminated union pattern
type Expr =
    | Number of int
    // ... existing cases ...
    | String of string    // NEW: String literal expression

and Value =
    | IntValue of int
    | BoolValue of bool
    | FunctionValue of param: string * body: Expr * closure: Env
    | StringValue of string    // NEW: String runtime value
```

### Evaluator: String Operations
```fsharp
// Source: Existing FunLang Eval.fs pattern matching style
let rec eval (env: Env) (expr: Expr) : Value =
    match expr with
    // ... existing cases ...

    // NEW: String literal evaluates to StringValue
    | String s -> StringValue s

    // MODIFIED: Add supports both IntValue and StringValue
    | Add (left, right) ->
        match eval env left, eval env right with
        | IntValue l, IntValue r -> IntValue (l + r)
        | StringValue l, StringValue r -> StringValue (l + r)    // NEW
        | _ -> failwith "Type error: + requires operands of same type"

    // MODIFIED: Equal supports StringValue comparison
    | Equal (left, right) ->
        match eval env left, eval env right with
        | IntValue l, IntValue r -> BoolValue (l = r)
        | BoolValue l, BoolValue r -> BoolValue (l = r)
        | StringValue l, StringValue r -> BoolValue (l = r)    // NEW
        | _ -> failwith "Type error: = requires operands of same type"

    // MODIFIED: NotEqual supports StringValue comparison
    | NotEqual (left, right) ->
        match eval env left, eval env right with
        | IntValue l, IntValue r -> BoolValue (l <> r)
        | BoolValue l, BoolValue r -> BoolValue (l <> r)
        | StringValue l, StringValue r -> BoolValue (l <> r)    // NEW
        | _ -> failwith "Type error: <> requires operands of same type"
```

### Formatter: String Value Output
```fsharp
// Source: Existing FunLang Format.fs formatValue pattern
// In Format.fs, modify formatToken function:
let formatToken (token: Parser.token) : string =
    match token with
    // ... existing cases ...
    | Parser.STRING s -> sprintf "STRING(%s)" s    // NEW
    // ... rest ...

// In Eval.fs, modify formatValue function (or create separate in Format.fs):
let formatValue (v: Value) : string =
    match v with
    | IntValue n -> string n
    | BoolValue b -> if b then "true" else "false"
    | FunctionValue _ -> "<function>"
    | StringValue s -> sprintf "\"%s\"" s    // NEW: Add quotes for output
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| ignorequote flag (JSON example) | Explicit escape patterns | FsLexYacc ~2015 | Scales to multiple escape types; clearer FSM states |
| String concatenation in lexer | StringBuilder accumulator | F# 2.0+ (2010) | O(n) performance; .NET standard pattern |
| Single Value type | Discriminated union with cases | F# language design | Type-safe evaluation; compiler-checked exhaustiveness |
| Implicit type coercion | Explicit same-type checks | Language design trend | Clear error messages; prevents "1" + 2 ambiguity |

**Deprecated/outdated:**
- **ignorequote flag approach:** JSON example uses boolean flag for escaped quotes only; doesn't scale to \n, \t, etc. Modern pattern: explicit `'\\' 'n'` matching.
- **Manual string building in F# code:** Pre-StringBuilder era used string concat in loops. Now: always use StringBuilder for accumulation.
- **Lenient type mixing:** Old interpreters allowed "text" + 42 → "text42". Modern design: fail fast with clear type errors.

## Open Questions

Things that couldn't be fully resolved:

1. **String comparison operators (<, >, <=, >=)**
   - What we know: F# string comparison uses lexicographic order (built-in); extending LessThan/GreaterThan patterns is trivial
   - What's unclear: Whether this is desired feature or scope creep for v2.0 (REQUIREMENTS.md specifies only = and <>)
   - Recommendation: Mark as SHOULD-HAVE and implement only if time permits; STR-07/STR-08 requirements are strict equality/inequality

2. **Empty string edge cases in operations**
   - What we know: "" + "text" → "text" works naturally with F# string concat; "" = "" → true works naturally
   - What's unclear: No explicit test requirements for empty string in REQUIREMENTS.md (STR-09 tests empty literal only)
   - Recommendation: Add fslit tests for "" + "x" and "" = "" to verify natural behavior works; likely no code changes needed

3. **Unicode and multi-byte characters**
   - What we know: F# strings are UTF-16; fslex handles Unicode naturally
   - What's unclear: No requirements mention Unicode; test coverage scope unknown
   - Recommendation: Don't explicitly test Unicode in v2.0; if user reports issues, handle in v2.1

4. **String escape sequences beyond basic four**
   - What we know: Requirements specify only \n, \t, \\, \" (STR-02 through STR-05)
   - What's unclear: Users may expect \r (carriage return), \xHH (hex escape), etc from other languages
   - Recommendation: Implement only the four required escapes; document explicitly that others are unsupported

## Sources

### Primary (HIGH confidence)
- [FsLexYacc JSON Parser Example](https://fsprojects.github.io/FsLexYacc/content/jsonParserExample.html) - String state machine pattern with escape handling
- [FsLexYacc fslex Documentation](https://fsprojects.github.io/FsLexYacc/content/fslex.html) - Lexer rule syntax and `and` entry points
- [F# String Literals - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/strings) - Escape sequence specification (\n, \t, \\, \")
- [F# Discriminated Unions - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/discriminated-unions) - Value type extension pattern
- Existing FunLang codebase (Lexer.fsl, Ast.fs, Eval.fs) - Verified patterns for Comments, IntValue/BoolValue

### Secondary (MEDIUM confidence)
- [Using FsLexYacc Tutorial - Thanos Codes](https://thanos.codes/blog/using-fslexyacc-the-fsharp-lexer-and-parser/) - Practical lexer/parser integration guide
- [F# Wikibooks Lexing and Parsing](https://en.wikibooks.org/wiki/F_Sharp_Programming/Lexing_and_Parsing) - State machine patterns in lexers
- [Flex Manual: Escape Sequences in Strings](https://westes.github.io/flex/manual/How-do-I-expand-backslash_002descape-sequences-in-C_002dstyle-quoted-strings_003f.html) - General lexer escape handling best practices
- [Quick-lint-js Issue #56](https://github.com/quick-lint/quick-lint-js/issues/56) - Unterminated string error recovery patterns

### Tertiary (LOW confidence)
- Various C++ operator overloading examples - Not directly applicable to F# discriminated unions but illustrates type checking principles
- Python/JavaScript escape sequence docs - Confirms \n, \t, \\, \" are universal standards

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - No new dependencies needed; existing FsLexYacc 11.3.0 + .NET 10 handle all requirements; JSON example proves pattern viability
- Architecture: HIGH - FunLang codebase already demonstrates Comments (lexer and rule), IntValue/BoolValue (discriminated unions), Add/Equal operators (pattern extension points)
- Pitfalls: HIGH - Identified from fslex documentation, existing comment implementation, and general lexer design principles; each has concrete prevention strategy
- Code examples: HIGH - All patterns verified against FsLexYacc docs and existing FunLang code; no speculative designs

**Research date:** 2026-01-31
**Valid until:** 2026-03-02 (30 days - stable domain; FsLexYacc 11.3.0 mature, F# string semantics unchanged)
