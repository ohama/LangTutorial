# Chapter 11: Type Error Diagnostics

이 장에서는 FunLang의 타입 에러에 **정확한 위치 정보와 컨텍스트**를 추가하여 사용자 친화적인 진단 메시지를 생성한다. Rust 컴파일러 스타일의 멀티라인 에러 포맷을 구현한다.

## 개요

진단 시스템은 다음 기능을 제공한다:

- **Span 추적**: 모든 AST 노드에 소스 위치 (파일, 라인, 컬럼) 기록
- **에러 코드**: E0301~E0304로 에러 유형 식별
- **컨텍스트 스택**: 타입 추론 경로 추적 (어디서 에러가 발생했는지)
- **Blame Assignment**: Primary/secondary span으로 관련 위치 표시
- **Rust-style 출력**: 멀티라인 에러 포맷 (위치, 노트, 힌트)

**핵심 아이디어**: Lexer가 위치를 수집하고, Parser가 AST에 전파하며, 타입 추론이 컨텍스트를 쌓고, 최종적으로 Diagnostic으로 변환하여 출력한다.

## 구현 전략

진단 시스템은 4단계로 구현된다:

1. **Span Infrastructure**: Lexer/Parser에서 위치 추적
2. **Error Representation**: Diagnostic, TypeError, InferContext, UnifyPath 타입
3. **Blame Assignment**: 컨텍스트 스택에서 secondary span 추출
4. **Output Formatting**: Rust-style 멀티라인 포맷

### 에러 출력 예시

```
error[E0301]: Type mismatch: expected int but got bool
 --> test.fun:3:10-14
   = in if condition: test.fun:3:4-20
   = note: in if then-branch at test.fun:3:4
   = hint: Check that all branches of your expression return the same type
```

## Ast.fs: Span 타입

`FunLang/Ast.fs` 상단에 Span 타입과 헬퍼 함수를 정의한다.

### Span 타입 정의

```fsharp
/// Source location span for error messages
type Span = {
    FileName: string
    StartLine: int
    StartColumn: int
    EndLine: int
    EndColumn: int
}
```

**설계 결정**:
- 1-based 라인/컬럼: FsLexYacc Position API와 일치
- 시작/끝 위치: 단일 토큰부터 복합 표현식까지 표현

### 헬퍼 함수

```fsharp
/// Create span from FsLexYacc Position records
let mkSpan (startPos: Position) (endPos: Position) : Span =
    {
        FileName = startPos.FileName
        StartLine = startPos.Line
        StartColumn = startPos.Column
        EndLine = endPos.Line
        EndColumn = endPos.Column
    }

/// Sentinel span for built-in/synthetic definitions
let unknownSpan : Span =
    { FileName = "<unknown>"; StartLine = 0; StartColumn = 0; EndLine = 0; EndColumn = 0 }

/// Format span for error messages
let formatSpan (span: Span) : string =
    if span = unknownSpan then "<unknown location>"
    elif span.StartLine = span.EndLine then
        sprintf "%s:%d:%d-%d" span.FileName span.StartLine span.StartColumn span.EndColumn
    else
        sprintf "%s:%d:%d-%d:%d" span.FileName span.StartLine span.StartColumn span.EndLine span.EndColumn
```

- `unknownSpan`: Prelude 함수처럼 소스가 없는 노드용
- `formatSpan`: 같은 라인이면 `file:line:col1-col2`, 다르면 `file:line1:col1-line2:col2`

### AST에 Span 추가

모든 Expr/Pattern variant에 `span: Span`을 **마지막 named parameter**로 추가한다:

```fsharp
type Expr =
    | Number of int * span: Span
    | Add of Expr * Expr * span: Span
    | Lambda of string * Expr * span: Span
    | App of Expr * Expr * span: Span
    | Let of string * Expr * Expr * span: Span
    | IfThenElse of Expr * Expr * Expr * span: Span
    // ... 모든 variant에 span 추가
```

**Named parameter 이유**: F#에서 패턴 매칭 시 `_`로 무시할 수 있어 기존 코드 호환성 유지.

```fsharp
// 기존 코드는 span을 _ 로 무시
| Add (e1, e2, _) -> evalExpr env e1 + evalExpr env e2
```

### spanOf 헬퍼

```fsharp
/// Extract span from any expression
let spanOf (expr: Expr) : Span =
    match expr with
    | Number(_, s) | Bool(_, s) | String(_, s) | Var(_, s) -> s
    | Add(_, _, s) | Subtract(_, _, s) | Multiply(_, _, s) | Divide(_, _, s) -> s
    | Lambda(_, _, s) | App(_, _, s) | Let(_, _, _, s) -> s
    // ... 모든 variant
```

