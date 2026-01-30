module FunLang.Tests

open Expecto
open FSharp.Text.Lexing
open Ast
open Eval

/// Parse a string input and return the AST
let parse (input: string) : Expr =
    let lexbuf = LexBuffer<char>.FromString input
    Parser.start Lexer.tokenize lexbuf

/// Parse and evaluate a string expression
let evaluate (input: string) : int =
    input |> parse |> eval

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

[<EntryPoint>]
let main argv =
    runTestsWithCLIArgs [] argv phase2Tests
