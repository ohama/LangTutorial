# Project Research Summary

**Project:** F# Language Implementation Tutorial (LangTutorial)
**Domain:** Programming language interpreter/compiler tutorial using fslex/fsyacc
**Researched:** 2026-01-30
**Confidence:** HIGH

## Executive Summary

This project is an educational interpreter tutorial teaching F# developers how to build programming language interpreters using fslex (lexical analyzer generator) and fsyacc (parser generator). Expert implementations follow a classic multi-stage pipeline: lexer tokenizes source code, parser builds an Abstract Syntax Tree (AST), and an evaluator executes the AST using tree-walking interpretation. The recommended approach prioritizes educational clarity over performance, using .NET 8 LTS with FsLexYacc 11.3.0, leveraging F#'s discriminated unions and pattern matching for AST representation and evaluation.

The critical success factor is incremental tutorial design: each chapter must add exactly ONE major feature and produce working, runnable code. The primary risk is overwhelming learners with complexity, which can be mitigated by starting with a minimal arithmetic calculator and progressively adding variables, statements, control flow, and functions. fslex/fsyacc-specific pitfalls include build order dependencies (parser must generate token types before lexer), shift-reduce conflicts in grammar design, and weak default error messages.

The recommended phase structure follows compiler construction pedagogy: foundation (lexer, parser, basic evaluation), state management (variables, environments), control flow (conditionals, loops), and abstraction (functions, closures). This structure maps directly to traditional CS curriculum, making it valuable for both self-learners and classroom use.

## Key Findings

### Recommended Stack

The stack centers on FsLexYacc 11.3.0 running on .NET 8 LTS, chosen specifically for educational clarity and alignment with traditional compiler construction pedagogy. While modern alternatives like FParsec offer better ergonomics, they hide the lexer/parser separation that's fundamental to understanding how compilers work. The fslex/fsyacc toolchain teaches shift-reduce parsing, grammar conflict resolution, and formal language theory in ways that parser combinators do not.

**Core technologies:**
- **.NET SDK 8.0 LTS**: Long-term support until November 2026, providing tutorial longevity and stable tooling
- **FsLexYacc 11.3.0**: Industry-standard yacc/lex paradigm, teaches traditional parser generator concepts (LALR parsing, shift-reduce conflicts)
- **F# Discriminated Unions**: Perfect fit for AST representation with compiler-verified exhaustive pattern matching
- **Immutable Map**: Built-in FSharp.Core type for symbol tables and environment management, idiomatic functional approach
- **Expecto 10.x**: Functional-first testing framework more aligned with F# philosophy than xUnit/NUnit

**Critical version constraint:** .NET 8 LTS (not .NET 9 STS) ensures tutorial remains valid through 2026 without obsolescence.

### Expected Features

The tutorial must balance educational completeness with manageable complexity. Research shows successful interpreter tutorials build progressively from simple arithmetic to Turing-complete languages.

**Must have (table stakes):**
- Lexical analysis (tokenization with fslex) — foundation of all language processing
- Parsing (grammar definition with fsyacc, AST construction) — core compiler concept
- Arithmetic expressions with precedence — immediate gratification, establishes testing
- Variables and environment management — introduces state and scope
- Statements vs expressions distinction — fundamental language design concept
- Control flow (if/then/else) — enables branching logic
- Functions with parameters and return values — code reuse and abstraction
- Progressive complexity (one feature per chapter) — pedagogical soundness
- Comprehensive error handling — professional quality, not optional

**Should have (differentiators):**
- Closures and lexical scoping — separates toy from professional implementations
- First-class functions — especially relevant for F# audience
- Interactive REPL — professional polish, teaches interactive systems
- Visual AST representations — makes abstract concepts concrete
- Debugging the interpreter itself — practical skill rarely covered
- Comprehensive error messages with suggestions — modern language feature
- Testing examples (unit and integration) — validates learning

**Defer (v2+):**
- Static type checking and type inference — massive complexity increase, deserves separate tutorial
- Garbage collection implementation — implementation-heavy, F# handles memory
- Bytecode compilation and VM — different tutorial focus (tree-walking interpreter is simpler)
- Module system and imports — adds file I/O complexity
- Concurrency primitives — highly complex, far beyond beginner scope
- Object-oriented features — focus on functional style aligns with F#

