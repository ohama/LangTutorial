---
phase: 01-parser-extensions
plan: 01
subsystem: lexer-ast
tags: [lexer, ast, tokens, type-annotations, v6.0]
requires: [v5.0-functions]
provides: [type-annotation-tokens, type-expr-ast]
affects: [01-02-parser-rules, 02-inference-foundation]
tech-stack:
  added: []
  patterns: [ml-style-annotations]
key-files:
  created: []
  modified:
    - path: FunLang/Lexer.fsl
      reason: Added 6 new tokens for type annotation syntax
    - path: FunLang/Ast.fs
      reason: Added TypeExpr type and Annot/LambdaAnnot expression variants
decisions:
  - key: type-var-includes-apostrophe
    choice: TYPE_VAR lexeme includes the apostrophe character
    rationale: Simplifies parser handling - full type variable string captured at lexer level
  - key: colon-after-cons
    choice: COLON token placed after :: (CONS) in lexer rules
    rationale: Ensures :: is matched as single token, not as two colons
  - key: type-expr-no-span
    choice: TypeExpr variants do not carry Span information
    rationale: Type expressions are not the source of runtime errors - the annotated expressions are
metrics:
  duration: 1m 52s
  completed: 2026-02-03
---

# Phase 01 Plan 01: Lexer & AST Foundation Summary

**One-liner:** Added 6 type annotation tokens (TYPE_INT/BOOL/STRING/LIST/VAR, COLON) and TypeExpr AST with 7 variants for bidirectional type system.

## What Was Built

### Lexer Tokens (FunLang/Lexer.fsl)

Added foundation tokens for type annotation syntax:

1. **Type variable character class** (`type_var`): Matches `'a`, `'b`, etc.
2. **Type keyword tokens**: TYPE_INT, TYPE_BOOL, TYPE_STRING, TYPE_LIST
3. **Type variable token**: TYPE_VAR (captures full string including apostrophe)
4. **Annotation separator**: COLON token for `(e : T)` syntax

