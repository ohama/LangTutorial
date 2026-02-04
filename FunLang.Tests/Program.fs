module FunLang.Tests

open Expecto
open FsCheck
open FSharp.Text.Lexing
open Ast
open Eval

/// Parse a string input and return the AST
let parse (input: string) : Expr =
    let lexbuf = LexBuffer<char>.FromString input
    Lexer.setInitialPos lexbuf "<test>"
    Parser.start Lexer.tokenize lexbuf

/// Parse and evaluate a string expression, returning Value
let evaluateToValue (input: string) : Value =
    input |> parse |> evalExpr

/// Parse and evaluate a string expression, extracting int (for backward compatibility)
let evaluate (input: string) : int =
    match evaluateToValue input with
    | IntValue n -> n
    | BoolValue _ -> failwith "Expected int but got bool"
    | FunctionValue _ -> failwith "Expected int but got function"
    | StringValue _ -> failwith "Expected int but got string"
    | TupleValue _ -> failwith "Expected int but got tuple"
    | ListValue _ -> failwith "Expected int but got list"

/// Parse and evaluate a string expression, extracting bool
let evaluateToBool (input: string) : bool =
    match evaluateToValue input with
    | BoolValue b -> b
    | IntValue _ -> failwith "Expected bool but got int"
    | FunctionValue _ -> failwith "Expected bool but got function"
    | StringValue _ -> failwith "Expected bool but got string"
    | TupleValue _ -> failwith "Expected bool but got tuple"
    | ListValue _ -> failwith "Expected bool but got list"

/// Check if value is a function
let isFunction (input: string) : bool =
    match evaluateToValue input with
    | FunctionValue _ -> true
    | _ -> false

/// Parse and evaluate a string expression, extracting string
let evaluateToString (input: string) : string =
    match evaluateToValue input with
    | StringValue s -> s
    | IntValue _ -> failwith "Expected string but got int"
    | BoolValue _ -> failwith "Expected string but got bool"
    | FunctionValue _ -> failwith "Expected string but got function"
    | TupleValue _ -> failwith "Expected string but got tuple"
    | ListValue _ -> failwith "Expected string but got list"

// ============================================================
// Phase 1: Comments (v2.0)
// ============================================================

/// Helper for tests - evaluates to Value (used in comment tests)
let parseAndEval (input: string) : Value =
    evaluateToValue input

[<Tests>]
let commentTests =
    testList "Comments" [
        testList "CMT-01: Single-line comments" [
            test "lexer skips // comment" {
                let result = parseAndEval "1 + 2 // ignored"
                Expect.equal result (IntValue 3) ""
            }

            test "// comment only line" {
                let result = parseAndEval "// comment\n42"
                Expect.equal result (IntValue 42) ""
            }
        ]

        testList "CMT-02: Block comments" [
            test "lexer skips (* ... *)" {
                let result = parseAndEval "(* comment *) 5"
                Expect.equal result (IntValue 5) ""
            }

            test "block comment in middle" {
                let result = parseAndEval "1 + (* mid *) 2"
                Expect.equal result (IntValue 3) ""
            }

            test "multiline block comment" {
                let result = parseAndEval "(* line1\nline2 *) 7"
                Expect.equal result (IntValue 7) ""
            }
        ]

        testList "CMT-03: Nested comments" [
            test "one level nesting" {
                let result = parseAndEval "(* outer (* inner *) *) 10"
                Expect.equal result (IntValue 10) ""
            }

            test "multiple levels nesting" {
                let result = parseAndEval "(* 1 (* 2 (* 3 *) 2 *) 1 *) 20"
                Expect.equal result (IntValue 20) ""
            }
        ]

        testList "CMT-04: Error handling" [
            test "unterminated comment throws" {
                Expect.throws
                    (fun () -> parseAndEval "(* unclosed" |> ignore)
                    "should throw on unterminated comment"
            }
        ]

        testList "Non-interference" [
            test "division still works" {
                let result = parseAndEval "10 / 2"
                Expect.equal result (IntValue 5) "slash should not be confused with comment"
            }

            test "parentheses still work" {
                let result = parseAndEval "(1 + 2) * 3"
                Expect.equal result (IntValue 9) "parens should not trigger block comment"
            }
        ]
    ]

// ============================================================
// Phase 2: Strings (v2.0)
// ============================================================

