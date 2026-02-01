# EP10: Hindley-Milner 타입 시스템 - 컴파일 타임에 버그를 잡아라!

## 영상 정보
- **예상 길이**: 20-22분
- **난이도**: 고급
- **필요 사전 지식**: EP01-09 시청 (특히 EP05 함수, EP09 Prelude)

## 인트로 (0:00)

여러분, v4.0의 첫 번째 에피소드입니다! 오늘은 정말 특별한 날이에요.

[화면: FunLang REPL에서 `1 + true` 입력 → 에러 메시지 "Cannot unify int with bool"]

지금까지 FunLang은 실행해봐야 버그를 알 수 있었죠. 하지만 오늘부터는 다릅니다. **코드를 실행하기도 전에** 타입 오류를 잡아낼 수 있어요!

[화면: "Hindley-Milner Type Inference" 타이틀과 Algorithm W 다이어그램]

이건 마치... 건물을 짓기 전에 설계도가 안전한지 검사하는 것과 같아요. 실행(construction) 전에 문제를 발견하는 거죠.

오늘은 Hindley-Milner 타입 추론 시스템을 구현합니다. ML, OCaml, F#, Haskell - 이 강력한 언어들의 기반이 되는 알고리즘이에요. 명시적 타입 주석 없이도 모든 표현식의 타입을 자동으로 알아내는 마법 같은 기술입니다!

Let's go!

## 본문

### 섹션 1: 왜 타입 시스템인가? (1:30)

먼저 타입 시스템이 왜 필요한지 생각해봅시다.

[화면: 런타임 에러 예시 코드들 - Python TypeError, JavaScript NaN 등]

동적 타입 언어에서는 이런 일이 자주 일어나죠. 코드가 프로덕션에 나가고, 사용자가 특정 버튼을 누르고, 그제서야 "TypeError: 'NoneType' has no attribute..." 에러가 터져요.

[화면: 정적 타입 체크 vs 동적 타입 체크 비교 다이어그램]

**정적 타입 검사**는 이런 버그를 **컴파일 타임**에 잡아냅니다. 코드가 사용자에게 닿기 전에요.

하지만 문제가 있어요. 타입을 일일이 적는 건 너무 번거롭죠.

```java
Map<String, List<Integer>> map = new HashMap<String, List<Integer>>();
```

[화면: 위 Java 코드 → 아래 FunLang 코드 비교]

```funlang
let map = [] in ...
```

**Hindley-Milner**는 이 문제를 해결합니다. 타입 주석 없이도 가장 일반적인 타입을 **자동으로 추론**하거든요!

### 섹션 2: 타입 언어 정의 (3:00)

타입을 추론하려면 먼저 "타입이 뭔지" 정의해야 해요.

[화면: Type.fs 파일 오픈, Type 타입 정의]

```fsharp
type Type =
    | TInt                        // int
    | TBool                       // bool
    | TString                     // string
    | TVar of int                 // 타입 변수 'a, 'b, ...
    | TArrow of Type * Type       // 함수 타입 'a -> 'b
    | TTuple of Type list         // 튜플 타입 'a * 'b
    | TList of Type               // 리스트 타입 'a list
```

**TVar**가 핵심이에요. 이게 바로 "아직 모르는 타입"을 표현합니다.

[화면: TVar 시각화 - 물음표가 있는 빈 칸]

`fun x -> x`의 타입을 생각해볼까요? x가 뭔지 모르니까 일단 `'a`라고 해요. 그러면 함수는 `'a -> 'a` 타입이 됩니다.

그리고 **Scheme**이라는 게 있어요. 다형성을 표현합니다.

[화면: Scheme 정의와 예시]

```fsharp
type Scheme = Scheme of vars: int list * ty: Type

// id: forall 'a. 'a -> 'a
Scheme([0], TArrow(TVar 0, TVar 0))
```

`forall 'a`가 바로 "어떤 타입이든 될 수 있다"는 의미예요. 숫자 0은 타입 변수 이름이고요.

