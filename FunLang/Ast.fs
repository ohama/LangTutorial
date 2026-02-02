module Ast

open FSharp.Text.Lexing

/// Source location span for error messages
type Span = {
    FileName: string
    StartLine: int
    StartColumn: int
    EndLine: int
    EndColumn: int
}

/// Create span from FsLexYacc Position records
let mkSpan (startPos: Position) (endPos: Position) : Span =
    {
        FileName = startPos.FileName
        StartLine = startPos.Line
        StartColumn = startPos.Column
        EndLine = endPos.Line
        EndColumn = endPos.Column
    }

/// Sentinel span for built-in/synthetic definitions (like F# compiler's range0)
let unknownSpan : Span =
    {
        FileName = "<unknown>"
        StartLine = 0
        StartColumn = 0
        EndLine = 0
        EndColumn = 0
    }

/// Format span for error messages
let formatSpan (span: Span) : string =
    if span = unknownSpan then
        "<unknown location>"
    elif span.StartLine = span.EndLine then
        sprintf "%s:%d:%d-%d" span.FileName span.StartLine span.StartColumn span.EndColumn
    else
        sprintf "%s:%d:%d-%d:%d" span.FileName span.StartLine span.StartColumn span.EndLine span.EndColumn

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
    // Phase 2 (v3.0): Lists
    | EmptyList                        // Empty list: []
    | List of Expr list                // List literal: [e1, e2, ...]
    | Cons of Expr * Expr              // Cons operator: h :: t
    // Phase 3 (v3.0): Pattern Matching
    | Match of scrutinee: Expr * clauses: MatchClause list

/// Pattern for destructuring bindings
/// Phase 1 (v3.0): Tuple patterns
/// Phase 3 (v3.0): Extended with ConsPat, EmptyListPat, ConstPat
and Pattern =
    | VarPat of string           // Variable pattern: x
    | TuplePat of Pattern list   // Tuple pattern: (p1, p2, ...)
    | WildcardPat                // Wildcard pattern: _
    // Phase 3 (v3.0): New pattern types for match expressions
    | ConsPat of Pattern * Pattern     // Cons pattern: h :: t
    | EmptyListPat                     // Empty list pattern: []
    | ConstPat of Constant             // Constant pattern: 1, true, false

/// Match clause: pattern -> expression
/// Phase 3 (v3.0)
and MatchClause = Pattern * Expr

/// Constant values for patterns
/// Phase 3 (v3.0)
and Constant =
    | IntConst of int
    | BoolConst of bool

/// Value type for evaluation results
/// Phase 4: Heterogeneous types (int and bool)
/// Phase 5: FunctionValue for first-class functions (mutual recursion with Expr, Env)
and Value =
    | IntValue of int
    | BoolValue of bool
    | FunctionValue of param: string * body: Expr * closure: Env
    | StringValue of string   // v2.0: String values
    | TupleValue of Value list  // v3.0: Tuple values
    | ListValue of Value list  // v3.0: List values

/// Environment mapping variable names to values
/// Phase 5: Defined here for mutual recursion with Value
and Env = Map<string, Value>
