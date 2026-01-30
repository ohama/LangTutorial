# Features Research: REPL, Strings, Comments

**Project:** FunLang v2.0 Milestone
**Domain:** Interactive interpreter with enhanced type system
**Researched:** 2026-01-31
**Confidence:** HIGH

## Executive Summary

This document analyzes features for three additions to FunLang: REPL (interactive shell), string type, and comments. Features are categorized as table stakes (must have), differentiators (nice to have), and anti-features (explicitly avoid). Each feature includes complexity estimates and dependencies on existing FunLang infrastructure.

---

## REPL Features

### Table Stakes

Features users expect from any REPL. Missing any of these makes the REPL feel broken.

| Feature | Why Expected | Complexity | Dependencies | Notes |
|---------|--------------|------------|--------------|-------|
| **Basic read-eval-print loop** | Core functionality | Low | Existing parser/eval | Wrap existing `parse` and `evalExpr` in a loop |
| **Prompt display** | Users need visual cue for input | Low | None | Standard `> ` or `funlang> ` prompt |
| **Error recovery** | REPL should not crash on bad input | Low | Existing error handling | Catch exceptions, print error, continue loop |
| **Graceful exit** | Users need to quit cleanly | Low | None | Ctrl+D (EOF) or `exit`/`quit` command |
| **Result display** | Users need to see evaluation results | Low | Existing `formatValue` | Already implemented in Eval.fs |
| **Multi-expression sessions** | Variables from previous expressions usable | Medium | Environment threading | Persist `Env` across REPL iterations |

### Differentiators

Features that enhance usability but are not strictly required for a tutorial project.

| Feature | Value Proposition | Complexity | Dependencies | Notes |
|---------|-------------------|------------|--------------|-------|
| **Command history (readline)** | Up/down arrow for previous inputs | Medium | System.Console or external lib | F# can use `System.Console.ReadLine()` but no history by default |
| **Special `it` variable** | Last result accessible as `it` | Low | Env threading | Common in F# Interactive, OCaml, Python (`_`) |
| **Multi-line input** | Enter complex expressions | Medium | Parser modification | Detect incomplete input, continue reading |
| **REPL commands** | `#help`, `#clear`, `#env` | Low-Medium | Command parsing | Useful for debugging/learning |
| **Syntax highlighting** | Visual distinction of tokens | High | ANSI escape codes | Nice but complex, overkill for tutorial |
| **Tab completion** | Complete variable names | High | Trie/autocomplete logic | Overkill for tutorial scope |

### Anti-Features

Features to explicitly NOT build. Common mistakes in REPL implementation.

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| **Full IDE integration** | Scope creep, complex | Keep CLI-focused, simple text interface |
| **Persistent session save/load** | Complex serialization | Document expressions can be saved in `.fun` files |
| **Remote REPL server** | Network complexity, security | Local-only for tutorial |
| **Debugger integration** | Way out of scope | Print-based debugging sufficient |
| **Fancy prompt customization** | Unnecessary complexity | Fixed simple prompt |

### REPL Implementation Strategy

**Recommended approach:**
1. Simple `while true` loop with `Console.ReadLine()`
2. Thread environment between iterations for variable persistence
3. Special handling for EOF (Ctrl+D) to exit gracefully
4. Wrap parse/eval in try-catch for error recovery

**Entry point modification:**
- Add `--repl` flag to existing CLI
- When no args provided, could default to REPL (common pattern)

---

## String Features

### Table Stakes

Features users expect from any string type. Missing these makes strings unusable.

