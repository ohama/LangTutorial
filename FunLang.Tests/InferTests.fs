module InferTests

open Expecto
open Type
open Infer
open Ast
open Diagnostic
open FSharp.Text.Lexing

/// Parse a string input and return the AST
let parse (input: string) : Expr =
    let lexbuf = LexBuffer<char>.FromString input
    Lexer.setInitialPos lexbuf "<test>"
    Parser.start Lexer.tokenize lexbuf

/// Infer type in empty environment (for pure inference tests)
let inferEmpty expr =
    let s, ty = infer Map.empty expr
    apply s ty

/// Infer type with TypeCheck.initialTypeEnv (for Prelude-aware tests)
let inferWithPrelude expr =
    let s, ty = infer TypeCheck.initialTypeEnv expr
    apply s ty

[<Tests>]
let inferTests = testList "Type Inference" [
    testList "Core functions" [
        test "freshVar generates unique variables" {
            let v1 = freshVar()
            let v2 = freshVar()
            Expect.notEqual v1 v2 "freshVar should generate unique vars"
        }

        test "instantiate with empty vars returns type unchanged" {
            let scheme = Scheme([], TInt)
            let result = instantiate scheme
            Expect.equal result TInt "monomorphic scheme should return type unchanged"
        }

        test "instantiate creates fresh vars" {
            let scheme = Scheme([0], TVar 0)
            let result = instantiate scheme
            match result with
            | TVar n -> Expect.isGreaterThanOrEqual n 1000 "instantiate should create fresh var"
            | _ -> failtest "instantiate should return TVar"
        }

        test "generalize with empty env" {
            let ty = TVar 1000
            let result = generalize Map.empty ty
            match result with
            | Scheme(vars, TVar 1000) ->
                Expect.contains vars 1000 "should generalize free var"
            | _ -> failtest "generalize should create scheme"
        }

        test "generalize excludes env vars" {
            let env = Map.ofList [("x", Scheme([], TVar 1000))]
            let ty = TArrow(TVar 1000, TVar 1001)
            let (Scheme(vars, _)) = generalize env ty
            Expect.isFalse (List.contains 1000 vars) "should not generalize env vars"
            Expect.contains vars 1001 "should generalize free vars"
        }
    ]

    testList "Literal inference (INFER-04)" [
        test "Number infers to int" {
            let result = inferEmpty (Number (42, unknownSpan))
            Expect.equal result TInt "Number should infer to int"
        }

        test "Bool true infers to bool" {
            let result = inferEmpty (Bool (true, unknownSpan))
            Expect.equal result TBool "Bool true should infer to bool"
        }

        test "Bool false infers to bool" {
            let result = inferEmpty (Bool (false, unknownSpan))
            Expect.equal result TBool "Bool false should infer to bool"
        }

        test "String infers to string" {
            let result = inferEmpty (String ("hello", unknownSpan))
            Expect.equal result TString "String should infer to string"
        }
    ]

    testList "Variable inference (INFER-06)" [
        test "Variable lookup returns monomorphic type" {
            let env = Map.ofList [("x", Scheme([], TInt))]
            let s, ty = infer env (Var ("x", unknownSpan))
            Expect.equal (apply s ty) TInt "variable lookup should return type"
        }

        test "Polymorphic instantiation creates fresh vars" {
            let env = Map.ofList [("id", Scheme([0], TArrow(TVar 0, TVar 0)))]
            let s, ty = infer env (Var ("id", unknownSpan))
            match apply s ty with
            | TArrow(TVar a, TVar b) ->
                Expect.equal a b "instantiated vars should be same"
                Expect.isGreaterThanOrEqual a 1000 "should use fresh vars"
            | _ -> failtest "id should have arrow type"
        }

        test "Unbound variable raises TypeError" {
            Expect.throws
                (fun () -> infer Map.empty (Var ("x", unknownSpan)) |> ignore)
                "unbound variable should raise error"
        }

        test "Unbound variable has correct error kind" {
            try
                infer Map.empty (Var ("x", unknownSpan)) |> ignore
                failtest "Expected TypeException"
            with
            | TypeException err ->
                Expect.equal err.Kind (UnboundVar "x") "Should be UnboundVar with variable name"
        }
    ]

    testList "Arithmetic operators (INFER-05)" [
        test "Add infers int -> int -> int" {
            let result = inferEmpty (Add(Number (1, unknownSpan), Number (2, unknownSpan), unknownSpan))
            Expect.equal result TInt "Add should infer to int"
        }

        test "Subtract infers int -> int -> int" {
            let result = inferEmpty (Subtract(Number (10, unknownSpan), Number (4, unknownSpan), unknownSpan))
            Expect.equal result TInt "Subtract should infer to int"
        }

        test "Multiply infers int -> int -> int" {
            let result = inferEmpty (Multiply(Number (3, unknownSpan), Number (4, unknownSpan), unknownSpan))
            Expect.equal result TInt "Multiply should infer to int"
        }

        test "Divide infers int -> int -> int" {
            let result = inferEmpty (Divide(Number (20, unknownSpan), Number (4, unknownSpan), unknownSpan))
            Expect.equal result TInt "Divide should infer to int"
        }

        test "Negate infers int -> int" {
            let result = inferEmpty (Negate(Number (5, unknownSpan), unknownSpan))
            Expect.equal result TInt "Negate should infer to int"
        }

        test "Add with non-int raises TypeError" {
            Expect.throws
                (fun () -> inferEmpty (Add(Bool (true, unknownSpan), Number (1, unknownSpan), unknownSpan)) |> ignore)
                "Add with bool should raise error"
        }
    ]

    testList "Comparison operators (INFER-05)" [
        test "LessThan infers int -> int -> bool" {
            let result = inferEmpty (LessThan(Number (3, unknownSpan), Number (5, unknownSpan), unknownSpan))
            Expect.equal result TBool "LessThan should infer to bool"
        }

        test "Equal infers int -> int -> bool" {
            let result = inferEmpty (Equal(Number (5, unknownSpan), Number (5, unknownSpan), unknownSpan))
            Expect.equal result TBool "Equal should infer to bool"
        }

        test "GreaterThan infers int -> int -> bool" {
            let result = inferEmpty (GreaterThan(Number (5, unknownSpan), Number (3, unknownSpan), unknownSpan))
            Expect.equal result TBool "GreaterThan should infer to bool"
        }

        test "Comparison with non-int raises TypeError" {
            Expect.throws
                (fun () -> inferEmpty (LessThan(Bool (true, unknownSpan), Number (1, unknownSpan), unknownSpan)) |> ignore)
                "Comparison with bool should raise error"
        }
    ]

    testList "Logical operators (INFER-05)" [
        test "And infers bool -> bool -> bool" {
            let result = inferEmpty (And(Bool (true, unknownSpan), Bool (false, unknownSpan), unknownSpan))
            Expect.equal result TBool "And should infer to bool"
        }

        test "Or infers bool -> bool -> bool" {
            let result = inferEmpty (Or(Bool (true, unknownSpan), Bool (false, unknownSpan), unknownSpan))
            Expect.equal result TBool "Or should infer to bool"
        }

        test "And with non-bool raises TypeError" {
            Expect.throws
                (fun () -> inferEmpty (And(Number (1, unknownSpan), Bool (true, unknownSpan), unknownSpan)) |> ignore)
                "And with int should raise error"
        }
    ]

    testList "Lambda inference (INFER-08)" [
        test "Identity function" {
            let result = parse "fun x -> x" |> inferEmpty
            match result with
            | TArrow(TVar a, TVar b) ->
                Expect.equal a b "identity should have type 'a -> 'a"
            | _ -> failtest "lambda should have arrow type"
        }

        test "Constant function" {
            let result = parse "fun x -> 42" |> inferEmpty
            match result with
            | TArrow(TVar _, TInt) -> ()
            | _ -> failtest "constant function should have type 'a -> int"
        }

        test "Using parameter" {
            let result = parse "fun x -> x + 1" |> inferEmpty
            Expect.equal result (TArrow(TInt, TInt)) "should infer int -> int"
        }

        test "Nested lambda" {
            let result = parse "fun x -> fun y -> x + y" |> inferEmpty
            Expect.equal result (TArrow(TInt, TArrow(TInt, TInt)))
                "should infer int -> int -> int"
        }

        test "Lambda with condition" {
            let result = parse "fun x -> if x then 1 else 2" |> inferEmpty
            Expect.equal result (TArrow(TBool, TInt)) "should infer bool -> int"
        }
    ]

    testList "Application inference (INFER-08)" [
        test "Simple application" {
            let result = parse "let f = fun x -> x + 1 in f 5" |> inferEmpty
            Expect.equal result TInt "application should infer to int"
        }

        test "Curried application" {
            let result = parse "let add = fun x -> fun y -> x + y in add 3 4" |> inferEmpty
            Expect.equal result TInt "curried app should infer to int"
        }

        test "Partial application" {
            let result = parse "let add = fun x -> fun y -> x + y in add 5" |> inferEmpty
            Expect.equal result (TArrow(TInt, TInt)) "partial app should infer to int -> int"
        }

        test "Applying non-function raises TypeError" {
            Expect.throws
                (fun () -> parse "let x = 5 in x 3" |> inferEmpty |> ignore)
                "applying non-function should raise error"
        }
    ]

    testList "Let-polymorphism (INFER-07)" [
        test "Classic polymorphism test" {
            let result = parse "let id = fun x -> x in (id 5, id true)" |> inferEmpty
            Expect.equal result (TTuple [TInt; TBool])
                "id should be used at different types"
        }

        test "Polymorphic function used once" {
            let result = parse "let id = fun x -> x in id 5" |> inferEmpty
            Expect.equal result TInt "polymorphic function used at int"
        }

        test "Nested let bindings" {
            let result = parse "let x = 5 in let y = x + 1 in y" |> inferEmpty
            Expect.equal result TInt "nested let should infer to int"
        }

        test "Let binding shadows outer variable" {
            let result = parse "let x = 5 in let x = true in x" |> inferEmpty
            Expect.equal result TBool "inner binding should shadow outer"
        }

        test "Polymorphic const function" {
            let result = parse "let const = fun x -> fun y -> x in (const 5 true, const true 5)"
                         |> inferEmpty
            Expect.equal result (TTuple [TInt; TBool])
                "const should be polymorphic"
        }
    ]

    testList "LetRec inference (INFER-09)" [
        test "Simple recursive function" {
            let result = parse "let rec f x = x in f 42" |> inferEmpty
            Expect.equal result TInt "simple rec should infer to int"
        }

        test "Factorial type" {
            let result = parse "let rec fact n = if n <= 1 then 1 else n * fact (n-1) in fact"
                         |> inferEmpty
            Expect.equal result (TArrow(TInt, TInt)) "fact should have type int -> int"
        }

        test "Recursive function with condition" {
            let result = parse "let rec countdown n = if n <= 0 then 0 else countdown (n - 1) in countdown 10"
                         |> inferEmpty
            Expect.equal result TInt "countdown should infer to int"
        }

        test "Fibonacci type" {
            let result = parse "let rec fib n = if n <= 1 then n else fib (n - 1) + fib (n - 2) in fib"
                         |> inferEmpty
            Expect.equal result (TArrow(TInt, TInt)) "fib should have type int -> int"
        }
    ]

    testList "If inference (INFER-10)" [
        test "If with int branches" {
            let result = parse "if true then 1 else 2" |> inferEmpty
            Expect.equal result TInt "if should infer to int"
        }

        test "If with bool branches" {
            let result = parse "if true then true else false" |> inferEmpty
            Expect.equal result TBool "if should infer to bool"
        }

        test "Branch type mismatch raises TypeError" {
            Expect.throws
                (fun () -> parse "if true then 1 else true" |> inferEmpty |> ignore)
                "branch type mismatch should raise error"
        }

        test "Condition must be bool" {
            Expect.throws
                (fun () -> parse "if 1 then 2 else 3" |> inferEmpty |> ignore)
                "condition must be bool"
        }
    ]

    testList "Tuple inference (INFER-11)" [
        test "Pair inference" {
            let result = parse "(1, true)" |> inferEmpty
            Expect.equal result (TTuple [TInt; TBool]) "pair should infer correctly"
        }

        test "Triple inference" {
            let result = parse "(1, 2, 3)" |> inferEmpty
            Expect.equal result (TTuple [TInt; TInt; TInt]) "triple should infer correctly"
        }

        test "Nested tuple" {
            let result = parse "((1, 2), true)" |> inferEmpty
            Expect.equal result (TTuple [TTuple [TInt; TInt]; TBool])
                "nested tuple should infer correctly"
        }
    ]

    testList "List inference (INFER-12)" [
        test "EmptyList has polymorphic type" {
            let result = parse "[]" |> inferEmpty
            match result with
            | TList (TVar _) -> ()
            | _ -> failtest "empty list should have type 'a list"
        }

        test "List literal infers element type" {
            let result = parse "[1, 2, 3]" |> inferEmpty
            Expect.equal result (TList TInt) "list should infer to int list"
        }

        test "Cons operator" {
            let result = parse "1 :: []" |> inferEmpty
            Expect.equal result (TList TInt) "cons should infer to int list"
        }

        test "Cons with tail" {
            let result = parse "1 :: [2, 3]" |> inferEmpty
            Expect.equal result (TList TInt) "cons with tail should infer to int list"
        }

        test "Mixed list elements raise TypeError" {
            Expect.throws
                (fun () -> parse "[1, true]" |> inferEmpty |> ignore)
                "mixed list should raise error"
        }
    ]

    testList "Match inference (INFER-13)" [
        test "Simple match on int" {
            let result = parse "match 1 with | 1 -> true | _ -> false" |> inferEmpty
            Expect.equal result TBool "match should infer to bool"
        }

        test "Match on list with cons pattern" {
            let result = parse "match [1, 2] with | h :: t -> h | [] -> 0" |> inferEmpty
            Expect.equal result TInt "match should infer to int"
        }

        test "All branches have consistent types" {
            // Note: Current implementation successfully type-checks when all branches
            // unify to a common result type
            let result = parse "match [1, 2] with | [] -> 0 | h :: t -> h + 1" |> inferEmpty
            Expect.equal result TInt "match branches should have consistent types"
        }

        test "Pattern bindings infer correctly" {
            let result = parse "match (1, true) with | (x, y) -> x" |> inferEmpty
            Expect.equal result TInt "pattern binding should infer correctly"
        }
    ]

    testList "Lambda parameter monomorphism" [
        test "Lambda parameter cannot be polymorphic" {
            Expect.throws
                (fun () -> parse "fun f -> (f 1, f true)" |> inferEmpty |> ignore)
                "lambda param should be monomorphic"
        }

        test "Lambda parameter used at single type is ok" {
            let result = parse "fun f -> f 1" |> inferEmpty
            match result with
            | TArrow(TArrow(TInt, TVar a), TVar b) ->
                Expect.equal a b "result type should match"
            | _ -> failtest "should infer function type"
        }
    ]

    testList "Integration tests" [
        test "Complex expression with let-polymorphism" {
            let result = parse "let id = fun x -> x in let a = id 1 in let b = id true in (a, b)"
                         |> inferEmpty
            Expect.equal result (TTuple [TInt; TBool])
                "complex expression should infer correctly"
        }

        test "Nested function composition" {
            let result = parse "let f = fun x -> x + 1 in let g = fun y -> f (f y) in g 5"
                         |> inferEmpty
            Expect.equal result TInt "nested composition should infer to int"
        }

        test "Higher-order function" {
            let result = parse "let apply = fun f -> fun x -> f x in apply (fun y -> y + 1) 5"
                         |> inferEmpty
            Expect.equal result TInt "higher-order function should infer correctly"
        }

        test "List operation" {
            let result = parse "let rec length xs = match xs with | [] -> 0 | h :: t -> 1 + length t in length [1,2,3]"
                         |> inferEmpty
            Expect.equal result TInt "list length should infer to int"
        }
    ]
]
