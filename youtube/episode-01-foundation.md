# Episode 1: 프로그래밍 언어 만들기 - 프로젝트 설정

**예상 길이:** 12-15분
**난이도:** 초급

---

## 썸네일 텍스트

```
F#으로 언어 만들기
Episode 1: 파이프라인 구축
```

---

## 인트로 (0:00 - 1:00)

[화면: 코드 에디터 + 터미널]

안녕하세요! 오늘부터 함께 **프로그래밍 언어**를 만들어 볼 겁니다.

"언어를 만든다고요? 그거 엄청 어려운 거 아닌가요?"

네, 컴파일러 이론 책 펼치면 복잡한 수식이 가득하죠. 하지만 걱정 마세요.
우리는 이론보다 **실습 중심**으로 갈 겁니다.

이 시리즈가 끝나면 여러분은:
- 사칙연산 계산기
- 변수 지원
- 조건문
- 그리고 **함수와 재귀**까지

지원하는 완전한 인터프리터를 갖게 됩니다.

오늘은 첫 번째 에피소드로, **프로젝트 기초 설정**을 해보겠습니다.

---

## 도구 소개 (1:00 - 2:30)

[화면: fslex/fsyacc 로고 또는 텍스트]

우리가 사용할 도구는:

**F#** - 마이크로소프트의 함수형 언어입니다. 패턴 매칭이 강력해서 컴파일러 작성에 딱이에요.

**fslex** - 렉서 생성기. 문자열을 토큰으로 쪼개줍니다.

**fsyacc** - 파서 생성기. 토큰을 AST(추상 구문 트리)로 변환합니다.

이 조합은 C 세계의 lex/yacc, 자바 세계의 ANTLR과 비슷한 역할을 합니다.

[화면: 파이프라인 다이어그램]

```
문자열 → [Lexer] → 토큰들 → [Parser] → AST → [Evaluator] → 결과
```

오늘은 이 파이프라인의 앞부분, Lexer와 Parser까지 연결해 보겠습니다.

---

## 프로젝트 생성 (2:30 - 4:00)

[화면: 터미널]

자, 바로 시작해 봅시다.

```bash
dotnet new console -lang F# -n FunLang -f net10.0
cd FunLang
dotnet add package FsLexYacc --version 11.3.0
```

FunLang이라고 이름 지었습니다. Fun은 함수(Function)의 약자이기도 하고, 재미(Fun)있게 만들자는 의미도 있죠.

[빌드 확인]

```bash
dotnet build
```

기본 프로젝트가 생성됐습니다.

---

## AST 정의 (4:00 - 6:00)

[화면: Ast.fs 편집]

**AST**는 Abstract Syntax Tree, 추상 구문 트리입니다.

코드를 트리 구조로 표현한 거예요. 예를 들어 `2 + 3 * 4`를 파싱하면:

```
    Add
   /   \
  2    Multiply
       /    \
      3      4
```

이런 트리가 됩니다.

F#에서는 Discriminated Union으로 아주 깔끔하게 표현할 수 있어요.

```fsharp
// Ast.fs
module Ast

type Expr =
    | Number of int
```

지금은 숫자만 있습니다. 사칙연산은 다음 에피소드에서 추가할 거예요.

[타이핑하면서]

"왜 한 번에 다 안 넣나요?"

**점진적으로** 만드는 게 중요합니다. 한 번에 많이 만들면 어디서 에러가 났는지 찾기 어려워요.

---

## Parser 작성 (6:00 - 8:30)

[화면: Parser.fsy 편집]

이제 Parser를 만들어 봅시다. `.fsy` 파일을 만들어요.

