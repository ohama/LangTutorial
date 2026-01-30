# FunLang Testing Guide

이 문서는 Claude가 테스트를 확장할 때 참조하는 가이드입니다.

---

## Phase 완료 시 필수 테스트

**각 Phase 구현이 완료되면 반드시 다음 테스트를 수행한다:**

### 1. fslit 파일 기반 테스트 (CLI E2E)

```bash
# 새 Phase용 테스트 디렉토리 생성
mkdir -p tests/<phase-name>/

# 테스트 파일 작성 (최소 10-20개)
# tests/<phase-name>/01-feature.flt, 02-feature.flt, ...

# 실행
make -C tests <phase-name>
```

### 2. Expecto 단위 테스트

```bash
# FunLang.Tests/Program.fs에 Phase별 테스트 추가
# - 각 요구사항(REQ-XX)별 테스트 그룹
# - 정상 케이스 + 에러 케이스
# - AST 구성 테스트

dotnet run --project FunLang.Tests
```

### 3. FsCheck 속성 테스트 (해당 시)

```bash
# 수학적 불변식이 있는 경우 추가
# - 교환법칙, 결합법칙
# - 항등원, 역원
# - 타입 보존 속성

dotnet run --project FunLang.Tests
```

### 체크리스트

- [ ] `tests/<phase>/` 디렉토리 생성 및 fslit 테스트 작성
- [ ] `tests/Makefile`에 새 타겟 추가
- [ ] `FunLang.Tests/Program.fs`에 Phase 테스트 추가
- [ ] 모든 fslit 테스트 통과: `make -C tests`
- [ ] 모든 Expecto 테스트 통과: `dotnet run --project FunLang.Tests`
- [ ] `TESTING.md` 테스트 현황 업데이트

---

## Quick Reference

```bash
# 1. fslit 파일 기반 테스트 (CLI 검증)
make -C tests

# 2. Expecto 단위 테스트 (내부 로직)
dotnet run --project FunLang.Tests

# 3. 전체 테스트
make -C tests && dotnet run --project FunLang.Tests
```

---

## 테스트 피라미드

```
        ┌─────────┐
        │  fslit  │  CLI E2E (느림, 넓음)
        └────┬────┘
        ┌────┴────┐
        │ Expecto │  단위 테스트 (빠름)
        └────┬────┘
   ┌─────────┴─────────┐
   │  FsCheck (선택)   │  속성 테스트 (자동 생성)
   └───────────────────┘
```

| 도구 | 목적 | 언제 사용 |
|------|------|-----------|
| **fslit** | CLI 통합 테스트 | 회귀 방지, E2E 검증 |
| **Expecto** | 단위 테스트 | 모듈별 로직 검증 |
| **FsCheck** | 속성 기반 테스트 | 수학적 불변식 검증 (선택) |

---

## 테스트 구조

```
LangTutorial/
├── tests/                    # fslit 파일 기반 테스트
│   ├── Makefile
│   ├── cli/                  # --expr 기본 평가
│   ├── emit-tokens/          # --emit-tokens 출력
│   ├── emit-ast/             # --emit-ast 출력
│   ├── file-input/           # 파일 입력 (%input)
│   ├── variables/            # Phase 3: let, let-in
│   ├── control/              # Phase 4: if, bool, comparison, logical
│   └── functions/            # Phase 5: fn, rec (TODO)
│
└── FunLang.Tests/            # Expecto 단위 테스트 프로젝트
    ├── FunLang.Tests.fsproj
    ├── LexerTests.fs
    ├── ParserTests.fs
    ├── EvalTests.fs
    └── Program.fs
```

---

## Part 1: fslit 파일 기반 테스트

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

### 실행

```bash
# 전체 테스트
make -C tests

# 카테고리별
make -C tests cli
make -C tests variables
make -C tests emit-tokens

# 빌드 후 테스트
make -C tests check
```

---

## Part 2: Expecto 단위 테스트

### 프로젝트 설정

