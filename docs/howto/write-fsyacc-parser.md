---
created: 2026-01-30
description: fsyacc로 F# 파서 작성하기 - 개념, 구조, 예제
---

# fsyacc: What, How, Examples

fsyacc는 F#용 파서 생성기다. `.fsy` 파일에서 문법 규칙을 정의하면 F# 소스 코드를 생성한다.

## What: fsyacc란?

**fsyacc**는 FsLexYacc 패키지의 파서 생성기 도구다.

- **입력**: `.fsy` 파일 (문법 명세)
- **출력**: `.fs`, `.fsi` 파일 (F# 파서 코드 + 토큰 타입)
- **역할**: 토큰 스트림을 AST로 변환

```
소스 코드 → [Lexer] → 토큰 스트림 → [fsyacc 생성 Parser] → AST
```

**핵심**: fsyacc가 토큰 타입을 생성하므로 Lexer보다 먼저 빌드해야 한다.

## How: .fsy 파일 구조

`.fsy` 파일은 4개 섹션으로 구성된다:

```
%{
  // 1. Header: F# 코드 (open 문)
%}

// 2. Declarations: 토큰, 시작 심볼, 타입
%token <type> TOKEN_NAME
%start start
%type <return_type> start

%%

// 3. Rules: 문법 규칙
rule_name:
  | pattern { action }

%%

// 4. Footer (선택): 추가 F# 코드
```

### Section 1: Header

`%{` 와 `%}` 사이에 F# 코드를 작성한다.

```fsharp
%{
open Ast  // AST 타입 사용
%}
```

### Section 2: Declarations

토큰과 시작 심볼을 선언한다.

```fsharp
// 토큰 선언
%token <int> NUMBER        // 값을 가지는 토큰
%token PLUS MINUS          // 값 없는 토큰
%token LPAREN RPAREN
%token EOF

// 시작 심볼과 반환 타입
%start start
%type <Ast.Expr> start

// 연산자 우선순위 (낮은 것부터)
%left PLUS MINUS           // 좌결합
%left TIMES DIVIDE         // 높은 우선순위
%nonassoc UMINUS           // 단항 마이너스
```

**우선순위 규칙:**
- 아래에 선언할수록 우선순위 높음
- `%left`: 좌결합 (2+3+4 → (2+3)+4)
- `%right`: 우결합
- `%nonassoc`: 결합 없음

### Section 3: Rules

`%%` 다음에 문법 규칙을 정의한다.

```fsharp
%%

start:
  | expr EOF { $1 }    // $1 = 첫 번째 심볼의 값

expr:
  | NUMBER { Number $1 }
  | expr PLUS expr { Add($1, $3) }
  | LPAREN expr RPAREN { $2 }
```

**Action 문법:**
- `$1`, `$2`, `$3`: 규칙의 n번째 심볼 값
- `{ }` 안에 F# 표현식 작성
- 반환값이 해당 규칙의 값이 됨

## Examples

### 기본 정수 파서

```fsharp
%{
open Ast
%}

%token <int> NUMBER
%token EOF

%start start
%type <Ast.Expr> start

%%

start:
  | NUMBER EOF { Number $1 }
```

### 사칙연산 파서

```fsharp
%{
open Ast
%}

%token <int> NUMBER
%token PLUS MINUS TIMES DIVIDE
%token LPAREN RPAREN
%token EOF

%start start
%type <Ast.Expr> start

// 우선순위: 아래가 높음
%left PLUS MINUS
%left TIMES DIVIDE
%nonassoc UMINUS

%%

start:
  | expr EOF { $1 }

expr:
  | NUMBER                    { Number $1 }
  | expr PLUS expr            { Add($1, $3) }
  | expr MINUS expr           { Sub($1, $3) }
  | expr TIMES expr           { Mul($1, $3) }
  | expr DIVIDE expr          { Div($1, $3) }
  | MINUS expr %prec UMINUS   { Neg $2 }           // 단항 마이너스
  | LPAREN expr RPAREN        { $2 }
```

**`%prec UMINUS` 설명:**
- `MINUS expr`는 이항 MINUS와 토큰이 같음
- `%prec UMINUS`로 별도 우선순위 지정
- UMINUS는 가장 높은 우선순위

### let 바인딩 파서

```fsharp
%token <string> IDENT
%token <int> NUMBER
%token LET EQUALS IN
%token EOF

%start start
%type <Ast.Expr> start

%%

start:
  | expr EOF { $1 }

expr:
  | NUMBER { Number $1 }
  | IDENT { Var $1 }
  | LET IDENT EQUALS expr { Let($2, $4) }
  | LET IDENT EQUALS expr IN expr { LetIn($2, $4, $6) }
```

## 빌드 설정

`.fsproj`에서 FsYacc 설정:

```xml
<ItemGroup>
  <!-- Parser 먼저! (토큰 타입 생성) -->
  <FsYacc Include="Parser.fsy">
    <OtherFlags>--module Parser</OtherFlags>
  </FsYacc>

  <!-- Lexer 다음 (Parser 토큰 참조) -->
  <FsLex Include="Lexer.fsl">
    <OtherFlags>--module Lexer --unicode</OtherFlags>
  </FsLex>

  <!-- 생성된 파일 참조 -->
  <Compile Include="Parser.fsi" />
  <Compile Include="Parser.fs" />
  <Compile Include="Lexer.fs" />
</ItemGroup>
```

**옵션:**
- `--module Parser`: 생성 모듈 이름

## 생성되는 파일

fsyacc는 두 파일을 생성한다:

**Parser.fsi** (인터페이스):
```fsharp
type token =
  | NUMBER of int
  | PLUS
  | MINUS
  | EOF

val start: (FSharp.Text.Lexing.LexBuffer<'a> -> token) -> FSharp.Text.Lexing.LexBuffer<'a> -> Ast.Expr
```

**Parser.fs** (구현):
- 파싱 테이블
- 상태 머신 코드

## 체크리스트

- [ ] Header에 `open Ast` 있는가?
- [ ] 모든 토큰이 `%token`으로 선언되어 있는가?
- [ ] `%start`와 `%type`이 정의되어 있는가?
- [ ] 연산자 우선순위가 올바른 순서인가? (낮은 것 먼저)
- [ ] `.fsproj`에서 FsYacc가 FsLex보다 위에 있는가?
- [ ] `EOF` 토큰을 시작 규칙에서 처리하는가?

## 관련 문서

- [setup-fslexyacc-build-order](setup-fslexyacc-build-order.md) - 빌드 순서 설정
- [write-fslex-lexer](write-fslex-lexer.md) - fslex 렉서 작성법
- [FsLexYacc 공식 문서](https://fsprojects.github.io/FsLexYacc/)
