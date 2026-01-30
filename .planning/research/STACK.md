# Stack Research: F# Language Implementation Ecosystem

## Executive Summary

For building a programming language interpreter tutorial in F# using fslex/fsyacc in 2025, the recommended stack focuses on **FsLexYacc 11.3.0** as the core parsing technology, with **.NET 8 LTS** as the runtime platform. This stack prioritizes educational clarity and follows traditional compiler construction pedagogy while acknowledging modern alternatives.

## Recommended Stack

### Core Platform
- **.NET SDK 8.0 (LTS)**
  - **Version**: 8.0.x (latest patch)
  - **Rationale**: Long-term support until November 2026, providing stability for tutorial longevity. While .NET 9 is available, it's an STS release with support only until May 2026.
  - **F# Version**: F# 8.0 (included with .NET 8)
  - **Confidence**: HIGH

### Lexer & Parser Generation
- **FsLexYacc**
  - **Version**: 11.3.0 (released April 8, 2024)
  - **NuGet Package**: `FsLexYacc`
  - **Runtime Package**: `FsLexYacc.Runtime 11.3.0`
  - **Rationale**:
    - Industry-standard yacc/lex paradigm familiar to students from Dragon Book and compiler courses
    - Educational value: teaches traditional parser generator concepts (shift-reduce, LALR)
    - Well-documented with official examples and community tutorials
    - Directly answers your requirement to use fslex/fsyacc specifically
  - **Installation**: `dotnet add package FsLexYacc`
  - **Confidence**: HIGH (for educational/tutorial purposes)

