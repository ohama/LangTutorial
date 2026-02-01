---
created: 2026-02-01
description: F#에서 Hindley-Milner 타입 추론 시스템 (Algorithm W) 구현
---

# Hindley-Milner Algorithm W 구현

F#에서 완전한 Hindley-Milner 타입 추론을 단계별로 구현한다. Type AST → Substitution → Unification → Inference 순서로 진행.

## The Insight

타입 추론은 "타입 방정식 풀기"다. 식을 순회하며 타입 변수와 제약을 수집하고, 단일화(unification)로 해를 구한다. 핵심은 **substitution threading** - 각 단계에서 얻은 지식을 다음 단계에 전파해야 한다.

## Why This Matters

- 타입 시스템 없이는 런타임 오류 발견 불가
- 명시적 타입 주석 없이도 정적 타입 검사 가능
- ML 계열 언어(OCaml, F#, Haskell)의 핵심 기술

## Recognition Pattern

다음 상황에서 이 문서가 필요하다:

- 인터프리터/컴파일러에 타입 추론 추가
- OCaml/Haskell 스타일 let-polymorphism 구현
- "타입 추론은 어떻게 작동하는가?" 질문에 답할 때

## The Approach

4단계로 나눈다. 각 단계가 다음 단계의 기반이 된다.

### Step 1: Type AST 정의

타입을 표현하는 자료구조를 정의한다.

```fsharp
type Type =
    | TInt                        // int
    | TBool                       // bool
    | TString                     // string
    | TVar of int                 // 'a, 'b (정수로 구분)
    | TArrow of Type * Type       // 'a -> 'b
    | TTuple of Type list         // 'a * 'b
    | TList of Type               // 'a list

/// 다형성 지원을 위한 Scheme
type Scheme = Scheme of vars: int list * ty: Type

/// 타입 환경: 변수 이름 → Scheme
type TypeEnv = Map<string, Scheme>

/// 대체: 타입 변수 → 타입
type Subst = Map<int, Type>
```

**설계 결정:**
- `TVar of int` — 문자열 대신 정수 사용. 비교/해싱이 빠르고 fresh 변수 생성이 단순
- `Scheme` — forall 변수 리스트 + 타입. `id: forall 'a. 'a -> 'a`

### Step 2: Substitution 연산

대체(substitution)를 타입에 적용하는 함수들.

```fsharp
/// 빈 대체
let empty: Subst = Map.empty

/// 타입에 대체 적용
/// 핵심: TVar에서 재귀 호출 (transitive chain 처리)
let rec apply (s: Subst) = function
    | TInt -> TInt
    | TBool -> TBool
    | TString -> TString
    | TVar n ->
        match Map.tryFind n s with
        | Some t -> apply s t  // ← 재귀! {0→'b, 'b→int} 처리
        | None -> TVar n
    | TArrow (t1, t2) -> TArrow (apply s t1, apply s t2)
    | TTuple ts -> TTuple (List.map (apply s) ts)
    | TList t -> TList (apply s t)

/// 대체 합성: s2 after s1
let compose (s2: Subst) (s1: Subst): Subst =
    let s1' = Map.map (fun _ t -> apply s2 t) s1
    Map.fold (fun acc k v -> Map.add k v acc) s1' s2
```

**핵심 포인트:**
- `apply s (TVar n)` 에서 결과에 다시 `apply s`를 호출한다. `{0→TVar 1, 1→TInt}`를 `TVar 0`에 적용하면 `TInt`가 된다.
- `compose s2 s1`은 "s1 먼저, 그 다음 s2" 순서다.

### Step 3: Unification (단일화)

두 타입을 같게 만드는 대체를 찾는다.

```fsharp
exception TypeError of string

/// Occurs check: v가 t에 나타나는가?
let occurs (v: int) (t: Type): bool =
    Set.contains v (freeVars t)

/// Robinson's unification algorithm
let rec unify (t1: Type) (t2: Type): Subst =
    match t1, t2 with
    | TInt, TInt | TBool, TBool | TString, TString -> empty

    // 대칭 패턴: TVar는 양쪽 모두 처리
    | TVar n, t | t, TVar n ->
        if t = TVar n then empty
        elif occurs n t then
            raise (TypeError (sprintf "Infinite type: %s = %s" ...))
        else singleton n t

    // Arrow: 도메인 단일화 → 결과에 적용 → 치역 단일화
    | TArrow (a1, b1), TArrow (a2, b2) ->
        let s1 = unify a1 a2
        let s2 = unify (apply s1 b1) (apply s1 b2)  // ← threading
        compose s2 s1

    | TTuple ts1, TTuple ts2 when List.length ts1 = List.length ts2 ->
        List.fold2 (fun s t1 t2 ->
            let s' = unify (apply s t1) (apply s t2)
            compose s' s
        ) empty ts1 ts2

    | TList t1, TList t2 -> unify t1 t2

    | _ -> raise (TypeError ...)
```

**핵심 포인트:**
- `| TVar n, t | t, TVar n ->` — 대칭 패턴으로 양방향 처리
- `occurs check` — 무한 타입 방지 (`'a = 'a -> int` 거부)
- Arrow 단일화에서 `apply s1 b1` — 도메인 단일화 결과를 치역에 적용

### Step 4: Type Inference (Algorithm W)

AST를 순회하며 타입을 추론한다.

```fsharp
/// Fresh 타입 변수 생성기
let freshVar =
    let counter = ref 1000  // 0-999는 Prelude용으로 예약
    fun () ->
        let n = !counter
        counter := n + 1
        TVar n

/// Scheme 인스턴스화: bound var를 fresh로 교체
let instantiate (Scheme (vars, ty)): Type =
    match vars with
    | [] -> ty
    | _ ->
        let freshVars = List.map (fun _ -> freshVar()) vars
        let subst = List.zip vars freshVars |> Map.ofList
        apply subst ty

/// 일반화: 환경에 없는 free var를 forall로
let generalize (env: TypeEnv) (ty: Type): Scheme =
    let envFree = freeVarsEnv env
    let tyFree = freeVars ty
    let vars = Set.difference tyFree envFree |> Set.toList
    Scheme (vars, ty)

/// 핵심: infer 함수
let rec infer (env: TypeEnv) (expr: Expr): Subst * Type =
    match expr with
    | Number _ -> (empty, TInt)
    | Bool _ -> (empty, TBool)

    | Var name ->
        match Map.tryFind name env with
        | Some scheme -> (empty, instantiate scheme)
        | None -> raise (TypeError (sprintf "Unbound variable: %s" name))

    | Lambda (param, body) ->
        let paramTy = freshVar()
        let bodyEnv = Map.add param (Scheme ([], paramTy)) env
        let s, bodyTy = infer bodyEnv body
        (s, TArrow (apply s paramTy, bodyTy))

    | App (func, arg) ->
        let s1, funcTy = infer env func
        let s2, argTy = infer (applyEnv s1 env) arg  // ← threading
        let resultTy = freshVar()
        let s3 = unify (apply s2 funcTy) (TArrow (argTy, resultTy))
        (compose s3 (compose s2 s1), apply s3 resultTy)

    | Let (name, value, body) ->
        let s1, valueTy = infer env value
        let env' = applyEnv s1 env
        let scheme = generalize env' (apply s1 valueTy)  // ← polymorphism
        let bodyEnv = Map.add name scheme env'
        let s2, bodyTy = infer bodyEnv body
        (compose s2 s1, bodyTy)
```

**핵심 포인트:**
- `infer`는 `(Subst, Type)` 튜플을 반환
- 매 단계에서 이전 substitution을 환경에 적용: `applyEnv s1 env`
- Let에서 `generalize` 호출 → let-polymorphism

## Example

```fsharp
// 입력: let id = fun x -> x in (id 5, id true)
// 추론 과정:
// 1. fun x -> x → 'a -> 'a
// 2. generalize → forall 'a. 'a -> 'a
// 3. id 5 → instantiate to 'b -> 'b, unify 'b = int → int
// 4. id true → instantiate to 'c -> 'c, unify 'c = bool → bool
// 5. 최종: (int, bool) = int * bool
```

## 체크리스트

- [ ] Type AST가 모든 타입을 표현하는가?
- [ ] apply가 transitive chain을 처리하는가?
- [ ] occurs check가 무한 타입을 방지하는가?
- [ ] 단일화가 대칭적인가? (TVar 양쪽 처리)
- [ ] infer가 substitution을 threading하는가?
- [ ] Let에서 generalize를 호출하는가?

## 관련 문서

- `implement-let-polymorphism.md` - generalize/instantiate 상세
- `avoid-type-variable-collision.md` - freshVar 시작점 선택
- `write-type-inference-tests.md` - 타입 추론 테스트 방법
