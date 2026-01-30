# LangTutorial

F# 개발자를 위한 프로그래밍 언어 구현 튜토리얼.

fslex와 fsyacc를 사용하여 인터프리터를 단계별로 구현합니다. 사칙연산부터 시작해 변수, 조건문, 함수까지 확장하며, 각 챕터는 독립적으로 실행 가능한 완전한 예제를 제공합니다.

## 진행 상태

| 챕터 | 내용 | 상태 |
|------|------|------|
| 1 | Foundation & Pipeline | ✓ 완료 |
| 2 | Arithmetic Expressions | ✓ 완료 |
| 3 | Variables & Binding | ✓ 완료 |
| 4 | Control Flow | ✓ 완료 |
| 5 | Functions & Abstraction | ✓ 완료 |
| 6 | Quality & Polish | ○ 예정 |

**현재:** Turing-complete 언어 달성 (Phase 5 완료)

## 빠른 시작

```bash
# 빌드
dotnet build FunLang/FunLang.fsproj

# 산술 연산
dotnet run --project FunLang -- --expr "2 + 3 * 4"
14

# 변수 바인딩
dotnet run --project FunLang -- --expr "let x = 5 in x * 2"
10

# 조건문
dotnet run --project FunLang -- --expr "if 5 > 3 then 10 else 20"
10

# 함수 정의와 호출
dotnet run --project FunLang -- --expr "let f = fun x -> x + 1 in f 5"
6

# 재귀 함수 (팩토리얼)
dotnet run --project FunLang -- --expr "let rec fact n = if n <= 1 then 1 else n * fact (n - 1) in fact 5"
120

# 클로저
dotnet run --project FunLang -- --expr "let x = 10 in let f = fun y -> x + y in f 5"
15

# 테스트
make -C tests              # fslit CLI 테스트 (66개)
dotnet run --project FunLang.Tests  # Expecto 테스트 (129개)
```

## 튜토리얼 구성

| 챕터 | 내용 | 핵심 기능 |
|------|------|-----------|
| 1 | Foundation & Pipeline | 프로젝트 설정, fslex/fsyacc 기초 |
| 2 | Arithmetic Expressions | 사칙연산, 연산자 우선순위, 괄호, 단항 마이너스 |
| 3 | Variables & Binding | let 바인딩, 변수 참조, 스코프, 섀도잉 |
| 4 | Control Flow | if-then-else, Boolean, 비교/논리 연산자, 타입 검사 |
| 5 | Functions & Abstraction | 함수 정의/호출, 재귀, 클로저 |
| 6 | Quality & Polish | 에러 메시지, REPL, 테스트 |

## 디렉토리 구조

```
LangTutorial/
├── FunLang/              # 언어 구현 (F# 프로젝트)
│   ├── Ast.fs            # AST 타입 정의 (Expr, Value, Env)
│   ├── Parser.fsy        # fsyacc 문법
│   ├── Lexer.fsl         # fslex 렉서
│   ├── Eval.fs           # 평가기 (타입 검사, 클로저, 재귀)
│   └── Program.fs        # CLI
├── FunLang.Tests/        # Expecto 단위 테스트 (129개)
├── tests/                # fslit CLI 테스트 (66개)
│   ├── cli/              # 기본 CLI 테스트
│   ├── variables/        # 변수 바인딩 테스트
│   ├── control/          # 제어 흐름 테스트
│   ├── functions/        # 함수 테스트
│   └── ...
├── tutorial/             # 튜토리얼 문서
│   ├── chapter-01-foundation.md
│   ├── chapter-02-arithmetic.md
│   ├── chapter-03-variables.md
│   ├── chapter-04-conditionals.md
│   ├── chapter-05-functions.md
│   └── appendix-01-testing.md
└── docs/howto/           # 개발 지식 문서 (13개)
```

## 기술 스택

- **F#** (.NET 10)
- **FsLexYacc 11.3.0** — 렉서/파서 생성기
- **Expecto** — 단위 테스트
- **FsCheck** — 속성 기반 테스트
- **fslit** — 파일 기반 CLI 테스트

## Git 태그

각 챕터 완료 시점의 코드를 태그로 확인할 수 있습니다:

```bash
git checkout tutorial-v1.0  # Chapter 1: Foundation
git checkout tutorial-v2.0  # Chapter 2: Arithmetic
git checkout tutorial-v3    # Chapter 3: Variables
git checkout tutorial-v4.0  # Chapter 4: Control Flow
git checkout tutorial-v5.0  # Chapter 5: Functions (예정)
```

## 문서

- **tutorial/** — 단계별 튜토리얼 (5 chapters + 1 appendix)
- **docs/howto/** — 개발 지식 문서 (13개)
  - fsyacc 파서 작성, 연산자 우선순위
  - fslex 렉서 작성, 키워드 우선순위
  - 단항 마이너스 구현
  - 함수 적용 vs 뺄셈 파서 해결
  - Expecto/FsCheck 테스트 설정
  - Value 타입 진화 시 테스트 적응
  - 등

## 라이선스

MIT
