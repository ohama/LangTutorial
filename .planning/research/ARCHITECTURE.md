# Architecture Research: REPL, Strings, Comments

**Domain:** Language interpreter features for FunLang
**Researched:** 2026-01-31
**Confidence:** HIGH

## Executive Summary

Adding REPL, strings, and comments to the existing FunLang interpreter follows well-established patterns. Each feature has clear integration points with minimal coupling between them:

- **Comments**: Lexer-only change, zero parser/AST/eval impact
- **Strings**: Full pipeline change (lexer -> parser -> AST -> eval)
- **REPL**: CLI-only change, uses existing parse/eval infrastructure

The existing pipeline (Lexer.fsl -> Parser.fsy -> Ast.fs -> Eval.fs -> Program.fs) is well-structured for extension. The modular design allows each feature to be implemented independently.

---

## Current Architecture Analysis

### Existing Components

| Component | File | Lines | Purpose |
|-----------|------|-------|---------|
| Lexer | FunLang/Lexer.fsl | 56 | Token generation |
| Parser | FunLang/Parser.fsy | 86 | AST construction |
| AST | FunLang/Ast.fs | 48 | Type definitions |
| Evaluator | FunLang/Eval.fs | 152 | Expression evaluation |
| CLI | FunLang/Program.fs | 123 | Command-line interface |
| Format | FunLang/Format.fs | 54 | Token/value formatting |

### Existing Data Flow

```
Input (string)
    |
    v
Lexer.tokenize (Lexer.fsl)
    |
    v
Token stream
    |
    v
Parser.start (Parser.fsy)
    |
    v
Ast.Expr
    |
    v
Eval.eval (Eval.fs)
    |
    v
Ast.Value
    |
    v
Eval.formatValue (Eval.fs)
    |
    v
Output (string)
```

---

## Component Changes

### Lexer (Lexer.fsl)

**Current State:**
- Single `tokenize` rule with pattern matching
- No state management (simple stateless lexer)
- Handles: whitespace, newlines, numbers, keywords, identifiers, operators

**Changes for Comments:**

```fsl
// Single-line comments: skip to end of line
| "//" [^ '\n']* newline?  { tokenize lexbuf }
| "//" [^ '\n']* eof       { EOF }

// Multi-line comments: nested comment support requires state
// Option A: Simple (no nesting) - skip to first *)
| "(*"                     { comment lexbuf }

// Define new rule for multi-line comments
and comment = parse
    | "*)"                 { tokenize lexbuf }
    | newline              { comment lexbuf }
    | eof                  { failwith "Unterminated comment" }
    | _                    { comment lexbuf }
```

**Changes for Strings:**

```fsl
// Add STRING token to parser declarations first
| '"'                      { read_string "" lexbuf }

// String reading rule with escape sequence handling
and read_string str = parse
    | '"'                  { STRING str }
    | "\\\""               { read_string (str + "\"") lexbuf }
    | "\\\\"               { read_string (str + "\\") lexbuf }
    | "\\n"                { read_string (str + "\n") lexbuf }
    | "\\t"                { read_string (str + "\t") lexbuf }
    | eof                  { failwith "Unterminated string" }
    | [^ '"' '\\' '\n']+   { read_string (str + (lexeme lexbuf)) lexbuf }
    | '\n'                 { failwith "Newline in string literal" }
    | _                    { failwith (sprintf "Invalid escape: %s" (lexeme lexbuf)) }
```

**Integration Point:** The `and` keyword creates additional lexer states that share the same lexbuf. This is the standard fslex pattern for multi-character constructs.

---

### Parser (Parser.fsy)

**Current State:**
- Token declarations in header
- Precedence for operators
- Grammar: Expr -> Term -> Factor -> AppExpr -> Atom

**Changes for Strings:**

```fsy
// Add token declaration
%token <string> STRING

// Add to Atom production (lowest level, like NUMBER)
Atom:
    | NUMBER             { Number($1) }
    | STRING             { String($1) }    // NEW
    | IDENT              { Var($1) }
    | TRUE               { Bool(true) }
    | FALSE              { Bool(false) }
    | LPAREN Expr RPAREN { $2 }
```

**Integration Point:** String literals are atoms (highest precedence). They don't require new operators or precedence rules.

**Future Consideration:** String concatenation operator (`^` or `++`) would need:
- New token in lexer
- Precedence declaration (between comparison and arithmetic?)
- Production in Expr level

---

### AST (Ast.fs)

**Current State:**
- `Expr` discriminated union with 19 cases
- `Value` type with `IntValue | BoolValue | FunctionValue`
- `Env` type alias for `Map<string, Value>`

**Changes for Strings:**

```fsharp
type Expr =
    | Number of int
    | String of string    // NEW: string literal
    | Bool of bool
    // ... existing cases

type Value =
    | IntValue of int
    | BoolValue of bool
    | StringValue of string    // NEW: string value
    | FunctionValue of param: string * body: Expr * closure: Env
```

**Integration Point:** Both types use F# discriminated unions. Adding a new case is a compile-time checked change - the compiler will flag all incomplete pattern matches in Eval.fs and Format.fs.

---

