---
plan: 05-02
status: completed
---

# 05-02 Summary: Function Evaluation

## Completed Tasks
- Task 1: Implement function evaluation in Eval.fs - Done
- Task 2: Update formatValue for FunctionValue - Done
- Task 3: Create fslit tests for functions - Done

## Key Changes
- `FunLang/Eval.fs`: Added Lambda, App, LetRec evaluation cases with closure semantics
  - Lambda: creates FunctionValue capturing current environment
  - App: evaluates function and argument, extends closure env with param binding
  - LetRec: creates recursive function with call-time self-augmentation
  - formatValue: displays FunctionValue as `<function>`
- `FunLang/Parser.fsy`: Fixed grammar to distinguish subtraction from unary minus
  - Added Atom non-terminal for function arguments (excludes unary minus)
  - Subtraction (`f - 1`) now works correctly; negative args need parens (`f (-1)`)
- `FunLang/Parser.fs`, `FunLang/Parser.fsi`: Regenerated from Parser.fsy
- `tests/functions/`: Added 13 fslit test files
- `tests/Makefile`: Added functions target

## Implementation Details

### Recursive Function Strategy
The challenge: creating a self-referential closure (function that contains itself).

Solution: Call-time augmentation
- LetRec creates a function with the base environment (no self-reference)
- App detects named function calls (`Var name`)
- When calling by name, App adds `name -> funcVal` to the closure
- This enables recursion: each call re-injects the self-reference

This avoids the need for mutable refs, lazy evaluation, or circular data structures.

### Parser Fix
Phase 5-01 introduced function application which caused `1 - 1` to parse as `App(1, Negate(1))`.

Fix: Separate Atom from Factor
- Atom: NUMBER, IDENT, TRUE, FALSE, (Expr) - used in AppExpr
- Factor: MINUS Factor | Atom - includes unary minus
- AppExpr uses Atom to avoid ambiguity

Result: `f -1` is subtraction, `f (-1)` is application with negative argument.

## Verification
- [x] Build succeeds without warnings
- [x] `let f = fun x -> x + 1 in f 5` returns 6
- [x] `let add = fun x -> fun y -> x + y in add 3 4` returns 7
- [x] `let rec fact n = if n <= 1 then 1 else n * fact (n - 1) in fact 5` returns 120
- [x] `let rec fib n = if n <= 1 then n else fib (n - 1) + fib (n - 2) in fib 6` returns 8
- [x] `let x = 10 in let f = fun y -> x + y in f 5` returns 15
- [x] All 66 fslit tests pass
- [x] All 93 Expecto tests pass

## Commits
- 7dfeedc: feat(05-02): implement function evaluation with closure semantics
- 90a86e4: test(05-02): add fslit tests for functions

## Notes
- FunLang is now Turing-complete with recursion support
- The closure model follows lexical scoping (definition-time environment)
- Currying works naturally through nested lambdas
- The parser has shift/reduce conflicts (expected for this grammar style, resolved by preferring shift)
