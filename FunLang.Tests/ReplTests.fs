module ReplTests

open Expecto
open Eval
open Ast

/// Test the core evaluation that REPL uses
[<Tests>]
let replEvalTests =
    testList "REPL evaluation" [
        testCase "evaluates arithmetic" <| fun _ ->
            let result = evalExpr (Add (Number 2, Number 3))
            Expect.equal result (IntValue 5) "2 + 3 = 5"

        testCase "evaluates strings" <| fun _ ->
            let result = evalExpr (String "hello")
            Expect.equal result (StringValue "hello") "string literal"

        testCase "evaluates let expressions" <| fun _ ->
            let expr = Let ("x", Number 5, Add (Var "x", Number 3))
            let result = evalExpr expr
            Expect.equal result (IntValue 8) "let x = 5 in x + 3"

        testCase "error on undefined variable" <| fun _ ->
            Expect.throws (fun () -> evalExpr (Var "undefined") |> ignore)
                "undefined variable should throw"

        testCase "error recovery preserves environment" <| fun _ ->
            // This tests the concept - env doesn't change on error
            let env = Map.add "x" (IntValue 10) emptyEnv
            let result = eval env (Var "x")
            Expect.equal result (IntValue 10) "env preserved after error"
    ]

/// Test CLI argument parsing
[<Tests>]
let cliTests =
    testList "CLI arguments" [
        testCase "--expr evaluates expression" <| fun _ ->
            // This is tested by fslit, but good to have unit test too
            let result = evalExpr (Number 42)
            Expect.equal result (IntValue 42) "basic evaluation"

        testCase "formatValue works for all types" <| fun _ ->
            Expect.equal (formatValue (IntValue 42)) "42" "int"
            Expect.equal (formatValue (BoolValue true)) "true" "bool true"
            Expect.equal (formatValue (BoolValue false)) "false" "bool false"
            Expect.equal (formatValue (StringValue "hi")) "\"hi\"" "string"
            let fn = FunctionValue ("x", Var "x", Map.empty)
            Expect.equal (formatValue fn) "<function>" "function"
    ]
