module FunLang.Tests

open Expecto
open FsCheck
open FSharp.Text.Lexing
open Ast
open Eval

/// Parse a string input and return the AST
let parse (input: string) : Expr =
    let lexbuf = LexBuffer<char>.FromString input
    Parser.start Lexer.tokenize lexbuf

/// Parse and evaluate a string expression, returning Value
let evaluateToValue (input: string) : Value =
    input |> parse |> evalExpr

/// Parse and evaluate a string expression, extracting int (for backward compatibility)
let evaluate (input: string) : int =
    match evaluateToValue input with
    | IntValue n -> n
    | BoolValue _ -> failwith "Expected int but got bool"

/// Parse and evaluate a string expression, extracting bool
let evaluateToBool (input: string) : bool =
    match evaluateToValue input with
    | BoolValue b -> b
    | IntValue _ -> failwith "Expected bool but got int"

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
                Expect.equal ast (Let("x", Number 5, Var "x")) "AST should be Let(x, 5, Var x)"
            }
            test "parse nested let-in" {
                let ast = parse "let x = 1 in let y = 2 in x + y"
                let expected = Let("x", Number 1, Let("y", Number 2, Add(Var "x", Var "y")))
                Expect.equal ast expected "nested let AST structure"
            }
            test "parse variable reference" {
                let ast = parse "let x = 5 in x + 1"
                let expected = Let("x", Number 5, Add(Var "x", Number 1))
                Expect.equal ast expected "let with expression body AST"
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

[<Tests>]
let propertyTests =
    testList "Property-Based Tests" [
        testList "Number Properties" [
            testProperty "number evaluates to itself" <| fun (n: int) ->
                asInt (evalExpr (Number n)) = n

            testProperty "double negation is identity" <| fun (n: int) ->
                asInt (evalExpr (Negate(Negate(Number n)))) = n
        ]

        testList "Arithmetic Properties" [
            testProperty "addition is commutative" <| fun (a: int) (b: int) ->
                let left = asInt (evalExpr (Add(Number a, Number b)))
                let right = asInt (evalExpr (Add(Number b, Number a)))
                left = right

            testProperty "multiplication is commutative" <| fun (a: int) (b: int) ->
                let left = asInt (evalExpr (Multiply(Number a, Number b)))
                let right = asInt (evalExpr (Multiply(Number b, Number a)))
                left = right

            testProperty "zero is additive identity" <| fun (n: int) ->
                asInt (evalExpr (Add(Number n, Number 0))) = n

            testProperty "one is multiplicative identity" <| fun (n: int) ->
                asInt (evalExpr (Multiply(Number n, Number 1))) = n

            testProperty "subtraction of same number is zero" <| fun (n: int) ->
                asInt (evalExpr (Subtract(Number n, Number n))) = 0
        ]

        testList "Variable Properties" [
            testProperty "let binding returns bound value" <| fun (n: int) ->
                asInt (evalExpr (Let("x", Number n, Var "x"))) = n

            testProperty "let preserves expression value" <| fun (a: int) (b: int) ->
                let direct = a + b
                let viaLet = asInt (evalExpr (Let("x", Number a, Add(Var "x", Number b))))
                direct = viaLet

            testProperty "shadowing uses inner value" <| fun (outer: int) (inner: int) ->
                let expr = Let("x", Number outer, Let("x", Number inner, Var "x"))
                asInt (evalExpr expr) = inner

            testProperty "nested let uses correct scope" <| fun (a: int) (b: int) ->
                let expr = Let("x", Number a, Let("y", Number b, Add(Var "x", Var "y")))
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
                Expect.equal ast (If(Bool true, Number 1, Number 2)) "if AST"
            }
            test "parse comparison" {
                let ast = parse "5 > 3"
                Expect.equal ast (GreaterThan(Number 5, Number 3)) "comparison AST"
            }
            test "parse logical and" {
                let ast = parse "true && false"
                Expect.equal ast (And(Bool true, Bool false)) "and AST"
            }
            test "parse logical or" {
                let ast = parse "true || false"
                Expect.equal ast (Or(Bool true, Bool false)) "or AST"
            }
        ]
    ]

// ============================================================
// Entry Point
// ============================================================

[<EntryPoint>]
let main argv =
    runTestsWithCLIArgs [] argv <| testList "FunLang Tests" [
        phase2Tests
        phase3Tests
        phase4Tests
        propertyTests
        lexerTests
    ]
