# EP01: Foundation & Pipeline - F# 언어 인터프리터 시작하기

## 영상 정보
- **예상 길이**: 12-15분
- **난이도**: 입문
- **필요 사전 지식**: F# 기초, .NET CLI, 컴파일러 기본 개념

## 인트로 (0:00)

안녕하세요! 오늘부터 F#으로 나만의 프로그래밍 언어를 만드는 시리즈를 시작합니다.

[화면: 터미널에서 "42" 입력 → "AST: Number 42" 출력]

"내가 만든 언어로 코드를 실행한다"는 거, 상상만 해도 멋지지 않나요? 이 시리즈를 끝내면 여러분은 변수, 함수, 재귀까지 지원하는 Turing-complete 언어를 직접 만들 수 있게 됩니다.

첫 번째 에피소드인 오늘은 언어 구현의 핵심 파이프라인을 구축합니다. 아직 계산은 못하지만, Lexer → Parser → AST로 이어지는 뼈대를 완성할 거예요.

그럼 시작해볼까요!

## 본문

### 섹션 1: 컴파일러 파이프라인 이해하기 (1:00)

먼저, 우리가 만들 파이프라인이 어떻게 동작하는지 큰 그림을 봅시다.

[화면: 파이프라인 다이어그램 - "42" → Lexer → [NUMBER(42), EOF] → Parser → AST: Number 42]

사용자가 "42"라는 코드를 입력하면:

1. **Lexer(렉서)**가 문자열을 토큰으로 쪼갭니다. "42"를 NUMBER(42)라는 토큰으로 만드는 거죠.

2. **Parser(파서)**가 토큰의 나열을 분석해서 의미 있는 구조로 만듭니다. NUMBER 토큰을 받아서 "아, 이건 숫자 표현식이구나"라고 판단하는 겁니다.

3. **AST(추상 구문 트리)**가 최종 결과물입니다. 우리 코드가 어떤 구조인지 F# 타입으로 표현한 거예요.

이 세 단계가 모든 컴파일러의 기본입니다. Python도, JavaScript도, 심지어 F# 자체도 이렇게 동작해요.

### 섹션 2: 프로젝트 셋업 (2:30)

자, 이제 직접 만들어봅시다. 터미널을 열어주세요.

[화면: 터미널 실행]

먼저 .NET 10 F# 프로젝트를 생성합니다.

```bash
dotnet new console -lang F# -n FunLang -f net10.0
```

[화면: 프로젝트 생성 과정]

FunLang이라는 이름으로 프로젝트가 만들어졌죠? 이제 가장 중요한 도구를 설치할 차례입니다.

```bash
cd FunLang
dotnet add package FsLexYacc --version 11.3.0
```

[화면: 패키지 설치]

**FsLexYacc**는 F#용 Lexer와 Parser 생성기입니다. 우리가 문법을 정의하면 자동으로 Lexer와 Parser 코드를 만들어줘요. 직접 손으로 파서를 짜는 건 정말 고통스러우니까, 이런 도구를 쓰는 게 현명합니다.

### 섹션 3: AST 타입 정의하기 (4:00)

이제 첫 번째 파일, `Ast.fs`를 만듭니다.

[화면: VS Code에서 Ast.fs 파일 생성]

```fsharp
module Ast

/// Expression AST - minimal foundation for lexer/parser pipeline
/// Phase 1: Number only (proof of pipeline)
/// Phase 2 will add: Add, Subtract, Multiply, Divide
type Expr =
    | Number of int
```

[화면: 코드 작성]

간단하죠? `Expr`이라는 타입을 정의했는데, 지금은 `Number`만 있습니다. 정수 하나를 담을 수 있는 거예요.

"왜 이렇게 단순하게 시작하냐"고요? 언어를 만들 때는 작은 단계로 나눠서 진행하는 게 핵심입니다. 지금은 파이프라인이 제대로 동작하는지 증명하는 게 목표예요. 덧셈, 곱셈은 다음 에피소드에서 추가할 겁니다.

F#의 Discriminated Union을 쓰면 이런 트리 구조를 표현하기가 정말 쉽습니다. 다른 언어였으면 클래스 계층 구조를 만들어야 했을 텐데 말이죠.

### 섹션 4: Parser 작성하기 (5:30)

이제 `Parser.fsy` 파일을 만듭니다. 확장자가 `.fsy`인 거 보이시죠? 이건 fsyacc 문법 명세 파일입니다.

[화면: Parser.fsy 파일 생성 및 작성]

```fsharp
%{
open Ast
%}

// Token declarations
%token <int> NUMBER
%token EOF

// Start symbol and its type
%start start
%type <Ast.Expr> start

%%

// Grammar rules
start:
  | NUMBER EOF { Number $1 }
```

