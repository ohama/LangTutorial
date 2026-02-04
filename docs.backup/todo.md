# FunLang Type System Implementation Plan

## 개요

FunLang에 Hindley-Milner 타입 시스템을 구현하여 정적 타입 검사와 타입 추론을 지원한다.

**목표:**

- 명시적 타입 주석 없이 모든 표현식의 타입을 추론
- 컴파일 시점에 타입 오류 검출
- 다형성(Polymorphism) 지원 (let-polymorphism)

**현재 FunLang 타입:**

- `int` — 정수
- `bool` — 불리언
- `string` — 문자열
- `'a -> 'b` — 함수
- `'a * 'b * ...` — 튜플
- `'a list` — 리스트

---

## Phase 1: 타입 정의 (Type.fs)

### 1.1 타입 AST

```fsharp
// Type.fs
module Type

/// 타입 표현
type Type =
    | TInt                           // int
    | TBool                          // bool
    | TString                        // string
    | TVar of int                    // 타입 변수 'a, 'b, ...
    | TArrow of Type * Type          // 함수 타입 'a -> 'b
    | TTuple of Type list            // 튜플 타입 'a * 'b
    | TList of Type                  // 리스트 타입 'a list

/// 타입 스킴 (다형성)
/// forall 'a 'b. 'a -> 'b -> 'a
type Scheme = Scheme of vars: int list * ty: Type

/// 타입 환경: 변수 이름 -> 타입 스킴
type TypeEnv = Map<string, Scheme>

/// 타입 대체: 타입 변수 -> 타입
type Subst = Map<int, Type>
```

### 1.2 타입 출력

```fsharp
/// 타입을 문자열로 변환
let rec formatType = function
    | TInt -> "int"
    | TBool -> "bool"
    | TString -> "string"
    | TVar n -> sprintf "'%c" (char (97 + n % 26))  // 'a, 'b, ...
    | TArrow (t1, t2) ->
        let left = match t1 with TArrow _ -> sprintf "(%s)" (formatType t1) | _ -> formatType t1
        sprintf "%s -> %s" left (formatType t2)
    | TTuple ts -> ts |> List.map formatType |> String.concat " * "
    | TList t -> sprintf "%s list" (formatType t)
```

---

## Phase 2: 대체(Substitution) 연산

### 2.1 기본 연산

```fsharp
// Subst.fs
module Subst

open Type

/// 빈 대체
let empty: Subst = Map.empty

/// 단일 대체 생성
let singleton (v: int) (t: Type): Subst = Map.ofList [(v, t)]

/// 타입에 대체 적용
let rec apply (s: Subst) = function
    | TInt -> TInt
    | TBool -> TBool
    | TString -> TString
    | TVar n ->
        match Map.tryFind n s with
        | Some t -> apply s t  // 재귀적으로 적용 (chain substitution)
        | None -> TVar n
    | TArrow (t1, t2) -> TArrow (apply s t1, apply s t2)
    | TTuple ts -> TTuple (List.map (apply s) ts)
    | TList t -> TList (apply s t)

/// 대체 합성: s2 ∘ s1 (s1 먼저 적용, 그 다음 s2)
let compose (s2: Subst) (s1: Subst): Subst =
    let s1' = Map.map (fun _ t -> apply s2 t) s1
    Map.fold (fun acc k v -> Map.add k v acc) s1' s2

/// 스킴에 대체 적용 (bound 변수는 제외)
let applyScheme (s: Subst) (Scheme (vars, ty)): Scheme =
    let s' = List.fold (fun acc v -> Map.remove v acc) s vars
    Scheme (vars, apply s' ty)

/// 환경에 대체 적용
let applyEnv (s: Subst) (env: TypeEnv): TypeEnv =
    Map.map (fun _ scheme -> applyScheme s scheme) env
```

### 2.2 자유 타입 변수

```fsharp
/// 타입의 자유 타입 변수
let rec freeVars = function
    | TInt | TBool | TString -> Set.empty
    | TVar n -> Set.singleton n
    | TArrow (t1, t2) -> Set.union (freeVars t1) (freeVars t2)
    | TTuple ts -> ts |> List.map freeVars |> Set.unionMany
    | TList t -> freeVars t

/// 스킴의 자유 타입 변수
let freeVarsScheme (Scheme (vars, ty)) =
    Set.difference (freeVars ty) (Set.ofList vars)

/// 환경의 자유 타입 변수
let freeVarsEnv (env: TypeEnv) =
    env |> Map.values |> Seq.map freeVarsScheme |> Set.unionMany
```

