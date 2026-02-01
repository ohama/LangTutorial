# Phase 1: Type Definition - Research

**Researched:** 2026-02-01
**Domain:** Hindley-Milner type system implementation in F#
**Confidence:** HIGH

## Summary

Researched F# discriminated union design patterns, Hindley-Milner type system fundamentals, and type AST representation for implementing Phase 1 of the FunLang type system. The standard approach uses discriminated unions for type AST (TInt, TBool, TString, TVar, TArrow, TTuple, TList), Scheme for polymorphic type quantification (forall variables), and Map-based TypeEnv for variable-to-scheme bindings. Type formatting converts type AST to human-readable strings using standard ML notation ('a -> 'b, int list, etc.).

The existing codebase already follows F# idioms with discriminated unions for expression AST (Ast.fs), so extending this pattern to Type.fs will maintain consistency. The reference implementation in docs/todo.md provides complete F# code examples that align with Microsoft F# conventions and existing project structure.

**Primary recommendation:** Define Type.fs as a standalone module with discriminated unions for Type and Scheme, use Map<string, Scheme> for TypeEnv, and implement formatType with pattern matching. Follow existing project conventions (PascalCase for union cases, explicit type annotations in public signatures).

## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| F# Discriminated Unions | .NET 10.0 | Type AST representation | Idiomatic F# pattern for algebraic data types, exhaustive pattern matching |
| Map<'K, 'V> | .NET 10.0 | TypeEnv and Subst storage | Immutable, persistent data structure, standard F# collection |
| sprintf | .NET 10.0 | Type formatting | String formatting with type safety |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| [<RequireQualifiedAccess>] | .NET 10.0 | Prevent name collisions | Optional for Type module if needed |
| [<Struct>] | .NET 10.0 | Performance optimization | NOT recommended for recursive Type union |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Map<int, Type> | Dictionary<int, Type> | Map is immutable (safer for functional code), Dictionary is mutable (slightly faster) |
| Discriminated Union | Class hierarchy | DU provides exhaustive matching and immutability, classes provide inheritance |
| int for TVar | string for TVar | int simplifies fresh variable generation, string is more debuggable but requires name management |

**Installation:**
```bash
# No external packages needed for Phase 1
# F# core libraries provide all necessary features
```

## Architecture Patterns

### Recommended Project Structure
```
FunLang/
├── Ast.fs              # Existing expression AST
├── Type.fs             # NEW: Type AST, Scheme, TypeEnv, formatType
├── Parser.fsy          # Existing (no changes in Phase 1)
├── Lexer.fsl           # Existing (no changes in Phase 1)
├── Eval.fs             # Existing (no changes in Phase 1)
└── Program.fs          # Existing (no changes in Phase 1)
```

### Pattern 1: Type AST with Discriminated Unions
**What:** Represent types as recursive discriminated union
**When to use:** Type system AST design
**Example:**
```fsharp
// Source: docs/todo.md
type Type =
    | TInt                           // int
    | TBool                          // bool
    | TString                        // string
    | TVar of int                    // 'a, 'b, ... (using int for simplicity)
    | TArrow of Type * Type          // 'a -> 'b
    | TTuple of Type list            // 'a * 'b * ...
    | TList of Type                  // 'a list
```

### Pattern 2: Type Scheme for Polymorphism
**What:** Wrap types with forall quantification
**When to use:** Let-polymorphism in Hindley-Milner
**Example:**
```fsharp
// Source: docs/todo.md
/// Type scheme: forall 'a 'b. 'a -> 'b -> 'a
type Scheme = Scheme of vars: int list * ty: Type

// Example: id function has scheme
// Scheme([0], TArrow(TVar 0, TVar 0))
// Represents: forall 'a. 'a -> 'a
```

### Pattern 3: Environment as Map
**What:** Use Map<string, Scheme> for type environment
**When to use:** Variable name to type scheme lookup
**Example:**
```fsharp
// Source: docs/todo.md
type TypeEnv = Map<string, Scheme>

// Example usage
let env: TypeEnv = Map.ofList [
    ("id", Scheme([0], TArrow(TVar 0, TVar 0)))
]
```

