# EP12: 양방향 타입 체킹 - 타입 어노테이션의 힘!

## 영상 정보
- **예상 길이**: 20-22분
- **난이도**: 고급
- **필요 사전 지식**: EP10 시청 (Hindley-Milner 타입 시스템)

## 인트로 (0:00)

여러분, v6.0의 핵심 에피소드입니다! 오늘은 **양방향 타입 체킹**을 구현해요.

[화면: Algorithm W의 한계 - 타입 추론만 가능]

지금까지 FunLang의 타입 시스템은 모든 걸 추론했어요. `fun x -> x + 1`은 자동으로 `int -> int`가 됐죠. 그런데...

[화면: 어노테이션 문법 예시]

```funlang
fun (x: int) -> x + 1        // 파라미터 타입 명시
(42 : int)                   // 표현식 타입 명시
```

이렇게 **직접 타입을 적고 싶을 때**가 있어요! 문서화도 되고, 더 명확한 에러 메시지도 받을 수 있거든요.

오늘은 **양방향 타입 체킹(Bidirectional Type Checking)**을 구현해서 이런 어노테이션을 지원합니다.

Let's go!

## 본문

### 섹션 1: 양방향이란? - 두 가지 모드 (1:30)

먼저 핵심 개념부터 이해합시다.

[화면: 단방향 vs 양방향 다이어그램]

**Algorithm W (단방향)**:
```
Expression → [추론] → Type
```

표현식에서 타입을 "합성(synthesize)"하는 한 방향만 있어요.

**Bidirectional (양방향)**:
```
Expression → [합성 synth] → Type     (bottom-up)
Expected ← [검사 check] ← Expression (top-down)
```

두 가지 모드가 있습니다!

[화면: synth/check 시그니처]

```fsharp
// Synthesis: 표현식에서 타입 추론
let rec synth (env: TypeEnv) (expr: Expr): Subst * Type

// Checking: 예상 타입에 대해 검증
and check (env: TypeEnv) (expr: Expr) (expected: Type): Subst
```

**synth**는 "이 표현식의 타입이 뭐지?" - 아래에서 위로.
**check**는 "이 표현식이 이 타입인가?" - 위에서 아래로.

[화면: 어노테이션이 모드 전환을 유발]

어노테이션 `(e : T)`를 만나면 checking 모드로 전환됩니다. T가 "예상 타입"이 되거든요!

### 섹션 2: TypeExpr - 타입 표현식 파싱 (4:00)

먼저 타입 어노테이션 문법을 파싱해야 해요.

[화면: TypeExpr 타입 정의]

```fsharp
// Ast.fs
type TypeExpr =
    | TEInt                          // int
    | TEBool                         // bool
    | TEString                       // string
    | TEList of TypeExpr             // T list
    | TEArrow of TypeExpr * TypeExpr // T1 -> T2
    | TETuple of TypeExpr list       // T1 * T2
    | TEVar of string                // 'a, 'b
```

`TypeExpr`은 파서가 인식하는 문법 표현이에요. `Type`과 다릅니다!

[화면: TypeExpr vs Type 비교]

| TypeExpr | Type |
|----------|------|
| `TEVar "'a"` | `TVar 0` |
| 파서가 생성 | 타입 추론에서 사용 |
| 문자열 이름 | 정수 인덱스 |

`'a`라는 문자열을 숫자 0으로 변환해야 해요.

[화면: Parser.fsy 타입 문법]

```fsy
TypeExpr:
    | INT_TYPE                        { TEInt }
    | BOOL_TYPE                       { TEBool }
    | STRING_TYPE                     { TEString }
    | TypeExpr LIST_TYPE              { TEList $1 }
    | TypeExpr ARROW TypeExpr         { TEArrow ($1, $3) }
    | LPAREN TypeExpr RPAREN          { $2 }
    | TYPEVAR                         { TEVar $1 }
```