## Lexer.fsl: 위치 초기화

Lexer에서 위치 추적을 활성화한다.

### setInitialPos 함수

```fsharp
// Lexer.fsl 상단 { } 블록
open FSharp.Text.Lexing

/// Initialize position tracking for the lexbuf
let setInitialPos (lexbuf: LexBuffer<_>) (filename: string) =
    lexbuf.EndPos <- {
        pos_fname = filename
        pos_lnum = 1
        pos_bol = 0
        pos_cnum = 0
        pos_orig_lnum = 1  // 필수! 문서에 없지만 빠지면 컴파일 에러
    }
```

### Newline 위치 갱신

**모든** newline 발생 지점에서 `NextLine`을 호출해야 한다:

```fsharp
rule tokenize = parse
    | newline { lexbuf.EndPos <- lexbuf.EndPos.NextLine
                tokenize lexbuf }
    | ...

// 블록 주석 내에서도!
and block_comment depth = parse
    | newline { lexbuf.EndPos <- lexbuf.EndPos.NextLine
                block_comment depth lexbuf }
    | ...
```

**주의**: `AsNewLinePos()`는 deprecated. 반드시 `NextLine` 프로퍼티를 사용한다.

## Parser.fsy: Span 전파

Parser에서 AST 노드에 span을 전달한다.

### 헬퍼 함수

```fsharp
%{
open FSharp.Text.Parsing  // IParseState 접근

/// Create span from first symbol's start to last symbol's end
let ruleSpan (parseState: IParseState) (firstSym: int) (lastSym: int) : Span =
    mkSpan (parseState.InputStartPosition firstSym) (parseState.InputEndPosition lastSym)

/// Get span of a single symbol
let symSpan (parseState: IParseState) (n: int) : Span =
    mkSpan (parseState.InputStartPosition n) (parseState.InputEndPosition n)
%}
```

- `ruleSpan`: 다중 심볼 규칙용 (e.g., `IF Expr THEN Expr ELSE Expr`)
- `symSpan`: 단일 심볼 규칙용 (e.g., `NUMBER`)
- 심볼 번호는 1부터 시작

### 문법 규칙에서 span 전파

```fsharp
Expr:
    // 단일 토큰: symSpan 사용
    | NUMBER        { Number($1, symSpan parseState 1) }
    | TRUE          { Bool(true, symSpan parseState 1) }

    // 다중 심볼: ruleSpan 사용
    | Expr PLUS Expr    { Add($1, $3, ruleSpan parseState 1 3) }
    | IF Expr THEN Expr ELSE Expr
                        { IfThenElse($2, $4, $6, ruleSpan parseState 1 6) }

    // 괄호: 내부 표현식의 span 유지
    | LPAREN Expr RPAREN { $2 }
```

## Diagnostic.fs: 에러 표현

`FunLang/Diagnostic.fs`는 타입 에러를 사용자 친화적인 진단으로 변환한다.

### Diagnostic 타입

```fsharp
/// General error representation with location, message, and helpful context
type Diagnostic = {
    Code: string option           // e.g., Some "E0301"
    Message: string               // Primary error message
    PrimarySpan: Span             // Main error location
    SecondarySpans: (Span * string) list  // Related locations with labels
    Notes: string list            // Additional context
    Hint: string option           // Suggested fix
}
```

### TypeErrorKind

```fsharp
/// Type error kind - what went wrong
type TypeErrorKind =
    | UnifyMismatch of expected: Type * actual: Type  // E0301
    | OccursCheck of var: int * ty: Type              // E0302
    | UnboundVar of name: string                      // E0303
    | NotAFunction of ty: Type                        // E0304
```

**에러 코드 체계**:
- `E0301`: 타입 불일치 (가장 흔함)
- `E0302`: Occurs check 실패 (무한 타입)
- `E0303`: 정의되지 않은 변수
- `E0304`: 함수가 아닌 값 호출

### InferContext: 추론 경로

```fsharp
/// Inference context - path through the expression being type checked
type InferContext =
    | InIfCond of Span
    | InIfThen of Span
    | InIfElse of Span
    | InAppFun of Span
    | InAppArg of Span
    | InLetRhs of name: string * Span
    | InLetBody of name: string * Span
    | InLetRecBody of name: string * Span
    | InMatch of Span
    | InMatchClause of index: int * Span
    | InTupleElement of index: int * Span
    | InListElement of index: int * Span
    | InConsHead of Span
    | InConsTail of Span
```

