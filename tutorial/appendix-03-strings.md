# Appendix C: Strings

이 부록에서는 FunLang에 문자열 타입을 추가한다. 문자열 리터럴, 이스케이프 시퀀스, 문자열 연산을 구현한다.

## 개요

문자열은 실용적인 언어의 필수 기능이다:
- **리터럴**: `"hello"`
- **이스케이프**: `"line1\nline2"`, `"tab\there"`
- **연결**: `"hello" + " world"`
- **비교**: `"abc" = "abc"`, `"a" < "b"`

## AST 확장

### Value 타입에 StringValue 추가

```fsharp
// FunLang/Ast.fs

type Value =
    | IntValue of int
    | BoolValue of bool
    | StringValue of string
    | FunctionValue of param: string * body: Expr * closure: Env
    | TupleValue of Value list
    | ListValue of Value list
```

### Expr 타입에 String 추가

```fsharp
type Expr =
    // ... 기존 케이스들 ...
    | String of value: string * span: Span
```

## Lexer 구현

### 문자열 리터럴 시작

큰따옴표(`"`)를 만나면 문자열 읽기 모드로 전환한다.

```fsharp
// FunLang/Lexer.fsl

rule tokenize = parse
    // ... 다른 규칙들 ...
    | '"'           { read_string (System.Text.StringBuilder()) lexbuf }
```

### 문자열 읽기 핸들러

`StringBuilder`로 문자열을 누적하며 이스케이프를 처리한다.

```fsharp
// 문자열 리터럴 핸들러 - 이스케이프 시퀀스 지원
and read_string buf = parse
    | '"'           { STRING (buf.ToString()) }           // 종료
    | "\\n"         { buf.Append('\n') |> ignore          // 개행
                      read_string buf lexbuf }
    | "\\t"         { buf.Append('\t') |> ignore          // 탭
                      read_string buf lexbuf }
    | "\\\\"        { buf.Append('\\') |> ignore          // 백슬래시
                      read_string buf lexbuf }
    | "\\\""        { buf.Append('\"') |> ignore          // 큰따옴표
                      read_string buf lexbuf }
    | newline       { failwith "Newline in string literal" }  // 줄바꿈 에러
    | eof           { failwith "Unterminated string literal" } // EOF 에러
    | _             { buf.Append(lexeme lexbuf) |> ignore
                      read_string buf lexbuf }            // 일반 문자
```

### 지원하는 이스케이프 시퀀스

| 이스케이프 | 문자 | 설명 |
|-----------|------|------|
| `\n` | 개행 | 줄바꿈 |
| `\t` | 탭 | 수평 탭 |
| `\\` | `\` | 백슬래시 |
| `\"` | `"` | 큰따옴표 |

## Parser 확장

```fsharp
// FunLang/Parser.fsy

%token <string> STRING

Atom:
    | NUMBER                    { Number ($1, mkSpan parseState 1 1) }
    | TRUE                      { Bool (true, mkSpan parseState 1 1) }
    | FALSE                     { Bool (false, mkSpan parseState 1 1) }
    | STRING                    { String ($1, mkSpan parseState 1 1) }
    | IDENT                     { Var ($1, mkSpan parseState 1 1) }
    // ... 나머지 ...
```

## Eval 확장

### 문자열 연산 평가

```fsharp
// FunLang/Eval.fs

let rec eval (env: Env) (expr: Expr): Value =
    match expr with
    // 리터럴
    | String (s, _) -> StringValue s

    // 연결 (+ 연산자 오버로딩)
    | Add (e1, e2, _) ->
        match eval env e1, eval env e2 with
        | IntValue a, IntValue b -> IntValue (a + b)
        | StringValue a, StringValue b -> StringValue (a + b)
        | _ -> failwith "Type error in addition"

    // 비교
    | Equal (e1, e2, _) ->
        match eval env e1, eval env e2 with
        | IntValue a, IntValue b -> BoolValue (a = b)
        | BoolValue a, BoolValue b -> BoolValue (a = b)
        | StringValue a, StringValue b -> BoolValue (a = b)
        | _ -> failwith "Type error in equality"
```

### 문자열 비교

문자열은 사전순(lexicographic) 비교를 지원한다:

```fsharp
| LessThan (e1, e2, _) ->
    match eval env e1, eval env e2 with
    | IntValue a, IntValue b -> BoolValue (a < b)
    | StringValue a, StringValue b -> BoolValue (a < b)
    | _ -> failwith "Type error in comparison"
```

## Format 확장

```fsharp
// FunLang/Format.fs

let formatValue (value: Value): string =
    match value with
    | IntValue n -> string n
    | BoolValue b -> if b then "true" else "false"
    | StringValue s -> sprintf "\"%s\"" (escapeString s)
    | FunctionValue _ -> "<function>"
    | TupleValue vs -> sprintf "(%s)" (String.concat ", " (List.map formatValue vs))
    | ListValue vs -> sprintf "[%s]" (String.concat "; " (List.map formatValue vs))

// 출력 시 이스케이프 복원
let private escapeString (s: string) =
    s.Replace("\\", "\\\\")
     .Replace("\"", "\\\"")
     .Replace("\n", "\\n")
     .Replace("\t", "\\t")
```

## Type 확장

```fsharp
// FunLang/Type.fs

type Type =
    | TInt
    | TBool
    | TString
    | TArrow of Type * Type
    | TVar of int
    | TTuple of Type list
    | TList of Type
```

## 테스트

### 기본 리터럴

```bash
$ dotnet run --project FunLang -- --expr '"hello"'
"hello"
```

### 이스케이프 시퀀스

```bash
$ dotnet run --project FunLang -- --expr '"line1\nline2"'
"line1\nline2"

$ dotnet run --project FunLang -- --expr '"tab\there"'
"tab\there"
```

### 문자열 연결

```bash
$ dotnet run --project FunLang -- --expr '"hello" + " " + "world"'
"hello world"
```

### 문자열 비교

```bash
$ dotnet run --project FunLang -- --expr '"abc" = "abc"'
true

$ dotnet run --project FunLang -- --expr '"apple" < "banana"'
true
```

### 타입 추론

```bash
$ dotnet run --project FunLang -- --emit-type --expr '"hello"'
string

$ dotnet run --project FunLang -- --emit-type --expr 'fun s -> s + "!"'
string -> string
```

## 제한사항

현재 구현에서 지원하지 않는 기능:
- 문자열 인덱싱 (`s.[0]`)
- 문자열 슬라이싱 (`s.[1..3]`)
- 문자열 길이 함수 (Prelude에서 추가 가능)
- 유니코드 이스케이프 (`\uXXXX`)

## 요약

| 기능 | 구문 | 예시 |
|------|------|------|
| 리터럴 | `"..."` | `"hello"` |
| 이스케이프 | `\n`, `\t`, `\\`, `\"` | `"line1\nline2"` |
| 연결 | `+` | `"a" + "b"` |
| 비교 | `=`, `<>`, `<`, `>`, `<=`, `>=` | `"a" < "b"` |

**구현 포인트:**
- Lexer: 별도 핸들러로 이스케이프 처리
- Eval: `+` 연산자 오버로딩
- Type: `TString` 타입 추가
