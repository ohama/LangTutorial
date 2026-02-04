# 부록 D: REPL - 대화형 인터프리터 구현하기

## 영상 정보
- **예상 길이**: 12-14분
- **난이도**: 초급-중급
- **필요 사전 지식**: EP01-05 시청 (Lexer, Parser, Eval 기초)

## 인트로 (0:00)

여러분 안녕하세요! 오늘은 정말 재미있는 기능을 추가합니다.

[화면: REPL 세션 예시]

```
$ dotnet run --project FunLang -- --repl
FunLang REPL
Type '#quit' or Ctrl+D to quit.

funlang> 1 + 2
3
funlang> let x = 42
42
funlang> x + 1
43
funlang> #quit
```

바로 **REPL**입니다! Read-Eval-Print Loop의 약자예요.

코드를 입력하면 즉시 결과를 보여주는 대화형 환경. Python, Node.js처럼 FunLang도 REPL이 생깁니다!

Let's go!

## 본문

### 섹션 1: REPL이란? (1:00)

REPL의 동작을 분해해봅시다.

[화면: REPL 순환 다이어그램]

```
    ┌──────────────────────────────────┐
    │                                  │
    ▼                                  │
  Read ──▶ Eval ──▶ Print ──▶ Loop ───┘
  (입력)   (평가)   (출력)    (반복)
```

- **Read**: 사용자 입력 읽기
- **Eval**: 코드 평가
- **Print**: 결과 출력
- **Loop**: 다시 처음으로

[화면: REPL의 장점]

| 장점 | 설명 |
|------|------|
| 즉각적 피드백 | 결과를 바로 확인 |
| 환경 유지 | 이전 정의 재사용 |
| 에러 복구 | 오류 후에도 계속 |
| 실험 용이 | 언어 기능 탐색 |

### 섹션 2: 기본 구조 (2:30)

REPL 모듈을 만들어봅시다.

[화면: Repl.fs 모듈 구조]

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

`parse` 함수는 문자열을 받아서 AST로 변환해요. `<repl>`은 에러 메시지에 표시될 파일명입니다.

### 섹션 3: 환경 스레딩 (3:30)

REPL의 핵심은 **환경 유지**예요.

[화면: 환경 스레딩 시나리오]

```
funlang> let x = 42    // x를 정의
42
funlang> x + 1         // x를 사용!
43
```

한 번 정의한 `x`를 다음 입력에서 사용할 수 있어야 해요.

[화면: 잘못된 구현 vs 올바른 구현]

```fsharp
// 잘못된 방식 - 환경이 매번 리셋됨
let rec replLoop () =
    let env = Map.empty  // 매번 새로운 환경!
    // ...

// 올바른 방식 - 환경을 인자로 전달
let rec replLoop (env: Env) =
    // env가 누적됨
    // ...
    replLoop env  // 같은 환경으로 재귀
```

재귀 함수의 인자로 환경을 전달하는 게 핵심입니다!

### 섹션 4: REPL 루프 구현 (5:00)

전체 루프를 구현해봅시다.

[화면: replLoop 함수]

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
            // 4. 에러 복구
            eprintfn "Error: %s" ex.Message
            replLoop env
```

[화면: 흐름 다이어그램]

```
입력
  ↓
null? ──yes──▶ 종료 (EOF)
  │no
  ↓
"#quit"? ──yes──▶ 종료
  │no
  ↓
빈 줄? ──yes──▶ 다시 루프
  │no
  ↓
try:
  parse → eval → print
catch:
  에러 출력
  ↓
다시 루프 (환경 유지)
```

### 섹션 5: 에러 복구 (7:00)

REPL에서 에러가 나도 죽으면 안 됩니다!

[화면: 에러 복구 시나리오]

```
funlang> 1 / 0
Error: Division by zero
funlang> 1 + true
Error: Type error in addition
funlang> 1 + 2      // 여전히 동작!
3
```

[화면: try-with 구조]

```fsharp
try
    let ast = parse line
    let result = eval env ast
    printfn "%s" (formatValue result)
    replLoop env    // 성공 → 같은 환경으로 계속
with ex ->
    eprintfn "Error: %s" ex.Message
    replLoop env    // 실패해도 같은 환경으로 계속!
