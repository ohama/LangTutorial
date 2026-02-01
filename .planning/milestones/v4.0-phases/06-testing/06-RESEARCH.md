# Phase 6: Testing - Research

**Researched:** 2026-02-01
**Domain:** F# unit testing for Hindley-Milner type inference system
**Confidence:** HIGH

## Summary

This research covers comprehensive testing strategies for the FunLang Hindley-Milner type inference system implemented in Phase 1-5. The project already has a mature test infrastructure with **Expecto** for unit tests (129 existing tests) and **fslit** for CLI integration tests (66 existing tests). Phase 6 extends this to cover the type system modules: Type.fs, Unify.fs, Infer.fs, and TypeCheck.fs.

The standard approach is two-tiered: (1) Expecto unit tests for module-level verification of type operations, substitution, unification, and inference logic; (2) fslit CLI tests for end-to-end type checking with the --emit-type flag. Testing Hindley-Milner systems requires special attention to: occurs check edge cases, polymorphic instantiation/generalization, substitution composition correctness, and clear error messages for type mismatches.

**Primary recommendation:** Use Expecto unit tests as the primary test layer for type system correctness (TEST-01 through TEST-05), add fslit CLI tests for --emit-type output verification (TEST-06), and create comprehensive type error message tests (TEST-07) covering common mistakes like infinite types, unbound variables, and type mismatches.

## Standard Stack

The established testing tools for F# language implementations:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Expecto | 10.2.3 | F# unit testing framework | Functional-first design, tests as values, parallel execution, already used in project |
| FsCheck | 2.16.6 (via Expecto.FsCheck 10.2.3) | Property-based testing | Random input generation, QuickCheck for .NET, ideal for type system invariants |
| fslit | Latest | File-based CLI testing | Already used for 66 tests, perfect for --emit-type integration tests |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| dotnet test | .NET 10.0 | Test runner | Alternative to Expecto's built-in runner (not needed here) |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Expecto | xUnit.net or NUnit | Expecto is more functional, tests as values, better F# integration |
| fslit | Custom bash scripts | fslit provides structured test format and clear reporting |

**Installation:**
Already installed in FunLang.Tests/FunLang.Tests.fsproj:
```xml
<PackageReference Include="Expecto" Version="10.2.3" />
<PackageReference Include="Expecto.FsCheck" Version="10.2.3" />
```

## Architecture Patterns

### Recommended Test Structure
```
FunLang.Tests/
├── TypeTests.fs         # TEST-01: Type module (formatType, apply, freeVars)
├── UnifyTests.fs        # TEST-03: Unify module (occurs check, unification)
├── InferTests.fs        # TEST-04: Infer module (inference for all expressions)
├── TypeCheckTests.fs    # TEST-05: TypeCheck integration tests
└── Program.fs           # Test runner (updated with new test lists)

tests/
├── type-inference/      # TEST-06: fslit CLI tests for --emit-type
│   ├── 01-literals.flt
│   ├── 02-functions.flt
│   ├── 03-polymorphism.flt
│   └── ...
└── type-errors/         # TEST-07: Type error messages
    ├── 01-infinite-type.flt
    ├── 02-unbound-var.flt
    ├── 03-type-mismatch.flt
    └── ...
```

### Pattern 1: Module Unit Tests (Expecto)
**What:** Test individual functions in type system modules
**When to use:** For testing Type.fs, Unify.fs, Infer.fs operations in isolation
**Example:**
```fsharp
// Source: Existing FunLang.Tests/Program.fs pattern
module TypeTests

open Expecto
open Type

[<Tests>]
let typeTests =
    testList "Type Module" [
        testList "formatType" [
            test "formats primitive types" {
                Expect.equal (formatType TInt) "int" ""
                Expect.equal (formatType TBool) "bool" ""
                Expect.equal (formatType TString) "string" ""
            }

            test "formats type variables" {
                Expect.equal (formatType (TVar 0)) "'a" ""
                Expect.equal (formatType (TVar 1)) "'b" ""
                Expect.equal (formatType (TVar 26)) "'a" "cycles after 'z"
            }

            test "formats arrow types with parentheses" {
                let t = TArrow(TArrow(TInt, TBool), TString)
                Expect.equal (formatType t) "(int -> bool) -> string" ""
            }
        ]

        testList "Substitution" [
            test "apply substitutes type variables" {
                let s = Map.ofList [(0, TInt)]
                Expect.equal (apply s (TVar 0)) TInt ""
            }

            test "apply handles transitive chains" {
                let s = Map.ofList [(0, TVar 1); (1, TInt)]
                Expect.equal (apply s (TVar 0)) TInt "should follow chain"
            }

            test "compose applies substitutions correctly" {
                let s1 = Map.ofList [(0, TVar 1)]
                let s2 = Map.ofList [(1, TInt)]
                let s = compose s2 s1
                Expect.equal (apply s (TVar 0)) TInt ""
            }
        ]
    ]
```

