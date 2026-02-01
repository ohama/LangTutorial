---
phase: 02-lists
verified: 2026-02-01T10:07:00Z
status: gaps_found
score: 9/10 must-haves verified
gaps:
  - artifact: "FunLang/Format.fs"
    issue: "Missing token formatters for list tokens (LBRACKET, RBRACKET, CONS, COMMA, UNDERSCORE)"
    severity: "blocking"
    impact: "--emit-tokens mode fails for expressions containing list syntax, causing test hangs"
    missing:
      - "Add LBRACKET case to formatToken function"
      - "Add RBRACKET case to formatToken function"
      - "Add CONS case to formatToken function"
      - "Add COMMA case to formatToken function (from Phase 1)"
      - "Add UNDERSCORE case to formatToken function (from Phase 1)"
---

# Phase 2: Lists Verification Report

**Phase Goal:** 동종 데이터의 가변 길이 컬렉션 지원
**Verified:** 2026-02-01T10:07:00Z
**Status:** gaps_found
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | Empty list [] can be parsed and evaluated | ✓ VERIFIED | Parser.fsy has `LBRACKET RBRACKET { EmptyList }`, Eval.fs has `EmptyList -> ListValue []` |
| 2 | List literal [1, 2, 3] can be parsed | ✓ VERIFIED | Parser.fsy has list literal rules, tested with `dotnet run -e '[1, 2, 3]'` outputs `[1, 2, 3]` |
| 3 | Cons operator :: can be parsed | ✓ VERIFIED | Parser.fsy has `Expr CONS Expr { Cons($1, $3) }`, tested with `dotnet run -e '1 :: []'` outputs `[1]` |
| 4 | Cons is right-associative (1 :: 2 :: [] groups as 1 :: (2 :: [])) | ✓ VERIFIED | Parser.fsy has `%right CONS`, AST output shows `Cons (Number 1, Cons (Number 2, EmptyList))` |
| 5 | Empty list [] evaluates to ListValue [] | ✓ VERIFIED | Eval.fs line 80-81: `EmptyList -> ListValue []` |
| 6 | List literal [1, 2, 3] evaluates to ListValue with three IntValues | ✓ VERIFIED | Eval.fs line 83-85, tested successfully |
| 7 | Cons operator 0 :: [1, 2] evaluates to ListValue [0, 1, 2] | ✓ VERIFIED | Eval.fs line 87-91, tested with `dotnet run -e '0 :: [1, 2]'` outputs `[0, 1, 2]` |
| 8 | List literals desugar correctly: [1, 2] equals 1 :: 2 :: [] | ✓ VERIFIED | Tested with `dotnet run -e '[1, 2] = 1 :: 2 :: []'` outputs `true` |
| 9 | Lists display in REPL as [1, 2, 3] format | ✓ VERIFIED | Eval.fs line 19-21: formatValue for ListValue uses `sprintf "[%s]"` |
| 10 | --emit-tokens mode works for list syntax | ✗ FAILED | Format.fs missing token formatters, causes incomplete pattern match error |

