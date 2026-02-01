FunLang 확장 가능 기능 조사 보고서

## 개요

현재 FunLang은 Turing-complete 언어로서 기본 기능을 갖추고 있습니다:
- 사칙연산, 변수, 조건문, 함수, 재귀, 클로저

아래는 마일스톤별로 분류한 확장 가능 기능들입니다.

---

## Milestone 2: 실용성 강화 (v2.0)

**예상 기간:** 1-2주
**목표:** 실제 사용 가능한 언어로 발전

### Phase 1: REPL (Read-Eval-Print Loop)

**설명:** 대화형 셸로 즉시 코드를 실행하고 결과 확인

**구현 요소:**
- 입력 루프와 프롬프트
- 히스토리 (상/하 화살표)
- 멀티라인 입력 지원
- 상태 유지 (이전 정의 기억)

**예시:**
```
funlang> let x = 5
val x = 5
funlang> x + 10
15
funlang> let f = fun y -> x + y
val f = <function>
funlang> f 3
8
```

**참고:** F#의 `System.Console.ReadLine()` 사용, readline 라이브러리 통합 가능

---

### Phase 2: 문자열 타입

**설명:** 문자열 리터럴과 연산 지원

**구현 요소:**
```fsharp
type Value =
    | IntValue of int
    | BoolValue of bool
    | StringValue of string
    | FunctionValue of ...
```

**연산:**
- 연결: `"hello" ^ " world"` → `"hello world"`
- 길이: `length "abc"` → `3`
- 비교: `"a" < "b"` → `true`

---

### Phase 3: 주석 (Comments)

**설명:** 코드 문서화 지원

**구현:**
```fsharp
// Lexer.fsl
| "//" [^ '\n']* { tokenize lexbuf }           // 한 줄 주석
| "(*" ([^*] | '*'[^)])* "*)" { tokenize lexbuf }  // 블록 주석
```

---

## Milestone 3: 데이터 구조 (v3.0)

**예상 기간:** 2-3주
**목표:** 복합 데이터 타입, 패턴 매칭, 표준 라이브러리

### Phase 1: 튜플 (Tuples)

**설명:** 고정 크기 이종 데이터 컬렉션

**예시:**
```
let pair = (1, true) in
let (x, y) = pair in
x + (if y then 1 else 0)
```

**구현:**
```fsharp
type Expr =
    | Tuple of Expr list
    | TupleGet of Expr * int  // 또는 패턴 매칭으로
```

---

### Phase 2: 리스트 (Lists)

**설명:** 동종 데이터의 가변 길이 컬렉션

**구문:**
```
let xs = [1, 2, 3] in
let head = hd xs in      // 1
let tail = tl xs in      // [2, 3]
let ys = 0 :: xs in      // [0, 1, 2, 3]
```

**AST 확장:**
```fsharp
type Expr =
    | ListEmpty                    // []
    | ListCons of Expr * Expr      // head :: tail
    | ListHead of Expr             // hd
    | ListTail of Expr             // tl

type Value =
    | ListValue of Value list
```

---

### Phase 3: 패턴 매칭 (Pattern Matching)

**설명:** 구조적 분해와 조건 분기의 통합

**구문:**
```
match xs with
| [] -> 0
| h :: t -> h + sum t
```

**구현 복잡도:**
1. 패턴 AST 정의
2. 매칭 알고리즘 (깊이 우선)
3. 바인딩 추출
4. 완전성 검사 (선택적)

**AST:**
```fsharp
type Pattern =
    | PWildcard                    // _
    | PVar of string               // x
    | PConst of int                // 42
    | PCons of Pattern * Pattern   // h :: t
    | PTuple of Pattern list       // (x, y)
```

---

### Phase 4: Prelude (표준 라이브러리)

**설명:** 자주 사용되는 함수들을 미리 정의하여 제공

**구현 요소:**
- 시작 시 자동 로드되는 정의들
- 리스트 함수: `map`, `filter`, `fold`, `length`, `reverse`, `append`
- 수학 함수: `abs`, `max`, `min`
- 유틸리티: `id`, `const`, `compose`

**예시:**
```
// Prelude.fun (자동 로드)
let id x = x
let const x y = x
let compose f g x = f (g x)

let rec map f xs =
    match xs with
    | [] -> []
    | h :: t -> f h :: map f t

let rec filter p xs =
    match xs with
    | [] -> []
    | h :: t -> if p h then h :: filter p t else filter p t

let rec fold f acc xs =
    match xs with
    | [] -> acc
    | h :: t -> fold f (f acc h) t

let length xs = fold (fun n _ -> n + 1) 0 xs
let sum xs = fold (fun a b -> a + b) 0 xs
let reverse xs = fold (fun acc x -> x :: acc) [] xs
```

**구현:**
```fsharp
// 시작 시 Prelude 로드
let initialEnv =
    let preludeCode = File.ReadAllText "Prelude.fun"
    let preludeAst = parse preludeCode
    eval Map.empty preludeAst
```

