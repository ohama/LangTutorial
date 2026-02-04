# 부록 C: Strings - 문자열 타입 구현하기

## 영상 정보
- **예상 길이**: 12-14분
- **난이도**: 초급-중급
- **필요 사전 지식**: EP01-02 시청 (Lexer, Eval 기초)

## 인트로 (0:00)

여러분 안녕하세요! 오늘은 실용적인 기능을 추가합니다.

[화면: 문자열 예시]

```funlang
"hello"                    // 문자열 리터럴
"hello" + " world"         // 문자열 연결
"line1\nline2"            // 이스케이프 시퀀스
```

바로 **문자열**입니다! 문자열 없는 프로그래밍 언어는 정말 불편하죠?

오늘은 문자열 리터럴, 이스케이프 시퀀스, 문자열 연산을 모두 구현합니다.

Let's go!

## 본문

### 섹션 1: 문자열의 구성 요소 (1:00)

문자열을 추가하려면 여러 곳을 수정해야 해요.

[화면: 수정할 파일 목록]

| 파일 | 추가할 내용 |
|------|-------------|
| Ast.fs | StringValue, String 노드 |
| Lexer.fsl | 문자열 리터럴 토큰 |
| Parser.fsy | STRING 토큰 처리 |
| Eval.fs | 문자열 평가, 연결 |
| Type.fs | TString 타입 |
| Format.fs | 문자열 출력 |

하나씩 구현해봅시다!

### 섹션 2: AST 확장 (2:00)

먼저 문자열을 표현할 타입이 필요해요.

[화면: Ast.fs 수정]

```fsharp
// FunLang/Ast.fs

type Value =
    | IntValue of int
    | BoolValue of bool
    | StringValue of string    // 추가!
    | FunctionValue of param: string * body: Expr * closure: Env
    | TupleValue of Value list
    | ListValue of Value list

type Expr =
    // ... 기존 케이스들 ...
    | String of value: string * span: Span    // 추가!
```

`StringValue`는 런타임 값, `String`은 AST 노드예요.

### 섹션 3: Lexer - 문자열 토큰화 (3:00)

이제 Lexer가 문자열을 인식해야 해요.

[화면: 문자열의 구조]

```
"hello world"
 ↑          ↑
시작 "      끝 "
```

큰따옴표 안의 모든 것이 문자열 내용이에요.

[화면: Lexer.fsl - 문자열 시작]

```fsharp
rule tokenize = parse
    // ...
    | '"'    { read_string (System.Text.StringBuilder()) lexbuf }
```

`"`를 만나면 `read_string` 핸들러로 전환합니다.

[화면: read_string 핸들러]

```fsharp
// 문자열 리터럴 핸들러
and read_string buf = parse
    | '"'           { STRING (buf.ToString()) }        // 종료
    | "\\n"         { buf.Append('\n') |> ignore       // 개행
                      read_string buf lexbuf }
    | "\\t"         { buf.Append('\t') |> ignore       // 탭
                      read_string buf lexbuf }
    | "\\\\"        { buf.Append('\\') |> ignore       // 백슬래시
                      read_string buf lexbuf }
    | "\\\""        { buf.Append('"') |> ignore        // 큰따옴표
                      read_string buf lexbuf }
    | newline       { failwith "Newline in string literal" }
    | eof           { failwith "Unterminated string literal" }
    | _             { buf.Append(lexeme lexbuf) |> ignore
                      read_string buf lexbuf }
```

**StringBuilder**로 문자를 누적하면서 이스케이프를 처리해요!

### 섹션 4: 이스케이프 시퀀스 (5:00)

이스케이프 시퀀스가 왜 필요할까요?

[화면: 이스케이프 필요성]

```funlang
"hello\nworld"     // 두 줄로 출력하고 싶어요
"say \"hi\""       // 따옴표를 문자열에 포함하고 싶어요
```

[화면: 지원하는 이스케이프 시퀀스]

| 이스케이프 | 문자 | 설명 |
|-----------|------|------|
| `\n` | 개행 | 줄바꿈 |
| `\t` | 탭 | 수평 탭 |
| `\\` | `\` | 백슬래시 자체 |
| `\"` | `"` | 큰따옴표 자체 |

[화면: 이스케이프 처리 흐름]

```
입력: "a\nb"
       ↓
Lexer: "a" → buf에 'a' 추가
       "\n" → buf에 '\n' (실제 개행 문자) 추가
       "b" → buf에 'b' 추가
       ↓
결과: StringValue("a\nb")  // 실제 개행 문자 포함
```

Lexer에서 이미 실제 문자로 변환됩니다!

### 섹션 5: Parser와 Eval (7:00)

Parser는 간단해요.

[화면: Parser.fsy]

```fsharp
%token <string> STRING

Atom:
    | NUMBER    { Number ($1, mkSpan parseState 1 1) }
    | TRUE      { Bool (true, mkSpan parseState 1 1) }
    | FALSE     { Bool (false, mkSpan parseState 1 1) }
    | STRING    { String ($1, mkSpan parseState 1 1) }    // 추가!
    | IDENT     { Var ($1, mkSpan parseState 1 1) }
