# Phase 1: Foundation & Pipeline - Research

**Researched:** 2026-01-30
**Domain:** FsLexYacc lexer/parser generation with F# and .NET 10
**Confidence:** HIGH

## Summary

FsLexYacc is the standard lexer and parser generator for F# projects, providing fslex (lexer) and fsyacc (parser) tools that transform .fsl and .fsy specification files into F# source code. The latest stable version is 11.3.0, targeting .NETStandard 2.0 with full compatibility for .NET 10 projects.

The critical architectural insight is the build order dependency: despite lexing occurring before parsing at runtime, the parser must be defined first in the build pipeline. This is because fsyacc generates a token union type that the lexer depends on. The canonical file ordering is: AST type definitions → Parser.fsy → Lexer.fsl → generated Parser.fs → generated Lexer.fs → main program.

This phase establishes the foundation for subsequent language features by creating a working compilation pipeline where developers can modify .fsl and .fsy files and see the build system automatically regenerate the lexer and parser code.

**Primary recommendation:** Use FsLexYacc 11.3.0 with .NET 10, define AST types in a separate file (e.g., Ast.fs), configure .fsproj to process Parser.fsy before Lexer.fsl, and document the build order explicitly to avoid the most common pitfall in FsLexYacc projects.

## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| FsLexYacc | 11.3.0 | Lexer and parser generation | Official F# community project, only maintained lex/yacc tooling for F# |
| FsLexYacc.Runtime | 11.3.0 | Runtime support for generated lexer/parser | Required dependency for FsLexYacc 11.3.0 |
| .NET SDK | 10.0 | Runtime and tooling | Latest LTS release (requires Visual Studio 2026 for IDE support) |
| F# | 10.0+ | Language | Bundled with .NET 10 SDK |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Expecto | Latest | Testing framework | Phase 6 - defer until testing phase |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| FsLexYacc | FParsec (parser combinator) | FParsec better for complex grammars but tutorial focuses on lex/yacc approach |
| FsLexYacc | Hand-rolled recursive descent | Educational value but defeats purpose of tutorial |
| .NET 10 | .NET 8/9 | .NET 10 specified in requirements, 8/9 would work but miss latest features |

**Installation:**
```bash
# Create .NET 10 F# console project
dotnet new console -lang F# -f net10.0 -o FunLang
cd FunLang

# Install FsLexYacc
dotnet add package FsLexYacc --version 11.3.0
```

## Architecture Patterns

### Recommended Project Structure
```
FunLang/
├── Ast.fs                    # AST type definitions (Discriminated Unions)
├── Parser.fsy                # Parser specification
├── Lexer.fsl                 # Lexer specification
├── Program.fs                # Main entry point
├── FunLang.fsproj            # Project file with build configuration
└── obj/                      # Generated files (Parser.fs, Lexer.fs, etc.)
    └── Debug/
        └── net10.0/
            ├── Parser.fs     # Auto-generated from Parser.fsy
            ├── Parser.fsi    # Auto-generated signature file
            └── Lexer.fs      # Auto-generated from Lexer.fsl
```

### Pattern 1: Build Order Configuration
**What:** Configure .fsproj to process parser before lexer and enforce correct compilation order.

**When to use:** Always - this is mandatory for FsLexYacc projects.

**Example:**
```xml
<!-- Source: https://fsprojects.github.io/FsLexYacc/content/jsonParserExample.html -->
<ItemGroup>
  <!-- 1. AST definitions - manually written -->
  <Compile Include="Ast.fs" />

  <!-- 2. Parser generator input - generates Parser.fs and Parser.fsi -->
  <FsYacc Include="Parser.fsy">
    <OtherFlags>--module Parser</OtherFlags>
  </FsYacc>

  <!-- 3. Lexer generator input - generates Lexer.fs -->
  <FsLex Include="Lexer.fsl">
    <OtherFlags>--module Lexer --unicode</OtherFlags>
  </FsLex>

  <!-- 4. Generated parser files - referenced but not manually created -->
  <Compile Include="$(IntermediateOutputPath)Parser.fsi">
    <Link>Parser.fsi</Link>
  </Compile>
  <Compile Include="$(IntermediateOutputPath)Parser.fs">
    <Link>Parser.fs</Link>
  </Compile>

  <!-- 5. Generated lexer file -->
  <Compile Include="$(IntermediateOutputPath)Lexer.fs">
    <Link>Lexer.fs</Link>
  </Compile>

  <!-- 6. Main program - uses parser and lexer -->
  <Compile Include="Program.fs" />
</ItemGroup>
```

