# Phase 3: REPL (CLI Modernization + Interactive Shell) - Research

**Researched:** 2026-02-01
**Domain:** CLI argument parsing (Argu) + REPL loop implementation (F#)
**Confidence:** HIGH

## Summary

This phase combines two related tasks: modernizing the CLI argument parsing using the Argu library and adding an interactive REPL (Read-Eval-Print Loop). The research focused on:

1. **Argu CLI Parsing** - Declarative argument parsing using F# discriminated unions
2. **REPL Implementation** - Idiomatic F# patterns for interactive loops with state persistence

The standard approach is to define CLI arguments as a discriminated union implementing `IArgParserTemplate`, then use `ArgumentParser.Create<T>()` to parse command-line arguments. For the REPL, use a recursive loop with `try-with` for error recovery and `Console.ReadLine()` with null check for EOF detection.

**Primary recommendation:** Use Argu 6.2.5 with `MainCommand` for positional file argument, `AltCommandLine` for `-e` alias, and implement REPL as a recursive function with environment threading.

## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Argu | 6.2.5 | CLI argument parsing | De facto F# CLI parser, declarative DU-based, auto-generates help |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| System.Console | .NET 10 | REPL I/O | Always - Console.ReadLine() for input, Console.Write for prompt |
| FSharp.Core | 10.0 | Pattern matching, recursion | Already included in project |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Argu | Manual pattern matching | Current approach - works but verbose, no auto-help |
| Argu | CommandLineParser.FSharp | Less idiomatic F#, more C# oriented |
| Argu | System.CommandLine | Microsoft's new library - more complex, less F# integration |

**Installation:**
```bash
# Add to FunLang.fsproj
dotnet add package Argu --version 6.2.5
```

Or add to .fsproj directly:
```xml
<PackageReference Include="Argu" Version="6.2.5" />
```

## Architecture Patterns

### Recommended Project Structure
```
FunLang/
├── Cli.fs           # NEW: Argu argument definitions
├── Repl.fs          # NEW: REPL loop implementation
└── Program.fs       # Refactored: dispatch based on parsed args
```

**Alternative (simpler):** Keep all in Program.fs since the code is ~50-60 LOC total.

### Pattern 1: Declarative CLI with Argu
**What:** Define CLI arguments as a discriminated union with attributes
**When to use:** Any F# CLI application
**Example:**
```fsharp
// Source: https://fsprojects.github.io/Argu/tutorial.html
open Argu

type CliArgs =
    | [<AltCommandLine("-e")>] Expr of expression: string
    | [<CustomCommandLine("--emit-tokens")>] Emit_Tokens
    | [<CustomCommandLine("--emit-ast")>] Emit_Ast
    | [<CustomCommandLine("--emit-type")>] Emit_Type
    | Repl
    | [<MainCommand; Last>] File of filename: string
with
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Expr _ -> "evaluate expression"
            | Emit_Tokens -> "show lexer tokens"
            | Emit_Ast -> "show parsed AST"
            | Emit_Type -> "show inferred types (reserved)"
            | Repl -> "start interactive REPL"
            | File _ -> "evaluate program from file"
```

### Pattern 2: REPL with Environment Persistence
**What:** Recursive loop that threads environment through iterations
**When to use:** Interactive interpreters needing state persistence
**Example:**
```fsharp
// Source: F# functional patterns
let rec replLoop (env: Env) : unit =
    Console.Write "funlang> "
    match Console.ReadLine() with
    | null -> ()  // EOF (Ctrl+D)
    | "exit" -> ()
    | line ->
        try
            let (newEnv, result) = evalWithEnv env line
            printfn "%s" (formatValue result)
            replLoop newEnv
        with ex ->
            eprintfn "Error: %s" ex.Message
            replLoop env  // Continue with unchanged env
```

### Pattern 3: Parser Error Handling
**What:** Use ProcessExiter for graceful error handling
**When to use:** CLI tools that should exit cleanly on bad arguments
**Example:**
```fsharp
// Source: https://chester.codes/easy-clis-with-fsharp-and-dotnet-tools/
let errorHandler =
    ProcessExiter(colorizer = function
        | ErrorCode.HelpText -> None
        | _ -> Some ConsoleColor.Red)

let parser = ArgumentParser.Create<CliArgs>(
    programName = "funlang",
    errorHandler = errorHandler)
```

### Anti-Patterns to Avoid
- **Mutable environment in REPL:** Don't use `let mutable env = ...`. Thread environment through recursive calls.
- **Ignoring null from ReadLine:** Always check for null - it indicates EOF (Ctrl+D).
- **Catching all exceptions:** Only catch recoverable errors in REPL. Let fatal errors propagate.

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| CLI parsing | Manual pattern matching on argv | Argu | Auto-help, validation, error messages, maintainability |
| Help text generation | Manual printfn "Usage: ..." | Argu's IArgParserTemplate | Keeps docs in sync with code |
| Argument validation | Manual checks for file exists | Argu's ExistsFile attribute | Declarative, consistent errors |

**Key insight:** The current 120-line Program.fs with manual pattern matching will reduce to ~60 lines with Argu while gaining auto-generated help, proper error messages, and easier extensibility.

## Common Pitfalls

### Pitfall 1: Underscore-to-Hyphen Conversion in Argu
**What goes wrong:** `Emit_Tokens` becomes `--emit-tokens` automatically
**Why it happens:** Argu converts underscores to hyphens by default for CLI readability
**How to avoid:** Either accept the conversion (recommended) or use `[<CustomCommandLine("--emit-tokens")>]` to override
**Warning signs:** Tests fail because CLI flags don't match expected names

### Pitfall 2: MainCommand Must Be Last
**What goes wrong:** Positional file argument consumes flags
**Why it happens:** MainCommand greedily captures unrecognized arguments
**How to avoid:** Add `[<Last>]` attribute alongside `[<MainCommand>]`
**Warning signs:** File argument captures `--repl` or other flags

### Pitfall 3: REPL Environment Reset on Error
**What goes wrong:** `let x = 5` followed by error loses x binding
**Why it happens:** Exception handling creates new scope, doesn't return new env
**How to avoid:** Always pass previous env to next iteration, regardless of success/failure
**Warning signs:** "Undefined variable" after recovery from error

### Pitfall 4: Missing EOF Handling
**What goes wrong:** REPL crashes or infinite loops on Ctrl+D
**Why it happens:** Console.ReadLine() returns null on EOF, not empty string
**How to avoid:** Pattern match on `null` as first case: `| null -> ()`
**Warning signs:** Crash on Ctrl+D, or loop printing empty prompts

### Pitfall 5: Let-Expression vs Statement Semantics
**What goes wrong:** `let x = 5` returns unit, user expects 5
**Why it happens:** FunLang let requires `in` body - `let x = 5 in x`
**How to avoid:** Either require full let-in syntax in REPL, or add statement-level let as new feature
**Warning signs:** Confusion about why `let x = 5` doesn't show value

**Recommendation for FunLang:** Keep existing `let ... in ...` syntax. REPL will show `()` or similar for let bindings (when we add unit type) or require complete expressions.

## Code Examples

Verified patterns from official sources:

### Complete Argu Argument Type
```fsharp
// Source: Argu tutorial + adapted for FunLang
open Argu

[<CliPrefix(CliPrefix.DoubleDash)>]
type CliArgs =
    | [<AltCommandLine("-e")>] Expr of expression: string
    | Emit_Tokens
    | Emit_Ast
    | Emit_Type
    | Repl
    | [<AltCommandLine("-h")>] Help
    | [<MainCommand; Last>] File of filename: string
with
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Expr _ -> "evaluate expression"
            | Emit_Tokens -> "show lexer tokens"
            | Emit_Ast -> "show parsed AST"
            | Emit_Type -> "show inferred types (reserved)"
            | Repl -> "start interactive REPL"
            | Help -> "show this help message"
            | File _ -> "evaluate program from file"
```

### Parser Creation and Usage
```fsharp
// Source: Argu documentation
[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<CliArgs>(programName = "funlang")

    try
        let results = parser.ParseCommandLine(argv)

        // Priority: --help > --repl > --emit-* > --expr > file > default repl
        if results.Contains Help then
            printfn "%s" (parser.PrintUsage())
            0
        elif results.Contains Repl then
            startRepl()
            0
        elif results.Contains Emit_Tokens then
            // ... handle emit tokens
            0
        // ... other cases
        else
            // No args -> start REPL
            startRepl()
            0
    with
    | :? ArguParseException as ex ->
        eprintfn "%s" ex.Message
        1
```

### REPL with Environment Threading
```fsharp
// Source: F# functional patterns
open System

/// Evaluate expression and return (updated env, result)
let evalWithEnv (env: Env) (input: string) : Env * Value =
    let ast = parse input
    // For now, eval doesn't update env (let ... in ... is self-contained)
    // Future: could extract top-level let bindings
    let result = eval env ast
    (env, result)

/// Run REPL loop
let rec replLoop (env: Env) : unit =
    Console.Write "funlang> "
    Console.Out.Flush()  // Ensure prompt appears before ReadLine

    match Console.ReadLine() with
    | null ->
        printfn ""  // Newline after Ctrl+D
    | "exit" ->
        ()
    | "" ->
        replLoop env  // Empty line, just continue
    | line ->
        try
            let (newEnv, result) = evalWithEnv env line
            printfn "%s" (formatValue result)
            replLoop newEnv
        with ex ->
            eprintfn "Error: %s" ex.Message
            replLoop env  // Continue with unchanged env

/// Start REPL with welcome message
let startRepl () =
    printfn "FunLang REPL v2.0"
    printfn "Type 'exit' or Ctrl+D to quit."
    printfn ""
    replLoop emptyEnv
```

### Unit Type Consideration
```fsharp
// Current: No unit type, let expressions require in-body
// Future consideration: Add Unit value
type Value =
    | IntValue of int
    | BoolValue of bool
    | FunctionValue of param: string * body: Expr * closure: Env
    | StringValue of string
    | UnitValue  // Could add for REPL "let x = 5" without in-body

// For now, require complete expressions in REPL
// "let x = 5 in x + 1" evaluates to 6
// Bare "let x = 5" is a syntax error (no in-body)
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Manual argv matching | Argu declarative | Argu stable since 2020 | Reduces boilerplate, adds auto-help |
| Mutable REPL state | Recursive with env threading | F# idiom | Cleaner, easier to reason about |
| `while true` loops | Recursive functions | F# idiom | More functional, easier termination |

**Deprecated/outdated:**
- UnionArgParser (old name): Renamed to Argu in 2015
- CommandLineParser: Still works but less F# idiomatic than Argu

## Open Questions

Things that couldn't be fully resolved:

1. **Environment persistence semantics**
   - What we know: Current FunLang requires `let x = 5 in body`
   - What's unclear: Should REPL support bare `let x = 5` with implicit persistence?
   - Recommendation: For v2.0, require full `let ... in ...` syntax. Future phase could add statement-level let.

2. **Multi-line input**
   - What we know: F# Interactive uses `;;` to end multi-line input
   - What's unclear: Should FunLang REPL support multi-line expressions?
   - Recommendation: For v2.0, single-line only. Keep it simple.

3. **REPL testing strategy**
   - What we know: fslit tests work for CLI, but REPL is interactive
   - What's unclear: How to test REPL automatically?
   - Recommendation: Unit test the replLoop with mock input. Test CLI `--repl` flag exists. Manual testing for interactive behavior.

## Sources

### Primary (HIGH confidence)
- [Argu Official Documentation](https://fsprojects.github.io/Argu/) - Attribute reference, tutorial
- [Argu Tutorial](https://fsprojects.github.io/Argu/tutorial.html) - MainCommand, AltCommandLine, parsing patterns
- [Argu NuGet](https://www.nuget.org/packages/Argu) - Version 6.2.5 confirmed as latest

### Secondary (MEDIUM confidence)
- [Console.ReadLine Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.console.readline) - EOF returns null
- [F# Exception Handling](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/exception-handling/) - try-with patterns
- [Matthew Manela - Functional Console Loop](https://matthewmanela.com/blog/a-functional-take-on-console-program-loop-in-f/) - Recursive vs while patterns

### Tertiary (LOW confidence)
- [chester.codes - Easy CLI with Argu](https://chester.codes/easy-clis-with-fsharp-and-dotnet-tools/) - Practical example (blog post)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Argu is well-documented, stable, widely used
- Architecture: HIGH - F# REPL patterns are well-established
- Pitfalls: HIGH - Common issues are documented in Argu and F# community

**Research date:** 2026-02-01
**Valid until:** 60 days (Argu is stable, ~1 release/year)
