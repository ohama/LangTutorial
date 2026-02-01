# EP07: Lists - 리스트와 Cons 연산자

## 영상 정보
- **예상 길이**: 18-20분
- **난이도**: 중급
- **필요 사전 지식**: EP01-06 시청 (특히 EP05 튜플 편)

## 인트로 (0:00)

안녕하세요! FunLang 튜토리얼 시리즈 일곱 번째 영상입니다.

[화면: 타이틀 - "EP07: Lists - 함수형 프로그래밍의 핵심 자료구조"]

오늘은 함수형 프로그래밍에서 가장 중요한 자료구조 중 하나인 **리스트**를 구현해볼 거예요. 튜플과 비슷해 보이지만, 리스트는 완전히 다른 특성을 가지고 있습니다.

튜플은 고정 길이에 서로 다른 타입을 담을 수 있었죠? 반면 리스트는 **가변 길이**에 **같은 타입**만 담을 수 있어요. 그리고 함수형 언어에서 리스트를 다루는 독특한 방법이 있는데, 바로 **Cons 연산자** `::`입니다.

[화면: 코드 예시]
```
[1, 2, 3]  = 1 :: [2, 3]
           = 1 :: 2 :: [3]
           = 1 :: 2 :: 3 :: []
```

이게 무슨 의미인지, 왜 이렇게 설계되었는지, 오늘 차근차근 알아보겠습니다.

자, 시작해볼까요!

## 본문

### 섹션 1: 리스트의 재귀적 구조 이해하기 (1:30)

리스트를 구현하기 전에, 먼저 리스트가 어떤 개념인지 이해해야 합니다.

[화면: 리스트 정의 표시]

리스트는 **재귀적 자료구조**예요. 리스트는 두 가지 중 하나입니다:
1. **빈 리스트** `[]`, 또는
2. **head 요소**와 **tail 리스트**의 조합

[화면: 다이어그램 - [1,2,3] 분해 과정]

예를 들어 `[1, 2, 3]`을 재귀적으로 분해해보면:
- head = 1, tail = [2, 3]
- [2, 3]의 head = 2, tail = [3]
- [3]의 head = 3, tail = []
- []는 빈 리스트

[화면: Cons 연산자 설명]

이 head와 tail을 결합하는 연산자가 바로 **Cons 연산자** `::`입니다.
"Cons"는 "construct"의 줄임말로, LISP 시절부터 내려온 전통적인 이름이에요.

```
1 :: [2, 3]  → [1, 2, 3]
```

1을 head로, [2, 3]을 tail로 결합해서 새 리스트를 만듭니다.

[화면: 우결합 강조]

그리고 중요한 점! Cons는 **우결합(right-associative)** 연산자입니다.

```
1 :: 2 :: 3 :: []
= 1 :: (2 :: (3 :: []))
```

왜 우결합이어야 할까요? 좌결합이면 어떻게 될까요?

```
((1 :: 2) :: 3) :: []  ← 이렇게 되면 문제!
```

첫 번째 `1 :: 2`에서 이미 타입 에러가 발생합니다. Cons의 두 번째 인자는 반드시 **리스트**여야 하는데, 2는 정수니까요.

우결합으로 파싱되어야 `3 :: []`이 먼저 계산되고, 그 결과에 2를 cons하고, 최종적으로 1을 cons해서 올바른 리스트가 만들어집니다.

### 섹션 2: AST 확장 - 리스트 표현식 추가 (4:30)

자, 이제 구현을 시작해봅시다.

[화면: Ast.fs 파일]

먼저 `Ast.fs`의 `Expr` 타입에 세 가지 새로운 케이스를 추가합니다.

```fsharp
type Expr =
    // ... 기존 케이스들 ...

    // Phase 2 (v3.0): Lists
    | EmptyList                        // 빈 리스트: []
    | List of Expr list                // 리스트 리터럴: [e1, e2, ...]
    | Cons of Expr * Expr              // Cons 연산자: h :: t
```

[화면: 각 케이스 설명 애니메이션]

- `EmptyList`: 빈 리스트 `[]`
- `List`: 리스트 리터럴 `[1, 2, 3]` - 표현식 목록을 담습니다
- `Cons`: Cons 연산자 `h :: t` - head와 tail 표현식 쌍

[화면: Value 타입]

그리고 `Value` 타입에도 `ListValue`를 추가합니다.

```fsharp
and Value =
    | IntValue of int
    | BoolValue of bool
    | FunctionValue of param: string * body: Expr * closure: Env
    | StringValue of string
    | TupleValue of Value list
    | ListValue of Value list  // 새로 추가!
```

흥미로운 점은 `TupleValue`와 `ListValue` 모두 `Value list`를 감싸는 형태라는 거예요. 내부 표현은 같지만, 의미론적으로는 완전히 다릅니다.