### Pattern 2: Parser File Structure (.fsy)
**What:** Define tokens, start symbol, and grammar rules in fsyacc format.

**When to use:** Every parser specification file.

**Example:**
```fsharp
// Source: https://github.com/fsprojects/FsLexYacc/blob/master/docs/content/fsyacc.md
%{
// Header section - open modules and define helper functions
open Ast
%}

// Token declarations
%token <int> INT
%token <string> ID
%token PLUS MINUS EOF

// Start symbol and its return type
%start expression
%type <Ast.Expr> expression

%%

// Grammar rules
expression:
  | INT           { Number $1 }
  | ID            { Variable $1 }
  | expression PLUS expression   { Add($1, $3) }
  | expression MINUS expression  { Subtract($1, $3) }
```

### Pattern 3: Lexer File Structure (.fsl)
**What:** Define token patterns and lexing rules in fslex format.

**When to use:** Every lexer specification file.

**Example:**
```fsharp
// Source: https://github.com/fsprojects/FsLexYacc/blob/master/docs/content/fslex.md
{
// Header section - open Parser module to access token types
open System
open Parser
}

// Regular expression definitions
let digit = ['0'-'9']
let whitespace = [' ' '\t']
let newline = ('\n' | '\r' '\n')

// Lexing rules
rule tokenize = parse
  | whitespace  { tokenize lexbuf }
  | newline     { tokenize lexbuf }
  | digit+      { INT (Int32.Parse(LexBuffer<_>.LexemeString lexbuf)) }
  | '+'         { PLUS }
  | '-'         { MINUS }
  | eof         { EOF }
```

### Pattern 4: AST Definition with Discriminated Unions
**What:** Define the abstract syntax tree using F# discriminated unions.

**When to use:** Always - before defining parser/lexer.

**Example:**
```fsharp
// Source: https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/discriminated-unions
module Ast

// Simple expression AST
type Expr =
    | Number of int
    | Variable of string
    | Add of Expr * Expr
    | Subtract of Expr * Expr
    | Multiply of Expr * Expr
    | Divide of Expr * Expr

// For complex AST with statements, use mutual recursion
type Expr =
    | Literal of int
    | BinOp of string * Expr * Expr
and Statement =
    | Assign of string * Expr
    | Sequence of Statement list
```

### Anti-Patterns to Avoid
- **Lexer before Parser in .fsproj:** Causes compilation errors because Lexer.fs references token types not yet generated from Parser.fsy
- **Manual creation of Parser.fs/Lexer.fs:** These are auto-generated; creating them manually causes conflicts
- **Using F# keywords as non-terminals:** Keywords like `type`, `let`, `match` cannot be used as grammar rule names - use `typ`, `letExpr`, etc.
- **Mixing FsLexYacc.Runtime with FSPowerPack:** Creates duplicate LexBuffer type definitions and build failures
- **Relative paths in .fsproj:** Use $(IntermediateOutputPath) for generated files, not hardcoded paths

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Lexical analysis | Manual character-by-character scanner | fslex with .fsl specification | Handles Unicode, line tracking, lexbuf management, error positions automatically |
| Parser generation | Recursive descent parser | fsyacc with .fsy specification | Handles precedence, associativity, LALR(1) conflicts, reduces boilerplate |
| Token type definition | Manual token enum | fsyacc-generated token union | Ensures type safety between parser and lexer, automatic updates when grammar changes |
| AST pattern matching | Manual type checking with if/else | Discriminated unions with match expressions | Compiler-enforced exhaustiveness checking, cleaner code |

**Key insight:** FsLexYacc generates thousands of lines of boilerplate code including LexBuffer management, parse tables, error recovery, and token type definitions. The generated code is thoroughly tested and optimized. Hand-rolling these components would take weeks and introduce bugs that FsLexYacc has already solved.