**Score:** 9/10 truths verified

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `FunLang/Ast.fs` | List AST types (EmptyList, List, Cons, ListValue) | ✓ VERIFIED | Lines 42-44: EmptyList, List, Cons. Line 62: ListValue. Substantive (67 lines total). |
| `FunLang/Lexer.fsl` | List tokens (LBRACKET, RBRACKET, CONS) | ✓ VERIFIED | Line 48: CONS, Line 66: LBRACKET, Line 67: RBRACKET. Substantive (89 lines total). |
| `FunLang/Parser.fsy` | List grammar rules | ✓ VERIFIED | Line 29: `%right CONS`, Lines 65, 101-103: list grammar. Substantive (122 lines total). |
| `FunLang/Eval.fs` | List evaluation logic | ✓ VERIFIED | Lines 80-91: EmptyList, List, Cons cases. Lines 148, 157: list equality. Substantive (218 lines total). |
| `FunLang/Eval.fs` | List formatting | ✓ VERIFIED | Lines 19-21: formatValue for ListValue. Used in REPL. |
| `FunLang/Format.fs` | Token formatters for list tokens | ✗ STUB | Missing LBRACKET, RBRACKET, CONS, COMMA, UNDERSCORE cases. Causes incomplete pattern match. |
| `tests/lists/*.flt` | List integration tests | ✓ VERIFIED | 12 test files present, 11/12 pass (one fails due to Format.fs gap). |
| `tests/Makefile` | Lists test target | ✓ VERIFIED | Line 47-48: `lists:` target defined. |

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|----|--------|---------|
| Parser.fsy | Ast.fs | List grammar rules produce List/EmptyList/Cons AST nodes | ✓ WIRED | Parser lines 65, 101-103 produce EmptyList, List, Cons from Ast.fs |
| Parser.fsy | %right CONS | Cons operator right-associativity | ✓ WIRED | Line 29: `%right CONS` declaration enforces right-associativity |
| Eval.fs eval function | EmptyList/List/Cons cases | Pattern match on Expr type | ✓ WIRED | Lines 80-91 handle all three list expression types |
| Eval.fs formatValue | ListValue formatting | Pattern match on Value type | ✓ WIRED | Lines 19-21 format ListValue as `[%s]` |
| tests/lists/*.flt | FunLang binary | fslit test execution | ✓ WIRED | 11/12 tests pass successfully |
| Format.fs formatToken | List tokens | Pattern match on Parser.token type | ✗ NOT_WIRED | Missing cases for LBRACKET, RBRACKET, CONS, COMMA, UNDERSCORE |

### Requirements Coverage

| Requirement | Status | Supporting Evidence |
|-------------|--------|---------------------|
| LIST-01: Empty list literal `[]` | ✓ SATISFIED | Parsing, evaluation, and display all work correctly |
| LIST-02: List literal `[1, 2, 3]` (syntactic sugar) | ✓ SATISFIED | Parsing, evaluation, desugaring verified |
| LIST-03: Cons operator `0 :: xs` | ✓ SATISFIED | Parsing with right-associativity, evaluation verified |
| LIST-04: ListValue type | ✓ SATISFIED | Type defined, equality works, formatting works |

### Success Criteria Verification

| # | Criterion | Status | Evidence |
|---|-----------|--------|----------|
| 1 | `[]` expression returns empty ListValue | ✓ VERIFIED | Tested: `dotnet run -e '[]'` outputs `[]` |
| 2 | `[1, 2, 3]` is equivalent to `1 :: 2 :: 3 :: []` | ✓ VERIFIED | Tested: `[1, 2, 3] = 1 :: 2 :: 3 :: []` outputs `true` |
| 3 | `0 :: [1, 2]` returns `[0, 1, 2]` | ✓ VERIFIED | Tested: `dotnet run -e '0 :: [1, 2]'` outputs `[0, 1, 2]` |
| 4 | Nested lists `[[1, 2], [3, 4]]` work | ✓ VERIFIED | Tested: `dotnet run -e '[[1, 2], [3, 4]]'` outputs `[[1, 2], [3, 4]]` |
| 5 | REPL displays lists as `[1, 2, 3]` | ✓ VERIFIED | formatValue implements correct formatting |

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| FunLang/Format.fs | 8 | Incomplete pattern match | 🛑 BLOCKER | --emit-tokens mode fails for list syntax, causes test failures and hangs |

### Test Results

**fslit tests (list tests only):**
- 11/12 passed
- 1 failure: `08-list-heterogeneous.flt` (fails due to Format.fs incomplete pattern match in build warning)

**fslit tests (other categories):**
- tuples: 10/10 passed (no regressions)
- functions: 13/13 passed (no regressions)
- emit-tokens: 4/4 passed (no list syntax used, so Format.fs gap not triggered)

**Expecto tests:**
- 175/175 passed
- 4 warnings about incomplete pattern matches in test code (ListValue not handled in test helpers)

**Total test count:** 122 fslit tests (.flt files)

### Gaps Summary

**One gap blocking full Phase 2 completion:**

1. **Format.fs missing list token formatters** (BLOCKER)
   - **Issue:** The `formatToken` function in Format.fs doesn't handle the new list tokens (LBRACKET, RBRACKET, CONS) or tuple tokens (COMMA, UNDERSCORE) added in v3.0.
   - **Impact:** `--emit-tokens` mode fails with "incomplete pattern matches" error when processing list syntax, causing test failures and hangs.
   - **Missing implementations:**
     - `| Parser.LBRACKET -> "LBRACKET"`
     - `| Parser.RBRACKET -> "RBRACKET"`
     - `| Parser.CONS -> "CONS"`
     - `| Parser.COMMA -> "COMMA"` (from Phase 1)
     - `| Parser.UNDERSCORE -> "UNDERSCORE"` (from Phase 1)
   - **Files affected:** FunLang/Format.fs (lines 7-41)
   - **Evidence:** Running `dotnet run --project FunLang -- --emit-tokens -e '[1, 2, 3]'` produces error: "Error: The match cases were incomplete"

**Why this is a gap despite working functionality:**

The core list feature works perfectly - parsing, evaluation, REPL display, and all success criteria pass. However, the --emit-tokens debugging utility is broken for list syntax. This is a blocking gap because:

1. **Completeness:** The phase includes adding list tokens to the lexer, and Format.fs is responsible for displaying those tokens. An incomplete implementation is a stub.
2. **Testing infrastructure:** One fslit test fails due to build warnings from the incomplete pattern match, indicating the gap affects the test suite.
3. **Debugging utility:** --emit-tokens is a documented feature for language exploration and debugging. Breaking it for list syntax is a regression.

**Note:** This gap also affects Phase 1 (Tuples), as COMMA and UNDERSCORE tokens from that phase are also missing from Format.fs. This went unnoticed because no tests use --emit-tokens with tuple or pattern syntax.

---

_Verified: 2026-02-01T10:07:00Z_
_Verifier: Claude (gsd-verifier)_