### Pattern 4: Type Formatting with Pattern Matching
**What:** Convert Type AST to human-readable string
**When to use:** Displaying inferred types, error messages
**Example:**
```fsharp
// Source: docs/todo.md
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

### Anti-Patterns to Avoid
- **Don't use string for type variables:** Requires complex name generation and alpha-renaming; int is simpler
- **Don't use mutable references in Type:** Breaks referential transparency, makes substitution complex
- **Don't add [<Struct>] to recursive unions:** Causes compiler errors; Type is recursive via TArrow, TTuple
- **Don't mix data representation and operations:** Keep Type.fs focused on definitions; put substitution in Subst.fs (Phase 2)

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Type variable naming | Custom string generator | int with modulo arithmetic | int simplifies freshness checking; convert to 'a, 'b only for display |
| Type environment | Custom tree structure | Map<string, Scheme> | Map provides immutable updates, O(log n) lookup, standard API |
| Recursive type traversal | Manual recursion with accumulator | Pattern matching with rec keyword | F# pattern matching is exhaustive, compiler-verified |
| String formatting | Manual concatenation | sprintf and String.concat | Type-safe, handles edge cases (parentheses, precedence) |

**Key insight:** F# standard library provides all necessary primitives for Phase 1. Discriminated unions, Map, and pattern matching are sufficient. Don't introduce external dependencies or custom data structures.

## Common Pitfalls

### Pitfall 1: Incorrect Arrow Type Parenthesization
**What goes wrong:** formatType produces "int -> int -> bool" when it should be "(int -> int) -> bool"
**Why it happens:** Arrow is right-associative by default; left operand needs parens if also arrow
**How to avoid:** Pattern match on left operand; wrap in parens if TArrow
**Warning signs:** Type displays look wrong; function types appear ambiguous
**Example:**
```fsharp
// WRONG: No parenthesization
| TArrow (t1, t2) -> sprintf "%s -> %s" (formatType t1) (formatType t2)
// Produces: int -> int -> bool (ambiguous)

// CORRECT: Check if left is arrow
| TArrow (t1, t2) ->
    let left = match t1 with TArrow _ -> sprintf "(%s)" (formatType t1) | _ -> formatType t1
    sprintf "%s -> %s" left (formatType t2)
// Produces: (int -> int) -> bool (clear)
```

### Pitfall 2: Type Variable Display Beyond 26 Letters
**What goes wrong:** Using 'a through 'z works for 0-25, but n=26 produces '{' (char 123)
**Why it happens:** char(97 + n) wraps around ASCII; no handling for n >= 26
**How to avoid:** Use modulo 26 for simple cases; for production, implement multi-letter scheme ('aa, 'ab)
**Warning signs:** Type variables display as non-alphabetic characters
**Example:**
```fsharp
// SIMPLE (Phase 1): Modulo for demonstration
| TVar n -> sprintf "'%c" (char (97 + n % 26))

// PRODUCTION (later): Multi-letter handling
| TVar n when n < 26 -> sprintf "'%c" (char (97 + n))
| TVar n -> sprintf "'%s" (varToString n)  // 'aa, 'ab, ...
```

### Pitfall 3: Scheme vs Type Confusion
**What goes wrong:** Storing Type in TypeEnv instead of Scheme; prevents polymorphism
**Why it happens:** Scheme seems redundant for monomorphic types
**How to avoid:** ALWAYS use Scheme in TypeEnv, even for monomorphic types (empty vars list)
**Warning signs:** Polymorphic functions can't be reused at different types
**Example:**
```fsharp
// WRONG: TypeEnv with Type
type TypeEnv = Map<string, Type>  // Can't represent forall

// CORRECT: TypeEnv with Scheme
type TypeEnv = Map<string, Scheme>
// Monomorphic: Scheme([], TInt)
// Polymorphic: Scheme([0], TArrow(TVar 0, TVar 0))
```

### Pitfall 4: Build Order Dependency
**What goes wrong:** Type.fs placed after Program.fs in .fsproj; compilation fails
**Why it happens:** F# requires dependency order; Program.fs will reference Type.fs (indirectly via later phases)
**How to avoid:** Insert Type.fs after Ast.fs, before Parser.fsy in FunLang.fsproj
**Warning signs:** "Type not defined" errors despite file existing
**Example:**
```xml
<!-- CORRECT placement in FunLang.fsproj -->
<ItemGroup>
    <Compile Include="Ast.fs" />
    <Compile Include="Type.fs" />      <!-- NEW: After Ast.fs -->
    <FsYacc Include="Parser.fsy">
    <!-- ... rest of build order ... -->
</ItemGroup>
```

## Code Examples

Verified patterns from official sources:

### Complete Type Module Structure
```fsharp
// Source: docs/todo.md (reference implementation)
module Type

/// Type representation
type Type =
    | TInt                           // int
    | TBool                          // bool
    | TString                        // string
    | TVar of int                    // Type variable 'a, 'b, ...
    | TArrow of Type * Type          // Function type 'a -> 'b
    | TTuple of Type list            // Tuple type 'a * 'b
    | TList of Type                  // List type 'a list

