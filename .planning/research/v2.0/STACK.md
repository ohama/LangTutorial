# Stack Research: REPL, Strings, Comments

**Project:** FunLang v2.0 Milestone
**Researched:** 2026-01-31
**Confidence:** HIGH

## Executive Summary

Adding REPL, strings, and comments to FunLang requires **no new library dependencies**. The existing stack (FsLexYacc 11.3.0, .NET 10) provides everything needed:

- **REPL:** Use `System.Console.ReadLine()` - sufficient for tutorial scope
- **Strings:** FsLex multiple-rule pattern handles escape sequences natively
- **Comments:** FsLex lexer rules with state transitions (already supported)

A readline library would add polish but introduces dependency complexity inappropriate for a tutorial project. The recommendation is to implement all three features using only the existing stack.

---

## Recommended Stack Additions

### Summary: No New Dependencies Required

| Feature | Solution | Why Not External Library |
|---------|----------|--------------------------|
| REPL line input | `System.Console.ReadLine()` | Built-in, zero dependencies, tutorial-appropriate |
| REPL history | In-memory `List<string>` | Simple, no persistence needed for tutorial |
| String lexing | FsLex multi-rule pattern | [FsLexYacc docs](https://github.com/fsprojects/FsLexYacc/blob/master/docs/content/fslex.md) show this pattern |
| String escape sequences | Lexer state machine | Same approach as JSON parser example |
| Comments | FsLex skip rules | Standard lexer pattern |

---

## REPL Implementation Stack

### Recommended: System.Console (Built-in)

```fsharp
// No package needed - part of .NET BCL
open System

let input = Console.ReadLine()
Console.Write("funlang> ")
```

**Rationale:**
- Zero dependencies, works everywhere .NET runs
- Sufficient for tutorial scope (single-line expressions)
- Students understand it immediately
- Cross-platform (Windows, Linux, macOS)

### Not Recommended: External Readline Libraries

I evaluated several readline libraries. None are appropriate for this tutorial:

| Library | Version | Status | Why Not |
|---------|---------|--------|---------|
| [ReadLine (tonerdo)](https://github.com/tonerdo/readline) | 2.0.1 | **Abandoned** (last update 2018) | No updates in 8 years, technical debt |
| [ReadLine.Reboot](https://github.com/Aptivi-Archives/ReadLine.Reboot) | 3.2.0 | **Deprecated** | Maintainer explicitly recommends Terminaux instead |
| [Terminaux](https://www.nuget.org/packages/Terminaux) | 6.1.6 | Active (updated 2025-02) | **Overkill** - 60+ features when we need 1; complex API |
| [RadLine](https://github.com/spectreconsole/radline) | 0.9.0 | **Preview only** | Not production-ready; requires Spectre.Console dependency |
| [FsReadLine](https://github.com/B2R2-org/FsReadLine) | N/A | **No NuGet release** | Not published, cannot be easily added to project |

**If readline features are requested later:**
Document as "Optional Enhancement" appendix showing how to add Terminaux >= 6.1.6. Do not include in core tutorial.

### REPL Feature Implementation Map

| Feature | Implementation | Complexity |
|---------|----------------|------------|
| Basic loop | `while true do ... done` | ~15 LOC |
| Prompt display | `Console.Write("funlang> ")` | 1 LOC |
| Input reading | `Console.ReadLine()` | 1 LOC |
| Environment persistence | Thread `Env` between iterations | ~5 LOC |
| Error recovery | `try/catch` around parse/eval | ~5 LOC |
| Graceful exit | Check for `null` (EOF) or "exit" | ~3 LOC |
| Result display | Existing `formatValue` | 0 LOC (reuse) |

**Total: ~30 lines of new code**

---

## String Implementation Stack

### Recommended: FsLex Multi-Rule Pattern

FsLex supports multiple lexer rules (states) using the `and` keyword. This is the standard pattern for lexing strings with escape sequences, as shown in the [FsLexYacc JSON Parser Example](https://github.com/fsprojects/FsLexYacc/blob/master/docs/content/jsonParserExample.md).

**No external library needed** - everything is in FsLexYacc 11.3.0.

### String Lexer Design

```fsl
{
// In header section
let stringBuffer = System.Text.StringBuilder()
}

rule tokenize = parse
    // ... existing rules ...
    | '"'     { stringBuffer.Clear() |> ignore
                read_string lexbuf }

and read_string = parse
    | '"'           { STRING (stringBuffer.ToString()) }
    | '\\' 'n'      { stringBuffer.Append('\n') |> ignore
                      read_string lexbuf }
    | '\\' 't'      { stringBuffer.Append('\t') |> ignore
                      read_string lexbuf }
    | '\\' '\\'     { stringBuffer.Append('\\') |> ignore
                      read_string lexbuf }
    | '\\' '"'      { stringBuffer.Append('"') |> ignore
                      read_string lexbuf }
    | [^ '"' '\\']+  { stringBuffer.Append(LexBuffer<_>.LexemeString lexbuf) |> ignore
                      read_string lexbuf }
    | eof           { failwith "Unterminated string literal" }
```

### String Token and Type Additions

**Parser.fsy:**
```
%token <string> STRING
```

**Ast.fs:**
```fsharp
type Expr =
    // ... existing ...
    | String of string              // NEW

type Value =
    // ... existing ...
    | StringValue of string         // NEW
```

### String Operations to Support

| Operation | Syntax | Implementation | Complexity |
|-----------|--------|----------------|------------|
| Literal | `"hello"` | Lexer + Parser + Eval | Medium |
| Concatenation | `"a" + "b"` | Extend existing Add eval | Low |
| Equality | `"a" = "b"` | Extend existing Equal eval | Low |
| Inequality | `"a" <> "b"` | Extend existing NotEqual eval | Low |

**Escape sequences (minimal viable set):**

| Escape | Character | Why Include |
|--------|-----------|-------------|
| `\\` | Backslash | Standard, required for paths |
| `\"` | Double quote | Required to include quotes in strings |
| `\n` | Newline | Most common escape |
| `\t` | Tab | Common for formatting |

### String Operations: Not Recommended for MVP

| Operation | Why Defer |
|-----------|-----------|
| `length` function | Requires built-in function mechanism |
| `substring` function | Requires built-in function mechanism |
| String indexing `s.[0]` | Requires new syntax |
| Comparison `<`, `>` | Not essential for MVP |
| String interpolation `$"{x}"` | High complexity, can add in v3.0 |

---

## Comments Implementation Stack

### Recommended: FsLex Skip Rules

Comments are lexer-only - they never reach the parser or evaluator. FsLex handles this natively with rules that return the next token instead of emitting a comment token.

**No external library needed.**

### Single-Line Comments

```fsl
rule tokenize = parse
    // ... other rules first (order matters!) ...
    | "//" [^ '\n']* { tokenize lexbuf }  // Skip to end of line, continue
```

**Placement:** Must come AFTER multi-character operators that start with `/` (none in current FunLang, so safe).

### Multi-Line Comments (Optional Enhancement)

If nested comments `(* (* inner *) outer *)` are desired:

```fsl
rule tokenize = parse
    // ... other rules ...
    | "(*"    { comment 1 lexbuf }

and comment depth = parse
    | "*)"    { if depth = 1 then tokenize lexbuf
                else comment (depth - 1) lexbuf }
    | "(*"    { comment (depth + 1) lexbuf }  // Nested
    | _       { comment depth lexbuf }
    | eof     { failwith "Unterminated comment" }
```

**Rationale for ML-style `(* *)` over C-style `/* */`:**
- Consistent with F# (the implementation language)
- Supports nesting (unlike C-style)
- Familiar to functional programming learners

### Comment Complexity Assessment

| Comment Style | Complexity | LOC | Notes |
|---------------|------------|-----|-------|
| Single-line `//` | Low | ~2 | One lexer rule |
| Multi-line `(* *)` non-nested | Medium | ~5 | Second lexer rule |
| Multi-line `(* *)` nested | Medium | ~10 | Counter parameter |

**Recommendation:** Implement single-line `//` first. Add multi-line `(* *)` with nesting as an enhancement.

---

## Integration Points with Existing Stack

### Lexer.fsl Changes

```fsl
{
// ADD: String buffer for accumulating string contents
let stringBuffer = System.Text.StringBuilder()
}

rule tokenize = parse
    // ADD: Comments (must come before operators)
    | "//" [^ '\n']* { tokenize lexbuf }

    // EXISTING rules for whitespace, newline, numbers, keywords...

    // ADD: String literal start
    | '"'     { stringBuffer.Clear() |> ignore
                read_string lexbuf }

    // EXISTING rules for operators, identifiers, eof...

// ADD: String lexer state
and read_string = parse
    | '"'           { STRING (stringBuffer.ToString()) }
    | '\\' 'n'      { stringBuffer.Append('\n') |> ignore; read_string lexbuf }
    | '\\' 't'      { stringBuffer.Append('\t') |> ignore; read_string lexbuf }
    | '\\' '\\'     { stringBuffer.Append('\\') |> ignore; read_string lexbuf }
    | '\\' '"'      { stringBuffer.Append('"') |> ignore; read_string lexbuf }
    | [^ '"' '\\']+  { stringBuffer.Append(LexBuffer<_>.LexemeString lexbuf) |> ignore
                      read_string lexbuf }
    | eof           { failwith "Unterminated string literal" }
```

### Parser.fsy Changes

```
// ADD to %token declarations
%token <string> STRING

// ADD to precedence (if needed for concatenation)
// No change if reusing + for string concatenation

// ADD to atom production
atom:
  | NUMBER    { Number $1 }
  | STRING    { String $1 }    // NEW
  // ... rest ...
```

### Ast.fs Changes

```fsharp
type Expr =
    // ... existing 15+ cases ...
    | String of string              // NEW: String literal

type Value =
    | IntValue of int
    | BoolValue of bool
    | StringValue of string         // NEW: String value
    | FunctionValue of param: string * body: Expr * closure: Env
```

### Eval.fs Changes

```fsharp
let rec eval (env: Env) (expr: Expr) : Value =
    match expr with
    // ... existing cases ...

    // NEW: String literal
    | String s -> StringValue s

    // MODIFY: Extend Add for strings
    | Add (left, right) ->
        match eval env left, eval env right with
        | IntValue l, IntValue r -> IntValue (l + r)
        | StringValue l, StringValue r -> StringValue (l + r)  // NEW
        | _ -> failwith "Type error: + requires same-type operands"

    // MODIFY: Extend Equal for strings
    | Equal (left, right) ->
        match eval env left, eval env right with
        | IntValue l, IntValue r -> BoolValue (l = r)
        | BoolValue l, BoolValue r -> BoolValue (l = r)
        | StringValue l, StringValue r -> BoolValue (l = r)  // NEW
        | _ -> failwith "Type error: = requires operands of same type"
```

### Format.fs Changes

```fsharp
let formatValue (v: Value) : string =
    match v with
    | IntValue n -> string n
    | BoolValue b -> if b then "true" else "false"
    | StringValue s -> sprintf "\"%s\"" s  // NEW: Display with quotes
    | FunctionValue _ -> "<function>"
```

### Program.fs Changes (REPL)

```fsharp
[<EntryPoint>]
let main argv =
    match argv with
    // EXISTING patterns ...

    // NEW: REPL mode (no args or --repl)
    | [| |] | [| "--repl" |] ->
        printfn "FunLang REPL (type 'exit' to quit)"
        let rec loop (env: Env) =
            printf "funlang> "
            match Console.ReadLine() with
            | null | "exit" -> 0
            | input ->
                try
                    let ast = parse input
                    let result = eval env ast
                    printfn "%s" (formatValue result)
                    // Persist env - extract new bindings if needed
                    loop env  // or loop (updatedEnv) for let bindings
                with ex ->
                    eprintfn "Error: %s" ex.Message
                    loop env
        loop Eval.emptyEnv

    // EXISTING patterns ...
```

---

## Not Recommended

### External Readline Libraries

**Why avoid for this project:**

1. **Tutorial complexity** - Adds dependency management concepts before language concepts
2. **Ecosystem fragmentation** - ReadLine -> ReadLine.Reboot -> Terminaux deprecation chain
3. **Platform variance** - Terminal capabilities differ; adds debugging surface
4. **Scope creep** - REPL polish is not core to "language implementation tutorial"

### String Interpolation

**Why defer:**
- Requires parsing expressions inside strings (`$"x = {x + 1}"`)
- High implementation complexity
- Not foundational to language implementation learning
- Can be added in v3.0 milestone if desired

### Character Type

**Why avoid:**
- Adds type system complexity (char vs string)
- F#/OCaml treat char as separate type, but for tutorial simplicity, strings suffice
- `"a"` is a string of length 1 - good enough

### String Methods (OOP-style)

**Why avoid:**
- `s.Length`, `s.Substring(0, 3)` is OOP style
- FunLang is functional - use `length s`, `substring s 0 3` if adding these
- Methods require different evaluation strategy

---

## Version Constraints

### Required (No Changes to Package References)

| Package | Version | Notes |
|---------|---------|-------|
| FsLexYacc | 11.3.0 | Already in project; provides multi-rule lexer for strings |
| .NET SDK | 10.0 | Already targeted; `System.Console` and `System.Text.StringBuilder` built-in |

### Optional (Future Enhancement Only)

| Package | Version | When | Notes |
|---------|---------|------|-------|
| Terminaux | >= 6.1.6 | If REPL polish requested post-tutorial | .NET Standard 2.0 compatible; actively maintained |

---

## Implementation Complexity Assessment

| Component | Complexity | LOC Estimate | Risk |
|-----------|------------|--------------|------|
| Single-line comments `//` | Low | ~2 lines lexer | Minimal |
| Multi-line comments `(* *)` | Medium | ~10 lines lexer | Low |
| String literals | Medium | ~15 lines lexer | Low |
| String escape sequences | Medium | ~10 lines lexer | Low |
| STRING token in parser | Low | ~3 lines parser | Minimal |
| StringValue in AST | Low | ~2 lines AST | Minimal |
| String evaluation | Low | ~5 lines eval | Minimal |
| String concatenation | Low | ~3 lines eval | Minimal |
| String equality | Low | ~2 lines eval | Minimal |
| String display | Low | ~2 lines format | Minimal |
| REPL basic loop | Low | ~25 lines program | Low |
| REPL env persistence | Medium | ~10 lines program | Low |

**Total estimate:** ~90 lines of new/modified code

---

## Recommended Implementation Order

Based on dependencies and complexity:

### Phase 1: Comments
- **Scope:** Single-line `//` comments
- **Why first:** Simplest, no AST changes, enables documenting code immediately
- **LOC:** ~2 lines

### Phase 2: Strings
- **Scope:** Literals, escapes, concatenation, equality
- **Why second:** Adds new type, requires lexer state machine
- **LOC:** ~40 lines

### Phase 3: REPL
- **Scope:** Basic loop, env persistence, error recovery
- **Why third:** Builds on complete language, best UX when all features work
- **LOC:** ~35 lines

---

## Sources

### Primary Documentation
- [FsLexYacc Documentation - FsLex Overview](https://fsprojects.github.io/FsLexYacc/content/fslex.html)
- [FsLexYacc GitHub - fslex.md](https://github.com/fsprojects/FsLexYacc/blob/master/docs/content/fslex.md)
- [FsLexYacc JSON Parser Example](https://github.com/fsprojects/FsLexYacc/blob/master/docs/content/jsonParserExample.md) - String lexing pattern

### Library Evaluation
- [ReadLine by tonerdo](https://github.com/tonerdo/readline) - Original, abandoned since 2018
- [ReadLine.Reboot](https://github.com/Aptivi-Archives/ReadLine.Reboot) - Deprecated, recommends Terminaux
- [Terminaux NuGet](https://www.nuget.org/packages/Terminaux) - v6.1.6, actively maintained (2025-02)
- [RadLine NuGet](https://www.nuget.org/packages/RadLine) - v0.9.0, preview only (2025-11)
- [FsReadLine GitHub](https://github.com/B2R2-org/FsReadLine) - F# native, no NuGet release

### Pattern References
- [Using FsLexYacc Tutorial](https://thanos.codes/blog/using-fslexyacc-the-fsharp-lexer-and-parser/) - Multiple rule state pattern
- [Wikipedia: String literal](https://en.wikipedia.org/wiki/String_literal) - Escape sequence conventions
- [Spectre.Console GitHub](https://github.com/spectreconsole/spectre.console) - Modern terminal library context