[<Tests>]
let stringTests =
    testList "Strings" [
        testList "STR-01: String Literals" [
            test "simple string" {
                let result = parseAndEval "\"hello\""
                Expect.equal result (StringValue "hello") ""
            }
            test "empty string" {
                let result = parseAndEval "\"\""
                Expect.equal result (StringValue "") ""
            }
            test "string with spaces" {
                let result = parseAndEval "\"hello world\""
                Expect.equal result (StringValue "hello world") ""
            }
        ]

        testList "STR-02 to STR-05: Escape Sequences" [
            test "escape backslash" {
                let result = parseAndEval "\"a\\\\b\""
                Expect.equal result (StringValue "a\\b") ""
            }
            test "escape quote" {
                let result = parseAndEval "\"say \\\"hi\\\"\""
                Expect.equal result (StringValue "say \"hi\"") ""
            }
            test "escape newline" {
                let result = parseAndEval "\"line1\\nline2\""
                Expect.equal result (StringValue "line1\nline2") ""
            }
            test "escape tab" {
                let result = parseAndEval "\"col1\\tcol2\""
                Expect.equal result (StringValue "col1\tcol2") ""
            }
            test "multiple escapes" {
                let result = parseAndEval "\"a\\tb\\nc\""
                Expect.equal result (StringValue "a\tb\nc") ""
            }
        ]

        testList "STR-06: String Concatenation" [
            test "concat two strings" {
                let result = parseAndEval "\"hello\" + \" world\""
                Expect.equal result (StringValue "hello world") ""
            }
            test "concat three strings" {
                let result = parseAndEval "\"a\" + \"b\" + \"c\""
                Expect.equal result (StringValue "abc") ""
            }
            test "concat with empty" {
                let result = parseAndEval "\"\" + \"text\""
                Expect.equal result (StringValue "text") ""
            }
            test "concat empty with empty" {
                let result = parseAndEval "\"\" + \"\""
                Expect.equal result (StringValue "") ""
            }
        ]

        testList "STR-07 and STR-08: String Comparison" [
            test "equal true" {
                let result = evaluateToBool "\"abc\" = \"abc\""
                Expect.isTrue result ""
            }
            test "equal false" {
                let result = evaluateToBool "\"abc\" = \"xyz\""
                Expect.isFalse result ""
            }
            test "not equal true" {
                let result = evaluateToBool "\"abc\" <> \"xyz\""
                Expect.isTrue result ""
            }
            test "not equal false" {
                let result = evaluateToBool "\"abc\" <> \"abc\""
                Expect.isFalse result ""
            }
            test "empty string equal" {
                let result = evaluateToBool "\"\" = \"\""
                Expect.isTrue result ""
            }
        ]

        testList "STR-10 to STR-12: Error Handling" [
            test "unterminated string throws" {
                Expect.throws
                    (fun () -> parseAndEval "\"unclosed" |> ignore)
                    "should throw on unterminated string"
            }
            test "string + int throws type error" {
                Expect.throws
                    (fun () -> parseAndEval "\"text\" + 1" |> ignore)
                    "should throw on string + int"
            }
            test "int + string throws type error" {
                Expect.throws
                    (fun () -> parseAndEval "1 + \"text\"" |> ignore)
                    "should throw on int + string"
            }
            test "string = int throws type error" {
                Expect.throws
                    (fun () -> parseAndEval "\"text\" = 1" |> ignore)
                    "should throw on string = int"
            }
        ]

        testList "Integration" [
            test "string in let binding" {
                let result = parseAndEval "let s = \"hello\" in s"
                Expect.equal result (StringValue "hello") ""
            }
            test "string concat in let" {
                let result = parseAndEval "let s = \"hello\" in s + \" world\""
                Expect.equal result (StringValue "hello world") ""
            }
            test "string in if condition" {
                let result = evaluate "if \"a\" = \"a\" then 1 else 0"
                Expect.equal result 1 ""
            }
            test "string result from if" {
                let result = parseAndEval "if true then \"yes\" else \"no\""
                Expect.equal result (StringValue "yes") ""
            }
        ]

        testList "Lexer" [
            test "tokenizes string" {
                let lexbuf = LexBuffer<char>.FromString "\"hello\""
                let token = Lexer.tokenize lexbuf
                Expect.equal token (Parser.STRING "hello") ""
            }
            test "tokenizes empty string" {
                let lexbuf = LexBuffer<char>.FromString "\"\""
                let token = Lexer.tokenize lexbuf
                Expect.equal token (Parser.STRING "") ""
            }
        ]

        testList "AST" [
            test "parse string literal" {
                let ast = parse "\"hello\""
                // Compare ignoring span
                match ast with
                | String ("hello", _) -> ()
                | _ -> failtest "Expected String(\"hello\", _)"
            }
            test "parse string concat" {
                let ast = parse "\"a\" + \"b\""
                // Compare ignoring span
                match ast with
                | Add (String ("a", _), String ("b", _), _) -> ()
                | _ -> failtest "Expected Add(String \"a\", String \"b\", _)"
            }
        ]
    ]

