module BidirTests

open Expecto
open Type
open Ast
open Bidir
open Infer
open Diagnostic
open FSharp.Text.Lexing

/// Parse a string input and return the AST
let parse (input: string) : Expr =
    let lexbuf = LexBuffer<char>.FromString input
    Lexer.setInitialPos lexbuf "<test>"
    Parser.start Lexer.tokenize lexbuf

/// Synthesize type using bidirectional checker in empty environment
let synthEmpty expr =
    Bidir.synthTop Map.empty expr

/// Synthesize type using Infer module (Algorithm W) for comparison
let inferEmpty expr =
    let s, ty = Infer.infer Map.empty expr
    apply s ty

/// Helper to normalize type for comparison (remove specific var numbers)
let rec normalizeType ty =
    match ty with
    | TVar _ -> TVar 0  // Normalize all vars to 0 for comparison
    | TArrow(t1, t2) -> TArrow(normalizeType t1, normalizeType t2)
    | TTuple ts -> TTuple(List.map normalizeType ts)
    | TList t -> TList(normalizeType t)
    | _ -> ty

/// Compare types ignoring specific type variable numbers
let typesEqual t1 t2 =
    normalizeType t1 = normalizeType t2

[<Tests>]
let bidirTests = testList "Bidirectional Type Checking" [
    testList "Literals - BIDIR-03 (synthesis mode)" [
        test "Number synthesizes to int" {
            let result = synthEmpty (Number (42, unknownSpan))
            Expect.equal result TInt "Number should synthesize to int"
        }

        test "Bool synthesizes to bool" {
            let result = synthEmpty (Bool (true, unknownSpan))
            Expect.equal result TBool "Bool should synthesize to bool"
        }

        test "String synthesizes to string" {
            let result = synthEmpty (String ("hello", unknownSpan))
            Expect.equal result TString "String should synthesize to string"
        }

        test "Unit synthesizes to unit" {
            let result = parse "()" |> synthEmpty
            Expect.equal result (TTuple []) "Unit should synthesize to unit"
        }
    ]

    testList "Variables - synthesis mode" [
        test "Variable synthesizes to instantiated scheme" {
            let env = Map.ofList [("x", Scheme([], TInt))]
            let result = Bidir.synthTop env (Var ("x", unknownSpan))
            Expect.equal result TInt "Variable should synthesize to monomorphic type"
        }

        test "Polymorphic variable instantiates to fresh var" {
            let env = Map.ofList [("id", Scheme([0], TArrow(TVar 0, TVar 0)))]
            let result = Bidir.synthTop env (Var ("id", unknownSpan))
            match result with
            | TArrow(TVar a, TVar b) when a = b && a >= 1000 ->
                Expect.isTrue true "Polymorphic var should instantiate to fresh arrow"
            | _ -> failtest $"Expected fresh arrow type, got {result}"
        }
    ]

    testList "Lambda - BIDIR-05 (hybrid approach with fresh vars)" [
        test "Unannotated lambda synthesizes with fresh var" {
            let expr = parse "fun x -> x"
            let result = synthEmpty expr
            match result with
            | TArrow(TVar a, TVar b) when a = b && a >= 1000 ->
                Expect.isTrue true "Unannotated lambda should use fresh var for parameter"
            | _ -> failtest $"Expected arrow with fresh vars, got {result}"
        }

        test "Annotated lambda synthesizes with specified type" {
            let expr = parse "fun (x : int) -> x"
            let result = synthEmpty expr
            Expect.equal result (TArrow(TInt, TInt)) "Annotated lambda should use annotation"
        }

        test "Nested lambda synthesizes correctly" {
            let expr = parse "fun x -> fun y -> x"
            let result = synthEmpty expr
            match result with
            | TArrow(TVar a, TArrow(TVar _, TVar c)) when a = c && a >= 1000 ->
                Expect.isTrue true "Nested lambda should maintain variable consistency"
            | _ -> failtest $"Expected nested arrow type, got {result}"
        }
    ]

    testList "Application - BIDIR-03 (synthesis mode)" [
        test "Simple application synthesizes result type" {
            let expr = parse "(fun x -> x) 42"
            let result = synthEmpty expr
            Expect.equal result TInt "Application should synthesize result type"
        }

        test "Multi-argument application" {
            let expr = parse "(fun x -> fun y -> x) 1 2"
            let result = synthEmpty expr
            Expect.equal result TInt "Multi-arg application should synthesize correctly"
        }

        test "Applying to wrong type fails" {
            Expect.throws
                (fun () -> parse "(fun (x : int) -> x) true" |> synthEmpty |> ignore)
                "Should fail when applying bool to int parameter"
        }
    ]

    testList "Let-polymorphism - BIDIR-07" [
        test "Let generalizes over free variables" {
            let expr = parse "let id = fun x -> x in id 42"
            let result = synthEmpty expr
            Expect.equal result TInt "Let should generalize id and instantiate to int"
        }

        test "Let-bound polymorphic function can be used at different types" {
            let expr = parse "let id = fun x -> x in (id 42, id true)"
            let result = synthEmpty expr
            Expect.equal result (TTuple [TInt; TBool]) "Let-bound id should work at multiple types"
        }

        test "Let with type annotation" {
            let expr = parse "let (x : int) = 42 in x"
            let result = synthEmpty expr
            Expect.equal result TInt "Let with annotation should respect annotation"
        }

        test "Nested let expressions" {
            let expr = parse "let x = 1 in let y = 2 in x"
            let result = synthEmpty expr
            Expect.equal result TInt "Nested let should work correctly"
        }
    ]

    testList "If expression" [
        test "If synthesizes branch type" {
            let expr = parse "if true then 1 else 2"
            let result = synthEmpty expr
            Expect.equal result TInt "If should synthesize branch type"
        }

        test "If requires bool condition" {
            Expect.throws
                (fun () -> parse "if 1 then 2 else 3" |> synthEmpty |> ignore)
                "If should require bool condition"
        }

        test "If requires matching branch types" {
            Expect.throws
                (fun () -> parse "if true then 1 else true" |> synthEmpty |> ignore)
                "If branches should have matching types"
        }
    ]

    testList "Tuples" [
        test "Empty tuple synthesizes to unit" {
            let expr = parse "()"
            let result = synthEmpty expr
            Expect.equal result (TTuple []) "Empty tuple should be unit"
        }

        test "Pair synthesizes to tuple type" {
            let expr = parse "(1, true)"
            let result = synthEmpty expr
            Expect.equal result (TTuple [TInt; TBool]) "Pair should synthesize to tuple"
        }

        test "Triple synthesizes correctly" {
            let expr = parse "(1, true, \"hi\")"
            let result = synthEmpty expr
            Expect.equal result (TTuple [TInt; TBool; TString]) "Triple should synthesize correctly"
        }
    ]

    testList "Lists" [
        test "Empty list synthesizes to generic list" {
            let expr = parse "[]"
            let result = synthEmpty expr
            match result with
            | TList (TVar _) -> Expect.isTrue true "Empty list should be polymorphic"
            | _ -> failtest $"Expected list type, got {result}"
        }

        test "Non-empty list synthesizes to list of element type" {
            let expr = parse "[1, 2, 3]"
            let result = synthEmpty expr
            Expect.equal result (TList TInt) "Int list should synthesize to list int"
        }

        test "List with mixed types fails" {
            Expect.throws
                (fun () -> parse "[1, true]" |> synthEmpty |> ignore)
                "List with mixed types should fail"
        }
    ]

    testList "Match expression" [
        test "Match on tuple destructures correctly" {
            let expr = parse "match (1, true) with (x, y) -> x"
            let result = synthEmpty expr
            Expect.equal result TInt "Match should extract tuple elements"
        }

        test "Match on list with cons pattern" {
            let expr = parse "match [1, 2] with [] -> 0 | x :: xs -> x"
            let result = synthEmpty expr
            Expect.equal result TInt "Match on list should work with cons pattern"
        }

        test "Match with wildcard pattern" {
            let expr = parse "match 42 with _ -> true"
            let result = synthEmpty expr
            Expect.equal result TBool "Match with wildcard should work"
        }
    ]

    testList "LetRec" [
        test "Simple recursive function" {
            let expr = parse "let rec fact = fun n -> if n == 0 then 1 else n * fact (n - 1) in fact 5"
            let result = synthEmpty expr
            Expect.equal result TInt "Recursive factorial should synthesize to int"
        }

        test "Mutually recursive functions via letrec" {
            let expr = parse "let rec isEven = fun n -> if n == 0 then true else isOdd (n - 1) and isOdd = fun n -> if n == 0 then false else isEven (n - 1) in isEven 4"
            let result = synthEmpty expr
            Expect.equal result TBool "Mutual recursion should work"
        }
    ]

    testList "Binary operators" [
        test "Arithmetic operators" {
            let expr = parse "1 + 2 * 3"
            let result = synthEmpty expr
            Expect.equal result TInt "Arithmetic should synthesize to int"
        }

        test "Comparison operators" {
            let expr = parse "1 < 2"
            let result = synthEmpty expr
            Expect.equal result TBool "Comparison should synthesize to bool"
        }

        test "Equality operators" {
            let expr = parse "1 == 2"
            let result = synthEmpty expr
            Expect.equal result TBool "Equality should synthesize to bool"
        }

        test "Logical operators" {
            let expr = parse "true && false"
            let result = synthEmpty expr
            Expect.equal result TBool "Logical operators should synthesize to bool"
        }

        test "String concatenation" {
            let expr = parse "\"hello\" ^ \" world\""
            let result = synthEmpty expr
            Expect.equal result TString "String concat should synthesize to string"
        }
    ]

    testList "Backward compatibility with Algorithm W" [
        test "Literals produce same types" {
            let tests = ["42"; "true"; "\"hello\""; "()"]
            for input in tests do
                let expr = parse input
                let bidirTy = synthEmpty expr
                let inferTy = inferEmpty expr
                Expect.equal bidirTy inferTy $"Bidir and Infer should agree on {input}"
        }

        test "Lambda inference matches Algorithm W (modulo var names)" {
            let expr = parse "fun x -> x"
            let bidirTy = synthEmpty expr
            let inferTy = inferEmpty expr
            Expect.isTrue (typesEqual bidirTy inferTy) "Lambda types should match (modulo vars)"
        }

        test "Application produces same types" {
            let expr = parse "(fun x -> x) 42"
            let bidirTy = synthEmpty expr
            let inferTy = inferEmpty expr
            Expect.equal bidirTy inferTy "Application should produce same type"
        }

        test "Let-polymorphism matches Algorithm W" {
            let expr = parse "let id = fun x -> x in (id 42, id true)"
            let bidirTy = synthEmpty expr
            let inferTy = inferEmpty expr
            Expect.equal bidirTy inferTy "Let-polymorphism should match"
        }

        test "If expression matches Algorithm W" {
            let expr = parse "if true then 1 else 2"
            let bidirTy = synthEmpty expr
            let inferTy = inferEmpty expr
            Expect.equal bidirTy inferTy "If expression should match"
        }

        test "Tuple construction matches Algorithm W" {
            let expr = parse "(1, true, \"hi\")"
            let bidirTy = synthEmpty expr
            let inferTy = inferEmpty expr
            Expect.equal bidirTy inferTy "Tuple construction should match"
        }

        test "List construction matches Algorithm W" {
            let expr = parse "[1, 2, 3]"
            let bidirTy = synthEmpty expr
            let inferTy = inferEmpty expr
            Expect.equal bidirTy inferTy "List construction should match"
        }

        test "Complex expression matches Algorithm W" {
            let expr = parse "let double = fun x -> x + x in let quad = fun x -> double (double x) in quad 5"
            let bidirTy = synthEmpty expr
            let inferTy = inferEmpty expr
            Expect.equal bidirTy inferTy "Complex expression should match"
        }
    ]
]
