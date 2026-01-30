---
created: 2026-01-30
description: FunLang 테스트 전략 - fslit, Expecto, FsCheck 활용 가이드
---

# FunLang 테스트 전략

세 가지 테스트 도구를 조합하여 인터프리터의 정확성을 검증한다.

## 테스트 피라미드

```
                    ┌─────────────┐
                    │   fslit     │  E2E / CLI 검증
                    │ (file-based)│  "사용자 관점"
                    └──────┬──────┘
                    ┌──────┴──────┐
                    │   Expecto   │  단위 테스트
                    │   (unit)    │  "개발자 관점"
                    └──────┬──────┘
              ┌────────────┴────────────┐
              │        FsCheck          │  속성 기반 테스트
              │  (property-based)       │  "수학적 불변식"
              └─────────────────────────┘
```

| 도구 | 목적 | 속도 | 커버리지 |
|------|------|------|----------|
| fslit | CLI 통합 테스트, 회귀 방지 | 느림 | 좁음 (구체적 케이스) |
| Expecto | 모듈별 단위 테스트 | 빠름 | 중간 |
| FsCheck | 엣지 케이스 자동 발견 | 빠름 | 넓음 (무작위 생성) |

---

## 1. File-Based Testing (fslit)

### 용도

- **CLI 동작 검증**: `--expr`, `--emit-tokens`, `--emit-ast` 옵션이 올바르게 작동하는지
- **회귀 테스트**: 새 기능 추가 시 기존 동작이 깨지지 않는지
- **문서화**: 테스트 자체가 사용 예제가 됨

### 현재 구조

```
tests/
├── Makefile
├── cli/              # 기본 평가 테스트
│   ├── 01-simple-add.flt
│   ├── 02-precedence.flt
│   └── ...
├── emit-tokens/      # 토큰 출력 테스트
├── emit-ast/         # AST 출력 테스트
└── file-input/       # 파일 입력 테스트
```

### 테스트 파일 형식

```flt
// 테스트 설명 (주석)
// --- Command: <실행할 명령>
// --- Input:        (선택사항)
<입력 내용>
// --- Output:
<기대 출력>
```

**변수:**
- `%input` - Input 섹션 내용을 임시 파일로 생성, 경로 대체
- `%s` - 테스트 파일 경로
- `%S` - 테스트 파일 디렉토리

### 새 테스트 추가

**Phase 3 (Variables) 예시:**

```bash
# tests/cli/10-let-binding.flt
mkdir -p tests/variables
```

```flt
// Test: Simple let binding
// --- Command: dotnet run --project FunLang -- --expr "let x = 5 in x + 1"
// --- Output:
6
```

**Phase 4 (Control Flow) 예시:**

```flt
// Test: If-then-else true branch
// --- Command: dotnet run --project FunLang -- --expr "if true then 1 else 2"
// --- Output:
1
```

### 실행

```bash
# 전체 테스트
make -C tests

# 카테고리별
make -C tests cli
make -C tests emit-tokens
make -C tests emit-ast
make -C tests file-input

# 빌드 후 테스트
make -C tests check

# 단일 파일
cd /path/to/project && fslit tests/cli/01-simple-add.flt
```

### 확장 시 주의사항

1. **한 파일 = 한 테스트**: fslit은 파일당 하나의 테스트만 지원
2. **번호 접두사**: 실행 순서 보장을 위해 `01-`, `02-` 형식 사용
3. **명확한 이름**: 테스트 목적이 파일명에 드러나게
4. **Output 검증**: 실제 CLI 출력과 정확히 일치해야 함 (공백, 줄바꿈 주의)

---

## 2. Expecto (단위 테스트)

### 용도

- **모듈별 테스트**: Lexer, Parser, Eval 각각 독립 테스트
- **내부 함수 테스트**: CLI를 거치지 않고 직접 함수 호출
- **빠른 피드백**: 개발 중 빠른 검증

### 프로젝트 설정

```bash
# 테스트 프로젝트 생성
dotnet new console -lang F# -n FunLang.Tests -f net10.0

# 패키지 추가
cd FunLang.Tests
dotnet add package Expecto
dotnet add package FsCheck           # 속성 테스트용
dotnet add package Expecto.FsCheck   # 통합용

# 메인 프로젝트 참조
dotnet add reference ../FunLang/FunLang.fsproj
```

### 테스트 코드 예시

**FunLang.Tests/Program.fs:**

