# Roadmap: LangTutorial v4.0 타입 시스템

## Overview

Implement Hindley-Milner type inference system for FunLang, enabling static type checking with full type inference. Starting from type AST definitions, building substitution and unification operations, implementing Algorithm W for type inference, and integrating with CLI for type checking and display. The implementation follows classic bottom-up approach: define types, build operations, implement inference algorithm, integrate with existing interpreter.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [x] **Phase 1: Type Definition** - Define type AST, schemes, and type environment
- [x] **Phase 2: Substitution** - Implement substitution operations and free variable tracking
- [x] **Phase 3: Unification** - Implement unification algorithm with occurs check
- [x] **Phase 4: Inference** - Implement Algorithm W for complete type inference
- [ ] **Phase 5: Integration** - Integrate with CLI and define Prelude types
- [ ] **Phase 6: Testing** - Comprehensive test coverage for type system

## Phase Details

### Phase 1: Type Definition
**Goal**: Type system foundation established with AST, schemes, and formatting
**Depends on**: Nothing (first phase)
**Requirements**: TYPE-01, TYPE-02, TYPE-03, TYPE-04
**Success Criteria** (what must be TRUE):
  1. Type AST represents all FunLang types (int, bool, string, arrow, tuple, list, type variables)
  2. Scheme type supports polymorphism with forall quantification
  3. TypeEnv maps variable names to type schemes
  4. formatType displays types in readable notation ('a -> 'b, int list, etc.)
**Plans**: 1 plan

Plans:
- [x] 01-01-PLAN.md — Create Type.fs with type definitions and formatType

### Phase 2: Substitution
**Goal**: Substitution operations work correctly for types, schemes, and environments
**Depends on**: Phase 1
**Requirements**: SUBST-01, SUBST-02, SUBST-03
**Success Criteria** (what must be TRUE):
  1. apply function correctly substitutes type variables in types
  2. compose function chains substitutions in correct order (s2 after s1)
  3. freeVars correctly identifies free type variables in types, schemes, and environments
  4. applyScheme respects bound variables (doesn't substitute forall variables)
**Plans**: 1 plan

Plans:
- [x] 02-01-PLAN.md — Implement substitution operations and free variable tracking

### Phase 3: Unification
**Goal**: Unification algorithm finds substitutions that make types equal
**Depends on**: Phase 2
**Requirements**: UNIFY-01, UNIFY-02, UNIFY-03
**Success Criteria** (what must be TRUE):
  1. occurs check prevents infinite types ('a = 'a -> int)
  2. unify finds most general unifier for compatible types
  3. unify correctly handles all type constructors (arrows, tuples, lists)
  4. TypeError provides clear messages when types cannot be unified
**Plans**: 1 plan

Plans:
- [x] 03-01-PLAN.md — Implement Unify.fs with occurs check and unify function

### Phase 4: Inference
**Goal**: Algorithm W infers types for all FunLang expressions
**Depends on**: Phase 3
**Requirements**: INFER-01, INFER-02, INFER-03, INFER-04, INFER-05, INFER-06, INFER-07, INFER-08, INFER-09, INFER-10, INFER-11, INFER-12, INFER-13, INFER-14, INFER-15
**Success Criteria** (what must be TRUE):
  1. Literals (Number, Bool, String) infer correct primitive types
  2. Binary operators infer correct types with proper constraints
  3. Variables instantiate polymorphic schemes from environment
  4. Let bindings support let-polymorphism (generalize and instantiate)
  5. Lambda and App infer function types correctly
  6. LetRec infers recursive function types
  7. If expressions unify branch types
  8. Tuples infer product types
  9. Lists (EmptyList, List, Cons) infer parameterized list types
  10. Match expressions infer pattern types and unify all branches
  11. LetPat generalizes pattern bindings
**Plans**: 5 plans

Plans:
- [x] 04-01-PLAN.md — Core functions (freshVar, instantiate, generalize)
- [x] 04-02-PLAN.md — Basic inference (literals, operators, variables)
- [x] 04-03-PLAN.md — Binding inference (Let, Lambda, App, LetRec)
- [x] 04-04-PLAN.md — Data inference (If, Tuple, List, EmptyList, Cons)
- [x] 04-05-PLAN.md — Pattern inference (Match, LetPat, inferPattern)

### Phase 5: Integration
**Goal**: Type system integrated with CLI and Prelude functions typed
**Depends on**: Phase 4
**Requirements**: INTEG-01, INTEG-02, INTEG-03, INTEG-04
**Success Criteria** (what must be TRUE):
  1. initialEnv defines polymorphic types for all Prelude functions
  2. typecheck function successfully type-checks valid expressions
  3. --emit-type CLI option displays inferred type
  4. Programs with type errors exit with code 1 and clear error message
  5. Type checking happens before evaluation by default
**Plans**: TBD

Plans:
- TBD

### Phase 6: Testing
**Goal**: Comprehensive test coverage validates type system correctness
**Depends on**: Phase 5
**Requirements**: TEST-01, TEST-02, TEST-03, TEST-04, TEST-05, TEST-06, TEST-07
**Success Criteria** (what must be TRUE):
  1. Type module unit tests verify type AST and formatType
  2. Subst module unit tests verify substitution operations
  3. Unify module unit tests verify unification with occurs check
  4. Infer module unit tests verify inference for all expression types
  5. TypeCheck integration tests verify end-to-end type checking
  6. fslit CLI tests verify --emit-type flag output
  7. Type error tests verify clear error messages for common mistakes
**Plans**: TBD

Plans:
- TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5 → 6

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Type Definition | 1/1 | Complete | 2026-02-01 |
| 2. Substitution | 1/1 | Complete | 2026-02-01 |
| 3. Unification | 1/1 | Complete | 2026-02-01 |
| 4. Inference | 5/5 | Complete | 2026-02-01 |
| 5. Integration | 0/TBD | Not started | - |
| 6. Testing | 0/TBD | Not started | - |

---
*Roadmap created: 2026-02-01*
*Milestone: v4.0 타입 시스템*