이제 `int -> bool`, `'a list`, `int * string` 같은 타입을 파싱할 수 있어요!

### 섹션 3: Elaborate - 타입 정교화 (7:00)

`TypeExpr`을 `Type`으로 변환하는 과정이 **정교화(Elaboration)**입니다.

[화면: Elaborate.fs 모듈]

```fsharp
module Elaborate

/// 타입 변수 환경: 'a -> 0, 'b -> 1
type TypeVarEnv = Map<string, int>

/// Fresh 타입 변수 인덱스 (0부터 시작)
let freshTypeVarIndex =
    let counter = ref 0
    fun () ->
        let n = !counter
        counter := n + 1
        n
```

**중요한 설계**: 사용자 타입 변수는 0부터, 추론 타입 변수는 1000부터 시작해요. 충돌 방지!

[화면: elaborateWithVars 함수]

```fsharp
let rec elaborateWithVars (vars: TypeVarEnv) (te: TypeExpr): Type * TypeVarEnv =
    match te with
    | TEInt -> (TInt, vars)
    | TEBool -> (TBool, vars)

    | TEVar name ->
        // 'a, 'b 등의 타입 변수
        match Map.tryFind name vars with
        | Some idx -> (TVar idx, vars)  // 이미 본 변수
        | None ->
            let idx = freshTypeVarIndex()
            let vars' = Map.add name idx vars
            (TVar idx, vars')           // 새 변수 할당
```

같은 `'a`는 같은 인덱스를 공유해요!

[화면: 변환 예시 애니메이션]

```
'a -> 'a list
↓ elaborate
TArrow(TVar 0, TList(TVar 0))
```

두 `'a`가 모두 `TVar 0`이 됩니다!

### 섹션 4: AST 확장 - Annot과 LambdaAnnot (9:30)

어노테이션을 표현하는 새로운 AST 노드가 필요해요.

[화면: Ast.fs 확장]

```fsharp
type Expr =
    // ... 기존 노드들 ...
    | Annot of expr: Expr * ty: TypeExpr * span: Span
        // (e : T)
    | LambdaAnnot of param: string * paramTy: TypeExpr * body: Expr * span: Span
        // fun (x: T) -> e
```

[화면: Parser.fsy 어노테이션 문법]

```fsy
Atom:
    | LPAREN Expr COLON TypeExpr RPAREN
        { Annot ($2, $4, mkSpan parseState 1 5) }

LambdaExpr:
    | FUN LPAREN IDENT COLON TypeExpr RPAREN ARROW Expr
        { LambdaAnnot ($3, $5, $8, mkSpan parseState 1 8) }
```

이제 `(42 : int)`, `fun (x: int) -> x + 1`을 파싱할 수 있어요!

### 섹션 5: Bidir.fs - synth 구현 (11:00)

이제 핵심! Bidir 모듈을 구현합니다.

[화면: synth 함수 - 리터럴과 변수]

```fsharp
let rec synth (ctx: InferContext list) (env: TypeEnv) (expr: Expr): Subst * Type =
    match expr with
    // 리터럴 - 타입은 고정
    | Number (_, _) -> (empty, TInt)
    | Bool (_, _) -> (empty, TBool)
    | String (_, _) -> (empty, TString)

    // 변수 - 환경에서 조회
    | Var (name, span) ->
        match Map.tryFind name env with
        | Some scheme -> (empty, instantiate scheme)
        | None -> raise (TypeException { Kind = UnboundVar name; ... })
```

여기까지는 Algorithm W와 동일해요.

[화면: synth - 어노테이션 없는 람다]

```fsharp
    // Lambda (어노테이션 없음) - HYBRID 방식
    | Lambda (param, body, _) ->
        let paramTy = freshVar()  // fresh 타입 변수 'a
        let bodyEnv = Map.add param (Scheme ([], paramTy)) env
        let s, bodyTy = synth ctx bodyEnv body
        (s, TArrow (apply s paramTy, bodyTy))
```