```fsharp
module FunLang.Tests

open Expecto
open FSharp.Text.Lexing

// === Lexer Tests ===
[<Tests>]
let lexerTests =
    testList "Lexer" [
        test "tokenizes number" {
            let lexbuf = LexBuffer<char>.FromString "42"
            let token = Lexer.tokenize lexbuf
            Expect.equal token (Parser.NUMBER 42) "should be NUMBER(42)"
        }

        test "tokenizes operators" {
            let input = "+-*/"
            let lexbuf = LexBuffer<char>.FromString input
            let tokens = [
                Lexer.tokenize lexbuf
                Lexer.tokenize lexbuf
                Lexer.tokenize lexbuf
                Lexer.tokenize lexbuf
            ]
            Expect.equal tokens [Parser.PLUS; Parser.MINUS; Parser.STAR; Parser.SLASH] ""
        }
    ]

// === Parser Tests ===
[<Tests>]
let parserTests =
    testList "Parser" [
        test "parses addition" {
            let lexbuf = LexBuffer<char>.FromString "2 + 3"
            let ast = Parser.start Lexer.tokenize lexbuf
            Expect.equal ast (Ast.Add(Ast.Number 2, Ast.Number 3)) ""
        }

        test "respects precedence" {
            let lexbuf = LexBuffer<char>.FromString "2 + 3 * 4"
            let ast = Parser.start Lexer.tokenize lexbuf
            let expected = Ast.Add(Ast.Number 2, Ast.Multiply(Ast.Number 3, Ast.Number 4))
            Expect.equal ast expected "multiplication binds tighter"
        }
    ]

// === Evaluator Tests ===
[<Tests>]
let evalTests =
    testList "Eval" [
        test "evaluates number" {
            Expect.equal (Eval.eval (Ast.Number 5)) 5 ""
        }

        test "evaluates addition" {
            let expr = Ast.Add(Ast.Number 2, Ast.Number 3)
            Expect.equal (Eval.eval expr) 5 ""
        }

        test "evaluates complex expression" {
            // (2 + 3) * 4 = 20
            let expr = Ast.Multiply(Ast.Add(Ast.Number 2, Ast.Number 3), Ast.Number 4)
            Expect.equal (Eval.eval expr) 20 ""
        }
    ]

[<EntryPoint>]
let main argv =
    runTestsWithCLIArgs [] argv <| testList "All" [
        lexerTests
        parserTests
        evalTests
    ]
```

### 실행

```bash
dotnet run --project FunLang.Tests

# 특정 테스트만
dotnet run --project FunLang.Tests -- --filter "Lexer"

# 병렬 실행 (기본값)
dotnet run --project FunLang.Tests -- --parallel
```

### 유용한 Expect 함수

```fsharp
Expect.equal actual expected "message"      // 동등성
Expect.isTrue condition "message"           // 불린
Expect.isSome option "message"              // Option
Expect.throws<ExnType> (fun () -> ...) ""   // 예외
Expect.containsAll actual expected ""       // 컬렉션 포함
```

---

## 3. FsCheck (속성 기반 테스트)

### 용도

- **불변식 검증**: "모든 입력에 대해 이 성질이 유지되어야 한다"
- **엣지 케이스 발견**: 무작위 생성으로 예상치 못한 버그 발견
- **수학적 성질 테스트**: 결합법칙, 교환법칙 등

### Expecto와 통합

```fsharp
open Expecto
open FsCheck

[<Tests>]
let propertyTests =
    testList "Properties" [
        // 숫자 리터럴은 그대로 평가됨
        testProperty "number evaluates to itself" <| fun (n: int) ->
            Eval.eval (Ast.Number n) = n

        // 덧셈은 교환법칙 성립
        testProperty "addition is commutative" <| fun (a: int) (b: int) ->
            let left = Eval.eval (Ast.Add(Ast.Number a, Ast.Number b))
            let right = Eval.eval (Ast.Add(Ast.Number b, Ast.Number a))
            left = right

        // 곱셈은 결합법칙 성립
        testProperty "multiplication is associative" <| fun (a: int) (b: int) (c: int) ->
            let left = Eval.eval (Ast.Multiply(Ast.Multiply(Ast.Number a, Ast.Number b), Ast.Number c))
            let right = Eval.eval (Ast.Multiply(Ast.Number a, Ast.Multiply(Ast.Number b, Ast.Number c)))
            left = right

        // 0을 더해도 값이 변하지 않음 (항등원)
        testProperty "zero is additive identity" <| fun (n: int) ->
            Eval.eval (Ast.Add(Ast.Number n, Ast.Number 0)) = n

        // 1을 곱해도 값이 변하지 않음 (항등원)
        testProperty "one is multiplicative identity" <| fun (n: int) ->
            Eval.eval (Ast.Multiply(Ast.Number n, Ast.Number 1)) = n
    ]
```

