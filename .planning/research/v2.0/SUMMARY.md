# Project Research Summary

**Project:** FunLang v2.0 Milestone
**Domain:** Language interpreter extension (REPL, Strings, Comments)
**Researched:** 2026-01-31
**Confidence:** HIGH

## Executive Summary

FunLang v2.0 adds three features to an established fslex/fsyacc interpreter: comments, strings, and a REPL. All three features follow well-documented patterns in the fslex ecosystem and require **no new library dependencies**. The existing FsLexYacc 11.3.0 and .NET 10 stack provides everything needed. Each feature has distinct integration characteristics: comments are lexer-only (zero downstream impact), strings flow through the full pipeline (lexer to eval), and REPL wraps existing infrastructure (CLI-only change).

The recommended approach is a three-phase implementation in dependency order: Comments first (enables code documentation immediately, lowest risk), Strings second (full pipeline change, well-understood patterns), REPL third (benefits from complete language features). This order minimizes risk by starting with isolated changes and progressively integrating more components. Total estimated effort is approximately 90 lines of new/modified code across all phases.

Key risks center on lexer state management (string escape sequences, nested comments) and REPL environment persistence. Both are well-documented patterns in fslex tutorials and the FsLexYacc JSON parser example. The critical mitigation is using separate lexer entry points (`and` keyword in fslex) for stateful parsing of strings and block comments, and threading the environment through REPL iterations rather than recreating it fresh each input.

## Key Findings

### Recommended Stack

No new dependencies required. The existing stack handles all requirements:

**Core technologies:**
- **System.Console.ReadLine()**: REPL input - built-in, zero dependencies, cross-platform
- **FsLex multi-rule pattern**: String lexing - `and read_string` state machine for escapes
- **FsLex skip rules**: Comments - standard lexer pattern, return next token on match

**Explicitly avoided:**
- External readline libraries (ReadLine.Reboot deprecated, Terminaux overkill, RadLine preview-only)
- String interpolation (`$"{x}"` - too complex for tutorial scope)
- Character type (strings of length 1 suffice)

### Expected Features

**Must have (table stakes):**
- Single-line comments `// ...` - basic code documentation
- String literals `"hello"` with escapes `\n`, `\t`, `\\`, `\"`
- String concatenation with `+` operator
- String equality with `=` and `<>` operators
- Basic REPL loop with prompt, error recovery, graceful exit
- Environment persistence across REPL inputs

**Should have (differentiators):**
- Multi-line comments `(* ... *)` with nesting support
- `it` variable for last REPL result
- REPL commands `:help`, `:env`, `:quit`
- String comparison operators `<`, `>`, `<=`, `>=`

**Defer (v2.1+):**
- Command history (requires external library)
- Tab completion
- String interpolation
- String indexing and substring operations
- Multi-line REPL input detection

### Architecture Approach

The existing FunLang pipeline (Lexer.fsl -> Parser.fsy -> Ast.fs -> Eval.fs -> Program.fs) is well-structured for extension. Each feature has minimal coupling:

**Major components affected:**

| Feature | Lexer | Parser | AST | Eval | CLI | Format |
|---------|-------|--------|-----|------|-----|--------|
| Comments | MODIFY | - | - | - | - | - |
| Strings | MODIFY | MODIFY | MODIFY | MODIFY | - | MODIFY |
| REPL | - | - | - | - | MODIFY | - |

**Key patterns:**
1. **Lexer states** - Use `and` keyword for string/comment state machines
2. **Type extension** - Add cases to discriminated unions (compiler catches missing matches)
3. **Environment threading** - REPL passes Env between iterations, not recreating emptyEnv

### Critical Pitfalls

1. **Environment not persisted between REPL inputs** - Create REPL-specific `evalInEnv : Env -> Expr -> Value * Env` function; thread environment through iterations
2. **String lexer uses simple regex instead of state machine** - Use fslex's `and read_string` entry point with StringBuilder accumulator; handle escapes, newlines (error), EOF (error)
3. **Token declaration order wrong** - Add STRING token to Parser.fsy BEFORE modifying Lexer.fsl; run fsyacc before fslex
4. **Comment patterns don't match before operators** - Put `"//"` and `"(*"` BEFORE `'/'` in lexer rules; longer matches first
5. **Value type not extended for strings** - Add `StringValue of string` to Value union; update `formatValue` and comparison operators in Eval.fs

## Implications for Roadmap

Based on research, suggested phase structure:

### Phase 1: Comments (Lexer-only)

**Rationale:** Simplest change with zero downstream impact. Enables documenting code immediately. Cannot break existing functionality.

**Delivers:** Single-line `//` comments, optionally multi-line `(* *)` with nesting

**Addresses features:**
- Single-line comments (table stakes)
- Multi-line comments (differentiator)

**Avoids pitfalls:**
- Comment patterns before operators (pattern order)
- Nested comment depth tracking
- Unterminated comment detection

**Complexity:** Low (~15 lines lexer)

### Phase 2: Strings (Full Pipeline)