---

## Phase 3: 단일화(Unification)

### 3.1 Occurs Check

```fsharp
// Unify.fs
module Unify

open Type
open Subst

/// Occurs check: 타입 변수가 타입에 나타나는지 확인
/// (무한 타입 방지: 'a = 'a -> int 같은 경우)
let rec occurs (v: int) = function
    | TInt | TBool | TString -> false
    | TVar n -> n = v
    | TArrow (t1, t2) -> occurs v t1 || occurs v t2
    | TTuple ts -> List.exists (occurs v) ts
    | TList t -> occurs v t
```

### 3.2 단일화 알고리즘

```fsharp
/// 타입 오류
exception TypeError of string

/// 두 타입을 같게 만드는 대체를 찾음
let rec unify (t1: Type) (t2: Type): Subst =
    match t1, t2 with
    | TInt, TInt -> empty
    | TBool, TBool -> empty
    | TString, TString -> empty

    | TVar n, t | t, TVar n ->
        if t = TVar n then empty
        elif occurs n t then
            raise (TypeError (sprintf "Infinite type: %s = %s" (formatType (TVar n)) (formatType t)))
        else
            singleton n t

    | TArrow (a1, b1), TArrow (a2, b2) ->
        let s1 = unify a1 a2
        let s2 = unify (apply s1 b1) (apply s1 b2)
        compose s2 s1

    | TTuple ts1, TTuple ts2 when List.length ts1 = List.length ts2 ->
        List.fold2 (fun s t1 t2 ->
            let s' = unify (apply s t1) (apply s t2)
            compose s' s
        ) empty ts1 ts2

    | TList t1, TList t2 ->
        unify t1 t2

    | _ ->
        raise (TypeError (sprintf "Cannot unify %s with %s" (formatType t1) (formatType t2)))
```

---

## Phase 4: 타입 추론 (Algorithm W)

### 4.1 타입 변수 생성

```fsharp
// Infer.fs
module Infer

open Ast
open Type
open Subst
open Unify

/// 새 타입 변수 생성
let mutable private nextVar = 0

let freshVar () =
    let n = nextVar
    nextVar <- nextVar + 1
    TVar n

let reset () = nextVar <- 0
```

### 4.2 인스턴스화와 일반화

```fsharp
/// 스킴을 타입으로 인스턴스화 (forall 변수를 새 변수로 대체)
let instantiate (Scheme (vars, ty)): Type =
    let s = vars |> List.map (fun v -> (v, freshVar ())) |> Map.ofList
    apply s ty

/// 타입을 스킴으로 일반화 (환경에 없는 자유 변수를 forall로 묶음)
let generalize (env: TypeEnv) (ty: Type): Scheme =
    let envFree = freeVarsEnv env
    let tyFree = freeVars ty
    let vars = Set.difference tyFree envFree |> Set.toList
    Scheme (vars, ty)
```

### 4.3 Algorithm W

