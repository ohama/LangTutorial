# EP08: Pattern Matching - 패턴 매칭의 힘

## 영상 정보
- **예상 길이**: 18-20분
- **난이도**: 중급
- **필요 사전 지식**: EP01-07 시청 (특히 EP03-재귀함수, EP04-리스트)

## 인트로 (0:00)

여러분, 안녕하세요! FunLang 튜토리얼 시리즈 8화에 오신 걸 환영합니다.

지난 영상에서 우리는 재귀 함수와 리스트를 배웠는데요, 혹시 이런 생각 해보신 적 있나요? "리스트가 비어있는지 어떻게 확인하지? 첫 번째 요소는 어떻게 꺼내지?"

If-then-else로는 이런 게 쉽지 않습니다. 그래서 오늘은 함수형 프로그래밍의 슈퍼파워 중 하나인 **패턴 매칭**을 구현해볼 거예요.

[화면: 코드 예시 - if-then-else vs pattern matching 비교]

패턴 매칭은 단순히 값을 비교하는 게 아니라, 값의 **구조**를 분해하면서 동시에 변수에 바인딩할 수 있는 강력한 기능입니다.

오늘 영상 끝나면 여러분은 리스트의 합계를 구하고, 길이를 세고, 심지어 뒤집는 함수까지 우아하게 작성할 수 있게 될 겁니다!

[화면: 목차 슬라이드]

자, 시작해볼까요?

## 본문

### 섹션 1: 패턴 매칭이란? (1:30)

[화면: match 표현식 기본 구문]

패턴 매칭의 핵심 구문은 `match` 표현식입니다.

```fsharp
match x with
| 1 -> "one"
| 2 -> "two"
| _ -> "other"
```

이 코드는 x의 값을 위에서부터 순서대로 검사합니다. 1이면 "one", 2면 "two", 그 외엔 "other"를 반환하죠.

여기서 `_`는 와일드카드 패턴이라고 하는데, 모든 값을 매칭하는 만능 패턴입니다.

[화면: first-match semantics 시각화]

중요한 건, **first-match semantics**입니다. 위에서부터 순서대로 시도하고, 첫 번째로 매칭되는 패턴의 결과를 반환합니다. 나머지는 무시되죠.

그래서 와일드카드 패턴은 보통 맨 마지막에 놓습니다. 만약 맨 위에 놓으면? 모든 값이 거기에 매칭되니까 아래 패턴들은 실행될 기회가 없겠죠?

[화면: 비소진 매칭 에러 예시]

그런데 만약 어떤 패턴도 매칭되지 않으면 어떻게 될까요?

```fsharp
match 3 with
| 1 -> "one"
| 2 -> "two"
// 3은? 런타임 에러!
```

이걸 **비소진 매칭(non-exhaustive match)** 이라고 합니다. 이 경우 런타임 에러가 발생해요. 그래서 와일드카드로 모든 경우를 커버하는 게 중요합니다!

### 섹션 2: 패턴의 종류 (4:00)

[화면: 패턴 종류 표]

FunLang이 지원하는 패턴 종류를 살펴볼게요.

**1. 변수 패턴 (Variable Pattern)**
```fsharp
match x with
| n -> n + 1
```
모든 값을 매칭하고, 그 값을 변수 `n`에 바인딩합니다. 와일드카드와 다른 점은 값을 사용할 수 있다는 거죠.

**2. 와일드카드 패턴 (Wildcard Pattern)**
```fsharp
match x with
| _ -> "default"
```
모든 값을 매칭하지만 바인딩하지 않습니다. 값이 필요 없을 때 사용하죠.

**3. 상수 패턴 (Constant Pattern)**
```fsharp
match x with
| 0 -> "zero"
| 1 -> "one"
| true -> "yes"
| false -> "no"
```
정수와 불린 상수를 매칭합니다. 정확히 그 값일 때만 매칭되죠.

[화면: 리스트 패턴 설명]

**4. 빈 리스트 패턴 (Empty List Pattern)**
```fsharp
match xs with
| [] -> 0
```
리스트가 비어있을 때만 매칭됩니다. 재귀 함수의 종료 조건으로 자주 사용돼요!

