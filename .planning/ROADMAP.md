# Milestone v6.0: Bidirectional Type System

**Status:** IN PROGRESS
**Started:** 2026-02-03
**Phases:** 6

## Overview

Transform FunLang's type system from Algorithm W to bidirectional type checking. Add ML-style type annotations with synthesis/checking modes. Maintain backward compatibility for unannotated code while enabling explicit type annotations for better error messages and documentation.

**Key Features:**
- Synthesis mode (up): infer type from expression
- Checking mode (down): verify expression against expected type
- ML-style annotations: `fun (x: int) -> x + 1`, `let f (x: int) : int = x`
- Curried multi-parameter: `fun (x: int) (y: int) -> x + y`
- Polymorphic annotations: `let id (x: 'a) : 'a = x`

## Research

See: `.planning/research/bidirectional-typing.md`

## Phases

### Phase 1: Parser Extensions ✓

**Goal**: Add type annotation syntax to lexer and parser
**Depends on**: Nothing (extends existing parser)
**Requirements**: PARSE-01, PARSE-02, PARSE-03, PARSE-04, PARSE-05, PARSE-06, PARSE-07
**Status:** COMPLETE (2026-02-03)
**Plans:** 3 plans

Plans:
- [x] 01-01-PLAN.md - Lexer tokens and AST types
- [x] 01-02-PLAN.md - Parser token declarations and TypeExpr grammar
- [x] 01-03-PLAN.md - Annotation syntax rules

**Success Criteria**:
1. COLON token recognized in lexer
2. Type keywords (int, bool, string, list) tokenized
3. Type variables ('a, 'b) tokenized
4. TypeExpr grammar parses type expressions
5. Annot and LambdaAnnot AST nodes defined
6. Curried annotated lambdas parse correctly
7. All existing tests pass (no breaking changes)

### Phase 2: Type Expression Elaboration ✓

**Goal**: Convert surface type syntax to internal Type representation
**Depends on**: Phase 1 (needs TypeExpr AST)
**Requirements**: ELAB-01, ELAB-02, ELAB-03
**Status:** COMPLETE (2026-02-03)
**Plans:** 2 plans

Plans:
- [x] 02-01-PLAN.md - Core elaboration module (Elaborate.fs)
- [x] 02-02-PLAN.md - Unit tests and integration validation

**Success Criteria**:
1. elaborateTypeExpr converts TypeExpr -> Type
2. Type variables in same binding scope map to same TVar index
3. Polymorphic annotations work correctly
4. Unit tests validate elaboration logic

### Phase 3: Bidirectional Core ✓

**Goal**: Implement synthesis and checking modes with hybrid approach
**Depends on**: Phase 2 (needs type elaboration)
**Requirements**: BIDIR-01, BIDIR-02, BIDIR-03, BIDIR-04, BIDIR-05, BIDIR-06, BIDIR-07
**Status:** COMPLETE (2026-02-03)
**Plans:** 2 plans

Plans:
- [x] 03-01-PLAN.md — Create Bidir.fs with synth/check functions
- [x] 03-02-PLAN.md — Build integration and unit tests

**Success Criteria**:
1. `synth` function infers types for synthesizing expressions
2. `check` function verifies expressions against expected types
3. Literals, variables, applications synthesize
4. Lambdas check against arrow types (parameter type from expected)
5. Unannotated lambdas use fresh type variables (hybrid approach)
6. Subsumption bridges synthesis to checking via unification
7. Let-polymorphism (generalize at let) preserved
8. All unannotated code produces same types as Algorithm W

### Phase 4: Annotation Checking

**Goal**: Handle annotated expressions and validate annotations
**Depends on**: Phase 3 (needs bidirectional core)
**Requirements**: ANNOT-01, ANNOT-02, ANNOT-03, ANNOT-04

**Success Criteria**:
1. `(e : T)` annotation expressions type check correctly
2. `fun (x: int) -> e` annotated lambdas synthesize correct types
3. Annotation type is validated against expression
4. Wrong annotations produce clear error messages
5. Tests cover valid and invalid annotations

### Phase 5: Error Integration

**Goal**: Mode-aware diagnostics with expected type information
**Depends on**: Phase 4 (needs annotation checking working)
**Requirements**: ERR-01, ERR-02, ERR-03

**Success Criteria**:
1. InferContext includes checking mode information
2. Error messages include expected type from annotations
3. Existing Diagnostic infrastructure handles bidirectional errors
4. Golden tests verify error message format

### Phase 6: Migration

**Goal**: Complete switchover from Algorithm W to bidirectional
**Depends on**: Phase 5 (needs all features working)
**Requirements**: MIG-01, MIG-02, MIG-03

**Success Criteria**:
1. All 570+ existing tests pass with Bidir module
2. CLI and REPL use Bidir instead of Infer
3. Tutorial chapter documents bidirectional type system
4. Old Infer module can be deprecated

---

## Requirements Coverage

| Phase | Requirements | Count |
|-------|--------------|-------|
| Phase 1 | PARSE-01 ~ PARSE-07 | 7 |
| Phase 2 | ELAB-01 ~ ELAB-03 | 3 |
| Phase 3 | BIDIR-01 ~ BIDIR-07 | 7 |
| Phase 4 | ANNOT-01 ~ ANNOT-04 | 4 |
| Phase 5 | ERR-01 ~ ERR-03 | 3 |
| Phase 6 | MIG-01 ~ MIG-03 | 3 |
| **Total** | | **27** |

## Key Decisions

| Decision | Rationale |
|----------|-----------|
| ML-style annotations | Familiar syntax, matches existing lambda syntax |
| Curried multi-parameter | `fun (x: int) (y: int) -> e` matches FunLang's curried style |
| Hybrid approach | Fresh vars for unannotated lambdas preserves backward compatibility |
| Keep let-polymorphism | Orthogonal to bidirectional structure, maintains expressiveness |
| Polymorphic annotations | `'a` syntax enables explicit polymorphism documentation |

## Dependencies

```
Phase 1 (Parser) -> Phase 2 (Elaboration) -> Phase 3 (Bidir Core)
                                                    |
                                          Phase 4 (Annotation)
                                                    |
                                          Phase 5 (Errors)
                                                    |
                                          Phase 6 (Migration)
```

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Parser conflicts | Medium | Medium | Careful grammar design, test incrementally |
| Type variable scoping | Medium | Low | Clear scoping rules, thorough tests |
| Backward incompatibility | Low | High | Compare with Algorithm W on all existing tests |
| Performance regression | Low | Low | Profile if needed, same asymptotic complexity |