튜플은 고정 길이, 다른 타입 가능. 리스트는 가변 길이, 같은 타입만 가능.

### 섹션 3: Lexer - 새 토큰 추가 (6:00)

[화면: Lexer.fsl 파일]

렉서에 세 개의 새 토큰을 추가합니다.

```fsharp
rule tokenize = parse
    // ... 기존 규칙들 ...

    // Phase 2 (v3.0): Cons 연산자 (다중 문자, 단일 문자보다 먼저)
    | "::"          { CONS }

    // Phase 2 (v3.0): 리스트 괄호
    | '['           { LBRACKET }
    | ']'           { RBRACKET }
```

[화면: 토큰 순서 강조]

여기서 중요한 팁! `::`는 두 문자 토큰이죠? 만약 나중에 단일 `:` 토큰을 추가한다면, 반드시 `::`를 먼저 배치해야 합니다. Longest match 규칙 때문이에요.

현재 FunLang에는 `:` 토큰이 없지만, 다중 문자 연산자를 항상 먼저 쓰는 게 좋은 습관입니다.

[화면: 토큰 테스트]

터미널에서 확인해볼까요?

```bash
$ dotnet run --project FunLang -- --emit-tokens --expr "[1, 2, 3]"
LBRACKET NUMBER(1) COMMA NUMBER(2) COMMA NUMBER(3) RBRACKET EOF
```

완벽하게 토큰화되었습니다!

### 섹션 4: Parser - 우결합 선언과 문법 규칙 (7:30)

[화면: Parser.fsy 파일]

이제 파서를 확장해봅시다. 먼저 토큰을 선언하고요.

```fsharp
// Phase 2 (v3.0): List tokens
%token LBRACKET RBRACKET CONS
```

[화면: 우선순위 선언 섹션]

그리고 가장 중요한 부분! Cons의 우선순위와 결합성을 선언합니다.

```fsharp
// 우선순위 선언 (낮은 것부터 높은 것 순)
%left OR
%left AND
%nonassoc EQUALS LT GT LE GE NE
%right CONS    // 우결합: 1 :: 2 :: [] = 1 :: (2 :: [])
```

`%right CONS`로 우결합을 명시합니다. 이게 없으면 기본적으로 좌결합이 되어서 타입 에러가 발생해요.

[화면: 우선순위 표]

Cons의 위치도 중요합니다. 비교 연산자보다는 낮고, 산술 연산자보다는 높아요.

```
1 + 2 :: [3]  →  (1 + 2) :: [3]  →  [3, 3]
```

산술이 먼저 계산되어야 의미가 맞죠.

[화면: 문법 규칙]

이제 문법 규칙을 추가합니다.

```fsharp
Expr:
    // ... 기존 규칙들 ...
    | Expr CONS Expr                 { Cons($1, $3) }

Atom:
    // ... 기존 규칙들 ...
    | LBRACKET RBRACKET                      { EmptyList }
    | LBRACKET Expr RBRACKET                 { List([$2]) }
    | LBRACKET Expr COMMA ExprList RBRACKET  { List($2 :: $4) }
```

[화면: 각 규칙 설명]

- `[]` → `EmptyList`
- `[1]` → `List([Number 1])`
- `[1, 2, 3]` → `List([Number 1; Number 2; Number 3])`

`ExprList` 비단말은 튜플에서 이미 정의했던 걸 재사용합니다. 콤마로 구분된 표현식 목록이죠.

[화면: AST 출력 테스트]

파싱 결과를 확인해볼까요?

```bash
$ dotnet run --project FunLang -- --emit-ast --expr "1 :: 2 :: []"
Cons (Number 1, Cons (Number 2, EmptyList))
```

우결합이 명확히 드러나죠! `Cons`가 오른쪽부터 중첩되어 있습니다.

### 섹션 5: Evaluator - 리스트 평가 로직 (10:30)

[화면: Eval.fs 파일]

이제 가장 재미있는 부분, 평가 로직을 구현해봅시다.

[화면: EmptyList 평가]

**빈 리스트**는 간단합니다.

```fsharp
| EmptyList ->
    ListValue []
```

빈 `ListValue`를 반환하면 끝!

[화면: List 리터럴 평가]

**리스트 리터럴**도 직관적입니다.

```fsharp
| List exprs ->
    let values = List.map (eval env) exprs
    ListValue values
```

각 표현식을 순서대로 평가하고, 결과를 `ListValue`로 감쌉니다.

[화면: Cons 평가 - 핵심]

**Cons 연산자**가 핵심입니다!

```fsharp
| Cons (headExpr, tailExpr) ->
    let headVal = eval env headExpr
    match eval env tailExpr with
    | ListValue tailVals -> ListValue (headVal :: tailVals)
    | _ -> failwith "Type error: cons (::) requires list as second argument"
```

