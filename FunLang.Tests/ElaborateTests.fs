module ElaborateTests

open Expecto
open Ast
open Type
open Elaborate

/// Tests for Elaborate module (Phase 2: Type Expression Elaboration - ELAB-01, ELAB-02, ELAB-03)
[<Tests>]
let elaborateTests =
    testList "Elaborate Module" [
        testList "Primitives (ELAB-01)" [
            testCase "TEInt -> TInt" <| fun _ ->
                let result = elaborateTypeExpr TEInt
                Expect.equal result TInt "TEInt should elaborate to TInt"

            testCase "TEBool -> TBool" <| fun _ ->
                let result = elaborateTypeExpr TEBool
                Expect.equal result TBool "TEBool should elaborate to TBool"

            testCase "TEString -> TString" <| fun _ ->
                let result = elaborateTypeExpr TEString
                Expect.equal result TString "TEString should elaborate to TString"
        ]

        testList "Compound Types - Lists (ELAB-01)" [
            testCase "TEList TEInt -> TList TInt" <| fun _ ->
                let result = elaborateTypeExpr (TEList TEInt)
                Expect.equal result (TList TInt) "TEList TEInt should elaborate to TList TInt"

            testCase "Nested list: TEList (TEList TEBool) -> TList (TList TBool)" <| fun _ ->
                let result = elaborateTypeExpr (TEList (TEList TEBool))
                Expect.equal result (TList (TList TBool)) "nested lists should elaborate correctly"
        ]

        testList "Compound Types - Arrows (ELAB-01)" [
            testCase "Simple arrow: TEArrow (TEInt, TEBool) -> TArrow (TInt, TBool)" <| fun _ ->
                let result = elaborateTypeExpr (TEArrow (TEInt, TEBool))
                Expect.equal result (TArrow (TInt, TBool)) "simple arrow should elaborate"

            testCase "Curried arrow: int -> bool -> string" <| fun _ ->
                let result = elaborateTypeExpr (TEArrow (TEInt, TEArrow (TEBool, TEString)))
                Expect.equal result (TArrow (TInt, TArrow (TBool, TString))) "curried arrow should elaborate"
        ]

        testList "Compound Types - Tuples (ELAB-01)" [
            testCase "Two-element tuple: TEInt * TEBool" <| fun _ ->
                let result = elaborateTypeExpr (TETuple [TEInt; TEBool])
                Expect.equal result (TTuple [TInt; TBool]) "two-element tuple should elaborate"

            testCase "Three-element tuple: TEInt * TEBool * TEString" <| fun _ ->
                let result = elaborateTypeExpr (TETuple [TEInt; TEBool; TEString])
                Expect.equal result (TTuple [TInt; TBool; TString]) "three-element tuple should elaborate"
        ]

        testList "Type Variables (ELAB-02)" [
            testCase "TEVar 'a elaborates to TVar with some index" <| fun _ ->
                let result = elaborateTypeExpr (TEVar "'a")
                match result with
                | TVar _ -> ()  // Just check it's a TVar, don't care about exact index
                | _ -> failtest "TEVar 'a should elaborate to TVar"

            testCase "Two different vars get different indices" <| fun _ ->
                let result = elaborateTypeExpr (TEArrow (TEVar "'a", TEVar "'b"))
                match result with
                | TArrow (TVar a, TVar b) ->
                    Expect.notEqual a b "different type variables should have different indices"
                | _ -> failtest "Expected TArrow (TVar, TVar)"

            testCase "Same var in single expr gets same index" <| fun _ ->
                // Note: elaborateTypeExpr uses fresh scope, so same var in one expression should be same
                let result = elaborateTypeExpr (TEArrow (TEVar "'a", TEVar "'a"))
                match result with
                | TArrow (TVar a, TVar b) ->
                    Expect.equal a b "same type variable 'a should have same index"
                | _ -> failtest "Expected TArrow (TVar, TVar)"
        ]

        testList "Scoped Elaboration (ELAB-02, ELAB-03)" [
            testCase "elaborateScoped: same var gets same index" <| fun _ ->
                let result = elaborateScoped [TEVar "'a"; TEVar "'a"]
                match result with
                | [TVar a; TVar b] ->
                    Expect.equal a b "'a should have same index in both positions"
                | _ -> failtest "Expected two TVar results"

            testCase "elaborateScoped: different vars get different indices" <| fun _ ->
                let result = elaborateScoped [TEVar "'a"; TEVar "'b"; TEVar "'a"]
                match result with
                | [TVar a; TVar b; TVar c] ->
                    Expect.equal a c "first and third 'a should have same index"
                    Expect.notEqual a b "'a and 'b should have different indices"
                | _ -> failtest "Expected three TVar results"

            testCase "elaborateScoped: identity function pattern 'a -> 'a" <| fun _ ->
                // Models: fun (x: 'a) : 'a = x
                let result = elaborateScoped [TEVar "'a"; TEVar "'a"]
                match result with
                | [TVar a; TVar b] ->
                    Expect.equal a b "parameter and return type should share same 'a"
                | _ -> failtest "Expected two TVar results"
        ]

        testList "Complex Patterns (ELAB-03)" [
            testCase "Polymorphic identity: 'a -> 'a has same TVar on both sides" <| fun _ ->
                let result = elaborateTypeExpr (TEArrow (TEVar "'a", TEVar "'a"))
                match result with
                | TArrow (TVar a, TVar b) ->
                    Expect.equal a b "identity function should have same type var"
                | _ -> failtest "Expected TArrow (TVar, TVar)"

            testCase "Map-like signature: ('a -> 'b) -> 'a list -> 'b list" <| fun _ ->
                // TEArrow (TEArrow (TEVar "'a", TEVar "'b"), TEArrow (TEList (TEVar "'a"), TEList (TEVar "'b")))
                let mapSig =
                    TEArrow (
                        TEArrow (TEVar "'a", TEVar "'b"),
                        TEArrow (TEList (TEVar "'a"), TEList (TEVar "'b"))
                    )
                let result = elaborateTypeExpr mapSig
                match result with
                | TArrow (TArrow (TVar a1, TVar b1), TArrow (TList (TVar a2), TList (TVar b2))) ->
                    Expect.equal a1 a2 "'a should be same in function and list"
                    Expect.equal b1 b2 "'b should be same in function and list"
                    Expect.notEqual a1 b1 "'a and 'b should be different"
                | _ -> failtest "Expected map signature structure"
        ]

        testList "Polymorphic Annotation Patterns (ELAB-03)" [
            testCase "identity function pattern" <| fun _ ->
                // let id (x: 'a) : 'a = x
                // Parameter and return share same 'a
                let [paramTy; retTy] = elaborateScoped [TEVar "'a"; TEVar "'a"]
                match (paramTy, retTy) with
                | (TVar a, TVar b) ->
                    Expect.equal a b "'a in param and return should be same"
                | _ -> failtest "Expected TVar"

            testCase "swap function uses two type vars" <| fun _ ->
                // let swap (x: 'a) (y: 'b) : 'b * 'a = (y, x)
                let tys = elaborateScoped [TEVar "'a"; TEVar "'b"; TETuple [TEVar "'b"; TEVar "'a"]]
                match tys with
                | [TVar a; TVar b; TTuple [TVar b2; TVar a2]] ->
                    Expect.equal a a2 "'a should be consistent"
                    Expect.equal b b2 "'b should be consistent"
                    Expect.notEqual a b "'a and 'b should be distinct"
                | _ -> failtest "Expected swap pattern"

            testCase "const function ignores second arg type" <| fun _ ->
                // let const (x: 'a) (y: 'b) : 'a = x
                let tys = elaborateScoped [TEVar "'a"; TEVar "'b"; TEVar "'a"]
                match tys with
                | [TVar a; TVar b; TVar a2] ->
                    Expect.equal a a2 "'a at positions 0 and 2 should match"
                    Expect.notEqual a b "'a and 'b should be distinct"
                | _ -> failtest "Expected const pattern"
        ]
    ]
