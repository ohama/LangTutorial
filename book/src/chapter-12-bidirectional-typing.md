# Chapter 12: 양방향 타입 체킹 (Bidirectional Type Checking)

이 장에서는 FunLang의 타입 시스템을 **양방향 타입 체킹**으로 확장하여 타입 어노테이션을 지원한다. 기존 Algorithm W는 표현식에서 타입을 "합성"하는 단일 방향만 지원했지만, 양방향 체킹은 추가로 주어진 타입에 대해 표현식을 "검사"하는 기능을 제공한다.

## 개요

양방향 타입 체킹은 다음 기능을 제공한다:

- **Synthesis (합성)**: 표현식에서 타입을 추론 (bottom-up)
- **Checking (검사)**: 예상 타입에 대해 표현식 검증 (top-down)
- **타입 어노테이션**: `(e : T)`, `fun (x: int) -> e`

**핵심 아이디어**: Algorithm W는 모든 타입을 추론하지만, 양방향 체킹은 주어진 타입 정보를 활용하여 더 나은 에러 메시지와 명시적 문서화를 제공한다.

## Algorithm W와의 비교

| 특성 | Algorithm W | Bidirectional |
|------|-------------|---------------|
| 방향 | Bottom-up only | Both directions |
| 어노테이션 | 지원 안함 | `(e : T)`, `fun (x: T) -> e` |
| 에러 메시지 | 추론된 타입만 표시 | "expected T due to annotation" |
| 다형성 | let-polymorphism | let-polymorphism (동일) |
| 구현 | `infer` 함수 하나 | `synth` + `check` 두 함수 |

**Algorithm W의 한계**:

```funlang
// Algorithm W: 모든 타입을 추론
fun x -> x + 1    // int -> int (추론됨)
```

**양방향 체킹의 장점**:

```funlang
// 명시적 어노테이션으로 의도 표현
fun (x: int) -> x + 1              // int -> int (어노테이션 확인)
(fun x -> x + 1 : int -> int)      // 표현식 전체에 타입 지정
```

어노테이션이 있으면 에러 메시지가 더 명확해진다:

```
// Algorithm W
error: Cannot unify int with bool

// Bidirectional (어노테이션 있을 때)
error[E0301]: Type mismatch: expected int but got bool
   = note: expected int due to annotation at <expr>:1:0-12
```

## 구현 전략

양방향 타입 체킹은 3단계로 구현된다:

1. **Parser Extensions**: 타입 어노테이션 문법 추가
2. **Type Elaboration**: TypeExpr -> Type 변환
3. **Bidir Module**: synth/check 함수 구현

## TypeExpr: 타입 표현식 AST

파서가 인식하는 타입 표현식 문법이다. `FunLang/Ast.fs`에 정의:

```fsharp
/// Type expression AST for type annotations
type TypeExpr =
    | TEInt                               // int
    | TEBool                              // bool
    | TEString                            // string
    | TEList of TypeExpr                  // T list
    | TEArrow of TypeExpr * TypeExpr      // T1 -> T2 (right-associative)
    | TETuple of TypeExpr list            // T1 * T2 * ... (n >= 2)
    | TEVar of string                     // 'a, 'b (includes apostrophe)
```

**주요 차이점**:

- `TypeExpr`: 파서가 생성하는 문법 표현 (문자열 `'a` 포함)
- `Type`: 타입 추론에서 사용하는 내부 표현 (`TVar of int`)

## Elaborate.fs: 타입 정교화

`TypeExpr`을 `Type`으로 변환한다. 핵심은 타입 변수 처리:

```fsharp
module Elaborate

/// Type variable environment: maps type variable names to TVar indices
/// Example: 'a -> 0, 'b -> 1
type TypeVarEnv = Map<string, int>

/// Fresh type variable index generator for elaboration
/// Start at 0 (separate range from inference's 1000+)
let freshTypeVarIndex =
    let counter = ref 0
    fun () ->
        let n = !counter
        counter := n + 1
        n
```

**설계 결정**: 사용자 타입 변수는 0부터, 추론 타입 변수는 1000부터 시작하여 충돌을 방지한다.

### elaborateWithVars: 핵심 변환 함수