## Common Pitfalls

### Pitfall 1: Wrong Build Order
**What goes wrong:** Compilation fails with "token type not found" or "Parser module does not exist" errors.

**Why it happens:** The lexer file opens the Parser module to access token types (INT, PLUS, EOF, etc.), but if Parser.fsy is processed after Lexer.fsl, those types don't exist yet.

**How to avoid:** In .fsproj, always list `<FsYacc Include="Parser.fsy">` before `<FsLex Include="Lexer.fsl">`. Document this requirement prominently.

**Warning signs:**
- Build errors mentioning undefined tokens
- Lexer.fs compilation failures about missing Parser module
- Errors disappear after manual deletion of obj folder and rebuild

### Pitfall 2: Missing Generated File References
**What goes wrong:** Compilation succeeds but generated files are not used, or IDE shows red squiggles.

**Why it happens:** The .fsproj must explicitly include the generated .fs files from $(IntermediateOutputPath) for the compiler to see them.

**How to avoid:** Include generated files with Link elements:
```xml
<Compile Include="$(IntermediateOutputPath)Parser.fs">
  <Link>Parser.fs</Link>
</Compile>
```

**Warning signs:**
- Parser.fs and Lexer.fs exist in obj/ but are not visible in IDE
- "module not found" errors despite successful code generation

### Pitfall 3: F# Compilation Order Confusion
**What goes wrong:** Files compile in wrong order even after fixing .fsproj, causing forward reference errors.

**Why it happens:** F# requires strict ordering - files can only reference code from files listed earlier in the .fsproj `<ItemGroup>`.

**How to avoid:** Order ALL files correctly: Ast.fs → Parser.fsi → Parser.fs → Lexer.fs → Program.fs

**Warning signs:**
- Errors about undefined types that clearly exist in another file
- Moving file up/down in .fsproj changes whether code compiles

### Pitfall 4: .NET 10 Tooling Incompatibility
**What goes wrong:** Visual Studio 2022 refuses to open project or shows errors for .NET 10 projects.

**Why it happens:** Visual Studio 2022 does not support .NET 10 - only Visual Studio 2026 does.

**How to avoid:** Either install Visual Studio 2026, or use `dotnet build` and `dotnet run` from CLI with any editor.

**Warning signs:**
- "This version of Visual Studio does not support .NET 10" message
- Project file loads but all code shows red underlines

### Pitfall 5: Forgetting --module Flag
**What goes wrong:** Generated code doesn't use proper module names, causing namespace collisions.

**Why it happens:** Without `--module` flag, fslex/fsyacc generate code in default namespace.

**How to avoid:** Always specify module name:
```xml
<FsYacc Include="Parser.fsy">
  <OtherFlags>--module Parser</OtherFlags>
</FsYacc>
```

**Warning signs:**
- Generated code lacks module declaration at top
- Conflicts with other generated files

## Code Examples

Verified patterns from official sources:

### Complete Minimal Parser
```fsharp
// Source: https://github.com/fsprojects/FsLexYacc/blob/master/tests/LexAndYaccMiniProject/Parser.fsy
%{
open Ast
%}

%token <int> NUMBER
%token EOF
%start start
%type <int> start

%%

start: NUMBER { $1 }
```

### Complete Minimal Lexer
```fsharp
// Source: https://github.com/fsprojects/FsLexYacc/blob/master/docs/content/fslex.md
{
open Parser
}

let digit = ['0'-'9']

rule tokenize = parse
  | digit+  { NUMBER (System.Int32.Parse(LexBuffer<_>.LexemeString lexbuf)) }
  | eof     { EOF }
```

### Calling Parser from Main Program
```fsharp
// Source: https://fsprojects.github.io/FsLexYacc/content/jsonParserExample.html
open System
open Microsoft.FSharp.Text.Lexing

[<EntryPoint>]
let main argv =
    let input = "42"
    let lexbuf = LexBuffer<char>.FromString input

    try
        let result = Parser.start Lexer.tokenize lexbuf
        printfn "Parsed: %A" result
        0
    with e ->
        printfn "Error: %s" e.Message
        1
```

