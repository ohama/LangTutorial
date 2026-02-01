module Infer

open Type
open Unify

/// Generate fresh type variable (unique ID via mutable counter)
let freshVar =
    let counter = ref 0
    fun () ->
        let n = !counter
        counter := n + 1
        TVar n

/// Instantiate scheme: replace bound vars with fresh type variables
/// Example: forall 'a. 'a -> 'a becomes 'c -> 'c (with fresh 'c)
let instantiate (Scheme (vars, ty)): Type =
    match vars with
    | [] -> ty  // Monomorphic - no substitution needed
    | _ ->
        let freshVars = List.map (fun _ -> freshVar()) vars
        let subst = List.zip vars freshVars |> Map.ofList
        apply subst ty

/// Generalize type: abstract over free vars not in environment
/// Creates polymorphic scheme at let boundaries
let generalize (env: TypeEnv) (ty: Type): Scheme =
    let envFree = freeVarsEnv env
    let tyFree = freeVars ty
    let vars = Set.difference tyFree envFree |> Set.toList
    Scheme (vars, ty)

open Ast

/// Infer type for expression (Algorithm W)
/// Returns (substitution, inferred type)
let rec infer (env: TypeEnv) (expr: Expr): Subst * Type =
    match expr with
    // === Literals (INFER-04) ===
    | Number _ -> (empty, TInt)
    | Bool _ -> (empty, TBool)
    | String _ -> (empty, TString)

    // === Variable reference (INFER-06) ===
    | Var name ->
        match Map.tryFind name env with
        | Some scheme -> (empty, instantiate scheme)
        | None -> raise (TypeError (sprintf "Unbound variable: %s" name))

    // === Arithmetic operators (INFER-05) ===
    // All arithmetic: int -> int -> int
    | Add (e1, e2) | Subtract (e1, e2) | Multiply (e1, e2) | Divide (e1, e2) ->
        inferBinaryOp env e1 e2 TInt TInt TInt

    // Unary minus: int -> int
    | Negate e ->
        let s, t = infer env e
        let s' = unify (apply s t) TInt
        (compose s' s, TInt)

    // === Comparison operators (INFER-05) ===
    // Comparison: int -> int -> bool
    | Equal (e1, e2) | NotEqual (e1, e2)
    | LessThan (e1, e2) | GreaterThan (e1, e2)
    | LessEqual (e1, e2) | GreaterEqual (e1, e2) ->
        inferBinaryOp env e1 e2 TInt TInt TBool

    // === Logical operators (INFER-05) ===
    // Logical: bool -> bool -> bool
    | And (e1, e2) | Or (e1, e2) ->
        inferBinaryOp env e1 e2 TBool TBool TBool

    // Placeholder for remaining cases (will be implemented in later plans)
    | _ -> failwith "Not yet implemented"

/// Helper: infer binary operator with expected operand and result types
and inferBinaryOp env e1 e2 leftTy rightTy resultTy =
    let s1, t1 = infer env e1
    let s2, t2 = infer (applyEnv s1 env) e2
    let s3 = unify (apply s2 t1) leftTy
    let s4 = unify (apply s3 t2) rightTy
    (compose s4 (compose s3 (compose s2 s1)), resultTy)
