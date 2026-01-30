# LangTutorial

F# 개발자를 위한 프로그래밍 언어 구현 튜토리얼.

fslex와 fsyacc를 사용하여 인터프리터를 단계별로 구현합니다. 사칙연산부터 시작해 변수, 조건문, 함수까지 확장하며, 각 챕터는 독립적으로 실행 가능한 완전한 예제를 제공합니다.

## 목표

- fslex/fsyacc 기반 렉서/파서 구현
- AST 설계와 인터프리터 패턴 학습
- 점진적 기능 확장 (사칙연산 → 변수 → 조건문 → 함수)

## 기술 스택

- **F#** (.NET 10)
- **fslex** — 렉서 생성기
- **fsyacc** — 파서 생성기
- **Expecto** — 단위 테스트
- **FsCheck** — 속성 기반 테스트
- **fslit** — 파일 기반 테스트

## 튜토리얼 구성

| 챕터 | 내용 | 핵심 기능 |
|------|------|-----------|
| 1 | Foundation & Pipeline | 프로젝트 설정, fslex/fsyacc 기초 |
| 2 | Arithmetic Expressions | 사칙연산, 연산자 우선순위, 괄호 |
| 3 | Variables & Binding | let 바인딩, 변수 참조, 스코프 |
| 4 | Control Flow | if-then-else, Boolean, 비교/논리 연산자 |
| 5 | Functions & Abstraction | 함수 정의/호출, 재귀, 클로저 |
| 6 | Quality & Polish | 에러 메시지, REPL, 테스트 |

## 디렉토리 구조

```
LangTutorial/
├── tutorial/           # 튜토리얼 문서 (Markdown)
│   ├── chapter-01-arithmetic.md
│   ├── chapter-02-variables.md
│   ├── chapter-03-conditionals.md
│   └── chapter-04-functions.md
└── FunLang/            # 언어 구현 코드 (F# 프로젝트)
```

## 예제

튜토리얼 완료 후 FunLang에서 실행 가능한 코드:

```
> 2 + 3 * 4
14

> let x = 10
> x + 5
15

> if x > 5 then "big" else "small"
"big"

> let rec fib n = if n <= 1 then n else fib (n-1) + fib (n-2)
> fib 6
8
```

## 시작하기

```bash
# .NET 10 SDK 필요
dotnet new console -lang F# -o FunLang
cd FunLang
dotnet add package FsLexYacc
```

## 라이선스

MIT