**Rationale:** Adds new type to language. Full pipeline change but well-understood patterns. Benefits from comments (can document string-handling code).

**Delivers:** String literals, escape sequences, concatenation, equality comparison

**Uses stack elements:**
- FsLex multi-rule pattern (string state machine)
- StringBuilder for escape accumulation

**Implements architecture:**
- STRING token and Expr.String AST node
- StringValue in Value type
- Extended Add/Equal/NotEqual patterns in Eval

**Addresses features:**
- String literals with escapes (table stakes)
- String concatenation (table stakes)
- String equality (table stakes)

**Avoids pitfalls:**
- Lexer state not reset on error
- Newlines in strings (should error)
- Value type missing StringValue
- Comparison operators not extended

**Complexity:** Medium (~40 lines across lexer, parser, AST, eval, format)

### Phase 3: REPL (CLI Integration)

**Rationale:** Builds on complete language. Best user experience when all features work. Uses existing parse/eval infrastructure unchanged.

**Delivers:** Interactive read-eval-print loop with environment persistence, error recovery, clean exit

**Uses stack elements:**
- System.Console.ReadLine() (no external dependencies)
- Existing parse/evalExpr functions

**Implements architecture:**
- REPL loop in Program.fs
- Environment threading between iterations
- `--repl` CLI flag

**Addresses features:**
- Basic REPL loop (table stakes)
- Error recovery (table stakes)
- Environment persistence (table stakes)
- Graceful exit on Ctrl+D or `exit` (table stakes)

**Avoids pitfalls:**
- Environment recreated each iteration
- Exceptions crash REPL loop
- No exit mechanism
- evalExpr ignores environment persistence

**Complexity:** Medium (~35 lines in Program.fs)

### Phase Ordering Rationale

- **Comments first:** Zero risk, enables documenting subsequent work, builds confidence
- **Strings before REPL:** REPL benefits from complete type system; testing strings in REPL is valuable
- **REPL last:** Transforms user experience once language is feature-complete; uses all prior work

This order follows the "inside-out" pattern: core language features first (comments, strings), then developer experience layer (REPL).

### Research Flags

**Phases with standard patterns (skip research-phase):**
- **Phase 1 (Comments):** Well-documented fslex pattern; FsLexYacc docs have exact examples
- **Phase 2 (Strings):** FsLexYacc JSON parser example demonstrates exact pattern
- **Phase 3 (REPL):** Basic loop is straightforward; Console.ReadLine is well-understood

**Phases needing attention during planning (not research, just care):**
- **Phase 2 (Strings):** Escape sequence handling requires careful lexer state machine design
- **Phase 3 (REPL):** Environment persistence requires modifying how eval returns updated Env

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | No new dependencies; existing FsLexYacc handles all needs; library evaluation complete |
| Features | HIGH | Clear table stakes vs differentiators; scope well-defined; anti-features identified |
| Architecture | HIGH | Existing pipeline analyzed; integration points clear; minimal coupling between features |
| Pitfalls | HIGH | Comprehensive research with fslex-specific issues; prevention strategies documented |

**Overall confidence:** HIGH

All four research areas have strong documentation and verified patterns. The fslex/fsyacc ecosystem is mature, and similar implementations (JSON parser, OCaml tutorials) provide proven patterns.

### Gaps to Address

- **REPL environment persistence mechanism:** Current `evalExpr` returns only Value, not updated Env. Need to modify signature or create REPL-specific variant. Address during Phase 3 planning.
- **String operation scope creep:** `length`, `substring`, indexing deferred but may be requested. Keep firm boundary on MVP.
- **Multi-line REPL input:** Detecting incomplete expressions is non-trivial. Explicitly defer to v2.1.

## Sources

### Primary (HIGH confidence)
- [FsLexYacc Documentation](https://fsprojects.github.io/FsLexYacc/content/fslex.html) - lexer states, multi-rule patterns
- [FsLexYacc JSON Parser Example](https://github.com/fsprojects/FsLexYacc/blob/master/docs/content/jsonParserExample.md) - string handling patterns
- [Microsoft F# Interactive](https://learn.microsoft.com/en-us/dotnet/fsharp/tools/fsharp-interactive/) - REPL reference implementation

### Secondary (MEDIUM confidence)
- [Crafting Interpreters - Statements and State](https://craftinginterpreters.com/statements-and-state.html) - environment persistence
- [Using FsLexYacc Tutorial](https://thanos.codes/blog/using-fslexyacc-the-fsharp-lexer-and-parser/) - practical implementation guide
- [Wikibooks F# Lexing and Parsing](https://en.wikibooks.org/wiki/F_Sharp_Programming/Lexing_and_Parsing) - comprehensive guide

### Library Evaluation
- ReadLine by tonerdo - abandoned (2018), not recommended
- ReadLine.Reboot - deprecated, recommends Terminaux
- Terminaux 6.1.6 - overkill for tutorial scope
- RadLine 0.9.0 - preview only

---
*Research completed: 2026-01-31*
*Ready for roadmap: yes*
