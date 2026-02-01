# EP04: Control Flow - 조건문과 Boolean 타입

## 영상 정보
- **예상 길이**: 18분
- **난이도**: 초급
- **필요 사전 지식**: EP01-03 시청 (특히 EP03의 변수 바인딩)

## 인트로 (0:00)

안녕하세요! FunLang 튜토리얼 네 번째 에피소드입니다.

지난 시간까지 우리는 계산기에 변수를 추가해서 "let x = 10 in x + 5" 같은 표현식을 만들 수 있게 됐죠. 그런데 아직 뭔가 부족합니다. 프로그램이 상황에 따라 다르게 동작할 수 없거든요.

[화면: 코드 에디터에 "if 5 > 3 then 1 else 2" 입력]

오늘은 우리 언어에 "생각하는 능력"을 추가합니다. Boolean 타입과 if-then-else를 구현하면서, FunLang이 드디어 여러 타입을 다루는 언어로 진화하는 모습을 볼 거예요.

이번 에피소드가 특별한 이유가 있는데요, 지금까지는 모든 표현식이 정수를 반환했지만 이제는 Boolean도 반환할 수 있어야 합니다. 이 변화가 코드 전체에 어떤 영향을 미치는지, 함께 살펴보시죠!

## 본문

### 섹션 1: 왜 Value 타입이 필요한가? (1:30)

자, 먼저 문제 상황부터 봅시다.

[화면: Eval.fs의 기존 코드]

지금까지 우리의 evalExpr 함수는 int를 반환했습니다. 간단하죠.

```fsharp
let rec evalExpr (expr: Expr) : int = ...
```

그런데 Boolean을 추가하면 어떻게 될까요? "true"라는 표현식을 평가하면 뭘 반환해야 하나요? 정수가 아닌데요?

[화면: 세 가지 선택지 표시]

선택지는 세 가지입니다.

**첫 번째, obj 타입으로 반환**
```fsharp
let rec evalExpr (expr: Expr) : obj = ...
```

이건 C#이나 Java에서 쓰는 방식인데요, F#에서는 별로입니다. 타입 안정성이 사라지거든요. 결과를 쓸 때마다 "이게 int인가 bool인가?" 체크하고 캐스팅해야 합니다.

**두 번째, 제네릭**
```fsharp
let rec evalExpr<'T> (expr: Expr) : 'T = ...
```

이것도 문제가 있습니다. 표현식마다 타입이 다른데 어떻게 한 함수로 처리하죠? "5 + 3"은 int를 반환하고 "5 > 3"은 bool을 반환하는데, 타입을 미리 알 수가 없어요.

**세 번째, Discriminated Union**
```fsharp
type Value =
    | IntValue of int
    | BoolValue of bool
```

[화면: Ast.fs에 Value 타입 추가]

이게 F# 스타일입니다! 가능한 타입들을 명시적으로 열거하고, 패턴 매칭으로 안전하게 처리하는 거죠.

이 Value 타입은 "결과는 정수거나 불린이다"라고 컴파일러에게 알려줍니다. 그러면 컴파일러가 모든 경우를 처리했는지 검사해줘요. 타입 안정성과 표현력, 두 마리 토끼를 잡는 겁니다.

### 섹션 2: AST 확장 - 새로운 표현식들 (4:00)

이제 AST에 새 노드들을 추가해봅시다.

[화면: Ast.fs 파일 열기]

```fsharp
type Expr =
    // ... 기존 노드들
    // Phase 4: Control flow
    | Bool of bool            // Boolean 리터럴
    | If of Expr * Expr * Expr  // if condition then expr1 else expr2
    // Phase 4: 비교 연산자
    | Equal of Expr * Expr       // =
    | NotEqual of Expr * Expr    // <>
    | LessThan of Expr * Expr    // <
    // ... 등등
    // Phase 4: 논리 연산자
    | And of Expr * Expr  // &&
    | Or of Expr * Expr   // ||
```

