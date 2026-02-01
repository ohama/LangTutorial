# Roadmap: LangTutorial v3.0

**Milestone:** v3.0 데이터 구조
**Goal:** 복합 데이터 타입, 패턴 매칭, 표준 라이브러리를 추가하여 FunLang을 실용적인 함수형 언어로 발전
**Created:** 2026-02-01

## Overview

```
v3.0 데이터 구조
├── Phase 1: Tuples (튜플) ────────── TUP-01~04
├── Phase 2: Lists (리스트) ───────── LIST-01~04
├── Phase 3: Pattern Matching ─────── PAT-01~08
└── Phase 4: Prelude ──────────────── PRE-01~09
```

**Total:** 4 phases | 25 requirements

## Phase Details

---

### Phase 1: Tuples (튜플)

**Goal:** 고정 크기 이종 데이터 컬렉션 지원

**Requirements:**
- TUP-01: 튜플 리터럴 생성 `(1, true)`, `(1, 2, 3)`
- TUP-02: 튜플 패턴 분해 `let (x, y) = pair`
- TUP-03: 중첩 튜플 지원 `((1, 2), 3)`
- TUP-04: TupleValue 타입 추가

**Success Criteria:**
1. `(1, 2)` 표현식이 TupleValue를 반환한다
2. `let (x, y) = (1, 2) in x + y`가 3을 반환한다
3. `let ((a, b), c) = ((1, 2), 3) in a + b + c`가 6을 반환한다
4. 서로 다른 타입의 값을 포함하는 튜플 `(1, true, "hello")`가 동작한다
5. REPL에서 튜플이 `(1, 2)`로 출력된다

**Dependencies:** None (기존 Lexer/Parser/Eval 확장)

**Plans:** 2 plans

Plans:
- [x] 01-01-PLAN.md — AST, Lexer, Parser infrastructure for tuple syntax
- [x] 01-02-PLAN.md — Evaluation, pattern matching, integration tests

---

### Phase 2: Lists (리스트)

**Goal:** 동종 데이터의 가변 길이 컬렉션 지원

**Requirements:**
- LIST-01: 빈 리스트 리터럴 `[]`
- LIST-02: 리스트 리터럴 `[1, 2, 3]` (syntactic sugar)
- LIST-03: Cons 연산자 `0 :: xs`
- LIST-04: ListValue 타입 추가

**Success Criteria:**
1. `[]` 표현식이 빈 ListValue를 반환한다
2. `[1, 2, 3]`이 `1 :: 2 :: 3 :: []`와 동일하게 동작한다
3. `0 :: [1, 2]`가 `[0, 1, 2]`를 반환한다
4. 중첩 리스트 `[[1, 2], [3, 4]]`가 동작한다
5. REPL에서 리스트가 `[1, 2, 3]`으로 출력된다

**Dependencies:** None (Phase 1과 독립적으로 구현 가능)

---

### Phase 3: Pattern Matching (패턴 매칭)

**Goal:** 구조적 분해와 조건 분기를 통합하는 match 표현식 지원

**Requirements:**
- PAT-01: match 표현식 `match e with | p1 -> e1 | p2 -> e2`
- PAT-02: 변수 패턴 `x`
- PAT-03: 와일드카드 패턴 `_`
- PAT-04: 상수 패턴 (정수, 불리언)
- PAT-05: Cons 패턴 `h :: t`
- PAT-06: 튜플 패턴 `(x, y)`
- PAT-07: 빈 리스트 패턴 `[]`
- PAT-08: 완전성 검사 (exhaustiveness check)

**Success Criteria:**
1. `match 1 with | 1 -> "one" | _ -> "other"`가 `"one"`을 반환한다
2. `match [1, 2] with | [] -> 0 | h :: t -> h`가 1을 반환한다
3. `match (1, 2) with | (x, y) -> x + y`가 3을 반환한다
4. 매칭되지 않는 경우 런타임 에러가 발생한다
5. 완전성 검사가 누락된 패턴에 대해 경고를 출력한다

**Dependencies:** Phase 1 (튜플 패턴), Phase 2 (리스트/cons 패턴)

---

### Phase 4: Prelude (표준 라이브러리)

**Goal:** 자주 사용되는 함수들을 미리 정의하여 제공

**Requirements:**
- PRE-01: `map` 함수 구현
- PRE-02: `filter` 함수 구현
- PRE-03: `fold` 함수 구현
- PRE-04: `length` 함수 구현
- PRE-05: `reverse` 함수 구현
- PRE-06: `append` 함수 구현
- PRE-07: `id`, `const`, `compose` 유틸리티
- PRE-08: `hd`, `tl` 리스트 연산 함수
- PRE-09: 시작 시 Prelude.fun 자동 로드

**Success Criteria:**
1. `map (fun x -> x * 2) [1, 2, 3]`이 `[2, 4, 6]`을 반환한다
2. `filter (fun x -> x > 1) [1, 2, 3]`이 `[2, 3]`을 반환한다
3. `fold (fun a b -> a + b) 0 [1, 2, 3]`이 6을 반환한다
4. `hd [1, 2, 3]`이 1을, `tl [1, 2, 3]`이 `[2, 3]`을 반환한다
5. FunLang 시작 시 Prelude 함수들이 자동으로 사용 가능하다

**Dependencies:** Phase 2 (리스트), Phase 3 (패턴 매칭)

---

## Dependency Graph

```
Phase 1: Tuples ──────┐
                      ├──▶ Phase 3: Pattern Matching ──▶ Phase 4: Prelude
Phase 2: Lists ───────┘
```

Phase 1과 2는 병렬로 진행 가능. Phase 3은 1, 2 완료 후. Phase 4는 3 완료 후.

---

## Requirements Coverage

| Phase | Requirements | Count |
|-------|--------------|-------|
| Phase 1 | TUP-01~04 | 4 |
| Phase 2 | LIST-01~04 | 4 |
| Phase 3 | PAT-01~08 | 8 |
| Phase 4 | PRE-01~09 | 9 |
| **Total** | | **25** |

**Coverage:** 100%

---

*Roadmap created: 2026-02-01*