타입 추론이 AST를 순회하며 컨텍스트를 스택에 push한다. 에러 발생 시 이 스택이 "어디서 에러가 났는지" 알려준다.

### UnifyPath: 단일화 경로

```fsharp
/// Unification path - where in the type structure unification failed
type UnifyPath =
    | AtFunctionParam of Type
    | AtFunctionReturn of Type
    | AtTupleIndex of index: int * Type
    | AtListElement of Type
```

`(int -> bool, int -> int)`을 단일화할 때 `AtFunctionReturn`에서 실패한다는 정보를 제공한다.

### TypeError: 통합 에러 타입

```fsharp
/// Rich type error with full context for diagnostics
type TypeError = {
    Kind: TypeErrorKind
    Span: Span
    Term: Expr option
    ContextStack: InferContext list
    Trace: UnifyPath list
}
```

### typeErrorToDiagnostic 변환

```fsharp
/// Convert TypeError to Diagnostic for display
let typeErrorToDiagnostic (err: TypeError) : Diagnostic =
    let code, message, hint =
        match err.Kind with
        | UnifyMismatch (expected, actual) ->
            Some "E0301",
            sprintf "Type mismatch: expected %s but got %s" (formatType expected) (formatType actual),
            Some "Check that all branches of your expression return the same type"

        | OccursCheck (var, ty) ->
            Some "E0302",
            sprintf "Occurs check: cannot construct infinite type '%c = %s"
                (char (97 + var % 26)) (formatType ty),
            Some "This usually means you're trying to define a recursive type without a base case"

        | UnboundVar name ->
            Some "E0303",
            sprintf "Unbound variable: %s" name,
            Some "Make sure the variable is defined before use"

        | NotAFunction ty ->
            Some "E0304",
            sprintf "Type %s is not a function and cannot be applied" (formatType ty),
            Some "Check that you're calling a function, not a value"

    // ... notes, secondarySpans 구성
```

## Infer.fs: 컨텍스트 스택 관리

타입 추론 함수가 컨텍스트를 추적하도록 수정한다.

### inferWithContext

```fsharp
/// Type inference with context tracking
let rec inferWithContext (env: TypeEnv) (ctx: InferContext list) (expr: Expr) : Subst * Type =
    match expr with
    | IfThenElse (cond, thenExpr, elseExpr, span) ->
        // 조건 추론 시 InIfCond 컨텍스트 push
        let s1, condTy = inferWithContext env (InIfCond (spanOf cond) :: ctx) cond
        let s2 = unifyWithContext condTy TBool ctx []
        // then 브랜치 추론 시 InIfThen 컨텍스트 push
        let s3, thenTy = inferWithContext (applyEnv s2 env) (InIfThen (spanOf thenExpr) :: ctx) thenExpr
        // else 브랜치 추론 시 InIfElse 컨텍스트 push
        let s4, elseTy = inferWithContext (applyEnv s3 env) (InIfElse (spanOf elseExpr) :: ctx) elseExpr
        // ...

    | App (func, arg, span) ->
        let s1, funcTy = inferWithContext env (InAppFun (spanOf func) :: ctx) func
        let s2, argTy = inferWithContext (applyEnv s1 env) (InAppArg (spanOf arg) :: ctx) arg
        // NotAFunction 체크
        match apply s2 funcTy with
        | TInt | TBool | TString | TTuple _ | TList _ as ty ->
            raise (TypeException { Kind = NotAFunction ty; Span = spanOf func; ... })
        | _ ->
            // 정상 단일화
```

**핵심**: 각 재귀 호출 전에 현재 위치를 컨텍스트 스택에 push한다.

## Type.fs: 타입 변수 정규화

내부 타입 변수 인덱스(1000, 1001, ...)를 사용자 친화적인 'a, 'b, 'c로 변환한다.

### formatTypeNormalized

