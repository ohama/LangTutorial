# v2.0 Roadmap: 실용성 강화

**Milestone:** FunLang v2.0
**Goal:** 코드 문서화, 문자열 타입, 대화형 개발 환경 추가
**Phases:** 3

---

## Phase Overview

| Phase | Name | Complexity | Est. LOC | Key Changes |
|-------|------|------------|----------|-------------|
| 1 | Comments | Low | ~15 | Lexer only |
| 2 | Strings | Medium | ~40 | Full pipeline |
| 3 | REPL | Medium | ~35 | CLI only |

**Total:** ~90 lines of new code

---

## Phase 1: Comments (주석)

### Goal
코드 문서화를 위한 주석 기능 추가

### Scope
- 단일행 주석 `// ...`
- 다중행 주석 `(* ... *)` (중첩 지원)

### Requirements
- CMT-01: 단일행 주석
- CMT-02: 다중행 주석
- CMT-03: 중첩 주석 지원
- CMT-04: 미종료 주석 오류

### Files to Modify
```
FunLang/Lexer.fsl   - 주석 규칙 추가
tests/*.fslit       - 주석 테스트 추가
```

### Success Criteria
```bash
$ funlang --expr "1 + 2 // comment"
3
$ funlang --expr "(* block *) 5"
5
```

### Dependencies
- None (첫 번째 Phase)

### Plans
**Plans:** 1 plan

Plans:
- [x] 01-01-PLAN.md — Implement comment lexer rules and tests

### Status
**Completed:** 2026-01-31

---

## Phase 2: Strings (문자열)

### Goal
문자열 데이터 타입 추가

### Scope
- 문자열 리터럴 `"hello"`
- 이스케이프 시퀀스 `\n`, `\t`, `\\`, `\"`
- 문자열 연결 `+`
- 문자열 비교 `=`, `<>`

### Requirements
- STR-01 ~ STR-12

### Files to Modify
```
FunLang/Lexer.fsl    - STRING 토큰, read_string 상태
FunLang/Parser.fsy   - STRING 토큰 선언, atom 규칙
FunLang/Ast.fs       - String expr, StringValue
FunLang/Eval.fs      - String 평가, Add/Equal 확장
FunLang/Format.fs    - StringValue 포맷팅
tests/*.fslit        - 문자열 테스트 추가
```

### Success Criteria
```bash
$ funlang --expr "\"hello\" + \" world\""
"hello world"
$ funlang --expr "\"a\" = \"a\""
true
```

### Dependencies
- Phase 1 (Comments) - 문자열 관련 코드에 주석 가능

### Plans
**Plans:** 1 plan

Plans:
- [ ] 02-01-PLAN.md — Add string type with full pipeline support

---

## Phase 3: REPL (대화형 셸)

### Goal
대화형 read-eval-print 루프 제공

### Scope
- 기본 REPL 루프
- 환경 지속성 (let 바인딩 유지)
- 오류 복구
- 정상 종료 (Ctrl+D, exit)

### Requirements
- REPL-01 ~ REPL-08

### Files to Modify
```
FunLang/Program.fs   - REPL 루프 추가, --repl 플래그
tests/*.fslit        - REPL 테스트 (가능한 경우)
```

### Success Criteria
```bash
$ funlang --repl
funlang> let x = 10
()
funlang> x + 5
15
funlang> exit
```

### Dependencies
- Phase 1 (Comments)
- Phase 2 (Strings) - 완전한 언어로 REPL 제공

---

## Implementation Order Rationale

```
Phase 1 → Phase 2 → Phase 3
  │         │         │
  │         │         └─ 완전한 언어로 최고의 UX
  │         │
  │         └─ 새 타입 추가, 주석으로 문서화 가능
  │
  └─ 가장 단순, 하위 영향 없음, 즉시 유용
```

**Inside-out 패턴:**
1. 코어 언어 기능 먼저 (Comments, Strings)
2. 개발자 경험 레이어 나중 (REPL)

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Lexer 상태 관리 오류 | Medium | Medium | FsLexYacc JSON 예제 패턴 따르기 |
| 환경 지속성 누락 | Medium | High | eval 반환값에 Env 포함 또는 REPL 전용 함수 |
| 토큰 순서 오류 | Low | Low | fsyacc 먼저, fslex 나중 빌드 |

---

## Definition of Done

### Phase 1: Comments ✓
- [x] `//` 단일행 주석 동작
- [x] `(* *)` 다중행 주석 동작
- [x] 중첩 주석 지원
- [x] 미종료 주석 오류 메시지
- [x] 기존 테스트 통과
- [x] 새 테스트 22개 추가 (12 fslit + 10 Expecto)

### Phase 2: Strings
- [ ] 문자열 리터럴 파싱
- [ ] 이스케이프 시퀀스 4종
- [ ] 문자열 연결 `+`
- [ ] 문자열 비교 `=`, `<>`
- [ ] 타입 오류 메시지
- [ ] 기존 테스트 통과
- [ ] 새 테스트 15개 추가

### Phase 3: REPL
- [ ] 기본 루프 동작
- [ ] 환경 지속성
- [ ] 오류 복구
- [ ] Ctrl+D, exit 종료
- [ ] `--repl` CLI 플래그
- [ ] 기존 테스트 통과
- [ ] 새 테스트 10개 추가 (가능한 경우)

---

## Milestone Completion Criteria

v2.0이 완료되면:

1. **주석** - 모든 코드에 주석 추가 가능
2. **문자열** - `"hello"` 리터럴, 연결, 비교 동작
3. **REPL** - `funlang --repl`로 대화형 세션 시작
4. **테스트** - 약 230개 테스트 (기존 195 + 새 35)
5. **문서** - `tutorial/chapter-06-strings.md`, `chapter-07-repl.md`

---

*Created: 2026-01-31*
*Based on: `.planning/v2.0/REQUIREMENTS.md`*