```

Eval도 추가합니다.

[화면: Eval.fs - 리터럴 평가]

```fsharp
let rec eval (env: Env) (expr: Expr): Value =
    match expr with
    | String (s, _) -> StringValue s
    // ...
```

### 섹션 6: 문자열 연산 (8:00)

문자열 연결은 `+` 연산자로 합니다.

[화면: Eval.fs - Add 연산자 오버로딩]

```fsharp
    | Add (e1, e2, _) ->
        match eval env e1, eval env e2 with
        | IntValue a, IntValue b -> IntValue (a + b)
        | StringValue a, StringValue b -> StringValue (a + b)    // 추가!
        | _ -> failwith "Type error in addition"
```

**연산자 오버로딩**: 같은 `+`가 숫자는 덧셈, 문자열은 연결!

[화면: 문자열 비교]

```fsharp
    | Equal (e1, e2, _) ->
        match eval env e1, eval env e2 with
        | IntValue a, IntValue b -> BoolValue (a = b)
        | BoolValue a, BoolValue b -> BoolValue (a = b)
        | StringValue a, StringValue b -> BoolValue (a = b)    // 추가!
        | _ -> failwith "Type error in equality"

    | LessThan (e1, e2, _) ->
        match eval env e1, eval env e2 with
        | IntValue a, IntValue b -> BoolValue (a < b)
        | StringValue a, StringValue b -> BoolValue (a < b)    // 사전순 비교
        | _ -> failwith "Type error in comparison"
```

문자열 비교는 **사전순(lexicographic)**입니다. `"apple" < "banana"`가 true!

### 섹션 7: Type과 Format (9:30)

타입 시스템에도 문자열을 추가해요.

[화면: Type.fs]

```fsharp
type Type =
    | TInt
    | TBool
    | TString    // 추가!
    | TArrow of Type * Type
    | TVar of int
    | TTuple of Type list
    | TList of Type
```

출력 포맷도 필요합니다.

[화면: Format.fs]

```fsharp
let formatValue (value: Value): string =
    match value with
    | IntValue n -> string n
    | BoolValue b -> if b then "true" else "false"
    | StringValue s -> sprintf "\"%s\"" (escapeString s)    // 추가!
    | FunctionValue _ -> "<function>"
    // ...

// 출력 시 이스케이프 복원
let private escapeString (s: string) =
    s.Replace("\\", "\\\\")
     .Replace("\"", "\\\"")
     .Replace("\n", "\\n")
     .Replace("\t", "\\t")
```

출력할 때는 이스케이프를 다시 표시해야 해요!

### 섹션 8: 테스트 (10:30)

실제로 동작하는지 확인해봅시다!

[화면: 터미널 데모]

```bash
# 기본 리터럴
$ dotnet run --project FunLang -- --expr '"hello"'
"hello"

# 이스케이프 시퀀스
$ dotnet run --project FunLang -- --expr '"line1\nline2"'
"line1\nline2"

# 문자열 연결
$ dotnet run --project FunLang -- --expr '"hello" + " " + "world"'
"hello world"

# 문자열 비교
$ dotnet run --project FunLang -- --expr '"abc" = "abc"'
true

$ dotnet run --project FunLang -- --expr '"apple" < "banana"'
true
```

[화면: 타입 추론]

```bash
$ dotnet run --project FunLang -- --emit-type --expr '"hello"'
string

$ dotnet run --project FunLang -- --emit-type --expr 'fun s -> s + "!"'
string -> string
```

타입 추론도 완벽히 동작합니다!

## 아웃트로 (11:30)

[화면: 요약 표]

| 기능 | 구문 | 예시 |
|------|------|------|
| 리터럴 | `"..."` | `"hello"` |
| 이스케이프 | `\n`, `\t`, `\\`, `\"` | `"line1\nline2"` |
| 연결 | `+` | `"a" + "b"` |
| 비교 | `=`, `<>`, `<`, `>` | `"a" < "b"` |

[화면: 핵심 구현 포인트]

**구현 포인트:**
- Lexer: StringBuilder로 이스케이프 처리
- Eval: `+` 연산자 오버로딩
- Type: `TString` 타입 추가

[화면: 제한사항]

**현재 미지원:**
- 문자열 인덱싱 (`s.[0]`)
- 문자열 슬라이싱 (`s.[1..3]`)
- 유니코드 이스케이프 (`\uXXXX`)

이런 기능은 향후 Prelude 함수나 언어 확장으로 추가할 수 있어요!

[화면: 다음 부록 예고]

다음 부록에서는 **REPL**을 구현합니다. 대화형으로 코드를 실행하는 거죠!

질문이나 제안은 댓글로 남겨주세요. 좋아요와 구독 잊지 마시고, 다음 영상에서 만나요!

## 핵심 키워드

- Strings
- 문자열
- 문자열 리터럴
- 이스케이프 시퀀스
- 문자열 연결
- Lexer
- StringBuilder
- TString
- 연산자 오버로딩
- FunLang
- 언어 구현
