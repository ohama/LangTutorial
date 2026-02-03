# Bidirectional Type Checking for FunLang

**Research Date:** 2026-02-03
**Domain:** Type System Implementation
**Current State:** Hindley-Milner (Algorithm W) type inference
**Target:** Bidirectional type checking with ML-style annotations

---

## Executive Summary

Bidirectional type checking is a principled approach that splits type inference into two complementary modes: **synthesis** (determining a type from an expression) and **checking** (verifying an expression against a known type). This approach offers several advantages over Algorithm W:

1. **Better error locality** - errors are detected closer to their source
2. **Scalability** - remains decidable for type systems where full inference is undecidable
3. **Simpler implementation** - cleaner separation of concerns
4. **Natural annotation support** - type annotations fit organically into the system

For FunLang, adopting bidirectional typing will enable:
- Explicit type annotations: `(x: int) -> x + 1`
- Return type annotations: `let f (x: int) : int = x`
- Better error messages through mode-aware type checking
- Foundation for future extensions (higher-rank types, GADTs)

---

## 1. Bidirectional Typing Fundamentals

### 1.1 Two Modes of Typing

The traditional typing judgment `G |- e : A` ("expression e has type A in context G") splits into two:

| Mode | Judgment | Meaning | Direction |
|------|----------|---------|-----------|
| **Synthesis** | `G |- e => A` | Infer type A from expression e | Information flows UP from expression |
| **Checking** | `G |- e <= A` | Verify expression e has type A | Information flows DOWN to expression |

**Key insight:** Some expressions naturally provide their type (synthesize), while others need type information from context (check).

### 1.2 Which Expressions Synthesize vs Check

| Expression | Mode | Rationale |
|------------|------|-----------|
| Variables | Synthesize | Type known from context |
| Literals (int, bool, string) | Synthesize | Type inherent in syntax |
| Annotated expressions | Synthesize | Annotation provides type |
| Function application | Synthesize | Result type from function type |
| If-then-else | Check (or both) | Branches must match expected type |
| Lambda (unannotated) | **Check only** | Cannot determine parameter type |
| Lambda (annotated) | Synthesize | Parameter type from annotation |
| Let expression | Depends | Can do either |

### 1.3 Core Typing Rules

**Variable (Synthesis):**
```
x : A in G
-----------
G |- x => A
```

**Lambda Checking (when no annotation):**
```
G, x:A |- e <= B
------------------------
G |- (fun x -> e) <= A -> B
```

**Lambda Synthesis (with annotation):**
```
G, x:A |- e => B
--------------------------------
G |- (fun (x:A) -> e) => A -> B
```

**Application (Synthesis):**
```
G |- f => A -> B    G |- arg <= A
---------------------------------
G |- f arg => B
```

**Annotation (Mode Switch):**
```
G |- e <= A
-------------------
G |- (e : A) => A
```

**Subsumption (Mode Switch):**
```
G |- e => A    A <: B
---------------------
G |- e <= B
```

### 1.4 The Subsumption Rule

The subsumption rule enables switching from synthesis to checking mode. In its simplest form (without subtyping):

```
G |- e => A    A = B
--------------------
G |- e <= B
```

This allows any synthesizing expression to be used where checking is expected, as long as the types match.

---

## 2. Key Academic Resources

### 2.1 Essential Papers

