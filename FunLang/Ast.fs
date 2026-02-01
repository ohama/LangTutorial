module Ast

/// Expression AST for arithmetic operations
/// Phase 2: Arithmetic expressions with precedence
/// Phase 3: Variables and let binding
/// Phase 4: Control flow, comparisons, and logical operators
/// Phase 5: Functions (Lambda, App, LetRec)
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
    // Phase 2 (v2.0): Strings
    | String of string        // String literal
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
    // Phase 5: Functions
    | Lambda of param: string * body: Expr      // fun param -> body
    | App of func: Expr * arg: Expr             // func arg (function application)
    | LetRec of name: string * param: string * body: Expr * inExpr: Expr
    // let rec name param = body in inExpr
    // Phase 1 (v3.0): Tuples
    | Tuple of Expr list               // Tuple expression: (e1, e2, ...)
    | LetPat of Pattern * Expr * Expr  // Let with pattern binding: let pat = expr in body

/// Pattern for destructuring bindings
/// Phase 1 (v3.0): Tuple patterns
and Pattern =
    | VarPat of string           // Variable pattern: x
    | TuplePat of Pattern list   // Tuple pattern: (p1, p2, ...)
    | WildcardPat                // Wildcard pattern: _

/// Value type for evaluation results
/// Phase 4: Heterogeneous types (int and bool)
/// Phase 5: FunctionValue for first-class functions (mutual recursion with Expr, Env)
and Value =
    | IntValue of int
    | BoolValue of bool
    | FunctionValue of param: string * body: Expr * closure: Env
    | StringValue of string   // v2.0: String values
    | TupleValue of Value list  // v3.0: Tuple values

/// Environment mapping variable names to values
/// Phase 5: Defined here for mutual recursion with Value
and Env = Map<string, Value>