[화면: 단계별 설명 애니메이션]

1. head 표현식을 평가 → `headVal` (어떤 값이든 가능)
2. tail 표현식을 평가 → 반드시 `ListValue`여야 함
3. F#의 `::` 연산자로 실제 prepend 수행
4. 결과를 `ListValue`로 감싸서 반환

만약 tail이 리스트가 아니면 타입 에러를 발생시킵니다.

[화면: 터미널 - 타입 에러 데모]

```bash
$ dotnet run --project FunLang -- --expr "1 :: 2"
Error: Type error: cons (::) requires list as second argument
```

정확하게 에러 처리되는 걸 확인할 수 있습니다.

### 섹션 6: 구조적 동등성과 포맷팅 (12:30)

[화면: 동등성 연산자 확장]

리스트도 다른 값들처럼 비교할 수 있어야겠죠?

```fsharp
| Equal (left, right) ->
    match eval env left, eval env right with
    // ... 기존 케이스들 ...
    | ListValue l, ListValue r -> BoolValue (l = r)
    | _ -> failwith "Type error: = requires operands of same type"

| NotEqual (left, right) ->
    match eval env left, eval env right with
    // ... 기존 케이스들 ...
    | ListValue l, ListValue r -> BoolValue (l <> r)
    | _ -> failwith "Type error: <> requires operands of same type"
```