| Paper | Authors | Key Contribution |
|-------|---------|------------------|
| [Complete and Easy Bidirectional Typechecking for Higher-Rank Polymorphism](https://arxiv.org/abs/1306.6032) | Dunfield & Krishnaswami (2013) | Foundational algorithm for bidirectional typing with polymorphism |
| [Bidirectional Typing](https://dl.acm.org/doi/10.1145/3450952) | Dunfield & Krishnaswami (2022) | Comprehensive survey of bidirectional typing |
| [Local Type Inference](https://www.cis.upenn.edu/~bcpierce/papers/lti-toplas.pdf) | Pierce & Turner (2000) | Original work on local type inference |

### 2.2 Tutorials

| Resource | Notes |
|----------|-------|
| [David Christiansen's Tutorial](https://davidchristiansen.dk/tutorials/bidirectional.pdf) | Accessible introduction to bidirectional typing rules |
| [Simple Bidirectional Type Inference (ettolrach)](https://ettolrach.com/blog/bidirectional_inference.html) | Practical implementation tutorial (Rust-based) |
| [Haskell for All: Appeal of Bidirectional Type-Checking](https://www.haskellforall.com/2022/06/the-appeal-of-bidirectional-type.html) | Motivation and benefits |

### 2.3 Reference Implementations

| Implementation | Language | Notes |
|----------------|----------|-------|
| [ollef/Bidirectional](https://github.com/ollef/Bidirectional) | Haskell | Direct implementation of Dunfield-Krishnaswami |
| [samuela/bidirectional-typing](https://github.com/samuela/bidirectional-typing) | Haskell | Uses unordered mapping approach |

---

## 3. Implementation Patterns for F#

### 3.1 Core Type Definitions

Extend existing types to support annotations:

```fsharp
// Extend Expr AST
type Expr =
    // ... existing cases ...
    | Annot of Expr * TypeExpr * Span      // (e : T)
    | LambdaAnnot of param: string * paramTy: TypeExpr * body: Expr * Span

// Type expressions (what users write)
type TypeExpr =
    | TEInt
    | TEBool
    | TEString
    | TEArrow of TypeExpr * TypeExpr
    | TETuple of TypeExpr list
    | TEList of TypeExpr
    | TEVar of string                       // 'a, 'b (user-written type variables)
```

### 3.2 Bidirectional Checker Structure

```fsharp
module Bidir

open Type
open Ast

/// Synthesis mode: expression -> type
/// Returns (substitution, inferred type)
let rec synth (ctx: InferContext list) (env: TypeEnv) (expr: Expr): Subst * Type =
    match expr with
    // Literals synthesize their types
    | Number (_, _) -> (empty, TInt)
    | Bool (_, _) -> (empty, TBool)
    | String (_, _) -> (empty, TString)

    // Variables synthesize from environment
    | Var (name, span) ->
        match Map.tryFind name env with
        | Some scheme -> (empty, instantiate scheme)
        | None -> raise (TypeException { Kind = UnboundVar name; ... })

    // Annotated expressions synthesize
    | Annot (e, tyExpr, span) ->
        let ty = elaborateTypeExpr tyExpr
        let s = check ctx env e ty
        (s, ty)

    // Annotated lambda synthesizes
    | LambdaAnnot (param, paramTyExpr, body, span) ->
        let paramTy = elaborateTypeExpr paramTyExpr
        let bodyEnv = Map.add param (Scheme ([], paramTy)) env
        let s, bodyTy = synth ctx bodyEnv body
        (s, TArrow (apply s paramTy, bodyTy))

    // Application synthesizes
    | App (func, arg, span) ->
        let s1, funcTy = synth ctx env func
        match apply s1 funcTy with
        | TArrow (paramTy, resultTy) ->
            let s2 = check ctx (applyEnv s1 env) arg paramTy
            (compose s2 s1, apply s2 resultTy)
        | TVar _ as ty ->
            // Unknown function type - introduce fresh variables
            let argTy = freshVar()
            let resultTy = freshVar()
            let s2 = unify ty (TArrow (argTy, resultTy))
            let s3 = check ctx (applyEnv (compose s2 s1) env) arg (apply s2 argTy)
            (compose s3 (compose s2 s1), apply s3 resultTy)
        | ty ->
            raise (TypeException { Kind = NotAFunction ty; ... })

    // Unannotated lambda cannot synthesize alone
    | Lambda (param, body, span) ->
        // Option 1: Error - require annotation
        // Option 2: Infer with fresh type variable (like Algorithm W)
        let paramTy = freshVar()
        let bodyEnv = Map.add param (Scheme ([], paramTy)) env
        let s, bodyTy = synth ctx bodyEnv body
        (s, TArrow (apply s paramTy, bodyTy))

    // ... other cases

/// Checking mode: expression -> expected type -> substitution
/// Returns substitution that makes expression have the expected type
and check (ctx: InferContext list) (env: TypeEnv) (expr: Expr) (expected: Type): Subst =
    match expr, expected with
    // Lambda checks against arrow type
    | Lambda (param, body, _), TArrow (paramTy, resultTy) ->
        let bodyEnv = Map.add param (Scheme ([], paramTy)) env
        check ctx bodyEnv body resultTy

    // If-then-else checks both branches
    | If (cond, thenE, elseE, _), expected ->
        let s1 = check ctx env cond TBool
        let s2 = check ctx (applyEnv s1 env) thenE (apply s1 expected)
        let s3 = check ctx (applyEnv (compose s2 s1) env) elseE (apply (compose s2 s1) expected)
        compose s3 (compose s2 s1)

    // Match expression - all clauses check against expected
    | Match (scrutinee, clauses, _), expected ->
        // ... check each clause produces expected type

    // Subsumption: synthesize then unify
    | expr, expected ->
        let s1, actual = synth ctx env expr
        let s2 = unify (apply s1 expected) actual
        compose s2 s1
```

### 3.3 Integration with Unification

Bidirectional typing can coexist with unification. The key insight:

1. **Synthesis** may produce types with unification variables (like `TVar n`)
2. **Checking** uses unification to match synthesized type with expected type
3. **Subsumption** bridges the two modes via unification

```fsharp
/// Mode switch: use synthesis result where checking is expected
and subsume (ctx: InferContext list) (env: TypeEnv) (expr: Expr) (expected: Type): Subst =
    let s1, actual = synth ctx env expr
    let s2 = unifyWithContext ctx [] (spanOf expr) (apply s1 actual) (apply s1 expected)
    compose s2 s1
```

### 3.4 Let-Polymorphism in Bidirectional Setting

The original Dunfield-Krishnaswami paper notes that let-generalization can be simplified:

**Option 1: No implicit generalization (simpler)**
```fsharp
| Let (name, value, body, _) ->
    let s1, valueTy = synth ctx env value
    let bodyEnv = Map.add name (Scheme ([], valueTy)) (applyEnv s1 env)
    let s2, bodyTy = synth ctx bodyEnv body
    (compose s2 s1, bodyTy)
```

**Option 2: Generalize at let (HM-style, current FunLang approach)**
```fsharp
| Let (name, value, body, _) ->
    let s1, valueTy = synth ctx env value
    let env' = applyEnv s1 env
    let scheme = generalize env' (apply s1 valueTy)
    let bodyEnv = Map.add name scheme env'
    let s2, bodyTy = synth ctx bodyEnv body
    (compose s2 s1, bodyTy)
```

**Recommendation for FunLang:** Keep HM-style let-polymorphism for backward compatibility. The bidirectional structure is orthogonal to let-generalization.

---

## 4. ML-Style Annotation Syntax

### 4.1 Grammar Extensions

**Lexer additions (`Lexer.fsl`):**
```
// Type annotation colon (distinct from EQUALS)
| ':'           { COLON }

// Type keywords
| "int"         { TYPE_INT }
| "bool"        { TYPE_BOOL }
| "string"      { TYPE_STRING }
| "list"        { TYPE_LIST }
```

**Parser additions (`Parser.fsy`):**
```yacc
// Token declarations
%token COLON
%token TYPE_INT TYPE_BOOL TYPE_STRING TYPE_LIST
%token <string> TYPE_VAR    // 'a, 'b

// Type expressions
TypeExpr:
    | TYPE_INT                           { TEInt }
    | TYPE_BOOL                          { TEBool }
    | TYPE_STRING                        { TEString }
    | TypeExpr ARROW TypeExpr            { TEArrow($1, $3) }
    | LPAREN TypeExprList RPAREN         { TETuple($2) }
    | TypeExpr TYPE_LIST                 { TEList($1) }
    | TYPE_VAR                           { TEVar($1) }
    | LPAREN TypeExpr RPAREN             { $2 }

TypeExprList:
    | TypeExpr STAR TypeExpr             { [$1; $3] }
    | TypeExpr STAR TypeExprList         { $1 :: $3 }

// Annotated expressions
Atom:
    | LPAREN Expr COLON TypeExpr RPAREN  { Annot($2, $4, ruleSpan parseState 1 5) }
    // ... existing cases

// Annotated lambda parameters
Expr:
    // fun (x: int) -> body
    | FUN LPAREN IDENT COLON TypeExpr RPAREN ARROW Expr
        { LambdaAnnot($3, $5, $8, ruleSpan parseState 1 8) }

    // let f (x: int) : int = body in expr
    | LET IDENT LPAREN IDENT COLON TypeExpr RPAREN COLON TypeExpr EQUALS Expr IN Expr
        { LetAnnot($2, $4, $6, $9, $11, $13, ruleSpan parseState 1 13) }

    // ... existing cases
```

### 4.2 Syntax Examples

```ml
(* Parameter type annotation *)
fun (x: int) -> x + 1

(* Return type annotation *)
let f (x: int) : int = x * 2 in f 5

(* Expression annotation *)
let x = (42 : int) in x

(* Polymorphic annotation - future *)
let id (x: 'a) : 'a = x in id 5
```

### 4.3 Type Expression Elaboration

Convert user-written `TypeExpr` to internal `Type`:

```fsharp
/// Convert surface type syntax to internal Type
let rec elaborateTypeExpr (te: TypeExpr): Type =
    match te with
    | TEInt -> TInt
    | TEBool -> TBool
    | TEString -> TString
    | TEArrow (t1, t2) -> TArrow (elaborateTypeExpr t1, elaborateTypeExpr t2)
    | TETuple ts -> TTuple (List.map elaborateTypeExpr ts)
    | TEList t -> TList (elaborateTypeExpr t)
    | TEVar name ->
        // For now: map named type vars to internal indices
        // Future: proper scoping of user type variables
        let idx = int name.[1] - int 'a'  // 'a -> 0, 'b -> 1, etc.
        TVar idx
```

---

## 5. Migration Strategy

### 5.1 Phase 1: Parser Extensions (Non-Breaking)

1. Add `COLON` token to lexer
2. Add `TypeExpr` non-terminal to parser
3. Add `Annot` AST node
4. All existing tests continue to pass

**Test strategy:** Verify that unannotated code works identically.

### 5.2 Phase 2: Parallel Type Checker

1. Create new `Bidir.fs` module alongside existing `Infer.fs`
2. Implement `synth` and `check` functions
3. For unannotated expressions, behavior matches Algorithm W
4. Run both checkers in tests, compare results

**Test strategy:** Golden tests comparing `Infer.infer` vs `Bidir.synth` on existing code.

### 5.3 Phase 3: Annotation Support

1. Extend `synth` to handle `Annot` and annotated lambdas
2. Checking mode uses unification with expected type
3. Add tests for annotated expressions

**Test strategy:** New test files for annotated syntax.

### 5.4 Phase 4: Error Message Improvement

1. Mode-aware error messages ("expected type X based on annotation")
2. Better blame assignment using bidirectional context
3. Integration with existing `Diagnostic` infrastructure

**Test strategy:** Golden tests for error messages.

### 5.5 Phase 5: Switchover

1. Replace `Infer.infer` calls with `Bidir.synth`
2. Deprecate old module
3. All tests pass

### 5.6 What Breaks, What Stays

| Aspect | Status | Notes |
|--------|--------|-------|
| Unannotated code | SAME | Bidirectional with fresh vars = Algorithm W |
| Let polymorphism | SAME | Keep generalization at let |
| Error messages | IMPROVED | Mode context provides better blame |
| Inferred types | SAME | Same principal types |
| Parser | EXTENDED | New syntax is additive |
| Existing tests | PASS | Behavior identical for unannotated code |

---

## 6. Potential Pitfalls

### 6.1 Lambda Without Context

**Problem:** Unannotated lambdas cannot synthesize a type without context.

**In Algorithm W:** `fun x -> x` gets type `'a -> 'a` via fresh type variable.

**In pure bidirectional:** Would require annotation or checking context.

**Solution for FunLang:** Allow unannotated lambdas to synthesize with fresh type variables (hybrid approach). This preserves backward compatibility.

```fsharp
| Lambda (param, body, _) ->
    // Hybrid: synthesize with fresh variable (like Algorithm W)
    let paramTy = freshVar()
    let bodyEnv = Map.add param (Scheme ([], paramTy)) env
    let s, bodyTy = synth ctx bodyEnv body
    (s, TArrow (apply s paramTy, bodyTy))
```

### 6.2 Annotation Consistency

**Problem:** User annotations must be validated, not just trusted.

**Solution:** When checking against annotation, verify the expression can have that type:

```fsharp
| Annot (e, tyExpr, span) ->
    let annotTy = elaborateTypeExpr tyExpr
    let s = check ctx env e annotTy
    // Return the annotation type, not synthesized type
    (s, annotTy)
```

### 6.3 Type Variable Scoping

**Problem:** User-written `'a` in annotations needs consistent meaning within a binding.

**Example issue:**
```ml
(* Both 'a should refer to same type *)
let f (x: 'a) : 'a = x
```

**Solution:** Track type variable scope in elaboration:

```fsharp
/// Elaborate with type variable environment
let rec elaborateWithVars (vars: Map<string, int>) (te: TypeExpr): Type * Map<string, int> =
    match te with
    | TEVar name ->
        match Map.tryFind name vars with
        | Some idx -> (TVar idx, vars)
        | None ->
            let idx = freshVarIndex()
            (TVar idx, Map.add name idx vars)
    // ... other cases thread vars through
```

### 6.4 Recursive Definitions

**Problem:** `let rec` needs the function's type before checking the body.

**Current approach (keep):**
```fsharp
| LetRec (name, param, body, expr, _) ->
    let funcTy = freshVar()
    let paramTy = freshVar()
    // ... bind and infer body, unify with funcTy
```

**With annotations (new):**
```fsharp
| LetRecAnnot (name, param, paramTy, returnTy, body, expr, _) ->
    let funcTy = TArrow (elaborateTypeExpr paramTy, elaborateTypeExpr returnTy)
    // Check body against returnTy instead of inferring
```

### 6.5 Principal Types

**Problem:** Bidirectional typing might not infer principal types in all cases.

**Reality for FunLang:** With the hybrid approach (fresh vars for unannotated lambdas), principal type inference is preserved for unannotated code. Annotations may constrain types to be less general than principal.

---

## 7. Test Strategy

### 7.1 Regression Tests

All existing tests should pass unchanged. The bidirectional checker must produce identical results for unannotated code.

```fsharp
[<Test>]
let ``bidirectional matches algorithm W for unannotated code`` () =
    let expr = parse "fun x -> x + 1"
    let oldResult = Infer.infer initialTypeEnv expr
    let newResult = Bidir.synth [] initialTypeEnv expr
    Expect.equal (snd oldResult) (snd newResult) "Types should match"
```

### 7.2 Annotation Tests

New tests for annotation syntax and checking:

```fsharp
[<Test>]
let ``annotated lambda synthesizes correctly`` () =
    let expr = parse "fun (x: int) -> x + 1"
    let _, ty = Bidir.synth [] initialTypeEnv expr
    Expect.equal ty (TArrow (TInt, TInt)) "Should be int -> int"

[<Test>]
let ``annotation checking rejects wrong type`` () =
    let expr = parse "(true : int)"
    Expect.throws (fun () -> Bidir.synth [] initialTypeEnv expr |> ignore)
```

### 7.3 Error Message Tests

Golden tests for improved error messages:

```
// input: (fun (x: int) -> x) true

// expected error:
error[E0301]: Type mismatch: expected int but got bool
 --> test.fun:1:22-25
   = in argument to function expecting int
   = hint: The function parameter is annotated as int
```

### 7.4 fslit Integration

Add new fslit test files:

```markdown
# Bidirectional Type Checking

## Annotated Lambda

    >>> fun (x: int) -> x + 1
    - : int -> int

## Type Annotation

    >>> (42 : int)
    42

## Wrong Annotation

    >>> (true : int)
    !!! Type mismatch: expected int but got bool
```

---

## 8. Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Core bidirectional rules | HIGH | Well-established theory, many implementations |
| Integration with unification | HIGH | Known hybrid approach, used in practice |
| Let-polymorphism compatibility | HIGH | Orthogonal concerns |
| Parser grammar | MEDIUM | Standard ML syntax, may need iteration |
| Error message improvement | MEDIUM | Benefits are clear, specifics need design |
| Type variable scoping | MEDIUM | Needs careful implementation |

---

## 9. Recommended Roadmap

### Phase Structure for v6.0 Milestone

**Phase 1: Parser Extensions** (2-3 tasks)
- Add COLON token and type keywords to lexer
- Add TypeExpr grammar to parser
- Add Annot and LambdaAnnot to AST
- Tests: Parser-level tests only

**Phase 2: Type Expression Elaboration** (2 tasks)
- Implement TypeExpr -> Type conversion
- Handle type variable scoping
- Tests: Unit tests for elaboration

**Phase 3: Bidirectional Core** (3-4 tasks)
- Implement `synth` function (synthesis mode)
- Implement `check` function (checking mode)
- Subsumption via unification
- Tests: Compare with Algorithm W on existing code

**Phase 4: Annotation Checking** (2-3 tasks)
- Handle Annot expressions
- Handle LambdaAnnot expressions
- Validate annotations match synthesized types
- Tests: Annotated expression tests

**Phase 5: Error Integration** (2 tasks)
- Mode-aware error context
- Enhanced diagnostic messages
- Tests: Error message golden tests

**Phase 6: Migration** (1-2 tasks)
- Switch from Infer to Bidir module
- Verify all existing tests pass
- Documentation updates

---

## 10. References

### Papers
- Dunfield, J., & Krishnaswami, N. (2013). [Complete and Easy Bidirectional Typechecking for Higher-Rank Polymorphism](https://arxiv.org/abs/1306.6032). ICFP.
- Dunfield, J., & Krishnaswami, N. (2022). [Bidirectional Typing](https://dl.acm.org/doi/10.1145/3450952). ACM Computing Surveys.
- Pierce, B., & Turner, D. (2000). [Local Type Inference](https://www.cis.upenn.edu/~bcpierce/papers/lti-toplas.pdf). TOPLAS.

### Tutorials
- Christiansen, D. [Bidirectional Typing Rules: A Tutorial](https://davidchristiansen.dk/tutorials/bidirectional.pdf)
- [Simple Bidirectional Type Inference](https://ettolrach.com/blog/bidirectional_inference.html) (ettolrach, 2025)
- [The Appeal of Bidirectional Type-Checking](https://www.haskellforall.com/2022/06/the-appeal-of-bidirectional-type.html) (Haskell for All)

### Implementations
- [ollef/Bidirectional](https://github.com/ollef/Bidirectional) - Haskell reference implementation
- [samuela/bidirectional-typing](https://github.com/samuela/bidirectional-typing) - Alternative Haskell implementation
- [nLab: Bidirectional Typechecking](https://ncatlab.org/nlab/show/bidirectional+typechecking) - Formal rules

### ML/F# Resources
- [SML Grammar](https://people.mpi-sws.org/~rossberg/sml.html) - Standard ML type annotation syntax
- [F# Type Annotations](https://fsharpforfunandprofit.com/posts/how-types-work-with-functions/) - F# annotation patterns
- [FsLexYacc Documentation](https://fsprojects.github.io/FsLexYacc/) - Parser generator for F#
