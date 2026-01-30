module Eval

open Ast

/// Environment mapping variable names to values
/// Phase 4: Now stores Value (not int) for heterogeneous types
type Env = Map<string, Value>

/// Empty environment for top-level evaluation
let emptyEnv : Env = Map.empty

/// Format a value for user-friendly output
let formatValue (v: Value) : string =
    match v with
    | IntValue n -> string n
    | BoolValue b -> if b then "true" else "false"

/// Evaluate an expression in an environment
/// Returns Value (IntValue or BoolValue)
/// Raises exception for type errors and undefined variables
let rec eval (env: Env) (expr: Expr) : Value =
    match expr with
    | Number n -> IntValue n
    | Bool b -> BoolValue b

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

    // Arithmetic operations - type check for IntValue
    | Add (left, right) ->
        match eval env left, eval env right with
        | IntValue l, IntValue r -> IntValue (l + r)
        | _ -> failwith "Type error: + requires integer operands"

    | Subtract (left, right) ->
        match eval env left, eval env right with
        | IntValue l, IntValue r -> IntValue (l - r)
        | _ -> failwith "Type error: - requires integer operands"

    | Multiply (left, right) ->
        match eval env left, eval env right with
        | IntValue l, IntValue r -> IntValue (l * r)
        | _ -> failwith "Type error: * requires integer operands"

    | Divide (left, right) ->
        match eval env left, eval env right with
        | IntValue l, IntValue r -> IntValue (l / r)
        | _ -> failwith "Type error: / requires integer operands"

    | Negate e ->
        match eval env e with
        | IntValue n -> IntValue (-n)
        | _ -> failwith "Type error: unary - requires integer operand"

    // Comparison operators - type check for IntValue, return BoolValue
    | LessThan (left, right) ->
        match eval env left, eval env right with
        | IntValue l, IntValue r -> BoolValue (l < r)
        | _ -> failwith "Type error: < requires integer operands"

    | GreaterThan (left, right) ->
        match eval env left, eval env right with
        | IntValue l, IntValue r -> BoolValue (l > r)
        | _ -> failwith "Type error: > requires integer operands"

    | LessEqual (left, right) ->
        match eval env left, eval env right with
        | IntValue l, IntValue r -> BoolValue (l <= r)
        | _ -> failwith "Type error: <= requires integer operands"

    | GreaterEqual (left, right) ->
        match eval env left, eval env right with
        | IntValue l, IntValue r -> BoolValue (l >= r)
        | _ -> failwith "Type error: >= requires integer operands"

    // Equal and NotEqual work on both int and bool (same type required)
    | Equal (left, right) ->
        match eval env left, eval env right with
        | IntValue l, IntValue r -> BoolValue (l = r)
        | BoolValue l, BoolValue r -> BoolValue (l = r)
        | _ -> failwith "Type error: = requires operands of same type"

    | NotEqual (left, right) ->
        match eval env left, eval env right with
        | IntValue l, IntValue r -> BoolValue (l <> r)
        | BoolValue l, BoolValue r -> BoolValue (l <> r)
        | _ -> failwith "Type error: <> requires operands of same type"

    // Logical operators - short-circuit evaluation
    | And (left, right) ->
        match eval env left with
        | BoolValue false -> BoolValue false  // Short-circuit: don't evaluate right
        | BoolValue true ->
            match eval env right with
            | BoolValue b -> BoolValue b
            | _ -> failwith "Type error: && requires boolean operands"
        | _ -> failwith "Type error: && requires boolean operands"

    | Or (left, right) ->
        match eval env left with
        | BoolValue true -> BoolValue true  // Short-circuit: don't evaluate right
        | BoolValue false ->
            match eval env right with
            | BoolValue b -> BoolValue b
            | _ -> failwith "Type error: || requires boolean operands"
        | _ -> failwith "Type error: || requires boolean operands"

    // If-then-else - condition must be boolean
    | If (condition, thenBranch, elseBranch) ->
        match eval env condition with
        | BoolValue true -> eval env thenBranch
        | BoolValue false -> eval env elseBranch
        | _ -> failwith "Type error: if condition must be boolean"

/// Convenience function for top-level evaluation
let evalExpr (expr: Expr) : Value =
    eval emptyEnv expr