// ============================================================
// Phase 2: Arithmetic Expressions
// ============================================================

[<Tests>]
let phase2Tests =
    testList "Phase 2: Arithmetic Expressions" [
        testList "EXPR-01: Basic Operations" [
            test "number literal" {
                Expect.equal (evaluate "42") 42 "42 should evaluate to 42"
            }
            test "addition" {
                Expect.equal (evaluate "2 + 3") 5 "2 + 3 should be 5"
            }
            test "subtraction" {
                Expect.equal (evaluate "10 - 4") 6 "10 - 4 should be 6"
            }
            test "multiplication" {
                Expect.equal (evaluate "3 * 4") 12 "3 * 4 should be 12"
            }
            test "division" {
                Expect.equal (evaluate "20 / 4") 5 "20 / 4 should be 5"
            }
        ]

        testList "EXPR-02: Operator Precedence" [
            test "multiplication before addition" {
                Expect.equal (evaluate "2 + 3 * 4") 14 "2 + 3 * 4 should be 14"
            }
            test "division before subtraction" {
                Expect.equal (evaluate "10 - 6 / 2") 7 "10 - 6 / 2 should be 7"
            }
            test "mixed precedence" {
                Expect.equal (evaluate "2 * 3 + 4 * 5") 26 "2 * 3 + 4 * 5 should be 26"
            }
        ]

        testList "EXPR-03: Parentheses" [
            test "parentheses override precedence" {
                Expect.equal (evaluate "(2 + 3) * 4") 20 "(2 + 3) * 4 should be 20"
            }
            test "nested parentheses" {
                Expect.equal (evaluate "((2 + 3) * (4 - 1))") 15 "((2 + 3) * (4 - 1)) should be 15"
            }
            test "parentheses with division" {
                Expect.equal (evaluate "(10 + 2) / 3") 4 "(10 + 2) / 3 should be 4"
            }
        ]

        testList "EXPR-04: Unary Minus" [
            test "negative number" {
                Expect.equal (evaluate "-5") -5 "-5 should be -5"
            }
            test "negative in expression" {
                Expect.equal (evaluate "-5 + 3") -2 "-5 + 3 should be -2"
            }
            test "double negation" {
                Expect.equal (evaluate "--5") 5 "--5 should be 5"
            }
            test "negate expression" {
                Expect.equal (evaluate "-(2 + 3)") -5 "-(2 + 3) should be -5"
            }
            test "negate in multiplication" {
                Expect.equal (evaluate "-3 * 2") -6 "-3 * 2 should be -6"
            }
        ]

        testList "Left Associativity" [
            test "subtraction is left associative" {
                Expect.equal (evaluate "2 - 3 - 4") -5 "2 - 3 - 4 should be (2 - 3) - 4 = -5"
            }
            test "division is left associative" {
                Expect.equal (evaluate "24 / 4 / 2") 3 "24 / 4 / 2 should be (24 / 4) / 2 = 3"
            }
            test "mixed left associativity" {
                Expect.equal (evaluate "10 / 2 - 3") 2 "10 / 2 - 3 should be 2"
            }
        ]
    ]

// ============================================================
// Phase 3: Variables & Binding
// ============================================================