### Architecture Approach

The architecture follows a classic compiler pipeline where each component has a single, well-defined responsibility. The lexer (Lexer.fsl) converts raw source text into tokens, the parser (Parser.fsy) validates syntax and constructs an AST using F# discriminated unions, and the evaluator recursively walks the AST to produce values. This separation of concerns is both pedagogically sound and makes testing straightforward.

**Major components:**
1. **Lexer (Lexer.fsl)** — Tokenizes source code using regex patterns, filters whitespace, reports lexical errors
2. **Parser (Parser.fsy)** — Validates token sequences against grammar rules, constructs immutable AST, handles precedence/associativity
3. **AST (Ast.fs)** — Discriminated unions representing language constructs (expressions, statements, operators)
4. **Values (Values.fs)** — Runtime value types (integers, booleans, functions) with operations
5. **Environment (Environment.fs)** — Immutable variable bindings using Map, handles scope management
6. **Evaluator (Evaluator.fs)** — Tree-walking interpreter using pattern matching, produces values and updated environments
7. **REPL (Repl.fs)** — Interactive shell maintaining session state across inputs
8. **Error Handling (Errors.fs)** — Custom exception types for lexical, syntax, and runtime errors with position tracking

**Critical build constraint:** F# requires files in dependency order, but fsyacc must run before fslex (parser generates token types that lexer imports). Project structure must be: Ast.fs → Values.fs → Parser.fsy (generates Parser.fs) → Lexer.fsl (generates Lexer.fs) → Environment.fs → Evaluator.fs → Main.fs.

### Critical Pitfalls

1. **Build order dependencies** — Parser must be defined before lexer because fsyacc generates token type definitions that fslex needs. Solution: Always generate Parser.fs before Lexer.fs, document this in setup chapter, show complete .fsproj file with correct ordering.

2. **Non-incremental tutorial structure** — Requiring students to write large code blocks before seeing results, or breaking working code between chapters. Solution: Every chapter must produce runnable code, each chapter adds ONE feature, provide complete working code at each step.

3. **Shift-reduce and reduce-reduce conflicts** — fsyacc reports grammar conflicts that are difficult for beginners to understand. Solution: Start with conflict-free grammars, use precedence declarations (%left, %right) correctly, provide debugging techniques before complex expressions.

4. **Mega interpreter anti-pattern** — Building one giant interpreter instead of modular components. Solution: Separate lexer, parser, and evaluator from chapter 1, use visitor pattern via pattern matching, maintain clear component boundaries.

5. **Insufficient error handling** — Only showing happy path without error cases. Solution: Include error examples in each chapter, provide helpful error messages with position tracking, test with intentionally broken code.

## Implications for Roadmap

Based on research, suggested phase structure follows traditional compiler construction pedagogy with modern tutorial best practices:

