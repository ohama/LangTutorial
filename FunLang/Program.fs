open System
open System.IO
open FSharp.Text.Lexing
open Argu
open Cli
open Ast
open Eval
open Format
open TypeCheck

/// Parse a string input and return the AST
let parse (input: string) : Expr =
    let lexbuf = LexBuffer<char>.FromString input
    Parser.start Lexer.tokenize lexbuf

[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<CliArgs>(
        programName = "funlang",
        errorHandler = ProcessExiter(colorizer = function
            | ErrorCode.HelpText -> None
            | _ -> Some ConsoleColor.Red))

    try
        let results = parser.Parse(argv, raiseOnUsage = false)

        // Load prelude for evaluation modes
        let initialEnv = Prelude.loadPrelude()

        // Check if help was requested
        if results.IsUsageRequested then
            printfn "%s" (parser.PrintUsage())
            0
        // --repl flag
        elif results.Contains Repl then
            Repl.startRepl()
        // --emit-tokens with --expr
        elif results.Contains Emit_Tokens && results.Contains Expr then
            let expr = results.GetResult Expr
            try
                let tokens = lex expr
                printfn "%s" (formatTokens tokens)
                0
            with ex ->
                eprintfn "Error: %s" ex.Message
                1
        // --emit-ast with --expr
        elif results.Contains Emit_Ast && results.Contains Expr then
            let expr = results.GetResult Expr
            try
                let ast = parse expr
                printfn "%A" ast
                0
            with ex ->
                eprintfn "Error: %s" ex.Message
                1
        // --emit-type with --expr
        elif results.Contains Emit_Type && results.Contains Expr then
            let expr = results.GetResult Expr
            try
                let ast = parse expr
                match typecheck ast with
                | Ok ty ->
                    printfn "%s" (Type.formatType ty)
                    0
                | Error msg ->
                    eprintfn "TypeError: %s" msg
                    1
            with ex ->
                eprintfn "Error: %s" ex.Message
                1
        // --emit-type with file
        elif results.Contains Emit_Type && results.Contains File then
            let filename = results.GetResult File
            if File.Exists filename then
                try
                    let input = File.ReadAllText filename
                    let ast = parse input
                    match typecheck ast with
                    | Ok ty ->
                        printfn "%s" (Type.formatType ty)
                        0
                    | Error msg ->
                        eprintfn "TypeError: %s" msg
                        1
                with ex ->
                    eprintfn "Error: %s" ex.Message
                    1
            else
                eprintfn "File not found: %s" filename
                1
        // --emit-tokens with file
        elif results.Contains Emit_Tokens && results.Contains File then
            let filename = results.GetResult File
            if File.Exists filename then
                try
                    let input = File.ReadAllText filename
                    let tokens = lex input
                    printfn "%s" (formatTokens tokens)
                    0
                with ex ->
                    eprintfn "Error: %s" ex.Message
                    1
            else
                eprintfn "File not found: %s" filename
                1
        // --emit-ast with file
        elif results.Contains Emit_Ast && results.Contains File then
            let filename = results.GetResult File
            if File.Exists filename then
                try
                    let input = File.ReadAllText filename
                    let ast = parse input
                    printfn "%A" ast
                    0
                with ex ->
                    eprintfn "Error: %s" ex.Message
                    1
            else
                eprintfn "File not found: %s" filename
                1
        // --expr only
        elif results.Contains Expr then
            let expr = results.GetResult Expr
            try
                let ast = parse expr
                // Type check first
                match typecheck ast with
                | Error msg ->
                    eprintfn "TypeError: %s" msg
                    1
                | Ok _ ->
                    // Type check passed, evaluate
                    let result = eval initialEnv ast
                    printfn "%s" (formatValue result)
                    0
            with ex ->
                eprintfn "Error: %s" ex.Message
                1
        // file only
        elif results.Contains File then
            let filename = results.GetResult File
            if File.Exists filename then
                try
                    let input = File.ReadAllText filename
                    let ast = parse input
                    // Type check first
                    match typecheck ast with
                    | Error msg ->
                        eprintfn "TypeError: %s" msg
                        1
                    | Ok _ ->
                        // Type check passed, evaluate
                        let result = eval initialEnv ast
                        printfn "%s" (formatValue result)
                        0
                with ex ->
                    eprintfn "Error: %s" ex.Message
                    1
            else
                eprintfn "File not found: %s" filename
                1
        // no arguments - start REPL
        else
            Repl.startRepl()
    with
    | :? ArguParseException as ex ->
        eprintfn "%s" ex.Message
        1