```bash
# 테스트 프로젝트 생성
dotnet new console -lang F# -n FunLang.Tests -f net10.0
cd FunLang.Tests

# 패키지 추가
dotnet add package Expecto
dotnet add package Expecto.FsCheck  # FsCheck 통합 (선택)
dotnet add reference ../FunLang/FunLang.fsproj
```

### 테스트 구조

**FunLang.Tests/LexerTests.fs:**

```fsharp
module LexerTests

open Expecto
open FSharp.Text.Lexing

[<Tests>]
let lexerTests =
    testList "Lexer" [
        test "tokenizes number" {
            let lexbuf = LexBuffer<char>.FromString "42"
            let token = Lexer.tokenize lexbuf
            Expect.equal token (Parser.NUMBER 42) "should be NUMBER(42)"
        }

        test "tokenizes let keyword" {
            let lexbuf = LexBuffer<char>.FromString "let"
            let token = Lexer.tokenize lexbuf
            Expect.equal token Parser.LET "should be LET"
        }

        test "tokenizes identifier" {
            let lexbuf = LexBuffer<char>.FromString "foo"
            let token = Lexer.tokenize lexbuf
            Expect.equal token (Parser.IDENT "foo") "should be IDENT(foo)"
        }

        test "distinguishes keywords from identifiers" {
            let lexbuf = LexBuffer<char>.FromString "letter"
            let token = Lexer.tokenize lexbuf
            Expect.equal token (Parser.IDENT "letter") "letter is IDENT, not LET"
        }
    ]
```

**FunLang.Tests/ParserTests.fs:**

```fsharp
module ParserTests

open Expecto
open FSharp.Text.Lexing
open Ast

let parse input =
    let lexbuf = LexBuffer<char>.FromString input
    Parser.start Lexer.tokenize lexbuf

[<Tests>]
let parserTests =
    testList "Parser" [
        test "parses number" {
            let ast = parse "42"
            Expect.equal ast (Number 42) ""
        }

        test "parses addition" {
            let ast = parse "2 + 3"
            Expect.equal ast (Add(Number 2, Number 3)) ""
        }

        test "parses let-in" {
            let ast = parse "let x = 5 in x"
            Expect.equal ast (Let("x", Number 5, Var "x")) ""
        }

        test "respects operator precedence" {
            let ast = parse "2 + 3 * 4"
            Expect.equal ast (Add(Number 2, Multiply(Number 3, Number 4))) ""
        }

        test "parses nested let" {
            let ast = parse "let x = 1 in let y = 2 in x + y"
            let expected = Let("x", Number 1, Let("y", Number 2, Add(Var "x", Var "y")))
            Expect.equal ast expected ""
        }
    ]
```

**FunLang.Tests/EvalTests.fs:**

```fsharp
module EvalTests

open Expecto
open Ast
open Eval

[<Tests>]
let evalTests =
    testList "Eval" [
        test "evaluates number" {
            Expect.equal (evalExpr (Number 42)) 42 ""
        }

        test "evaluates addition" {
            Expect.equal (evalExpr (Add(Number 2, Number 3))) 5 ""
        }

        test "evaluates let binding" {
            let expr = Let("x", Number 5, Var "x")
            Expect.equal (evalExpr expr) 5 ""
        }

        test "evaluates let with expression body" {
            let expr = Let("x", Number 5, Add(Var "x", Number 1))
            Expect.equal (evalExpr expr) 6 ""
        }

        test "evaluates nested let" {
            let expr = Let("x", Number 1, Let("y", Number 2, Add(Var "x", Var "y")))
            Expect.equal (evalExpr expr) 3 ""
        }

        test "evaluates shadowing correctly" {
            let expr = Let("x", Number 1, Let("x", Number 2, Var "x"))
            Expect.equal (evalExpr expr) 2 ""
        }

        test "throws on undefined variable" {
            Expect.throws (fun () -> evalExpr (Var "x") |> ignore) "should throw"
        }
    ]
```