[화면: AST 시각화]

"if 5 > 3 then 1 else 2"는 어떤 AST가 될까요?

```
If(GreaterThan(Number 5, Number 3), Number 1, Number 2)
```

If 노드가 세 개의 자식을 가집니다. 조건식, then 브랜치, else 브랜치. 이 구조가 나중에 evaluator에서 그대로 평가 순서가 되는 거죠.

### 섹션 3: Lexer - 연산자 순서의 함정 (5:30)

이제 Lexer에 새 토큰들을 추가해야 하는데요, 여기 조심해야 할 부분이 있습니다.

[화면: Lexer.fsl]

```fsharp
rule tokenize = parse
    | whitespace+   { tokenize lexbuf }
    | digit+        { NUMBER (Int32.Parse(lexeme lexbuf)) }
    // 키워드는 식별자보다 먼저!
    | "true"        { TRUE }
    | "false"       { FALSE }
    | "if"          { IF }
    // ...
    | ident_start ident_char* { IDENT (lexeme lexbuf) }
```

[화면: 하이라이트 강조]

여기 보세요. "true" 같은 키워드가 식별자 규칙보다 먼저 나와야 합니다. 왜냐고요?

만약 순서를 바꾸면:
```fsharp
| ident_start ident_char* { IDENT (lexeme lexbuf) }
| "true"        { TRUE }
```

"true"를 만나면 식별자 규칙이 먼저 매칭돼서 IDENT("true") 토큰이 되어버립니다. "true"는 키워드가 아니라 변수 이름처럼 취급되는 거죠!

[화면: 연산자 순서]

연산자도 마찬가지입니다.

```fsharp
// 올바른 순서
| "<="          { LE }
| "<"           { LT }

// 잘못된 순서 - 절대 하지 마세요!
| "<"           { LT }
| "<="          { LE }  // 도달 불가!
```

"<=" 입력이 들어오면 먼저 나온 "<"에 매칭되고, "="는 다음 토큰으로 처리됩니다. "<=" 규칙은 절대 실행되지 않아요.

**핵심 원칙: 긴 것을 먼저, 짧은 것을 나중에!**

### 섹션 4: Parser - 우선순위의 세계 (7:30)

이제 파서인데요, 여기가 오늘의 하이라이트입니다.

[화면: Parser.fsy]

```fsharp
%left OR
%left AND
%nonassoc EQUALS LT GT LE GE NE
```

이 우선순위 선언이 뭘 의미하는지 예제로 봅시다.

[화면: 표현식 예제]

```
a || b && c < 5 + 1
```

이걸 어떻게 파싱할까요?

[화면: 우선순위 표]

```
낮음 → 높음:
  OR < AND < 비교 < +,- < *,/ < 단항- < 괄호
```

낮은 우선순위가 트리의 위쪽(루트)에 옵니다. 그래서:

```
a || (b && (c < (5 + 1)))
```

[화면: AST 시각화 - 트리 구조로 표시]

OR이 루트에 있고, 가장 나중에 평가되는 거죠. 수학 공식의 사칙연산 우선순위랑 같은 원리입니다.

[화면: %nonassoc 설명]

그런데 `%nonassoc`는 뭘까요?

```fsharp
%nonassoc EQUALS LT GT LE GE NE
```

이건 비교 연산자를 비연관성으로 만듭니다. 무슨 뜻이냐면:

```
1 < 2 < 3  // 파싱 에러!
```

[화면: 에러 메시지]

왜 막을까요? 수학에서 "1 < 2 < 3"은 "1 < 2이고 2 < 3"을 의미하잖아요?

문제는 대부분의 프로그래밍 언어가 이걸 왼쪽부터 평가한다는 겁니다.

```
(1 < 2) < 3
→ true < 3  // 타입 에러!
```

혼란을 방지하기 위해 아예 문법 에러로 만드는 거죠. 원하면 괄호를 명시적으로 쓰라는 겁니다.

