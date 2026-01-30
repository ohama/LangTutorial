# Pitfalls Research: REPL, Strings, Comments

Research findings on common mistakes when adding REPL, string literals, and comments to an existing fslex/fsyacc-based interpreter (FunLang).

**Domain:** Adding features to existing F# interpreter
**Researched:** 2026-01-31
**Existing System:** FunLang v1.0 with fslex/fsyacc, 195 tests, closure-based functions

---

## REPL Pitfalls

| Pitfall | Warning Signs | Prevention | Phase |
|---------|--------------|------------|-------|
| **Environment not persisted between inputs** | `let x = 5` works, but `x` undefined on next line | Store and pass environment between REPL iterations; do not recreate `emptyEnv` each loop | REPL Core |
| **No incomplete input detection** | Multi-line `let ... in` crashes instead of prompting for continuation | Check for "Unexpected end of input" SyntaxError; return continuation prompt | REPL Core |
| **Creating new LexBuffer per line loses position** | Line numbers always show 1; error positions wrong | Accumulate input text or track global line offset; consider fresh buffer with offset tracking | REPL Core |
| **Ctrl+C crashes instead of canceling** | REPL exits on interrupt | Install `Console.CancelKeyPress` handler; reset input state | REPL Polish |
| **No way to exit cleanly** | Users type `:quit` but get "undefined variable" | Detect REPL commands before parsing; common: `:quit`, `:env`, `:help` | REPL Core |
| **Blocking on Console.ReadLine loses history** | No up-arrow for previous commands | Use readline library (e.g., ReadLine.NET) or implement basic history | REPL Polish |
| **evalExpr always starts with emptyEnv** | REPL cannot define persistent bindings | Create REPL-specific eval that accepts and returns Env | REPL Core |
| **Exceptions crash REPL loop** | One syntax error exits the entire REPL | Wrap parse/eval in try-catch; print error and continue | REPL Core |

### Critical REPL Pitfall: Environment Persistence

**What goes wrong:** Current `evalExpr` uses `emptyEnv`, so each REPL input starts fresh. Users expect:
```
> let x = 5
5
> x + 1
6
```
But get: "Undefined variable: x"

**Root cause:** `evalExpr` is designed for single-expression evaluation, not session persistence.

**Prevention:**
1. Create REPL-specific function: `evalInEnv : Env -> Expr -> Value * Env`
2. Let expressions should extend the persistent environment
3. Other expressions should evaluate in persistent environment but not modify it

**Detection:** Test multi-input sessions in REPL development.

### Critical REPL Pitfall: Multi-line Input

**What goes wrong:** User types `let f x =` and presses Enter. Parser throws "Unexpected end of input" and REPL shows error instead of continuation prompt.

**Root cause:** Parser treats incomplete input as error, not as "needs more input".

**Prevention:**
1. Catch parse exceptions and check if error indicates incomplete input
2. Patterns to detect: "Unexpected EOF", "Unexpected end of input"
3. Accumulate lines with secondary prompt (`...`) until complete
4. Provide `.break` command to abandon multi-line input

**Detection:** Try entering `if true then` without the else clause.

---

## String Pitfalls

| Pitfall | Warning Signs | Prevention | Phase |
|---------|--------------|------------|-------|
| **String rule swallows everything including EOF** | Unterminated strings hang or consume rest of file | Use separate lexer state with explicit newline/EOF handling | String Lexer |
| **Escape sequences not processed** | `"\n"` outputs literal backslash-n | Process escapes in lexer action or post-process; use StringBuilder accumulator | String Lexer |
| **Newlines allowed in string literals** | Multi-line strings accepted but shouldn't be | Check for unescaped newline in string state; report "unterminated string" error | String Lexer |
| **Lexer state not reset on error** | After bad string, all subsequent tokens wrong | Ensure error recovery returns to initial state | String Lexer |
| **STRING token uses wrong type** | Token carries string but `%token` declares no type | Declare `%token <string> STRING` in Parser.fsy | Parser Integration |
| **No StringValue in Value type** | Can lex/parse strings but cannot store them | Add `StringValue of string` to Value discriminated union | AST/Eval |
| **String operations missing** | Have strings but cannot concatenate or get length | Plan string operations: `^` (concat), `length`, `substring` | String Operations |
| **Quote escaping broken** | `"he said \"hello\""` doesn't work | Handle `\"` escape sequence explicitly in lexer state | String Lexer |

