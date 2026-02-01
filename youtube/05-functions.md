# EP05: Functions & Abstraction - 함수와 클로저로 Turing-complete 달성

## 영상 정보
- **예상 길이**: 18-20분
- **난이도**: 중급
- **필요 사전 지식**: EP01-04 시청 (특히 let 바인딩과 조건문)

## 인트로 (0:00)

안녕하세요, 여러분! 오늘은 정말 특별한 에피소드입니다.

지금까지 우리는 산술 연산, 변수 바인딩, 조건문까지 구현했는데요. 오늘 구현할 기능이 들어가면 우리 FunLang은 드디어 **Turing-complete 언어**가 됩니다.

무슨 뜻이냐고요? 이론적으로 모든 계산 가능한 문제를 풀 수 있다는 뜻입니다. Python이나 JavaScript로 할 수 있는 모든 계산을 우리 언어로도 할 수 있게 되는 거죠!

[화면: "Turing-complete" 텍스트와 함께 체크마크 애니메이션]

오늘 다룰 내용은:
- 일급 함수 (First-class functions)
- 람다 표현식과 클로저
- 재귀 함수
- 커링 (Currying)

시작해볼까요!

## 본문

### 섹션 1: 왜 함수가 필요한가? (1:30)

지금까지 우리 언어로는 이런 코드를 짤 수 없었습니다:

[화면: 코드 예시]
```
let double = fun x -> x * 2 in
double 7
```

함수를 값처럼 다룰 수 없었거든요. 하지만 함수형 프로그래밍에서 함수는 **일급 시민(first-class citizen)**입니다.

[화면: "First-class" 개념 다이어그램 - 함수를 변수에 저장, 인자로 전달, 반환값으로 사용]

숫자나 불린처럼 함수도:
- 변수에 바인딩할 수 있고
- 다른 함수의 인자로 전달할 수 있고
- 함수의 반환값이 될 수 있습니다

이게 함수형 프로그래밍의 핵심이죠!

### 섹션 2: AST 확장 - 함수도 값이다 (3:00)

먼저 타입부터 확장해야 합니다. 함수도 값이니까요.

[화면: FunLang/Ast.fs 파일]

```fsharp
and Value =
    | IntValue of int
    | BoolValue of bool
    | FunctionValue of param: string * body: Expr * closure: Env

and Env = Map<string, Value>
```

여기서 `FunctionValue`를 주목해주세요. 세 가지 정보를 담고 있습니다:
- `param`: 매개변수 이름
- `body`: 함수 본문
- `closure`: 클로저 환경 (잠시 후 설명!)

[화면: 세 요소를 하이라이트]

`Value`와 `Env`가 서로를 참조하니까 `and` 키워드로 상호 재귀 타입을 만듭니다. F#의 멋진 기능이죠!

이제 표현식 타입도 확장합니다:

```fsharp
type Expr =
    // ... 기존 케이스들 ...
    | Lambda of param: string * body: Expr
    | App of func: Expr * arg: Expr
    | LetRec of name: string * param: string * body: Expr * inExpr: Expr
```

[화면: 각 케이스 옆에 예시 코드]
- `Lambda`: `fun x -> x + 1` (익명 함수)
- `App`: `f 5` (함수 호출)
- `LetRec`: `let rec fact n = ...` (재귀 함수)

### 섹션 3: Lexer와 Parser - 새로운 문법 (5:00)

새로운 키워드를 추가해야겠죠?

[화면: FunLang/Lexer.fsl]

```fsharp
| "fun"         { FUN }
| "rec"         { REC }
| "->"          { ARROW }
```

여기서 중요한 건, `->` 규칙이 `-` 규칙보다 **먼저** 와야 한다는 점입니다!

[화면: 잘못된 순서와 올바른 순서 비교]

그렇지 않으면 `fun x -> x + 1`이 `fun x MINUS GT x + 1`로 잘못 토큰화됩니다. fslex는 위에서부터 매칭하니까 순서가 중요해요.

Parser는 이렇게 확장합니다:

[화면: FunLang/Parser.fsy]

```fsharp
| LET REC IDENT IDENT EQUALS Expr IN Expr  { LetRec($3, $4, $6, $8) }
| FUN IDENT ARROW Expr                     { Lambda($2, $4) }
```

간단하죠? 하지만 여기서 문제가 하나 생깁니다...

### 섹션 4: 함수 호출 vs 뺄셈 - 문법 충돌 해결 (6:30)

함수 호출은 나란히 쓰기(juxtaposition)로 표현합니다: `f 5`

그런데 이 코드를 봐주세요:

[화면: 큰 글씨로 "f - 1"]

이게 뭘까요?
- `f`에서 `1`을 뺀 건가요?
- `f`에 `-1`을 전달한 건가요?

[화면: 두 가지 해석 시각화]

이걸 해결하려면 문법 구조를 잘 설계해야 합니다.

[화면: 문법 계층 다이어그램]

```
Expr → ... → Factor → AppExpr → Atom
```

여기서 핵심은 **Atom** 개념입니다:

