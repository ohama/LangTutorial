---
status: testing
phase: 05-functions
source: 05-01-SUMMARY.md, 05-02-SUMMARY.md
started: 2026-01-31T10:00:00Z
updated: 2026-01-31T10:00:00Z
---

## Current Test

number: 1
name: Lambda Expression Evaluation
expected: |
  Run: `dotnet run --project FunLang -- --expr "let f = fun x -> x + 1 in f 5"`
  Expected output: `6`
awaiting: user response

## Tests

### 1. Lambda Expression Evaluation
expected: `let f = fun x -> x + 1 in f 5` returns `6`
result: [pending]

### 2. Multi-Parameter Function (Currying)
expected: `let add = fun x -> fun y -> x + y in add 3 4` returns `7`
result: [pending]

### 3. Recursive Factorial
expected: `let rec fact n = if n <= 1 then 1 else n * fact (n - 1) in fact 5` returns `120`
result: [pending]

### 4. Recursive Fibonacci
expected: `let rec fib n = if n <= 1 then n else fib (n - 1) + fib (n - 2) in fib 6` returns `8`
result: [pending]

### 5. Closure (Capture Outer Variable)
expected: `let x = 10 in let f = fun y -> x + y in f 5` returns `15`
result: [pending]

### 6. Function as Value Display
expected: `fun x -> x + 1` returns `<function>` (function value display)
result: [pending]

### 7. Let Rec with Named Function
expected: `let rec f x = x in f 1` returns `1` (simple rec binding)
result: [pending]

## Summary

total: 7
passed: 0
issues: 0
pending: 7
skipped: 0

## Gaps

[none yet]
