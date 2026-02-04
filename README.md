# LangTutorial

F# 개발자를 위한 프로그래밍 언어 구현 튜토리얼.

fslex와 fsyacc를 사용하여 인터프리터를 단계별로 구현합니다. 사칙연산부터 시작해 변수, 조건문, 함수, 데이터 구조, 타입 시스템까지 확장하며, 각 챕터는 독립적으로 실행 가능한 완전한 예제를 제공합니다.

**온라인 튜토리얼:** https://ohama.github.io/LangTutorial/

## 진행 상태

| 챕터 | 내용 | 상태 |
|------|------|------|
| 1 | Foundation & Pipeline | ✓ 완료 |
| 2 | Arithmetic Expressions | ✓ 완료 |
| 3 | Variables & Binding | ✓ 완료 |
| 4 | Control Flow | ✓ 완료 |
| 5 | Functions & Abstraction | ✓ 완료 |
| 6 | Tuples | ✓ 완료 |
| 7 | Lists | ✓ 완료 |
| 8 | Pattern Matching | ✓ 완료 |
| 9 | Prelude (Standard Library) | ✓ 완료 |
| 10 | Type System (Hindley-Milner) | ✓ 완료 |
| 11 | Type Annotations (v6.0) | ✓ 완료 |

**현재:** v6.0 Bidirectional Type System 완료 — ML 스타일 타입 주석, synth/check 모드, 타입 검증

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

# 튜플
dotnet run --project FunLang -- --expr "let pair = (1, 2) in let (x, y) = pair in x + y"
3

# 리스트와 cons 연산
dotnet run --project FunLang -- --expr "0 :: [1, 2, 3]"
[0, 1, 2, 3]

# 패턴 매칭
dotnet run --project FunLang -- --expr "match [1, 2, 3] with | [] -> 0 | h :: t -> h"
1

# 표준 라이브러리 함수
dotnet run --project FunLang -- --expr "map (fun x -> x * 2) [1, 2, 3]"
[2, 4, 6]

dotnet run --project FunLang -- --expr "fold (fun a -> fun b -> a + b) 0 [1, 2, 3, 4, 5]"
15

# 타입 추론
dotnet run --project FunLang -- --emit-type --expr "fun x -> x"
'm -> 'm

dotnet run --project FunLang -- --emit-type --expr "map"
('m -> 'n) -> 'm list -> 'n list

dotnet run --project FunLang -- --emit-type --expr "let id = fun x -> x in (id 5, id true)"
int * bool

# 타입 주석 (v6.0)
dotnet run --project FunLang -- --expr "(42 : int)"
42

dotnet run --project FunLang -- --expr "fun (x: int) -> x + 1"
<function>

dotnet run --project FunLang -- --emit-type --expr "fun (x: int) -> x + 1"
int -> int

# REPL 모드
dotnet run --project FunLang -- -i

# 테스트
make -C tests              # fslit CLI 테스트 (200개)
dotnet run --project FunLang.Tests  # Expecto 테스트 (419개)
```

## 튜토리얼 구성

| 챕터 | 내용 | 핵심 기능 |
|------|------|-----------|
| 1 | Foundation & Pipeline | 프로젝트 설정, fslex/fsyacc 기초 |
| 2 | Arithmetic Expressions | 사칙연산, 연산자 우선순위, 괄호, 단항 마이너스 |
| 3 | Variables & Binding | let 바인딩, 변수 참조, 스코프, 섀도잉 |
| 4 | Control Flow | if-then-else, Boolean, 비교/논리 연산자, 타입 검사 |
| 5 | Functions & Abstraction | 함수 정의/호출, 재귀, 클로저 |
| 6 | Tuples | 튜플 리터럴, 튜플 패턴 분해, 중첩 튜플 |
| 7 | Lists | 빈 리스트, 리스트 리터럴, cons 연산자 |
| 8 | Pattern Matching | match 표현식, 패턴 타입, first-match 의미론 |
| 9 | Prelude | 자체 호스팅 표준 라이브러리, map/filter/fold 등 |
| 10 | Type System | Hindley-Milner 타입 추론, Let-polymorphism |
| 11 | Type Annotations | ML 스타일 타입 주석, Bidirectional Type Checking |

## 예제 (examples/)

FunLang의 기능을 보여주는 실용적인 예제 프로그램들:

| 파일 | 내용 | 핵심 개념 |
|------|------|-----------|
| `01-quicksort.fun` | Quicksort 정렬 | 재귀, 리스트 분할, filter |
| `02-mergesort.fun` | Merge sort 정렬 | 분할 정복, 리스트 병합 |
| `03-insertion-sort.fun` | Insertion sort 정렬 | 재귀적 삽입 |
| `04-binary-search.fun` | 이진 탐색 | 정렬된 리스트 검색, 인덱스 기반 |
| `05-fibonacci.fun` | 피보나치 수열 | naive vs tail-recursive 비교 |
| `06-prime-sieve.fun` | 에라토스테네스의 체 | 소수 생성, filter |
| `07-list-comprehension.fun` | 리스트 패턴 | zip, cartesian product, fold |
| `08-recursion.fun` | 재귀 함수 모음 | factorial, fibonacci, gcd |

```bash
# 예제 실행
dotnet run --project FunLang -- examples/01-quicksort.fun
# 출력: ([1, 2, 3, 4, 5], [1, 1, 2, 3, 5, 8, 13], [-3, 0, 1, 2, 5, 9])