### 섹션 3: 단일화 알고리즘 (5:30)

이제 핵심입니다. **단일화(Unification)**!

[화면: Unify.fs 파일, "두 타입을 같게 만드는 방법 찾기" 텍스트]

단일화란 "두 타입이 같아지려면 어떻게 해야 할까?"를 푸는 거예요.

```
'a = int          → 'a를 int로 대체하면 됨!
'a -> 'b = int -> bool   → 'a = int, 'b = bool
```

[화면: unify 함수 코드]

```fsharp
let rec unify (t1: Type) (t2: Type): Subst =
    match t1, t2 with
    | TInt, TInt -> empty       // 같으면 OK
    | TBool, TBool -> empty

    | TVar n, t | t, TVar n ->  // 변수면 대체 생성
        if t = TVar n then empty
        elif occurs n t then
            raise (TypeError "Infinite type!")
        else
            singleton n t

    | TArrow (a1, b1), TArrow (a2, b2) ->
        let s1 = unify a1 a2    // 왼쪽 단일화
        let s2 = unify (apply s1 b1) (apply s1 b2)  // 오른쪽도!
        compose s2 s1
```

**occurs check**가 중요해요! `'a = 'a -> int`를 생각해보세요. 'a를 대입하면 `('a -> int) -> int`가 되고, 또 대입하면... 무한 반복!

[화면: 무한 타입 시각화 - 끝없이 중첩되는 화살표]

이걸 **occurs check**가 막아줍니다.

### 섹션 4: Algorithm W - 타입 추론의 심장 (8:00)

이제 진짜 마법입니다. Algorithm W!

[화면: Infer.fs 파일, infer 함수 시그니처]

```fsharp
let rec infer (env: TypeEnv) (expr: Expr): Subst * Type
```

환경과 표현식을 받아서, **대체(Substitution)**와 **타입**을 반환합니다.

**리터럴**은 간단해요:

```fsharp
| Number _ -> (empty, TInt)    // 숫자는 int
| Bool _ -> (empty, TBool)     // 불리언은 bool
```

[화면: 숫자 42와 타입 int 연결 애니메이션]

**변수**는 환경에서 찾습니다:

```fsharp
| Var name ->
    match Map.tryFind name env with
    | Some scheme -> (empty, instantiate scheme)
    | None -> raise (TypeError "Unbound variable")
```

**instantiate**가 뭐냐고요? `forall 'a. 'a -> 'a`를 `'x -> 'x`로 바꿉니다. 매번 fresh한 타입 변수를 만들어서요.

[화면: instantiate 과정 시각화 - 'a가 새로운 'x로 교체]

### 섹션 5: Lambda와 Application (10:00)

**Lambda**는 좀 복잡해요:

```fsharp
| Lambda (param, body) ->
    let paramTy = freshVar()              // 파라미터 타입? 모름!
    let bodyEnv = Map.add param (Scheme([], paramTy)) env
    let s, bodyTy = infer bodyEnv body    // body 추론
    (s, TArrow(apply s paramTy, bodyTy))  // 함수 타입 반환
```

[화면: fun x -> x + 1 추론 과정 애니메이션]

1. `x`에 fresh 변수 `'a` 할당
2. `x + 1` 추론 → `'a`가 `int`여야 함!
3. 결과: `int -> int`

**Application**은 더 재밌어요:

```fsharp
| App (func, arg) ->
    let s1, funcTy = infer env func       // 함수 추론
    let s2, argTy = infer (applyEnv s1 env) arg  // 인자 추론
    let resultTy = freshVar()
    let s3 = unify (apply s2 funcTy) (TArrow(argTy, resultTy))
    (compose s3 (compose s2 s1), apply s3 resultTy)
```

[화면: f x 호출 시 타입 플로우 다이어그램]

함수 `f`가 `'a -> 'b` 타입이고 인자 `x`가 `int`면, `'a = int`로 단일화되고 결과는 `'b`!

### 섹션 6: Let-Polymorphism - 마법의 핵심 (12:30)