### Phase 1: Foundation (Lexer, Parser, AST)
**Rationale:** Establishes the pipeline architecture and tooling before adding language features. Students must understand tokenization and parsing before they can evaluate anything. This phase maps to traditional compiler course structure.
**Delivers:** Working lexer that tokenizes arithmetic expressions, parser that builds AST from tokens, AST type definitions using discriminated unions.
**Addresses:** Lexical analysis (table stakes), parsing (table stakes), AST representation (architecture foundation)
**Avoids:** Build order dependencies (critical pitfall #1), confusing lexer/parser responsibilities, complex grammar design too early

### Phase 2: Expression Evaluation
**Rationale:** Once AST exists, evaluation is straightforward and provides immediate gratification. Arithmetic calculator is simplest meaningful interpreter. Enables testing infrastructure.
**Delivers:** Tree-walking evaluator for arithmetic expressions, basic value types, working calculator that computes results.
**Uses:** F# pattern matching for recursive evaluation, discriminated unions for type-safe AST traversal
**Implements:** Evaluator component, Values component
**Avoids:** Undefined behavior (critical pitfall), rushing to complex features

### Phase 3: Variables and Environment
**Rationale:** Variables introduce state management, the next conceptual leap. Requires environment component. Natural progression from pure expressions to stateful computation.
**Delivers:** Variable declaration and lookup, immutable Map-based environment, scope management foundation.
**Addresses:** Variables (table stakes), environment management (architecture component)
**Avoids:** Mutable state anti-pattern, global variables, scope confusion

### Phase 4: Statements and Side Effects
**Rationale:** Separates expressions (return values) from statements (side effects like print). Essential language design concept. Enables multi-statement programs.
**Delivers:** Statement execution, print statements, statement sequences, program structure.
**Addresses:** Statements vs expressions (table stakes), side effects
**Avoids:** Mega interpreter anti-pattern by keeping statement and expression evaluation separate

### Phase 5: Control Flow
**Rationale:** Conditionals make the language useful for real logic. Requires boolean values and comparison operators. With loops or recursion (added later), makes language Turing-complete.
**Delivers:** If/then/else expressions, boolean values and operators, comparison operators.
**Addresses:** Control flow (table stakes), boolean expressions
**Implements:** Extended value types, conditional evaluation logic

### Phase 6: Functions and Abstraction
**Rationale:** Functions are the key abstraction mechanism. Introduces function definitions, calls, parameters, return values, and local scope. Major conceptual milestone.
**Delivers:** Function definitions and calls, parameter passing, local scopes, call stack management (implicit in recursion).
**Addresses:** Functions (table stakes), code reuse and abstraction
**Avoids:** Scope management pitfalls by using immutable environments

### Phase 7: Closures and Advanced Features
**Rationale:** Closures demonstrate first-class functions and lexical scoping, differentiating features that separate toy from professional implementations. Especially relevant for F# audience.
**Delivers:** Nested functions, closure representation capturing environments, first-class function values.
**Addresses:** Closures and lexical scoping (differentiator), first-class functions (differentiator)
**Implements:** Function values with captured environments

### Phase 8: Error Handling and REPL
**Rationale:** Professional polish that makes the tutorial production-quality. Error handling was deferred from earlier phases but is now comprehensive. REPL demonstrates interactive systems.
**Delivers:** Custom error types, position tracking, helpful error messages, interactive REPL with multi-line input.
**Addresses:** Error handling (table stakes), interactive REPL (differentiator), comprehensive error messages (differentiator)
**Avoids:** Insufficient error handling (critical pitfall #5), weak error messages

### Phase Ordering Rationale

- **Phases 1-2 are sequential and foundational**: Cannot evaluate without parsing, cannot parse without tokenizing. This is the compiler pipeline.
- **Phase 3 builds on Phase 2**: Variables require working evaluation infrastructure.
- **Phases 4-5 are independent of each other**: Could be swapped, but statements before control flow feels more natural pedagogically.
- **Phase 6 depends on Phases 3-5**: Functions need variables, statements, and control flow to be meaningful.
- **Phase 7 extends Phase 6**: Closures are advanced function features.
- **Phase 8 integrates everything**: Error handling and REPL touch all previous phases.

This ordering avoids non-incremental structure (critical pitfall #2) by ensuring each phase produces working code. It avoids tutorial fever (pitfall) by maintaining one-feature-per-phase discipline. It follows architecture best practices by establishing component separation early (Phase 1).

### Research Flags

Phases likely needing deeper research during planning:
- **Phase 3 (Variables/Environment):** Scope management strategies (simple vs nested scopes), immutable vs functional approaches, shadowing semantics — multiple valid approaches need evaluation for tutorial clarity.
- **Phase 7 (Closures):** Closure representation techniques, environment capture strategies, memory management implications — advanced topic with multiple implementation strategies.

Phases with standard patterns (skip research-phase):
- **Phase 1 (Lexer/Parser):** Well-documented fslex/fsyacc patterns, official examples exist, traditional compiler course material applies.
- **Phase 2 (Evaluation):** Tree-walking interpretation is standard, pattern matching approach is canonical for F#.
- **Phase 4 (Statements):** Straightforward extension of evaluation, well-understood patterns.
- **Phase 5 (Control Flow):** Standard if/else evaluation, boolean operations are basic.
- **Phase 8 (REPL):** Interactive loop is standard pattern, position tracking techniques are established.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | FsLexYacc is directly specified requirement, .NET 8 LTS is obvious choice, all sources agree on discriminated unions for AST |
| Features | HIGH | Strong consensus from "Crafting Interpreters", "Writing an Interpreter in Go", and academic compiler courses on progressive feature set |
| Architecture | HIGH | Multi-stage pipeline (lexer/parser/evaluator) is universal in compiler design, F#-specific patterns are well-documented |
| Pitfalls | HIGH | Build order issues are well-documented in FsLexYacc resources, incremental structure validated by successful tutorials like "Crafting Interpreters" |

**Overall confidence:** HIGH

Research drew from authoritative sources: official FsLexYacc documentation, Microsoft .NET docs, "Crafting Interpreters" (gold standard tutorial), compiler construction textbooks, and F#-specific tutorials. fslex/fsyacc-specific pitfalls are documented in GitHub issues and community tutorials. Tutorial structure patterns are validated by successful projects (Crafting Interpreters, Writing an Interpreter in Go, Let's Build A Simple Interpreter).

### Gaps to Address

- **Testing framework integration**: Research recommends Expecto but doesn't provide F# interpreter-specific test patterns. During Phase 2 planning, define testing structure (lexer tests, parser tests, evaluator tests, integration tests).

- **Error message quality**: PITFALLS.md identifies weak fsyacc error messages as a known issue, but doesn't provide comprehensive solutions. During Phase 8 planning, research error recovery techniques and custom error message strategies.

- **REPL multi-line input handling**: REPL is recommended feature but multi-line input logic (detecting incomplete expressions) is not detailed. During Phase 8 planning, research bracket matching or continuation prompt strategies.

- **Closure representation trade-offs**: Multiple closure implementation strategies exist (environment capture vs closure conversion). During Phase 7 planning, evaluate which approach is most pedagogically clear for F# learners.

- **Build automation**: Research mentions generated files can be read-only, but doesn't provide complete build script. During Phase 1 planning, create build scripts or MSBuild targets that handle file generation cleanly.

## Sources

### Primary (HIGH confidence)
- [FsLexYacc Official Documentation](https://fsprojects.github.io/FsLexYacc/) — Tool usage, grammar syntax, examples
- [FsLexYacc GitHub Repository](https://github.com/fsprojects/FsLexYacc) — Issues documenting %nonassoc bugs, reduce/reduce conflicts
- [Microsoft F# Language Reference](https://learn.microsoft.com/en-us/dotnet/fsharp/) — Discriminated unions, pattern matching, F# 8 features
- [.NET SDK Documentation](https://learn.microsoft.com/en-us/dotnet/) — .NET 8 LTS support timeline, SDK versions
- [Crafting Interpreters](https://craftinginterpreters.com/) — Incremental tutorial structure, chapter progression, testing patterns
- [F Sharp Programming/Lexing and Parsing - Wikibooks](https://en.wikibooks.org/wiki/F_Sharp_Programming/Lexing_and_Parsing) — Build order, file generation

### Secondary (MEDIUM confidence)
- [Using FSLexYacc Tutorial by Thanos](https://thanos.codes/blog/using-fslexyacc-the-fsharp-lexer-and-parser/) — Practical setup examples
- [Lexing and Parsing with F# - Sergey Tihon](https://sergeytihon.com/2014/07/04/lexing-and-parsing-with-f-part-i/) — Community tutorial
- [Writing an Interpreter in Go](https://interpreterbook.com/) — Tutorial structure patterns
- [Let's Build A Simple Interpreter](https://ruslanspivak.com/lsbasi-part1/) — Progressive teaching approach
- [Crafting Interpreters Reviews](https://www.chidiwilliams.com/posts/crafting-interpreters-a-review) — Validation of incremental approach

### Tertiary (LOW confidence)
- [Shift-Reduce Conflicts Documentation](https://www2.cs.arizona.edu/~debray/Teaching/CSc453/DOCS/conflicts.pdf) — Academic reference for grammar conflicts
- [Anti-patterns resources](https://sahandsaba.com/nine-anti-patterns-every-programmer-should-be-aware-of-with-examples.html) — General software engineering practices
- Community blog posts on fslex/fsyacc (various) — Practical tips but not authoritative

---
*Research completed: 2026-01-30*
*Ready for roadmap: yes*
