module Eval

open Ast

/// Environment mapping variable names to values
type Env = Map<string, int>

/// Empty environment for top-level evaluation
let emptyEnv : Env = Map.empty

/// Evaluate an expression in an environment
/// Raises exception for undefined variables
let rec eval (env: Env) (expr: Expr) : int =
    match expr with
    | Number n -> n

    | Var name ->
        match Map.tryFind name env with
        | Some value -> value
        | None -> failwithf "Undefined variable: %s" name

    | Let (name, binding, body) ->
        // Evaluate binding in current environment
        let value = eval env binding
        // Extend environment with new binding
        let extendedEnv = Map.add name value env
        // Evaluate body in extended environment
        eval extendedEnv body

    | Add (left, right) ->
        eval env left + eval env right

    | Subtract (left, right) ->
        eval env left - eval env right

    | Multiply (left, right) ->
        eval env left * eval env right

    | Divide (left, right) ->
        eval env left / eval env right

    | Negate e ->
        -(eval env e)

/// Convenience function for top-level evaluation
let evalExpr (expr: Expr) : int =
    eval emptyEnv expr