[화면: 코드 섹션별로 하이라이트]

구조를 살펴봅시다.

**첫 번째 섹션** `%{ %}` 안에는 일반 F# 코드를 씁니다. 여기서는 `Ast` 모듈을 열었어요.

**두 번째 섹션**은 토큰 선언입니다. `%token <int> NUMBER`는 "NUMBER라는 토큰이 있고, 정수 값을 가진다"는 뜻이에요. `EOF`는 입력의 끝을 나타냅니다.

`%start`와 `%type`은 파서의 진입점을 지정합니다. "start라는 규칙부터 시작하고, 결과 타입은 Ast.Expr이다"라고 선언하는 거죠.

**세 번째 섹션** `%%` 아래는 문법 규칙입니다.

```
start: NUMBER EOF { Number $1 }
```

이 규칙의 의미는 "NUMBER 토큰 하나와 EOF가 오면, NUMBER의 값(`$1`)으로 Number 생성자를 호출한다"입니다. `$1`은 첫 번째 심볼의 값을 가리키는 특별한 변수예요.

fsyacc가 이 파일을 읽고 실제로 동작하는 Parser.fs 파일을 생성해줍니다.

### 섹션 5: Lexer 작성하기 (7:30)

이제 `Lexer.fsl` 파일을 만듭니다. 이것도 fslex 명세 파일이에요.

[화면: Lexer.fsl 파일 생성]

```fsharp
{
open System
open FSharp.Text.Lexing
open Parser  // Import token types from generated Parser module

// Helper to get lexeme as string
let lexeme (lexbuf: LexBuffer<_>) =
    LexBuffer<_>.LexemeString lexbuf
}

// Character class definitions
let digit = ['0'-'9']
let whitespace = [' ' '\t']
let newline = ('\n' | '\r' '\n')

// Lexer rules
rule tokenize = parse
  | whitespace+   { tokenize lexbuf }           // Skip whitespace
  | newline       { tokenize lexbuf }           // Skip newlines
  | digit+        { NUMBER (Int32.Parse(lexeme lexbuf)) }  // Integer literal
  | eof           { EOF }                       // End of input
```

[화면: 코드 작성 및 설명]

Lexer는 두 부분으로 나뉩니다.

**상단 `{}` 블록**은 F# 코드입니다. 여기서 중요한 건 `open Parser`예요. Lexer가 Parser에서 정의한 토큰 타입(NUMBER, EOF)을 사용하기 때문입니다. 이게 나중에 빌드 순서에서 중요해져요.

**중간 `let` 정의**는 정규식 패턴입니다. `digit`은 0부터 9까지, `whitespace`는 공백과 탭이에요.

**`rule tokenize` 섹션**이 핵심입니다. 패턴 매칭으로 문자열을 분석합니다.

- `whitespace+`나 `newline`을 만나면? `tokenize lexbuf`를 재귀 호출해서 그냥 건너뜁니다.
- `digit+`를 만나면? 그 문자열을 정수로 파싱해서 NUMBER 토큰을 반환합니다.
- 입력 끝(`eof`)에 도달하면? EOF 토큰을 반환합니다.

이 규칙들로 "42"라는 문자열이 [NUMBER(42), EOF]라는 토큰 리스트로 변환되는 거죠.

### 섹션 6: 빌드 순서의 함정 (9:30)

자, 여기서 가장 중요한 포인트가 나옵니다. **빌드 순서** 얘기예요.

[화면: 다이어그램 - 런타임 vs 빌드 순서 비교]

런타임에는 Lexer → Parser 순서로 동작합니다. 문자열이 먼저 토큰으로 바뀌고, 토큰이 AST로 바뀌죠.

하지만 빌드할 때는? **Parser → Lexer** 순서여야 합니다!

왜냐하면 Lexer 코드가 `open Parser`를 하고 있잖아요? Parser 모듈이 먼저 생성되어야 Lexer를 컴파일할 수 있어요.

이걸 실수하면 어떻게 될까요?

[화면: 에러 메시지]

```
error FS0039: The namespace or module 'Parser' is not defined
```

이런 에러가 뜹니다. Lexer를 컴파일하려는데 Parser 모듈을 찾을 수 없다는 거죠.

그럼 `FunLang.fsproj` 파일을 제대로 설정해봅시다.

