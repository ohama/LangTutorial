# Phase 4: Annotation Checking - Research

**Researched:** 2026-02-04
**Domain:** Type annotation validation in bidirectional type system
**Confidence:** HIGH

## Summary

Phase 4 validates type annotations in FunLang's bidirectional type system. The infrastructure already exists in Bidir.fs with `Annot` and `LambdaAnnot` cases implemented. The focus is on testing existing functionality and adding error cases for invalid annotations.

**Key findings:**
- Bidir.fs already handles Annot (line 80-83) and LambdaAnnot (line 72-77) correctly
- Annot uses check mode to validate expression against annotation, then synthesizes the annotation type
- LambdaAnnot synthesizes arrow type with elaborated parameter type
- elaborateTypeExpr (Elaborate.fs) converts surface syntax (TypeExpr) to internal types (Type)
- unifyWithContext produces TypeException with rich diagnostic context

**Primary recommendation:** Phase 4 is primarily a testing and error validation phase. The core implementation is complete. Focus on comprehensive test coverage for valid annotations, invalid annotations, and error message quality.

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| F# | 10.0 | Host language | Project baseline |
| fslex/fsyacc | (via FsLexYacc) | Parser infrastructure | Already integrated |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Expecto | Latest | Unit testing | All internal logic tests |
| fslit | Latest | CLI integration tests | End-to-end annotation syntax tests |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Expecto | xUnit/NUnit | Expecto already integrated, F#-idiomatic |
| fslit | Custom test harness | fslit provides CLI golden testing |

**Installation:**
Testing infrastructure already in place. No new dependencies needed.

## Architecture Patterns

### Recommended Project Structure
```
FunLang/
├── Bidir.fs              # Contains synth/check functions (ALREADY IMPLEMENTED)
├── Elaborate.fs          # TypeExpr -> Type conversion (ALREADY IMPLEMENTED)
├── Unify.fs             # Type unification with error context
├── Diagnostic.fs        # Error representation
└── Ast.fs               # Annot, LambdaAnnot variants

tests/
├── type-inference/      # Valid annotation tests (NEW)
├── type-errors/         # Invalid annotation tests (NEW)
└── FunLang.Tests/       # Unit tests for annotation checking
```

### Pattern 1: Annotation Validation (Check then Synthesize)
**What:** Annot expression validates expression against annotation, then synthesizes annotation type
**When to use:** `(expr : T)` annotation expressions
**Example:**
```fsharp
// Source: Bidir.fs:80-83
| Annot (e, tyExpr, span) ->
    let expectedTy = elaborateTypeExpr tyExpr
    let s = check ctx env e expectedTy
    (s, apply s expectedTy)
```