### Evaluator (Eval.fs)

**Current State:**
- Single recursive `eval` function
- Pattern matching on Expr
- `formatValue` helper for output

**Changes for Strings:**

```fsharp
// In eval function
| String s -> StringValue s

// In formatValue function
| StringValue s -> sprintf "\"%s\"" (escapeString s)
// or just: s (without quotes, user preference)
```

**Integration Point:** Minimal change. String literals are self-evaluating (like Number and Bool). No environment lookup or sub-expression evaluation needed.

**Future Consideration:** String operations would need:
- Concat: `| Concat(e1, e2) -> match eval... with StringValue s1, StringValue s2 -> StringValue (s1 + s2)`
- Length: `| Length e -> match eval... with StringValue s -> IntValue s.Length`
- Type checking in comparison operators (currently int-only for <, >, etc.)

---

### CLI (Program.fs)

**Current State:**
- Pattern matching on `argv` array
- Modes: --expr, --emit-tokens, --emit-ast, file input, --help
- Single-shot execution (parse, eval, print, exit)

**Changes for REPL:**

```fsharp
// New pattern for REPL mode
| [| |] | [| "--repl" |] ->
    repl ()

// REPL implementation
let rec repl () =
    printf "> "
    match Console.ReadLine() with
    | null -> 0  // Ctrl+D / EOF
    | "" -> repl ()  // Empty line, prompt again
    | input when input.Trim() = "exit" || input.Trim() = "quit" -> 0
    | input ->
        try
            let result = input |> parse |> evalExpr
            printfn "%s" (formatValue result)
        with ex ->
            eprintfn "Error: %s" ex.Message
        repl ()
```

**Integration Point:** Uses existing `parse` and `evalExpr` functions. The REPL is a loop wrapper around single-expression evaluation.

**Enhancement Options:**

1. **Basic Console.ReadLine**: Zero dependencies, works immediately
2. **ReadLine.Reboot NuGet**: History, arrow keys, tab completion
3. **Multi-line input**: Accumulate lines until balanced parens/keywords

---

### Format.fs

**Current State:**
- `formatToken` for token display (--emit-tokens)
- `formatTokens` joins tokens with spaces
- `lex` function for tokenizing

**Changes for Strings:**

```fsharp
// In formatToken
| Parser.STRING s -> sprintf "STRING(\"%s\")" s
```

**Integration Point:** Simple pattern match addition, parallel to NUMBER and IDENT handling.

---

### New Components

**None required for MVP.** All features integrate into existing components.

**Optional Future Components:**

| Component | Purpose | When Needed |
|-----------|---------|-------------|
| `History.fs` | REPL history persistence | If saving history to file |
| `Repl.fs` | REPL logic extraction | If REPL grows complex |
| `Escape.fs` | String escape utilities | If escape logic reused |

---

## Data Flow with New Features

**Comments:** Filtered out at lexer stage. Invisible to parser and beyond.

**Strings:** Flow through entire pipeline with new STRING token and String/StringValue types.

**REPL:** Wraps the entire flow in a loop, adds prompt/response cycle.

```
+--------------------+
|  REPL Loop (new)   |  <-- Only affects CLI layer
+--------------------+
         |
         v
    Input (string)
         |
         v
    [Lexer with comments filtered]  <-- Modified
    [Lexer with STRING token]       <-- Modified
         |
         v
    Token stream (+ STRING)
         |
         v
    [Parser with String production]  <-- Modified
         |
         v
    Ast.Expr (+ String case)         <-- Modified
         |
         v
    [Eval with StringValue]          <-- Modified
         |
         v
    Ast.Value (+ StringValue)        <-- Modified
         |
         v
    formatValue (+ StringValue)      <-- Modified
         |
         v
    Output (string)
         |
         v
+--------------------+
|  REPL continues    |
+--------------------+
```

---

## Build Order

Recommended implementation order based on dependencies and risk:

### Phase A: Comments (Lexer-only, lowest risk)

**Order:**
1. Add single-line comment rule (`// ...`)
2. Add multi-line comment rule (`(* ... *)`)
3. Test that comments are properly skipped

**Why First:**
- Zero parser/AST/eval changes
- Immediately useful for all future work
- Cannot break existing functionality (only adds skip rules)
- Quick win for validation

**Estimated Impact:**
- Lexer.fsl: +10-15 lines
- Tests: +10 test cases

---

### Phase B: Strings (Full pipeline, medium risk)

**Order:**
1. Add `STRING` token to Parser.fsy header
2. Add `String of string` to Expr in Ast.fs
3. Add `StringValue of string` to Value in Ast.fs
4. Compile - let compiler find all incomplete matches
5. Add `read_string` rule to Lexer.fsl
6. Add String production to Parser.fsy Atom
7. Add String case to Eval.eval
8. Add StringValue case to Eval.formatValue
9. Add STRING case to Format.formatToken

**Why This Order:**
- Type changes first forces compile-time checking
- Parser token declaration required before lexer can return STRING
- Each step is independently testable

**Estimated Impact:**
- Lexer.fsl: +15-20 lines (string rule with escapes)
- Parser.fsy: +2 lines
- Ast.fs: +2 lines
- Eval.fs: +5 lines
- Format.fs: +1 line
- Tests: +20 test cases

