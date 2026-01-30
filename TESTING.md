# FunLang Testing Guide

이 문서는 Claude가 테스트를 확장할 때 참조하는 가이드입니다.

## Quick Reference

```bash
# 전체 테스트 실행
make -C tests

# 카테고리별 실행
make -C tests cli
make -C tests emit-tokens
make -C tests emit-ast
make -C tests file-input

# 빌드 후 테스트
make -C tests check
```

---

## 테스트 구조

```
tests/
├── Makefile              # 테스트 실행 스크립트
├── cli/                  # --expr 기본 평가
├── emit-tokens/          # --emit-tokens 출력
├── emit-ast/             # --emit-ast 출력
├── file-input/           # 파일 입력 (%input)
├── variables/            # Phase 3: let, let-in (TODO)
├── control/              # Phase 4: if, bool, comparison (TODO)
└── functions/            # Phase 5: fn, rec, closure (TODO)
```

---

## fslit 테스트 작성법

### 기본 형식

```flt
// 테스트 설명
// --- Command: dotnet run --project FunLang -- <args>
// --- Output:
<expected output>
```

### 파일 입력 테스트

```flt
// 파일에서 읽기
// --- Command: dotnet run --project FunLang -- %input
// --- Input:
2 + 3
// --- Output:
5
```

### 규칙

1. **한 파일 = 한 테스트** (fslit 제약)
2. **파일명**: `NN-description.flt` (번호-설명)
3. **Output은 정확히 일치** (공백, 줄바꿈 포함)
4. **새 디렉토리 생성 시 Makefile 업데이트**

---

## Phase별 테스트 템플릿

### Phase 3: Variables & Binding

디렉토리: `tests/variables/`

```bash
mkdir -p tests/variables
```

**tests/variables/01-let-simple.flt:**
```flt
// Test: Simple let binding
// --- Command: dotnet run --project FunLang -- --expr "let x = 5 in x"
// --- Output:
5
```

**tests/variables/02-let-expr.flt:**
```flt
// Test: Let with expression
// --- Command: dotnet run --project FunLang -- --expr "let x = 2 + 3 in x * 2"
// --- Output:
10
```

**tests/variables/03-let-nested.flt:**
```flt
// Test: Nested let
// --- Command: dotnet run --project FunLang -- --expr "let x = 5 in let y = x + 1 in y"
// --- Output:
6
```

**tests/variables/04-let-shadow.flt:**
```flt
// Test: Variable shadowing
// --- Command: dotnet run --project FunLang -- --expr "let x = 5 in let x = 10 in x"
// --- Output:
10
```

### Phase 4: Control Flow

디렉토리: `tests/control/`

```bash
mkdir -p tests/control
```

**tests/control/01-if-true.flt:**
```flt
// Test: If true branch
// --- Command: dotnet run --project FunLang -- --expr "if true then 1 else 2"
// --- Output:
1
```

**tests/control/02-if-false.flt:**
```flt
// Test: If false branch
// --- Command: dotnet run --project FunLang -- --expr "if false then 1 else 2"
// --- Output:
2
```

**tests/control/03-comparison-gt.flt:**
```flt
// Test: Greater than comparison
// --- Command: dotnet run --project FunLang -- --expr "if 5 > 3 then 10 else 20"
// --- Output:
10
```

**tests/control/04-comparison-eq.flt:**
```flt
// Test: Equality comparison
// --- Command: dotnet run --project FunLang -- --expr "if 5 = 5 then 1 else 0"
// --- Output:
1
```

**tests/control/05-logical-and.flt:**
```flt
// Test: Logical AND
// --- Command: dotnet run --project FunLang -- --expr "if true && true then 1 else 0"
// --- Output:
1
```

**tests/control/06-logical-or.flt:**
```flt
// Test: Logical OR
// --- Command: dotnet run --project FunLang -- --expr "if false || true then 1 else 0"
// --- Output:
1
```

### Phase 5: Functions

디렉토리: `tests/functions/`

```bash
mkdir -p tests/functions
```