```fsharp
// Factor: 단항 마이너스 포함
Factor:
    | MINUS Factor       { Negate($2) }
    | AppExpr            { $1 }

// AppExpr: 함수 호출 (왼쪽 결합)
AppExpr:
    | AppExpr Atom       { App($1, $2) }
    | Atom               { $1 }

// Atom: 연산자가 없는 기본 표현식
Atom:
    | NUMBER | IDENT | TRUE | FALSE
    | LPAREN Expr RPAREN { $2 }
```

이 구조 덕분에:
- `f - 1` → 뺄셈 (Factor에서 처리)
- `f (-1)` → 함수 호출 (괄호로 명시)
- `f 1 2` → 커링 (연쇄 호출)

[화면: 각 예시의 AST 시각화]

명확하게 구분됩니다!

### 섹션 5: 클로저의 핵심 - 환경 캡처 (9:00)

이제 평가 로직입니다. 먼저 람다부터 봅시다.

[화면: FunLang/Eval.fs - Lambda 케이스]

```fsharp
| Lambda (param, body) ->
    FunctionValue (param, body, env)
```

딱 세 줄인데, 여기에 클로저의 모든 마법이 담겨 있습니다.

`env`는 **현재 환경**입니다. 람다가 정의되는 시점의 모든 변수 바인딩이죠. 함수 값이 이 환경을 통째로 저장합니다. 이게 클로저입니다!

[화면: 클로저 개념 시각화 - 함수가 주변 환경을 "닫아버린다"]

예를 들어볼까요?

```
let x = 10 in
let f = fun y -> x + y in
let x = 100 in
f 5
```

[화면: 단계별 실행 과정]

1. `x = 10` 바인딩
2. `f` 정의 시점에 **클로저가 x=10을 캡처**
3. `x = 100`으로 섀도잉 (새 바인딩)
4. `f 5` 호출 → 결과는?

정답은 **15**입니다! `f`의 클로저에 있는 `x = 10`을 사용하기 때문이죠.

[화면: "Lexical Scope" vs "Dynamic Scope" 비교 표]

이게 렉시컬 스코프입니다. 정의 시점의 환경을 사용하는 거죠. 대부분의 현대 언어가 이 방식을 사용합니다.

만약 동적 스코프였다면 호출 시점의 환경을 보니까 결과가 115가 됐을 겁니다. 하지만 그건 예측하기 어렵고 버그가 많아요.

### 섹션 6: 함수 호출 - 클로저 환경 사용 (11:30)

이제 함수를 호출해봅시다.

[화면: App 케이스 코드]

```fsharp
| App (funcExpr, argExpr) ->
    let funcVal = eval env funcExpr
    match funcVal with
    | FunctionValue (param, body, closureEnv) ->
        let argValue = eval env argExpr
        let augmentedClosureEnv =
            match funcExpr with
            | Var name -> Map.add name funcVal closureEnv
            | _ -> closureEnv
        let callEnv = Map.add param argValue augmentedClosureEnv
        eval callEnv body
    | _ -> failwith "Type error: attempted to call non-function"
```

여기서 중요한 포인트가 세 가지입니다:

[화면: 세 단계로 나누어 하이라이트]

**첫째**, 인자는 **호출 시점 환경**에서 평가합니다.
```fsharp
let argValue = eval env argExpr
```

**둘째**, 함수 본문은 **클로저 환경**을 확장해서 평가합니다.
```fsharp
let callEnv = Map.add param argValue augmentedClosureEnv
eval callEnv body
```

호출 시점 환경이 아니에요! 클로저 환경입니다. 이게 핵심이죠.

**셋째**, 재귀 함수를 위해 자기 자신을 클로저에 추가합니다.
```fsharp
let augmentedClosureEnv =
    match funcExpr with
    | Var name -> Map.add name funcVal closureEnv
    | _ -> closureEnv
```

[화면: 재귀 함수 실행 과정 애니메이션]

이 트릭 덕분에 함수가 자기 자신을 호출할 수 있게 됩니다!

### 섹션 7: LetRec - 재귀 함수 정의 (13:30)

재귀 함수 정의는 간단합니다:

[화면: LetRec 케이스]

```fsharp
| LetRec (name, param, funcBody, inExpr) ->
    let funcVal = FunctionValue (param, funcBody, env)
    let recEnv = Map.add name funcVal env
    eval recEnv inExpr
```

함수를 환경에 바인딩하고, 그 환경에서 나머지 표현식을 평가합니다.

실제로 작동하는지 볼까요?

[화면: 터미널 실행]

```bash
$ dotnet run --project FunLang -- --expr "let rec fact n = if n <= 1 then 1 else n * fact (n - 1) in fact 5"
120
```

[화면: 결과 강조 - 120]

완벽합니다! 팩토리얼이 제대로 계산됐네요.

피보나치도 해볼까요?

```bash
$ dotnet run --project FunLang -- --expr "let rec fib n = if n <= 1 then n else fib (n - 1) + fib (n - 2) in fib 10"
55
```

[화면: 피보나치 수열 시각화와 함께 결과 표시]

완벽하게 작동합니다!

### 섹션 8: 커링 - 다중 매개변수의 우아한 해결책 (15:00)