**의존성:** 리스트, 패턴 매칭 필요

---

## Milestone 4: 안정성 강화 (v4.0)

**예상 기간:** 2-4주
**목표:** TCO, ADT, 에러 처리로 안정성 확보

### Phase 1: 꼬리 호출 최적화 (Tail Call Optimization)

**설명:** 꼬리 위치 재귀 호출을 루프로 변환하여 스택 오버플로 방지

**현재 문제:**
```
let rec count n = if n <= 0 then 0 else count (n - 1)
count 100000  // 스택 오버플로!
```

**해결책 - Trampoline 패턴:**
```fsharp
type TrampolineResult<'a> =
    | Done of 'a
    | More of (unit -> TrampolineResult<'a>)

let rec run = function
    | Done v -> v
    | More f -> run (f())
```

**또는 CPS 변환:**
- 모든 함수를 continuation-passing style로 변환
- 스택 대신 힙에 continuation 저장

---

### Phase 2: 대수적 데이터 타입 (Algebraic Data Types)

**설명:** 합 타입(Sum Type)과 곱 타입(Product Type) 정의

**구문:**
```
type Option = None | Some of int

type List = Nil | Cons of int * List

let x = Some 42 in
match x with
| None -> 0
| Some n -> n
```

**구현:**
```fsharp
type TypeDef = {
    name: string
    constructors: (string * Type option) list
}

type Value =
    | ConstructorValue of name: string * value: Value option
```

---

### Phase 3: 에러 처리

**설명:** 예외 또는 Result 타입 기반 에러 처리

**Option A - 예외:**
```
try
    10 / x
with DivisionByZero ->
    0
```

**Option B - Result 타입:**
```
type Result = Ok of int | Error of string

let safe_div a b =
    if b = 0 then Error "division by zero"
    else Ok (a / b)
```

---

## Milestone 5: 타입 시스템 (v5.0)

**예상 기간:** 3-4주
**목표:** 정적 타입 검사와 타입 추론

### Phase 1: 타입 시스템 (Type System)

**설명:** 정적 타입 검사로 런타임 에러 방지

**단계별 구현:**

#### 10a. 기본 타입 주석
```
let x: int = 5
let f: int -> int = fun x -> x + 1
```

#### 10b. Hindley-Milner 타입 추론

**알고리즘 W:**
1. 각 표현식에 타입 변수 할당
2. 제약 조건 수집
3. 단일화(Unification) 알고리즘으로 해결

```fsharp
type Type =
    | TInt
    | TBool
    | TArrow of Type * Type   // 함수 타입
    | TVar of int             // 타입 변수

type Constraint = Type * Type  // 같아야 하는 타입 쌍
```

**핵심 함수:**
```fsharp
// 타입 추론
let rec infer (env: TypeEnv) (expr: Expr) : Type * Constraint list

// 단일화
let rec unify (constraints: Constraint list) : Substitution
```

