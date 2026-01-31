---
created: 2026-02-01
description: F# CLI를 Argu로 선언적으로 정의하기
---

# Argu CLI 설정하기

Discriminated Union으로 CLI 인자를 선언적으로 정의하고, 자동 help 생성과 타입 안전성을 얻는다.

## The Insight

CLI 파싱은 "문자열 배열 → 구조화된 데이터" 변환이다. 패턴 매칭으로 직접 처리하면 조합 폭발이 발생한다. Argu는 DU를 CLI 스키마로 사용하여 파싱, 검증, help 생성을 자동화한다.

## Why This Matters

수동 패턴 매칭 방식의 문제:
- 조합이 늘어날수록 분기 폭발 (10개 옵션 → 수십 개 패턴)
- help 텍스트가 코드와 동기화 안 됨
- 오타나 잘못된 인자에 대한 에러 메시지가 불친절

## Recognition Pattern

- F# CLI 도구를 만들 때
- `argv` 패턴 매칭이 20줄을 넘어갈 때
- `--help` 출력을 수동으로 관리하고 있을 때

## The Approach

1. CLI 인자를 DU case로 모델링
2. 속성으로 별칭, 위치 인자, 필수 여부 지정
3. `IArgParserTemplate`으로 usage 텍스트 제공
4. `ArgumentParser.Create<T>()`로 파싱

### Step 1: 패키지 추가

```bash
dotnet add package Argu
```

또는 `.fsproj`에 직접:

```xml
<PackageReference Include="Argu" Version="6.2.5" />
```

### Step 2: CLI 인자 타입 정의

```fsharp
module Cli

open Argu

[<CliPrefix(CliPrefix.DoubleDash)>]
type CliArgs =
    | [<AltCommandLine("-e")>] Expr of expression: string
    | Emit_Tokens
    | Emit_Ast
    | Repl
    | [<MainCommand; Last>] File of filename: string
with
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Expr _ -> "evaluate expression"
            | Emit_Tokens -> "show lexer tokens"
            | Emit_Ast -> "show parsed AST"
            | Repl -> "start interactive REPL"
            | File _ -> "evaluate program from file"
```

### Step 3: Main에서 파싱

```fsharp
open Argu

[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<CliArgs>(programName = "myapp")

    try
        let results = parser.Parse(argv, raiseOnUsage = false)

        if results.IsUsageRequested then
            printfn "%s" (parser.PrintUsage())
            0
        elif results.Contains Repl then
            // REPL 모드
            0
        elif results.Contains Expr then
            let expr = results.GetResult Expr
            // 표현식 처리
            0
        else
            printfn "%s" (parser.PrintUsage())
            0
    with
    | :? ArguParseException as ex ->
        eprintfn "%s" ex.Message
        1
```

## Example

핵심 속성들:

```fsharp
// 짧은 별칭
| [<AltCommandLine("-e")>] Expr of string
// → --expr "code" 또는 -e "code"

// 위치 인자 (플래그 없이)
| [<MainCommand; Last>] File of string
// → myapp program.txt

// 밑줄 → 하이픈 자동 변환
| Emit_Tokens
// → --emit-tokens

// 값 없는 플래그
| Verbose
// → --verbose (bool처럼 Contains로 체크)
```

에러 핸들링:

```fsharp
// 색상 있는 에러 메시지
let parser = ArgumentParser.Create<CliArgs>(
    programName = "myapp",
    errorHandler = ProcessExiter(colorizer = function
        | ErrorCode.HelpText -> None  // help는 색상 없음
        | _ -> Some ConsoleColor.Red))

// --help가 예외를 던지지 않게
let results = parser.Parse(argv, raiseOnUsage = false)
if results.IsUsageRequested then ...
```

## 체크리스트

- [ ] `IArgParserTemplate` 구현으로 usage 텍스트 제공
- [ ] `MainCommand`에 `Last` 추가 (다른 플래그를 먹지 않도록)
- [ ] `raiseOnUsage = false`로 help 처리
- [ ] 밑줄 네이밍이 하이픈으로 변환됨 확인

## 관련 문서

- [Argu 공식 문서](https://fsprojects.github.io/Argu/)