**FunLang.Tests/Program.fs:**

```fsharp
open Expecto

[<EntryPoint>]
let main argv =
    runTestsWithCLIArgs [] argv <| testList "All" [
        LexerTests.lexerTests
        ParserTests.parserTests
        EvalTests.evalTests
    ]
```

### 실행

```bash
# 전체 테스트
dotnet run --project FunLang.Tests

# 필터링
dotnet run --project FunLang.Tests -- --filter "Lexer"
dotnet run --project FunLang.Tests -- --filter "Eval"

# 상세 출력
dotnet run --project FunLang.Tests -- --debug
```

### 주요 Expect 함수

| 함수 | 용도 |
|------|------|
| `Expect.equal actual expected msg` | 동등성 |
| `Expect.isTrue condition msg` | 불린 |
| `Expect.throws<ExnType> (fun () -> ...) msg` | 예외 |
| `Expect.isSome option msg` | Option 값 |

---

## Part 3: FsCheck 속성 테스트 (선택)

수학적 성질을 검증할 때 사용. 복잡하면 Expecto 단위 테스트로 대체 가능.

### 설정

```bash
dotnet add package FsCheck
dotnet add package Expecto.FsCheck
```

### 속성 테스트 예시

**FunLang.Tests/PropertyTests.fs:**

```fsharp
module PropertyTests

open Expecto
open FsCheck
open Ast
open Eval

[<Tests>]
let propertyTests =
    testList "Properties" [
        // 숫자는 그대로 평가됨
        testProperty "number evaluates to itself" <| fun (n: int) ->
            evalExpr (Number n) = n

        // 덧셈은 교환법칙
        testProperty "addition is commutative" <| fun (a: int) (b: int) ->
            let left = evalExpr (Add(Number a, Number b))
            let right = evalExpr (Add(Number b, Number a))
            left = right

        // 0을 더해도 변하지 않음
        testProperty "zero is additive identity" <| fun (n: int) ->
            evalExpr (Add(Number n, Number 0)) = n

        // 1을 곱해도 변하지 않음
        testProperty "one is multiplicative identity" <| fun (n: int) ->
            evalExpr (Multiply(Number n, Number 1)) = n

        // 이중 부정 = 원래 값
        testProperty "double negation is identity" <| fun (n: int) ->
            evalExpr (Negate(Negate(Number n))) = n
    ]
```

**Program.fs에 추가:**

```fsharp
[<EntryPoint>]
let main argv =
    runTestsWithCLIArgs [] argv <| testList "All" [
        LexerTests.lexerTests
        ParserTests.parserTests
        EvalTests.evalTests
        PropertyTests.propertyTests  // 추가
    ]
```

### 조건부 속성

```fsharp
testProperty "division by non-zero" <| fun (a: int) (b: int) ->
    b <> 0 ==> lazy (
        evalExpr (Divide(Number a, Number b)) = a / b
    )
```

---

## Phase별 테스트 작성

### Phase 3: Variables (완료)

**fslit 테스트:** `tests/variables/` (12개)
- 01-12: let binding, variable reference, nested scope, shadowing, emit tests

**Expecto 테스트 추가:**

```fsharp
// EvalTests.fs에 추가
test "inner scope doesn't affect outer" {
    // let x = 1 in (let y = x + 1 in y) + x = 2 + 1 = 3
    let expr = Let("x", Number 1,
                   Add(Let("y", Add(Var "x", Number 1), Var "y"),
                       Var "x"))
    Expect.equal (evalExpr expr) 3 ""
}
```

### Phase 4: Control Flow (완료)

**fslit 테스트:** `tests/control/` (20개)
- 01-17: if-then-else, boolean literals, comparison ops, logical ops
- 18-20: emit-tokens/emit-ast verification

**Expecto 테스트:** `FunLang.Tests/Program.fs` Phase 4 section (~30개)
- CTRL-01: if-then-else (true/false branch, nested, with let)
- CTRL-02: boolean literals (true, false)
- CTRL-03: comparison operators (=, <>, <, >, <=, >=)
- CTRL-04: logical operators (&&, ||, short-circuit)
- Type errors (if condition must be bool, comparison operands must be int)
- AST construction tests