우리 언어에서 함수는 매개변수를 하나만 받습니다. 그럼 두 개를 받으려면?

[화면: "Currying" 제목]

커링을 사용합니다! 함수를 반환하는 함수를 만드는 거죠:

```
let add = fun x -> fun y -> x + y in
add 3 4
```

[화면: 실행 과정 애니메이션]

이게 어떻게 동작하는지 봅시다:

1. `add 3` 평가 → `fun y -> 3 + y` 반환 (클로저에 x=3 저장)
2. `(add 3) 4` 평가 → `3 + 4` → `7`

[화면: 각 단계에서 클로저 상태 표시]

실제로 해볼까요?

```bash
$ dotnet run --project FunLang -- --expr "let add = fun x -> fun y -> x + y in add 3 4"
7
```

부분 적용(partial application)도 가능합니다:

```bash
$ dotnet run --project FunLang -- --expr "let add = fun x -> fun y -> x + y in let add5 = add 5 in add5 10"
15
```

[화면: "add5"가 클로저를 캡처한 모습]

`add5`는 `add 5`를 부분 적용한 새 함수입니다. 클로저에 `x = 5`를 저장하고 있죠!

### 섹션 9: 실전 예제 - 고차 함수 패턴 (16:30)

함수형 프로그래밍의 전형적인 패턴을 하나 더 봅시다.

[화면: "makeAdder" 패턴]

```bash
$ dotnet run --project FunLang -- --expr "let makeAdder = fun x -> fun y -> x + y in let add5 = makeAdder 5 in add5 3"
8
```

`makeAdder`는 함수를 만드는 함수입니다. 이런 걸 **고차 함수(higher-order function)**라고 하죠.

[화면: 고차 함수 개념 다이어그램]

함수를 인자로 받거나 함수를 반환하는 함수를 말합니다.

클로저의 진짜 위력은 이런 패턴에서 나옵니다:

```
let a = 1 in
let b = 2 in
let f = fun x -> a + b + x in
f 3
```

[화면: 실행 결과 6]

`f`가 두 개의 외부 변수 `a`와 `b`를 모두 캡처했네요! 클로저의 강력함입니다.

### 섹션 10: Turing-complete 달성! (17:30)

자, 이제 우리가 무엇을 달성했는지 정리해봅시다.

[화면: 체크리스트 애니메이션]

✓ 일급 함수 (First-class functions)
✓ 람다 표현식 (Lambda expressions)
✓ 클로저 (Closures)
✓ 재귀 함수 (Recursive functions)
✓ 커링 (Currying)

그리고 가장 중요한 건...

[화면: 큰 텍스트로 "Turing-complete!" 애니메이션]

우리 FunLang은 이제 **Turing-complete** 언어입니다!

재귀 함수만 있으면 모든 종류의 루프를 표현할 수 있고, 함수와 클로저로 복잡한 데이터 구조를 시뮬레이션할 수 있습니다.

이론적으로 Python이나 JavaScript로 할 수 있는 모든 계산을 우리 언어로도 할 수 있게 된 거죠!

[화면: 다양한 예제 코드들이 빠르게 지나가는 몽타주]
- factorial
- fibonacci
- nested closures
- curried functions

물론 실용적으로는 아직 멀었죠. 리스트도 없고, 입출력도 없고... 하지만 **계산 능력의 본질**은 모두 갖췄습니다.

## 아웃트로 (18:30)

오늘 우리는 정말 큰 이정표를 달성했습니다!

[화면: 여정 타임라인]
- EP01: 파싱과 산술
- EP02: 변수와 let 바인딩
- EP03: REPL
- EP04: 조건문
- **EP05: Turing-complete!** ← 오늘!

함수형 프로그래밍의 핵심인 클로저와 고차 함수를 구현했고, 재귀를 통해 우리 언어를 Turing-complete하게 만들었습니다.

[화면: 코드 통계]
- 새로운 토큰: 3개 (FUN, REC, ARROW)
- 새로운 AST 노드: 3개 (Lambda, App, LetRec)
- 새로운 Value 타입: 1개 (FunctionValue)
- 구현한 테스트: 13개

하지만 가장 중요한 건 **개념**입니다. 클로저가 어떻게 작동하는지, 렉시컬 스코프가 무엇인지, 커링이 왜 유용한지 이해하셨다면 성공입니다!

다음 에피소드에서는 아마도... 리스트? 패턴 매칭? 타입 시스템? 아직 미정입니다만, 재밌는 걸로 준비하겠습니다!

[화면: GitHub 링크와 구독 버튼]

오늘도 시청해주셔서 감사합니다. 질문이나 의견은 댓글로 남겨주세요. 구독과 좋아요는 큰 힘이 됩니다!

다음 에피소드에서 만나요!

## 핵심 키워드

- Turing-complete
- First-class functions (일급 함수)
- Lambda expressions (람다 표현식)
- Closures (클로저)
- Lexical scope (렉시컬 스코프)
- Recursive functions (재귀 함수)
- Currying (커링)
- Higher-order functions (고차 함수)
- Partial application (부분 적용)
- Function application (함수 호출)
- fslex/fsyacc
- F# language implementation
- Interpreter design
