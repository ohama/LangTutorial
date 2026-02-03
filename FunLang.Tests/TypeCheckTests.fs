module TypeCheckTests

open Expecto
open Type
open TypeCheck
open Ast
open FSharp.Text.Lexing

/// Parse a string input and return the AST
let parse (input: string) : Expr =
    let lexbuf = LexBuffer<char>.FromString input
    Parser.start Lexer.tokenize lexbuf

/// Type check a string expression
let check input =
    let expr = parse input
    typecheck expr

[<Tests>]
let typeCheckTests = testList "TypeCheck Integration" [
    testList "Prelude function types (TEST-05)" [
        test "map has type ('a -> 'b) -> 'a list -> 'b list" {
            let result = check "map"
            match result with
            | Ok (TArrow(TArrow(TVar a, TVar b), TArrow(TList(TVar a'), TList(TVar b')))) ->
                Expect.equal a a' "map first type var should match"
                Expect.equal b b' "map second type var should match"
            | _ -> failtest "map should have correct type"
        }

        test "filter has type ('a -> bool) -> 'a list -> 'a list" {
            let result = check "filter"
            match result with
            | Ok (TArrow(TArrow(TVar a, TBool), TArrow(TList(TVar a'), TList(TVar a'')))) ->
                Expect.equal a a' "filter type var should match"
                Expect.equal a' a'' "filter type var should match"
            | _ -> failtest "filter should have correct type"
        }

        test "fold has type ('b -> 'a -> 'b) -> 'b -> 'a list -> 'b" {
            let result = check "fold"
            match result with
            | Ok (TArrow(TArrow(TVar b, TArrow(TVar a, TVar b')),
                         TArrow(TVar b'', TArrow(TList(TVar a'), TVar b''')))) ->
                Expect.equal a a' "fold first type var should match"
                Expect.equal b b' "fold second type var should match"
                Expect.equal b b'' "fold second type var should match"
                Expect.equal b b''' "fold second type var should match"
            | _ -> failtest "fold should have correct type"
        }

        test "length has type 'a list -> int" {
            let result = check "length"
            match result with
            | Ok (TArrow(TList(TVar _), TInt)) -> ()
            | _ -> failtest "length should have correct type"
        }

        test "reverse has type 'a list -> 'a list" {
            let result = check "reverse"
            match result with
            | Ok (TArrow(TList(TVar a), TList(TVar a'))) ->
                Expect.equal a a' "reverse type var should match"
            | _ -> failtest "reverse should have correct type"
        }

        test "append has type 'a list -> 'a list -> 'a list" {
            let result = check "append"
            match result with
            | Ok (TArrow(TList(TVar a), TArrow(TList(TVar a'), TList(TVar a'')))) ->
                Expect.equal a a' "append type var should match"
                Expect.equal a' a'' "append type var should match"
            | _ -> failtest "append should have correct type"
        }

        test "id has type 'a -> 'a" {
            let result = check "id"
            match result with
            | Ok (TArrow(TVar a, TVar a')) ->
                Expect.equal a a' "id type var should match"
            | _ -> failtest "id should have correct type"
        }

        test "const has type 'a -> 'b -> 'a" {
            let result = check "const"
            match result with
            | Ok (TArrow(TVar a, TArrow(TVar b, TVar a'))) ->
                Expect.equal a a' "const type var should match"
                Expect.notEqual a b "const type vars should be different"
            | _ -> failtest "const should have correct type"
        }

        test "compose has type ('b -> 'c) -> ('a -> 'b) -> 'a -> 'c" {
            let result = check "compose"
            match result with
            | Ok (TArrow(TArrow(TVar b, TVar c),
                         TArrow(TArrow(TVar a, TVar b'), TArrow(TVar a', TVar c')))) ->
                Expect.equal a a' "compose first type var should match"
                Expect.equal b b' "compose second type var should match"
                Expect.equal c c' "compose third type var should match"
            | _ -> failtest "compose should have correct type"
        }

        test "hd has type 'a list -> 'a" {
            let result = check "hd"
            match result with
            | Ok (TArrow(TList(TVar a), TVar a')) ->
                Expect.equal a a' "hd type var should match"
            | _ -> failtest "hd should have correct type"
        }

        test "tl has type 'a list -> 'a list" {
            let result = check "tl"
            match result with
            | Ok (TArrow(TList(TVar a), TList(TVar a'))) ->
                Expect.equal a a' "tl type var should match"
            | _ -> failtest "tl should have correct type"
        }
    ]

    testList "Prelude usage tests" [
        test "map with concrete types" {
            let result = check "map (fun x -> x + 1) [1, 2, 3]"
            Expect.equal result (Ok (TList TInt)) "map should infer to int list"
        }

        test "filter with predicate" {
            let result = check "filter (fun x -> x > 0) [1, -2, 3]"
            Expect.equal result (Ok (TList TInt)) "filter should infer to int list"
        }

        test "fold with accumulator" {
            let result = check "fold (fun acc -> fun x -> acc + x) 0 [1, 2, 3]"
            Expect.equal result (Ok TInt) "fold should infer to int"
        }

        test "length with list" {
            let result = check "length [1, 2, 3]"
            Expect.equal result (Ok TInt) "length should infer to int"
        }

        test "reverse with list" {
            let result = check "reverse [1, 2, 3]"
            Expect.equal result (Ok (TList TInt)) "reverse should infer to int list"
        }

        test "append two lists" {
            let result = check "append [1, 2] [3, 4]"
            Expect.equal result (Ok (TList TInt)) "append should infer to int list"
        }

        test "id application" {
            let result = check "id 42"
            Expect.equal result (Ok TInt) "id should infer to int"
        }

        test "compose functions" {
            let result = check "compose (fun x -> x + 1) (fun x -> x * 2) 5"
            Expect.equal result (Ok TInt) "compose should infer to int"
        }

        test "Prelude polymorphism" {
            let result = check "let f = id in (f 1, f true)"
            Expect.equal result (Ok (TTuple [TInt; TBool]))
                "Prelude functions should be polymorphic"
        }

        test "map with bool transformation" {
            let result = check "map (fun x -> x > 0) [1, 2, 3]"
            Expect.equal result (Ok (TList TBool)) "map should support type change"
        }

        test "hd extracts first element" {
            let result = check "hd [1, 2, 3]"
            Expect.equal result (Ok TInt) "hd should infer to int"
        }

        test "tl returns tail" {
            let result = check "tl [1, 2, 3]"
            Expect.equal result (Ok (TList TInt)) "tl should infer to int list"
        }
    ]

    testList "typecheck returns Ok" [
        test "Simple expression" {
            let result = check "1 + 2"
            Expect.equal result (Ok TInt) "arithmetic should type check"
        }

        test "Let binding" {
            let result = check "let x = 5 in x"
            Expect.equal result (Ok TInt) "let binding should type check"
        }

        test "Lambda function" {
            let result = check "fun x -> x"
            match result with
            | Ok (TArrow(TVar _, TVar _)) -> ()
            | _ -> failtest "lambda should type check"
        }

        test "Complex recursive function" {
            let result = check "let rec fact n = if n <= 1 then 1 else n * fact (n-1) in fact 5"
            Expect.equal result (Ok TInt) "factorial should type check"
        }

        test "Tuple expression" {
            let result = check "(1, true, \"hello\")"
            Expect.equal result (Ok (TTuple [TInt; TBool; TString]))
                "tuple should type check"
        }

        test "List expression" {
            let result = check "[1, 2, 3]"
            Expect.equal result (Ok (TList TInt)) "list should type check"
        }

        test "Match expression" {
            let result = check "match [1, 2] with | h :: t -> h | [] -> 0"
            Expect.equal result (Ok TInt) "match should type check"
        }
    ]

    testList "typecheck returns Error" [
        test "Unbound variable" {
            let result = check "x"
            match result with
            | Error msg ->
                Expect.stringContains msg "Unbound variable" "should report unbound variable"
            | Ok _ -> failtest "unbound variable should fail type check"
        }

        test "Type mismatch in addition" {
            let result = check "1 + true"
            match result with
            | Error msg ->
                Expect.stringContains msg "Type mismatch" "should report type mismatch"
            | Ok _ -> failtest "type mismatch should fail type check"
        }

        test "If branch type mismatch" {
            let result = check "if true then 1 else false"
            match result with
            | Error msg ->
                Expect.stringContains msg "Type mismatch" "should report branch mismatch"
            | Ok _ -> failtest "branch mismatch should fail type check"
        }

        test "Applying non-function" {
            let result = check "let x = 5 in x 3"
            match result with
            | Error msg ->
                Expect.stringContains msg "Type mismatch" "should report application error"
            | Ok _ -> failtest "applying non-function should fail type check"
        }

        test "Mixed list elements" {
            let result = check "[1, true]"
            match result with
            | Error msg ->
                Expect.stringContains msg "Type mismatch" "should report element type mismatch"
            | Ok _ -> failtest "mixed list should fail type check"
        }
    ]

    testList "Integration with Prelude" [
        test "Complex Prelude composition" {
            let result = check "let doubled = map (fun x -> x * 2) in doubled [1, 2, 3]"
            Expect.equal result (Ok (TList TInt)) "partial application should work"
        }

        test "Nested Prelude functions" {
            let result = check "length (filter (fun x -> x > 0) [1, -2, 3])"
            Expect.equal result (Ok TInt) "nested Prelude calls should work"
        }

        test "User function with Prelude" {
            let result = check "let double = fun x -> x * 2 in map double [1, 2, 3]"
            Expect.equal result (Ok (TList TInt)) "user function with Prelude should work"
        }

        test "Polymorphic const in different contexts" {
            let result = check "let c1 = const 5 true in let c2 = const true 5 in (c1, c2)"
            Expect.equal result (Ok (TTuple [TInt; TBool]))
                "const should work polymorphically"
        }
    ]
]