[<Tests>]
let phase3Tests =
    testList "Phase 3: Variables & Binding" [
        testList "VAR-01: Let Binding" [
            test "basic let binding" {
                Expect.equal (evaluate "let x = 5 in x") 5 "let x = 5 in x should be 5"
            }
            test "let with expression binding" {
                Expect.equal (evaluate "let x = 2 + 3 in x") 5 "let x = 2 + 3 in x should be 5"
            }
            test "let with expression body" {
                Expect.equal (evaluate "let x = 5 in x + 1") 6 "let x = 5 in x + 1 should be 6"
            }
            test "let with complex body" {
                Expect.equal (evaluate "let x = 10 in x * 2 + 5") 25 "let x = 10 in x * 2 + 5 should be 25"
            }
        ]

        testList "VAR-02: Variable Reference" [
            test "variable in multiplication" {
                Expect.equal (evaluate "let x = 3 in x * 4") 12 "let x = 3 in x * 4 should be 12"
            }
            test "multiple uses of same variable" {
                Expect.equal (evaluate "let x = 2 in x + x") 4 "let x = 2 in x + x should be 4"
            }
            test "variable in complex expression" {
                Expect.equal (evaluate "let x = 10 in x / 2 - 3") 2 "let x = 10 in x / 2 - 3 should be 2"
            }
            test "variable in both sides" {
                Expect.equal (evaluate "let x = 5 in x * x") 25 "let x = 5 in x * x should be 25"
            }
        ]

        testList "VAR-03: Local Scope (let-in)" [
            test "nested let" {
                Expect.equal (evaluate "let x = 1 in let y = 2 in x + y") 3
                    "let x = 1 in let y = 2 in x + y should be 3"
            }
            test "shadowing" {
                Expect.equal (evaluate "let x = 1 in let x = 2 in x") 2
                    "let x = 1 in let x = 2 in x should be 2 (shadowing)"
            }
            test "inner uses outer" {
                Expect.equal (evaluate "let x = 5 in let y = x + 1 in y") 6
                    "let x = 5 in let y = x + 1 in y should be 6"
            }
            test "inner scope doesn't affect outer" {
                // let x = 1 in (let y = x + 1 in y) + x = 2 + 1 = 3
                Expect.equal (evaluate "let x = 1 in (let y = x + 1 in y) + x") 3
                    "inner let doesn't affect outer scope"
            }
            test "parenthesized let" {
                Expect.equal (evaluate "let x = 1 in (let y = x + 1 in y) * 2") 4
                    "parenthesized let should be 4"
            }
            test "deep nesting" {
                Expect.equal (evaluate "let a = 1 in let b = 2 in let c = 3 in a + b + c") 6
                    "three-level nesting should work"
            }
        ]

        testList "Error Cases" [
            test "undefined variable throws" {
                Expect.throws (fun () -> evaluate "x" |> ignore)
                    "undefined variable should throw"
            }
            test "undefined variable in expression throws" {
                Expect.throws (fun () -> evaluate "y + 1" |> ignore)
                    "undefined variable in expression should throw"
            }
        ]

        testList "AST Construction" [
            test "parse let-in correctly" {
                let ast = parse "let x = 5 in x"
                match ast with
                | Let ("x", Number (5, _), Var ("x", _), _) -> ()
                | _ -> failtest "AST should be Let(x, Number 5, Var x)"
            }
            test "parse nested let-in" {
                let ast = parse "let x = 1 in let y = 2 in x + y"
                match ast with
                | Let ("x", Number (1, _), Let ("y", Number (2, _), Add (Var ("x", _), Var ("y", _), _), _), _) -> ()
                | _ -> failtest "nested let AST structure"
            }
            test "parse variable reference" {
                let ast = parse "let x = 5 in x + 1"
                match ast with
                | Let ("x", Number (5, _), Add (Var ("x", _), Number (1, _), _), _) -> ()
                | _ -> failtest "let with expression body AST"
            }
        ]
    ]

// ============================================================
// Property-Based Tests (FsCheck)
// ============================================================

/// Helper to extract int from Value for property tests
let asInt (v: Value) : int =
    match v with
    | IntValue n -> n
    | BoolValue _ -> failwith "Expected int"
    | FunctionValue _ -> failwith "Expected int"
    | StringValue _ -> failwith "Expected int"
    | TupleValue _ -> failwith "Expected int"
    | ListValue _ -> failwith "Expected int"

