# LangTutorial

## What This Is

F# 개발자를 위한 프로그래밍 언어 구현 튜토리얼. fslex와 fsyacc를 사용하여 인터프리터를 단계별로 만들어가는 과정을 문서화한다. 가장 간단한 사칙연산 언어에서 시작해 변수, 조건문, 함수까지 확장해 나간다.

## Core Value

각 챕터가 독립적으로 동작하는 완전한 예제를 제공하여, 독자가 언어 구현의 각 단계를 직접 따라하고 실행해볼 수 있어야 한다.

## Requirements

### Validated

(None yet — ship to validate)

### Active

- [ ] 사칙연산 언어 구현 (Chapter 1)
- [ ] 변수 바인딩 추가 (Chapter 2)
- [ ] 조건문 추가 (Chapter 3)
- [ ] 함수 정의와 호출 추가 (Chapter 4)
- [ ] `/tutorial` 스킬 등록 — 튜토리얼 목록 표시 및 작성 지시

### Out of Scope

- 실수 (float/double) — 정수만 지원, 파싱/연산 단순화
- 컴파일러 (바이트코드/네이티브 코드 생성) — 인터프리터에 집중
- 타입 시스템 — v1 범위 초과, 추후 확장 가능
- 표준 라이브러리 — 언어 코어에 집중

## Context

**기술 스택:**
- F# (.NET 10)
- fslex — 렉서 생성기
- fsyacc — 파서 생성기

**디렉토리 구조:**
- `tutorial/` — 튜토리얼 문서 (Markdown)
  - `chapter-01-arithmetic.md`
  - `chapter-02-variables.md`
  - `chapter-03-conditionals.md`
  - `chapter-04-functions.md`
- `FunLang/` — 실제 언어 구현 코드 (F# 프로젝트)

**문서 형식:**
- Markdown with inline code blocks
- 각 챕터는 해당 기능의 전체 구현을 포함

## Constraints

- **Language**: F# — 함수형 언어로 인터프리터 구현에 적합
- **Tools**: fslex, fsyacc — F#용 전통적 렉서/파서 생성기
- **Target Audience**: F# 개발자 — F# 문법 설명 불필요

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| 인터프리터 방식 선택 | 컴파일러보다 단순하고 즉각적인 결과 확인 가능 | — Pending |
| fslex/fsyacc 사용 | F# 생태계의 표준 도구, 학습 가치 있음 | — Pending |
| 사칙연산부터 시작 | 최소한의 복잡도로 전체 파이프라인 구축 | — Pending |

---
*Last updated: 2025-01-30 after initialization*