**참고 자료:**
- [Write You a Haskell - Hindley-Milner](http://dev.stephendiehl.com/fun/006_hindley_milner.html)
- [Algorithm W Step by Step](https://github.com/wh5a/Algorithm-W-Step-By-Step)

---

### Phase 2: 모듈 시스템 (Module System)

**설명:** 코드 조직화와 네임스페이스 관리

**기본 수준:**
```
module Math =
    let pi = 3
    let square x = x * x
end

Math.square 5
```

**고급 수준 (ML-style Functors):**
```
module type Comparable = sig
    type t
    val compare: t -> t -> int
end

module MakeSet(C: Comparable) = struct
    type t = C.t list
    let empty = []
    let add x s = x :: s
end
```

---

## Milestone 6: 성능 & 고급 기능 (v6.0)

**예상 기간:** 1-2개월
**목표:** 성능 최적화와 고급 언어 기능

### Phase 1: 바이트코드 VM

**설명:** 인터프리터 대신 바이트코드로 컴파일 후 VM에서 실행

**장점:**
- 성능 향상 (2-10배)
- 플랫폼 독립적
- 최적화 가능

**아키텍처:**
```
Source → Lexer → Parser → AST → Compiler → Bytecode → VM → Result
```

**바이트코드 명령어 예시:**
```fsharp
type Instruction =
    | PUSH of int
    | ADD | SUB | MUL | DIV
    | LOAD of string      // 변수 로드
    | STORE of string     // 변수 저장
    | JUMP of int         // 점프
    | JUMP_IF_FALSE of int
    | CALL of int         // 함수 호출
    | RET                 // 리턴
```

**스택 기반 VM:**
```fsharp
type VM = {
    code: Instruction array
    mutable ip: int           // 명령 포인터
    stack: Value Stack
    frames: Frame Stack       // 호출 스택
}

let rec run (vm: VM) =
    match vm.code.[vm.ip] with
    | PUSH n ->
        Stack.push (IntValue n) vm.stack
        vm.ip <- vm.ip + 1
        run vm
    | ADD ->
        let b = Stack.pop vm.stack
        let a = Stack.pop vm.stack
        Stack.push (a + b) vm.stack
        vm.ip <- vm.ip + 1
        run vm
    // ...
```

**참고:** [Crafting Interpreters](https://craftinginterpreters.com) - Clox 챕터

---

### Phase 2: 지연 평가 (Lazy Evaluation)

**설명:** 값이 필요할 때까지 평가를 미룸

**구현 - Thunk:**
```fsharp
type Value =
    | Thunk of Expr * Env * mutable Option<Value>

let force = function
    | Thunk (expr, env, cache) ->
        match !cache with
        | Some v -> v
        | None ->
            let v = eval env expr
            cache := Some v
            v
    | v -> v
```

**장점:**
- 무한 데이터 구조 가능
- 불필요한 계산 회피

**예시:**
```
let ones = 1 :: ones      // 무한 리스트
take 5 ones               // [1, 1, 1, 1, 1]
```

---

### Phase 3: 대수적 효과 (Algebraic Effects)

**설명:** 부수 효과를 일급 객체로 다루는 현대적 패러다임

**개념:**
```
effect Exception
effect State of int

let divide a b =
    if b = 0 then perform Exception
    else a / b

handle (divide 10 0) with
| val x -> x
| Exception -> 0
```

**장점:**
- 모나드보다 조합성 좋음
- 효과 시스템으로 타입 안전성

**참고:**
- [Eff 언어](https://www.eff-lang.org/)
- [Koka 언어](https://koka-lang.github.io/)
- [Effekt 언어](https://effekt-lang.org/)

---

## 마일스톤 의존성 그래프

```
┌─────────────────────────────────────────────────────────────────┐
│                      v1.0 (완료)                                │
│         사칙연산, 변수, 조건문, 함수, 재귀, 클로저              │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  Milestone 2: 실용성 강화 (v2.0)                                │
│  ┌────────┐    ┌────────┐    ┌────────┐                         │
│  │  REPL  │    │ 문자열 │    │  주석  │                         │
│  └────────┘    └────────┘    └────────┘                         │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  Milestone 3: 데이터 구조 (v3.0)                                │
│  ┌────────┐    ┌────────┐    ┌────────────┐    ┌─────────┐      │
│  │  튜플  │───▶│ 리스트 │───▶│ 패턴 매칭  │───▶│ Prelude │      │
│  └────────┘    └────────┘    └────────────┘    └─────────┘      │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  Milestone 4: 안정성 강화 (v4.0)                                │
│  ┌────────┐    ┌────────┐    ┌────────────┐                     │
│  │  TCO   │    │  ADT   │    │ 에러 처리  │                     │
│  └────────┘    └────────┘    └────────────┘                     │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  Milestone 5: 타입 시스템 (v5.0)                                │
│  ┌──────────────┐    ┌──────────────┐                           │
│  │  타입 추론   │───▶│  모듈 시스템 │                           │
│  └──────────────┘    └──────────────┘                           │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│  Milestone 6: 성능 & 고급 (v6.0)                                │
│  ┌────────────┐   ┌────────────┐   ┌────────────┐               │
│  │바이트코드VM│   │ 지연 평가  │   │대수적 효과 │               │
│  └────────────┘   └────────────┘   └────────────┘               │
└─────────────────────────────────────────────────────────────────┘
```

---

## 마일스톤 요약

| 마일스톤 | 버전 | 예상 기간 | 주요 기능 |
|----------|------|-----------|-----------|
| **v1.0** (완료) | v1.0 | - | 사칙연산, 변수, 조건문, 함수, 재귀, 클로저 |
| **Milestone 2** | v2.0 | 1-2주 | REPL, 문자열, 주석 |
| **Milestone 3** | v3.0 | 2-3주 | 튜플, 리스트, 패턴 매칭, Prelude |
| **Milestone 4** | v4.0 | 2-4주 | TCO, ADT, 에러 처리 |
| **Milestone 5** | v5.0 | 3-4주 | 타입 시스템, 모듈 시스템 |
| **Milestone 6** | v6.0 | 1-2개월 | 바이트코드 VM, 지연 평가, 대수적 효과 |

---

## 학습 자료

| 주제 | 자료 |
|------|------|
| 전반적 | [Crafting Interpreters](https://craftinginterpreters.com) |
| 타입 추론 | [Write You a Haskell](http://dev.stephendiehl.com/fun/) |
| 패턴 매칭 | [ML for the Working Programmer](https://www.cl.cam.ac.uk/~lp15/MLbook/) |
| VM | [Crafting Interpreters - Clox](https://craftinginterpreters.com/a-bytecode-virtual-machine.html) |
| 대수적 효과 | [An Introduction to Algebraic Effects](https://overreacted.io/algebraic-effects-for-the-rest-of-us/) |