dotnet run --project FunLang -- examples/05-fibonacci.fun
# 출력: (55, 55, [0, 1, 1, 2, 3, 5, 8, 13, 21, 34])

dotnet run --project FunLang -- examples/06-prime-sieve.fun
# 출력: [2, 3, 5, 7, 11, 13, 17, 19, 23, 29]
```

## 디렉토리 구조

```
LangTutorial/
├── FunLang/              # 언어 구현 (F# 프로젝트)
│   ├── Ast.fs            # AST 타입 정의 (Expr, Pattern, Value, Env, TypeExpr)
│   ├── Parser.fsy        # fsyacc 문법
│   ├── Lexer.fsl         # fslex 렉서
│   ├── Eval.fs           # 평가기 (타입 검사, 클로저, 재귀, 패턴 매칭)
│   ├── Prelude.fs        # 표준 라이브러리 로더
│   ├── Type.fs           # 타입 AST, Substitution, Scheme
│   ├── Unify.fs          # 단일화 알고리즘, Occurs Check
│   ├── Infer.fs          # Algorithm W 타입 추론
│   ├── Elaborate.fs      # TypeExpr → Type 변환
│   ├── Bidir.fs          # Bidirectional 타입 체킹 (synth/check)
│   ├── TypeCheck.fs      # Prelude 타입, typecheck 함수
│   └── Program.fs        # CLI
├── Prelude.fun           # 표준 라이브러리 (FunLang 소스)
├── examples/             # 실용적인 FunLang 예제 (8개)
│   ├── 01-quicksort.fun
│   ├── 02-mergesort.fun
│   ├── 03-insertion-sort.fun
│   ├── 04-binary-search.fun
│   ├── 05-fibonacci.fun
│   ├── 06-prime-sieve.fun
│   ├── 07-list-comprehension.fun
│   └── 08-recursion.fun
├── FunLang.Tests/        # Expecto 단위 테스트 (419개)
├── tests/                # fslit CLI 테스트 (200개)
│   ├── cli/              # 기본 CLI 테스트
│   ├── variables/        # 변수 바인딩 테스트
│   ├── control/          # 제어 흐름 테스트
│   ├── functions/        # 함수 테스트
│   ├── tuples/           # 튜플 테스트
│   ├── lists/            # 리스트 테스트
│   ├── pattern/          # 패턴 매칭 테스트
│   ├── prelude/          # 표준 라이브러리 테스트
│   ├── type-inference/   # 타입 추론 테스트
│   ├── type-errors/      # 타입 에러 테스트
│   └── ...
├── tutorial/             # 튜토리얼 문서
│   ├── chapter-01-foundation.md
│   ├── chapter-02-arithmetic.md
│   ├── chapter-03-variables.md
│   ├── chapter-04-conditionals.md
│   ├── chapter-05-functions.md
│   ├── chapter-06-tuples.md
│   ├── chapter-07-lists.md
│   ├── chapter-08-pattern-matching.md
│   ├── chapter-09-prelude.md
│   ├── chapter-10-type-system.md
│   └── appendix-01-testing.md
├── youtube/              # YouTube 대본 (10 episodes + 1 appendix)
├── book/                # mdBook 소스
│   └── src/             # 튜토리얼 마크다운
├── docs/                # mdBook 출력 (GitHub Pages)
├── docs.backup/         # 기존 문서
│   ├── grammar.md       # 언어 문법 명세 (BNF)
│   └── howto/           # 개발 지식 문서 (24개)
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
# 튜토리얼 챕터별 태그
git checkout tutorial-v1.0   # Chapter 1: Foundation
git checkout tutorial-v2.0   # Chapter 2: Arithmetic
git checkout tutorial-v3     # Chapter 3: Variables
git checkout tutorial-v4.0   # Chapter 4: Control Flow
git checkout tutorial-v5.0   # Chapter 5: Functions
git checkout tutorial-v12.0  # Chapter 11: Type Annotations (v6.0)

# 마일스톤 태그
git checkout milestone-v3.0  # v3.0: Tuples, Lists, Pattern Matching, Prelude
git checkout milestone-v4.0  # v4.0: Type System (Hindley-Milner)
git checkout v6.0            # v6.0: Bidirectional Type System
```

## 문서

- **[온라인 튜토리얼](https://ohama.github.io/LangTutorial/)** — mdBook 기반 웹 문서
- **[docs.backup/grammar.md](docs.backup/grammar.md)** — FunLang 문법 명세 (BNF, 타입 주석 포함)
- **[examples/](examples/)** — 실용적인 FunLang 예제 (정렬, 검색, 피보나치 등)
- **tutorial/** — 단계별 튜토리얼 (11 chapters + 1 appendix)
- **youtube/** — YouTube 대본 (10 episodes + 1 appendix)
- **docs.backup/howto/** — 개발 지식 문서 (24개)
  - fsyacc 파서 작성, 연산자 우선순위
  - fslex 렉서 작성, 키워드 우선순위
  - 단항 마이너스 구현
  - 함수 적용 vs 뺄셈 파서 해결
  - 패턴 매칭 구현
  - 자체 호스팅 표준 라이브러리
  - Hindley-Milner 타입 추론
  - Let-polymorphism 구현
  - Bidirectional 타입 체킹
  - Expecto/FsCheck 테스트 설정
  - 등

## 라이선스

MIT