### AST & Interpreter Implementation
- **F# Discriminated Unions**
  - **Version**: Built-in language feature (F# 8.0)
  - **Rationale**:
    - Perfect fit for representing AST nodes
    - Pattern matching provides elegant evaluation logic
    - Compiler-verified exhaustive matching ensures completeness
    - Example: `type Expr = Num of int | Add of Expr * Expr | Var of string`
  - **Confidence**: ABSOLUTE

- **F# Immutable Collections**
  - **Version**: Built-in (FSharp.Core)
  - **Types**: `Map<string, 'T>` for symbol tables/environments
  - **Rationale**:
    - Immutable by default, simplifying scope management
    - `Map.add` for extending environments
    - Natural fit for functional programming pedagogy
  - **Confidence**: HIGH

### Testing Framework
- **Expecto**
  - **Version**: 10.x (latest stable)
  - **NuGet Package**: `Expecto`
  - **Rationale**:
    - Functional-first design, idiomatic F#
    - Tests as values that compose
    - Parallel execution by default
    - Better than xUnit/NUnit for F# learners
    - Simple, expressive syntax for interpreter testing
  - **Alternative**: xUnit 2.x (if integration with existing C# projects needed)
  - **Installation**: `dotnet add package Expecto`
  - **Confidence**: HIGH

### Property-Based Testing (Optional)
- **FsCheck**
  - **Version**: 2.x or 3.x
  - **NuGet Package**: `FsCheck`
  - **Rationale**:
    - Great for testing interpreter invariants
    - Works seamlessly with Expecto
    - Educational value for showing correctness properties
    - Example: `forAll <| fun (x, y) -> eval (Add(Num x, Num y)) = x + y`
  - **Confidence**: MEDIUM (optional but valuable)

### Project Structure
- **F# Project Files (.fsproj)**
  - **Build Tool**: dotnet CLI
  - **Structure Recommendation**:
    ```
    src/
      Lexer.fsl        # Lexer definition
      Parser.fsy       # Parser grammar
      Ast.fs           # AST types (discriminated unions)
      Eval.fs          # Interpreter/evaluator
      Program.fs       # REPL or main entry
    tests/
      Tests.fs         # Expecto tests
    ```
  - **File Ordering**: Critical in F# - files must be in dependency order
  - **Confidence**: HIGH

### Development Environment
- **F# Interactive (FSI)**
  - **Version**: Included with .NET SDK
  - **Command**: `dotnet fsi`
  - **Rationale**:
    - REPL-driven development for testing evaluator functions
    - Interactive exploration of AST transformations
    - Teaching tool for demonstrating interpreter behavior
  - **Confidence**: HIGH

- **IDE Support**:
  - **Visual Studio Code** with Ionide plugin (recommended for cross-platform)
  - **JetBrains Rider** (excellent F# support, paid)
  - **Visual Studio 2022** (Windows only)
  - **Confidence**: HIGH

## Why These Choices

### Educational Clarity Over Modern Convenience
While modern alternatives like FParsec and Farkle offer better ergonomics and performance, **FsLexYacc teaches fundamental compiler concepts**:
- Separation of lexical analysis (tokens) and syntactic analysis (grammar)
- Formal grammar notation (BNF-style)
- Shift-reduce parsing mechanics
- Conflict resolution (shift/reduce, reduce/reduce)

This aligns with traditional CS curriculum and compiler textbooks.

### F# Language Features for Interpreters
F#'s discriminated unions + pattern matching are **purpose-built for AST manipulation**:
- Algebraic data types represent language constructs naturally
- Exhaustive pattern matching catches missing cases at compile-time
- No null reference errors (unlike C#/Java interpreter implementations)
- Concise evaluation functions using recursive patterns

### Functional Programming Pedagogy
For F# developers learning interpreters:
- Immutable data structures simplify reasoning about state
- Pure functions for evaluation make testing straightforward
- Environment as immutable map teaches scope management
- Natural progression: arithmetic → variables → functions → closures

### .NET 8 LTS Stability
Tutorial content has longer shelf-life with LTS release:
- 3-year support window
- Stable API surface
- Mature tooling ecosystem
- Time to update tutorial before .NET 10 LTS (2026)

## What NOT to Use

### ❌ FParsec
- **Why NOT for this tutorial**:
  - Parser combinators are a different paradigm than yacc/lex
  - Hides the lexer/parser separation
  - Less pedagogical value for traditional compiler course concepts
  - Your requirement specifically mentions fslex/fsyacc
- **When to use instead**: Production parsers needing complex error messages or context-sensitive grammars
- **Confidence**: HIGH (don't use for fslex/fsyacc tutorial)

### ❌ Farkle
- **Why NOT for this tutorial**:
  - Modern alternative that combines parser generator + combinator approaches
  - Not widely known in academic/tutorial contexts
  - Smaller ecosystem and community compared to FsLexYacc
  - Doesn't align with fslex/fsyacc requirement
- **When to use instead**: New projects needing performance + composability
- **Confidence**: HIGH (don't use for fslex/fsyacc tutorial)

### ❌ .NET 9
- **Why NOT for tutorial**:
  - STS release with shorter support (until May 2026)
  - Tutorial longevity requires LTS stability
  - No compelling features for basic interpreter tutorial
  - F# 9 features not needed for teaching fundamentals
- **When to use instead**: Cutting-edge projects, short-term deployments
- **Confidence**: HIGH (stick with .NET 8 LTS)

### ❌ NUnit / xUnit
- **Why NOT as primary choice**:
  - C#-first design, less idiomatic for F#
  - Attribute-based (less functional)
  - Expecto is more aligned with F# philosophy
- **When to use instead**: Mixed C#/F# codebases, existing xUnit test suites
- **Confidence**: MEDIUM (Expecto is better, but xUnit works)

### ❌ Hand-Written Recursive Descent Parser
- **Why NOT for this tutorial**:
  - Misses the point of teaching parser generators
  - More code to maintain in tutorial
  - Doesn't teach yacc/lex ecosystem
- **When to use instead**: Simple grammars, maximum control, avoiding dependencies
- **Confidence**: HIGH (defeats purpose of fslex/fsyacc tutorial)

### ❌ Mutable Variables for Environment
- **Why NOT**:
  - Anti-pattern in F# (prefer immutable)
  - Complicates scope management
  - Harder to test and reason about
  - Bad example for F# learners
- **When to use instead**: Performance-critical interpreters (rare need)
- **Confidence**: ABSOLUTE (use immutable Map)

## Confidence Levels

| Component | Confidence | Notes |
|-----------|-----------|-------|
| .NET 8 LTS | ★★★★★ | Obvious choice for tutorial longevity |
| FsLexYacc 11.3.0 | ★★★★★ | Directly fulfills requirement |
| Discriminated Unions | ★★★★★ | Core F# feature, perfect fit |
| Immutable Map for Env | ★★★★★ | Idiomatic F# approach |
| Expecto Testing | ★★★★☆ | Best for F#, but xUnit viable too |
| FsCheck (optional) | ★★★☆☆ | Nice-to-have, not essential |
| F# Interactive (FSI) | ★★★★★ | Essential teaching/dev tool |
| VS Code + Ionide | ★★★★☆ | Most accessible cross-platform |

## Implementation Workflow

### 1. Project Setup
```bash
dotnet new console -lang F# -n LangTutorial
cd LangTutorial
dotnet add package FsLexYacc
dotnet add package FsLexYacc.Runtime
dotnet add package Expecto
```

### 2. Define AST (Ast.fs)
```fsharp
type Expr =
    | Num of int
    | Add of Expr * Expr
    | Mul of Expr * Expr
    | Var of string
```

### 3. Create Lexer (Lexer.fsl)
- Define tokens (INT, PLUS, MUL, ID, etc.)
- Specify regex patterns for each token
- Generate with: `fslex Lexer.fsl`

### 4. Create Parser (Parser.fsy)
- Define grammar rules in yacc syntax
- Specify precedence and associativity
- Generate with: `fsyacc Parser.fsy`

### 5. Implement Evaluator (Eval.fs)
```fsharp
let rec eval env = function
    | Num n -> n
    | Add(e1, e2) -> eval env e1 + eval env e2
    | Var x -> Map.find x env
```

### 6. Write Tests (Tests.fs)
```fsharp
open Expecto

[<Tests>]
let tests =
    testList "Evaluator" [
        test "simple addition" {
            let expr = Add(Num 2, Num 3)
            Expect.equal (eval Map.empty expr) 5 "2+3=5"
        }
    ]
```

## Additional Resources

### Official Documentation
- [FsLexYacc GitHub](https://github.com/fsprojects/FsLexYacc)
- [FsLexYacc Documentation](https://fsprojects.github.io/FsLexYacc/)
- [F# Language Reference](https://learn.microsoft.com/en-us/dotnet/fsharp/)
- [F# Interactive Reference](https://learn.microsoft.com/en-us/dotnet/fsharp/tools/fsharp-interactive/)

### Tutorials & Examples
- [Using FsLexYacc Tutorial](https://thanos.codes/blog/using-fslexyacc-the-fsharp-lexer-and-parser/)
- [JSON Parser Example with FsLexYacc](https://github.com/fsprojects/FsLexYacc/blob/master/docs/content/jsonParserExample.md)
- [F# for Fun and Profit - Discriminated Unions](https://fsharpforfunandprofit.com/posts/discriminated-unions/)
- [Crafting Interpreters - Resolving and Binding](https://craftinginterpreters.com/resolving-and-binding.html)

### Books
- "Programming Language Concepts" - Uses F# for interpreter implementation
- Dragon Book (Compilers: Principles, Techniques, and Tools) - Yacc/Lex theory

## Version Control Recommendations

### .gitignore Entries
```
# Generated lexer/parser files
Lexer.fs
Lexer.fsi
Parser.fs
Parser.fsi

# Or commit them if you want reproducible builds without FsLexYacc tools
```

**Decision**: For tutorials, **commit generated files** so learners can run code without build-time generation complexity. Clearly document which files are generated.

## Migration Path

If students want to evolve beyond FsLexYacc after completing tutorial:

1. **FParsec** - For production parsers with better error messages
2. **Farkle** - For performance-critical parsers with composition
3. **Hand-written** - For maximum control and custom error recovery

Tutorial should mention these alternatives in conclusion with pros/cons.

## Summary Table

| Need | Solution | Version | Priority |
|------|----------|---------|----------|
| Runtime Platform | .NET SDK | 8.0 LTS | Required |
| Lexer Generator | FsLexYacc | 11.3.0 | Required |
| Parser Generator | FsLexYacc | 11.3.0 | Required |
| AST Representation | Discriminated Unions | F# 8.0 | Required |
| Environment/Symbol Table | Immutable Map | FSharp.Core | Required |
| Testing Framework | Expecto | 10.x | Recommended |
| Property Testing | FsCheck | 2.x/3.x | Optional |
| REPL/Development | F# Interactive | Included | Required |
| IDE | VS Code + Ionide | Latest | Recommended |

## Final Recommendations

**For a tutorial teaching F# developers to build interpreters using fslex/fsyacc:**

✅ **Use**: .NET 8 LTS, FsLexYacc 11.3.0, discriminated unions, immutable Map, Expecto, FSI

❌ **Avoid**: FParsec, Farkle, .NET 9, mutable state, hand-written parsers

🎯 **Focus**: Educational clarity, traditional compiler concepts, idiomatic F# patterns

This stack provides the best balance of **pedagogical value**, **tooling maturity**, and **F# idioms** for your interpreter tutorial goals.
