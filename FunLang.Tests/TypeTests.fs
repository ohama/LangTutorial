module TypeTests

open Expecto
open Type

/// Tests for Type module functions (Phase 6: Testing - TEST-01)
[<Tests>]
let typeTests =
    testList "Type Module" [
        testList "formatType - Primitives" [
            testCase "formats TInt" <| fun _ ->
                Expect.equal (formatType TInt) "int" "TInt should format as 'int'"

            testCase "formats TBool" <| fun _ ->
                Expect.equal (formatType TBool) "bool" "TBool should format as 'bool'"

            testCase "formats TString" <| fun _ ->
                Expect.equal (formatType TString) "string" "TString should format as 'string'"
        ]

        testList "formatType - Type Variables" [
            testCase "formats TVar 0 as 'a" <| fun _ ->
                Expect.equal (formatType (TVar 0)) "'a" "TVar 0 should be 'a'"

            testCase "formats TVar 1 as 'b" <| fun _ ->
                Expect.equal (formatType (TVar 1)) "'b" "TVar 1 should be 'b'"

            testCase "formats TVar 25 as 'z" <| fun _ ->
                Expect.equal (formatType (TVar 25)) "'z" "TVar 25 should be 'z'"

            testCase "formats TVar 26 cycles to 'a" <| fun _ ->
                Expect.equal (formatType (TVar 26)) "'a" "TVar 26 should cycle to 'a'"

            testCase "formats large TVar with modulo" <| fun _ ->
                Expect.equal (formatType (TVar 1000)) "'m" "TVar 1000 % 26 = 12 -> 'm'"
        ]

        testList "formatType - Arrow Types" [
            testCase "formats simple arrow" <| fun _ ->
                let t = TArrow(TInt, TBool)
                Expect.equal (formatType t) "int -> bool" "simple arrow type"

            testCase "formats arrow with type variable" <| fun _ ->
                let t = TArrow(TVar 0, TVar 1)
                Expect.equal (formatType t) "'a -> 'b" "arrow with type vars"

            testCase "formats nested arrow (right-associative)" <| fun _ ->
                let t = TArrow(TInt, TArrow(TBool, TString))
                Expect.equal (formatType t) "int -> bool -> string" "right-associative arrows"

            testCase "formats left arrow with parentheses" <| fun _ ->
                let t = TArrow(TArrow(TInt, TBool), TString)
                Expect.equal (formatType t) "(int -> bool) -> string" "left arrow needs parens"

            testCase "formats complex nested arrows" <| fun _ ->
                let t = TArrow(TArrow(TVar 0, TVar 1), TArrow(TVar 2, TVar 3))
                Expect.equal (formatType t) "('a -> 'b) -> 'c -> 'd" "complex nested arrows"
        ]

        testList "formatType - Tuples" [
            testCase "formats two-element tuple" <| fun _ ->
                let t = TTuple [TInt; TBool]
                Expect.equal (formatType t) "int * bool" "two-element tuple"

            testCase "formats three-element tuple" <| fun _ ->
                let t = TTuple [TInt; TBool; TString]
                Expect.equal (formatType t) "int * bool * string" "three-element tuple"

            testCase "formats tuple with type variables" <| fun _ ->
                let t = TTuple [TVar 0; TVar 1]
                Expect.equal (formatType t) "'a * 'b" "tuple with type vars"
        ]

        testList "formatType - Lists" [
            testCase "formats int list" <| fun _ ->
                let t = TList TInt
                Expect.equal (formatType t) "int list" "int list"

            testCase "formats type variable list" <| fun _ ->
                let t = TList (TVar 0)
                Expect.equal (formatType t) "'a list" "'a list"

            testCase "formats nested list" <| fun _ ->
                let t = TList (TList TInt)
                Expect.equal (formatType t) "int list list" "list of lists"

            testCase "formats list of arrows" <| fun _ ->
                let t = TList (TArrow(TInt, TBool))
                Expect.equal (formatType t) "int -> bool list" "list of arrows"
        ]

        testList "Substitution - apply" [
            testCase "apply to primitives returns unchanged" <| fun _ ->
                let s = Map.ofList [(0, TInt)]
                Expect.equal (apply s TInt) TInt "TInt unchanged"
                Expect.equal (apply s TBool) TBool "TBool unchanged"
                Expect.equal (apply s TString) TString "TString unchanged"

            testCase "apply empty substitution is identity" <| fun _ ->
                Expect.equal (apply empty (TVar 0)) (TVar 0) "empty subst is identity"
                Expect.equal (apply empty TInt) TInt "empty subst leaves TInt"

            testCase "apply simple substitution" <| fun _ ->
                let s = singleton 0 TInt
                Expect.equal (apply s (TVar 0)) TInt "TVar 0 -> TInt"

            testCase "apply transitive chain" <| fun _ ->
                let s = Map.ofList [(0, TVar 1); (1, TInt)]
                Expect.equal (apply s (TVar 0)) TInt "TVar 0 -> TVar 1 -> TInt"

            testCase "apply to unbound variable" <| fun _ ->
                let s = singleton 0 TInt
                Expect.equal (apply s (TVar 1)) (TVar 1) "TVar 1 remains unbound"

            testCase "apply to arrow type" <| fun _ ->
                let s = singleton 0 TInt
                let t = TArrow(TVar 0, TVar 1)
                Expect.equal (apply s t) (TArrow(TInt, TVar 1)) "applies to arrow domain"

            testCase "apply to tuple" <| fun _ ->
                let s = singleton 0 TInt
                let t = TTuple [TVar 0; TVar 1; TVar 0]
                Expect.equal (apply s t) (TTuple [TInt; TVar 1; TInt]) "applies to all tuple elements"

            testCase "apply to list" <| fun _ ->
                let s = singleton 0 TBool
                let t = TList (TVar 0)
                Expect.equal (apply s t) (TList TBool) "applies to list element type"
        ]

        testList "Substitution - compose" [
            testCase "compose with empty is identity" <| fun _ ->
                let s = singleton 0 TInt
                Expect.equal (compose empty s) s "compose empty s = s"
                Expect.equal (compose s empty) s "compose s empty = s"

            testCase "basic composition" <| fun _ ->
                let s1 = singleton 0 (TVar 1)
                let s2 = singleton 1 TInt
                let s = compose s2 s1
                Expect.equal (apply s (TVar 0)) TInt "s2 after s1: TVar 0 -> TVar 1 -> TInt"

            testCase "composition order matters" <| fun _ ->
                let s1 = singleton 0 TInt
                let s2 = singleton 0 TBool
                let s = compose s2 s1
                // s2 overwrites s1 binding for 0
                Expect.equal (apply s (TVar 0)) TBool "s2 overwrites s1"

            testCase "composition applies s2 to s1 values" <| fun _ ->
                let s1 = singleton 0 (TVar 1)
                let s2 = singleton 1 TString
                let s = compose s2 s1
                // s1 has {0 -> TVar 1}, s2 applies to give {0 -> TString}
                Expect.equal (apply s (TVar 0)) TString "s2 applied to s1 values"

            testCase "composition preserves both bindings" <| fun _ ->
                let s1 = singleton 0 (TVar 2)
                let s2 = singleton 1 TInt
                let s = compose s2 s1
                Expect.equal (apply s (TVar 0)) (TVar 2) "s1 binding for 0"
                Expect.equal (apply s (TVar 1)) TInt "s2 binding for 1"
        ]

        testList "Substitution - applyScheme" [
            testCase "respects bound variables" <| fun _ ->
                let s = singleton 0 TInt
                let scheme = Scheme([0], TVar 0)
                match applyScheme s scheme with
                | Scheme(vars, ty) ->
                    Expect.equal ty (TVar 0) "bound var 0 not substituted"
                    Expect.equal vars [0] "bound vars unchanged"

            testCase "substitutes free variables" <| fun _ ->
                let s = singleton 1 TInt
                let scheme = Scheme([0], TArrow(TVar 0, TVar 1))
                match applyScheme s scheme with
                | Scheme(vars, ty) ->
                    Expect.equal ty (TArrow(TVar 0, TInt)) "free var 1 substituted, bound var 0 not"

            testCase "handles monomorphic scheme" <| fun _ ->
                let s = singleton 0 TInt
                let scheme = Scheme([], TVar 0)
                match applyScheme s scheme with
                | Scheme(vars, ty) ->
                    Expect.equal ty TInt "monomorphic scheme gets substitution"
                    Expect.equal vars [] "no bound vars"
        ]

        testList "Free Variables - freeVars" [
            testCase "primitives have no free vars" <| fun _ ->
                Expect.equal (freeVars TInt) Set.empty "TInt has no free vars"
                Expect.equal (freeVars TBool) Set.empty "TBool has no free vars"
                Expect.equal (freeVars TString) Set.empty "TString has no free vars"

            testCase "type variable is free" <| fun _ ->
                Expect.equal (freeVars (TVar 0)) (Set.ofList [0]) "TVar 0 is free"
                Expect.equal (freeVars (TVar 5)) (Set.ofList [5]) "TVar 5 is free"

            testCase "arrow combines domain and range free vars" <| fun _ ->
                let t = TArrow(TVar 0, TVar 1)
                Expect.equal (freeVars t) (Set.ofList [0; 1]) "arrow has both free vars"

            testCase "arrow with duplicate free vars" <| fun _ ->
                let t = TArrow(TVar 0, TVar 0)
                Expect.equal (freeVars t) (Set.ofList [0]) "duplicate var counted once"

            testCase "tuple collects all free vars" <| fun _ ->
                let t = TTuple [TVar 0; TInt; TVar 1; TVar 0]
                Expect.equal (freeVars t) (Set.ofList [0; 1]) "tuple free vars"

            testCase "list element free vars" <| fun _ ->
                let t = TList (TVar 3)
                Expect.equal (freeVars t) (Set.ofList [3]) "list element free var"
        ]

        testList "Free Variables - freeVarsScheme" [
            testCase "bound variable excluded" <| fun _ ->
                let scheme = Scheme([0], TVar 0)
                Expect.equal (freeVarsScheme scheme) Set.empty "bound var 0 not free"

            testCase "free variable included" <| fun _ ->
                let scheme = Scheme([0], TArrow(TVar 0, TVar 1))
                Expect.equal (freeVarsScheme scheme) (Set.ofList [1]) "var 1 is free"

            testCase "multiple bound and free vars" <| fun _ ->
                let scheme = Scheme([0; 1], TArrow(TVar 0, TArrow(TVar 1, TVar 2)))
                Expect.equal (freeVarsScheme scheme) (Set.ofList [2]) "only var 2 is free"
        ]

        testList "Free Variables - freeVarsEnv" [
            testCase "empty environment" <| fun _ ->
                let env: TypeEnv = Map.empty
                Expect.equal (freeVarsEnv env) Set.empty "empty env has no free vars"

            testCase "environment with schemes" <| fun _ ->
                let env = Map.ofList [
                    ("x", Scheme([0], TVar 0))
                    ("y", Scheme([], TVar 1))
                    ("z", Scheme([2], TArrow(TVar 2, TVar 3)))
                ]
                Expect.equal (freeVarsEnv env) (Set.ofList [1; 3]) "free vars from all schemes"
        ]
    ]
