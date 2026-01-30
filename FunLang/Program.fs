open System
open FSharp.Text.Lexing
open Ast
open Eval

/// Parse a string input and return the AST
let parse (input: string) : Expr =
    let lexbuf = LexBuffer<char>.FromString input
    Parser.start Lexer.tokenize lexbuf

[<EntryPoint>]
let main argv =
    match argv with
    | [| "--expr"; expr |] ->
        try
            let result = expr |> parse |> eval
            printfn "%d" result
            0
        with ex ->
            eprintfn "Error: %s" ex.Message
            1
    | [| "--help" |] | [| "-h" |] ->
        printfn "FunLang Interpreter"
        printfn ""
        printfn "Usage:"
        printfn "  funlang --expr <expression>  Evaluate an expression"
        printfn "  funlang --help               Show this help"
        printfn ""
        printfn "Examples:"
        printfn "  funlang --expr \"2 + 3 * 4\""
        printfn "  funlang --expr \"(2 + 3) * 4\""
        0
    | _ ->
        eprintfn "Usage: funlang --expr <expression>"
        eprintfn "       funlang --help"
        1
