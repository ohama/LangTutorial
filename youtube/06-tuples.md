# EP06: Tuples - 튜플로 복합 데이터 다루기

## 영상 정보
- **예상 길이**: 15-18분
- **난이도**: 중급
- **필요 사전 지식**: EP01-05 시청 (특히 EP02 함수, EP03 REPL)

## 인트로 (0:00)

안녕하세요! FunLang 튜토리얼 여섯 번째 에피소드입니다.

지금까지 우리는 정수, 불린, 문자열, 함수 같은 단일 값들을 다뤘습니다. 하지만 실제 프로그래밍에서는 관련된 여러 값을 묶어서 다뤄야 할 때가 많죠. 좌표를 표현할 때 x와 y를 따로따로 관리하는 것보다 하나로 묶으면 훨씬 편리합니다.

[화면: (1, 2), (10, 20, 30) 같은 튜플 예시]

오늘부터 시작하는 v3.0 시리즈는 데이터 구조에 집중합니다. 첫 번째 주제는 바로 튜플입니다. 튜플은 서로 다른 타입의 값들을 고정된 크기로 묶는 가장 단순하면서도 강력한 자료구조입니다.

[화면: 영상 제목 + 주요 내용 미리보기]

이번 영상에서는 튜플 리터럴, 패턴 매칭을 통한 튜플 분해, 그리고 중첩 튜플까지 완벽하게 구현해보겠습니다. 시작합니다!

## 본문

### 섹션 1: 튜플이란 무엇인가? (1:30)

먼저 튜플이 뭔지 개념부터 잡고 가죠.

[화면: 튜플 정의]

튜플은 고정 크기의 이종 컬렉션입니다. 무슨 말이냐면:

**고정 크기**: 한번 만들면 크기를 바꿀 수 없습니다. 2-튜플은 항상 2개 요소, 3-튜플은 항상 3개 요소.

**이종 컬렉션**: 서로 다른 타입을 담을 수 있습니다. `(1, true, "hello")` 처럼 정수, 불린, 문자열을 한 번에 담을 수 있죠.

[화면: FunLang REPL]

```
> (1, 2)
(1, 2)

> (1, true, "hello")
(1, true, "hello")

> (1 + 2, 3 * 4)
(3, 12)
```

보시다시피 괄호와 쉼표로 값들을 묶으면 튜플이 됩니다. 표현식도 넣을 수 있고, 각 요소는 평가된 후에 튜플로 묶입니다.

[화면: 튜플의 사용 사례]

튜플은 언제 쓰면 좋을까요?

1. **함수가 여러 값을 리턴할 때**: swap 함수처럼 두 값을 바꿔서 반환
2. **관련된 데이터를 묶을 때**: 좌표 (x, y), RGB 색상 (r, g, b)
3. **임시 구조체가 필요할 때**: 별도 타입 정의 없이 간단하게 사용

### 섹션 2: AST 확장하기 (4:00)

자, 이제 구현으로 들어갑니다. 먼저 AST를 확장해야겠죠.

[화면: FunLang/Ast.fs 파일]

```fsharp
type Expr =
    // ... 기존 케이스들 ...

    // Phase 1 (v3.0): Tuples
    | Tuple of Expr list               // 튜플 표현식
    | LetPat of Pattern * Expr * Expr  // 패턴 바인딩
```

두 가지를 추가합니다:

**`Tuple`**: 표현식 리스트를 받아서 튜플을 만듭니다. `(1, 2, 3)`은 `Tuple [Number 1; Number 2; Number 3]`로 표현되죠.

**`LetPat`**: 패턴 바인딩을 위한 새로운 let 표현식입니다. 기존 `Let`은 변수 하나만 바인딩했지만, `LetPat`은 패턴으로 여러 변수를 한 번에 바인딩할 수 있습니다.

[화면: Pattern 타입 정의]

그럼 패턴이 뭘까요?

```fsharp
and Pattern =
    | VarPat of string           // 변수 패턴: x
    | TuplePat of Pattern list   // 튜플 패턴: (p1, p2, ...)
    | WildcardPat                // 와일드카드: _
```

