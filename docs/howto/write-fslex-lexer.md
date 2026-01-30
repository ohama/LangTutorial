---
created: 2026-01-30
description: fslex로 F# 렉서 작성하기 - 개념, 구조, 예제
---

# fslex: What, How, Examples

fslex는 F#용 렉서 생성기다. `.fsl` 파일에서 토큰화 규칙을 정의하면 F# 소스 코드를 생성한다.

## What: fslex란?

**fslex**는 FsLexYacc 패키지의 렉서 생성기 도구다.

- **입력**: `.fsl` 파일 (렉서 명세)
- **출력**: `.fs` 파일 (F# 렉서 코드)
- **역할**: 문자열을 토큰 스트림으로 변환

```
소스 코드 → [fslex 생성 Lexer] → 토큰 스트림 → [Parser] → AST
```

## How: .fsl 파일 구조

`.fsl` 파일은 3개 섹션으로 구성된다:

```fsharp
{
  // 1. Header: F# 코드 (open 문, 헬퍼 함수)
}

// 2. Definitions: 문자 클래스 정의
let name = pattern

// 3. Rules: 토큰화 규칙
rule rulename = parse
  | pattern { action }
```

### Section 1: Header

중괄호 `{ }` 안에 F# 코드를 작성한다.

```fsharp
{
open System
open FSharp.Text.Lexing
open Parser  // Parser 토큰 타입 참조 (중요!)

let lexeme (lexbuf: LexBuffer<_>) =
    LexBuffer<_>.LexemeString lexbuf
}
```

**필수 요소:**
- `open Parser`: Parser에서 생성된 토큰 타입 사용
- `lexeme` 헬퍼: 현재 매치된 문자열 추출

### Section 2: Definitions

자주 사용하는 패턴에 이름을 붙인다.

```fsharp
let digit = ['0'-'9']
let letter = ['a'-'z' 'A'-'Z']
let whitespace = [' ' '\t']
let newline = ('\n' | '\r' '\n')
let identifier = letter (letter | digit | '_')*
```

**문자 클래스 문법:**
- `['a'-'z']`: a부터 z까지
- `['a'-'z' 'A'-'Z']`: 소문자 또는 대문자
- `[' ' '\t']`: 공백 또는 탭

**패턴 연산자:**
- `*`: 0회 이상 반복
- `+`: 1회 이상 반복
- `?`: 0회 또는 1회
- `|`: 선택 (or)
- `( )`: 그룹화

### Section 3: Rules

`rule` 키워드로 토큰화 규칙을 정의한다.

```fsharp
rule tokenize = parse
  | pattern1 { action1 }
  | pattern2 { action2 }
```

**Action에서 사용 가능한 것:**
- `lexbuf`: 현재 렉서 버퍼
- `lexeme lexbuf`: 매치된 문자열
- 토큰 생성자: `NUMBER`, `EOF` 등 (Parser에서 정의)
- 재귀 호출: `tokenize lexbuf` (토큰 건너뛰기)

## Examples

### 기본 정수 렉서

```fsharp
{
open System
open FSharp.Text.Lexing
open Parser

let lexeme (lexbuf: LexBuffer<_>) =
    LexBuffer<_>.LexemeString lexbuf
}

let digit = ['0'-'9']
let whitespace = [' ' '\t']
let newline = ('\n' | '\r' '\n')

rule tokenize = parse
  | whitespace+   { tokenize lexbuf }                        // 공백 무시
  | newline       { tokenize lexbuf }                        // 줄바꿈 무시
  | digit+        { NUMBER (Int32.Parse(lexeme lexbuf)) }    // 정수
  | eof           { EOF }                                    // 파일 끝
```

### 사칙연산 렉서

```fsharp
rule tokenize = parse
  | whitespace+   { tokenize lexbuf }
  | digit+        { NUMBER (Int32.Parse(lexeme lexbuf)) }
  | '+'           { PLUS }
  | '-'           { MINUS }
  | '*'           { TIMES }
  | '/'           { DIVIDE }
  | '('           { LPAREN }
  | ')'           { RPAREN }
  | eof           { EOF }
```

### 식별자와 키워드 렉서

```fsharp
let letter = ['a'-'z' 'A'-'Z']
let digit = ['0'-'9']
let identifier = letter (letter | digit | '_')*

rule tokenize = parse
  | "let"         { LET }           // 키워드 먼저 (우선순위)
  | "if"          { IF }
  | "then"        { THEN }
  | "else"        { ELSE }
  | identifier    { IDENT (lexeme lexbuf) }  // 식별자
  | digit+        { NUMBER (Int32.Parse(lexeme lexbuf)) }
  | eof           { EOF }
```

**주의**: 키워드는 식별자보다 먼저 정의해야 한다. fslex는 위에서 아래로 매칭한다.

## 빌드 설정

`.fsproj`에서 FsLex 설정:

```xml
<ItemGroup>
  <!-- Parser 먼저! (Lexer가 Parser 토큰 참조) -->
  <FsYacc Include="Parser.fsy">
    <OtherFlags>--module Parser</OtherFlags>
  </FsYacc>

  <FsLex Include="Lexer.fsl">
    <OtherFlags>--module Lexer --unicode</OtherFlags>
  </FsLex>
</ItemGroup>
```

**옵션:**
- `--module Lexer`: 생성 모듈 이름
- `--unicode`: 유니코드 지원

## 체크리스트

- [ ] Header에 `open Parser` 있는가?
- [ ] `lexeme` 헬퍼 함수 정의했는가?
- [ ] 키워드가 식별자보다 먼저 정의되어 있는가?
- [ ] `.fsproj`에서 FsYacc가 FsLex보다 위에 있는가?
- [ ] `eof` 규칙이 있는가?

## 관련 문서

- [setup-fslexyacc-build-order](setup-fslexyacc-build-order.md) - 빌드 순서 설정
- [FsLexYacc 공식 문서](https://fsprojects.github.io/FsLexYacc/)