| Feature | Why Expected | Complexity | Dependencies | Notes |
|---------|--------------|------------|--------------|-------|
| **String literals** | Basic syntax `"hello"` | Medium | Lexer modification | Add STRING token with quote-delimited matching |
| **StringValue type** | Store string in Value union | Low | Ast.fs modification | Add `StringValue of string` case |
| **String display** | Print strings with quotes or raw | Low | formatValue modification | `"hello"` or `hello` depending on context |
| **Basic escape sequences** | `\"`, `\\`, `\n`, `\t` | Medium | Lexer string processing | Minimum viable set for usability |
| **String concatenation** | Combine strings with `+` or `^` | Low | Eval.fs modification | Overload `+` for strings (like F#) or add `^` |
| **String equality** | Compare strings with `=`, `<>` | Low | Eval.fs modification | Extend existing Equal/NotEqual patterns |
| **Empty string** | `""` should work | Low | Part of literal parsing | Edge case that must work |

### Differentiators

Features that add value but are not essential for tutorial scope.

| Feature | Value Proposition | Complexity | Dependencies | Notes |
|---------|-------------------|------------|--------------|-------|
| **String length** | `length "hello"` -> 5 | Low-Medium | Built-in function | Requires function call syntax or method |
| **String indexing** | `s.[0]` or `charAt s 0` | Medium | New AST node or built-in | F#/OCaml style indexing |
| **Substring** | Extract part of string | Medium | Built-in function | `substring s start len` |
| **String comparison operators** | `<`, `>`, `<=`, `>=` for strings | Low | Eval.fs extension | Lexicographic comparison |
| **Unicode support** | Non-ASCII characters | Low | .NET handles this | F# strings are UTF-16 by default |
| **Verbatim strings** | `@"no\escape"` | Low | Lexer rule | Useful but not essential |
| **Interpolated strings** | `$"x = {x}"` | High | Complex parsing | F# 5+ feature, complex to implement |
| **Triple-quoted strings** | `"""multi\nline"""` | Medium | Lexer rule | Useful for multi-line without escapes |

### Anti-Features

Features to explicitly NOT build for string type.

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| **Mutable strings** | F#/OCaml strings are immutable | Keep strings immutable, consistent with functional style |
| **Character type** | Adds type system complexity | Strings only; char is string of length 1 |
| **Regex built-in** | Way out of scope | Document can use string operations |
| **String methods** | OOP style `s.Length` | Use functional style `length s` or skip entirely |
| **Full Unicode escape** | `\u{1F42B}` complex | Basic `\n`, `\t`, `\\`, `\"` sufficient |
| **String multiplication** | `"ab" * 3 = "ababab"` | Non-standard, confusing |

### String Lexer Strategy

**Recommended approach for fslex:**

```
// Add to lexer rules
| '"'            { read_string (System.Text.StringBuilder()) lexbuf }

and read_string buf = parse
| '"'            { STRING (buf.ToString()) }
| '\\' 'n'       { buf.Append('\n') |> ignore; read_string buf lexbuf }
| '\\' 't'       { buf.Append('\t') |> ignore; read_string buf lexbuf }
| '\\' '\\'      { buf.Append('\\') |> ignore; read_string buf lexbuf }
| '\\' '"'       { buf.Append('"') |> ignore; read_string buf lexbuf }
| [^ '"' '\\']+  { buf.Append(lexeme lexbuf) |> ignore; read_string buf lexbuf }
| eof            { failwith "Unterminated string literal" }
```

**AST modification:**
```fsharp
type Expr =
    // ... existing cases ...
    | String of string  // NEW: string literal

type Value =
    | IntValue of int
    | BoolValue of bool
    | StringValue of string  // NEW
    | FunctionValue of param: string * body: Expr * closure: Env
```

---

## Comment Features

### Table Stakes

Features users expect from comments. Missing these makes code documentation impossible.

| Feature | Why Expected | Complexity | Dependencies | Notes |
|---------|--------------|------------|--------------|-------|
| **Single-line comments** | Basic code documentation | Low | Lexer modification | `// comment` or `-- comment` style |
| **Comments ignored by parser** | Comments don't affect execution | Low | Lexer skips comments | Return next token, don't emit comment token |
| **End-of-line handling** | Comment ends at newline | Low | Lexer rule | `"//" [^\n]*` pattern |
| **Comments anywhere** | After expressions, on own line | Low | Lexer design | Comments are whitespace-like |

### Differentiators

Features that enhance comments but are not essential.

| Feature | Value Proposition | Complexity | Dependencies | Notes |
|---------|-------------------|------------|--------------|-------|
| **Multi-line comments** | `(* ... *)` or `/* ... */` | Medium | Lexer state | Nested comments add complexity |
| **Nested multi-line comments** | `(* (* inner *) outer *)` | Medium-High | Counter in lexer | F#/OCaml support this, C/Java don't |
| **Doc comments** | `/// documentation` | Low | Could emit or ignore | Only useful if adding doc generation |

### Anti-Features

Features to explicitly NOT build for comments.

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| **Doc generation** | Out of scope | Comments for human readers only |
| **Comment preservation in AST** | Complicates AST | Comments are lexer-level only |
| **Preprocessor directives** | `#if`, `#define` complex | Not needed for tutorial |
| **Annotation comments** | `@param`, `@return` | Overkill for untyped language |

### Comment Syntax Decision

**Option A: ML/F# style (Recommended)**
- Single-line: `// comment`
- Multi-line: `(* comment *)`
- Rationale: Consistent with F# implementation language

**Option B: Haskell/SQL style**
- Single-line: `-- comment`
- Multi-line: `{- comment -}`
- Rationale: Common in ML-family educational languages

**Option C: C style**
- Single-line: `// comment`
- Multi-line: `/* comment */`
- Rationale: Familiar to most programmers

**Recommendation:** Option A (ML/F# style) for consistency with implementation language. Start with single-line `//` only; add multi-line `(* *)` as differentiator if time permits.

### Comment Lexer Strategy

**Single-line comments (minimum viable):**
```
| "//" [^ '\n']* { tokenize lexbuf }  // Skip to end of line
```

**Multi-line comments (if implemented):**
```
| "(*"           { comment 1 lexbuf }

and comment depth = parse
| "*)"           { if depth = 1 then tokenize lexbuf else comment (depth-1) lexbuf }
| "(*"           { comment (depth+1) lexbuf }  // Nested
| _              { comment depth lexbuf }
| eof            { failwith "Unterminated comment" }
```

---

## Dependencies on Existing FunLang Features

### REPL Dependencies

| Existing Feature | How REPL Uses It | Modification Needed |
|------------------|------------------|---------------------|
| `parse` function (Program.fs) | Parse user input | None, use as-is |
| `evalExpr` function (Eval.fs) | Evaluate parsed AST | May need `eval env expr` instead for env threading |
| `formatValue` function (Eval.fs) | Display results | None, use as-is |
| `Env` type (Ast.fs) | Persist variables | Need to pass env between iterations |
| CLI entry point (Program.fs) | Add `--repl` flag | Add new pattern match case |

### String Dependencies

| Existing Feature | How Strings Affect It | Modification Needed |
|------------------|----------------------|---------------------|
| `Value` type (Ast.fs) | Add StringValue case | Add `StringValue of string` |
| `Expr` type (Ast.fs) | Add String literal node | Add `String of string` |
| Lexer (Lexer.fsl) | Tokenize string literals | Add STRING token and rules |
| Parser (Parser.fsy) | Parse string literals | Add STRING to grammar |
| `eval` function (Eval.fs) | Evaluate strings | Add String case, extend Add/Equal |
| `formatValue` (Eval.fs) | Display strings | Add StringValue formatting |

### Comment Dependencies

| Existing Feature | How Comments Affect It | Modification Needed |
|------------------|----------------------|---------------------|
| Lexer (Lexer.fsl) | Skip comments | Add comment rules |
| Parser (Parser.fsy) | None | Comments don't reach parser |
| Everything else | None | Comments are lexer-only |

---

## Feature Priority Matrix

Based on tutorial value and implementation complexity:

### Phase 1: Comments (Foundation)
- **Why first:** Simplest addition, no AST changes, enables documenting example code
- **Scope:** Single-line `//` comments only
- **Complexity:** Low
- **Tutorial value:** High (all example code can be documented)

### Phase 2: Strings (Core Type)
- **Why second:** Adds new type to the language, substantial but well-understood
- **Scope:** Literals, concatenation, equality, basic escapes
- **Complexity:** Medium
- **Tutorial value:** High (demonstrates type system extension)

### Phase 3: REPL (User Experience)
- **Why third:** Builds on all other features, best user experience when language is complete
- **Scope:** Basic loop, environment persistence, error recovery, exit
- **Complexity:** Medium
- **Tutorial value:** Very High (transforms user experience)

---

## MVP Feature Set Recommendation

### Minimum Viable Product

**Comments:**
- Single-line `//` comments only

**Strings:**
- String literals with `"hello"` syntax
- Escape sequences: `\"`, `\\`, `\n`, `\t`
- Concatenation with `+` operator
- Equality with `=` and `<>` operators

**REPL:**
- Basic read-eval-print loop
- Environment persistence across expressions
- Error recovery (continue on error)
- Exit on EOF (Ctrl+D) or `exit` command
- Simple `> ` prompt

### Nice-to-Have (If Time Permits)

- Multi-line comments `(* *)`
- String comparison operators `<`, `>`, `<=`, `>=`
- String length built-in function
- `it` variable for last result in REPL
- `#help` and `#env` REPL commands

---

## Sources

### REPL
- [Wikipedia: Read-eval-print loop](https://en.wikipedia.org/wiki/Read%E2%80%93eval%E2%80%93print_loop)
- [Real Python: The Python Standard REPL](https://realpython.com/python-repl/)
- [Microsoft Learn: F# Interactive](https://learn.microsoft.com/en-us/dotnet/fsharp/tools/fsharp-interactive/)
- [Node.js REPL Documentation](https://nodejs.org/api/repl.html)

### Strings
- [Wikipedia: String literal](https://en.wikipedia.org/wiki/String_literal)
- [Wikipedia: Escape sequences in C](https://en.wikipedia.org/wiki/Escape_sequences_in_C)
- [FSharp.Core String Module](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-core-stringmodule.html)
- [Microsoft Learn: F# Strings](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/strings)
- [OCaml String Module](https://ocaml.org/api/String.html)

### Comments
- [Wikipedia: Comment (computer programming)](https://en.wikipedia.org/wiki/Comment_(computer_programming))
- [W3Schools: C Comments](https://www.w3schools.com/c/c_comments.php)
- [Oracle: Java Code Conventions - Comments](https://www.oracle.com/java/technologies/javase/codeconventions-comments.html)

### FsLex/FsYacc
- [FsLexYacc GitHub](https://github.com/fsprojects/FsLexYacc)
- [FsLexYacc JSON Parser Example](https://fsprojects.github.io/FsLexYacc/content/jsonParserExample.html)
- [F# Wikibooks: Lexing and Parsing](https://en.wikibooks.org/wiki/F_Sharp_Programming/Lexing_and_Parsing)