```fsharp
/// 표현식의 타입을 추론
/// 반환: (대체, 추론된 타입)
let rec infer (env: TypeEnv) (expr: Expr): Subst * Type =
    match expr with
    // 리터럴
    | Number _ -> (empty, TInt)
    | Bool _ -> (empty, TBool)
    | String _ -> (empty, TString)

    // 변수
    | Var x ->
        match Map.tryFind x env with
        | Some scheme -> (empty, instantiate scheme)
        | None -> raise (TypeError (sprintf "Unbound variable: %s" x))

    // 산술 연산 (int -> int -> int)
    | Add (e1, e2) | Subtract (e1, e2) | Multiply (e1, e2) | Divide (e1, e2) ->
        let s1, t1 = infer env e1
        let s2, t2 = infer (applyEnv s1 env) e2
        let s3 = unify (apply s2 t1) TInt
        let s4 = unify (apply s3 t2) TInt
        (compose s4 (compose s3 (compose s2 s1)), TInt)

    | Negate e ->
        let s, t = infer env e
        let s' = unify t TInt
        (compose s' s, TInt)

    // 비교 연산 ('a -> 'a -> bool)
    | Equal (e1, e2) | NotEqual (e1, e2) ->
        let s1, t1 = infer env e1
        let s2, t2 = infer (applyEnv s1 env) e2
        let s3 = unify (apply s2 t1) t2
        (compose s3 (compose s2 s1), TBool)

    | LessThan (e1, e2) | GreaterThan (e1, e2)
    | LessEqual (e1, e2) | GreaterEqual (e1, e2) ->
        let s1, t1 = infer env e1
        let s2, t2 = infer (applyEnv s1 env) e2
        let s3 = unify (apply s2 t1) TInt
        let s4 = unify (apply s3 t2) TInt
        (compose s4 (compose s3 (compose s2 s1)), TBool)

    // 논리 연산 (bool -> bool -> bool)
    | And (e1, e2) | Or (e1, e2) ->
        let s1, t1 = infer env e1
        let s2, t2 = infer (applyEnv s1 env) e2
        let s3 = unify (apply s2 t1) TBool
        let s4 = unify (apply s3 t2) TBool
        (compose s4 (compose s3 (compose s2 s1)), TBool)

    // If 표현식
    | If (cond, then_, else_) ->
        let s1, t1 = infer env cond
        let s2 = unify t1 TBool
        let env' = applyEnv (compose s2 s1) env
        let s3, t2 = infer env' then_
        let s4, t3 = infer (applyEnv s3 env') else_
        let s5 = unify (apply s4 t2) t3
        (compose s5 (compose s4 (compose s3 (compose s2 s1))), apply s5 t3)

    // Let 바인딩 (let-polymorphism)
    | Let (x, e1, e2) ->
        let s1, t1 = infer env e1
        let env' = applyEnv s1 env
        let scheme = generalize env' t1
        let s2, t2 = infer (Map.add x scheme env') e2
        (compose s2 s1, t2)

    // Lambda
    | Lambda (x, body) ->
        let tv = freshVar ()
        let env' = Map.add x (Scheme ([], tv)) env
        let s, t = infer env' body
        (s, TArrow (apply s tv, t))

    // 함수 적용
    | App (func, arg) ->
        let s1, t1 = infer env func
        let s2, t2 = infer (applyEnv s1 env) arg
        let tv = freshVar ()
        let s3 = unify (apply s2 t1) (TArrow (t2, tv))
        (compose s3 (compose s2 s1), apply s3 tv)

    // Let Rec
    | LetRec (f, x, body, inExpr) ->
        let tv1 = freshVar ()  // 함수 타입
        let tv2 = freshVar ()  // 인자 타입
        let funcType = TArrow (tv2, tv1)
        let env' = Map.add f (Scheme ([], funcType)) env
        let env'' = Map.add x (Scheme ([], tv2)) env'
        let s1, t1 = infer env'' body
        let s2 = unify (apply s1 tv1) t1
        let s = compose s2 s1
        let scheme = generalize (applyEnv s env) (apply s funcType)
        let s3, t3 = infer (Map.add f scheme (applyEnv s env)) inExpr
        (compose s3 s, t3)

    // 튜플
    | Tuple es ->
        let rec inferTuple env s ts = function
            | [] -> (s, TTuple (List.rev ts))
            | e :: rest ->
                let s', t = infer (applyEnv s env) e
                inferTuple env (compose s' s) (apply s' t :: ts) rest
        inferTuple env empty [] es

    // 빈 리스트 ('a list)
    | EmptyList ->
        (empty, TList (freshVar ()))

    // 리스트 리터럴
    | List [] ->
        (empty, TList (freshVar ()))
    | List (e :: es) ->
        let s1, t1 = infer env e
        let rec checkRest s elemType = function
            | [] -> (s, TList elemType)
            | e :: rest ->
                let s', t = infer (applyEnv s env) e
                let s'' = unify (apply s' elemType) t
                checkRest (compose s'' (compose s' s)) (apply s'' elemType) rest
        checkRest s1 t1 es

    // Cons
    | Cons (head, tail) ->
        let s1, t1 = infer env head
        let s2, t2 = infer (applyEnv s1 env) tail
        let s3 = unify t2 (TList (apply s2 t1))
        (compose s3 (compose s2 s1), apply s3 t2)

    // Match 표현식
    | Match (scrutinee, clauses) ->
        let s1, scrutineeType = infer env scrutinee
        let resultType = freshVar ()
        let rec checkClauses s = function
            | [] -> (s, apply s resultType)
            | (pat, body) :: rest ->
                let s', patType, bindings = inferPattern pat
                let s'' = unify (apply s' (apply s scrutineeType)) patType
                let env' = bindings |> List.fold (fun e (x, t) ->
                    Map.add x (Scheme ([], apply s'' t)) e) (applyEnv (compose s'' (compose s' s)) env)
                let s3, bodyType = infer env' body
                let s4 = unify (apply s3 (apply (compose s'' s') (apply s resultType))) bodyType
                checkClauses (compose s4 (compose s3 (compose s'' (compose s' s)))) rest
        checkClauses s1 clauses

    // LetPat
    | LetPat (pat, e1, e2) ->
        let s1, t1 = infer env e1
        let s2, patType, bindings = inferPattern pat
        let s3 = unify (apply s2 t1) patType
        let s = compose s3 (compose s2 s1)
        let env' = bindings |> List.fold (fun e (x, t) ->
            let scheme = generalize (applyEnv s e) (apply s t)
            Map.add x scheme e) (applyEnv s env)
        let s4, t2 = infer env' e2
        (compose s4 s, t2)

/// 패턴의 타입을 추론
/// 반환: (대체, 패턴 타입, 바인딩 리스트)
and inferPattern (pat: Pattern): Subst * Type * (string * Type) list =
    match pat with
    | VarPat x ->
        let tv = freshVar ()
        (empty, tv, [(x, tv)])

    | WildcardPat ->
        (empty, freshVar (), [])

    | ConstPat (IntConst _) ->
        (empty, TInt, [])

    | ConstPat (BoolConst _) ->
        (empty, TBool, [])

    | TuplePat pats ->
        let rec go s ts bindings = function
            | [] -> (s, TTuple (List.rev ts), bindings)
            | p :: rest ->
                let s', t, bs = inferPattern p
                go (compose s' s) (t :: ts) (bindings @ bs) rest
        go empty [] [] pats

    | EmptyListPat ->
        (empty, TList (freshVar ()), [])

    | ConsPat (head, tail) ->
        let s1, t1, bs1 = inferPattern head
        let s2, t2, bs2 = inferPattern tail
        let s3 = unify t2 (TList (apply s2 t1))
        (compose s3 (compose s2 s1), apply s3 t2, bs1 @ bs2)
```