### Pattern 2: Unification Tests
**What:** Test Robinson's unification algorithm with occurs check
**When to use:** For verifying unify function correctness
**Example:**
```fsharp
// Source: Academic Hindley-Milner testing best practices
module UnifyTests

open Expecto
open Type
open Unify

[<Tests>]
let unifyTests =
    testList "Unification" [
        testList "occurs check" [
            test "detects infinite type 'a = 'a -> int" {
                Expect.throws (fun () ->
                    unify (TVar 0) (TArrow(TVar 0, TInt)) |> ignore
                ) "should raise TypeError for infinite type"
            }

            test "allows 'a = int -> 'b" {
                let s = unify (TVar 0) (TArrow(TInt, TVar 1))
                Expect.equal (Map.find 0 s) (TArrow(TInt, TVar 1)) ""
            }
        ]

        testList "primitive unification" [
            test "int unifies with int" {
                let s = unify TInt TInt
                Expect.equal s Map.empty ""
            }

            test "int does not unify with bool" {
                Expect.throws (fun () ->
                    unify TInt TBool |> ignore
                ) "should raise TypeError"
            }
        ]

        testList "arrow unification" [
            test "unifies compatible arrows" {
                let t1 = TArrow(TVar 0, TInt)
                let t2 = TArrow(TBool, TVar 1)
                let s = unify t1 t2
                Expect.equal (apply s (TVar 0)) TBool ""
                Expect.equal (apply s (TVar 1)) TInt ""
            }
        ]
    ]
```

### Pattern 3: Inference Tests
**What:** Test Algorithm W for all expression types
**When to use:** For verifying type inference correctness
**Example:**
```fsharp
// Source: Existing project pattern + HM testing best practices
module InferTests

open Expecto
open Type
open Infer
open Ast

let parse (input: string) : Expr =
    let lexbuf = FSharp.Text.Lexing.LexBuffer<char>.FromString input
    Parser.start Lexer.tokenize lexbuf

let inferType expr =
    let s, ty = infer Map.empty expr
    apply s ty

[<Tests>]
let inferTests =
    testList "Type Inference" [
        testList "literals" [
            test "infers int" {
                let ty = inferType (Number 42)
                Expect.equal ty TInt ""
            }

            test "infers bool" {
                let ty = inferType (Bool true)
                Expect.equal ty TBool ""
            }
        ]

        testList "polymorphism" [
            test "let-polymorphism works" {
                // let id = fun x -> x in id 5
                let expr = parse "let id = fun x -> x in id 5"
                let ty = inferType expr
                Expect.equal ty TInt "id should be polymorphic"
            }

            test "generalize creates polymorphic schemes" {
                let ty = TVar 1000  // Fresh var
                let scheme = generalize Map.empty ty
                Expect.equal scheme (Scheme([1000], TVar 1000)) ""
            }
        ]
    ]
```

### Pattern 4: CLI Integration Tests (fslit)
**What:** Test --emit-type flag output format
**When to use:** For end-to-end type checking verification
**Example:**
```flt
// Source: Existing tests/variables/01-basic-let.flt pattern
// Test: Polymorphic identity function type
// --- Command: dotnet run --project FunLang -- --emit-type %input
// --- Input:
let id = fun x -> x in id
// --- Output:
'a -> 'a
```

### Pattern 5: Error Message Tests (fslit)
**What:** Verify clear, helpful type error messages
**When to use:** For testing user-facing error reporting
**Example:**
```flt
// Test: Infinite type error message
// --- Command: dotnet run --project FunLang -- --emit-type %input
// --- Input:
let rec f x = f
// --- Output:
Error: Infinite type: 'a = 'a -> 'b
// --- Exit: 1
```

### Anti-Patterns to Avoid
- **Testing implementation details**: Don't test internal helper functions, test public API
- **Brittle error message matching**: Match on error type (TypeError), not exact string
- **Over-reliance on property tests**: Use property tests for invariants, not correctness
- **Missing negative tests**: Always test both success and failure cases

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Random test input generation | Custom random generators | FsCheck's Arbitrary | Handles edge cases, shrinking, distribution |
| Test runner | Custom test harness | Expecto's runTestsWithCLIArgs | Filtering, parallel execution, reporting |
| CLI output testing | Bash scripts with manual comparison | fslit | Structured format, clear diffs, regression prevention |
| Type variable fresh generation | Simple counter in tests | Infer.freshVar() | Ensures no collision with real implementation |