```fsharp
/// Elaborate type expression to type, threading type variable environment
/// Returns: (elaborated type, updated environment)
let rec elaborateWithVars (vars: TypeVarEnv) (te: TypeExpr): Type * TypeVarEnv =
    match te with
    | TEInt -> (TInt, vars)
    | TEBool -> (TBool, vars)
    | TEString -> (TString, vars)

    | TEList t ->
        let (ty, vars') = elaborateWithVars vars t
        (TList ty, vars')

    | TEArrow (t1, t2) ->
        let (ty1, vars1) = elaborateWithVars vars t1
        let (ty2, vars2) = elaborateWithVars vars1 t2
        (TArrow (ty1, ty2), vars2)

    | TEVar name ->
        // Type variable: 'a, 'b, etc.
        // If already seen in this scope, reuse index
        // If new, allocate fresh index and record it
        match Map.tryFind name vars with
        | Some idx -> (TVar idx, vars)
        | None ->
            let idx = freshTypeVarIndex()
            let vars' = Map.add name idx vars
            (TVar idx, vars')
```

**핵심 포인트**:

- 같은 이름의 타입 변수(`'a`)는 같은 `TVar` 인덱스를 공유
- 환경을 threading하여 스코프 내 일관성 유지

### API 함수

```fsharp
/// Elaborate single type expression with fresh scope
let elaborateTypeExpr (te: TypeExpr): Type =
    let (ty, _) = elaborateWithVars Map.empty te
    ty

/// Elaborate multiple type expressions sharing the same scope
/// Used for curried function parameters: fun (x: 'a) (y: 'a) -> ...
/// Both 'a refer to the same type variable
let elaborateScoped (tes: TypeExpr list): Type list =
    let folder (acc, env) te =
        let (ty, env') = elaborateWithVars env te
        (ty :: acc, env')
    let (revTypes, _) = List.fold folder ([], Map.empty) tes
    List.rev revTypes
```

## Bidir.fs: 핵심 구현

양방향 타입 체킹의 두 가지 모드:

- **synth (합성)**: 표현식 -> 타입 (bottom-up)
- **check (검사)**: 표현식 + 예상 타입 -> 검증 (top-down)

```fsharp
module Bidir

open Ast
open Type
open Unify
open Elaborate
open Diagnostic
open Infer  // Reuse freshVar, instantiate, generalize
```

### Synthesis Mode (synth)

대부분의 표현식은 타입을 합성한다:

```fsharp
/// Synthesize type for expression (inference mode)
/// Returns: (substitution, inferred type)
let rec synth (ctx: InferContext list) (env: TypeEnv) (expr: Expr): Subst * Type =
    match expr with
    // === Literals ===
    | Number (_, _) -> (empty, TInt)
    | Bool (_, _) -> (empty, TBool)
    | String (_, _) -> (empty, TString)

    // === Variables ===
    | Var (name, span) ->
        match Map.tryFind name env with
        | Some scheme -> (empty, instantiate scheme)
        | None -> raise (TypeException { Kind = UnboundVar name; ... })

    // === Lambda (unannotated) - HYBRID approach ===
    | Lambda (param, body, _) ->
        let paramTy = freshVar()
        let bodyEnv = Map.add param (Scheme ([], paramTy)) env
        let s, bodyTy = synth ctx bodyEnv body
        (s, TArrow (apply s paramTy, bodyTy))
```

**HYBRID approach**: 어노테이션 없는 람다는 Algorithm W처럼 fresh 타입 변수를 사용하여 하위 호환성을 유지한다.

### Annotated Expressions

어노테이션이 있는 표현식은 checking mode로 전환:

```fsharp
    // === LambdaAnnot (annotated lambda) ===
    | LambdaAnnot (param, paramTyExpr, body, span) ->
        let paramTy = elaborateTypeExpr paramTyExpr
        let ctx' = InCheckMode (paramTy, "annotation", span) :: ctx
        let bodyEnv = Map.add param (Scheme ([], paramTy)) env
        let s, bodyTy = synth ctx' bodyEnv body
        (s, TArrow (apply s paramTy, bodyTy))

    // === Annot (type annotation) ===
    | Annot (e, tyExpr, span) ->
        let expectedTy = elaborateTypeExpr tyExpr
        let ctx' = InCheckMode (expectedTy, "annotation", span) :: ctx
        let s = check ctx' env e expectedTy
        (s, apply s expectedTy)
```

