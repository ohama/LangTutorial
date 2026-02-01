module Unify

open Type

/// Exception for type errors during unification
exception TypeError of string

/// Occurs check: does type variable v appear in type t?
/// Used to detect infinite types like 'a = 'a -> int
let occurs (v: int) (t: Type): bool =
    Set.contains v (freeVars t)

/// Robinson's unification algorithm
/// Returns substitution that makes t1 and t2 equal, or raises TypeError
let rec unify (t1: Type) (t2: Type): Subst =
    match t1, t2 with
    // Primitive types: must be identical
    | TInt, TInt -> empty
    | TBool, TBool -> empty
    | TString, TString -> empty

    // Type variable: symmetric pattern handles both TVar,t and t,TVar
    | TVar n, t | t, TVar n ->
        if t = TVar n then empty  // Same variable
        elif occurs n t then
            raise (TypeError (sprintf "Infinite type: %s = %s"
                (formatType (TVar n)) (formatType t)))
        else
            singleton n t

    // Arrow types: unify domains, apply to ranges, unify ranges
    | TArrow (a1, b1), TArrow (a2, b2) ->
        let s1 = unify a1 a2
        let s2 = unify (apply s1 b1) (apply s1 b2)
        compose s2 s1

    // Tuple types: must have same length, fold with substitution threading
    | TTuple ts1, TTuple ts2 when List.length ts1 = List.length ts2 ->
        List.fold2 (fun s t1 t2 ->
            let s' = unify (apply s t1) (apply s t2)
            compose s' s
        ) empty ts1 ts2

    // List types: recursively unify element types
    | TList t1, TList t2 ->
        unify t1 t2

    // Everything else: type mismatch
    | _ ->
        raise (TypeError (sprintf "Cannot unify %s with %s"
            (formatType t1) (formatType t2)))