### Phase 5: Functions (TODO)

**fslit 테스트:** `tests/functions/` 생성

**Expecto 테스트:**

```fsharp
[<Tests>]
let functionTests =
    testList "Functions" [
        test "simple function application" {
            // let f x = x + 1 in f 5 = 6
            let expr = Let("f", Lambda("x", Add(Var "x", Number 1)),
                          App(Var "f", Number 5))
            Expect.equal (evalExpr expr) 6 ""
        }

        test "closure captures environment" {
            // let x = 10 in let f y = x + y in f 5 = 15
            let expr = Let("x", Number 10,
                          Let("f", Lambda("y", Add(Var "x", Var "y")),
                              App(Var "f", Number 5)))
            Expect.equal (evalExpr expr) 15 ""
        }
    ]
```

---

## 테스트 추가 워크플로우

### 새 기능 구현 시

```bash
# 1. fslit 테스트 먼저 (실패 확인)
mkdir -p tests/<category>
# tests/<category>/01-feature.flt 작성
make -C tests <category>  # FAIL 확인

# 2. 기능 구현

# 3. fslit 테스트 통과 확인
make -C tests <category>  # PASS

# 4. Expecto 단위 테스트 추가
# FunLang.Tests/<Module>Tests.fs 수정
dotnet run --project FunLang.Tests  # PASS

# 5. 전체 회귀 테스트
make -C tests && dotnet run --project FunLang.Tests

# 6. 커밋
git add tests/<category>/ FunLang.Tests/
git commit -m "test: add <feature> tests"
```

---

## 현재 테스트 현황

| 카테고리 | 테스트 수 | Phase | 상태 |
|----------|-----------|-------|------|
| cli | 6 | 2 | ✓ 완료 |
| emit-tokens | 4 | 7 | ✓ 완료 |
| emit-ast | 6 | 7 | ✓ 완료 |
| file-input | 5 | 7 | ✓ 완료 |
| variables | 12 | 3 | ✓ 완료 |
| control | 20 | 4 | ✓ 완료 |
| functions | 0 | 5 | 대기 |

**fslit 총 테스트: 53개** (Phase 2, 3, 4, 7 완료)

| 프로젝트 | 테스트 수 | 상태 |
|----------|-----------|------|
| FunLang.Tests | 93 | ✓ 완료 |

**Expecto 테스트 구성:**
- Phase 2 (산술): 18개
- Phase 3 (변수): 15개
- Phase 4 (제어흐름): 30개
- Property Tests: 11개 (FsCheck)
- Lexer Tests: 9개
- 기타: 10개

---

## Makefile 템플릿

```makefile
# tests/Makefile
.PHONY: all test cli emit-tokens emit-ast file-input variables control functions build check clean

all: test

test:
	@cd .. && fslit tests/

cli:
	@cd .. && fslit tests/cli/

emit-tokens:
	@cd .. && fslit tests/emit-tokens/

emit-ast:
	@cd .. && fslit tests/emit-ast/

file-input:
	@cd .. && fslit tests/file-input/

variables:
	@cd .. && fslit tests/variables/

control:
	@cd .. && fslit tests/control/

functions:
	@cd .. && fslit tests/functions/

check: build test

build:
	@cd .. && dotnet build
```

---

## 참고 문서

- `docs/howto/testing-strategies.md` - 테스트 전략 상세
- `docs/howto/setup-expecto-test-project.md` - Expecto 프로젝트 설정
- `docs/howto/write-fscheck-property-tests.md` - FsCheck 속성 테스트
- `docs/howto/write-fslit-file-tests.md` - fslit 파일 테스트
- `tutorial/appendix-01-testing.md` - 테스트 튜토리얼
- `.planning/ROADMAP.md` - Phase별 요구사항