### 섹션 5: Evaluator - 타입 검사의 예술 (10:30)

이제 evaluator를 수정합니다. 여기서 타입 검사가 일어나요.

[화면: Eval.fs]

```fsharp
let rec eval (env: Env) (expr: Expr) : Value =
    match expr with
    | Number n -> IntValue n
    | Bool b -> BoolValue b
    // ...
```

간단하죠? 이제 복잡한 부분을 봅시다.

[화면: 산술 연산]

```fsharp
| Add (left, right) ->
    match eval env left, eval env right with
    | IntValue l, IntValue r -> IntValue (l + r)
    | _ -> failwith "Type error: + requires integer operands"
```

[화면: 실행 예제]

덧셈은 반드시 정수 두 개를 받아야 합니다. 만약 "true + 1"을 실행하면?

```
$ dotnet run --project FunLang -- --expr "true + 1"
Error: Type error: + requires integer operands
```

런타임에 타입을 검사해서 에러를 던지는 거죠.

[화면: 비교 연산]

비교 연산자는 조금 다릅니다.

```fsharp
| LessThan (left, right) ->
    match eval env left, eval env right with
    | IntValue l, IntValue r -> BoolValue (l < r)
    | _ -> failwith "Type error: < requires integer operands"
```

정수 두 개를 받아서 Boolean을 반환합니다. 입력 타입과 출력 타입이 다른 거죠.

[화면: 타입 테이블]

| 연산자 | 입력 타입 | 출력 타입 |
|--------|-----------|-----------|
| `+`, `-`, `*`, `/` | int, int | int |
| `<`, `>`, `<=`, `>=` | int, int | bool |
| `=`, `<>` | 같은 타입 | bool |
| `&&`, `\|\|` | bool, bool | bool |

이 테이블을 머리에 넣어두면 타입 에러를 예측할 수 있어요.

### 섹션 6: 단락 평가 - 영리한 최적화 (13:00)

논리 연산자에는 특별한 게 있습니다. 단락 평가(short-circuit evaluation)라는 건데요.

[화면: And 구현]

```fsharp
| And (left, right) ->
    match eval env left with
    | BoolValue false -> BoolValue false  // 여기!
    | BoolValue true ->
        match eval env right with
        | BoolValue b -> BoolValue b
        | _ -> failwith "Type error: && requires boolean operands"
    | _ -> failwith "Type error: && requires boolean operands"
```

[화면: 하이라이트]

왼쪽이 false면 오른쪽을 평가하지도 않고 바로 false를 반환합니다!

왜 이게 중요하냐고요?

[화면: 예제]

```fsharp
x <> 0 && 10 / x > 1
```

만약 x가 0이면? 왼쪽 `x <> 0`이 false가 되고, 오른쪽 `10 / x`는 평가하지 않습니다. 만약 평가했다면 0으로 나누기 에러가 났겠죠!

[화면: Or 구현]

Or도 마찬가지입니다.

```fsharp
| Or (left, right) ->
    match eval env left with
    | BoolValue true -> BoolValue true  // 단락!
    | BoolValue false ->
        // 오른쪽 평가...
```

왼쪽이 true면 결과는 무조건 true니까, 오른쪽을 볼 필요가 없는 거죠.

**단락 평가는 성능 최적화이자 안전 장치입니다.**

### 섹션 7: If-Then-Else 구현 (15:00)

이제 드디어 if 표현식입니다!

[화면: If 구현]

```fsharp
| If (condition, thenBranch, elseBranch) ->
    match eval env condition with
    | BoolValue true -> eval env thenBranch
    | BoolValue false -> eval env elseBranch
    | _ -> failwith "Type error: if condition must be boolean"
```

[화면: 실행 흐름 다이어그램]

1. 먼저 condition을 평가
2. 결과가 Boolean인지 검사
3. true면 thenBranch 평가, false면 elseBranch 평가
4. 선택되지 않은 브랜치는 평가하지 않음!

[화면: 실행 예제]