**Key insight:** The type system modules are already implemented correctly. Testing verifies behavior, catches regressions, and documents expected behavior. Use existing test infrastructure patterns from Phase 2-5.

## Common Pitfalls

### Pitfall 1: Testing Without Type Environment Context
**What goes wrong:** Tests fail because Prelude functions aren't in scope
**Why it happens:** Forgetting that TypeCheck.initialTypeEnv includes 11 Prelude functions
**How to avoid:** Use TypeCheck.typecheck for integration tests, infer Map.empty for pure inference tests
**Warning signs:** "Unbound variable: map" errors in tests that seem valid

### Pitfall 2: Type Variable Numbering Confusion
**What goes wrong:** Tests break because type variables don't match expected values
**Why it happens:** freshVar uses counter starting at 1000, Prelude uses 0-9
**How to avoid:** Test with formatType (letter form) not raw TVar numbers
**Warning signs:** Expecting TVar 0 but getting TVar 1000

### Pitfall 3: Forgetting Substitution Composition Order
**What goes wrong:** Substitution tests give wrong results
**Why it happens:** compose s2 s1 means "s2 after s1", easy to reverse
**How to avoid:** Test with concrete examples: s1 = {0→1}, s2 = {1→Int}
**Warning signs:** Tests pass individually but fail when combined

### Pitfall 4: Incomplete Error Message Testing
**What goes wrong:** Users get cryptic type errors
**Why it happens:** Only testing successful type checking, not error cases
**How to avoid:** TEST-07 requires type error tests for common mistakes
**Warning signs:** No tests using Expect.throws or Exit: 1 in fslit

### Pitfall 5: Not Testing Polymorphic Instantiation
**What goes wrong:** Polymorphic functions break when used multiple times
**Why it happens:** Forgetting that each use of polymorphic var gets fresh type vars
**How to avoid:** Test classic example: let id = fun x -> x in (id 5, id true)
**Warning signs:** First use works, second use fails

### Pitfall 6: Ignoring Prelude Function Types
**What goes wrong:** Tests don't verify Prelude functions have correct types
**Why it happens:** Assuming initialTypeEnv is correct without testing
**How to avoid:** TEST-05 must verify all 11 Prelude function types
**Warning signs:** Runtime type errors in map, filter, fold, etc.

## Code Examples

Verified patterns from F# and type inference best practices:

### Testing formatType Display
```fsharp
// Source: Type.fs formatType function
test "formats list types" {
    Expect.equal (formatType (TList TInt)) "int list" ""
    Expect.equal (formatType (TList (TVar 0))) "'a list" ""
}

test "formats tuple types" {
    Expect.equal (formatType (TTuple [TInt; TBool])) "int * bool" ""
    Expect.equal (formatType (TTuple [TVar 0; TVar 1; TVar 2])) "'a * 'b * 'c" ""
}

test "formats complex nested types" {
    // ('a -> 'b) -> 'a list -> 'b list
    let mapTy = TArrow(TArrow(TVar 0, TVar 1), TArrow(TList (TVar 0), TList (TVar 1)))
    Expect.equal (formatType mapTy) "('a -> 'b) -> 'a list -> 'b list" ""
}
```

### Testing Occurs Check
```fsharp
// Source: Academic HM implementations
test "occurs check in nested types" {
    // 'a = ('a -> int) -> bool  (infinite)
    let ty = TArrow(TArrow(TVar 0, TInt), TBool)
    Expect.throws (fun () ->
        unify (TVar 0) ty |> ignore
    ) "should detect 'a in nested position"
}

test "occurs check with lists" {
    // 'a = 'a list  (infinite)
    Expect.throws (fun () ->
        unify (TVar 0) (TList (TVar 0)) |> ignore
    ) "should detect 'a list = 'a"
}
```

### Testing Let-Polymorphism
```fsharp
// Source: Classic HM test cases
test "polymorphic function used at different types" {
    let input = "let id = fun x -> x in (id 5, id true)"
    let expr = parse input
    let ty = inferType expr
    Expect.equal ty (TTuple [TInt; TBool]) "id used polymorphically"
}

test "function arguments are monomorphic" {
    // fun f -> (f 1, f true)  should fail
    let input = "fun f -> (f 1, f true)"
    let expr = parse input
    Expect.throws (fun () ->
        inferType expr |> ignore
    ) "lambda params are monomorphic"
}
```

