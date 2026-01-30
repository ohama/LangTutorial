module Eval

open Ast

/// Evaluate an expression to an integer result
/// Division by zero throws DivideByZeroException (to be handled in Phase 6)
let rec eval (expr: Expr) : int =
    match expr with
    | Number n -> n
    | Add (left, right) -> eval left + eval right
    | Subtract (left, right) -> eval left - eval right
    | Multiply (left, right) -> eval left * eval right
    | Divide (left, right) -> eval left / eval right
    | Negate e -> -(eval e)