패턴은 값의 구조를 설명하고, 그 구조에서 데이터를 추출합니다:

- **`VarPat`**: 값을 변수에 바인딩. `let x = 5` 할 때 `x`가 VarPat입니다.
- **`TuplePat`**: 튜플을 분해. `let (x, y) = (1, 2)` 할 때 `(x, y)`가 TuplePat이죠.
- **`WildcardPat`**: 값을 무시. `let (_, y) = (1, 2)` 할 때 `_`는 첫 번째 값을 무시합니다.

[화면: Value 타입 확장]

마지막으로 런타임 값 타입도 확장합니다:

```fsharp
and Value =
    | IntValue of int
    | BoolValue of bool
    | FunctionValue of param: string * body: Expr * closure: Env
    | StringValue of string
    | TupleValue of Value list  // 튜플 값
```

`TupleValue`는 평가된 값들의 리스트입니다. 간단하죠?

### 섹션 3: Lexer와 Parser 확장 (6:30)

이제 문법을 인식하도록 렉서와 파서를 확장합니다.

[화면: FunLang/Lexer.fsl]

렉서에는 토큰 두 개만 추가하면 됩니다:

```fsharp
| ','           { COMMA }
| '_'           { UNDERSCORE }
```

**중요한 포인트**: `_` 규칙은 반드시 identifier 패턴보다 먼저 와야 합니다. 그렇지 않으면 `_`가 일반 식별자로 인식되버립니다.

[화면: FunLang/Parser.fsy - 토큰 선언]

파서에서도 토큰을 선언하고:

```fsharp
%token COMMA
%token UNDERSCORE
```

[화면: 문법 규칙들]

문법 규칙을 추가합니다. 여기서 좀 복잡한데, 차근차근 보시죠.

**튜플 표현식**은 Atom 레벨에서 파싱합니다:

```fsharp
Atom:
    | LPAREN Expr COMMA ExprList RPAREN  { Tuple($2 :: $4) }
```

`(expr)` 는 그냥 괄호 묶은 표현식이지만, `(expr, expr, ...)` 처럼 쉼표가 있으면 튜플입니다.

**패턴 바인딩**은 Expr 레벨에서:

```fsharp
Expr:
    | LET TuplePattern EQUALS Expr IN Expr  { LetPat($2, $4, $6) }
```

**패턴 자체**는 재귀적으로 정의됩니다:

```fsharp
Pattern:
    | LPAREN PatternList RPAREN   { TuplePat($2) }
    | IDENT                       { VarPat($1) }
    | UNDERSCORE                  { WildcardPat }

PatternList:
    | Pattern COMMA Pattern       { [$1; $3] }
    | Pattern COMMA PatternList   { $1 :: $3 }
```

[화면: 파싱 예시]

`let (x, y) = (1, 2) in x`를 파싱하면:

```
LetPat (
  TuplePat [VarPat "x"; VarPat "y"],
  Tuple [Number 1; Number 2],
  Var "x"
)
```

이렇게 트리가 만들어집니다. PatternList는 최소 2개 패턴이 필요하다는 점 주목하세요. 단일 요소 튜플은 지원하지 않습니다.

### 섹션 4: Evaluator - 튜플 평가 (9:30)

이제 평가기를 구현합니다. 세 가지를 구현해야 하는데요.

[화면: Tuple 평가]

**1. 튜플 생성**

```fsharp
| Tuple exprs ->
    let values = List.map (eval env) exprs
    TupleValue values
```

각 표현식을 순서대로 평가해서 TupleValue로 묶습니다. 간단하죠?

[화면: matchPattern 함수]

**2. 패턴 매칭**

핵심은 `matchPattern` 함수입니다:

```fsharp
let rec matchPattern (pat: Pattern) (value: Value) : (string * Value) list option =
    match pat, value with
    | VarPat name, v -> Some [(name, v)]
    | WildcardPat, _ -> Some []
    | TuplePat pats, TupleValue vals ->
        if List.length pats <> List.length vals then
            None  // Arity 불일치
        else
            let bindings = List.map2 matchPattern pats vals
            if List.forall Option.isSome bindings then
                Some (List.collect Option.get bindings)
            else
                None
    | _ -> None  // 타입 불일치
```

