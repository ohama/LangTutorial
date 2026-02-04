# EP11: Rust 스타일 에러 메시지 - 개발자 경험의 혁명!

## 영상 정보
- **예상 길이**: 18-20분
- **난이도**: 중급-고급
- **필요 사전 지식**: EP10 시청 (타입 시스템)

## 인트로 (0:00)

여러분, v5.0의 첫 에피소드입니다! 오늘의 주제는 **에러 메시지**예요.

[화면: 일반적인 컴파일러 에러 - "type mismatch" 한 줄]

이런 에러 메시지 본 적 있죠? "타입이 안 맞는다"... 그래서 어디가요? 뭐가 문제예요?

[화면: FunLang의 새 에러 메시지 - Rust 스타일 멀티라인]

```
error[E0301]: Type mismatch
  --> <expr>:1:5-8
   |
 1 | 1 + true
   |     ^^^^ expected int but got bool
   |
help: Check that all branches of conditionals and operators have compatible types
```

와! 에러 코드, 소스 위치, 밑줄까지! Rust를 써보신 분들은 익숙하실 거예요.

오늘은 이런 **친절한 에러 메시지**를 어떻게 구현하는지 알아봅니다. 언어의 "personality"를 만드는 작업이에요!

Let's go!

## 본문

### 섹션 1: 좋은 에러 메시지란? (1:30)

먼저 좋은 에러 메시지의 조건을 생각해봅시다.

[화면: 나쁜 에러 메시지 vs 좋은 에러 메시지 비교]

**나쁜 에러 메시지:**
- "Error: type mismatch"
- 어디서? 뭐가? 왜?

**좋은 에러 메시지:**
1. **위치** - 어디서 문제가 발생했는지
2. **기대값 vs 실제값** - 뭘 원했고 뭐가 왔는지
3. **컨텍스트** - 왜 그 타입을 기대했는지
4. **힌트** - 어떻게 고칠 수 있는지

[화면: Elm, Rust, TypeScript 에러 메시지 예시]

Elm과 Rust는 이걸 정말 잘해요. "컴파일러가 친절하다"는 평가를 받죠. 우리도 이걸 만들어봅시다!

### 섹션 2: Span - 소스 위치 추적 (3:00)

에러 위치를 알려면 **모든 AST 노드**가 자기 위치를 알아야 해요.

[화면: Span 타입 정의]

```fsharp
type Span = {
    Filename: string
    StartLine: int
    StartCol: int
    EndLine: int
    EndCol: int
}
```

이걸 AST 노드마다 붙입니다:

```fsharp
type Expr =
    | Number of value: int * span: Span
    | Bool of value: bool * span: Span
    | Add of left: Expr * right: Expr * span: Span
    // ...
```

[화면: Lexer에서 위치 추적하는 코드]

fslex의 `lexbuf.EndPos`를 사용하면 현재 위치를 알 수 있어요.

```fsharp
let mkSpan (parseState: IParseState) startIdx endIdx =
    let startPos = parseState.InputStartPosition startIdx
    let endPos = parseState.InputEndPosition endIdx
    { Filename = startPos.FileName
      StartLine = startPos.Line
      StartCol = startPos.Column
      EndLine = endPos.Line
      EndCol = endPos.Column }
```

### 섹션 3: Diagnostic 타입 (6:00)

에러 정보를 담는 구조체를 만들어요.

[화면: Diagnostic.fs 파일]

```fsharp
type DiagnosticSpan = {
    Span: Span
    Label: string option
    Primary: bool
}

type Diagnostic = {
    Code: string           // E0301
    Message: string        // "Type mismatch"
    Spans: DiagnosticSpan list
    Notes: string list
    Hint: string option
}
```

**Primary span**은 메인 에러 위치, **Secondary span**은 관련 위치예요.

[화면: Primary/Secondary span 예시]

```
error[E0301]: Type mismatch
  --> <expr>:1:5-8
   |
 1 | 1 + true
   | -   ^^^^ got bool (primary)
   | |
   | expected int because of this (secondary)
```

### 섹션 4: 에러 코드 체계 (9:00)

에러를 분류하면 검색하기 쉬워요.