```

**핵심**: 에러가 나도 환경은 유지하고 루프를 계속합니다.

### 섹션 6: 종료 방법 (8:00)

세 가지 종료 방법을 지원해요.

[화면: 종료 방법 표]

| 방법 | 동작 | 플랫폼 |
|------|------|--------|
| `#quit` | 명시적 종료 명령 | 모두 |
| Ctrl+D | EOF 신호 | Unix/Linux/Mac |
| Ctrl+Z | EOF 신호 | Windows |

[화면: EOF 처리 코드]

```fsharp
match Console.ReadLine() with
| null ->       // EOF - Ctrl+D 또는 Ctrl+Z
    printfn ""  // 깔끔한 종료
| "#quit" ->    // 명시적 명령
    ()
```

`Console.ReadLine()`이 `null`을 반환하면 EOF입니다.

### 섹션 7: 진입점과 CLI 통합 (9:00)

REPL을 시작하는 함수를 만들어요.

[화면: startRepl 함수]

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

**Prelude 로드**: `map`, `filter`, `fold` 같은 내장 함수들을 초기 환경에 추가합니다.

[화면: Program.fs CLI 통합]

```fsharp
// FunLang/Program.fs

type CliArgs =
    | [<MainCommand>] Expression of expr: string
    | [<AltCommandLine("-e")>] Expr of expr: string
    | Repl            // 추가!
    | Emit_Ast
    | Emit_Type
    | Emit_Tokens

let run (args: ParseResults<CliArgs>) =
    if args.Contains Repl then
        Repl.startRepl()    // REPL 시작
    else
        // 파일 또는 표현식 평가
        // ...
```

### 섹션 8: 테스트 (10:30)

실제로 동작하는지 확인해봅시다!

[화면: 터미널 데모]

```bash
$ dotnet run --project FunLang -- --repl
FunLang REPL
Type '#quit' or Ctrl+D to quit.

funlang> 1 + 2
3
funlang> true && false
false
funlang> "hello"
"hello"
```

[화면: Prelude 함수 사용]

```
funlang> map (fun x -> x * 2) [1; 2; 3]
[2; 4; 6]
funlang> length [1; 2; 3; 4; 5]
5
funlang> fold (fun acc x -> acc + x) 0 [1; 2; 3]
6
```

Prelude 함수들이 바로 사용 가능해요!

[화면: 에러 복구 테스트]

```
funlang> 1 / 0
Error: Division by zero
funlang> 1 + true
Error: Type error in addition
funlang> 1 + 2    // 에러 후에도 계속!
3
funlang> #quit
```

## 아웃트로 (11:30)

[화면: 요약 표]

| 기능 | 구현 |
|------|------|
| 루프 | `let rec replLoop (env: Env)` |
| 환경 유지 | 재귀 인자로 스레딩 |
| 에러 복구 | try-with로 감싸기 |
| 종료 | `#quit`, Ctrl+D, Ctrl+Z |
| Prelude | `loadPrelude()`로 초기 환경 |

[화면: 핵심 구현 포인트]

**구현 포인트:**
- 재귀 함수로 무한 루프 표현
- 환경을 인자로 전달하여 상태 유지
- 에러 발생 시에도 루프 계속
- Prelude 함수들을 초기 환경에 로드

[화면: 현재 제한사항과 향후 개선]

**현재 제한:**
- 한 줄씩만 처리 (다중 줄 미지원)
- let 바인딩이 환경에 추가되지 않음

**향후 개선 가능:**
- `;;`로 다중 줄 입력 종료
- 히스토리 기능 (ReadLine 라이브러리)
- 탭 자동완성

[화면: 다음 에피소드 예고]

이것으로 부록 시리즈를 마칩니다! 주석, 문자열, REPL까지 FunLang이 더 실용적인 언어가 되었어요.

질문이나 제안은 댓글로 남겨주세요. 좋아요와 구독 잊지 마시고, 다음 영상에서 만나요!

## 핵심 키워드

- REPL
- Read-Eval-Print Loop
- 대화형 인터프리터
- 환경 스레딩
- 재귀 함수
- 에러 복구
- try-with
- Console.ReadLine
- EOF 처리
- Prelude
- FunLang
- 언어 구현