### Testing fslit --emit-type Output
```flt
// Source: Existing fslit test patterns
// Test: Map function type
// --- Command: dotnet run --project FunLang -- --emit-type %input
// --- Input:
map
// --- Output:
('a -> 'b) -> 'a list -> 'b list
```

### Testing Type Error Messages
```flt
// Source: Type error testing best practices
// Test: Type mismatch in if branches
// --- Command: dotnet run --project FunLang -- --emit-type %input
// --- Input:
if true then 1 else false
// --- Output:
Error: Cannot unify int with bool
// --- Exit: 1
```

### Property-Based Testing for Invariants
```fsharp
// Source: FsCheck documentation + HM properties
open FsCheck

testProperty "substitution is idempotent" <| fun (ty: Type) ->
    let s = Map.empty
    apply s (apply s ty) = apply s ty

testProperty "composition is associative" <| fun (s1: Subst) (s2: Subst) (s3: Subst) (ty: Type) ->
    let left = apply (compose (compose s3 s2) s1) ty
    let right = apply (compose s3 (compose s2 s1)) ty
    left = right

testProperty "freeVars after substitution" <| fun (ty: Type) (v: int) (t: Type) ->
    not (Set.contains v (freeVars t)) ==>
        lazy (Set.contains v (freeVars (apply (Map.ofList [(v, t)]) ty)) = false)
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Manual CLI testing | fslit file-based tests | v2.0 (2025) | Regression prevention, 66 tests |
| NUnit/xUnit | Expecto | FunLang start (2025) | Functional-first, tests as values |
| Example-based only | Property-based with FsCheck | Phase 2 (2026) | 11 property tests for invariants |
| No type testing | --emit-type flag | Phase 5 (2026-02-01) | Type inference verification |

**Deprecated/outdated:**
- Manual test scripts: Replaced by fslit in v2.0
- Inline test comments: Replaced by Expecto unit tests

## Open Questions

Things that couldn't be fully resolved:

1. **Property-based test coverage for type inference**
   - What we know: FsCheck can generate random types and substitutions
   - What's unclear: How to generate valid AST expressions for property tests
   - Recommendation: Start with unit tests, add property tests for Type/Unify modules only

2. **Test coverage metrics**
   - What we know: .NET has dotnet-coverage tool
   - What's unclear: Whether coverage metrics are needed for tutorial project
   - Recommendation: Manual review of requirements coverage is sufficient

3. **Integration with existing Prelude tests**
   - What we know: tests/prelude/ has 13 tests for Prelude functions
   - What's unclear: Whether to test Prelude types separately or via existing tests
   - Recommendation: TEST-05 should verify Prelude types, existing tests verify behavior

## Sources

### Primary (HIGH confidence)
- [Expecto GitHub Repository](https://github.com/haf/expecto) - Official F# testing framework
- [FsCheck Documentation](https://fscheck.github.io/FsCheck/) - Property-based testing
- [Unification Algorithm - Wikipedia](https://en.wikipedia.org/wiki/Unification_(computer_science)) - Occurs check testing
- [Occurs Check - Wikipedia](https://en.wikipedia.org/wiki/Occurs_check) - Edge cases

### Secondary (MEDIUM confidence)
- [F# for Fun and Profit - Property-Based Testing](https://fsharpforfunandprofit.com/posts/property-based-testing-1/) - Testing patterns
- [Type Inference Testing](https://course.ccs.neu.edu/cs4410sp19/lec_type-inference_notes.html) - Academic testing strategies
- [Getting into the Flow: Type Error Messages](https://doi.org/10.1145/3622812) - Error message best practices (2023)

### Tertiary (LOW confidence)
- [TypeScript Best Practices 2026](https://johal.in/typescript-best-practices-for-large-scale-web-applications-in-2026/) - General type system testing ideas
- [Designing Type Inference for High Quality Error Messages](https://blog.polybdenum.com/2025/02/14/designing-type-inference-for-high-quality-type-errors.html) - Recent blog post (2025)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Expecto and fslit already in use, proven in project
- Architecture: HIGH - Follows existing Phase 2-5 patterns, mature infrastructure
- Pitfalls: MEDIUM - Based on HM literature and common mistakes, project-specific

**Research date:** 2026-02-01
**Valid until:** 2026-03-01 (30 days - stable testing frameworks, established HM patterns)