### Critical String Pitfall: Lexer State Management

**What goes wrong:** Simple regex like `'"' [^"]* '"'` fails because:
1. Cannot handle escape sequences (`\"`)
2. Includes newlines (should be error)
3. No good error for unterminated string

**Root cause:** String literals need stateful lexing, not single regex.

**Prevention:** Use fslex's multiple entry points:

```
let str_buf = new System.Text.StringBuilder()

rule tokenize = parse
    | '"'      { str_buf.Clear() |> ignore; string_body lexbuf }
    | ...

and string_body = parse
    | '"'      { STRING (str_buf.ToString()) }
    | "\\n"    { str_buf.Append('\n') |> ignore; string_body lexbuf }
    | "\\t"    { str_buf.Append('\t') |> ignore; string_body lexbuf }
    | "\\\""   { str_buf.Append('"') |> ignore; string_body lexbuf }
    | "\\\\"   { str_buf.Append('\\') |> ignore; string_body lexbuf }
    | '\n'     { failwith "Unterminated string literal" }
    | eof      { failwith "Unterminated string literal" }
    | _        { str_buf.Append(lexbuf.LexemeChar 0) |> ignore; string_body lexbuf }
```

**Detection:** Test strings with escapes, newlines, and EOF.

### Critical String Pitfall: Value Type Extension

**What goes wrong:** Add STRING token and String AST node, but `eval` returns `IntValue 0` or crashes on string expressions.

**Root cause:** Value discriminated union only has `IntValue | BoolValue | FunctionValue`.

**Prevention:**
1. Add `StringValue of string` to Value type
2. Update `formatValue` to handle `StringValue`
3. Add string-specific operations (concatenation at minimum)
4. Update comparison operators if strings should be comparable

**Detection:** Evaluating `"hello"` should return `StringValue "hello"`, not crash.

---

## Comment Pitfalls

| Pitfall | Warning Signs | Prevention | Phase |
|---------|--------------|------------|-------|
| **Line comment consumes newline needed elsewhere** | `// comment` followed by code fails | Return to main rule after newline; do not consume newline in pattern | Comment Lexer |
| **Block comment order wrong** | `/*` matches before `*` in multiply | Put `"/*"` before `'*'` in lexer rules; longer matches first | Comment Lexer |
| **Block comment swallows EOF** | Unclosed `/*` hangs | Handle EOF in block comment state; report "unterminated comment" | Comment Lexer |
| **Nested block comments not handled** | `/* /* */ */` leaves `*/` as code | Pass nesting depth as argument; increment on `/*`, decrement on `*/` | Comment Lexer |
| **Comments inside strings processed** | `"foo // bar"` treated as comment | Only check for comments in main lexer state, not in string state | Comment Lexer |
| **Newline handling in block comments** | Line numbers wrong after multi-line comment | Track newlines in block comment state to maintain position info | Comment Lexer |
| **Line comment regex too greedy** | `//` followed by `*` causes issues | Use `"//" [^\n]*` pattern; do not try to match newline | Comment Lexer |

### Critical Comment Pitfall: Pattern Order in fslex

**What goes wrong:** You add `| '*'  { STAR }` for multiplication and `| "/*"  { ... }` for block comments. Block comments never match because `*` matches first.

**Root cause:** fslex uses longest-match, but if matches are same length, first rule wins. The real issue is `'/'` may match before `"/*"` is considered.

**Prevention:**
1. Order multi-character tokens before single-character tokens
2. Put `"/*"` and `"//"` BEFORE `'/'` if you have a division operator
3. Current FunLang uses `SLASH` for `/`, so add comment patterns before it

**Correct order in Lexer.fsl:**
```
// Comments (MUST be before SLASH)
| "//"          { line_comment lexbuf }
| "/*"          { block_comment 1 lexbuf }
// Then single-char
| '/'           { SLASH }
```

**Detection:** `1 /* comment */ + 2` should parse as `1 + 2`, not error.

### Critical Comment Pitfall: Nested Block Comments

**What goes wrong:** Users write `/* outer /* inner */ */` and get syntax error on trailing `*/`.

**Root cause:** Simple block comment rule matches first `*/`, leaving outer `*/` as tokens.

**Prevention:** Use argument passing to track nesting depth:

