module Ast

/// Expression AST for arithmetic operations
/// Phase 2: Arithmetic expressions with precedence
type Expr =
    | Number of int
    | Add of Expr * Expr
    | Subtract of Expr * Expr
    | Multiply of Expr * Expr
    | Divide of Expr * Expr
    | Negate of Expr  // Unary minus
