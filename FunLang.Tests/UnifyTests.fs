module UnifyTests

open Expecto
open Type
open Unify
open Diagnostic

/// Tests for Unify module functions (Phase 6: Testing - TEST-02, TEST-03)
[<Tests>]
let unifyTests =
    testList "Unification" [
        testList "Occurs Check" [
            testCase "detects infinite type TVar -> TVar" <| fun _ ->
                let t = TVar 0
                Expect.isTrue (occurs 0 t) "TVar 0 occurs in TVar 0"

            testCase "detects infinite arrow type" <| fun _ ->
                let t = TArrow(TVar 0, TInt)
                Expect.isTrue (occurs 0 t) "TVar 0 occurs in TVar 0 -> int"

            testCase "detects infinite list type" <| fun _ ->
                let t = TList (TVar 0)
                Expect.isTrue (occurs 0 t) "TVar 0 occurs in TVar 0 list"

            testCase "detects nested infinite type" <| fun _ ->
                let t = TArrow(TInt, TArrow(TVar 0, TBool))
                Expect.isTrue (occurs 0 t) "TVar 0 occurs in nested arrow"

            testCase "non-infinite type OK" <| fun _ ->
                let t = TArrow(TVar 1, TInt)
                Expect.isFalse (occurs 0 t) "TVar 0 doesn't occur in TVar 1 -> int"

            testCase "same variable in different positions" <| fun _ ->
                let t = TArrow(TVar 0, TVar 0)
                Expect.isTrue (occurs 0 t) "TVar 0 occurs in TVar 0 -> TVar 0"
        ]

        testList "Unify - Primitives" [
            testCase "int unifies with int" <| fun _ ->
                let s = unify TInt TInt
                Expect.equal s empty "int = int gives empty substitution"

            testCase "bool unifies with bool" <| fun _ ->
                let s = unify TBool TBool
                Expect.equal s empty "bool = bool gives empty substitution"

            testCase "string unifies with string" <| fun _ ->
                let s = unify TString TString
                Expect.equal s empty "string = string gives empty substitution"

            testCase "int does not unify with bool" <| fun _ ->
                Expect.throws
                    (fun () -> unify TInt TBool |> ignore)
                    "int cannot unify with bool"

            testCase "bool does not unify with string" <| fun _ ->
                Expect.throws
                    (fun () -> unify TBool TString |> ignore)
                    "bool cannot unify with string"

            testCase "type mismatch has correct error kind" <| fun _ ->
                try
                    unify TInt TBool |> ignore
                    failtest "Expected TypeException"
                with
                | TypeException err ->
                    Expect.equal err.Kind (UnifyMismatch (TInt, TBool)) "Should be UnifyMismatch"
        ]

        testList "Unify - Type Variables" [
            testCase "type var unifies with concrete type" <| fun _ ->
                let s = unify (TVar 0) TInt
                Expect.equal s (singleton 0 TInt) "TVar 0 = int"

            testCase "concrete type unifies with type var (symmetric)" <| fun _ ->
                let s = unify TBool (TVar 1)
                Expect.equal s (singleton 1 TBool) "bool = TVar 1"

            testCase "type var unifies with different type var" <| fun _ ->
                let s = unify (TVar 0) (TVar 1)
                Expect.equal s (singleton 0 (TVar 1)) "TVar 0 = TVar 1"

            testCase "same type var unifies with itself" <| fun _ ->
                let s = unify (TVar 0) (TVar 0)
                Expect.equal s empty "TVar 0 = TVar 0 gives empty"

            testCase "type var unification is symmetric" <| fun _ ->
                let s1 = unify (TVar 0) TInt
                let s2 = unify TInt (TVar 0)
                Expect.equal s1 s2 "order doesn't matter"

            testCase "type var infinite type rejected" <| fun _ ->
                Expect.throws
                    (fun () -> unify (TVar 0) (TArrow(TVar 0, TInt)) |> ignore)
                    "TVar 0 = TVar 0 -> int is infinite"

            testCase "occurs check has correct error kind" <| fun _ ->
                try
                    unify (TVar 0) (TArrow(TVar 0, TInt)) |> ignore
                    failtest "Expected TypeException"
                with
                | TypeException err ->
                    match err.Kind with
                    | OccursCheck (0, TArrow(TVar 0, TInt)) -> ()
                    | _ -> failtest "Should be OccursCheck with correct var and type"
        ]

        testList "Unify - Arrow Types" [
            testCase "compatible arrows unify" <| fun _ ->
                let t1 = TArrow(TVar 0, TVar 1)
                let t2 = TArrow(TInt, TBool)
                let s = unify t1 t2
                Expect.equal (apply s t1) (apply s t2) "arrows unify correctly"
                Expect.equal (apply s (TVar 0)) TInt "domain unified"
                Expect.equal (apply s (TVar 1)) TBool "range unified"

            testCase "domain mismatch fails" <| fun _ ->
                let t1 = TArrow(TInt, TVar 0)
                let t2 = TArrow(TBool, TVar 1)
                Expect.throws
                    (fun () -> unify t1 t2 |> ignore)
                    "int -> 'a cannot unify with bool -> 'b"

            testCase "range mismatch fails" <| fun _ ->
                let t1 = TArrow(TVar 0, TInt)
                let t2 = TArrow(TVar 1, TBool)
                Expect.throws
                    (fun () -> unify t1 t2 |> ignore)
                    "'a -> int cannot unify with 'b -> bool"

            testCase "nested arrows unify" <| fun _ ->
                let t1 = TArrow(TVar 0, TArrow(TVar 1, TVar 2))
                let t2 = TArrow(TInt, TArrow(TBool, TString))
                let s = unify t1 t2
                Expect.equal (apply s (TVar 0)) TInt "first var"
                Expect.equal (apply s (TVar 1)) TBool "second var"
                Expect.equal (apply s (TVar 2)) TString "third var"

            testCase "substitution threading in arrow unification" <| fun _ ->
                let t1 = TArrow(TVar 0, TVar 0)
                let t2 = TArrow(TInt, TVar 1)
                let s = unify t1 t2
                // After unifying domains, TVar 0 = TInt
                // Then range: TVar 0 (now TInt) unifies with TVar 1
                Expect.equal (apply s (TVar 0)) TInt "domain determines var"
                Expect.equal (apply s (TVar 1)) TInt "range follows domain"

            testCase "complex arrow chain" <| fun _ ->
                let t1 = TArrow(TArrow(TVar 0, TVar 1), TVar 2)
                let t2 = TArrow(TArrow(TInt, TBool), TString)
                let s = unify t1 t2
                Expect.equal (apply s t1) (apply s t2) "complex arrows unify"
        ]

        testList "Unify - Tuples" [
            testCase "same length tuples unify" <| fun _ ->
                let t1 = TTuple [TVar 0; TVar 1]
                let t2 = TTuple [TInt; TBool]
                let s = unify t1 t2
                Expect.equal (apply s (TVar 0)) TInt "first element"
                Expect.equal (apply s (TVar 1)) TBool "second element"

            testCase "different length tuples fail" <| fun _ ->
                let t1 = TTuple [TVar 0; TVar 1]
                let t2 = TTuple [TInt; TBool; TString]
                Expect.throws
                    (fun () -> unify t1 t2 |> ignore)
                    "tuples must have same length"

            testCase "element mismatch fails" <| fun _ ->
                let t1 = TTuple [TInt; TBool]
                let t2 = TTuple [TInt; TString]
                Expect.throws
                    (fun () -> unify t1 t2 |> ignore)
                    "second element mismatch"

            testCase "substitution threading in tuples" <| fun _ ->
                let t1 = TTuple [TVar 0; TVar 0; TVar 1]
                let t2 = TTuple [TInt; TVar 2; TBool]
                let s = unify t1 t2
                // First element: TVar 0 = TInt
                // Second element: TVar 0 (now TInt) = TVar 2, so TVar 2 = TInt
                // Third element: TVar 1 = TBool
                Expect.equal (apply s (TVar 0)) TInt "var 0"
                Expect.equal (apply s (TVar 1)) TBool "var 1"
                Expect.equal (apply s (TVar 2)) TInt "var 2 follows var 0"
        ]

        testList "Unify - Lists" [
            testCase "list element types unify" <| fun _ ->
                let t1 = TList (TVar 0)
                let t2 = TList TInt
                let s = unify t1 t2
                Expect.equal (apply s (TVar 0)) TInt "element type unified"

            testCase "incompatible list elements fail" <| fun _ ->
                let t1 = TList TInt
                let t2 = TList TBool
                Expect.throws
                    (fun () -> unify t1 t2 |> ignore)
                    "int list cannot unify with bool list"

            testCase "nested lists unify" <| fun _ ->
                let t1 = TList (TList (TVar 0))
                let t2 = TList (TList TString)
                let s = unify t1 t2
                Expect.equal (apply s (TVar 0)) TString "nested element type"
        ]

        testList "Unify - Cross-Type Errors" [
            testCase "arrow vs tuple fails" <| fun _ ->
                let t1 = TArrow(TInt, TBool)
                let t2 = TTuple [TInt; TBool]
                Expect.throws
                    (fun () -> unify t1 t2 |> ignore)
                    "arrow cannot unify with tuple"

            testCase "list vs arrow fails" <| fun _ ->
                let t1 = TList TInt
                let t2 = TArrow(TInt, TInt)
                Expect.throws
                    (fun () -> unify t1 t2 |> ignore)
                    "list cannot unify with arrow"

            testCase "tuple vs primitive fails" <| fun _ ->
                let t1 = TTuple [TInt; TBool]
                let t2 = TInt
                Expect.throws
                    (fun () -> unify t1 t2 |> ignore)
                    "tuple cannot unify with primitive"
        ]

        testList "Unify - Complex Scenarios" [
            testCase "multiple variable dependencies" <| fun _ ->
                // (TVar 0, TVar 1) unifies with (TVar 2, TVar 2)
                // Result: TVar 0 = TVar 2, TVar 1 = TVar 2
                let t1 = TTuple [TVar 0; TVar 1]
                let t2 = TTuple [TVar 2; TVar 2]
                let s = unify t1 t2
                // Both TVar 0 and TVar 1 should unify with TVar 2
                let result1 = apply s (TVar 0)
                let result2 = apply s (TVar 1)
                // They should be the same
                Expect.equal result1 result2 "both vars unify to same type"

            testCase "transitive unification" <| fun _ ->
                // First unify TVar 0 with TVar 1, then unify result with TInt
                let s1 = unify (TVar 0) (TVar 1)
                let s2 = unify (apply s1 (TVar 0)) TInt
                let s = compose s2 s1
                Expect.equal (apply s (TVar 0)) TInt "transitive to TInt"
                Expect.equal (apply s (TVar 1)) TInt "transitive to TInt"

            testCase "unification with substitution composition" <| fun _ ->
                // Unify 'a -> 'b with int -> 'c, then 'b with bool
                let s1 = unify (TArrow(TVar 0, TVar 1)) (TArrow(TInt, TVar 2))
                let s2 = unify (apply s1 (TVar 1)) TBool
                let s = compose s2 s1
                Expect.equal (apply s (TVar 0)) TInt "'a = int"
                Expect.equal (apply s (TVar 1)) TBool "'b = bool"
                Expect.equal (apply s (TVar 2)) TBool "'c = bool"
        ]
    ]