[<Tests>]
let propertyTests =
    testList "Property-Based Tests" [
        testList "Number Properties" [
            testProperty "number evaluates to itself" <| fun (n: int) ->
                asInt (evalExpr (Number (n, unknownSpan))) = n

            testProperty "double negation is identity" <| fun (n: int) ->
                asInt (evalExpr (Negate(Negate(Number (n, unknownSpan), unknownSpan), unknownSpan))) = n
        ]

        testList "Arithmetic Properties" [
            testProperty "addition is commutative" <| fun (a: int) (b: int) ->
                let left = asInt (evalExpr (Add(Number (a, unknownSpan), Number (b, unknownSpan), unknownSpan)))
                let right = asInt (evalExpr (Add(Number (b, unknownSpan), Number (a, unknownSpan), unknownSpan)))
                left = right

            testProperty "multiplication is commutative" <| fun (a: int) (b: int) ->
                let left = asInt (evalExpr (Multiply(Number (a, unknownSpan), Number (b, unknownSpan), unknownSpan)))
                let right = asInt (evalExpr (Multiply(Number (b, unknownSpan), Number (a, unknownSpan), unknownSpan)))
                left = right

            testProperty "zero is additive identity" <| fun (n: int) ->
                asInt (evalExpr (Add(Number (n, unknownSpan), Number (0, unknownSpan), unknownSpan))) = n

            testProperty "one is multiplicative identity" <| fun (n: int) ->
                asInt (evalExpr (Multiply(Number (n, unknownSpan), Number (1, unknownSpan), unknownSpan))) = n

            testProperty "subtraction of same number is zero" <| fun (n: int) ->
                asInt (evalExpr (Subtract(Number (n, unknownSpan), Number (n, unknownSpan), unknownSpan))) = 0
        ]

        testList "Variable Properties" [
            testProperty "let binding returns bound value" <| fun (n: int) ->
                asInt (evalExpr (Let("x", Number (n, unknownSpan), Var ("x", unknownSpan), unknownSpan))) = n

            testProperty "let preserves expression value" <| fun (a: int) (b: int) ->
                let direct = a + b
                let viaLet = asInt (evalExpr (Let("x", Number (a, unknownSpan), Add(Var ("x", unknownSpan), Number (b, unknownSpan), unknownSpan), unknownSpan)))
                direct = viaLet

            testProperty "shadowing uses inner value" <| fun (outer: int) (inner: int) ->
                let expr = Let("x", Number (outer, unknownSpan), Let("x", Number (inner, unknownSpan), Var ("x", unknownSpan), unknownSpan), unknownSpan)
                asInt (evalExpr expr) = inner

            testProperty "nested let uses correct scope" <| fun (a: int) (b: int) ->
                let expr = Let("x", Number (a, unknownSpan), Let("y", Number (b, unknownSpan), Add(Var ("x", unknownSpan), Var ("y", unknownSpan), unknownSpan), unknownSpan), unknownSpan)
                asInt (evalExpr expr) = a + b
        ]
    ]

// ============================================================
// Lexer Tests
// ============================================================

[<Tests>]
let lexerTests =
    testList "Lexer" [
        test "tokenizes number" {
            let lexbuf = LexBuffer<char>.FromString "42"
            let token = Lexer.tokenize lexbuf
            Expect.equal token (Parser.NUMBER 42) "should be NUMBER(42)"
        }

        test "tokenizes let keyword" {
            let lexbuf = LexBuffer<char>.FromString "let"
            let token = Lexer.tokenize lexbuf
            Expect.equal token Parser.LET "should be LET"
        }

        test "tokenizes in keyword" {
            let lexbuf = LexBuffer<char>.FromString "in"
            let token = Lexer.tokenize lexbuf
            Expect.equal token Parser.IN "should be IN"
        }

        test "tokenizes identifier" {
            let lexbuf = LexBuffer<char>.FromString "foo"
            let token = Lexer.tokenize lexbuf
            Expect.equal token (Parser.IDENT "foo") "should be IDENT(foo)"
        }

        test "distinguishes let from identifier starting with let" {
            let lexbuf = LexBuffer<char>.FromString "letter"
            let token = Lexer.tokenize lexbuf
            Expect.equal token (Parser.IDENT "letter") "letter is IDENT, not LET"
        }

        test "distinguishes in from identifier starting with in" {
            let lexbuf = LexBuffer<char>.FromString "inner"
            let token = Lexer.tokenize lexbuf
            Expect.equal token (Parser.IDENT "inner") "inner is IDENT, not IN"
        }

        test "tokenizes equals" {
            let lexbuf = LexBuffer<char>.FromString "="
            let token = Lexer.tokenize lexbuf
            Expect.equal token Parser.EQUALS "should be EQUALS"
        }

        test "tokenizes identifier with underscore" {
            let lexbuf = LexBuffer<char>.FromString "_foo"
            let token = Lexer.tokenize lexbuf
            Expect.equal token (Parser.IDENT "_foo") "should be IDENT(_foo)"
        }

        test "tokenizes identifier with numbers" {
            let lexbuf = LexBuffer<char>.FromString "x1"
            let token = Lexer.tokenize lexbuf
            Expect.equal token (Parser.IDENT "x1") "should be IDENT(x1)"
        }
    ]

// ============================================================
// Phase 4: Control Flow
// ============================================================