```fsharp
/// Format type with normalized variable names ('a, 'b, 'c based on first appearance)
let formatTypeNormalized (ty: Type) : string =
    // Collect all type variables in order of first appearance
    let rec collectVars acc = function
        | TVar n -> if List.contains n acc then acc else acc @ [n]
        | TArrow(t1, t2) -> collectVars (collectVars acc t1) t2
        | TTuple ts -> List.fold collectVars acc ts
        | TList t -> collectVars acc t
        | TInt | TBool | TString -> acc

    let vars = collectVars [] ty
    let varMap = vars |> List.mapi (fun i v -> (v, i)) |> Map.ofList

    let rec format = function
        | TVar n ->
            match Map.tryFind n varMap with
            | Some idx -> sprintf "'%c" (char (97 + idx % 26))
            | None -> sprintf "'%c" (char (97 + n % 26))
        | TArrow(t1, t2) ->
            let left = match t1 with TArrow _ -> sprintf "(%s)" (format t1) | _ -> format t1
            sprintf "%s -> %s" left (format t2)
        // ...
    format ty
```

**예시**:
- `TVar 1000 -> TVar 1001` → `'a -> 'b` (내부 인덱스 무관, 첫 등장 순서로 정규화)

## Diagnostic.fs: 포맷팅

### formatDiagnostic

```fsharp
/// Format diagnostic for display (Rust-inspired multi-line format)
let formatDiagnostic (diag: Diagnostic) : string =
    let sb = System.Text.StringBuilder()

    // Error header: error[E0301]: Type mismatch
    match diag.Code with
    | Some code -> sb.AppendLine(sprintf "error[%s]: %s" code diag.Message) |> ignore
    | None -> sb.AppendLine(sprintf "error: %s" diag.Message) |> ignore

    // Primary location: --> file.fun:2:5
    sb.AppendLine(sprintf " --> %s" (formatSpan diag.PrimarySpan)) |> ignore

    // Secondary spans (related locations)
    for (span, label) in diag.SecondarySpans do
        sb.AppendLine(sprintf "   = %s: %s" label (formatSpan span)) |> ignore

    // Notes (context stack, trace)
    for note in diag.Notes do
        sb.AppendLine(sprintf "   = note: %s" note) |> ignore

    // Hint
    match diag.Hint with
    | Some hint -> sb.AppendLine(sprintf "   = hint: %s" hint) |> ignore
    | None -> ()

    sb.ToString().TrimEnd()
```

### contextToSecondarySpans

컨텍스트 스택에서 관련 위치를 추출한다:

```fsharp
/// Extract secondary spans from context stack (limited to 3)
let contextToSecondarySpans (primarySpan: Span) (contexts: InferContext list) : (Span * string) list =
    contexts
    |> List.rev  // 외부→내부 순서로 변환
    |> List.map (function
        | InIfCond span -> (span, "in if condition")
        | InIfThen span -> (span, "in then branch")
        | InIfElse span -> (span, "in else branch")
        | InAppFun span -> (span, "in function position")
        | InAppArg span -> (span, "in argument position")
        // ...
    )
    |> List.filter (fun (span, _) -> span <> primarySpan)  // Primary span 제외
    |> List.distinctBy fst  // 중복 제거
    |> List.truncate 3  // 최대 3개로 제한
```

**설계 결정**:
- Primary span 제외: 중복 표시 방지
- 3개 제한: 과도한 정보는 오히려 방해

## CLI 통합

`Program.fs`에서 새 진단 포맷을 사용한다:

```fsharp
| EmitType ->
    let source = results.GetResult Expr
    let ast = parse source "<expr>"
    match typecheckWithDiagnostic ast with
    | Ok ty -> printfn "%s" (formatTypeNormalized ty)
    | Error diag ->
        eprintfn "%s" (formatDiagnostic diag)
        exit 1
```

## Examples

### 타입 불일치 (E0301)

```bash
$ dotnet run --project FunLang -- --emit-type -e 'if 1 then 2 else 3'
error[E0301]: Type mismatch: expected int but got bool
 --> <expr>:1:0-18
   = hint: Check that all branches of your expression return the same type
```

### Occurs Check (E0302)

```bash
$ dotnet run --project FunLang -- --emit-type -e 'let rec f x = f in f'
error[E0302]: Occurs check: cannot construct infinite type 'a = 'a -> 'b
 --> <expr>:1:14-14
   = hint: This usually means you're trying to define a recursive type without a base case
```

### 정의되지 않은 변수 (E0303)

```bash
$ dotnet run --project FunLang -- --emit-type -e 'xyz'
error[E0303]: Unbound variable: xyz
 --> <expr>:1:0-2
   = hint: Make sure the variable is defined before use
```

### 함수가 아닌 값 호출 (E0304)

```bash
$ dotnet run --project FunLang -- --emit-type -e '1 2'
error[E0304]: Type int is not a function and cannot be applied
 --> <expr>:1:0-1
   = hint: Check that you're calling a function, not a value
```