**5. Cons 패턴 (Cons Pattern)**
```fsharp
match xs with
| h :: t -> h
```
이게 진짜 강력한 겁니다! 비어있지 않은 리스트를 첫 번째 요소(head)와 나머지(tail)로 **자동으로 분해**해줘요.

`[1, 2, 3]`이 들어오면, `h`는 `1`, `t`는 `[2, 3]`이 됩니다. 마법 같죠?

[화면: 튜플 패턴 예시]

**6. 튜플 패턴 (Tuple Pattern)**
```fsharp
match pair with
| (x, y) -> x + y
```
튜플도 분해할 수 있습니다. `(1, 2)`가 들어오면 `x`는 `1`, `y`는 `2`가 되죠.

### 섹션 3: AST 확장 (7:00)

[화면: Ast.fs 코드]

이제 구현으로 들어가볼까요? AST부터 시작합니다.

```fsharp
type Expr =
    // ...
    | Match of scrutinee: Expr * clauses: MatchClause list
```

`Match` 케이스는 두 부분으로 구성됩니다:
- `scrutinee`: 검사할 표현식 (예: `x`, `xs`)
- `clauses`: 패턴-표현식 쌍의 리스트

[화면: Pattern 타입 정의]

```fsharp
type Pattern =
    | VarPat of string
    | TuplePat of Pattern list
    | WildcardPat
    | ConsPat of Pattern * Pattern
    | EmptyListPat
    | ConstPat of Constant
```

패턴 타입도 재귀적 구조입니다. `ConsPat`은 두 개의 패턴을 받죠. 그래서 `h1 :: h2 :: t` 같은 중첩 패턴도 가능해요!

[화면: Constant 타입]

```fsharp
type Constant =
    | IntConst of int
    | BoolConst of bool
```

상수 패턴을 위한 타입도 추가합니다.

### 섹션 4: Lexer와 Parser (9:00)

[화면: Lexer.fsl]

Lexer에는 세 개의 토큰만 추가하면 됩니다.

```fsharp
| "match"  { MATCH }
| "with"   { WITH }
| '|'      { PIPE }
```

`PIPE`는 각 패턴 절 앞의 `|` 문자입니다.

[화면: Parser.fsy - Match 표현식]

Parser는 조금 더 재미있습니다.

```fsharp
Expr:
    | MATCH Expr WITH MatchClauses   { Match($2, $4) }
```

Match 표현식은 가장 낮은 우선순위를 가집니다. let, if보다도 낮아요. 그래서 이렇게 쓸 수 있죠:

```fsharp
match if x > 0 then x else -x with
| n -> n + 1
```

[화면: MatchClauses 문법]

```fsharp
MatchClauses:
    | PIPE Pattern ARROW Expr                { [($2, $4)] }
    | PIPE Pattern ARROW Expr MatchClauses   { ($2, $4) :: $5 }
```

핵심 포인트: **모든 절이 `|`로 시작**합니다. 첫 번째 절도요! 일관성을 위해서죠.

```fsharp
match x with
| 1 -> "one"   // 첫 번째도 |로 시작
| 2 -> "two"
```

[화면: Pattern 문법]

```fsharp
Pattern:
    | IDENT                       { VarPat($1) }
    | UNDERSCORE                  { WildcardPat }
    | NUMBER                      { ConstPat(IntConst($1)) }
    | TRUE                        { ConstPat(BoolConst(true)) }
    | FALSE                       { ConstPat(BoolConst(false)) }
    | LBRACKET RBRACKET           { EmptyListPat }
    | Pattern CONS Pattern        { ConsPat($1, $3) }
    | LPAREN PatternList RPAREN   { TuplePat($2) }
```

`CONS` 연산자의 우선순위는 이미 선언되어 있어요. 리스트 표현식에서 사용한 `%right CONS`를 그대로 재사용합니다!

그래서 `h :: t :: rest`는 자동으로 `ConsPat(h, ConsPat(t, rest))`로 파싱되죠. 오른쪽 결합이니까요.

### 섹션 5: Evaluator - matchPattern (12:00)

[화면: matchPattern 함수 시그니처]

이제 핵심 함수입니다. `matchPattern`은 패턴과 값을 비교해서 바인딩을 생성합니다.

