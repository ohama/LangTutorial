# Project Milestones: LangTutorial

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
