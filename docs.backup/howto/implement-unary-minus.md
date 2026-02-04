---
created: 2026-01-30
description: 파서에서 단항 마이너스 구현 - 이항 마이너스와 구분, 우선순위 처리
---

# Implementing Unary Minus in Parsers

단항 마이너스(negation)와 이항 마이너스(subtraction)는 같은 `-` 토큰을 사용하지만, 파서에서 다르게 처리해야 한다.

## The Insight

**단항과 이항 연산자의 구분은 문법 위치(context)로 결정된다.** 렉서가 아닌 파서에서 구분한다.

- `5 - 3` → 이항 마이너스 (두 피연산자 사이)
- `-5` → 단항 마이너스 (피연산자 앞)
- `5 + -3` → 이항 PLUS + 단항 마이너스
- `--5` → 단항 마이너스 두 번

**핵심**: 렉서는 항상 `MINUS` 토큰 하나만 반환한다. 파서가 문맥에 따라 의미를 결정한다.

## Why This Matters

잘못 구현하면:
- `-5 + 3`이 `-(5 + 3) = -8` 대신 `-5 + 3 = -2`가 될 수 있음
- `1 - -2`가 파싱 에러
- shift-reduce 또는 reduce-reduce 충돌 발생

## Recognition Pattern

- 산술 표현식 파서를 구현할 때
- 같은 토큰이 단항/이항 연산자로 사용될 때 (-, +, !)
- "unary minus has higher precedence" 요구사항이 있을 때

## The Approach

### 핵심 원칙

1. **렉서**: `MINUS` 토큰 하나만 정의
2. **파서**: 문맥에 따라 두 가지 규칙으로 처리
3. **우선순위**: 단항 마이너스는 가장 높은 우선순위

### Step 1: AST에 Negate 노드 추가

```fsharp
type Expr =
    | Number of int
    | Add of Expr * Expr
    | Subtract of Expr * Expr      // 이항 마이너스
    | Multiply of Expr * Expr
    | Divide of Expr * Expr
    | Negate of Expr               // 단항 마이너스
```

**왜 별도 노드?**
- `Subtract(Number 0, e)`로 표현할 수 있지만, `-0`과 `0`이 다른 언어도 있음
- 의미적으로 명확함
- 최적화/분석에서 구분 필요

### Step 2: 렉서 - 단일 MINUS 토큰

```fsharp
// Lexer.fsl
rule tokenize = parse
    | '-'    { MINUS }   // 단일 토큰, 단항/이항 구분 없음
    // ...
```

**하지 말 것**: 렉서에서 `-5`를 음수 리터럴로 파싱하지 않는다.

```fsharp
// ❌ BAD: 렉서에서 음수 처리
| '-'? digit+  { NUMBER (Int32.Parse(lexeme lexbuf)) }

// 문제: -(1+2)를 처리할 수 없음
// 문제: 1--2 (1 - (-2))를 처리할 수 없음
```

### Step 3: 파서 - Factor 레벨에서 단항 처리

**Grammar Stratification 방식 (권장):**

```fsharp
// Parser.fsy

Expr:
    | Expr PLUS Term     { Add($1, $3) }
    | Expr MINUS Term    { Subtract($1, $3) }   // 이항 마이너스
    | Term               { $1 }

Term:
    | Term STAR Factor   { Multiply($1, $3) }
    | Term SLASH Factor  { Divide($1, $3) }
    | Factor             { $1 }

Factor:
    | NUMBER             { Number($1) }
    | LPAREN Expr RPAREN { $2 }
    | MINUS Factor       { Negate($2) }         // 단항 마이너스
```

**왜 Factor에서?**
- Factor는 가장 높은 우선순위
- `MINUS Factor`는 자기 자신을 재귀 호출 → `--5` 가능
- 괄호와 같은 레벨 → `-5 * 3`이 `(-5) * 3`으로 파싱

### Step 4: Evaluator

```fsharp
let rec eval expr =
    match expr with
    | Number n -> n
    | Add (a, b) -> eval a + eval b
    | Subtract (a, b) -> eval a - eval b
    | Multiply (a, b) -> eval a * eval b
    | Divide (a, b) -> eval a / eval b
    | Negate e -> -(eval e)
```

## Example

**파싱 예시:**

| 입력 | 파싱 결과 | 설명 |
|------|-----------|------|
| `-5` | `Negate(Number 5)` | 단항 마이너스 |
| `5 - 3` | `Subtract(Number 5, Number 3)` | 이항 마이너스 |
| `-5 + 3` | `Add(Negate(Number 5), Number 3)` | 단항이 먼저 |
| `--5` | `Negate(Negate(Number 5))` | 이중 부정 |
| `-(2 + 3)` | `Negate(Add(Number 2, Number 3))` | 표현식 부정 |
| `1 - -2` | `Subtract(Number 1, Negate(Number 2))` | 혼합 |

**파싱 과정 (`-5 * 3`):**

```
Term
├── Term → Factor → MINUS Factor → -5
├── STAR
└── Factor → 3

결과: Multiply(Negate(Number 5), Number 3) = -15
```

## Alternative: %prec 방식 (비권장)

FsYacc에서는 버그가 있지만, 다른 파서 생성기에서는 작동:

```fsharp
%left PLUS MINUS
%left STAR SLASH
%nonassoc UMINUS           // 가상 토큰

%%

expr:
    | expr MINUS expr            { Subtract($1, $3) }
    | MINUS expr %prec UMINUS    { Negate($2) }
    // ...
```

`%prec UMINUS`는 이 규칙이 UMINUS의 우선순위를 사용하도록 지정한다.

**FsYacc에서는 사용하지 않는다** (버그 있음).

## Common Mistakes

| 실수 | 증상 | 해결 |
|------|------|------|
| 렉서에서 `-5` 파싱 | `-(1+2)` 파싱 실패 | 렉서는 MINUS만, 파서에서 처리 |
| 단항을 Expr에서 처리 | 우선순위 오류 | Factor에서 처리 |
| `MINUS expr` 사용 | `--5` 파싱 실패 | `MINUS Factor` 사용 (자기 재귀) |
| Negate 없이 Subtract(0, e) | 의미 불명확 | 별도 Negate 노드 사용 |

## Checklist

- [ ] 렉서가 MINUS 토큰 하나만 반환하는가?
- [ ] AST에 Negate 노드가 있는가?
- [ ] 단항 마이너스가 Factor 레벨에서 처리되는가?
- [ ] `MINUS Factor` 형태로 자기 재귀하는가?
- [ ] `-5 + 3 = -2`, `--5 = 5`, `-(2+3) = -5` 테스트 통과?

## Related Documents

- [fsyacc-precedence-without-declarations](fsyacc-precedence-without-declarations.md) - Grammar Stratification 상세
- [fsyacc-operator-precedence-methods](fsyacc-operator-precedence-methods.md) - 우선순위 처리 방법 비교
- [write-fsyacc-parser](write-fsyacc-parser.md) - fsyacc 기본 문법
- [FsLexYacc Issue #39](https://github.com/fsprojects/FsLexYacc/issues/39) - %nonassoc 버그
- [FsLexYacc Issue #40](https://github.com/fsprojects/FsLexYacc/issues/40) - %prec 관련 버그
