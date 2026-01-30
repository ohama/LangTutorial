# LangTutorial

F# 개발자를 위한 프로그래밍 언어 구현 튜토리얼.

fslex와 fsyacc를 사용하여 인터프리터를 단계별로 구현합니다. 사칙연산부터 시작해 변수, 조건문, 함수까지 확장하며, 각 챕터는 독립적으로 실행 가능한 완전한 예제를 제공합니다.

## 진행 상태

| 챕터 | 내용 | 상태 |
|------|------|------|
| 1 | Foundation & Pipeline | ✓ 완료 |
| 2 | Arithmetic Expressions | ✓ 완료 |
| 3 | Variables & Binding | ○ 예정 |
| 4 | Control Flow | ○ 예정 |
| 5 | Functions & Abstraction | ○ 예정 |
| 6 | Quality & Polish | ○ 예정 |

## 빠른 시작

```bash
# 빌드
dotnet build FunLang/FunLang.fsproj

# 실행
dotnet run --project FunLang -- --expr "2 + 3 * 4"
14

dotnet run --project FunLang -- --expr "(2 + 3) * 4"
20

dotnet run --project FunLang -- --expr "-5 + 3"
-2

# 테스트
dotnet run --project FunLang.Tests
```

## 튜토리얼 구성

| 챕터 | 내용 | 핵심 기능 |
|------|------|-----------|
| 1 | Foundation & Pipeline | 프로젝트 설정, fslex/fsyacc 기초 |
| 2 | Arithmetic Expressions | 사칙연산, 연산자 우선순위, 괄호, 단항 마이너스 |
| 3 | Variables & Binding | let 바인딩, 변수 참조, 스코프 |
| 4 | Control Flow | if-then-else, Boolean, 비교/논리 연산자 |
| 5 | Functions & Abstraction | 함수 정의/호출, 재귀, 클로저 |
| 6 | Quality & Polish | 에러 메시지, REPL, 테스트 |

## 디렉토리 구조

```
LangTutorial/
├── FunLang/              # 언어 구현 (F# 프로젝트)
│   ├── Ast.fs            # AST 타입 정의
│   ├── Parser.fsy        # fsyacc 문법
│   ├── Lexer.fsl         # fslex 렉서
│   ├── Eval.fs           # 평가기
│   └── Program.fs        # CLI
├── FunLang.Tests/        # Expecto 테스트
├── tutorial/             # 튜토리얼 문서
│   ├── chapter-01-foundation.md
│   ├── chapter-02-arithmetic.md
│   └── pdf/              # PDF 버전
└── docs/howto/           # 개발 지식 문서
    ├── write-fsyacc-parser.md
    ├── write-fslex-lexer.md
    ├── fsyacc-precedence-without-declarations.md
    └── ...
```

## 기술 스택

- **F#** (.NET 10)
- **FsLexYacc 11.3.0** — 렉서/파서 생성기
- **Expecto** — 단위 테스트
- **FsCheck** — 속성 기반 테스트 (예정)

## Git 태그

각 챕터 완료 시점의 코드를 태그로 확인할 수 있습니다:

```bash
git checkout tutorial-v1.0  # Chapter 1: Foundation
git checkout tutorial-v2.0  # Chapter 2: Arithmetic
```

## 문서

- **tutorial/** — 단계별 튜토리얼 (Markdown/PDF)
- **docs/howto/** — 개발 지식 문서 (7개)
  - fsyacc 파서 작성
  - fslex 렉서 작성
  - 연산자 우선순위 처리
  - 단항 마이너스 구현
  - Expecto 테스트 설정
  - 등

## 라이선스

MIT