```fsharp
let rec matchPattern (pat: Pattern) (value: Value)
    : (string * Value) list option
```

반환 타입을 보세요. `option` 타입이죠?
- `Some [바인딩들]`: 매칭 성공
- `None`: 매칭 실패

[화면: 간단한 케이스들]

```fsharp
match pat, value with
| VarPat name, v -> Some [(name, v)]
| WildcardPat, _ -> Some []
```

변수 패턴은 항상 성공하고, 이름-값 쌍을 반환합니다.
와일드카드는 항상 성공하지만, 빈 바인딩을 반환하죠.

[화면: 상수 패턴]

```fsharp
| ConstPat (IntConst n), IntValue m ->
    if n = m then Some [] else None
| ConstPat (BoolConst b1), BoolValue b2 ->
    if b1 = b2 then Some [] else None
```

상수는 값이 정확히 같을 때만 성공합니다. 바인딩은 없어요.

[화면: EmptyListPat]

```fsharp
| EmptyListPat, ListValue [] -> Some []
```

빈 리스트 패턴은 정말 빈 리스트일 때만 매칭됩니다.

[화면: ConsPat - 가장 중요!]

```fsharp
| ConsPat (headPat, tailPat), ListValue (h :: t) ->
    match matchPattern headPat h with
    | Some headBindings ->
        match matchPattern tailPat (ListValue t) with
        | Some tailBindings ->
            Some (headBindings @ tailBindings)
        | None -> None
    | None -> None
```

이게 진짜 핵심입니다!

1. 리스트가 비어있지 않은지 확인 (`h :: t` 패턴)
2. head 패턴을 첫 요소 `h`와 매칭
3. tail 패턴을 나머지 `ListValue t`와 매칭
4. 둘 다 성공하면 바인딩을 합침

재귀적이죠? 패턴도 재귀적이니까 매칭도 재귀적입니다!

[화면: 예시 - h :: t 매칭]

```fsharp
matchPattern (ConsPat (VarPat "h", VarPat "t"))
             (ListValue [1, 2, 3])
```

1. `ListValue (1 :: [2, 3])` 분해
2. `VarPat "h"`를 `1`과 매칭 → `Some [("h", 1)]`
3. `VarPat "t"`를 `[2, 3]`과 매칭 → `Some [("t", [2, 3])]`
4. 합침 → `Some [("h", 1); ("t", [2, 3])]`

### 섹션 6: Evaluator - evalMatchClauses (14:30)

[화면: evalMatchClauses 함수]

```fsharp
let rec evalMatchClauses env scrutinee clauses =
    match clauses with
    | [] -> failwith "Match failure: no pattern matched"
    | (pattern, resultExpr) :: rest ->
        match matchPattern pattern scrutinee with
        | Some bindings ->
            let extendedEnv =
                List.fold (fun e (n, v) -> Map.add n v e)
                          env bindings
            eval extendedEnv resultExpr
        | None ->
            evalMatchClauses env scrutinee rest
```

작동 방식:

1. 절 리스트가 비었으면 → 에러! (비소진 매칭)
2. 첫 번째 절의 패턴 시도
3. 매칭 성공하면:
   - 바인딩을 환경에 추가
   - 확장된 환경에서 결과 표현식 평가
4. 실패하면 → 다음 절로 재귀

First-match semantics가 여기서 구현되는 거죠!

[화면: Match 평가]

```fsharp
| Match (scrutinee, clauses) ->
    let value = eval env scrutinee
    evalMatchClauses env value clauses
```

간단합니다. scrutinee를 평가하고, 그 값으로 절들을 시도하면 끝!

### 섹션 7: 실전 예제 - 리스트 합계 (16:00)

[화면: REPL 실행]

이제 진짜 파워를 보여드릴게요. 리스트 합계 함수입니다!

```fsharp
let rec sum xs =
    match xs with
    | [] -> 0
    | h :: t -> h + sum t
in sum [1, 2, 3, 4, 5]
```

[화면: 평가 과정 시각화]

