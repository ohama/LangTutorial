# Roadmap: LangTutorial

## Overview

Transform v4.0's basic type errors into precise diagnostics with location tracking, context awareness, and helpful messages. Build Span infrastructure to track source locations, define rich Diagnostic types with context stacks and unification traces, integrate blame assignment into Algorithm W, and render user-friendly error messages with type pretty-printing.

## Milestones

- ✅ **v1.0 Foundation** - Phases 1-6 (shipped 2025)
- ✅ **v2.0 REPL & Strings** - Phases 1-3 (shipped 2025)
- ✅ **v3.0 Data Structures** - Phases 1-4 (shipped 2025)
- ✅ **v4.0 Type System** - Phases 1-6 (shipped 2026-02-01)
- 🚧 **v5.0 Type Error Diagnostics** - Phases 1-4 (in progress)

## Phases

<details>
<summary>✅ v1.0 Foundation (Phases 1-6) - SHIPPED 2025</summary>

### Phase 1: Project Setup
**Goal**: .NET 10 project with FsLexYacc pipeline
**Plans**: Completed

### Phase 2: Arithmetic
**Goal**: Calculator with operator precedence
**Plans**: Completed

### Phase 3: Variables
**Goal**: Let bindings and environment-based scoping
**Plans**: Completed

### Phase 4: Control Flow
**Goal**: Booleans, if-then-else, comparison operators
**Plans**: Completed

### Phase 5: Functions
**Goal**: Lambda expressions, function calls, recursion, closures
**Plans**: Completed

### Phase 6: CLI & Testing
**Goal**: Command-line interface with Argu, comprehensive test suite
**Plans**: Completed

</details>

<details>
<summary>✅ v2.0 REPL & Strings (Phases 1-3) - SHIPPED 2025</summary>

### Phase 1: Comments
**Goal**: Single-line and nested multi-line comments
**Plans**: Completed

### Phase 2: Strings
**Goal**: String literals, escape sequences, concatenation
**Plans**: Completed

### Phase 3: REPL
**Goal**: Interactive read-eval-print loop with error recovery
**Plans**: Completed

</details>

<details>
<summary>✅ v3.0 Data Structures (Phases 1-4) - SHIPPED 2025</summary>

### Phase 1: Tuples
**Goal**: Fixed-size heterogeneous data with pattern destructuring
**Plans**: Completed

### Phase 2: Lists
**Goal**: Variable-length collections with cons operator
**Plans**: Completed

### Phase 3: Pattern Matching
**Goal**: Match expressions with 7 pattern types
**Plans**: Completed

### Phase 4: Prelude
**Goal**: Self-hosted standard library (map, filter, fold, etc.)
**Plans**: Completed

</details>

<details>
<summary>✅ v4.0 Type System (Phases 1-6) - SHIPPED 2026-02-01</summary>

### Phase 1: Type Definitions
**Goal**: Type AST, Scheme, TypeEnv, formatType
**Plans**: Completed

### Phase 2: Substitution
**Goal**: Substitution operations (apply, compose, freeVars)
**Plans**: Completed

### Phase 3: Unification
**Goal**: Occurs check, unify, basic TypeError
**Plans**: Completed

### Phase 4: Type Inference
**Goal**: Algorithm W with let-polymorphism
**Plans**: Completed

### Phase 5: Integration
**Goal**: Prelude types, typecheck, --emit-type flag
**Plans**: Completed

### Phase 6: Testing
**Goal**: Comprehensive type system test coverage
**Plans**: Completed

</details>

### 🚧 v5.0 Type Error Diagnostics (In Progress)

**Milestone Goal:** Transform basic type errors into precise diagnostics with location tracking, context awareness, and helpful error messages.

#### Phase 1: Span Infrastructure
**Goal**: Source location tracking across lexer, parser, and AST
**Depends on**: Nothing (foundation for all diagnostics)
**Requirements**: SPAN-01, SPAN-02, SPAN-03, SPAN-04
**Success Criteria** (what must be TRUE):
  1. Every Expr node carries span information (file, start/end line and column)
  2. Lexer generates position data for every token
  3. Parser propagates spans from tokens to AST nodes
  4. Span type can represent unknown locations for built-in definitions
**Plans**: 2 plans

Plans:
- [ ] 01-01-PLAN.md — Span type definition + Lexer position tracking
- [ ] 01-02-PLAN.md — AST span integration + Parser propagation

#### Phase 2: Error Representation
**Goal**: Rich diagnostic types with context stacks and unification traces
**Depends on**: Phase 1 (needs Span)
**Requirements**: DIAG-01, DIAG-02, DIAG-03, DIAG-04, CTX-01, CTX-02, CTX-03, TRACE-01, TRACE-02, TRACE-03
**Success Criteria** (what must be TRUE):
  1. Diagnostic type represents errors with code, message, spans, notes, and hints
  2. TypeError captures kind (UnifyMismatch, OccursCheck, UnboundVar, NotAFunction), expected/actual types, and term
  3. InferContext tracks inference path (InIfCond, InAppFun, InLetRhs, etc.)
  4. UnifyPath records structural failure location (AtFunctionReturn, AtTupleIndex, etc.)
  5. TypeError includes context stack and unification trace
**Plans**: TBD

Plans:
- [ ] 02-01: TBD

#### Phase 3: Blame Assignment
**Goal**: Accurate error location selection integrated with Algorithm W
**Depends on**: Phase 2 (needs error representation)
**Requirements**: BLAME-01, BLAME-02, BLAME-03
**Success Criteria** (what must be TRUE):
  1. Primary span points to the most direct cause of the error
  2. Secondary spans highlight related expressions contributing to the error
  3. Innermost expressions are prioritized for blame assignment
  4. Type inference functions maintain context stack during recursion
**Plans**: TBD

Plans:
- [ ] 03-01: TBD

#### Phase 4: Output & Testing
**Goal**: User-friendly error messages and comprehensive diagnostic tests
**Depends on**: Phase 3 (needs blame assignment)
**Requirements**: OUT-01, OUT-02, OUT-03, OUT-04, TEST-01, TEST-02, TEST-03, TEST-04, TEST-05, TEST-06
**Success Criteria** (what must be TRUE):
  1. Error codes follow defined schema (E0301, etc.)
  2. Error messages show location, expected/actual types, context summary, and hints
  3. Type variables are normalized to a,b,c format in output
  4. CLI displays new error format when type checking fails
  5. Tests cover if-condition type errors, non-function calls, argument mismatches, let RHS errors, and occurs check
  6. Golden test framework validates diagnostic output format
**Plans**: TBD

Plans:
- [ ] 04-01: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 1. Span Infrastructure | v5.0 | 0/2 | Planned | - |
| 2. Error Representation | v5.0 | 0/? | Not started | - |
| 3. Blame Assignment | v5.0 | 0/? | Not started | - |
| 4. Output & Testing | v5.0 | 0/? | Not started | - |

---
*Last updated: 2026-02-02 - Phase 1 planned (2 plans)*