```
and block_comment depth = parse
    | "/*"     { block_comment (depth + 1) lexbuf }
    | "*/"     { if depth = 1 then tokenize lexbuf
                 else block_comment (depth - 1) lexbuf }
    | '\n'     { block_comment depth lexbuf }  // Track newlines if needed
    | eof      { failwith "Unterminated block comment" }
    | _        { block_comment depth lexbuf }
```

**Detection:** Test `/* /* nested */ */` and `/* /* /* triple */ */ */`.

---

## Integration Pitfalls

These pitfalls are specific to integrating new features with the existing FunLang codebase.

### 1. Token Type Sharing Disruption

**What goes wrong:** Adding STRING token breaks existing lexer because Parser.fs must be regenerated first.

**Warning Signs:**
- Build error: "Token 'STRING' is not defined"
- Lexer.fs fails to compile after adding token

**Prevention:**
1. Always add new tokens to Parser.fsy FIRST
2. Run fsyacc before fslex
3. Existing build order: `Parser.fsy -> Lexer.fsl -> *.fs`
4. Add tokens in the `%token` section, not scattered

**Phase:** Affects all feature phases; critical to do correctly.

### 2. AST Extension Breaking Pattern Matches

**What goes wrong:** Add `String of string` to Expr, but forget to update Eval.fs, Format.fs.

**Warning Signs:**
- Compiler warning: "Incomplete pattern matches"
- Runtime: "Match failure" on new AST nodes

**Prevention:**
1. When adding to Expr type, grep for `match expr with`
2. Update ALL pattern matches (Eval.fs, Format.fs, tests)
3. F# compiler warnings help; do not suppress them

**Affected files for Expr extension:**
- `Ast.fs` - add case
- `Eval.fs` - add eval logic
- `Format.fs` - add formatting
- `FunLang.Tests/AstTests.fs` - add tests

**Phase:** All AST-extending features (strings).

### 3. Value Type Extension Breaking Equality

**What goes wrong:** Add StringValue but comparison operators crash.

**Warning Signs:**
- `"a" = "a"` throws "Type error: = requires operands of same type"
- String equality returns wrong results

**Prevention:**
1. Extend `Equal` and `NotEqual` match cases in Eval.fs:
```fsharp
| Equal (left, right) ->
    match eval env left, eval env right with
    | IntValue l, IntValue r -> BoolValue (l = r)
    | BoolValue l, BoolValue r -> BoolValue (l = r)
    | StringValue l, StringValue r -> BoolValue (l = r)  // Add this
    | _ -> failwith "Type error: = requires operands of same type"
```
2. Decide: should strings support `<`, `>` etc.? (lexicographic comparison)

**Phase:** String evaluation phase.

### 4. Program.fs Main Loop Not REPL-Ready

**What goes wrong:** Try to add REPL by putting loop in main, but `evalExpr` design prevents state persistence.

**Warning Signs:**
- REPL works but variables don't persist
- Have to restructure significant code to add REPL

**Prevention:**
1. Create separate REPL module (`Repl.fs`)
2. Add REPL entry point to Program.fs:
```fsharp
| [| "--repl" |] | [| "-i" |] ->
    Repl.run ()
```
3. Keep existing batch evaluation unchanged
4. Repl module handles environment threading

**Phase:** REPL implementation.

### 5. Test Infrastructure Not Covering REPL

**What goes wrong:** Existing fslit tests work for single expressions but cannot test multi-line REPL sessions.

**Warning Signs:**
- No way to test "line 1, then line 2, then check state"
- REPL bugs found only in manual testing

**Prevention:**
1. Create separate REPL test module with session simulation
2. Test pattern: `runReplSession ["let x = 5"; "x + 1"] = ["5"; "6"]`
3. Keep fslit tests for expression evaluation
4. Add Expecto tests for REPL behavior

**Phase:** REPL testing phase.

### 6. Comment Patterns Breaking Existing Operator Tests

**What goes wrong:** Add `"//"` comment pattern, existing tests with division stop working.

**Warning Signs:**
- `6 / 2` returns wrong result or "unexpected token"
- Division tests fail after adding comments

**Prevention:**
1. Put comment patterns AFTER checking they don't conflict
2. Pattern `"//"` must come BEFORE pattern `'/'`
3. Run full test suite after adding comments
4. Note: FunLang uses `SLASH` for division, so check interaction

**Phase:** Comment lexer phase.

### 7. Format.fs Not Updated for New Types