[화면: matchPattern 동작 설명 다이어그램]

이 함수는 패턴과 값을 매칭해서 바인딩 리스트를 반환합니다:

- **VarPat**: 어떤 값이든 변수에 바인딩. `x`에 `5`를 매칭하면 `[("x", IntValue 5)]`
- **WildcardPat**: 값을 무시하고 빈 리스트 반환
- **TuplePat**:
  - 먼저 길이 체크 (2-튜플 패턴에 3-튜플 값이면 실패)
  - 각 하위 패턴을 재귀적으로 매칭
  - 모든 매칭이 성공하면 바인딩들을 합침

매칭 실패하면 `None`을 반환합니다.

[화면: LetPat 평가]

**3. LetPat 표현식 평가**

```fsharp
| LetPat (pat, bindingExpr, bodyExpr) ->
    let value = eval env bindingExpr
    match matchPattern pat value with
    | Some bindings ->
        let extendedEnv = List.fold (fun e (n, v) -> Map.add n v e) env bindings
        eval extendedEnv bodyExpr
    | None ->
        // 상세한 에러 메시지...
```

1. 바인딩 표현식을 먼저 평가
2. 패턴 매칭 시도
3. 성공하면 바인딩들로 환경을 확장하고 본문 평가
4. 실패하면 상세한 에러 메시지 출력

[화면: 실행 예시]

`let (x, y) = (1, 2) in x + y`를 평가하면:

1. `(1, 2)` 평가 → `TupleValue [IntValue 1; IntValue 2]`
2. 패턴 `(x, y)` 매칭 → `[("x", IntValue 1); ("y", IntValue 2)]`
3. 환경에 x=1, y=2 추가
4. `x + y` 평가 → `IntValue 3`

### 섹션 5: 구조적 동등성과 출력 (12:00)

튜플은 구조적으로 비교할 수 있어야 합니다.

[화면: Equal 연산자 확장]

```fsharp
| Equal (left, right) ->
    match eval env left, eval env right with
    | IntValue l, IntValue r -> BoolValue (l = r)
    | BoolValue l, BoolValue r -> BoolValue (l = r)
    | TupleValue l, TupleValue r -> BoolValue (l = r)  // 추가!
    | _ -> failwith "Type error: = requires operands of same type"
```

F#의 `=` 연산자는 `Value list`에 대해 자동으로 구조적 비교를 수행합니다. 즉, 각 요소를 재귀적으로 비교하죠.

[화면: 동등성 테스트]

```
> (1, 2) = (1, 2)
true

> (1, 2) = (1, 3)
false

> ((1, 2), 3) = ((1, 2), 3)
true
```

중첩 튜플도 완벽하게 비교됩니다!

[화면: formatValue 함수]

출력 형식도 깔끔하게 만들어줍니다:

```fsharp
| TupleValue values ->
    let formattedElements = List.map formatValue values
    sprintf "(%s)" (String.concat ", " formattedElements)
```

재귀적으로 각 요소를 포맷팅해서 `(v1, v2, ...)` 형식으로 출력합니다.

### 섹션 6: 실전 예제 (13:30)

이제 재미있는 예제들을 보죠.

[화면: REPL - 중첩 튜플]

```
> ((1, 2), 3)
((1, 2), 3)

> let ((a, b), c) = ((1, 2), 3) in a + b + c
6
```

튜플 안에 튜플! 패턴도 중첩해서 분해할 수 있습니다.

[화면: 와일드카드 활용]

```
> let (_, y) = (1, 2) in y
2

> let (_, (_, z)) = (1, (2, 3)) in z
3
```

필요 없는 값은 `_`로 무시하고 원하는 값만 추출합니다.

[화면: 함수와 튜플 조합]

실용적인 헬퍼 함수들을 만들 수 있습니다:

