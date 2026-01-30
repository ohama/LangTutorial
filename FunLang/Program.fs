open System
open FSharp.Text.Lexing
open Ast

/// Parse a string input and return the AST
let parse (input: string) : Expr =
    let lexbuf = LexBuffer<char>.FromString input
    Parser.start Lexer.tokenize lexbuf

[<EntryPoint>]
let main argv =
    // Test input - just a number for Phase 1
    let testInput = "42"

    printfn "FunLang Interpreter - Phase 1: Foundation"
    printfn "========================================="
    printfn ""
    printfn "Input: %s" testInput

    try
        let ast = parse testInput
        printfn "AST: %A" ast
        printfn ""
        printfn "Pipeline successful!"
        0  // Success exit code
    with
    | ex ->
        printfn "Error: %s" ex.Message
        1  // Error exit code