**tests/functions/01-simple.flt:**
```flt
// Test: Simple function
// --- Command: dotnet run --project FunLang -- --expr "let f x = x + 1 in f 5"
// --- Output:
6
```

**tests/functions/02-two-args.flt:**
```flt
// Test: Two argument function
// --- Command: dotnet run --project FunLang -- --expr "let add x y = x + y in add 3 4"
// --- Output:
7
```

**tests/functions/03-recursive.flt:**
```flt
// Test: Recursive factorial
// --- Command: dotnet run --project FunLang -- --expr "let rec fact n = if n <= 1 then 1 else n * fact (n - 1) in fact 5"
// --- Output:
120
```

**tests/functions/04-fibonacci.flt:**
```flt
// Test: Recursive fibonacci
// --- Command: dotnet run --project FunLang -- --expr "let rec fib n = if n <= 1 then n else fib (n - 1) + fib (n - 2) in fib 10"
// --- Output:
55
```

**tests/functions/05-closure.flt:**
```flt
// Test: Closure captures variable
// --- Command: dotnet run --project FunLang -- --expr "let x = 10 in let f y = x + y in f 5"
// --- Output:
15
```

---

## Makefile 업데이트

새 디렉토리 추가 시:

```makefile
# tests/Makefile에 추가

.PHONY: variables control functions

variables:
	@cd .. && fslit tests/variables/

control:
	@cd .. && fslit tests/control/

functions:
	@cd .. && fslit tests/functions/
```

---

## 테스트 추가 워크플로우

### 1. 기능 구현 전

```bash
# 1. 테스트 디렉토리 생성
mkdir -p tests/<category>

# 2. 실패하는 테스트 작성
# tests/<category>/01-feature.flt

# 3. 테스트 실행 (실패 확인)
make -C tests <category>
```

### 2. 기능 구현 후

```bash
# 1. 테스트 실행 (통과 확인)
make -C tests <category>

# 2. 전체 회귀 테스트
make -C tests

# 3. 커밋
git add tests/<category>/
git commit -m "test: add <category> tests for Phase N"
```

### 3. 실제 출력 확인

테스트 작성 전 실제 출력 형식 확인:

```bash
# AST 출력 형식 확인
dotnet run --project FunLang -- --emit-ast --expr "let x = 5 in x"

# 토큰 출력 형식 확인
dotnet run --project FunLang -- --emit-tokens --expr "let x = 5 in x"

# 평가 결과 확인
dotnet run --project FunLang -- --expr "let x = 5 in x"
```

---

## 현재 테스트 현황

| 카테고리 | 테스트 수 | Phase | 상태 |
|----------|-----------|-------|------|
| cli | 6 | 2 | ✓ 완료 |
| emit-tokens | 4 | 7 | ✓ 완료 |
| emit-ast | 6 | 7 | ✓ 완료 |
| file-input | 5 | 7 | ✓ 완료 |
| variables | 0 | 3 | 대기 |
| control | 0 | 4 | 대기 |
| functions | 0 | 5 | 대기 |

**총 테스트: 21개** (Phase 2, 7 완료)

---

## emit 옵션별 테스트

각 Phase에서 emit 옵션 테스트도 추가:

```bash
# Phase 3 토큰 테스트
tests/emit-tokens/10-let.flt
tests/emit-tokens/11-in.flt

# Phase 3 AST 테스트
tests/emit-ast/10-let.flt
tests/emit-ast/11-let-in.flt

# Phase 4 토큰 테스트
tests/emit-tokens/20-if.flt
tests/emit-tokens/21-bool.flt
tests/emit-tokens/22-comparison.flt

# Phase 4 AST 테스트
tests/emit-ast/20-if.flt
tests/emit-ast/21-comparison.flt
```

---

## 참고 문서

- `docs/howto/testing-strategies.md` - 테스트 전략 상세 (Expecto, FsCheck 포함)
- `docs/howto/setup-expecto-test-project.md` - Expecto 프로젝트 설정
- `.planning/ROADMAP.md` - Phase별 요구사항 및 성공 기준