**Key insight:** The annotation type is trusted AFTER validation. `check` ensures expression can have the annotation type via unification. If check succeeds, synthesize returns the annotation type (not the expression's inferred type).

### Pattern 2: Annotated Lambda Synthesis
**What:** LambdaAnnot uses parameter type from annotation, synthesizes arrow type
**When to use:** `fun (x: T) -> body` annotated lambdas
**Example:**
```fsharp
// Source: Bidir.fs:72-77
| LambdaAnnot (param, paramTyExpr, body, _) ->
    let paramTy = elaborateTypeExpr paramTyExpr
    let bodyEnv = Map.add param (Scheme ([], paramTy)) env
    let s, bodyTy = synth ctx bodyEnv body
    (s, TArrow (apply s paramTy, bodyTy))
```

**Key insight:** Parameter type is elaborated from annotation. Body is synthesized (not checked) with parameter bound to annotation type. This allows body to determine return type naturally.

### Pattern 3: Type Elaboration with Scoping
**What:** Convert user-written TypeExpr to internal Type with consistent type variable scoping
**When to use:** All annotation processing
**Example:**
```fsharp
// Source: Elaborate.fs:57-59
let elaborateTypeExpr (te: TypeExpr): Type =
    let (ty, _) = elaborateWithVars Map.empty te
    ty
```

**Scoped elaboration for multiple annotations:**
```fsharp
// Source: Elaborate.fs:63-69
let elaborateScoped (tes: TypeExpr list): Type list =
    let folder (acc, env) te =
        let (ty, env') = elaborateWithVars env te
        (ty :: acc, env')
    let (revTypes, _) = List.fold folder ([], Map.empty) tes
    List.rev revTypes
```

**Key insight:** Type variable names ('a, 'b) must map consistently within a single scope. `elaborateWithVars` threads type variable environment to ensure both occurrences of 'a in `fun (x: 'a) (y: 'a) -> ...` refer to the same TVar index.

### Anti-Patterns to Avoid
- **Trust annotations without validation:** Always use `check` mode to verify expression can have annotation type before returning it
- **Ignore type variable scoping:** 'a in two positions must map to same TVar if in same scope
- **Mix synthesis and checking inconsistently:** Annot uses check internally but synthesizes externally; LambdaAnnot synthesizes throughout

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Type annotation syntax parsing | Custom parser rules | Existing fsyacc grammar | Already implemented (Phase 1) |
| TypeExpr to Type conversion | Ad-hoc conversion | Elaborate.elaborateTypeExpr | Handles scoping correctly |
| Type error reporting | String concatenation | Diagnostic.TypeException | Rich context with spans, traces |
| Test golden files | Custom diff logic | fslit | CLI integration testing standard |
| Type unification | Custom occurs check | Unify.unifyWithContext | Error context tracking built-in |

**Key insight:** Phase 4 benefits from complete infrastructure. Parser handles annotation syntax (Phase 1), elaboration handles type conversion (Phase 2), and bidir handles mode logic (Phase 3). Focus on testing, not reimplementation.

## Common Pitfalls

### Pitfall 1: Annotation Type vs Inferred Type Confusion
**What goes wrong:** Returning inferred type instead of annotation type from Annot case
**Why it happens:** Natural to synthesize expression's type, but annotation constrains it
**How to avoid:** Always return `apply s expectedTy` from Annot case, not inferred type
**Warning signs:** Test `(42 : int) + (true : bool)` doesn't catch type error
**Code example:**
```fsharp
// WRONG: Returns inferred type
| Annot (e, tyExpr, span) ->
    let annotTy = elaborateTypeExpr tyExpr
    let s, inferredTy = synth ctx env e  // ❌ synthesis
    let s' = unifyWithContext ctx [] span annotTy inferredTy
    (compose s' s, inferredTy)  // ❌ returns inferred, not annotation

// CORRECT: Returns annotation type
| Annot (e, tyExpr, span) ->
    let expectedTy = elaborateTypeExpr tyExpr
    let s = check ctx env e expectedTy  // ✓ checking mode
    (s, apply s expectedTy)  // ✓ returns annotation
```

### Pitfall 2: Forgetting Subsumption Fallback
**What goes wrong:** Annotations fail on valid expressions when check doesn't handle them
**Why it happens:** Not all expressions have explicit check cases (e.g., let, match)
**How to avoid:** Bidir.check already has subsumption fallback (line 243-246) - ensure it stays
**Warning signs:** `(let x = 1 in x : int)` fails to type check
**Code verification:**
```fsharp
// Source: Bidir.fs:243-246
| _ ->
    let s, actual = synth ctx env expr
    let s' = unifyWithContext ctx [] (spanOf expr) (apply s expected) actual
    compose s' s
```

### Pitfall 3: Invalid Annotation Error Messages
**What goes wrong:** Generic "type mismatch" without mentioning the annotation
**Why it happens:** unifyWithContext produces UnifyMismatch without annotation context
**How to avoid:** Phase 5 will add annotation-aware error messages; Phase 4 accepts current errors
**Warning signs:** Error for `(true : int)` doesn't mention user provided annotation
**Current behavior (acceptable for Phase 4):**
```
error[E0301]: Type mismatch: expected int but got bool
 --> <expr>:1:1-5
```
**Future behavior (Phase 5):**
```
error[E0301]: Type mismatch: expected int but got bool
 --> <expr>:1:1-5
   = note: Annotation requires type int
   = hint: Expression has type bool, cannot satisfy annotation
```

### Pitfall 4: Type Variable Scoping Bugs
**What goes wrong:** `fun (x: 'a) (y: 'a) -> ...` treats 'a as two different type variables
**Why it happens:** Using elaborateTypeExpr separately for each annotation (creates new scope each time)
**How to avoid:** Use elaborateScoped for curried parameter annotations
**Warning signs:** Function accepts different types for same type variable name
**Current status:** Single-parameter LambdaAnnot works correctly. Multi-parameter annotations not yet implemented (future work).

## Code Examples

Verified patterns from existing implementation:

### Valid Annotation: Expression Type Matches
```fsharp
// Test case: (42 : int)
// Source: Bidir.fs:80-83
let expr = Annot (Number (42, span), TEInt, span)
let s, ty = synth [] initialEnv expr
// Result: (empty, TInt)
// The check verifies Number 42 has type TInt, returns TInt
```

### Valid Annotation: Subsumption Applied
```fsharp
// Test case: (let x = 5 in x : int)
// Let synthesizes int -> check verifies against int -> succeeds
let expr = Annot (Let ("x", Number (5, span), Var ("x", span), span), TEInt, span)
let s, ty = synth [] initialEnv expr
// Result: (empty, TInt)
// Check calls synth on Let (gets TInt), unifies with annotation (TInt), succeeds
```

### Invalid Annotation: Type Mismatch
```fsharp
// Test case: (true : int)
// Source: TypeException raised by unifyWithContext
let expr = Annot (Bool (true, span), TEInt, span)
// Raises: TypeException { Kind = UnifyMismatch (TInt, TBool); ... }
// check verifies Bool true against TInt -> unification fails -> error
```

### Annotated Lambda: Synthesis
```fsharp
// Test case: fun (x: int) -> x + 1
// Source: Bidir.fs:72-77
let expr = LambdaAnnot ("x", TEInt, Add (Var ("x", span), Number (1, span), span), span)
let s, ty = synth [] initialEnv expr
// Result: (empty, TArrow (TInt, TInt))
// Parameter type TInt from annotation, body synthesized as TInt
```

### Type Variable Elaboration: Scoping
```fsharp
// Type expression: 'a -> 'a
// Source: Elaborate.fs:44-53
let te = TEArrow (TEVar "'a", TEVar "'a")
let ty = elaborateTypeExpr te
// Result: TArrow (TVar 0, TVar 0)
// Both 'a map to same TVar index 0 within scope
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Algorithm W only | Bidirectional with annotations | Phase 3 (v6.0) | Enables explicit type documentation |
| No annotation syntax | ML-style annotations | Phase 1 (v6.0) | User can write `(e : T)`, `fun (x: T) -> e` |
| Single type inference mode | Synthesis + checking modes | Phase 3 (v6.0) | Better error locality, annotation validation |
| Trust annotations | Check then synthesize | Phase 3 (v6.0) | Annotations are validated, not assumed |

**Deprecated/outdated:**
- Pure synthesis without checking: Bidirectional requires both modes for annotations
- Implicit-only parameter types: Annotations make parameter types explicit

## Open Questions

Things that couldn't be fully resolved:

1. **Curried annotated lambdas with polymorphic types**
   - What we know: Single-parameter LambdaAnnot works; elaborateScoped exists for shared scope
   - What's unclear: Grammar for `fun (x: 'a) (y: 'a) -> e` not yet implemented
   - Recommendation: Defer to future phase; Phase 4 covers single-parameter annotations

2. **Annotation-aware error messages**
   - What we know: Current errors show type mismatch generically
   - What's unclear: Should Phase 4 add annotation context to errors?
   - Recommendation: No. Phase 5 (Error Integration) will handle this. Phase 4 tests current behavior.

3. **Return type annotations**
   - What we know: Syntax not yet in grammar (no `let f (x: int) : int = e`)
   - What's unclear: Priority for Phase 4?
   - Recommendation: Defer. Phase 4 focuses on Annot and LambdaAnnot expressions only.

## Sources

### Primary (HIGH confidence)
- `/home/shoh/vibe-coding/LangTutorial/FunLang/Bidir.fs` - Annotation cases already implemented
- `/home/shoh/vibe-coding/LangTutorial/FunLang/Elaborate.fs` - Type elaboration logic
- `/home/shoh/vibe-coding/LangTutorial/FunLang/Unify.fs` - Error context for mismatches
- `/home/shoh/vibe-coding/LangTutorial/FunLang/Diagnostic.fs` - Error representation
- `.planning/research/bidirectional-typing.md` - Phase design document (2026-02-03)

### Secondary (MEDIUM confidence)
- `.planning/ROADMAP.md` - Phase 4 success criteria
- `TESTING.md` - Test infrastructure and patterns
- Tests (type-errors/, type-inference/) - Existing error test patterns

### Tertiary (LOW confidence)
- None - all findings from existing codebase

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Testing infrastructure already in place, no new dependencies
- Architecture: HIGH - Implementation complete in Bidir.fs, validated by code reading
- Pitfalls: HIGH - Identified from actual implementation patterns and error types
- Error integration: MEDIUM - Phase 5 responsibility, current behavior documented

**Research date:** 2026-02-04
**Valid until:** 2026-03-04 (30 days - stable bidirectional type system implementation)

## Ready for Planning

Research complete. Key findings:

1. **Implementation exists:** Bidir.fs already handles Annot and LambdaAnnot
2. **Testing focus:** Phase 4 is primarily about test coverage, not new implementation
3. **Error validation:** Test both valid and invalid annotations with clear test cases
4. **Infrastructure ready:** fslit for CLI tests, Expecto for unit tests
5. **Clear success criteria:** Tests demonstrate annotation checking works correctly

Next step: Create PLAN.md files for:
- Plan 1: Valid annotation tests (Annot, LambdaAnnot synthesis)
- Plan 2: Invalid annotation tests (type mismatch, error messages)
