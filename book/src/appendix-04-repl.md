# Appendix D: REPL

이 부록에서는 FunLang의 대화형 REPL(Read-Eval-Print Loop)을 구현한다. 환경 스레딩, 에러 복구, 종료 명령을 포함한다.

## 개요

REPL은 인터프리터 언어의 핵심 기능이다:
- **즉각적 피드백**: 코드를 입력하면 바로 결과 확인
- **환경 유지**: 이전에 정의한 변수/함수 사용 가능
- **에러 복구**: 오류 발생 후에도 계속 사용 가능
- **대화형 탐색**: 언어 기능을 실험하기 좋음

```
$ dotnet run --project FunLang -- --repl
FunLang REPL
Type '#quit' or Ctrl+D to quit.

funlang> let x = 42
42
funlang> x + 1
43
funlang> #quit
```

## 구현

### 모듈 구조

```fsharp
// FunLang/Repl.fs

module Repl

open System
open FSharp.Text.Lexing
open Ast
open Eval

/// Parse a string input and return the AST
let private parse (input: string) : Expr =
    let lexbuf = LexBuffer<char>.FromString input
    Lexer.setInitialPos lexbuf "<repl>"
    Parser.start Lexer.tokenize lexbuf
```

### REPL 루프

재귀 함수로 환경을 스레딩한다:

```fsharp
/// REPL loop with environment threading
let rec private replLoop (env: Env) : unit =
    // 1. 프롬프트 출력
    Console.Write "funlang> "
    Console.Out.Flush()

    // 2. 입력 읽기
    match Console.ReadLine() with
    | null ->
        // EOF (Ctrl+D on Unix, Ctrl+Z on Windows)
        printfn ""

    | "#quit" ->
        // 명시적 종료 명령
        ()

    | "" ->
        // 빈 줄 - 계속
        replLoop env

    | line ->
        // 3. 평가 시도
        try
            let ast = parse line
            let result = eval env ast
            printfn "%s" (formatValue result)
            replLoop env
        with ex ->
            // 4. 에러 복구 - 에러 출력 후 계속
            eprintfn "Error: %s" ex.Message
            replLoop env
```

### 진입점

```fsharp
/// Start the REPL with welcome message
let startRepl () : int =
    printfn "FunLang REPL"
    printfn "Type '#quit' or Ctrl+D to quit."
    printfn ""
    let initialEnv = Prelude.loadPrelude()  // Prelude 함수들 로드
    replLoop initialEnv
    0  // 종료 코드
```

## 핵심 설계

### 환경 스레딩

REPL에서 정의한 변수를 다음 입력에서 사용하려면 환경을 유지해야 한다.

```fsharp
// 잘못된 방식 - 환경이 유지되지 않음
let rec replLoop () =
    let env = Map.empty  // 매번 새로운 환경
    // ...

// 올바른 방식 - 환경을 인자로 전달
let rec replLoop (env: Env) =
    // env가 누적됨
    // ...
    replLoop env  // 동일한 환경으로 재귀
```

**현재 구현의 제한:**
- `let x = 1`은 `1`을 반환하지만 환경에 `x`를 추가하지 않음
- 환경을 업데이트하려면 `evalToEnv` 패턴 필요 (현재 미구현)

### 에러 복구

try-with로 파싱/평가 에러를 잡아서 REPL이 죽지 않게 한다:

```fsharp
try
    let ast = parse line
    let result = eval env ast
    printfn "%s" (formatValue result)
    replLoop env
with ex ->
    eprintfn "Error: %s" ex.Message
    replLoop env  // 에러 후에도 계속
```

### 종료 방법

세 가지 종료 방법을 지원한다:

| 방법 | 동작 |
|------|------|
| `#quit` | 명시적 종료 명령 |
| Ctrl+D | EOF 신호 (Unix) |
| Ctrl+Z | EOF 신호 (Windows) |

```fsharp
match Console.ReadLine() with
| null ->     // EOF - Ctrl+D 또는 Ctrl+Z
    printfn ""
| "#quit" ->  // 명시적 명령
    ()
```

## CLI 통합

### Program.fs

```fsharp
// FunLang/Program.fs

type CliArgs =
    | [<MainCommand>] Expression of expr: string
    | [<AltCommandLine("-e")>] Expr of expr: string
    | Repl
    | Emit_Ast
    | Emit_Type
    | Emit_Tokens

let run (args: ParseResults<CliArgs>) =
    if args.Contains Repl then
        Repl.startRepl()
    else
        // 파일 또는 표현식 평가
        // ...
```

### 사용법

```bash
# REPL 시작
$ dotnet run --project FunLang -- --repl

# 표현식 평가 (단발성)
$ dotnet run --project FunLang -- --expr "1 + 2"
3
```

## 테스트

### 기본 동작

```
funlang> 1 + 2
3
funlang> true && false
false
funlang> "hello"
"hello"
```

### Prelude 함수

```
funlang> map (fun x -> x * 2) [1; 2; 3]
[2; 4; 6]
funlang> length [1; 2; 3; 4; 5]
5
```

### 에러 복구

```
funlang> 1 / 0
Error: Division by zero
funlang> 1 + true
Error: Type error in addition
funlang> 1 + 2
3
```

### 다중 줄 (미지원)

현재는 한 줄씩만 처리한다:

```
funlang> let f = fun x ->
Error: ...  // 파싱 에러

funlang> let f = fun x -> x + 1 in f 5
6  // 한 줄에 작성하면 동작
```

## 향후 개선

### 환경 업데이트

```fsharp
// evalToEnv: 환경을 반환하는 eval
let rec evalToEnv (env: Env) (expr: Expr): Env * Value =
    match expr with
    | Let (name, value, body, _) ->
        let v = eval env value
        let newEnv = Map.add name v env
        evalToEnv newEnv body
    | _ ->
        (env, eval env expr)
```

### 다중 줄 입력

```fsharp
let rec readMultiLine (acc: string) =
    let line = Console.ReadLine()
    if line.EndsWith(";;") then
        acc + line.[..^2]  // ;; 제거
    else
        readMultiLine (acc + line + "\n")
```

### 히스토리

ReadLine 라이브러리 사용:

```fsharp
// NuGet: ReadLine
ReadLine.HistoryEnabled <- true
let line = ReadLine.Read("funlang> ")
```

## 요약

| 기능 | 구현 |
|------|------|
| 루프 | `let rec replLoop (env: Env)` |
| 환경 | 재귀 인자로 스레딩 |
| 에러 복구 | try-with로 감싸기 |
| 종료 | `#quit`, Ctrl+D, Ctrl+Z |
| Prelude | `loadPrelude()`로 초기 환경 |

**구현 포인트:**
- 재귀 함수로 무한 루프 표현
- 환경을 인자로 전달하여 상태 유지
- 에러 발생 시 환경 유지하고 계속
- Prelude 함수들을 초기 환경에 로드
