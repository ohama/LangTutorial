module Ast

/// Expression AST - minimal foundation for lexer/parser pipeline
/// Phase 1: Number only (proof of pipeline)
/// Phase 2 will add: Add, Subtract, Multiply, Divide
type Expr =
    | Number of int