### AST with Pattern Matching
```fsharp
// Source: https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/discriminated-unions
module Ast

type Expr =
    | Number of int
    | Add of Expr * Expr

let rec eval expr =
    match expr with
    | Number n -> n
    | Add(left, right) -> eval left + eval right

// Usage
let ast = Add(Number 1, Number 2)
let result = eval ast  // returns 3
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| FSPowerPack.Compiler.CodeDom | FsLexYacc (fsprojects) | ~2014 | FSPowerPack deprecated, FsLexYacc is maintained |
| .NET Framework only | .NETStandard 2.0 | v11.0.0 (Jan 2022) | Cross-platform support, works on Linux/macOS |
| Async lexing APIs | Synchronous only | v9.1.0 (Oct 2019) | Async lexing marked obsolete |
| Tools as .exe | Tools as .dll | v9.0.1 (Apr 2019) | .NET Core compatibility |
| Manual .fsproj config | NuGet MSBuild targets | ~v7.0 | Automatic task registration |

**Deprecated/outdated:**
- **FSPowerPack**: Obsolete, causes LexBuffer type conflicts with FsLexYacc.Runtime
- **Async lexing APIs**: Marked obsolete in 9.1.0, do not use
- **Hard-coded paths to fslex.exe/fsyacc.exe**: Use NuGet package which registers MSBuild tasks automatically

## Open Questions

Things that couldn't be fully resolved:

1. **Error recovery in parsers**
   - What we know: FsYacc supports error recovery but documentation is sparse (issue #67)
   - What's unclear: Best practices for implementing robust error recovery
   - Recommendation: Defer to later phase, start with basic parsing without error recovery

2. **Signature file generation**
   - What we know: FsYacc can generate .fsi files with --module flag
   - What's unclear: Whether to include Parser.fsi in compilation order or omit it
   - Recommendation: Test both approaches; JSON example includes it after Parser.fsy

3. **.NET 10 final release timing**
   - What we know: .NET 10 preview requires Visual Studio 2026
   - What's unclear: Exact release date for .NET 10 stable and VS 2026 stable
   - Recommendation: Document that users need VS 2026 or CLI-only workflow

## Sources

### Primary (HIGH confidence)
- [FsLexYacc official docs](https://fsprojects.github.io/FsLexYacc/) - Installation, usage, examples
- [JSON Parser Example](https://fsprojects.github.io/FsLexYacc/content/jsonParserExample.html) - Complete .fsproj configuration
- [FsLex documentation](https://github.com/fsprojects/FsLexYacc/blob/master/docs/content/fslex.md) - Lexer file structure
- [FsYacc documentation](https://github.com/fsprojects/FsLexYacc/blob/master/docs/content/fsyacc.md) - Parser file structure
- [Microsoft: Discriminated Unions](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/discriminated-unions) - AST patterns
- [FsLexYacc NuGet](https://www.nuget.org/packages/FsLexYacc/) - Version 11.3.0 details
- [FsLexYacc Release Notes](https://github.com/fsprojects/FsLexYacc/blob/master/RELEASE_NOTES.md) - Version history

### Secondary (MEDIUM confidence)
- [Using FSLexYacc tutorial](https://thanos.codes/blog/using-fslexyacc-the-fsharp-lexer-and-parser/) - Build order explanation, verified against official docs
- [F# file order article](https://dev.to/klimcio/file-order-in-f-the-most-annoying-thing-for-a-beginner-38dc) - Compilation order requirements
- [.NET 10 VS 2026 requirement](https://en.ittrip.xyz/c-sharp/net10-preview-vs-windows) - Visual Studio 2026 needed for .NET 10

### Tertiary (LOW confidence)
- [WebSearch: FsLexYacc common errors](https://github.com/fsprojects/FsLexYacc/issues) - GitHub issues for pitfalls
- [Microsoft: dotnet new](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new) - Project creation

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - FsLexYacc 11.3.0 verified from official NuGet and release notes
- Architecture: HIGH - Patterns extracted from official examples and documentation
- Pitfalls: HIGH - Build order documented in multiple official sources, errors verified in GitHub issues

**Research date:** 2026-01-30
**Valid until:** 2026-03-01 (30 days - stable technology, unlikely to change)