**HYBRID 방식**: 어노테이션 없는 람다는 Algorithm W처럼 동작해요. 하위 호환성!

[화면: synth - 어노테이션된 람다]

```fsharp
    // LambdaAnnot (어노테이션 있음)
    | LambdaAnnot (param, paramTyExpr, body, span) ->
        let paramTy = elaborateTypeExpr paramTyExpr  // 'a -> TVar 0
        let ctx' = InCheckMode (paramTy, "annotation", span) :: ctx
        let bodyEnv = Map.add param (Scheme ([], paramTy)) env
        let s, bodyTy = synth ctx' bodyEnv body
        (s, TArrow (apply s paramTy, bodyTy))
```

어노테이션이 있으면 `paramTy`를 elaborate해서 사용해요. 추론하지 않고 명시된 타입을 쓰는 거죠!

### 섹션 6: synth - Annot 처리 (13:00)

표현식 어노테이션 `(e : T)`는 어떻게 처리할까요?

[화면: synth - Annot 케이스]

```fsharp
    // Annot - 타입 어노테이션
    | Annot (e, tyExpr, span) ->
        let expectedTy = elaborateTypeExpr tyExpr  // T를 Type으로 변환
        let ctx' = InCheckMode (expectedTy, "annotation", span) :: ctx
        let s = check ctx' env e expectedTy        // checking 모드로 전환!
        (s, apply s expectedTy)
```

**핵심**: `check`를 호출해요! checking 모드로 전환됩니다.

[화면: 모드 전환 다이어그램]

```
(42 : int)
    ↓ synth
Annot(Number 42, TEInt)
    ↓ elaborate
expectedTy = TInt
    ↓ check(env, Number 42, TInt)
검증 후 TInt 반환
```

synth 도중에 check로 전환되는 거예요!

### 섹션 7: check 함수 구현 (14:30)

이제 checking 모드를 구현합니다.

[화면: check 함수 시그니처]

```fsharp
/// 예상 타입에 대해 표현식 검사
and check (ctx: InferContext list) (env: TypeEnv) (expr: Expr) (expected: Type): Subst =
```

check는 substitution만 반환해요. 타입은 이미 expected로 알고 있으니까요!

[화면: check - Lambda 케이스]

```fsharp
    | Lambda (param, body, _) ->
        match expected with
        | TArrow (paramTy, resultTy) ->
            // expected에서 파라미터 타입을 가져옴!
            let bodyEnv = Map.add param (Scheme ([], paramTy)) env
            let s = check ctx bodyEnv body resultTy  // body도 check
            s
        | _ ->
            // 화살표가 아니면 subsumption으로 폴백
            subsume ctx env expr expected
```

**핵심**: 람다를 화살표 타입에 대해 체크할 때, expected에서 파라미터 타입을 직접 가져와요!

[화면: 비교 - synth vs check Lambda]

```
synth: fun x -> e
  → paramTy = freshVar()  (추론)

check: fun x -> e against A -> B
  → paramTy = A           (expected에서)
```

checking 모드에서는 정보가 위에서 아래로 흐릅니다!

### 섹션 8: Subsumption - 폴백 규칙 (16:00)

check가 모든 케이스를 직접 처리할 필요는 없어요.

[화면: Subsumption 규칙]

```fsharp
/// Subsumption: synth 후 unify로 폴백
and subsume ctx env expr expected =
    let s, actual = synth ctx env expr  // 먼저 synth
    let s' = unifyWithContext ctx [] (spanOf expr) (apply s expected) actual
    compose s' s
```

**Subsumption**: 특별한 check 규칙이 없으면, synth 후 결과를 expected와 unify해요.

[화면: check의 기본 케이스]

```fsharp
    // 다른 모든 표현식 - subsumption으로 폴백
    | _ -> subsume ctx env expr expected
```

이렇게 하면 모든 표현식에 대해 check를 구현할 필요가 없어요!

