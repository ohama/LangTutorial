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

/// Infer pattern type and extract bindings (INFER-15)
/// Returns (environment with bindings, pattern type)
let rec inferPattern (pat: Pattern): TypeEnv * Type =
    match pat with
    | VarPat name ->
        let ty = freshVar()
        (Map.ofList [(name, Scheme ([], ty))], ty)

    | WildcardPat ->
        (Map.empty, freshVar())

    | TuplePat pats ->
        let envTys = List.map inferPattern pats
        let env = envTys
                  |> List.map fst
                  |> List.fold (fun acc m -> Map.fold (fun a k v -> Map.add k v a) acc m) Map.empty
        let tys = envTys |> List.map snd
        (env, TTuple tys)

    | EmptyListPat ->
        (Map.empty, TList (freshVar()))

    | ConsPat (headPat, tailPat) ->
        let headEnv, headTy = inferPattern headPat
        let tailEnv, tailTy = inferPattern tailPat
        // Note: tailTy should be TList headTy, but actual unification happens in Match
        // We return TList headTy as the pattern type
        let env = Map.fold (fun acc k v -> Map.add k v acc) headEnv tailEnv
        (env, TList headTy)

    | ConstPat (IntConst _) ->
        (Map.empty, TInt)

    | ConstPat (BoolConst _) ->
        (Map.empty, TBool)

/// Infer type for expression (Algorithm W)
/// Returns (substitution, inferred type)
and infer (env: TypeEnv) (expr: Expr): Subst * Type =
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

    // === Lambda (INFER-08) ===
    | Lambda (param, body) ->
        let paramTy = freshVar()
        let bodyEnv = Map.add param (Scheme ([], paramTy)) env
        let s, bodyTy = infer bodyEnv body
        (s, TArrow (apply s paramTy, bodyTy))

    // === Application (INFER-08) ===
    | App (func, arg) ->
        let s1, funcTy = infer env func
        let s2, argTy = infer (applyEnv s1 env) arg
        let resultTy = freshVar()
        let s3 = unify (apply s2 funcTy) (TArrow (argTy, resultTy))
        (compose s3 (compose s2 s1), apply s3 resultTy)

    // === If expression (INFER-10) ===
    | If (cond, thenExpr, elseExpr) ->
        let s1, condTy = infer env cond
        let s2, thenTy = infer (applyEnv s1 env) thenExpr
        let s3, elseTy = infer (applyEnv (compose s2 s1) env) elseExpr
        // Condition must be bool
        let s4 = unify (apply (compose s3 (compose s2 s1)) condTy) TBool
        // Branches must have same type
        let s5 = unify (apply s4 thenTy) (apply s4 elseTy)
        let finalSubst = compose s5 (compose s4 (compose s3 (compose s2 s1)))
        (finalSubst, apply s5 thenTy)

    // === Let with polymorphism (INFER-07) ===
    | Let (name, value, body) ->
        let s1, valueTy = infer env value
        let env' = applyEnv s1 env
        let scheme = generalize env' (apply s1 valueTy)
        let bodyEnv = Map.add name scheme env'
        let s2, bodyTy = infer bodyEnv body
        (compose s2 s1, bodyTy)

    // === LetRec (INFER-09) ===
    | LetRec (name, param, body, expr) ->
        // Pre-bind function with fresh type for recursive calls
        let funcTy = freshVar()
        let paramTy = freshVar()
        let recEnv = Map.add name (Scheme ([], funcTy)) env
        let bodyEnv = Map.add param (Scheme ([], paramTy)) recEnv
        // Infer body type
        let s1, bodyTy = infer bodyEnv body
        // Unify function type with inferred arrow
        let s2 = unify (apply s1 funcTy) (TArrow (apply s1 paramTy, bodyTy))
        let s = compose s2 s1
        // Generalize and add to env for expression
        let env' = applyEnv s env
        let scheme = generalize env' (apply s funcTy)
        let exprEnv = Map.add name scheme env'
        let s3, exprTy = infer exprEnv expr
        (compose s3 s, exprTy)

    // === Tuple (INFER-11) ===
    | Tuple exprs ->
        let folder (s, tys) e =
            let s', ty = infer (applyEnv s env) e
            (compose s' s, ty :: tys)
        let finalS, revTys = List.fold folder (empty, []) exprs
        (finalS, TTuple (List.rev revTys))

    // === EmptyList (INFER-12) ===
    | EmptyList ->
        let elemTy = freshVar()
        (empty, TList elemTy)

    // === List literal (INFER-12) ===
    | List exprs ->
        match exprs with
        | [] ->
            let elemTy = freshVar()
            (empty, TList elemTy)
        | first :: rest ->
            let s1, elemTy = infer env first
            let folder (s, ty) e =
                let s', eTy = infer (applyEnv s env) e
                let s'' = unify (apply s' ty) eTy
                (compose s'' (compose s' s), apply s'' eTy)
            let finalS, elemTy' = List.fold folder (s1, elemTy) rest
            (finalS, TList elemTy')

    // === Cons (INFER-12) ===
    | Cons (head, tail) ->
        let s1, headTy = infer env head
        let s2, tailTy = infer (applyEnv s1 env) tail
        let s3 = unify tailTy (TList (apply s2 headTy))
        (compose s3 (compose s2 s1), apply s3 tailTy)

    // === Match expression (INFER-13) ===
    | Match (scrutinee, clauses) ->
        let s1, scrutTy = infer env scrutinee
        let resultTy = freshVar()
        let folder s (pat, expr) =
            let patEnv, patTy = inferPattern pat
            // Unify scrutinee with pattern type
            let s' = unify (apply s scrutTy) patTy
            // Merge pattern env with current env (after applying substitution)
            let clauseEnv = Map.fold (fun acc k v -> Map.add k v acc)
                                     (applyEnv s' (applyEnv s env)) patEnv
            // Infer clause body
            let s'', exprTy = infer clauseEnv expr
            // Unify with result type
            let s''' = unify (apply s'' resultTy) exprTy
            compose s''' (compose s'' (compose s' s))
        let finalS = List.fold folder s1 clauses
        (finalS, apply finalS resultTy)

    // === LetPat (INFER-14) ===
    | LetPat (pat, value, body) ->
        // Infer value type
        let s1, valueTy = infer env value
        // Get pattern bindings and type
        let patEnv, patTy = inferPattern pat
        // Unify value type with pattern type
        let s2 = unify (apply s1 valueTy) patTy
        let s = compose s2 s1
        // Apply substitution and generalize each binding
        let env' = applyEnv s env
        let generalizedPatEnv =
            patEnv
            |> Map.map (fun _ (Scheme (_, ty)) ->
                let ty' = apply s ty
                generalize env' ty')
        // Merge into environment
        let bodyEnv = Map.fold (fun acc k v -> Map.add k v acc) env' generalizedPatEnv
        let s3, bodyTy = infer bodyEnv body
        (compose s3 s, bodyTy)

/// Helper: infer binary operator with expected operand and result types
and inferBinaryOp env e1 e2 leftTy rightTy resultTy =
    let s1, t1 = infer env e1
    let s2, t2 = infer (applyEnv s1 env) e2
    let s3 = unify (apply s2 t1) leftTy
    let s4 = unify (apply s3 t2) rightTy
    (compose s4 (compose s3 (compose s2 s1)), resultTy)
