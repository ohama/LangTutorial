---
created: 2026-02-01
description: 환경 스레딩과 에러 복구가 있는 F# REPL 루프 구현
---

# F# REPL 루프 패턴

재귀 함수로 REPL 루프를 구현하고, 환경을 스레딩하며, 에러에서 복구한다.

## The Insight

REPL은 세 가지를 동시에 처리해야 한다:
1. **상태 유지** - 이전 입력의 결과가 다음에 영향
2. **에러 복구** - 하나의 잘못된 입력이 전체를 죽이면 안 됨
3. **깔끔한 종료** - EOF와 명시적 종료 모두 처리

재귀 함수 + 환경 파라미터 + try-with가 이 세 가지를 자연스럽게 해결한다.

## Why This Matters

잘못 구현하면:
- `while true` + mutable state → 에러 시 상태 꼬임
- 에러가 루프를 종료시킴
- Ctrl+D가 크래시 또는 무한 루프 유발

## Recognition Pattern

- 인터프리터나 REPL을 만들 때
- 세션 간 상태 유지가 필요할 때
- 사용자 입력을 반복 처리할 때

## The Approach

1. 환경(Env)을 파라미터로 받는 재귀 함수
2. `Console.ReadLine()` 결과를 패턴 매칭
3. null (EOF), 종료 명령, 빈 줄, 일반 입력 분기
4. try-with로 에러 복구, 같은 환경으로 계속

### Step 1: 기본 REPL 루프

```fsharp
let rec private replLoop (env: Env) : unit =
    Console.Write "prompt> "
    Console.Out.Flush()  // 프롬프트가 입력 전에 보이도록

    match Console.ReadLine() with
    | null ->
        // EOF (Ctrl+D on Unix, Ctrl+Z on Windows)
        printfn ""  // 깔끔한 종료를 위한 개행
    | "#quit" ->
        // 명시적 종료
        ()
    | "" ->
        // 빈 줄 - 그냥 계속
        replLoop env
    | line ->
        // 실제 처리
        let newEnv = processLine env line
        replLoop newEnv
```

### Step 2: 에러 복구 추가

```fsharp
    | line ->
        try
            let result = eval env (parse line)
            printfn "%s" (formatValue result)
            replLoop env  // 현재는 env 불변, 추후 확장 가능
        with ex ->
            eprintfn "Error: %s" ex.Message
            replLoop env  // 에러 후에도 같은 env로 계속
```

### Step 3: 시작 함수

```fsharp
let startRepl () : int =
    printfn "MyLang REPL"
    printfn "Type '#quit' or Ctrl+D to quit."
    printfn ""
    replLoop emptyEnv
    0  // 종료 코드
```

## Example

전체 구현:

```fsharp
module Repl

open System

type Env = Map<string, Value>
let emptyEnv : Env = Map.empty

let rec private replLoop (env: Env) : unit =
    Console.Write "lang> "
    Console.Out.Flush()

    match Console.ReadLine() with
    | null ->
        printfn ""
    | "#quit" ->
        ()
    | "" ->
        replLoop env
    | line ->
        try
            let ast = parse line
            let result = eval env ast
            printfn "%s" (formatValue result)
            replLoop env
        with ex ->
            eprintfn "Error: %s" ex.Message
            replLoop env

let startRepl () : int =
    printfn "MyLang REPL"
    printfn "Type '#quit' or Ctrl+D to quit."
    printfn ""
    replLoop emptyEnv
    0
```

**왜 while 대신 재귀?**

```fsharp
// ❌ BAD: mutable + while
let mutable env = emptyEnv
while true do
    let line = Console.ReadLine()
    if line = null then exit 0  // 흐름 제어 어려움
    env <- process env line     // mutation

// ✅ GOOD: 재귀 + 불변
let rec loop env =
    match Console.ReadLine() with
    | null -> ()                // 자연스러운 종료
    | line ->
        let newEnv = process env line
        loop newEnv             // 상태 전달
```

## 체크리스트

- [ ] `Console.ReadLine()` null 체크 (EOF)
- [ ] `Console.Out.Flush()` 호출 (프롬프트 즉시 출력)
- [ ] 빈 줄 처리 (무한 루프 방지)
- [ ] try-with로 에러 복구
- [ ] 에러 후 같은 환경으로 계속

## 관련 문서

- `setup-argu-cli.md` - CLI 인자 처리
