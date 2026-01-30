---
plan: 05-01
status: completed
---

# 05-01 Summary: Function Syntax Extension

## Completed Tasks
- Task 1: Extend AST types for functions - Done
- Task 2: Extend Lexer and Parser for function syntax - Done
- Task 3: Extend Format.fs for new tokens - Done

## Key Changes
- `FunLang/Ast.fs`: Added Lambda, App, LetRec to Expr type; added FunctionValue to Value type; moved Env type here for mutual recursion with Value; reordered types (Expr -> Value -> Env) using F# `and` keyword
- `FunLang/Eval.fs`: Removed duplicate Env type definition (now uses Ast.Env)
- `FunLang/Lexer.fsl`: Added FUN, REC keywords and ARROW operator
- `FunLang/Parser.fsy`: Added token declarations for FUN, REC, ARROW; added grammar rules for `fun x -> expr` and `let rec f x = ... in ...`; added AppExpr non-terminal for left-associative function application
- `FunLang/Format.fs`: Added format cases for FUN, REC, ARROW tokens

## Verification
- [x] Build succeeds (warnings in Eval.fs expected, fixed in Plan 02)
- [x] Token output works: `fun x -> x + 1` outputs `FUN IDENT(x) ARROW IDENT(x) PLUS NUMBER(1) EOF`
- [x] AST output works: `fun x -> x + 1` outputs `Lambda ("x", Add (Var "x", Number 1))`
- [x] Let rec parsing: `let rec f x = x in f 1` outputs `LetRec ("f", "x", Var "x", App (Var "f", Number 1))`
- [x] Application parsing: `f 1 2` outputs `App (App (Var "f", Number 1), Number 2)` (left-associative for currying)

## Commits
- ff8fab5: feat(05-01): extend AST types for functions
- d86c041: feat(05-01): add function syntax to lexer and parser
- 0c3c77e: feat(05-01): add FUN, REC, ARROW token formatting

## Notes
- Mutual recursion between Expr, Value, and Env requires specific ordering in F#: Expr first (because Value references Expr in FunctionValue), then Value, then Env, connected with `and` keyword
- The parser has shift/reduce conflicts for constructs like `let`, `if`, `fun` that extend to the right, which are resolved by preferring shift (standard behavior for rightward-extending constructs)
- Eval.fs now has expected warnings about incomplete pattern matches for FunctionValue, Lambda, App, LetRec - these will be fixed in Plan 05-02 when function semantics are implemented
