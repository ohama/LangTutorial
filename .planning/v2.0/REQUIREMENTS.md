# v2.0 Requirements: 실용성 강화

**Milestone:** FunLang v2.0
**Goal:** 코드 문서화, 문자열 타입, 대화형 개발 환경 추가
**Research:** `.planning/research/v2.0/SUMMARY.md`

---

## Overview

v2.0은 v1.0의 Turing-complete 언어에 실용적인 기능을 추가합니다:
1. **주석** - 코드 문서화
2. **문자열** - 새로운 데이터 타입
3. **REPL** - 대화형 개발 환경

---

## Phase 1: Comments (주석) ✓ COMPLETE

### Requirements

| ID | Requirement | Priority | Status | Acceptance Criteria |
|----|-------------|----------|--------|---------------------|
| CMT-01 | 단일행 주석 `//` | MUST | ✓ | `// comment` 이후 줄 끝까지 무시 |
| CMT-02 | 다중행 주석 `(* *)` | SHOULD | ✓ | `(* ... *)` 사이 모든 내용 무시 |
| CMT-03 | 중첩 주석 지원 | SHOULD | ✓ | `(* outer (* inner *) outer *)` 올바르게 파싱 |
| CMT-04 | 미종료 주석 오류 | MUST | ✓ | `(* without close` → 명확한 오류 메시지 |

### Success Criteria

```bash
# 단일행 주석
$ funlang --expr "1 + 2  // this is ignored"
3

# 다중행 주석
$ funlang --expr "(* comment *) 5"
5

# 중첩 주석
$ funlang --expr "(* outer (* nested *) *) 10"
10

# 미종료 오류
$ funlang --expr "(* unclosed"
Error: Unterminated comment
```

### Implementation Notes

- Lexer-only 변경 (Parser, AST, Eval 변경 없음)
- `/` 연산자보다 `//` 패턴이 먼저 매칭되어야 함
- 다중행 주석은 fslex `and` 규칙으로 depth 추적

---

## Phase 2: Strings (문자열)

### Requirements

| ID | Requirement | Priority | Acceptance Criteria |
|----|-------------|----------|---------------------|
| STR-01 | 문자열 리터럴 | MUST | `"hello"` → StringValue "hello" |
| STR-02 | 이스케이프: `\\` | MUST | `"\\"` → 백슬래시 문자 |
| STR-03 | 이스케이프: `\"` | MUST | `"\""` → 큰따옴표 문자 |
| STR-04 | 이스케이프: `\n` | MUST | `"\n"` → 개행 문자 |
| STR-05 | 이스케이프: `\t` | MUST | `"\t"` → 탭 문자 |
| STR-06 | 문자열 연결 | MUST | `"a" + "b"` → `"ab"` |
| STR-07 | 문자열 동등 비교 | MUST | `"a" = "a"` → true |
| STR-08 | 문자열 부등 비교 | MUST | `"a" <> "b"` → true |
| STR-09 | 빈 문자열 | MUST | `""` → StringValue "" |
| STR-10 | 미종료 문자열 오류 | MUST | `"unclosed` → 명확한 오류 |
| STR-11 | 문자열 내 개행 금지 | MUST | 리터럴 개행 → 오류 |
| STR-12 | 혼합 타입 연산 오류 | MUST | `"a" + 1` → 타입 오류 |

### Success Criteria

```bash
# 기본 문자열
$ funlang --expr "\"hello\""
"hello"

# 이스케이프 시퀀스
$ funlang --expr "\"line1\\nline2\""
"line1
line2"

# 문자열 연결
$ funlang --expr "\"hello\" + \" \" + \"world\""
"hello world"

# 문자열 비교
$ funlang --expr "\"abc\" = \"abc\""
true

$ funlang --expr "\"abc\" <> \"def\""
true

# 변수와 함께
$ funlang --expr "let name = \"FunLang\" in \"Hello, \" + name"
"Hello, FunLang"

# 타입 오류
$ funlang --expr "\"text\" + 123"
Error: Type error: + requires operands of same type
```

### Implementation Notes

- **Lexer**: STRING 토큰, `and read_string` 상태 머신
- **Parser**: STRING 토큰 → String AST 노드
- **AST**: `String of string` 추가, `StringValue of string` 추가
- **Eval**: Add/Equal/NotEqual 패턴 확장
- **Format**: StringValue → `"quoted"` 형식 출력

---

## Phase 3: REPL (대화형 셸)

### Requirements

| ID | Requirement | Priority | Acceptance Criteria |
|----|-------------|----------|---------------------|
| REPL-01 | 기본 루프 | MUST | 프롬프트 표시, 입력 읽기, 평가, 결과 출력 |
| REPL-02 | 프롬프트 | MUST | `funlang> ` 프롬프트 표시 |
| REPL-03 | 환경 지속성 | MUST | let 바인딩이 다음 입력에서도 유효 |
| REPL-04 | 오류 복구 | MUST | 오류 발생 시 루프 계속 |
| REPL-05 | EOF 종료 | MUST | Ctrl+D (EOF) 시 정상 종료 |
| REPL-06 | exit 명령 | MUST | `exit` 입력 시 정상 종료 |
| REPL-07 | CLI 플래그 | SHOULD | `--repl` 또는 인자 없이 실행 |
| REPL-08 | 시작 메시지 | SHOULD | 버전, 종료 방법 안내 |

### Success Criteria

```bash
$ funlang --repl
FunLang REPL (type 'exit' to quit)
funlang> 1 + 2
3
funlang> let x = 10
()
funlang> x * 2
20
funlang> let f = fun y -> x + y
()
funlang> f 5
15
funlang> undefined_var
Error: Undefined variable: undefined_var
funlang> 3 + 4
7
funlang> exit
$

# 또는 Ctrl+D로 종료
funlang> ^D
$
```

### Implementation Notes

- **Program.fs**: `--repl` 패턴 추가, REPL 루프 함수
- **Environment**: 루프 간 Env 전달 (재생성 금지)
- **Error handling**: try-catch로 예외 포착, 루프 계속
- **Console**: `Console.ReadLine()` 사용 (외부 의존성 없음)

---

## Non-Goals (v2.1+로 연기)

| Feature | Reason |
|---------|--------|
| REPL 히스토리 (↑/↓) | 외부 라이브러리 필요 |
| Tab 자동완성 | 복잡도 높음 |
| 문자열 인덱싱 `s.[0]` | 새 구문 필요 |
| 문자열 함수 `length` | 내장 함수 메커니즘 필요 |
| 문자열 보간 `$"{x}"` | 파싱 복잡도 높음 |
| 다중행 REPL 입력 | 완료 감지 복잡 |

---

## Test Coverage

### Comments Phase
- `//` 단일행 주석 (3 tests)
- `(* *)` 다중행 주석 (3 tests)
- 중첩 주석 (2 tests)
- 오류 케이스 (2 tests)

### Strings Phase
- 리터럴 파싱 (3 tests)
- 이스케이프 시퀀스 (5 tests)
- 연산 (4 tests)
- 타입 오류 (3 tests)

### REPL Phase
- 기본 평가 (2 tests)
- 환경 지속성 (3 tests)
- 오류 복구 (2 tests)
- 종료 (2 tests)

---

## Dependencies

### Phase Dependencies
```
Phase 1 (Comments) → Phase 2 (Strings) → Phase 3 (REPL)
                     ↓
                     [Full pipeline test with comments + strings]
```

### External Dependencies
- **없음** - 기존 FsLexYacc 11.3.0 + .NET 10 스택 유지

---

*Created: 2026-01-31*
*Based on: `.planning/research/v2.0/SUMMARY.md`*