```
> let swap = fun p -> let (x, y) = p in (y, x) in swap (1, 2)
(2, 1)

> let fst = fun p -> let (x, _) = p in x in fst (10, 20)
10

> let snd = fun p -> let (_, y) = p in y in snd (10, 20)
20
```

`swap`은 튜플의 요소를 바꾸고, `fst`와 `snd`는 첫 번째, 두 번째 요소를 추출합니다. Haskell이나 ML 계열 언어에서 보던 그 함수들이죠!

[화면: 에러 케이스]

물론 에러도 잘 처리됩니다:

```
> let (x, y) = (1, 2, 3) in x
Pattern match failed: tuple pattern expects 2 elements but value has 3

> let (x, y) = 5 in x
Pattern match failed: expected tuple value
```

패턴과 값이 맞지 않으면 명확한 에러 메시지를 보여줍니다.

### 섹션 7: 구현 팁과 정리 (15:30)

[화면: 핵심 포인트 요약]

구현하면서 주의할 점들:

**1. Lexer 규칙 순서**: `_` 토큰은 반드시 identifier 패턴보다 먼저 와야 합니다.

**2. 단일 요소 튜플 제외**: `(expr)` 는 그냥 괄호고, `(expr,)` 같은 1-튜플은 지원하지 않습니다. 최소 2개 요소 필요.

**3. 재귀적 패턴 매칭**: `matchPattern`은 재귀적으로 동작하므로 중첩 패턴도 자연스럽게 처리됩니다.

**4. F#의 자동 구조 비교**: `Value list`를 `=`로 비교하면 F#이 알아서 구조적 비교를 해줍니다. 별도 구현 불필요!

[화면: 기능 정리 테이블]

오늘 구현한 기능들:

| 기능 | 구문 | 예시 |
|------|------|------|
| 튜플 생성 | `(e1, e2, ...)` | `(1, 2, 3)` |
| 변수 패턴 | `x` | `let x = 5 in x` |
| 튜플 패턴 | `(p1, p2, ...)` | `let (x, y) = (1, 2) in ...` |
| 와일드카드 | `_` | `let (_, y) = (1, 2) in y` |
| 중첩 패턴 | `((p1, p2), p3)` | `let ((a, b), c) = ... in ...` |
| 구조적 동등성 | `=`, `<>` | `(1, 2) = (1, 2)` |

## 아웃트로 (17:00)

[화면: 완성된 기능 시연]

자, 이제 FunLang은 튜플을 지원합니다! 단순한 값들만 다루던 언어에서 복합 데이터를 다룰 수 있는 언어로 진화했습니다.

튜플은 작아 보이지만, 패턴 매칭이라는 강력한 메커니즘을 도입했다는 점에서 의미가 큽니다. 앞으로 리스트, 레코드 같은 더 복잡한 데이터 구조를 추가할 때도 이 패턴 매칭 기반을 활용하게 됩니다.

[화면: 다음 에피소드 예고]

다음 에피소드에서는 리스트를 구현합니다. 튜플은 고정 크기였지만, 리스트는 가변 크기입니다. 재귀적 구조를 가진 리스트를 어떻게 파싱하고 평가할지, 패턴 매칭은 어떻게 확장할지 기대해주세요!

[화면: 소스 코드 링크]

오늘 구현한 전체 소스 코드는 GitHub 저장소의 `tutorial/chapter-06-tuples.md`에 있습니다. 테스트 코드도 참고하시고, 직접 실행해보세요.

영상이 도움되셨다면 좋아요와 구독 부탁드립니다. 궁금한 점은 댓글로 남겨주세요!

다음 에피소드에서 만나요. 감사합니다!

## 핵심 키워드

- 튜플 (Tuple)
- 패턴 매칭 (Pattern Matching)
- 구조 분해 (Destructuring)
- 와일드카드 패턴 (Wildcard Pattern)
- 중첩 튜플 (Nested Tuple)
- 구조적 동등성 (Structural Equality)
- TupleValue
- VarPat, TuplePat, WildcardPat
- LetPat
- matchPattern
- fslex COMMA, UNDERSCORE
- fsyacc PatternList
- F# 함수형 프로그래밍
- 컴파일러 구현
