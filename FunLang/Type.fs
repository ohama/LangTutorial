module Type

/// Type representation for Hindley-Milner type inference
/// Phase 1: Type definition (v4.0)
type Type =
    | TInt                           // int
    | TBool                          // bool
    | TString                        // string
    | TVar of int                    // type variable 'a, 'b, ... (using int for simplicity)
    | TArrow of Type * Type          // function type 'a -> 'b
    | TTuple of Type list            // tuple type 'a * 'b
    | TList of Type                  // list type 'a list

/// Type scheme for polymorphism
/// forall 'a 'b. 'a -> 'b -> 'a
type Scheme = Scheme of vars: int list * ty: Type

/// Type environment: variable name -> type scheme
type TypeEnv = Map<string, Scheme>

/// Type substitution: type variable -> type
type Subst = Map<int, Type>

/// Format type to string representation
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

// ============================================================================
// Substitution Operations
// ============================================================================

/// Empty substitution
let empty: Subst = Map.empty

/// Create a single variable substitution
let singleton (v: int) (t: Type): Subst = Map.ofList [(v, t)]

/// Apply substitution to type
/// CRITICAL: TVar case recursively applies for transitive chains
/// Example: {0 -> TVar 1, 1 -> TInt} applied to TVar 0 -> TInt
let rec apply (s: Subst) = function
    | TInt -> TInt
    | TBool -> TBool
    | TString -> TString
    | TVar n ->
        match Map.tryFind n s with
        | Some t -> apply s t  // Recursive for transitive substitution
        | None -> TVar n
    | TArrow (t1, t2) -> TArrow (apply s t1, apply s t2)
    | TTuple ts -> TTuple (List.map (apply s) ts)
    | TList t -> TList (apply s t)

/// Compose two substitutions: s2 after s1 (like function composition)
/// Apply s2 to all values in s1, then merge s2 bindings
let compose (s2: Subst) (s1: Subst): Subst =
    let s1' = Map.map (fun _ t -> apply s2 t) s1
    Map.fold (fun acc k v -> Map.add k v acc) s1' s2

/// Apply substitution to scheme
/// CRITICAL: Remove bound vars from substitution before applying
let applyScheme (s: Subst) (Scheme (vars, ty)): Scheme =
    let s' = List.fold (fun acc v -> Map.remove v acc) s vars
    Scheme (vars, apply s' ty)

/// Apply substitution to all schemes in environment
let applyEnv (s: Subst) (env: TypeEnv): TypeEnv =
    Map.map (fun _ scheme -> applyScheme s scheme) env

// ============================================================================
// Free Variable Operations
// ============================================================================

/// Collect free type variables in a type
let rec freeVars = function
    | TInt | TBool | TString -> Set.empty
    | TVar n -> Set.singleton n
    | TArrow (t1, t2) -> Set.union (freeVars t1) (freeVars t2)
    | TTuple ts -> ts |> List.map freeVars |> Set.unionMany
    | TList t -> freeVars t

/// Free variables in a type scheme (excludes bound variables)
let freeVarsScheme (Scheme (vars, ty)) =
    Set.difference (freeVars ty) (Set.ofList vars)

/// Free variables in entire type environment
let freeVarsEnv (env: TypeEnv) =
    env |> Map.values |> Seq.map freeVarsScheme |> Set.unionMany
