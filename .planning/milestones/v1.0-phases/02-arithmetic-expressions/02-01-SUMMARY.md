# Summary: 02-01 AST expansion + Evaluator

**Executed:** 2026-01-30
**Status:** Complete

## What Was Done

### Task 1: Expand AST with arithmetic operations
- Extended `Ast.fs` with 5 new cases:
  - `Add of Expr * Expr`
  - `Subtract of Expr * Expr`
  - `Multiply of Expr * Expr`
  - `Divide of Expr * Expr`
  - `Negate of Expr`

### Task 2: Create evaluator with pattern matching
- Created `Eval.fs` with recursive `eval` function
- Pattern matches all 6 AST cases exhaustively
- Division by zero deferred to Phase 6 error handling
- Updated `FunLang.fsproj` with Eval.fs in correct compilation order

## Files Changed

| File | Change |
|------|--------|
| FunLang/Ast.fs | Extended Expr type with arithmetic operations |
| FunLang/Eval.fs | **NEW** - Recursive evaluator function |
| FunLang/FunLang.fsproj | Added Eval.fs to compilation order |

## Verification

- `dotnet build` succeeds with 0 warnings, 0 errors
- All 6 AST cases defined and compilable
- Evaluator exhaustively matches all cases (compiler verified)

## Next

Wave 2 (02-02-PLAN.md) will update Parser and Lexer to produce arithmetic AST nodes.
