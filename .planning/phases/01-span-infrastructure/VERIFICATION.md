---
phase: 01-span-infrastructure
verified: 2026-02-02T18:25:00Z
status: passed
score: 4/4 must-haves verified
must_haves:
  truths:
    - "Every Expr node carries span information (file, start/end line and column)"
    - "Lexer generates position data for every token"
    - "Parser propagates spans from tokens to AST nodes"
    - "Span type can represent unknown locations for built-in definitions"
  artifacts:
    - path: "FunLang/Ast.fs"
      provides: "Span type definition, mkSpan, unknownSpan, formatSpan, spanOf, patternSpanOf"
    - path: "FunLang/Lexer.fsl"
      provides: "setInitialPos function, position tracking via NextLine"
    - path: "FunLang/Parser.fsy"
      provides: "ruleSpan/symSpan helpers, span propagation in all grammar rules"
  key_links:
    - from: "Parser.fsy"
      to: "Ast.fs"
      via: "mkSpan calls in ruleSpan/symSpan"
    - from: "Lexer.fsl"
      to: "lexbuf position tracking"
      via: "setInitialPos and NextLine calls"
---

# Phase 1: Span Infrastructure Verification Report

**Phase Goal:** Source location tracking across lexer, parser, and AST
**Verified:** 2026-02-02T18:25:00Z
**Status:** passed
**Re-verification:** No - initial verification

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Every Expr node carries span information | VERIFIED | All 26 Expr variants have `span: Span` as last parameter (Ast.fs lines 50-87) |
| 2 | Lexer generates position data for every token | VERIFIED | `setInitialPos` function (Lexer.fsl line 11-18), `NextLine` calls on all newlines (lines 32, 66, 93) |
| 3 | Parser propagates spans from tokens to AST nodes | VERIFIED | `ruleSpan`/`symSpan` helpers (Parser.fsy lines 7-12), used in ALL 40+ grammar rules |
| 4 | Span type can represent unknown locations | VERIFIED | `unknownSpan` sentinel constant (Ast.fs lines 25-32), used in tests and prelude |

**Score:** 4/4 truths verified

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `FunLang/Ast.fs` | Span type, helpers, spanOf | VERIFIED | 147 lines, Span type (lines 6-12), mkSpan (15-22), unknownSpan (25-32), formatSpan (35-41), spanOf (128-140), patternSpanOf (143-146) |
| `FunLang/Lexer.fsl` | Position tracking | VERIFIED | 108 lines, setInitialPos (11-18), NextLine in 3 contexts (32, 66, 93) |
| `FunLang/Parser.fsy` | Span propagation | VERIFIED | 147 lines, ruleSpan/symSpan (7-12), span in ALL AST constructions |

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| Parser.fsy | Ast.fs | mkSpan | WIRED | ruleSpan/symSpan call mkSpan (lines 8, 12) |
| Lexer.fsl | lexbuf | setInitialPos | WIRED | Called from Program.fs (14), Prelude.fs (11), Repl.fs (11), tests |
| Lexer.fsl | lexbuf | NextLine | WIRED | 3 occurrences: main tokenize (32), block comment (93), line comment (66) |
| Ast.fs | Expr | spanOf | WIRED | Extracts span from any Expr, covers all 26 variants |
| Ast.fs | Pattern | patternSpanOf | WIRED | Extracts span from any Pattern, covers all 6 variants |

### Requirements Coverage

| Requirement | Status | Evidence |
|-------------|--------|----------|
| SPAN-01: Span type definition | SATISFIED | Ast.fs lines 6-12: record with FileName, StartLine, StartColumn, EndLine, EndColumn |
| SPAN-02: Expr span field | SATISFIED | All 26 Expr variants have `span: Span` as last parameter |
| SPAN-03: Lexer position | SATISFIED | setInitialPos initializes, NextLine updates on all newlines |
| SPAN-04: Parser span propagation | SATISFIED | ruleSpan/symSpan used in every grammar rule |

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| None | - | - | - | No anti-patterns detected |

