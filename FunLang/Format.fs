module Format

open System
open FSharp.Text.Lexing

/// Format a single token as a string
let formatToken (token: Parser.token) : string =
    match token with
    | Parser.NUMBER n -> sprintf "NUMBER(%d)" n
    | Parser.IDENT s -> sprintf "IDENT(%s)" s
    | Parser.PLUS -> "PLUS"
    | Parser.MINUS -> "MINUS"
    | Parser.STAR -> "STAR"
    | Parser.SLASH -> "SLASH"
    | Parser.LPAREN -> "LPAREN"
    | Parser.RPAREN -> "RPAREN"
    | Parser.LET -> "LET"
    | Parser.IN -> "IN"
    | Parser.EQUALS -> "EQUALS"
    // Phase 4: Control flow tokens
    | Parser.TRUE -> "TRUE"
    | Parser.FALSE -> "FALSE"
    | Parser.IF -> "IF"
    | Parser.THEN -> "THEN"
    | Parser.ELSE -> "ELSE"
    // Phase 4: Comparison operator tokens
    | Parser.LT -> "LT"
    | Parser.GT -> "GT"
    | Parser.LE -> "LE"
    | Parser.GE -> "GE"
    | Parser.NE -> "NE"
    // Phase 4: Logical operator tokens
    | Parser.AND -> "AND"
    | Parser.OR -> "OR"
    | Parser.EOF -> "EOF"

/// Format a list of tokens as a space-separated string
let formatTokens (tokens: Parser.token list) : string =
    tokens |> List.map formatToken |> String.concat " "

/// Tokenize input string into a token list
let lex (input: string) : Parser.token list =
    let lexbuf = LexBuffer<char>.FromString input
    let rec loop acc =
        let token = Lexer.tokenize lexbuf
        match token with
        | Parser.EOF -> List.rev (Parser.EOF :: acc)
        | t -> loop (t :: acc)
    loop []
