# EP09: Prelude - 자체 호스팅 표준 라이브러리

## 영상 정보
- **예상 길이**: 18-20분
- **난이도**: 중급
- **필요 사전 지식**: EP01-08 시청 (특히 EP05 고차함수, EP06 리스트, EP07 패턴매칭)

## 인트로 (0:00)

여러분, 드디어 v3.0의 마지막 에피소드입니다! 오늘은 정말 특별한 순간을 함께하게 될 거예요.

[화면: FunLang REPL에서 `map`, `filter`, `fold` 같은 익숙한 함수들이 동작하는 모습]

우리가 8개 에피소드에 걸쳐 만들어온 FunLang이 드디어 **자기 자신의 표준 라이브러리를 작성할 수 있을 만큼** 성숙해졌습니다. 이게 얼마나 대단한 일인지 아시나요?

이건 마치... 요리사가 자기가 만든 칼로 요리를 하는 것과 같습니다. 언어가 자기 자신을 표현할 수 있다는 건, 그 언어가 진짜로 **완전하다**는 증거거든요.

[화면: "Self-Hosted Standard Library" 타이틀과 Prelude.fun 파일 미리보기]

오늘은 Prelude - FunLang의 표준 라이브러리를 구축합니다. map, filter, fold 같은 고차 함수들을 **FunLang 자체로** 작성하고, 인터프리터가 시작할 때 자동으로 로드하는 시스템을 만들 거예요.

이건 단순한 기능 추가가 아닙니다. 언어가 스스로를 증명하는 순간이에요. Let's go!

## 본문

### 섹션 1: Self-Hosting이란? (1:30)

먼저 "Self-Hosted Standard Library"가 뭔지부터 얘기해볼까요?

[화면: 일반적인 언어 구조 다이어그램 - C로 작성된 Python 인터프리터, 내장 함수는 C로 구현]

보통 프로그래밍 언어들은 이렇게 동작합니다. 인터프리터나 컴파일러는 C나 다른 저수준 언어로 작성되고, 표준 라이브러리의 핵심 함수들도 그 언어로 구현되죠. 예를 들어 Python의 `map`, `filter` 같은 함수들은 실제로는 C로 작성되어 있어요.