**Critical ordering maintained:**
- Type keywords BEFORE identifier pattern (else "int" lexes as IDENT)
- TYPE_VAR BEFORE identifier pattern (else 'a fails)
- COLON AFTER "::" (else "::" lexes as two COLONs)

### AST Types (FunLang/Ast.fs)

Added TypeExpr type for representing type annotations:

```fsharp
type TypeExpr =
    | TEInt                               // int
    | TEBool                              // bool
    | TEString                            // string
    | TEList of TypeExpr                  // T list
    | TEArrow of TypeExpr * TypeExpr      // T1 -> T2
    | TETuple of TypeExpr list            // T1 * T2 * ...
    | TEVar of string                     // 'a, 'b
```

Added expression variants for annotated code:
- **Annot**: `(e : T)` - explicit type annotation on expression
- **LambdaAnnot**: `fun (x: T) -> e` - parameter type annotation

Updated `spanOf` function to handle new expression variants.

## Commits

| Task | Description | Commit | Files |
|------|-------------|--------|-------|
| 1 | Add type annotation tokens | 0511561 | FunLang/Lexer.fsl |
| 2 | Add TypeExpr and annotated variants | e0ab2e6 | FunLang/Ast.fs |

## Deviations from Plan

None - plan executed exactly as written.

## Decisions Made

1. **Type variable lexeme includes apostrophe**
   - TYPE_VAR captures `'a` as full string (not just `a`)
   - Simplifies parser - doesn't need to reconstruct type variable name
   - Consistent with other token types that capture full lexemes

2. **COLON placement after CONS**
   - COLON token rule must come after "::" pattern
   - Prevents "::" being lexed as two separate COLON tokens
   - Follows multi-char before single-char ordering principle

3. **TypeExpr has no Span fields**
   - Type expressions themselves don't cause runtime errors
   - Span information kept on enclosing expression (Annot, LambdaAnnot)
   - Simplifies type expression construction in parser
   - Error messages still precise - point to the annotated expression

## Technical Details

### Token Ordering Rationale

The lexer rule ordering is critical for correct tokenization:

```fsharp
// Keywords (including type keywords) MUST come before identifier
| "int"  { TYPE_INT }
| "bool" { TYPE_BOOL }
// ...
| type_var { TYPE_VAR (lexeme lexbuf) }  // BEFORE identifier
| ident_start ident_char* { IDENT (lexeme lexbuf) }
```

Without this ordering:
- `int` would lex as `IDENT "int"` not `TYPE_INT`
- `'a` would fail to match any pattern

### Incomplete Pattern Match Warnings

Build now shows expected warnings:
```
Infer.fs(74,11): warning FS0025: Incomplete pattern matches on this expression.
  For example, the value 'Annot (_, _, _)' may indicate a case not covered
Eval.fs(70,11): warning FS0025: Incomplete pattern matches on this expression.
```

These are expected and will be resolved in Phase 2 (Inference) and Phase 5 (Elaboration).

## Current State

### Build Status

**Expected state:** Lexer tokens defined but not declared in Parser.fsy yet.

```bash
dotnet build FunLang  # Fails with "TYPE_INT not defined" etc.
```

This is correct - Parser.fsy will declare these tokens in plan 01-02.

### What Works

- Lexer character class definitions compile
- AST type definitions compile
- `spanOf` function handles all expression variants

### What Doesn't Work Yet

- Cannot parse type annotations (tokens not in parser grammar)
- Cannot type-check annotations (inference not implemented)
- Cannot evaluate annotations (elaboration not implemented)

## Next Phase Readiness

### Unblocked Work

- **Plan 01-02**: Can now add parser grammar rules for type expressions and annotations
- Parser has tokens to reference in grammar rules
- Parser can construct TypeExpr and Annot/LambdaAnnot AST nodes

### Blockers

None.

### Required Follow-up

1. **Parser.fsy token declarations** (Plan 01-02)
   - Add `%token TYPE_INT TYPE_BOOL TYPE_STRING TYPE_LIST TYPE_VAR COLON`
   - Without this, Lexer.fsl cannot compile

2. **Pattern match completion** (Phase 2, Phase 5)
   - Infer.fs needs to handle Annot and LambdaAnnot
   - Eval.fs needs to handle Annot and LambdaAnnot
   - Not blocking for parser extension work

### Dependencies for Other Phases

- Phase 2 (Inference): Depends on TypeExpr AST and Annot/LambdaAnnot variants ✓
- Phase 3 (Top-level): No dependency on this work
- Phase 4 (Multi-param): Depends on LambdaAnnot variant ✓
- Phase 5 (Elaboration): Depends on Annot and LambdaAnnot variants ✓

## Lessons Learned

1. **Lexer rule ordering is load-bearing**
   - Type keywords must precede identifier pattern
   - Multi-char operators must precede single-char
   - Easy to introduce tokenization bugs without tests

2. **Span placement design matters**
   - Keeping Span on expressions not type expressions is cleaner
   - Type expressions often constructed recursively
   - Error messages still precise - point to annotated expression

3. **Build fails are informative**
   - "TYPE_INT not defined" confirms tokens are referenced correctly
   - Incomplete pattern warnings show AST is recognized by type checker
   - Expected failures are good validation

## Verification Checklist

- [x] Lexer.fsl has `type_var` character class
- [x] Lexer.fsl has TYPE_INT, TYPE_BOOL, TYPE_STRING, TYPE_LIST tokens
- [x] Lexer.fsl has TYPE_VAR token (before identifier pattern)
- [x] Lexer.fsl has COLON token (after CONS pattern)
- [x] Ast.fs has TypeExpr type with all 7 variants
- [x] Ast.fs has Annot and LambdaAnnot in Expr type
- [x] Ast.fs spanOf function handles new Expr variants
- [x] No syntax errors in modified files

## References

- **Lexer token ordering**: Following fslex best practices
- **ML-style annotations**: Standard `(e : T)` and `fun (x: T) -> e` syntax
- **Type expression AST**: Covers all FunLang type constructors
