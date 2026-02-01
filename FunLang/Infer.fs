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