`InCheckMode` 컨텍스트는 에러 발생 시 어노테이션 위치를 표시하는 데 사용된다.

### Checking Mode (check)

예상 타입에 대해 표현식을 검사한다:

```fsharp
/// Check expression against expected type (checking mode)
/// Returns: substitution that makes expression have expected type
and check (ctx: InferContext list) (env: TypeEnv) (expr: Expr) (expected: Type): Subst =
    match expr with
    // === Lambda against TArrow (BIDIR-04) ===
    | Lambda (param, body, _) ->
        match expected with
        | TArrow (paramTy, resultTy) ->
            let bodyEnv = Map.add param (Scheme ([], paramTy)) env
            let s = check ctx bodyEnv body resultTy
            let s' = unifyWithContext ctx [] (spanOf expr) (apply s paramTy) paramTy
            compose s' s
        | _ ->
            // Not an arrow type - fall through to subsumption
            let s, actual = synth ctx env expr
            let s' = unifyWithContext ctx [] (spanOf expr) (apply s expected) actual
            compose s' s
```

람다를 화살표 타입에 대해 체크할 때, 파라미터 타입을 직접 사용하고 바디를 결과 타입에 대해 체크한다.

### Subsumption (폴백)

특별한 체킹 규칙이 없으면 합성 후 단일화:

```fsharp
    // === Fallback subsumption (BIDIR-06) ===
    | _ ->
        let s, actual = synth ctx env expr
        let s' = unifyWithContext ctx [] (spanOf expr) (apply s expected) actual
        compose s' s
```

### Let-polymorphism 보존

Let 바인딩에서 다형성은 Algorithm W와 동일하게 동작:

```fsharp
    // === Let (BIDIR-07 - let-polymorphism) ===
    | Let (name, value, body, span) ->
        let s1, valueTy = synth (InLetRhs (name, span) :: ctx) env value
        let env' = applyEnv s1 env
        let scheme = generalize env' (apply s1 valueTy)
        let bodyEnv = Map.add name scheme env'
        let s2, bodyTy = synth (InLetBody (name, span) :: ctx) bodyEnv body
        (compose s2 s1, bodyTy)
```

### Top-level Entry Point

```fsharp
/// Top-level entry: infer type for expression
let synthTop (env: TypeEnv) (expr: Expr): Type =
    let s, ty = synth [] env expr
    apply s ty
```

## 사용 예제

### 기본 어노테이션

```bash
$ dotnet run --project FunLang -- --emit-type -e '(42 : int)'
int

$ dotnet run --project FunLang -- --emit-type -e '(true : bool)'
bool
```

### 람다 어노테이션

```bash
$ dotnet run --project FunLang -- --emit-type -e 'fun (x: int) -> x + 1'
int -> int

$ dotnet run --project FunLang -- --emit-type -e 'fun (x: int) (y: int) -> x + y'
int -> int -> int
```

### 표현식 어노테이션

```bash
$ dotnet run --project FunLang -- --emit-type -e '(fun x -> x : int -> int)'
int -> int

$ dotnet run --project FunLang -- --emit-type -e '(let x = 5 in x + 1 : int)'
int
```

### 다형성 (Let-polymorphism)

```bash
$ dotnet run --project FunLang -- --emit-type -e 'let id = fun x -> x in id'
'm -> 'm

$ dotnet run --project FunLang -- --emit-type -e 'let id = fun x -> x in (id 5, id true)'
int * bool
```

### 타입 에러

어노테이션이 있으면 에러 메시지에 출처가 표시된다:

```bash
$ dotnet run --project FunLang -- --emit-type -e '(true : int)'
error[E0301]: Type mismatch: expected int but got bool
 --> <expr>:1:1-5
   = due to annotation: <expr>:1:0-12
   = note: expected int due to annotation at <expr>:1:0-12
   = hint: The type annotation at <expr>:1:0-12 expects int
```

```bash
$ dotnet run --project FunLang -- --emit-type -e 'fun (x: int) -> x && true'
error[E0301]: Type mismatch: expected int but got bool
 --> <expr>:1:16-17
   = due to annotation: <expr>:1:0-25
   = note: expected int due to annotation at <expr>:1:0-25
   = hint: The type annotation at <expr>:1:0-25 expects int
```

## 에러 메시지 개선

양방향 타입 체킹의 주요 이점 중 하나는 더 나은 에러 메시지다.

