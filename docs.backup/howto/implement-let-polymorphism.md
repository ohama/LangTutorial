---
created: 2026-02-01
description: Let-polymorphism 구현 시 generalize/instantiate 순서와 substitution threading
---

# Let-Polymorphism 구현

Hindley-Milner에서 let-polymorphism을 올바르게 구현한다. `let id = fun x -> x in (id 5, id true)`가 통과하려면 정확한 순서가 필요하다.

## The Insight

Let-polymorphism의 핵심은 **generalize 타이밍**이다. value를 추론한 후, 그 결과 substitution을 환경에 적용한 다음 generalize해야 한다. 순서가 틀리면 다형성이 작동하지 않거나 불완전한 타입이 된다.

## Why This Matters

순서가 틀리면:
- `let id = fun x -> x in id 5` 는 되지만
- `let id = fun x -> x in (id 5, id true)` 에서 타입 오류 발생
- "Cannot unify int with bool" — id가 monomorphic으로 처리됨

## Recognition Pattern

다음 증상이 있으면 이 문서를 참고한다:

- 같은 함수를 다른 타입으로 두 번 사용할 때 오류
- `let id = fun x -> x`가 `'a -> 'a` 대신 구체 타입으로 추론됨
- Lambda parameter가 의도치 않게 polymorphic

## The Approach

### Step 1: Lambda Parameter는 Monomorphic

Lambda 파라미터는 forall로 감싸지 않는다.

```fsharp
| Lambda (param, body) ->
    let paramTy = freshVar()
    // 주의: Scheme([], paramTy) — 빈 vars 리스트
    let bodyEnv = Map.add param (Scheme ([], paramTy)) env
    let s, bodyTy = infer bodyEnv body
    (s, TArrow (apply s paramTy, bodyTy))
```

**왜 monomorphic인가?**

`fun f -> (f 1, f true)`를 생각해보자. 만약 f가 polymorphic이면 이게 통과해야 하는데, 실제로는 불가능하다. f는 호출 시점에 하나의 타입으로 고정되어야 한다.

### Step 2: Let Value 추론 후 Substitution 적용

Value를 추론하고, 그 substitution을 환경에 적용한다.

```fsharp
| Let (name, value, body) ->
    // 1. value 추론
    let s1, valueTy = infer env value

    // 2. substitution을 환경에 적용 (필수!)
    let env' = applyEnv s1 env

    // 3. 적용된 환경으로 generalize
    let scheme = generalize env' (apply s1 valueTy)

    // 4. 이름 바인딩 추가
    let bodyEnv = Map.add name scheme env'

    // 5. body 추론
    let s2, bodyTy = infer bodyEnv body
    (compose s2 s1, bodyTy)
```

**순서가 중요한 이유:**

```fsharp
// 잘못된 순서 (applyEnv 없이)
let scheme = generalize env valueTy  // ← 환경에 아직 s1이 적용 안 됨!
```

환경에 `x: 'a`가 있고 value 추론에서 `'a = int`를 알아냈다면, generalize 전에 환경을 `x: int`로 업데이트해야 한다. 그래야 `'a`가 환경의 free var가 아니게 되어 generalize 대상이 된다.

### Step 3: Generalize는 환경 Free Var 제외

```fsharp
let generalize (env: TypeEnv) (ty: Type): Scheme =
    let envFree = freeVarsEnv env      // 환경의 모든 free var
    let tyFree = freeVars ty           // 타입의 free var
    let vars = Set.difference tyFree envFree  // 차집합만 generalize
    Scheme (vars, ty)
```

`id: 'a -> 'a`에서 환경이 비어있으면 `'a`가 generalize되어 `forall 'a. 'a -> 'a`가 된다.

### Step 4: Instantiate는 Fresh Var로 교체

사용 시점에 scheme을 인스턴스화한다.

```fsharp
let instantiate (Scheme (vars, ty)): Type =
    match vars with
    | [] -> ty  // monomorphic — 그대로
    | _ ->
        let freshVars = List.map (fun _ -> freshVar()) vars
        let subst = List.zip vars freshVars |> Map.ofList
        apply subst ty
```

`forall 'a. 'a -> 'a`를 인스턴스화하면 `'b -> 'b` (fresh 'b).

## Example

```fsharp
// let id = fun x -> x in (id 5, id true)

// Step 1: infer (fun x -> x)
// x: Scheme([], 'a) — monomorphic
// body: 'a
// result: 'a -> 'a

// Step 2: generalize
// env' = {} (비어있음)
// generalize {} ('a -> 'a) = Scheme(['a], 'a -> 'a)

// Step 3: id 5
// instantiate → 'b -> 'b (fresh)
// unify 'b = int → int

// Step 4: id true
// instantiate → 'c -> 'c (다른 fresh!)
// unify 'c = bool → bool

// Final: (int, bool) ✓
```

## 체크리스트

- [ ] Lambda param이 `Scheme([], ty)`로 바인딩되는가?
- [ ] Let에서 `applyEnv s1 env`를 호출하는가?
- [ ] Generalize가 `apply s1 valueTy`를 받는가?
- [ ] Instantiate가 매번 fresh var를 생성하는가?
- [ ] `fun f -> (f 1, f true)`가 오류를 내는가? (lambda mono 확인)
- [ ] `let id = fun x -> x in (id 1, id true)`가 통과하는가? (let poly 확인)

## 관련 문서

- `implement-hindley-milner-algorithm-w.md` - 전체 흐름
- `write-type-inference-tests.md` - polymorphism 테스트 방법