/// Type scheme (polymorphism)
/// forall 'a 'b. 'a -> 'b -> 'a
type Scheme = Scheme of vars: int list * ty: Type

/// Type environment: variable name -> type scheme
type TypeEnv = Map<string, Scheme>

/// Type substitution: type variable -> type
type Subst = Map<int, Type>

/// Format type as string
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

### Type Variable Naming
```fsharp
// Source: docs/todo.md, bernsteinbear.com/blog/type-inference
// Convert int to 'a, 'b, 'c style
| TVar n -> sprintf "'%c" (char (97 + n % 26))

// Examples:
// TVar 0  -> "'a"
// TVar 1  -> "'b"
// TVar 25 -> "'z"
// TVar 26 -> "'a" (wraps with modulo)
```

### Arrow Type Parenthesization
```fsharp
// Source: docs/todo.md
// Right-associative: int -> int -> bool is int -> (int -> bool)
// Left needs parens: (int -> int) -> bool

| TArrow (t1, t2) ->
    let left = match t1 with
               | TArrow _ -> sprintf "(%s)" (formatType t1)
               | _ -> formatType t1
    sprintf "%s -> %s" left (formatType t2)
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| String-based type vars | int-based type vars | Classic HM papers | Simplifies fresh variable generation, avoids alpha-renaming |
| Mutable refs for unification | Pure substitution | Modern FP practice | Easier to reason about, testable, but requires composition |
| Class hierarchies for types | Discriminated unions | F# idiom (2000s+) | Exhaustive matching, immutability by default |
| Pretty-printing with custom DSL | sprintf with pattern matching | F# standard (2010s+) | Less code, type-safe formatting |

**Deprecated/outdated:**
- Prolog-style unification variables (mutable refs): Modern F# uses pure substitution (Map-based)
- OOP visitor pattern for type traversal: Pattern matching is more idiomatic and type-safe in F#

## Open Questions

Things that couldn't be fully resolved:

1. **Multi-letter type variable names ('aa, 'ab for n >= 26)**
   - What we know: docs/todo.md uses modulo 26 for simplicity
   - What's unclear: Production implementation strategy for large n
   - Recommendation: Use modulo 26 for Phase 1; defer multi-letter scheme to later if needed

2. **Struct vs non-struct discriminated unions for Type**
   - What we know: [<Struct>] improves performance for non-recursive unions
   - What's unclear: Whether performance matters for Type (likely not in Phase 1)
   - Recommendation: Use default (reference type); Type is recursive, so struct is prohibited anyway

3. **Module vs namespace for Type.fs**
   - What we know: Existing files (Ast.fs, Eval.fs) use module declarations
   - What's unclear: No explicit module in Ast.fs; relies on file-level module
   - Recommendation: Add explicit `module Type` for consistency with later files (Subst, Unify, Infer)

## Sources

### Primary (HIGH confidence)
- [docs/todo.md](/home/shoh/vibe-coding/LangTutorial/docs/todo.md) - Complete reference implementation with F# code
- [FunLang/Ast.fs](/home/shoh/vibe-coding/LangTutorial/FunLang/Ast.fs) - Existing project patterns for discriminated unions
- [Microsoft F# Discriminated Unions](https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/discriminated-unions) - Official F# naming conventions

### Secondary (MEDIUM confidence)
- [write-you-an-inference-in-fsharp](https://github.com/7sharp9/write-you-an-inference-in-fsharp) - F# implementation patterns for HM
- [Damas-Hindley-Milner inference two ways](https://bernsteinbear.com/blog/type-inference/) - Algorithm insights and pitfalls
- [kekyo/TypeInferencer](https://github.com/kekyo/TypeInferencer) - Complete F# Algorithm W implementation

### Tertiary (LOW confidence)
- [Hindley-Milner Wikipedia](https://en.wikipedia.org/wiki/Hindley%E2%80%93Milner_type_system) - Background theory
- [F# for fun and profit - Type Inference](https://fsharpforfunandprofit.com/posts/type-inference/) - General F# type inference concepts

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - F# discriminated unions and Map are well-established patterns
- Architecture: HIGH - Reference implementation in docs/todo.md provides complete code
- Pitfalls: HIGH - Verified against existing Ast.fs patterns and official F# conventions

**Research date:** 2026-02-01
**Valid until:** 2026-03-01 (30 days - stable domain, F# language features don't change rapidly)

**Notes:**
- No CONTEXT.md found; no user decisions to constrain research
- Reference implementation in docs/todo.md is authoritative source
- Existing project follows F# idioms; Type.fs should maintain consistency
- Phase 1 has minimal dependencies; future phases (Subst, Unify, Infer) will build on these foundations