[화면: F# 구조적 동등성 설명]

여기서 F#의 강력한 기능이 빛을 발합니다! F#의 discriminated union은 자동으로 **구조적 동등성**을 지원해요.

`l = r`만 써도 중첩된 리스트까지 재귀적으로 비교됩니다. 직접 구현할 필요가 없어요!

[화면: formatValue 함수]

마지막으로 리스트를 예쁘게 출력하는 로직입니다.

```fsharp
let rec formatValue (v: Value) : string =
    match v with
    // ... 기존 케이스들 ...
    | ListValue values ->
        let formattedElements = List.map formatValue values
        sprintf "[%s]" (String.concat ", " formattedElements)
```

[화면: 출력 예시]

각 요소를 재귀적으로 포맷팅하고, 콤마로 연결해서 대괄호로 감쌉니다.

```
[1, 2, 3]
[[1, 2], [3, 4]]  ← 중첩 리스트도 올바르게 출력
```

### 섹션 7: 실전 테스트 - 다양한 예제 (14:00)

[화면: 터미널 - 라이브 데모]

자, 이제 실제로 작동하는지 확인해볼까요?

**기본 리스트 리터럴:**

```bash
$ dotnet run --project FunLang -- --expr "[]"
[]

$ dotnet run --project FunLang -- --expr "[1, 2, 3]"
[1, 2, 3]

$ dotnet run --project FunLang -- --expr "[true, false]"
[true, false]
```

**Cons 연산자:**

```bash
$ dotnet run --project FunLang -- --expr "1 :: [2, 3]"
[1, 2, 3]

$ dotnet run --project FunLang -- --expr "1 :: 2 :: 3 :: []"
[1, 2, 3]
```

모두 동일한 결과! 리스트 리터럴은 사실 Cons의 **syntactic sugar**예요.

**중첩 리스트:**

```bash
$ dotnet run --project FunLang -- --expr "[[1, 2], [3, 4]]"
[[1, 2], [3, 4]]
```

**동등성 비교:**

```bash
$ dotnet run --project FunLang -- --expr "[1, 2] = [1, 2]"
true

$ dotnet run --project FunLang -- --expr "[1, 2] <> [1, 2, 3]"
true
```

**튜플과 조합:**

```bash
$ dotnet run --project FunLang -- --expr "[(1, 2), (3, 4)]"
[(1, 2), (3, 4)]
```

튜플의 리스트! 타입이 섞이지 않는 한 자유롭게 조합 가능합니다.

**산술과 우선순위:**

```bash
$ dotnet run --project FunLang -- --expr "1 + 2 :: [3]"
[3, 3]
```

`1 + 2`가 먼저 계산되어 `3 :: [3]` = `[3, 3]`

**Let 바인딩과 조합:**

```bash
$ dotnet run --project FunLang -- --expr "let xs = [1, 2, 3] in xs"
[1, 2, 3]

$ dotnet run --project FunLang -- --expr "let x = 1 in x :: [2, 3]"
[1, 2, 3]
```

**함수와 조합:**

```bash
$ dotnet run --project FunLang -- --expr "let f = fun x -> x :: [] in f 42"
[42]
```

단일 요소를 리스트로 감싸는 함수!

### 섹션 8: 문법적 설탕 (Syntactic Sugar) 개념 (16:00)

[화면: Syntactic Sugar 설명]

여기서 중요한 개념 하나를 짚고 넘어가겠습니다. **Syntactic Sugar**예요.

`[1, 2, 3]`과 `1 :: 2 :: 3 :: []`는 완전히 동일한 의미입니다. 리스트 리터럴은 Cons 연산자의 "문법적 설탕"이에요.

[화면: 비교 다이어그램]

- **표면 문법** (Surface Syntax): `[1, 2, 3]` - 사람이 쓰기 편함
- **핵심 문법** (Core Syntax): `1 :: 2 :: 3 :: []` - 의미가 명확함

파서는 표면 문법을 받아서 AST로 변환하지만, 평가기는 어느 쪽이든 동일하게 처리합니다.

[화면: AST 비교]

```bash
$ dotnet run --project FunLang -- --emit-ast --expr "[1, 2, 3]"
List [Number 1; Number 2; Number 3]

$ dotnet run --project FunLang -- --emit-ast --expr "1 :: 2 :: 3 :: []"
Cons (Number 1, Cons (Number 2, Cons (Number 3, EmptyList)))
```

AST는 다르지만, 평가 결과는 같습니다!

이런 설계는 언어를 **계층적으로** 만들 수 있게 해줍니다. 핵심 기능은 최소한으로 유지하고, 편의 문법은 그 위에 추가하는 거죠.

### 섹션 9: 연산자 우선순위 전체 정리 (17:00)

[화면: 우선순위 표]

마지막으로 FunLang의 전체 연산자 우선순위를 정리해볼까요?

| 우선순위 | 연산자 | 결합성 | 설명 |
|----------|--------|--------|------|
| 1 (낮음) | `||` | 좌결합 | 논리 OR |
| 2 | `&&` | 좌결합 | 논리 AND |
| 3 | `=`, `<>`, `<`, `>`, `<=`, `>=` | 비결합 | 비교 |
| 4 | `::` | **우결합** | Cons |
| 5 | `+`, `-` | 좌결합 | 덧셈, 뺄셈 |
| 6 | `*`, `/` | 좌결합 | 곱셈, 나눗셈 |
| 7 | 단항 `-` | - | 부정 |
| 8 (높음) | 함수 호출 | 좌결합 | `f x` |

[화면: 결합성 강조]

대부분의 연산자는 **좌결합**이지만, Cons만 유일하게 **우결합**입니다. 이건 리스트의 재귀적 구조 때문이에요.

비교 연산자는 **비결합**이라 `1 < 2 < 3` 같은 체이닝이 불가능합니다.

## 아웃트로 (17:30)

자, 오늘은 여기까지입니다!

[화면: 요약 슬라이드]

오늘 우리가 구현한 내용을 요약하면:

1. **리스트의 재귀적 구조** - head와 tail의 조합
2. **Cons 연산자 `::`** - 우결합, 리스트의 핵심
3. **Syntactic Sugar** - `[1,2,3]`은 `1::2::3::[]`의 설탕
4. **구조적 동등성** - F#의 자동 지원 덕분에 쉽게 구현
5. **연산자 우선순위** - `%right CONS`로 우결합 선언

[화면: 파일 변경 요약]

변경된 파일:
- `Ast.fs` - EmptyList, List, Cons, ListValue
- `Lexer.fsl` - LBRACKET, RBRACKET, CONS
- `Parser.fsy` - %right CONS, 리스트 문법 규칙
- `Eval.fs` - 리스트 평가, formatValue

[화면: 다음 에피소드 예고]

다음 영상에서는 리스트를 **실제로 활용**하는 방법을 배워볼 거예요. 패턴 매칭으로 리스트를 분해하고, 재귀 함수로 리스트를 순회하는 방법을 다룰 예정입니다.

특히 `match` 표현식으로 `head :: tail` 패턴을 매칭하는 강력한 기법을 소개할 거예요. 함수형 프로그래밍의 진수를 맛볼 수 있을 겁니다!

[화면: 테스트 권장]

영상을 보신 후에는 직접 코드를 작성해보시고, 테스트도 돌려보세요!

```bash
make -C tests                      # fslit 테스트
dotnet run --project FunLang.Tests # Expecto 테스트
```

[화면: 클로징]

구독과 좋아요는 큰 힘이 됩니다! 질문이나 피드백은 댓글로 남겨주세요.

다음 영상에서 만나요. 감사합니다!

## 핵심 키워드

- 리스트 (List)
- Cons 연산자 (Cons operator ::)
- 재귀적 자료구조 (Recursive data structure)
- Head와 Tail (Head and Tail)
- 우결합 (Right-associative)
- Syntactic Sugar (문법적 설탕)
- ListValue
- 구조적 동등성 (Structural equality)
- 연산자 우선순위 (Operator precedence)
- %right 선언
- fsyacc
- F# 리스트
- 함수형 프로그래밍 (Functional programming)
- 동종 컬렉션 (Homogeneous collection)
- 가변 길이 자료구조