이게 Hindley-Milner의 진짜 마법입니다. **Let-Polymorphism**!

[화면: let id = fun x -> x in (id 5, id true) 코드]

```funlang
let id = fun x -> x in (id 5, id true)
```

생각해보세요. `id`가 `int -> int`면 `id true`에서 에러나야 하고, `bool -> bool`이면 `id 5`에서 에러나야 해요. 그런데... **둘 다 됩니다**!

[화면: Let 추론 코드]

```fsharp
| Let (name, value, body) ->
    let s1, valueTy = infer env value
    let env' = applyEnv s1 env
    let scheme = generalize env' (apply s1 valueTy)  // ← 핵심!
    let bodyEnv = Map.add name scheme env'
    let s2, bodyTy = infer bodyEnv body
```

**generalize**가 타입 변수를 `forall`로 감쌉니다!

```
id의 타입: 'a -> 'a
generalize 후: forall 'a. 'a -> 'a
```

[화면: generalize 과정 시각화]

그리고 `id 5` 할 때 **instantiate**해서 fresh 변수로 바꿔요. `id true` 할 때도 다른 fresh 변수로!

```
id 5: 'x -> 'x (인스턴스화) → int -> int (단일화)
id true: 'y -> 'y (인스턴스화) → bool -> bool (단일화)
```

**Lambda parameter는 다릅니다**:

```funlang
fun f -> (f 1, f true)  // 타입 에러!
```

[화면: Lambda vs Let 비교 다이어그램]

Lambda의 파라미터는 `Scheme([], ty)` - generalize 안 됨! 그래서 `f`는 한 타입으로 고정돼요.

### 섹션 7: Prelude 타입 정의 (15:00)

EP09에서 만든 Prelude 함수들에 타입을 줘야 해요.

[화면: TypeCheck.fs의 initialTypeEnv]

```fsharp
let initialTypeEnv: TypeEnv =
    Map.ofList [
        "map", Scheme([0; 1],
            TArrow(TArrow(TVar 0, TVar 1), TArrow(TList(TVar 0), TList(TVar 1))))

        "filter", Scheme([0],
            TArrow(TArrow(TVar 0, TBool), TArrow(TList(TVar 0), TList(TVar 0))))

        "fold", Scheme([0; 1],
            TArrow(TArrow(TVar 1, TArrow(TVar 0, TVar 1)),
                   TArrow(TVar 1, TArrow(TList(TVar 0), TVar 1))))
    ]
```

[화면: map 타입 시각화]

```
map: ('a -> 'b) -> 'a list -> 'b list
```

- 첫 번째 인자: 변환 함수 `'a -> 'b`
- 두 번째 인자: 원본 리스트 `'a list`
- 결과: 변환된 리스트 `'b list`

**중요한 트릭**: Prelude는 0-9 범위의 타입 변수를 쓰고, `freshVar`는 1000부터 시작해요. 충돌 방지!

[화면: 변수 범위 시각화 - 0-9 Prelude, 1000+ Fresh]

### 섹션 8: CLI 통합과 데모 (16:30)

이제 실제로 써봅시다!

[화면: --emit-type 플래그 추가된 CLI]

```bash
$ dotnet run --project FunLang -- --emit-type -e '42'
int

$ dotnet run --project FunLang -- --emit-type -e 'fun x -> x'
'm -> 'm

$ dotnet run --project FunLang -- --emit-type -e 'fun x -> x + 1'
int -> int
```

[화면: 라이브 데모 - 다양한 표현식 타입 추론]

**Let-polymorphism 증명**:

```bash
$ dotnet run --project FunLang -- --emit-type -e 'let id = fun x -> x in (id 5, id true)'
int * bool
```

**에러도 잘 잡습니다**:

```bash
$ dotnet run --project FunLang -- --emit-type -e '1 + true'
Error: Cannot unify int with bool

$ dotnet run --project FunLang -- --emit-type -e 'if true then 1 else "hello"'
Error: Cannot unify int with string
```