---

### Phase C: REPL (CLI-only, lowest coupling)

**Order:**
1. Add REPL pattern to main match in Program.fs
2. Implement basic repl() function with Console.ReadLine
3. Test basic REPL flow
4. (Optional) Add ReadLine.Reboot for enhanced input
5. (Optional) Add multi-line input support

**Why Last:**
- Uses existing parse/eval infrastructure
- Benefits from string support (users can test strings in REPL)
- Benefits from comments (users can comment in REPL session)
- Independent of parser/AST/eval changes

**Estimated Impact:**
- Program.fs: +20-30 lines (basic REPL)
- Program.fs: +10 more for enhanced features
- Dependencies: +1 NuGet package (optional)
- Tests: +5 integration tests

---

## Integration Points

| Feature | Lexer | Parser | AST | Eval | CLI | Format |
|---------|-------|--------|-----|------|-----|--------|
| Comments | MODIFY | - | - | - | - | - |
| Strings | MODIFY | MODIFY | MODIFY | MODIFY | - | MODIFY |
| REPL | - | - | - | - | MODIFY | - |

### Detailed Touch Points

**Comments:**
- Lexer.fsl: Add 2 rules (single-line, multi-line)
- No other files affected

**Strings:**
- Lexer.fsl: Add `read_string` rule with escapes
- Parser.fsy: Add STRING token, Atom production
- Ast.fs: Add String to Expr, StringValue to Value
- Eval.fs: Add String case, StringValue formatting
- Format.fs: Add STRING token formatting

**REPL:**
- Program.fs: Add REPL mode pattern and loop function
- Optionally add NuGet dependency for enhanced input

---

## Risk Assessment

| Feature | Risk | Mitigation |
|---------|------|------------|
| Comments | LOW | Lexer-only, cannot break parsing |
| Strings | MEDIUM | Compile-time checking via incomplete match warnings |
| REPL | LOW | Uses existing infrastructure, isolated to CLI |

### Potential Issues

**Strings:**
1. Escape sequence edge cases (Unicode, hex codes)
   - Mitigation: Start simple (\n, \t, \\, \"), extend later
2. Multi-line strings
   - Mitigation: Disallow initially (error on newline in string)
3. String in error messages (formatting)
   - Mitigation: Add helper function for escaping

**REPL:**
1. Multi-line expressions (let...in spans lines)
   - Mitigation: Start with single-line only
   - Enhancement: Buffer until balanced or explicit continuation
2. History persistence across sessions
   - Mitigation: Session-only history initially
3. Cross-platform input handling
   - Mitigation: Use Console.ReadLine or proven library

**Comments:**
1. Nested multi-line comments
   - Mitigation: Either disallow (simple) or track nesting depth (complex)
2. Comments in strings
   - Mitigation: String rule runs in separate lexer state, not affected

---

## Appendix: General Interpreter Architecture

For reference, a comprehensive overview of fslex/fsyacc interpreter architecture is provided below.

### Pipeline Overview

An interpreter built with fslex/fsyacc follows a classic multi-stage pipeline:

```
Source Code (string)
    |
    v
[LEXER (Lexer.fsl)]
    |
    v
Token Stream (Token list)
    |
    v
[PARSER (Parser.fsy)]
    |
    v
Abstract Syntax Tree (AST)
    |
    v
[EVALUATOR (Evaluator.fs)]
    |
    v
Result Value + Updated Environment
```

### Component Responsibilities

1. **Lexer** - Converts source to tokens, filters whitespace/comments
2. **Parser** - Validates syntax, constructs AST
3. **AST** - Discriminated unions representing language constructs
4. **Environment** - Variable bindings (Map<string, Value>)
5. **Evaluator** - Traverses AST, executes expressions
6. **REPL** - Interactive loop (optional)

### Build Order

F# requires files in dependency order:
1. Ast.fs (types)
2. Parser.fsy -> Parser.fs (generates tokens)
3. Lexer.fsl -> Lexer.fs (uses tokens from Parser)
4. Eval.fs (uses Ast)
5. Format.fs (uses Ast, Parser)
6. Program.fs (uses everything)

### Design Best Practices

- Keep AST immutable (discriminated unions)
- Maintain clear separation of concerns
- Use F# pattern matching for visitor pattern
- Provide good error messages with source location

---

## Sources

- [FsLex Overview](https://fsprojects.github.io/FsLexYacc/content/fslex.html) - Official fslex documentation
- [FsLexYacc JSON Parser Example](https://github.com/fsprojects/FsLexYacc/blob/master/docs/content/jsonParserExample.md) - String handling patterns
- [ReadLine.Reboot](https://www.nuget.org/packages/ReadLine.Reboot/3.2.0) - Enhanced console input library
- [F# Interactive](https://learn.microsoft.com/en-us/dotnet/fsharp/tools/fsharp-interactive/) - Microsoft REPL reference
- [Wikibooks F# Lexing](https://en.wikibooks.org/wiki/F_Sharp_Programming/Lexing_and_Parsing) - Tutorial reference