[<Tests>]
let phase4Tests =
    testList "Phase 4: Control Flow" [
        testList "CTRL-01: If-Then-Else" [
            test "if true evaluates then branch" {
                Expect.equal (evaluate "if true then 1 else 2") 1 "if true then 1 else 2 should be 1"
            }
            test "if false evaluates else branch" {
                Expect.equal (evaluate "if false then 1 else 2") 2 "if false then 1 else 2 should be 2"
            }
            test "nested if expressions" {
                Expect.equal (evaluate "if 5 > 3 then if 2 < 4 then 100 else 50 else 0") 100
                    "nested if should work"
            }
            test "if with arithmetic in condition" {
                Expect.equal (evaluate "if 2 + 3 > 4 then 10 else 20") 10
                    "if with arithmetic condition"
            }
            test "if with let binding" {
                Expect.equal (evaluate "let x = 10 in if x > 5 then x else 0") 10
                    "if with let binding"
            }
        ]

        testList "CTRL-02: Boolean Literals" [
            test "true evaluates to true" {
                Expect.isTrue (evaluateToBool "true") "true should be true"
            }
            test "false evaluates to false" {
                Expect.isFalse (evaluateToBool "false") "false should be false"
            }
            test "boolean in let binding" {
                Expect.isTrue (evaluateToBool "let b = true in b") "let b = true in b"
            }
        ]

        testList "CTRL-03: Comparison Operators" [
            test "less than true" {
                Expect.equal (evaluate "if 3 < 5 then 1 else 0") 1 "3 < 5 should be true"
            }
            test "less than false" {
                Expect.equal (evaluate "if 5 < 3 then 1 else 0") 0 "5 < 3 should be false"
            }
            test "greater than" {
                Expect.equal (evaluate "if 5 > 3 then 10 else 20") 10 "5 > 3 should be true"
            }
            test "less or equal true" {
                Expect.equal (evaluate "if 3 <= 3 then 1 else 0") 1 "3 <= 3 should be true"
            }
            test "less or equal false" {
                Expect.equal (evaluate "if 4 <= 3 then 1 else 0") 0 "4 <= 3 should be false"
            }
            test "greater or equal true" {
                Expect.equal (evaluate "if 5 >= 5 then 1 else 0") 1 "5 >= 5 should be true"
            }
            test "greater or equal false" {
                Expect.equal (evaluate "if 3 >= 5 then 1 else 0") 0 "3 >= 5 should be false"
            }
            test "equal integers" {
                Expect.equal (evaluate "if 5 = 5 then 1 else 0") 1 "5 = 5 should be true"
            }
            test "equal integers false" {
                Expect.equal (evaluate "if 5 = 3 then 1 else 0") 0 "5 = 3 should be false"
            }
            test "not equal true" {
                Expect.equal (evaluate "if 5 <> 3 then 1 else 0") 1 "5 <> 3 should be true"
            }
            test "not equal false" {
                Expect.equal (evaluate "if 5 <> 5 then 1 else 0") 0 "5 <> 5 should be false"
            }
        ]

        testList "CTRL-04: Logical Operators" [
            test "and true true" {
                Expect.equal (evaluate "if true && true then 1 else 0") 1 "true && true"
            }
            test "and true false" {
                Expect.equal (evaluate "if true && false then 1 else 0") 0 "true && false"
            }
            test "and false true" {
                Expect.equal (evaluate "if false && true then 1 else 0") 0 "false && true"
            }
            test "or true false" {
                Expect.equal (evaluate "if true || false then 1 else 0") 1 "true || false"
            }
            test "or false true" {
                Expect.equal (evaluate "if false || true then 1 else 0") 1 "false || true"
            }
            test "or false false" {
                Expect.equal (evaluate "if false || false then 1 else 0") 0 "false || false"
            }
            test "complex condition with and" {
                Expect.equal (evaluate "let x = 10 in let y = 20 in if x = 10 && y = 20 then 1 else 0") 1
                    "x = 10 && y = 20"
            }
            test "complex condition with or" {
                Expect.equal (evaluate "if 1 > 2 || 3 < 4 then 1 else 0") 1 "1 > 2 || 3 < 4"
            }
        ]

        testList "Type Errors" [
            test "if condition must be bool" {
                Expect.throws (fun () -> evaluate "if 1 then 2 else 3" |> ignore)
                    "if condition must be bool"
            }
            test "arithmetic requires int" {
                Expect.throws (fun () -> evaluate "true + 1" |> ignore)
                    "arithmetic requires int"
            }
            test "comparison requires int" {
                Expect.throws (fun () -> evaluateToBool "true < false" |> ignore)
                    "comparison requires int"
            }
            test "and requires bool" {
                Expect.throws (fun () -> evaluateToBool "1 && 2" |> ignore)
                    "and requires bool"
            }
        ]

        testList "AST Construction" [
            test "parse if-then-else" {
                let ast = parse "if true then 1 else 2"
                match ast with
                | If (Bool (true, _), Number (1, _), Number (2, _), _) -> ()
                | _ -> failtest "if AST"
            }
            test "parse comparison" {
                let ast = parse "5 > 3"
                match ast with
                | GreaterThan (Number (5, _), Number (3, _), _) -> ()
                | _ -> failtest "comparison AST"
            }
            test "parse logical and" {
                let ast = parse "true && false"
                match ast with
                | And (Bool (true, _), Bool (false, _), _) -> ()
                | _ -> failtest "and AST"
            }
            test "parse logical or" {
                let ast = parse "true || false"
                match ast with
                | Or (Bool (true, _), Bool (false, _), _) -> ()
                | _ -> failtest "or AST"
            }
        ]
    ]