[화면: FunLang 구조 다이어그램 - F#로 작성된 인터프리터, 표준 라이브러리는 FunLang으로 구현]

하지만 우리는 다릅니다. FunLang의 표준 라이브러리는... **FunLang 자체로** 작성됩니다!

인터프리터는 F#로 만들었지만, 표준 라이브러리는 우리가 만든 언어 자체로 작성하는 거예요. 이걸 **dogfooding**이라고도 하는데, 자기가 만든 걸 자기가 써보는 거죠.

**왜 이게 중요할까요?**

첫째, **언어 능력의 증명**입니다. FunLang이 충분히 표현력이 있다는 걸 보여주는 거예요. 재귀, 고차 함수, 패턴 매칭 - EP01부터 EP08까지 쌓아온 모든 기능이 실전에서 동작한다는 증거입니다.

[화면: Turing-complete 체크리스트 - 재귀 ✓, 조건문 ✓, 고차함수 ✓, Self-hosting ✓]

둘째, **일관성**이에요. 사용자가 작성하는 코드와 표준 라이브러리가 똑같은 문법을 사용합니다. 마법 같은 특수 함수가 없어요. 모든 게 투명하죠.

셋째, **교육적 가치**입니다. 표준 라이브러리가 어떻게 동작하는지 직접 볼 수 있어요. "map이 어떻게 구현되어 있을까?" 궁금하면 Prelude.fun 파일을 열어보면 됩니다!

### 섹션 2: Prelude.fun - 표준 라이브러리 소스 (4:00)

자, 이제 실제 코드를 볼까요?

[화면: Prelude.fun 파일 전체 구조 - 중첩된 let...in 체인]

Prelude.fun은 FunLang으로 작성된 표준 라이브러리 소스입니다. 총 11개의 함수를 제공하는데, 모두 중첩된 `let...in` 구조로 연결되어 있어요.

```funlang
let rec map f = fun xs -> ... in
let rec filter pred = fun xs -> ... in
let rec fold f = fun acc -> fun xs -> ... in
...
0
```

맨 마지막에 `0`이 있죠? 이건 더미 값이에요. 나중에 설명하겠지만, Prelude 로딩 시 이 값은 버려지고 바인딩만 추출됩니다.

**고차 함수 3총사부터 볼까요?**

[화면: map 함수 구현 하이라이트]

**map** - 리스트의 각 요소를 변환합니다.

```funlang
let rec map f = fun xs ->
    match xs with
    | [] -> []
    | h :: t -> (f h) :: (map f t)
in
```

재귀적이고 명확하죠? 빈 리스트면 빈 리스트 반환. 아니면 head에 함수 적용하고 tail을 재귀 호출. EP05에서 배운 고차 함수와 EP07의 패턴 매칭이 만나는 순간입니다.

[화면: filter 함수 구현 하이라이트]

**filter** - 조건을 만족하는 요소만 남깁니다.

```funlang
let rec filter pred = fun xs ->
    match xs with
    | [] -> []
    | h :: t -> if pred h then h :: (filter pred t) else filter pred t
in
```

조건자(predicate) 함수를 받아서, head가 조건을 만족하면 결과에 포함하고, 아니면 스킵하는 거예요.

[화면: fold 함수 구현 하이라이트]

**fold** - 리스트를 누적 연산으로 줄입니다.

```funlang
let rec fold f = fun acc -> fun xs ->
    match xs with
    | [] -> acc
    | h :: t -> fold f (f acc h) t
in
```

Left fold 방식이에요. 누적값 acc와 현재 요소 h를 함수 f에 넘겨서 새 누적값을 만들고, tail을 계속 처리합니다.

이 세 함수만 있으면 리스트로 할 수 있는 거의 모든 연산을 표현할 수 있어요!

[화면: 라이브 데모 - map, filter, fold 예제 실행]

```bash
$ dotnet run --project FunLang -- -e 'map (fun x -> x * 2) [1, 2, 3]'
[2, 4, 6]

$ dotnet run --project FunLang -- -e 'filter (fun x -> x > 1) [1, 2, 3]'
[2, 3]

$ dotnet run --project FunLang -- -e 'fold (fun a -> fun b -> a + b) 0 [1, 2, 3]'
6
```

완벽하게 동작합니다!

### 섹션 3: 리스트 유틸리티 함수들 (7:30)

고차 함수 외에도 유용한 유틸리티들이 있어요.

[화면: length, reverse, append 함수들 코드]

**length** - 리스트 길이를 센다. 간단한 재귀죠.

```funlang
let rec length xs =
    match xs with
    | [] -> 0
    | _ :: t -> 1 + (length t)
in
```

**reverse** - 리스트를 뒤집는데, 효율적인 tail-recursive 방식입니다.

```funlang
let reverse = fun xs ->
    let rec rev_acc acc = fun ys ->
        match ys with
        | [] -> acc
        | h :: t -> rev_acc (h :: acc) t
    in
    rev_acc [] xs
in
```

누적 리스트(accumulator)를 사용해서 요소를 하나씩 앞에 추가하는 거예요. `[1,2,3]`이 `[3,2,1]`이 되는 과정이 아름답습니다.

[화면: reverse 실행 과정 시각화 - rev_acc [] [1,2,3] → rev_acc [1] [2,3] → ...]

**append** - 두 리스트를 연결합니다.

```funlang
let rec append xs = fun ys ->
    match xs with
    | [] -> ys
    | h :: t -> h :: (append t ys)
in
```

그리고 **hd**와 **tl** - 리스트의 head와 tail을 추출하는 함수들도 있어요.

[화면: 라이브 데모 - 유틸리티 함수들 실행]

```bash
$ dotnet run --project FunLang -- -e 'length [1, 2, 3, 4, 5]'
5

$ dotnet run --project FunLang -- -e 'reverse [1, 2, 3]'
[3, 2, 1]

$ dotnet run --project FunLang -- -e 'append [1, 2] [3, 4]'
[1, 2, 3, 4]
```

### 섹션 4: Combinators - 함수형 프로그래밍의 기초 (9:30)

마지막으로 combinator 세 개를 봅시다.

[화면: id, const, compose 함수들]

**id** - 항등 함수. 받은 걸 그대로 돌려줍니다.

```funlang
let id = fun x -> x
```

"이게 무슨 쓸모가 있어?"라고 생각할 수 있지만, 함수형 프로그래밍에서 placeholder로 엄청 유용해요.

**const** - 상수 함수. 두 인자 중 첫 번째만 반환합니다.

```funlang
let const = fun x -> fun y -> x
```

**compose** - 함수 합성. 두 함수를 연결합니다.

```funlang
let compose = fun f -> fun g -> fun x -> f (g x)
```

`g`를 먼저 적용하고 그 결과에 `f`를 적용하는 거죠.

[화면: compose 예제 실행]

```bash
$ dotnet run --project FunLang -- -e 'let f = fun x -> x * 2 in let g = fun x -> x + 1 in (compose f g) 5'
12
```

`(compose f g) 5` = `f (g 5)` = `f 6` = `12`. 완벽합니다!

### 섹션 5: 로딩 인프라 - evalToEnv 패턴 (11:00)

자, 이제 핵심 질문입니다. 이 Prelude.fun 파일을 어떻게 인터프리터에 로드할까요?

[화면: Prelude.fs 파일 오픈]

일반적인 `eval` 함수는 표현식의 **값**을 반환합니다. 하지만 우리가 원하는 건 **바인딩들**이에요. map, filter, fold 같은 함수들을 환경에 추가해야 하거든요.

이걸 위해 **evalToEnv**라는 특별한 패턴을 사용합니다.

[화면: evalToEnv 함수 코드]

```fsharp
let rec private evalToEnv (env: Env) (expr: Expr) : Env =
    match expr with
    | Let (name, binding, body) ->
        let value = eval env binding
        let extendedEnv = Map.add name value env
        evalToEnv extendedEnv body
    | LetRec (name, param, funcBody, inExpr) ->
        let funcVal = FunctionValue (param, funcBody, env)
        let extendedEnv = Map.add name funcVal env
        evalToEnv extendedEnv inExpr
    | _ ->
        // Base case: return accumulated environment
        env
```

**동작 방식을 볼까요?**

[화면: evalToEnv 실행 과정 시각화]

1. `Let` 또는 `LetRec`를 만나면 바인딩을 평가합니다.
2. 환경을 확장하고 body를 재귀적으로 처리합니다.
3. 최종 표현식(예: 맨 끝의 `0`)을 만나면 누적된 환경을 반환합니다.

```
let map = ... in          → env' = env + {map = <func>}
let filter = ... in       → env'' = env' + {filter = <func>}
let fold = ... in         → env''' = env'' + {fold = <func>}
...
0                         → env''' 반환 (0은 버림!)
```

마지막 `0`은 완전히 무시됩니다. 우리가 원하는 건 그동안 모은 바인딩들이거든요!

### 섹션 6: loadPrelude - 자동 로딩 시스템 (13:00)

이제 `evalToEnv`를 실제로 사용하는 `loadPrelude` 함수를 봅시다.

[화면: loadPrelude 함수 코드]

```fsharp
let loadPrelude () : Env =
    let preludePath = "Prelude.fun"
    if File.Exists preludePath then
        try
            let source = File.ReadAllText preludePath
            let ast = parse source
            evalToEnv emptyEnv ast
        with ex ->
            eprintfn "Warning: Failed to load Prelude.fun: %s" ex.Message
            emptyEnv
    else
        eprintfn "Warning: Prelude.fun not found, starting with empty environment"
        emptyEnv
```

과정이 명확하죠?

1. Prelude.fun 파일을 읽습니다.
2. 파싱해서 AST로 만듭니다.
3. `evalToEnv`로 바인딩들을 환경에 추출합니다.

**중요한 점**: Graceful degradation! 파일이 없거나 에러가 나도 프로그램이 죽지 않아요. 경고만 출력하고 빈 환경을 반환합니다.

[화면: 에러 처리 시나리오 - Prelude.fun 삭제 후 실행]

이건 개발 중이거나 테스트할 때 유용합니다. Prelude 없이도 인터프리터가 동작하니까요.

### 섹션 7: REPL과 CLI 통합 (14:30)

이제 REPL과 CLI에서 Prelude를 사용하도록 연결해봅시다.

[화면: Repl.fs의 startRepl 함수]

```fsharp
let startRepl () : int =
    printfn "FunLang REPL"
    printfn "Type '#quit' or Ctrl+D to quit."
    printfn ""
    let initialEnv = Prelude.loadPrelude()
    replLoop initialEnv
    0
```

REPL 시작 시 `loadPrelude()`를 호출해서 초기 환경을 만듭니다. 이제 REPL에 들어가는 순간부터 map, filter, fold가 모두 사용 가능해요!

[화면: 라이브 REPL 데모]

```bash
$ dotnet run --project FunLang
FunLang REPL
Type '#quit' or Ctrl+D to quit.

funlang> map (fun x -> x * 2) [1, 2, 3]
[2, 4, 6]
funlang> length [1, 2, 3, 4, 5]
5
funlang> reverse [1, 2, 3]
[3, 2, 1]
```

마법 같지 않나요? 하지만 이건 마법이 아니라 우리가 만든 시스템이에요!

[화면: Program.fs의 CLI 통합 코드]

CLI에서 `--expr` 모드나 파일 실행 모드도 똑같이 Prelude를 로드합니다.

```fsharp
let initialEnv = Prelude.loadPrelude()

elif results.Contains Expr then
    let expr = results.GetResult Expr
    let result = eval initialEnv (parse expr)
    printfn "%s" (formatValue result)
```

어디서든 일관되게 표준 라이브러리를 사용할 수 있어요.

### 섹션 8: 실전 예제 - 함수 조합의 아름다움 (16:00)

이제 진짜 재미있는 부분입니다. 고차 함수들을 조합해봅시다!

[화면: 복합 예제 실행]

**예제 1**: 1보다 큰 수만 남기고 두 배로 만들기

```bash
$ dotnet run --project FunLang -- -e 'map (fun x -> x * 2) (filter (fun x -> x > 1) [1, 2, 3, 4])'
[4, 6, 8]
```

filter로 먼저 `[2, 3, 4]`를 만들고, map으로 두 배 해서 `[4, 6, 8]`이 됩니다.

**예제 2**: 제곱의 합 (sum of squares)

```bash
$ dotnet run --project FunLang -- -e 'fold (fun a -> fun b -> a + b) 0 (map (fun x -> x * x) [1, 2, 3, 4])'
30
```

[화면: 실행 과정 시각화]

- map으로 제곱: `[1, 2, 3, 4]` → `[1, 4, 9, 16]`
- fold로 합산: `1 + 4 + 9 + 16 = 30`

이게 바로 **선언적 프로그래밍**입니다. "어떻게"가 아니라 "무엇을" 하는지 표현하는 거죠. for 루프나 인덱스 없이 말이에요!

**예제 3**: 중첩 리스트 reverse

```bash
$ dotnet run --project FunLang -- -e 'reverse [[1, 2], [3, 4]]'
[[3, 4], [1, 2]]
```

리스트의 리스트도 문제없습니다. Generic하게 동작하거든요!

### 섹션 9: Self-Hosting의 의미 (17:30)

[화면: 전체 시스템 아키텍처 다이어그램]

잠깐 멈춰서 우리가 뭘 만들었는지 되돌아봅시다.

EP01에서는 정수 덧셈만 하는 계산기를 만들었어요. EP02에서 변수를 추가했고, EP03에서 함수를 만들었죠. EP04에서 재귀가 가능해졌고, EP05에서 고차 함수를 배웠습니다. EP06에서 리스트를 추가했고, EP07에서 패턴 매칭으로 강력한 제어 흐름을 얻었습니다. EP08에서 REPL로 상호작용이 가능해졌고...

[화면: v3.0 달성 체크리스트]

그리고 오늘, EP09에서 **언어가 스스로를 표현하는 능력**을 갖추었습니다.

- ✓ Turing-complete 계산 능력
- ✓ 고차 함수와 클로저
- ✓ 재귀와 패턴 매칭
- ✓ Self-hosted standard library

이건 단순한 toy language가 아닙니다. 실제로 유용한 프로그램을 작성할 수 있는 언어예요. Prelude.fun이 그 증거입니다.

[화면: Prelude.fun 파일 - 11개 함수 목록]

| 카테고리 | 함수 | 설명 |
|----------|------|------|
| 고차 함수 | map, filter, fold | 리스트 변환, 필터링, 누적 |
| 리스트 유틸리티 | length, reverse, append | 길이, 뒤집기, 연결 |
| 리스트 접근 | hd, tl | Head, tail |
| Combinators | id, const, compose | 항등, 상수, 합성 |

이 11개 함수는 FunLang으로 작성되었고, FunLang에서 실행되며, FunLang 사용자가 그대로 확장할 수 있습니다.

## 아웃트로 (18:30)

[화면: v3.0 마일스톤 완료 배너]

여러분, 축하합니다! 우리는 9개 에피소드에 걸쳐 완전한 함수형 프로그래밍 언어를 만들었습니다!

**v3.0에서 달성한 것들**:
- ✓ 완전한 언어 구현 (lexer, parser, evaluator)
- ✓ 고차 함수, 재귀, 패턴 매칭
- ✓ 리스트와 튜플 자료구조
- ✓ REPL과 CLI 인터페이스
- ✓ Self-hosted standard library

[화면: FunLang REPL에서 복잡한 함수형 프로그램이 돌아가는 모습]

이제 FunLang은 실제로 사용할 수 있는 언어입니다. 물론 프로덕션 레벨은 아니지만, 학습하고 실험하기에는 완벽해요.

**다음 단계는?**

v4.0에서는 아마도 타입 시스템을 추가할 수 있을 거예요. 또는 모듈 시스템, I/O, 더 많은 자료구조... 가능성은 무한합니다!

하지만 가장 중요한 건, **여러분이 이제 프로그래밍 언어가 어떻게 동작하는지 안다는 것**입니다. Lexer가 뭔지, Parser가 어떻게 AST를 만드는지, Evaluator가 어떻게 코드를 실행하는지. 그리고 언어가 어떻게 자기 자신을 표현할 수 있는지까지요.

[화면: GitHub 링크와 구독 버튼 하이라이트]

전체 소스 코드는 GitHub에 있고, 각 에피소드별로 커밋이 나뉘어 있어서 학습하기 좋습니다. 테스트 코드도 195개가 넘으니까 직접 실험해보세요!

**좋아요와 구독** 잊지 마시고, 댓글로 어떤 언어 기능을 더 추가하고 싶은지 알려주세요.

다음 시리즈에서 만나요. 여러분 모두 즐거운 코딩 되세요!

[화면: "Thanks for watching!" + FunLang 로고]

## 핵심 키워드

- Self-hosted standard library
- Dogfooding
- evalToEnv 패턴
- Prelude
- 고차 함수 (map, filter, fold)
- Tail recursion
- Function composition
- Graceful degradation
- Turing-complete
- 함수형 프로그래밍
- FunLang
- F# 인터프리터
- 표준 라이브러리 자동 로딩
- v3.0 마일스톤