[화면: 에러 메시지 하이라이트]

이제 코드 실행 전에 버그를 잡을 수 있어요!

### 섹션 9: Prelude 함수 타입 확인 (18:00)

Prelude 함수들의 타입도 확인해볼까요?

[화면: 라이브 데모 - map, filter, fold 타입]

```bash
$ dotnet run --project FunLang -- --emit-type -e 'map'
('m -> 'n) -> 'm list -> 'n list

$ dotnet run --project FunLang -- --emit-type -e 'filter'
('m -> bool) -> 'm list -> 'm list

$ dotnet run --project FunLang -- --emit-type -e 'fold'
('n -> 'm -> 'n) -> 'n -> 'm list -> 'n
```

**부분 적용도 됩니다**:

```bash
$ dotnet run --project FunLang -- --emit-type -e 'map (fun x -> x + 1)'
int list -> int list

$ dotnet run --project FunLang -- --emit-type -e 'filter (fun x -> x > 0)'
int list -> int list
```

[화면: 타입이 점점 구체화되는 과정 애니메이션]

`'a`가 `int`로 구체화되는 걸 볼 수 있어요!

### 섹션 10: 타입 시스템의 힘 (19:30)

[화면: 전체 아키텍처 다이어그램 - Type.fs → Unify.fs → Infer.fs → TypeCheck.fs]

우리가 구현한 건 꽤 대단한 거예요.

| 컴포넌트 | 역할 |
|----------|------|
| Type.fs | 타입 AST, Substitution, Free Variables |
| Unify.fs | Robinson 단일화, Occurs Check |
| Infer.fs | Algorithm W (freshVar, instantiate, generalize, infer) |
| TypeCheck.fs | Prelude 타입, typecheck 함수 |

[화면: Hindley-Milner 특성 체크리스트]

**Hindley-Milner의 특징**:
- ✓ **완전한 추론**: 타입 주석 없이 가장 일반적인 타입
- ✓ **Let-polymorphism**: let에서 다형성 지원
- ✓ **결정 가능성**: 항상 종료, 유일한 타입
- ✓ **Occurs check**: 무한 타입 방지

이건 OCaml, F#, Haskell의 기반이에요. 우리가 그 알고리즘을 직접 구현한 겁니다!

## 아웃트로 (20:30)

[화면: v4.0 시작 배너]

여러분, 축하합니다! FunLang이 드디어 **정적 타입 언어**가 되었습니다!

**v4.0에서 달성한 것**:
- ✓ Hindley-Milner 타입 추론
- ✓ Let-polymorphism
- ✓ 정적 타입 검사
- ✓ --emit-type CLI 플래그

[화면: FunLang REPL에서 타입 오류가 사전에 잡히는 모습]

이제 FunLang은 실행 전에 버그를 잡아주는 안전한 언어예요. 물론 완벽한 타입 시스템은 아니지만, ML 계열 언어의 핵심을 직접 구현했다는 건 대단한 일입니다!

**다음 에피소드 예고?**

타입 시스템을 더 확장할 수도 있어요. 재귀 타입, 타입 클래스, 모듈 시스템... 가능성은 무한합니다!

[화면: GitHub 링크와 구독 버튼]

전체 소스 코드는 GitHub에 있고, 460개가 넘는 테스트가 있습니다. 직접 실험해보세요!

**좋아요와 구독** 잊지 마시고, 어떤 기능을 더 추가했으면 좋겠는지 댓글로 알려주세요.

다음 에피소드에서 만나요. 즐거운 코딩 되세요!

[화면: "Thanks for watching!" + FunLang 로고]

## 핵심 키워드

- Hindley-Milner
- Algorithm W
- 타입 추론 (Type Inference)
- 단일화 (Unification)
- Occurs Check
- Let-Polymorphism
- generalize / instantiate
- Substitution
- Principal Type
- 정적 타입 검사
- Scheme (forall 타입)
- Free Variables
- Type AST
- FunLang
- F# 인터프리터
- v4.0 마일스톤
