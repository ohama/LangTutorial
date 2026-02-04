---
created: 2026-02-03
description: FsLexYacc에서 소스 위치 추적하여 정확한 에러 메시지 생성
---

# FsLexYacc 소스 위치 추적

FsLexYacc에서 lexer와 parser를 통해 소스 코드의 위치 정보를 추적하고, AST 노드에 전파하는 방법.

## The Insight

FsLexYacc의 위치 추적은 **lexer가 수집하고, parser가 조합한다**. Lexer는 각 토큰의 시작/끝 위치를 `lexbuf.EndPos`에 기록하고, parser는 `parseState.InputStartPosition(n)`과 `InputEndPosition(n)`으로 n번째 심볼의 위치를 가져와 규칙 전체의 span을 계산한다.

## Why This Matters

위치 추적 없이는 "type mismatch" 같은 에러만 나온다. 위치 추적이 있으면:

```
error[E0301]: Type mismatch: expected int but got bool
 --> test.fun:3:10-14
```

에러 메시지에 파일명, 라인, 컬럼이 포함되어 디버깅 시간이 줄어든다.

## Recognition Pattern

다음 상황에서 이 지식이 필요하다:

- 컴파일러/인터프리터에서 정확한 에러 위치 표시
- IDE 통합 (LSP 등)에서 위치 정보 제공
- 소스맵 생성

## The Approach

### Step 1: Span 타입 정의

AST에 부착할 위치 정보 타입을 정의한다.

```fsharp
// Ast.fs
type Span = {
    FileName: string
    StartLine: int
    StartColumn: int
    EndLine: int
    EndColumn: int
}

/// FsLexYacc Position → Span 변환
let mkSpan (startPos: Position) (endPos: Position) : Span = {
    FileName = startPos.pos_fname
    StartLine = startPos.Line       // 1-based (FsLexYacc 규칙)
    StartColumn = startPos.Column   // 1-based
    EndLine = endPos.Line
    EndColumn = endPos.Column
}

/// 내장 함수 등 소스 없는 노드용
let unknownSpan = { FileName = "<unknown>"; StartLine = 0; StartColumn = 0; EndLine = 0; EndColumn = 0 }
```

### Step 2: Lexer에서 위치 초기화

**핵심**: `setInitialPos`로 lexbuf 초기화, `NextLine`으로 newline마다 위치 갱신.

```fsharp
// Lexer.fsl 상단 { } 블록
open FSharp.Text.Lexing

/// 파일명과 초기 위치 설정
let setInitialPos (lexbuf: LexBuffer<_>) (filename: string) =
    lexbuf.EndPos <- {
        pos_fname = filename
        pos_lnum = 1
        pos_bol = 0
        pos_cnum = 0
        pos_orig_lnum = 1  // 필수! 문서화 안 되어 있음
    }
```

**주의**: `pos_orig_lnum` 필드를 빠뜨리면 컴파일 에러 발생.

### Step 3: Lexer에서 newline 위치 갱신

**모든** newline 발생 지점에서 위치를 갱신해야 한다.

```fsharp
// Lexer.fsl - 메인 규칙
rule tokenize = parse
    | newline { lexbuf.EndPos <- lexbuf.EndPos.NextLine  // ← 핵심
                tokenize lexbuf }
    | ...

// 블록 주석 내 newline
and block_comment depth = parse
    | newline { lexbuf.EndPos <- lexbuf.EndPos.NextLine  // ← 주석에서도!
                block_comment depth lexbuf }
    | ...
```

**`NextLine` vs `AsNewLinePos()`**: `AsNewLinePos()`는 deprecated. `NextLine` 프로퍼티를 사용한다.

### Step 4: Parser에서 span 헬퍼 정의

Parser 상단 `%{ %}` 블록에서 헬퍼 함수를 정의한다.

```fsharp
// Parser.fsy
%{
open FSharp.Text.Parsing  // IParseState 접근

/// 규칙의 첫 심볼부터 마지막 심볼까지 span
let ruleSpan (parseState: IParseState) (firstSym: int) (lastSym: int) : Span =
    mkSpan (parseState.InputStartPosition firstSym) (parseState.InputEndPosition lastSym)

/// 단일 심볼의 span
let symSpan (parseState: IParseState) (n: int) : Span =
    mkSpan (parseState.InputStartPosition n) (parseState.InputEndPosition n)
%}
```

**심볼 번호**: 1부터 시작. `A B C`에서 A=1, B=2, C=3.

### Step 5: 문법 규칙에서 span 전파

각 규칙에서 적절한 span을 계산하여 AST 노드에 전달한다.

```fsharp
// Parser.fsy 문법 규칙
Expr:
    // 단일 토큰: symSpan 사용
    | NUMBER        { Number($1, symSpan parseState 1) }
    | TRUE          { Bool(true, symSpan parseState 1) }

    // 다중 심볼: ruleSpan 사용 (첫번째 ~ 마지막)
    | Expr PLUS Expr    { Add($1, $3, ruleSpan parseState 1 3) }
    | IF Expr THEN Expr ELSE Expr
                        { IfThenElse($2, $4, $6, ruleSpan parseState 1 6) }

    // 괄호: 내부 표현식의 span 유지
    | LPAREN Expr RPAREN { $2 }  // $2의 span이 그대로 전달됨
```

### Step 6: 호출부에서 초기화

parse 함수 호출 전에 lexbuf를 초기화한다.

```fsharp
// Program.fs
let parse (source: string) (filename: string) =
    let lexbuf = LexBuffer<_>.FromString source
    Lexer.setInitialPos lexbuf filename  // ← 초기화
    Parser.start Lexer.tokenize lexbuf
```

## Example

```fsharp
// 전체 흐름 예시
let source = "1 + true"  // 타입 에러: int + bool
let lexbuf = LexBuffer<_>.FromString source
Lexer.setInitialPos lexbuf "test.fun"

// Lexer가 "true"를 읽을 때:
// lexbuf.EndPos = { pos_fname="test.fun"; pos_lnum=1; pos_cnum=8; ... }

// Parser가 Add 규칙 적용:
// ruleSpan parseState 1 3 = { StartLine=1; StartColumn=1; EndLine=1; EndColumn=8 }

// 결과 AST:
// Add(Number(1, span1), Bool(true, span2), span_full)
// span_full로 에러 위치 표시 가능
```

## 체크리스트

- [ ] `pos_orig_lnum` 필드 포함했는가?
- [ ] 모든 newline 지점(주석 포함)에서 `NextLine` 호출하는가?
- [ ] `AsNewLinePos()` 대신 `NextLine` 사용하는가?
- [ ] 심볼 번호가 1부터 시작하는지 확인했는가?

## 관련 문서

- `write-fslex-lexer.md` - fslex 기본 사용법
- `write-fsyacc-parser.md` - fsyacc 기본 사용법