**What goes wrong:** New StringValue added but REPL shows `<unknown>` or crashes when printing.

**Warning Signs:**
- Evaluating strings shows wrong output
- `--emit-tokens` shows unexpected format for STRING token

**Prevention:**
1. Update `formatValue` in Eval.fs (or Format.fs):
```fsharp
let formatValue (v: Value) : string =
    match v with
    | IntValue n -> string n
    | BoolValue b -> if b then "true" else "false"
    | FunctionValue _ -> "<function>"
    | StringValue s -> sprintf "\"%s\"" s  // Add this
```
2. Consider escape handling in output (show `\n` or actual newline?)

**Phase:** String evaluation phase.

---

## Phase-Specific Warnings

| Phase Topic | Likely Pitfall | Mitigation |
|-------------|---------------|------------|
| Comment Lexer | Pattern order wrong; `/*` not matching | Order multi-char before single-char tokens |
| Line Comments | Newline handling breaks subsequent parsing | Use `[^\n]*` without consuming newline |
| Block Comments | Unterminated comment hangs | Handle EOF in comment state |
| String Lexer | Simple regex fails on escapes | Use separate lexer state with StringBuilder |
| String Escapes | `\n` outputs literal characters | Process escapes in lexer, not later |
| String AST | Pattern matches incomplete | Update all `match expr with` locations |
| String Value | Comparison operators crash | Extend Equal/NotEqual for StringValue |
| REPL Core | Environment not persisted | Create REPL-specific eval with Env threading |
| REPL Multi-line | Incomplete input shows error | Detect "Unexpected EOF" and prompt for more |
| REPL Commands | `:quit` parsed as expression | Check for `:` prefix before parsing |
| REPL Testing | No automated session tests | Add Expecto tests simulating sessions |

---

## Prevention Checklist

Before starting implementation:

- [ ] Verify fsyacc/fslex build order documented
- [ ] Plan token additions to Parser.fsy
- [ ] Identify all files needing Expr case additions
- [ ] Identify all files needing Value case additions
- [ ] Plan escape sequence set for strings
- [ ] Decide on nested vs. non-nested block comments
- [ ] Design REPL environment persistence strategy
- [ ] Plan REPL command set (`:quit`, `:env`, etc.)
- [ ] Verify test infrastructure can cover new features

During implementation:

- [ ] Add tokens to Parser.fsy BEFORE modifying Lexer.fsl
- [ ] Run full test suite after each lexer modification
- [ ] Test strings with: escapes, newlines (should error), quotes, empty string
- [ ] Test comments with: line, block, nested (if supported), unterminated
- [ ] Test REPL with: multi-line input, environment persistence, error recovery

---

## Sources

### fslex/fsyacc
- [FsLex Overview](https://fsprojects.github.io/FsLexYacc/content/fslex.html) - Official documentation with state management
- [FsLexYacc fslex.md](https://github.com/fsprojects/FsLexYacc/blob/master/docs/content/fslex.md) - Multiple entry points, argument passing
- [Using FSLexYacc](https://thanos.codes/blog/using-fslexyacc-the-fsharp-lexer-and-parser/) - Practical tutorial
- [F Sharp Programming/Lexing and Parsing](https://en.wikibooks.org/wiki/F_Sharp_Programming/Lexing_and_Parsing) - Comprehensive guide

### REPL Implementation
- [Statements and State - Crafting Interpreters](https://craftinginterpreters.com/statements-and-state.html) - Environment persistence
- [Node.js REPL Documentation](https://nodejs.org/api/repl.html) - Recoverable errors for multi-line input
- [Read-eval-print loop - Wikipedia](https://en.wikipedia.org/wiki/Read%E2%80%93eval%E2%80%93print_loop) - REPL design patterns

### String Literal Handling
- [Improve error recovery for unterminated string literals](https://github.com/quick-lint/quick-lint-js/issues/56) - Error recovery strategies
- [LexBuffer API](https://fsprojects.github.io/FsLexYacc/reference/fsharp-text-lexing-lexbuffer-1.html) - F# lexer buffer operations

### Comment Handling
- [ocamllex tutorial](https://ohama.github.io/ocaml/ocamllex-tutorial/) - Multiple lexer states for comments
- [Lexer rules - ANTLR](https://github.com/antlr/antlr4/blob/master/doc/lexer-rules.md) - Pattern priority