---

## Phase 5: 통합

### 5.1 타입 검사 함수

```fsharp
// TypeCheck.fs
module TypeCheck

open Ast
open Type
open Infer

/// 초기 타입 환경 (Prelude 함수들)
let initialEnv: TypeEnv =
    Map.ofList [
        // id : 'a -> 'a
        "id", Scheme ([0], TArrow (TVar 0, TVar 0))

        // const : 'a -> 'b -> 'a
        "const", Scheme ([0; 1], TArrow (TVar 0, TArrow (TVar 1, TVar 0)))

        // compose : ('b -> 'c) -> ('a -> 'b) -> 'a -> 'c
        "compose", Scheme ([0; 1; 2],
            TArrow (TArrow (TVar 1, TVar 2),
                TArrow (TArrow (TVar 0, TVar 1),
                    TArrow (TVar 0, TVar 2))))

        // hd : 'a list -> 'a
        "hd", Scheme ([0], TArrow (TList (TVar 0), TVar 0))

        // tl : 'a list -> 'a list
        "tl", Scheme ([0], TArrow (TList (TVar 0), TList (TVar 0)))

        // map : ('a -> 'b) -> 'a list -> 'b list
        "map", Scheme ([0; 1],
            TArrow (TArrow (TVar 0, TVar 1),
                TArrow (TList (TVar 0), TList (TVar 1))))

        // filter : ('a -> bool) -> 'a list -> 'a list
        "filter", Scheme ([0],
            TArrow (TArrow (TVar 0, TBool),
                TArrow (TList (TVar 0), TList (TVar 0))))

        // fold : ('a -> 'b -> 'a) -> 'a -> 'b list -> 'a
        "fold", Scheme ([0; 1],
            TArrow (TArrow (TVar 0, TArrow (TVar 1, TVar 0)),
                TArrow (TVar 0,
                    TArrow (TList (TVar 1), TVar 0))))

        // length : 'a list -> int
        "length", Scheme ([0], TArrow (TList (TVar 0), TInt))

        // reverse : 'a list -> 'a list
        "reverse", Scheme ([0], TArrow (TList (TVar 0), TList (TVar 0)))

        // append : 'a list -> 'a list -> 'a list
        "append", Scheme ([0],
            TArrow (TList (TVar 0),
                TArrow (TList (TVar 0), TList (TVar 0))))
    ]

/// 표현식 타입 검사
let typecheck (expr: Expr): Result<Type, string> =
    try
        reset ()
        let s, t = infer initialEnv expr
        Ok (Subst.apply s t)
    with
    | TypeError msg -> Error msg
```

### 5.2 CLI 통합