### 커스텀 Generator

복잡한 AST를 자동 생성:

```fsharp
open FsCheck

// AST Generator
let exprGen =
    let rec expr' depth =
        if depth <= 0 then
            Gen.map Ast.Number Arb.generate<int>
        else
            let smaller = expr' (depth - 1)
            Gen.oneof [
                Gen.map Ast.Number Arb.generate<int>
                Gen.map2 (fun a b -> Ast.Add(a, b)) smaller smaller
                Gen.map2 (fun a b -> Ast.Subtract(a, b)) smaller smaller
                Gen.map2 (fun a b -> Ast.Multiply(a, b)) smaller smaller
                Gen.map Ast.Negate smaller
            ]
    Gen.sized (fun s -> expr' (min s 5))

type ExprArbitrary =
    static member Expr() = Arb.fromGen exprGen

// 등록
Arb.register<ExprArbitrary>() |> ignore

[<Tests>]
let astPropertyTests =
    testList "AST Properties" [
        testProperty "eval never throws on valid AST" <| fun (expr: Ast.Expr) ->
            try
                Eval.eval expr |> ignore
                true
            with
            | :? System.DivideByZeroException -> true  // 0으로 나누기는 허용
            | _ -> false
    ]
```

---

## 테스트 실행 시점

### 개발 중

```bash
# 빠른 피드백 - Expecto 단위 테스트
dotnet run --project FunLang.Tests

# 변경된 기능만 - fslit 특정 카테고리
make -C tests cli
```

### 커밋 전

```bash
# 전체 검증
make -C tests check   # 빌드 + fslit
dotnet run --project FunLang.Tests
```

### CI/CD

```yaml
# .github/workflows/test.yml 예시
- name: Build
  run: dotnet build

- name: Unit Tests
  run: dotnet run --project FunLang.Tests

- name: Integration Tests
  run: make -C tests
```

---

## Phase별 테스트 확장 가이드

### Phase 3: Variables & Binding

**fslit (tests/variables/):**
```flt
// tests/variables/01-let-simple.flt
// --- Command: dotnet run --project FunLang -- --expr "let x = 5 in x"
// --- Output:
5

// tests/variables/02-let-expr.flt
// --- Command: dotnet run --project FunLang -- --expr "let x = 2 + 3 in x * 2"
// --- Output:
10
```

**Expecto:**
```fsharp
test "let binding creates scope" {
    let expr = Ast.Let("x", Ast.Number 5, Ast.Var "x")
    Expect.equal (Eval.eval Map.empty expr) 5 ""
}
```

### Phase 4: Control Flow

**fslit (tests/control/):**
```flt
// tests/control/01-if-true.flt
// --- Command: dotnet run --project FunLang -- --expr "if true then 1 else 2"
// --- Output:
1

// tests/control/02-comparison.flt
// --- Command: dotnet run --project FunLang -- --expr "if 5 > 3 then 10 else 20"
// --- Output:
10
```

**FsCheck:**
```fsharp
testProperty "if-then-else returns one branch" <| fun (cond: bool) (a: int) (b: int) ->
    let expr = Ast.If(Ast.Bool cond, Ast.Number a, Ast.Number b)
    let result = Eval.eval Map.empty expr
    result = (if cond then a else b)
```

### Phase 5: Functions

**fslit (tests/functions/):**
```flt
// tests/functions/01-simple.flt
// --- Command: dotnet run --project FunLang -- --expr "let f x = x + 1 in f 5"
// --- Output:
6

// tests/functions/02-recursive.flt
// --- Command: dotnet run --project FunLang -- --expr "let rec fact n = if n <= 1 then 1 else n * fact (n - 1) in fact 5"
// --- Output:
120
```

---

## 체크리스트

### 새 기능 추가 시

- [ ] fslit 테스트 추가 (CLI 동작 검증)
- [ ] Expecto 단위 테스트 추가 (모듈 검증)
- [ ] FsCheck 속성 테스트 검토 (불변식 있으면 추가)
- [ ] 기존 테스트 모두 통과 확인

### 버그 수정 시

- [ ] 버그 재현 테스트 먼저 작성
- [ ] 수정 후 테스트 통과 확인
- [ ] 회귀 방지용 테스트로 유지

---

## 관련 문서

- [setup-expecto-test-project](setup-expecto-test-project.md) - Expecto 프로젝트 설정
- [fslit GitHub](https://github.com/ohama/fslit) - fslit 공식 문서
- [FsCheck Documentation](https://fscheck.github.io/FsCheck/) - FsCheck 공식 문서
- [Expecto GitHub](https://github.com/haf/expecto) - Expecto 공식 문서