### Human Verification Required

None required. All success criteria are programmatically verifiable through code inspection and build verification.

### Build Verification

```
dotnet build FunLang          # SUCCESS - 0 errors, 0 warnings
dotnet run --project FunLang.Tests  # 362 tests passed
```

## Verification Details

### 1. Span Type Definition (SPAN-01)

**Location:** `FunLang/Ast.fs` lines 6-12

```fsharp
type Span = {
    FileName: string
    StartLine: int
    StartColumn: int
    EndLine: int
    EndColumn: int
}
```

- Contains all required fields
- Uses 1-based indexing (matches FsLexYacc convention)
- Proper F# record syntax

### 2. Expr Span Fields (SPAN-02)

**Location:** `FunLang/Ast.fs` lines 49-87

All 26 Expr variants have span as last named parameter:
- Literals: Number, Bool, String
- Operators: Add, Subtract, Multiply, Divide, Negate
- Comparisons: Equal, NotEqual, LessThan, GreaterThan, LessEqual, GreaterEqual
- Logical: And, Or
- Variables: Var, Let, LetPat, LetRec
- Control: If
- Functions: Lambda, App
- Data: Tuple, EmptyList, List, Cons
- Matching: Match

Pattern variants (6 total) also have span: VarPat, TuplePat, WildcardPat, ConsPat, EmptyListPat, ConstPat

### 3. Lexer Position Tracking (SPAN-03)

**Location:** `FunLang/Lexer.fsl` lines 11-18, 32, 66, 93

- `setInitialPos`: Initializes lexbuf position with filename, line 1, column 0
- `NextLine` called in three contexts:
  1. Main tokenize rule (line 32): `newline` token
  2. Line comment rule (line 66): newline after `//`
  3. Block comment rule (line 93): newlines inside `(* ... *)`

### 4. Parser Span Propagation (SPAN-04)

**Location:** `FunLang/Parser.fsy` lines 7-12, 53-147

Helper functions:
```fsharp
let ruleSpan (parseState: IParseState) (firstSym: int) (lastSym: int) : Span =
    mkSpan (parseState.InputStartPosition firstSym) (parseState.InputEndPosition lastSym)

let symSpan (parseState: IParseState) (n: int) : Span =
    mkSpan (parseState.InputStartPosition n) (parseState.InputEndPosition n)
```

Usage patterns:
- Single token: `symSpan parseState 1` (e.g., NUMBER, IDENT, TRUE)
- Binary ops: `ruleSpan parseState 1 3` (e.g., Expr PLUS Term)
- Multi-part: `ruleSpan parseState 1 N` (e.g., IF Expr THEN Expr ELSE Expr uses 1-6)

### 5. Unknown Span Support

**Location:** `FunLang/Ast.fs` lines 25-32

```fsharp
let unknownSpan : Span =
    {
        FileName = "<unknown>"
        StartLine = 0
        StartColumn = 0
        EndLine = 0
        EndColumn = 0
    }
```

Used in:
- Test files for direct AST construction (14+ uses)
- Follows F# compiler's `range0` pattern

### 6. Helper Functions

**spanOf (Ast.fs lines 128-140):** Extracts span from any Expr - comprehensive coverage of all 26 variants

**patternSpanOf (Ast.fs lines 143-146):** Extracts span from any Pattern - covers all 6 variants

**formatSpan (Ast.fs lines 35-41):** Formats span for error messages, handles unknown location

## Gaps Summary

No gaps found. All four success criteria are fully implemented:

1. Every Expr node carries span information
2. Lexer generates position data for every token
3. Parser propagates spans from tokens to AST nodes
4. Span type can represent unknown locations for built-in definitions

---

*Verified: 2026-02-02T18:25:00Z*
*Verifier: Claude (gsd-verifier)*