```fsharp
// Parser.fsy
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

[설명하면서]

- `%{ %}` 안에는 F# 코드를 넣어요
- `%token`으로 토큰 종류를 선언합니다
- `%start`는 시작 규칙
- `%%` 아래가 실제 문법 규칙이에요

`$1`은 첫 번째 심볼의 값을 의미합니다. NUMBER가 42면 $1은 42가 되는 거죠.

---

## Lexer 작성 (8:30 - 10:30)

[화면: Lexer.fsl 편집]

Lexer는 문자열을 토큰으로 쪼개는 역할입니다.

```fsharp
// Lexer.fsl
{
open System
open FSharp.Text.Lexing
open Parser

let lexeme (lexbuf: LexBuffer<_>) =
    LexBuffer<_>.LexemeString lexbuf
}

let digit = ['0'-'9']
let whitespace = [' ' '\t']

rule tokenize = parse
    | whitespace+   { tokenize lexbuf }
    | digit+        { NUMBER (Int32.Parse(lexeme lexbuf)) }
    | eof           { EOF }
```

[설명하면서]

- `whitespace+`는 공백을 건너뜁니다
- `digit+`는 숫자를 NUMBER 토큰으로 변환합니다
- `eof`는 입력 끝을 의미해요

---

## 핵심! 빌드 순서 (10:30 - 12:00)

[화면: 빨간 박스로 강조]

자, 여기서 **가장 중요한 포인트**입니다.

**빌드 순서가 런타임 순서와 다릅니다!**

런타임: Lexer → Parser (문자를 토큰으로, 토큰을 AST로)
빌드: **Parser → Lexer** (Lexer가 Parser의 토큰 타입을 참조하니까)

[화면: fsproj 편집]

```xml
<!-- Parser가 먼저! -->
<FsYacc Include="Parser.fsy">
  <OtherFlags>--module Parser</OtherFlags>
</FsYacc>

<!-- 그 다음 Lexer -->
<FsLex Include="Lexer.fsl">
  <OtherFlags>--module Lexer --unicode</OtherFlags>
</FsLex>
```

이 순서가 틀리면:
```
error FS0039: The namespace or module 'Parser' is not defined
```

이런 에러가 뜹니다. 저도 처음에 이것 때문에 한참 헤맸어요.

---

## 테스트 (12:00 - 13:30)

[화면: Program.fs 편집 + 터미널]

마지막으로 Program.fs에서 연결해 봅시다.

```fsharp
open FSharp.Text.Lexing

let parse (input: string) =
    let lexbuf = LexBuffer<char>.FromString input
    Parser.start Lexer.tokenize lexbuf

[<EntryPoint>]
let main argv =
    let ast = parse "42"
    printfn "AST: %A" ast
    0
```

실행해 보면:

```bash
$ dotnet run
AST: Number 42
```

[축하하는 톤으로]

축하합니다! 우리의 첫 파이프라인이 동작합니다!

숫자 하나 파싱하는 게 대단한 거냐고요? 네, 대단한 겁니다.
**기초가 제대로 잡혀야 나머지를 쌓을 수 있거든요.**

---

## 마무리 (13:30 - 14:30)

[화면: 요약 슬라이드]

오늘 배운 내용:

1. **F# + FsLexYacc** 환경 설정
2. **AST** 타입 정의 (Discriminated Union)
3. **Parser** 문법 파일 작성
4. **Lexer** 규칙 파일 작성
5. **빌드 순서** - Parser 먼저, Lexer 나중!

다음 에피소드에서는 **사칙연산**을 추가해서 실제로 계산하는 인터프리터를 만들어 볼 겁니다.

`2 + 3 * 4`를 입력하면 14가 나오는 거죠.

[화면: 구독/좋아요 안내]

코드는 GitHub에 올려놨으니 설명란 링크 확인하세요.

질문이나 의견은 댓글로 남겨주시고, 다음 에피소드에서 만나요!

---

## B-roll / 화면 전환 제안

- 0:00 - 인트로 애니메이션
- 1:00 - fslex/fsyacc 다이어그램
- 2:30 - 터미널 전체 화면
- 4:00 - 코드 에디터 + 작은 터미널
- 10:30 - "중요!" 팝업 효과
- 13:30 - 요약 슬라이드

---

## 태그

F#, 프로그래밍 언어, 컴파일러, 인터프리터, fslex, fsyacc, 튜토리얼, 코딩