[화면: 에러 코드 목록]

| 코드 | 의미 |
|------|------|
| E0301 | Type mismatch |
| E0302 | Unbound variable |
| E0303 | Not a function |
| E0304 | Occurs check (infinite type) |

[화면: Rust의 `rustc --explain E0308` 데모]

나중에 `funlang --explain E0301` 같은 기능도 추가할 수 있어요!

### 섹션 5: InferContext - 타입 추론 경로 (11:00)

"왜 int를 기대했어요?"라는 질문에 답하려면 **추론 과정**을 기록해야 해요.

[화면: InferContext 타입]

```fsharp
type InferContext =
    | InLetRhs of name: string * span: Span
    | InLetBody of name: string * span: Span
    | InAppFun of span: Span
    | InAppArg of span: Span
    | InIfCond of span: Span
    | InIfThen of span: Span
    | InIfElse of span: Span
    // ...
```

타입 추론할 때 이 컨텍스트를 스택처럼 쌓아요:

```fsharp
let rec synth (ctx: InferContext list) (env: TypeEnv) (expr: Expr) =
    match expr with
    | If (cond, thenE, elseE, span) ->
        let s1, condTy = synth (InIfCond span :: ctx) env cond
        // ...
```

에러가 나면 이 스택을 보고 "아, if 조건에서 bool을 기대했구나!" 알 수 있어요.

### 섹션 6: 포매팅 - Rust 스타일 출력 (14:00)

이제 예쁘게 출력하는 부분!

[화면: formatDiagnostic 함수]

```fsharp
let formatDiagnostic (diag: Diagnostic) (source: string) : string =
    let sb = StringBuilder()

    // 1. 헤더: error[E0301]: Type mismatch
    sb.AppendLine($"error[{diag.Code}]: {diag.Message}") |> ignore

    // 2. 위치: --> <expr>:1:5
    for span in diag.Spans do
        if span.Primary then
            sb.AppendLine($"  --> {span.Span.Filename}:{span.Span.StartLine}:{span.Span.StartCol}")

    // 3. 소스 코드와 밑줄
    // ...
```

밑줄 그리는 게 까다로워요. 탭/공백, 멀티바이트 문자 등 고려할 게 많죠.

[화면: 실제 포매팅된 에러 메시지]

### 섹션 7: 타입 정규화 (16:00)

마지막으로, 타입 변수를 예쁘게 표시해야 해요.

[화면: 정규화 전후 비교]

```
Before: t1024 -> t1025    // 내부 인덱스
After:  'a -> 'b          // 사람 친화적
```

```fsharp
let formatTypeNormalized (ty: Type) : string =
    let mutable nextName = 0
    let nameMap = Dictionary<int, string>()

    let getName idx =
        if not (nameMap.ContainsKey idx) then
            let name = sprintf "'%c" (char (int 'a' + nextName))
            nameMap.[idx] <- name
            nextName <- nextName + 1
        nameMap.[idx]

    let rec format = function
        | TVar idx -> getName idx
        | TArrow (a, b) -> sprintf "%s -> %s" (format a) (format b)
        // ...
```

## 아웃트로 (18:00)

오늘 우리는 FunLang의 **개발자 경험**을 대폭 개선했어요!

[화면: Before/After 에러 메시지 비교]

**구현한 것들:**
- Span으로 소스 위치 추적
- Diagnostic 타입으로 에러 정보 구조화
- InferContext로 추론 경로 기록
- Rust 스타일 포매팅

에러 메시지는 언어의 "personality"예요. 친절한 에러 메시지가 있으면 개발자들이 언어를 더 좋아하게 됩니다!

다음 에피소드에서는 **양방향 타입 체킹**을 구현해서 타입 어노테이션을 지원할 거예요.

좋아요, 구독, 알림 설정 부탁드려요! 다음 영상에서 만나요! 👋

## 참고 링크
- [Rust Error Index](https://doc.rust-lang.org/error-index.html)
- [Elm Error Messages](https://elm-lang.org/news/compiler-errors-for-humans)
- [Ariadne - Rust diagnostic library](https://github.com/zesterer/ariadne)