```fsharp
// Program.fs 수정
[<EntryPoint>]
let main argv =
    match parseArgs argv with
    | Ok args ->
        let code = readSource args
        let ast = parse code

        // 타입 검사 (--no-typecheck 옵션 없으면)
        if not args.NoTypeCheck then
            match TypeCheck.typecheck ast with
            | Ok ty ->
                if args.EmitType then
                    printfn "Type: %s" (Type.formatType ty)
            | Error msg ->
                eprintfn "Type error: %s" msg
                exit 1

        // 평가
        let result = eval Map.empty ast
        printfn "%s" (Format.formatValue result)
        0
    | Error msg ->
        eprintfn "%s" msg
        1
```

---

## Phase 6: 테스트

### 6.1 단위 테스트

```fsharp
// InferTests.fs
module InferTests

open Expecto
open Type
open TypeCheck

[<Tests>]
let tests = testList "Type Inference" [
    test "int literal" {
        let result = typecheck (Number 42)
        Expect.equal result (Ok TInt) ""
    }

    test "bool literal" {
        let result = typecheck (Bool true)
        Expect.equal result (Ok TBool) ""
    }

    test "arithmetic" {
        let result = typecheck (Add (Number 1, Number 2))
        Expect.equal result (Ok TInt) ""
    }

    test "comparison" {
        let result = typecheck (LessThan (Number 1, Number 2))
        Expect.equal result (Ok TBool) ""
    }

    test "identity function" {
        // fun x -> x : 'a -> 'a
        let result = typecheck (Lambda ("x", Var "x"))
        match result with
        | Ok (TArrow (TVar a, TVar b)) -> Expect.equal a b ""
        | _ -> failtest "Expected arrow type"
    }

    test "let polymorphism" {
        // let id = fun x -> x in (id 1, id true)
        let expr = Let ("id", Lambda ("x", Var "x"),
                        Tuple [App (Var "id", Number 1);
                               App (Var "id", Bool true)])
        let result = typecheck expr
        Expect.equal result (Ok (TTuple [TInt; TBool])) ""
    }

    test "type error: if branches" {
        // if true then 1 else "hello"
        let result = typecheck (If (Bool true, Number 1, String "hello"))
        Expect.isError result ""
    }

    test "type error: arithmetic with bool" {
        let result = typecheck (Add (Number 1, Bool true))
        Expect.isError result ""
    }

    test "list type" {
        let result = typecheck (List [Number 1; Number 2; Number 3])
        Expect.equal result (Ok (TList TInt)) ""
    }

    test "type error: heterogeneous list" {
        let result = typecheck (List [Number 1; Bool true])
        Expect.isError result ""
    }
]
```

### 6.2 fslit 테스트

```
// tests/typecheck/basic.flt
// --- Command: dotnet run --project FunLang -- --emit-type --expr %input
// --- Input:
fun x -> x + 1
// --- Output:
Type: int -> int
```

```
// tests/typecheck/error.flt
// --- Command: dotnet run --project FunLang -- --expr %input
// --- ExpectFail: 1
// --- Input:
1 + true
// --- ErrorOutput:
Type error: Cannot unify int with bool
```

---

## 구현 순서

| 단계 | 파일 | 내용 |
|------|------|------|
| 1 | Type.fs | 타입 AST, Scheme, TypeEnv |
| 2 | Subst.fs | 대체 연산, 자유 변수 |
| 3 | Unify.fs | 단일화 알고리즘 |
| 4 | Infer.fs | Algorithm W, 타입 추론 |
| 5 | TypeCheck.fs | 초기 환경, typecheck 함수 |
| 6 | Program.fs | CLI 통합, --emit-type |
| 7 | Tests | 단위 테스트, fslit 테스트 |

---

## 참고 자료

- [Write You a Haskell - Hindley-Milner](http://dev.stephendiehl.com/fun/006_hindley_milner.html)
- [Algorithm W Step by Step](https://github.com/wh5a/Algorithm-W-Step-By-Step)
- [Typing Haskell in Haskell](https://web.cecs.pdx.edu/~mpj/thih/)
- [Types and Programming Languages (TAPL)](https://www.cis.upenn.edu/~bcpierce/tapl/)

---

## 확장 가능 기능

### 타입 주석 (선택적)

```
let x: int = 5
let f: int -> int = fun x -> x + 1
```

### 타입 클래스 (고급)

```
class Eq a where
    (==) : a -> a -> bool

instance Eq int where
    (==) = ...
```

### 레코드 타입 (고급)

```
type Person = { name: string, age: int }
let p = { name = "Alice", age = 30 }
p.name  // "Alice"
```

---

*Last updated: 2026-02-01*