### 섹션 9: 에러 메시지 개선 (17:00)

양방향 체킹의 큰 장점은 **더 나은 에러 메시지**예요.

[화면: InCheckMode 컨텍스트]

```fsharp
type InferContext =
    // ... 기존 컨텍스트
    | InCheckMode of expected: Type * source: string * Span
```

어노테이션이 있으면 이 컨텍스트가 쌓여요.

[화면: Algorithm W vs Bidirectional 에러 비교]

**Algorithm W**:
```
error: Cannot unify int with bool
```

**Bidirectional (어노테이션 있을 때)**:
```
error[E0301]: Type mismatch: expected int but got bool
  --> <expr>:1:1-5
   = due to annotation: <expr>:1:0-12
   = note: expected int due to annotation at <expr>:1:0-12
```

어노테이션 위치까지 알려줘요! "왜 int를 기대했냐면, 여기 어노테이션 때문이야."

### 섹션 10: 데모와 마무리 (18:30)

실제로 동작을 확인해봅시다!

[화면: 터미널 데모]

```bash
# 기본 어노테이션
$ dotnet run --project FunLang -- --emit-type -e '(42 : int)'
int

# 람다 어노테이션
$ dotnet run --project FunLang -- --emit-type -e 'fun (x: int) -> x + 1'
int -> int

# 표현식 어노테이션
$ dotnet run --project FunLang -- --emit-type -e '(fun x -> x : int -> int)'
int -> int
```

[화면: 에러 케이스]

```bash
# 잘못된 어노테이션
$ dotnet run --project FunLang -- --emit-type -e '(true : int)'
error[E0301]: Type mismatch: expected int but got bool
  --> <expr>:1:1-5
   = due to annotation: <expr>:1:0-12
```

[화면: 하위 호환성 확인]

```bash
# 어노테이션 없는 코드도 그대로 동작
$ dotnet run --project FunLang -- --emit-type -e 'fun x -> x'
'm -> 'm

$ dotnet run --project FunLang -- --emit-type -e 'let id = fun x -> x in (id 5, id true)'
int * bool
```

Algorithm W처럼 완전히 추론됩니다!

## 아웃트로 (20:00)

[화면: v6.0 완료 배너]

여러분, 축하합니다! FunLang이 **양방향 타입 체킹**을 지원하게 되었어요!

**v6.0에서 구현한 것들**:
- TypeExpr 파싱 (타입 어노테이션 문법)
- Elaborate (TypeExpr -> Type 변환)
- Bidir 모듈 (synth/check 함수)
- 어노테이션 기반 에러 메시지

[화면: 양방향 체킹 장점 요약]

| 장점 | 설명 |
|------|------|
| 명시적 문서화 | 타입을 코드에 명시 |
| 더 나은 에러 | 어노테이션 위치 표시 |
| 점진적 타이핑 | 일부만 어노테이션 가능 |
| 하위 호환성 | 기존 코드 그대로 동작 |

[화면: 전체 아키텍처 다이어그램]

```
TypeExpr (파서) → Elaborate → Type
                              ↓
                       Bidir (synth/check)
                              ↓
                        Diagnostic
```

이제 FunLang은 OCaml, Haskell처럼 타입 어노테이션을 지원하는 진짜 타입 언어가 되었습니다!

[화면: GitHub 링크와 구독 버튼]

전체 소스 코드는 GitHub에서 확인하세요. 질문이나 제안은 댓글로 남겨주세요.

좋아요와 구독 잊지 마시고, 다음 에피소드에서 만나요!

## 핵심 키워드

- Bidirectional Type Checking
- 양방향 타입 체킹
- synth (합성)
- check (검사)
- Subsumption
- TypeExpr
- Elaborate
- 타입 어노테이션
- (e : T) 문법
- fun (x: T) -> e
- InCheckMode
- Algorithm W 확장
- Hindley-Milner
- FunLang
- v6.0 마일스톤
