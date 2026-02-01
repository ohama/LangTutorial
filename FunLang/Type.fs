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