// ============================================================
// Phase 5: Functions & Abstraction
// ============================================================

[<Tests>]
let phase5Tests =
    testList "Phase 5: Functions & Abstraction" [
        testList "FUNC-01: Function Definition" [
            test "simple lambda" {
                Expect.isTrue (isFunction "fun x -> x") "fun x -> x should be a function"
            }
            test "lambda with body expression" {
                Expect.isTrue (isFunction "fun x -> x + 1") "fun x -> x + 1 should be a function"
            }
            test "function bound to variable" {
                Expect.equal (evaluate "let f = fun x -> x + 1 in f 5") 6
                    "let f = fun x -> x + 1 in f 5 should be 6"
            }
            test "function returning function" {
                Expect.isTrue (isFunction "fun x -> fun y -> x + y")
                    "fun x -> fun y -> x + y should be a function"
            }
        ]

        testList "FUNC-02: Function Application" [
            test "simple application" {
                Expect.equal (evaluate "let f = fun x -> x + 1 in f 5") 6
                    "function application should work"
            }
            test "application with expression argument" {
                Expect.equal (evaluate "let f = fun x -> x * 2 in f (3 + 4)") 14
                    "f (3 + 4) should be 14"
            }
            test "curried function" {
                Expect.equal (evaluate "let add = fun x -> fun y -> x + y in add 3 4") 7
                    "curried function add 3 4 should be 7"
            }
            test "partial application" {
                Expect.equal (evaluate "let add = fun x -> fun y -> x + y in let add5 = add 5 in add5 10") 15
                    "partial application should work"
            }
            test "nested function calls" {
                Expect.equal (evaluate "let double = fun x -> x * 2 in double (double 3)") 12
                    "double (double 3) should be 12"
            }
            test "application is left associative" {
                // f x y = (f x) y
                Expect.equal (evaluate "let add = fun x -> fun y -> x + y in add 2 3") 5
                    "add 2 3 = (add 2) 3 should be 5"
            }
        ]

        testList "FUNC-03: Recursive Functions" [
            test "simple recursion with let rec" {
                Expect.equal (evaluate "let rec f x = x in f 42") 42
                    "simple rec function"
            }
            test "factorial" {
                Expect.equal (evaluate "let rec fact n = if n <= 1 then 1 else n * fact (n - 1) in fact 5") 120
                    "fact 5 should be 120"
            }
            test "factorial of 0" {
                Expect.equal (evaluate "let rec fact n = if n <= 1 then 1 else n * fact (n - 1) in fact 0") 1
                    "fact 0 should be 1"
            }
            test "fibonacci" {
                Expect.equal (evaluate "let rec fib n = if n <= 1 then n else fib (n - 1) + fib (n - 2) in fib 6") 8
                    "fib 6 should be 8"
            }
            test "countdown" {
                Expect.equal (evaluate "let rec countdown n = if n <= 0 then 0 else countdown (n - 1) in countdown 10") 0
                    "countdown 10 should be 0"
            }
            test "sum to n" {
                Expect.equal (evaluate "let rec sum n = if n <= 0 then 0 else n + sum (n - 1) in sum 5") 15
                    "sum 5 should be 15"
            }
        ]

        testList "FUNC-04: Closures" [
            test "closure captures outer variable" {
                Expect.equal (evaluate "let x = 10 in let f = fun y -> x + y in f 5") 15
                    "closure should capture x = 10"
            }
            test "closure captures at definition time" {
                // x is captured when f is defined (x = 10), not when called
                Expect.equal (evaluate "let x = 10 in let f = fun y -> x + y in let x = 99 in f 5") 15
                    "closure captures at definition time, not call time"
            }
            test "nested closure" {
                Expect.equal (evaluate "let a = 1 in let b = 2 in let f = fun c -> a + b + c in f 3") 6
                    "nested closure captures multiple variables"
            }
            test "make adder (closure factory)" {
                Expect.equal (evaluate "let makeAdder = fun x -> fun y -> x + y in let add5 = makeAdder 5 in add5 3") 8
                    "closure factory pattern"
            }
            test "closure value is captured" {
                Expect.equal (evaluate "let x = 100 in let f = fun y -> x + y in let g = fun z -> f z + 1 in g 5") 106
                    "closure within closure"
            }
        ]

        testList "Lexer - Function Tokens" [
            test "tokenizes fun keyword" {
                let lexbuf = LexBuffer<char>.FromString "fun"
                let token = Lexer.tokenize lexbuf
                Expect.equal token Parser.FUN "should be FUN"
            }
            test "tokenizes rec keyword" {
                let lexbuf = LexBuffer<char>.FromString "rec"
                let token = Lexer.tokenize lexbuf
                Expect.equal token Parser.REC "should be REC"
            }
            test "tokenizes arrow" {
                let lexbuf = LexBuffer<char>.FromString "->"
                let token = Lexer.tokenize lexbuf
                Expect.equal token Parser.ARROW "should be ARROW"
            }
            test "distinguishes fun from identifier" {
                let lexbuf = LexBuffer<char>.FromString "funny"
                let token = Lexer.tokenize lexbuf
                Expect.equal token (Parser.IDENT "funny") "funny is IDENT, not FUN"
            }
            test "distinguishes rec from identifier" {
                let lexbuf = LexBuffer<char>.FromString "record"
                let token = Lexer.tokenize lexbuf
                Expect.equal token (Parser.IDENT "record") "record is IDENT, not REC"
            }
        ]

        testList "AST Construction" [
            test "parse lambda" {
                let ast = parse "fun x -> x"
                match ast with
                | Lambda ("x", Var ("x", _), _) -> ()
                | _ -> failtest "lambda AST"
            }
            test "parse lambda with expression body" {
                let ast = parse "fun x -> x + 1"
                match ast with
                | Lambda ("x", Add (Var ("x", _), Number (1, _), _), _) -> ()
                | _ -> failtest "lambda with expression body"
            }
            test "parse nested lambda" {
                let ast = parse "fun x -> fun y -> x + y"
                match ast with
                | Lambda ("x", Lambda ("y", Add (Var ("x", _), Var ("y", _), _), _), _) -> ()
                | _ -> failtest "nested lambda"
            }
            test "parse application" {
                let ast = parse "f 5"
                match ast with
                | App (Var ("f", _), Number (5, _), _) -> ()
                | _ -> failtest "application AST"
            }
            test "parse chained application" {
                let ast = parse "f 1 2"
                match ast with
                | App (App (Var ("f", _), Number (1, _), _), Number (2, _), _) -> ()
                | _ -> failtest "chained application is left-associative"
            }
            test "parse let rec" {
                let ast = parse "let rec f x = x in f 1"
                match ast with
                | LetRec ("f", "x", Var ("x", _), App (Var ("f", _), Number (1, _), _), _) -> ()
                | _ -> failtest "let rec AST"
            }
        ]

        testList "Edge Cases" [
            test "function subtraction vs application" {
                // f - 1 is subtraction, not application
                Expect.equal (evaluate "let f = 10 in f - 1") 9
                    "f - 1 should be subtraction when f is int"
            }
            test "function with negative argument needs parens" {
                Expect.equal (evaluate "let f = fun x -> x in f (-5)") -5
                    "f (-5) applies -5 to f"
            }
            test "identity function" {
                Expect.equal (evaluate "let id = fun x -> x in id 42") 42
                    "identity function"
            }
            test "constant function" {
                Expect.equal (evaluate "let const = fun x -> fun y -> x in const 5 10") 5
                    "constant function returns first argument"
            }
        ]
    ]

// ============================================================
// Entry Point
// ============================================================

[<EntryPoint>]
let main argv =
    runTestsWithCLIArgs [] argv <| testList "FunLang Tests" [
        TypeTests.typeTests
        ElaborateTests.elaborateTests
        UnifyTests.unifyTests
        ReplTests.replEvalTests
        ReplTests.cliTests
        InferTests.inferTests
        TypeCheckTests.typeCheckTests
        BidirTests.annotationSynthesisTests
        BidirTests.annotationErrorTests
        commentTests
        stringTests
        phase2Tests
        phase3Tests
        phase4Tests
        phase5Tests
        propertyTests
        lexerTests
    ]