### 브랜치 타입 불일치

```bash
$ dotnet run --project FunLang -- --emit-type -e 'if true then 1 else false'
error[E0301]: Type mismatch: expected int but got bool
 --> <expr>:1:0-25
   = hint: Check that all branches of your expression return the same type
```

### 리스트 요소 타입 불일치

```bash
$ dotnet run --project FunLang -- --emit-type -e '[1, true]'
error[E0301]: Type mismatch: expected int but got bool
 --> <expr>:1:0-8
   = hint: Check that all branches of your expression return the same type
```

## 구현 세부 사항

### Context Stack 저장 순서

컨텍스트 스택은 **inner-first** (가장 안쪽이 head)로 저장하고, 출력 시 reverse한다:

```fsharp
// 저장: [InIfCond; InLetBody; InAppArg] (inner-first)
// 출력: [InAppArg; InLetBody; InIfCond] (outer-first, 사용자 친화적)
let formatContextStack (stack: InferContext list) : string list =
    stack
    |> List.rev  // ← 출력 시 reverse
    |> List.map (function ...)
```

### Secondary Span 제한

관련 위치가 너무 많으면 오히려 혼란스럽다. 3개로 제한하여 가장 관련성 높은 정보만 표시한다.

### NotAFunction 감지 타이밍

App 추론에서 단일화 **전에** NotAFunction을 체크한다:

```fsharp
| App (func, arg, span) ->
    let s1, funcTy = inferWithContext env ctx func
    let s2, argTy = inferWithContext (applyEnv s1 env) ctx arg
    // 단일화 전에 체크!
    match apply s2 funcTy with
    | TInt | TBool | TString | TTuple _ | TList _ as ty ->
        raise (TypeException { Kind = NotAFunction ty; ... })
    | _ -> // 정상 단일화
```

이렇게 하면 "타입 불일치" 대신 더 명확한 "함수가 아님" 에러를 표시할 수 있다.

## 정리

이 장에서 구현한 내용:

| 기능 | 파일 | 설명 |
|------|------|------|
| Span 타입 | `Ast.fs` | 소스 위치 (파일, 라인, 컬럼) |
| Lexer 위치 | `Lexer.fsl` | setInitialPos, NextLine |
| Parser span | `Parser.fsy` | ruleSpan, symSpan |
| Diagnostic | `Diagnostic.fs` | 에러 코드, 메시지, span, 힌트 |
| TypeError | `Diagnostic.fs` | Kind, ContextStack, Trace |
| 포맷팅 | `Diagnostic.fs` | formatDiagnostic (Rust-style) |
| 정규화 | `Type.fs` | formatTypeNormalized ('a, 'b, 'c) |
| CLI | `Program.fs` | 새 에러 포맷 출력 |

**에러 코드 체계**:

| 코드 | 의미 | 예시 |
|------|------|------|
| E0301 | 타입 불일치 | `1 + true` |
| E0302 | Occurs check | `let rec f x = f` |
| E0303 | 정의되지 않은 변수 | `xyz` |
| E0304 | 함수가 아닌 값 호출 | `1 2` |

## 테스트

```bash
# 타입 에러 골든 테스트 (12개)
make -C tests type-errors

# 전체 fslit 테스트
make -C tests

# Expecto 단위 테스트
dotnet run --project FunLang.Tests

# 전체 테스트 (570개)
make -C tests && dotnet run --project FunLang.Tests
```

## 소스 참조

전체 소스 코드는 다음 위치에서 확인할 수 있다:

- **FunLang/Ast.fs**: Span 타입, mkSpan, formatSpan, AST span 필드
- **FunLang/Lexer.fsl**: setInitialPos, NextLine 위치 갱신
- **FunLang/Parser.fsy**: ruleSpan, symSpan, span 전파
- **FunLang/Diagnostic.fs**: Diagnostic, TypeError, InferContext, UnifyPath, formatDiagnostic
- **FunLang/Type.fs**: formatTypeNormalized
- **FunLang/Infer.fs**: inferWithContext, 컨텍스트 스택 관리
- **FunLang/Program.fs**: CLI 통합

## 관련 문서

- [track-source-positions-fslexyacc](../docs/howto/track-source-positions-fslexyacc.md) - FsLexYacc 위치 추적
- [implement-hindley-milner-algorithm-w](../docs/howto/implement-hindley-milner-algorithm-w.md) - Algorithm W 타입 추론
