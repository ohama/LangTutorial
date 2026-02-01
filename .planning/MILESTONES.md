# Project Milestones: LangTutorial

## v4.0 타입 시스템 (Shipped: 2026-02-01)

**Delivered:** Hindley-Milner 타입 추론 시스템으로 정적 타입 검사 지원

**Phases completed:** 1-6 (12 plans total)

**Key accomplishments:**

- Type AST — 7개 타입 생성자 (TInt, TBool, TString, TVar, TArrow, TTuple, TList)
- Algorithm W — 완전한 Hindley-Milner 타입 추론 구현
- Let-polymorphism — 다형성 let 바인딩 지원
- Prelude 타입 — 11개 표준 라이브러리 함수 타입 정의
- CLI 통합 — --emit-type 플래그로 타입 표시
- 460 total tests — 362 Expecto + 98 fslit (219개 타입 시스템 테스트 추가)

**Stats:**

- 4,805 lines of F# (2,608 core + 2,197 tests)
- 6 phases, 12 plans, 33 requirements
- 84 files modified
- 3.3 hours (2026-02-01 18:33 → 21:53)

**Git range:** `bde5582` → `c4a9f0a`

**What's next:** v5.0 (ADT, TCO, 예외 처리) 또는 프로젝트 완료

---

## v3.0 데이터 구조 (Shipped: 2026-02-01)

**Delivered:** 튜플, 리스트, 패턴 매칭, 자체 호스팅 표준 라이브러리를 추가하여 FunLang을 실용적인 함수형 언어로 발전

**Phases completed:** 1-4 (8 plans total)

**Key accomplishments:**

- Tuples — 고정 크기 이종 데이터 컬렉션, 패턴 분해 `let (x, y) = pair`
- Lists — 가변 길이 컬렉션, cons 연산자 `::`, syntactic sugar `[1, 2, 3]`
- Pattern Matching — match 표현식, 7가지 패턴 타입, first-match 의미론
- Self-hosted Prelude — FunLang으로 작성된 11개 표준 라이브러리 함수 (map, filter, fold 등)
- 333 total tests — 158 fslit + 175 Expecto

**Stats:**

- 2,325 lines of F#
- 4 phases, 8 plans, 25 requirements
- 2 days (2026-01-31 → 2026-02-01)
- ~62 commits

**Git range:** `4ad83d7` → `aa05225`

**What's next:** v4.0 (ADT, TCO, 예외 처리) 또는 프로젝트 완료

---

## v2.0 실용성 강화 (Shipped: 2026-02-01)

**Delivered:** 주석, 문자열 타입, 대화형 REPL을 추가하여 FunLang의 실용성 강화

**Phases completed:** 1-3 (4 plans total)

**Key accomplishments:**

- Comments - 단일행 `//` 및 중첩 가능한 다중행 `(* *)` 주석
- Strings - 리터럴, 이스케이프(\n, \t, \\, \"), 연결(+), 비교(=, <>)
- Argu CLI - 선언적 CLI 파싱으로 120 LOC 대체
- REPL - 오류 복구 및 #quit 명령을 가진 대화형 루프

**Stats:**

- 73 files modified
- ~200 lines of F# added (2,117 → 2,317)
- 3 phases, 4 plans
- 2 days from v1.0 to v2.0
- 275 total tests (100 fslit + 175 Expecto)

**Git range:** `1dbebae` → `9bc2f63`

**What's next:** v2.1 품질 개선 또는 프로젝트 완료

---

## v1.0 FunLang MVP (Shipped: 2026-01-31)

**Delivered:** F# 개발자를 위한 Turing-complete 프로그래밍 언어 인터프리터 튜토리얼

**Phases completed:** 1-5, 7 (12 plans total)

**Key accomplishments:**

- Foundation & Pipeline - .NET 10 + FsLexYacc 파이프라인 구축
- Arithmetic - 사칙연산, 연산자 우선순위, 단항 마이너스
- Variables - let 바인딩, 스코프 지원 환경 기반 평가
- Control Flow - Boolean, if-then-else, 비교/논리 연산자, 타입 검사
- Functions - 람다, 함수 호출, 재귀, 클로저 (Turing-complete 달성)
- Testing - 66 fslit + 129 Expecto 테스트 구축

**Stats:**

- 147 files created/modified
- 2,117 lines of F#
- 6 phases, 12 plans
- 2 days from start to ship
- 195 total tests (66 fslit + 129 Expecto)

**Git range:** `tutorial-v0.0` → `tutorial-v5.0`

**On hold:** Phase 6 (Quality & Polish) - REPL, 에러 메시지 개선

**What's next:** 프로젝트 완료 (Phase 6 필요시 재개 가능)

---