```
sum [1, 2, 3, 4, 5]
→ match [1, 2, 3, 4, 5] with ...
→ 1 + sum [2, 3, 4, 5]
→ 1 + 2 + sum [3, 4, 5]
→ 1 + 2 + 3 + sum [4, 5]
→ 1 + 2 + 3 + 4 + sum [5]
→ 1 + 2 + 3 + 4 + 5 + sum []
→ 1 + 2 + 3 + 4 + 5 + 0
→ 15
```

패턴 매칭 덕분에 리스트를 head와 tail로 자동 분해하니까, 재귀 구조가 정말 자연스럽죠?

[화면: 더 많은 예제들]

리스트 길이:
```fsharp
let rec length xs =
    match xs with
    | [] -> 0
    | _ :: t -> 1 + length t
```

여기서는 head 값이 필요 없으니까 와일드카드를 썼어요!

중첩 패턴 - 첫 두 요소의 합:
```fsharp
match xs with
| h1 :: h2 :: t -> h1 + h2
| _ -> 0
```

최소 두 개의 요소가 있어야 첫 번째 패턴이 매칭됩니다. 멋지죠?

### 섹션 8: 패턴 매칭 vs If-Then-Else (17:30)

[화면: 비교 표]

마지막으로 왜 패턴 매칭이 if-then-else보다 나은지 정리해볼게요.

**구조 분해**: if-then-else로는 `h :: t` 같은 분해가 불가능합니다. 별도의 함수가 필요하죠.

**가독성**: 여러 조건을 처리할 때 중첩 if문은 복잡해지지만, 패턴 매칭은 여러 절로 깔끔하게 표현됩니다.

**비소진 검사**: if-then-else는 else를 빠뜨려도 컴파일 에러가 없지만, 패턴 매칭은 런타임 에러라도 알려줍니다. (정적 타입 시스템이 있다면 컴파일 타임에 잡을 수 있죠!)

If-then-else로 리스트 합계를 구현하려면?

```fsharp
let rec sum xs =
    if xs = [] then
        0
    else
        // head와 tail을 어떻게 꺼내지???
        ???
```

답이 없습니다. 그래서 패턴 매칭이 함수형 프로그래밍에서 필수인 거예요!

## 아웃트로 (18:30)

[화면: 정리 슬라이드]

오늘 우리가 배운 내용 정리해볼게요:

1. **Match 표현식**: `match e with | p1 -> e1 | p2 -> e2`
2. **6가지 패턴**: 변수, 와일드카드, 상수, 빈 리스트, cons, 튜플
3. **First-match semantics**: 위에서 아래로 순서대로
4. **구조 분해**: 패턴으로 값을 분해하고 바인딩
5. **비소진 매칭 에러**: 와일드카드로 모든 경우 커버하기

[화면: 구현 요약]

구현 측면에서는:
- AST에 `Match`, `Pattern`, `Constant` 타입 추가
- Lexer에 `MATCH`, `WITH`, `PIPE` 토큰
- Parser에 match 표현식과 패턴 문법
- Evaluator에 `matchPattern`, `evalMatchClauses` 함수

[화면: 다음 예고]

패턴 매칭으로 FunLang은 진짜 함수형 언어다운 표현력을 갖추게 됐습니다. 재귀 함수와 패턴 매칭, 이 두 개만 있으면 리스트로 뭐든지 할 수 있어요!

다음 영상에서는... 음, 아직 비밀이지만, 우리 언어를 한층 더 강력하게 만들어줄 기능을 다룰 예정입니다. 기대해주세요!

오늘도 시청해주셔서 감사합니다. 코드는 GitHub 레포지토리에서 확인하실 수 있어요. 좋아요와 구독 부탁드리고, 다음 영상에서 만나요!

[화면: 엔딩 카드 - GitHub 링크, 다음 영상]

## 핵심 키워드

- Pattern Matching (패턴 매칭)
- Match Expression (매치 표현식)
- First-match Semantics (첫-매칭 의미론)
- Destructuring (구조 분해)
- Cons Pattern (cons 패턴)
- Wildcard Pattern (와일드카드 패턴)
- Non-exhaustive Match (비소진 매칭)
- Recursive List Processing (재귀적 리스트 처리)
- Functional Programming (함수형 프로그래밍)
- F# Language Implementation (F# 언어 구현)
