module Ast

/// Value type for evaluation results
/// Phase 4: Heterogeneous types (int and bool)
type Value =
    | IntValue of int
    | BoolValue of bool

/// Expression AST for arithmetic operations
/// Phase 2: Arithmetic expressions with precedence
/// Phase 3: Variables and let binding
/// Phase 4: Control flow, comparisons, and logical operators
type Expr =
    | Number of int
    | Add of Expr * Expr
    | Subtract of Expr * Expr
    | Multiply of Expr * Expr
    | Divide of Expr * Expr
    | Negate of Expr  // Unary minus
    // Phase 3: Variables
    | Var of string           // Variable reference
    | Let of string * Expr * Expr  // let name = expr1 in expr2
    // Phase 4: Control flow
    | Bool of bool            // Boolean literal (true, false)
    | If of Expr * Expr * Expr  // if condition then expr1 else expr2
    // Phase 4: Comparison operators (return BoolValue)
    | Equal of Expr * Expr       // =
    | NotEqual of Expr * Expr    // <>
    | LessThan of Expr * Expr    // <
    | GreaterThan of Expr * Expr // >
    | LessEqual of Expr * Expr   // <=
    | GreaterEqual of Expr * Expr // >=
    // Phase 4: Logical operators (short-circuit evaluation)
    | And of Expr * Expr  // &&
    | Or of Expr * Expr   // ||
