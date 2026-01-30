open System
open System.IO
open FSharp.Text.Lexing
open Ast
open Eval
open Format

/// Parse a string input and return the AST
let parse (input: string) : Expr =
    let lexbuf = LexBuffer<char>.FromString input
    Parser.start Lexer.tokenize lexbuf

[<EntryPoint>]
let main argv =
    match argv with
    // Pattern 1: --emit-tokens --expr <expr>
    | [| "--emit-tokens"; "--expr"; expr |] ->
        try
            let tokens = lex expr
            printfn "%s" (formatTokens tokens)
            0
        with ex ->
            eprintfn "Error: %s" ex.Message
            1

    // Pattern 2: --emit-ast --expr <expr>
    | [| "--emit-ast"; "--expr"; expr |] ->
        try
            let ast = parse expr
            printfn "%A" ast
            0
        with ex ->
            eprintfn "Error: %s" ex.Message
            1

    // Pattern 3: --emit-type --expr <expr>
    | [| "--emit-type"; "--expr"; _ |] ->
        eprintfn "Error: Type checking not yet implemented. Reserved for future phase."
        1

    // Pattern 4: --emit-tokens <filename>
    | [| "--emit-tokens"; filename |] when File.Exists filename ->
        try
            let input = File.ReadAllText filename
            let tokens = lex input
            printfn "%s" (formatTokens tokens)
            0
        with ex ->
            eprintfn "Error: %s" ex.Message
            1

    // Pattern 5: --emit-ast <filename>
    | [| "--emit-ast"; filename |] when File.Exists filename ->
        try
            let input = File.ReadAllText filename
            let ast = parse input
            printfn "%A" ast
            0
        with ex ->
            eprintfn "Error: %s" ex.Message
            1

    // Pattern 6: --emit-type <filename>
    | [| "--emit-type"; filename |] when File.Exists filename ->
        eprintfn "Error: Type checking not yet implemented. Reserved for future phase."
        1

    // Pattern 7: --expr <expr>
    | [| "--expr"; expr |] ->
        try
            let result = expr |> parse |> evalExpr
            printfn "%d" result
            0
        with ex ->
            eprintfn "Error: %s" ex.Message
            1

    // Pattern 8: --help or -h
    | [| "--help" |] | [| "-h" |] ->
        printfn "FunLang Interpreter"
        printfn ""
        printfn "Usage:"
        printfn "  funlang --expr <expression>          Evaluate an expression"
        printfn "  funlang <filename>                   Evaluate a program from file"
        printfn "  funlang --emit-tokens --expr <expr>  Show tokens for expression"
        printfn "  funlang --emit-tokens <filename>     Show tokens for file"
        printfn "  funlang --emit-ast --expr <expr>     Show AST for expression"
        printfn "  funlang --emit-ast <filename>        Show AST for file"
        printfn "  funlang --emit-type --expr <expr>    (Reserved for future)"
        printfn "  funlang --emit-type <filename>       (Reserved for future)"
        printfn "  funlang --help                       Show this help"
        printfn ""
        printfn "Examples:"
        printfn "  funlang --expr \"2 + 3 * 4\""
        printfn "  funlang --expr \"(2 + 3) * 4\""
        printfn "  funlang program.fun"
        printfn "  funlang --emit-tokens --expr \"2 + 3\""
        printfn "  funlang --emit-ast --expr \"2 + 3\""
        0

    // Pattern 9: <filename> (with file exists guard)
    | [| filename |] when File.Exists filename ->
        try
            let input = File.ReadAllText filename
            let result = input |> parse |> evalExpr
            printfn "%d" result
            0
        with ex ->
            eprintfn "Error: %s" ex.Message
            1

    // Pattern 10: <filename> (file not found)
    | [| filename |] ->
        eprintfn "File not found: %s" filename
        1

    // Pattern 11: Wildcard - usage error
    | _ ->
        eprintfn "Usage: funlang --expr <expression>"
        eprintfn "       funlang <filename>"
        eprintfn "       funlang --help"
        1