### InCheckMode 컨텍스트

`Diagnostic.fs`에 정의된 컨텍스트:

```fsharp
type InferContext =
    // ... 기존 컨텍스트
    | InCheckMode of expected: Type * source: string * Span
```

어노테이션이 있으면 에러 발생 시 이 컨텍스트를 통해 "왜 이 타입이 예상되었는지" 설명할 수 있다:

| 에러 유형 | Algorithm W | Bidirectional |
|----------|-------------|---------------|
| 타입 불일치 | "Cannot unify int with bool" | "expected int but got bool" + 어노테이션 위치 |
| 소스 추적 | 없음 | "due to annotation at ..." |
| 힌트 | 일반적 | 어노테이션 특화 힌트 |

## 구현 세부 사항

### synth vs check 선택

| 표현식 | synth | check 활용 |
|--------|-------|-----------|
| 리터럴 | O | - |
| 변수 | O | - |
| 함수 적용 | O | - |
| 람다 (어노테이션 없음) | O | - |
| 람다 (어노테이션 있음) | O (파라미터 타입 사용) | - |
| 어노테이션 `(e : T)` | O (check 호출) | O |
| if-then-else | O | O (브랜치 체크 시) |

### Fresh Variable 범위

```fsharp
// Elaborate.fs: 사용자 어노테이션용
let freshTypeVarIndex =
    let counter = ref 0  // 0, 1, 2, ...
    fun () -> ...

// Infer.fs: 추론용
let freshVar =
    let counter = ref 1000  // 1000, 1001, 1002, ...
    fun () -> TVar (!counter; counter := !counter + 1; !counter - 1)
```

### 하위 호환성

어노테이션 없는 코드는 기존 Algorithm W와 동일하게 동작한다:

```funlang
// 기존 코드 (변경 없이 동작)
fun x -> x + 1            // int -> int
let id = fun x -> x in id // 'a -> 'a

// 새 문법 (선택적 사용)
fun (x: int) -> x + 1     // int -> int (어노테이션)
(fun x -> x : int -> int) // int -> int (어노테이션)
```

## 정리

이 장에서 구현한 양방향 타입 체킹:

| 기능 | 파일 | 설명 |
|------|------|------|
| 타입 표현식 AST | `Ast.fs` | TypeExpr (TEInt, TEArrow, TEVar, ...) |
| 어노테이션 AST | `Ast.fs` | Annot, LambdaAnnot 노드 |
| 타입 정교화 | `Elaborate.fs` | TypeExpr -> Type 변환 |
| 양방향 체킹 | `Bidir.fs` | synth/check 함수 |
| 에러 컨텍스트 | `Diagnostic.fs` | InCheckMode 추가 |
| CLI 통합 | `TypeCheck.fs` | synthTop 호출 |

**핵심 개념**:

| 개념 | 설명 |
|------|------|
| Synthesis | 표현식에서 타입 추론 (bottom-up) |
| Checking | 예상 타입으로 표현식 검증 (top-down) |
| Subsumption | 체크 모드에서 합성 후 단일화로 폴백 |
| 어노테이션 | `(e : T)`, `fun (x: T) -> e` 문법 |
| 에러 출처 | 어노테이션 위치가 에러 메시지에 포함 |

## 테스트

```bash
# 어노테이션 테스트 (type-inference)
make -C tests type-inference

# 어노테이션 에러 테스트 (type-errors)
make -C tests type-errors

# 전체 fslit 테스트
make -C tests

# Expecto 단위 테스트
dotnet run --project FunLang.Tests
```

## 소스 참조

전체 소스 코드는 다음 위치에서 확인할 수 있다:

- **FunLang/Ast.fs**: TypeExpr, Annot, LambdaAnnot 정의
- **FunLang/Elaborate.fs**: 타입 표현식 정교화
- **FunLang/Bidir.fs**: 양방향 타입 체커 (synth/check)
- **FunLang/Diagnostic.fs**: InCheckMode 컨텍스트
- **FunLang/TypeCheck.fs**: 진입점 (synthTop 호출)

## 관련 문서

- [Chapter 10: Type System](chapter-10-type-system.md) - Hindley-Milner Algorithm W
- [Chapter 11: Type Error Diagnostics](chapter-11-type-error-diagnostics.md) - 에러 메시지 개선