```bash
$ dotnet run --project FunLang -- --expr "if 5 > 3 then 10 else 20"
10

$ dotnet run --project FunLang -- --expr "if 2 + 3 > 4 then 10 else 20"
10
```

[화면: 중첩 if]

if는 표현식이니까 중첩도 됩니다.

```bash
$ dotnet run --project FunLang -- --expr "if 5 > 3 then if 2 < 4 then 100 else 50 else 0"
100
```

else에 또 if가 올 수도 있고요:

```bash
$ dotnet run --project FunLang -- --expr "if 1 > 2 then 10 else if 3 > 4 then 20 else 30"
30
```

### 섹션 8: 실전 데모와 타입 에러 (16:30)

자, 실제로 동작하는지 봅시다!

[화면: 터미널]

```bash
# 변수와 조건문 조합
$ dotnet run --project FunLang -- --expr "let x = 10 in if x > 5 then x else 0"
10

# 복잡한 논리 연산
$ dotnet run --project FunLang -- --expr "let x = 10 in let y = 20 in if x = 10 && y = 20 then 1 else 0"
1
```

[화면: 타입 에러 예제들]

이제 일부러 틀린 코드를 실행해볼까요?

```bash
$ dotnet run --project FunLang -- --expr "if 1 then 2 else 3"
Error: Type error: if condition must be boolean

$ dotnet run --project FunLang -- --expr "true + 1"
Error: Type error: + requires integer operands

$ dotnet run --project FunLang -- --expr "1 && 2"
Error: Type error: && requires boolean operands
```

[화면: 에러 메시지 하이라이트]

보세요! 우리의 타입 검사가 제대로 동작하고 있습니다. 의미 없는 연산을 시도하면 명확한 에러 메시지를 보여주죠.

[화면: --emit-ast 디버깅]

디버깅 옵션도 써봅시다.

```bash
$ dotnet run --project FunLang -- --emit-ast --expr "if true then 1 else 2"
If (Bool true, Number 1, Number 2)
```

AST가 우리가 의도한 대로 만들어졌네요!

## 아웃트로 (17:30)

자, 오늘 우리가 한 걸 정리해볼까요?

[화면: 요약 화면]

1. **Value 타입** - Discriminated Union으로 여러 타입 안전하게 처리
2. **Boolean과 조건문** - if-then-else로 프로그램이 결정을 내릴 수 있게 됨
3. **타입 검사** - 런타임에 연산자 타입을 검증해서 에러 방지
4. **단락 평가** - 논리 연산자의 영리한 최적화
5. **연산자 우선순위** - fsyacc로 우선순위 정확하게 제어

[화면: 전후 비교]

우리 언어가 얼마나 발전했는지 보세요.

**Before:**
```
let x = 10 in x + 5
```

**After:**
```
let x = 10 in if x > 5 then x * 2 else 0
```

이제 FunLang이 조건에 따라 다르게 동작할 수 있습니다!

[화면: 다음 에피소드 예고]

다음 에피소드에서는 드디어 함수를 추가합니다. 함수 정의, 함수 호출, 클로저까지... FunLang이 진짜 함수형 언어가 되는 순간을 함께 하실 분들은 구독과 좋아요 부탁드립니다!

[화면: 코드 저장소 링크]

오늘 만든 코드는 GitHub에 있으니 직접 실행해보세요. 질문 있으시면 댓글로 남겨주시고요.

그럼 다음 에피소드에서 만나요!

## 핵심 키워드

- Discriminated Union (판별된 합집합)
- Value 타입
- Boolean 리터럴
- If-then-else 표현식
- 비교 연산자 (=, <>, <, >, <=, >=)
- 논리 연산자 (&&, ||)
- 타입 검사 (Type checking)
- 단락 평가 (Short-circuit evaluation)
- 연산자 우선순위 (Operator precedence)
- %nonassoc (비연관성)
- 패턴 매칭 (Pattern matching)
- fslex/fsyacc