[화면: .fsproj 파일 편집]

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <!-- 1. AST definitions (manually written) -->
    <Compile Include="Ast.fs" />

    <!-- 2. Parser generator - MUST come before Lexer -->
    <FsYacc Include="Parser.fsy">
      <OtherFlags>--module Parser</OtherFlags>
    </FsYacc>

    <!-- 3. Lexer generator - depends on Parser tokens -->
    <FsLex Include="Lexer.fsl">
      <OtherFlags>--module Lexer --unicode</OtherFlags>
    </FsLex>

    <!-- 4. Generated parser files -->
    <Compile Include="Parser.fsi" />
    <Compile Include="Parser.fs" />

    <!-- 5. Generated lexer file -->
    <Compile Include="Lexer.fs" />

    <!-- 6. Main program -->
    <Compile Include="Program.fs" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="FsLexYacc" Version="11.3.0" />
  </ItemGroup>

</Project>
```

[화면: 순서 강조]

순서를 정리하면:

1. Ast.fs - 타입 정의
2. Parser.fsy - 파서 명세 (생성기 실행)
3. Lexer.fsl - 렉서 명세 (생성기 실행)
4. Parser.fsi, Parser.fs - 생성된 파서 코드
5. Lexer.fs - 생성된 렉서 코드
6. Program.fs - 메인 프로그램

이 순서가 정확히 지켜져야 합니다. F# 컴파일러는 위에서 아래로 순서대로 컴파일하거든요.

### 섹션 7: 파이프라인 연결하기 (11:30)

마지막으로 `Program.fs`를 작성합니다.

[화면: Program.fs 파일 작성]

```fsharp
open System
open FSharp.Text.Lexing
open Ast

/// Parse a string input and return the AST
let parse (input: string) : Expr =
    let lexbuf = LexBuffer<char>.FromString input
    Parser.start Lexer.tokenize lexbuf

[<EntryPoint>]
let main argv =
    let testInput = "42"

    printfn "FunLang Interpreter - Phase 1: Foundation"
    printfn "========================================="
    printfn ""
    printfn "Input: %s" testInput

    try
        let ast = parse testInput
        printfn "AST: %A" ast
        printfn ""
        printfn "Pipeline successful!"
        0
    with
    | ex ->
        printfn "Error: %s" ex.Message
        1
```

[화면: 코드 설명]

`parse` 함수가 핵심입니다.

1. 문자열을 `LexBuffer`로 감싸고
2. `Parser.start`를 호출하면서 `Lexer.tokenize`를 전달합니다
3. 파서가 알아서 렉서를 호출하면서 토큰을 받아가고, 최종 AST를 반환해요

메인 함수에서는 "42"를 파싱해보고 결과를 출력합니다.

### 섹션 8: 실행해보기 (12:30)

드디어 실행할 시간입니다!

[화면: 터미널에서 빌드]

```bash
dotnet build
```

[화면: 빌드 성공 메시지]

Build succeeded! Parser.fs, Lexer.fs 파일이 자동 생성됐을 거예요.

이제 실행해봅시다.

```bash
dotnet run
```

[화면: 실행 결과]

```
FunLang Interpreter - Phase 1: Foundation
=========================================

Input: 42
AST: Number 42

Pipeline successful!
```

완벽합니다! 우리가 만든 파이프라인이 동작하네요.

[화면: testInput을 "123"으로 변경 후 재실행]

다른 숫자도 테스트해볼까요? Program.fs에서 `testInput`을 "123"으로 바꿔봅시다.

```
Input: 123
AST: Number 123

Pipeline successful!
```

잘 작동합니다!

## 아웃트로 (13:30)

오늘 우리는 언어 인터프리터의 핵심 파이프라인을 구축했습니다.

[화면: 오늘 만든 것들 요약]

- **Lexer**: 문자열 → 토큰
- **Parser**: 토큰 → AST
- **AST**: F# Discriminated Union으로 표현한 구문 트리

지금은 숫자 하나만 인식하지만, 이 파이프라인이 우리 언어의 기초가 됩니다.

다음 에피소드에서는 사칙연산(+, -, *, /)을 추가하고, **Evaluator**를 구현해서 실제로 계산하는 인터프리터를 만들 거예요. "2 + 3 * 4"를 입력하면 14가 나오는 걸 보게 될 겁니다.

[화면: 다음 에피소드 예고 - "2 + 3 * 4" → 14]

코드는 GitHub 저장소에 올려놨으니 참고하시고, 질문이나 피드백은 댓글로 남겨주세요.

구독과 좋아요도 잊지 마시고, 다음 에피소드에서 만나요!

## 핵심 키워드

- FsLexYacc
- Lexer (렉서)
- Parser (파서)
- AST (추상 구문 트리)
- Discriminated Union
- fslex
- fsyacc
- 빌드 순서
- 컴파일러 파이프라인
- F# 언어 구현
- 토큰화 (tokenization)
- 구문 분석 (parsing)
