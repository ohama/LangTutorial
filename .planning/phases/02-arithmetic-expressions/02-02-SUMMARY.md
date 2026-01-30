# Summary: 02-02 Parser + Lexer + Pipeline wiring

**Executed:** 2026-01-30
**Status:** Complete

## What Was Done

### Task 1: Expand parser with Expr/Term/Factor grammar
- Rewrote `Parser.fsy` with classic Expr/Term/Factor pattern
- Encodes operator precedence in grammar structure (not %left/%right)
- Added tokens: PLUS, MINUS, STAR, SLASH, LPAREN, RPAREN
- Left recursion for correct left associativity
- Unary minus in Factor rule for highest precedence

### Task 2: Expand lexer with operator tokens
- Updated `Lexer.fsl` to recognize: +, -, *, /, (, )
- Single-character token patterns
- MINUS serves both binary subtraction and unary negation

### Task 3: Wire everything in Program.fs
- Imported Eval module
- Added eval call to pipeline: parse → eval
- 9 test cases covering all success criteria:
  - Numbers (Phase 1 regression)
  - Basic operations (+, -, *, /)
  - Operator precedence (* before +)
  - Parentheses override
  - Left associativity
  - Unary minus (single, double, expressions)

## Files Changed

| File | Change |
|------|--------|
| FunLang/Parser.fsy | Expr/Term/Factor grammar with operator precedence |
| FunLang/Lexer.fsl | Operator and parenthesis tokens |
| FunLang/Program.fs | Test runner with 9 test cases |

## Test Results

```
[PASS] "42" = 42 (expected 42)
[PASS] "2 + 3" = 5 (expected 5)
[PASS] "2 + 3 * 4" = 14 (expected 14)
[PASS] "(2 + 3) * 4" = 20 (expected 20)
[PASS] "10 / 2 - 3" = 2 (expected 2)
[PASS] "-5 + 3" = -2 (expected -2)
[PASS] "2 - 3 - 4" = -5 (expected -5)
[PASS] "--5" = 5 (expected 5)
[PASS] "-(2 + 3)" = -5 (expected -5)

All tests passed!
```

## Requirements Verified

- **EXPR-01** (사칙연산): All 4 operations work
- **EXPR-02** (연산자 우선순위): "2 + 3 * 4" = 14
- **EXPR-03** (괄호 우선순위): "(2 + 3) * 4" = 20
- **EXPR-04** (단항 마이너스): "-5 + 3" = -2

## Phase 2 Complete

All plans executed, all tests pass, all requirements met.
