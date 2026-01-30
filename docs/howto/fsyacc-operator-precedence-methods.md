---
created: 2026-01-30
description: FsYacc에서 연산자 우선순위를 처리하는 3가지 방법 비교
---

# FsYacc Operator Precedence Methods

FsYacc에서 연산자 우선순위를 처리하는 3가지 방법과 각각의 장단점.

## The Insight

LALR 파서에서 연산자 우선순위를 처리하는 방법은 크게 3가지다:

1. **Precedence Declarations** (`%left`, `%right`) — 선언적, 간결
2. **Grammar Stratification** (Expr/Term/Factor) — 문법 구조로 인코딩
3. **`%prec` Pseudo-tokens** — 규칙별 우선순위 오버라이드

각 방법은 다른 상황에 적합하다. FsYacc에서는 버그로 인해 방법 2가 가장 안전하다.

## Method 1: Precedence Declarations

`%left`, `%right`, `%nonassoc`로 토큰 우선순위를 선언한다.

```fsharp
// 낮은 우선순위부터 선언 (아래가 높음)
%left PLUS MINUS           // + - 는 좌결합, 낮은 우선순위
%left STAR SLASH           // * / 는 좌결합, 높은 우선순위
%nonassoc UMINUS           // 단항 마이너스, 가장 높은 우선순위

%%

expr:
    | expr PLUS expr   { Add($1, $3) }
    | expr STAR expr   { Multiply($1, $3) }
    | MINUS expr %prec UMINUS { Negate($2) }
```

**장점:**
- 간결함, 문법 규칙이 짧음
- 표준 yacc/bison 패턴

**단점:**
- ⚠️ **FsYacc 버그**: `%left`가 `%right`처럼 동작하는 경우 있음 (Issue #40)
- ⚠️ **FsYacc 버그**: `%nonassoc` 처리 오류 (Issue #39)
- 디버깅이 어려움 (왜 그렇게 파싱되는지 파악 힘듦)

**권장:** FsYacc에서는 사용하지 않는다.

## Method 2: Grammar Stratification (Expr/Term/Factor)

문법 계층 자체로 우선순위를 인코딩한다.

```fsharp
// 선언 없음 - 우선순위를 문법 구조로 표현

%%

start:
    | Expr EOF           { $1 }

Expr:                              // 낮은 우선순위 (+ -)
    | Expr PLUS Term     { Add($1, $3) }
    | Expr MINUS Term    { Subtract($1, $3) }
    | Term               { $1 }

Term:                              // 중간 우선순위 (* /)
    | Term STAR Factor   { Multiply($1, $3) }
    | Term SLASH Factor  { Divide($1, $3) }
    | Factor             { $1 }

Factor:                            // 높은 우선순위 (숫자, 괄호, 단항)
    | NUMBER             { Number($1) }
    | LPAREN Expr RPAREN { $2 }
    | MINUS Factor       { Negate($2) }
```

**장점:**
- FsYacc 버그 영향 없음
- 명시적이고 이해하기 쉬움
- 디버깅 용이 (파싱 과정이 문법에서 보임)
- 교육적 (컴파일러 교과서 패턴)

**단점:**
- 문법이 길어짐
- 우선순위 레벨마다 논터미널 필요

**권장:** FsYacc에서 기본 선택.

## Method 3: %prec Pseudo-tokens

특정 규칙에 다른 우선순위를 적용한다.

```fsharp
%left PLUS MINUS
%left STAR SLASH
%nonassoc UMINUS        // 실제 토큰이 아닌 가상 토큰

%%

expr:
    | expr PLUS expr            { Add($1, $3) }
    | expr STAR expr            { Multiply($1, $3) }
    | MINUS expr %prec UMINUS   { Negate($2) }  // UMINUS 우선순위 적용
```

`%prec UMINUS`는 "이 규칙은 UMINUS의 우선순위를 사용하라"는 의미다.

**용도:**
- 같은 토큰이 다른 맥락에서 다른 우선순위를 가질 때
- 예: `-`가 이항 연산자일 때 vs 단항 연산자일 때

**단점:**
- ⚠️ FsYacc에서 버그로 인해 작동하지 않을 수 있음
- 방법 1의 문제를 공유함

**권장:** FsYacc에서는 피하고, 방법 2로 대체한다.

## Comparison Table

| 방법 | 코드량 | FsYacc 안정성 | 디버깅 | 권장 |
|------|--------|---------------|--------|------|
| %left/%right | 적음 | ❌ 버그 있음 | 어려움 | ❌ |
| Expr/Term/Factor | 많음 | ✅ 안전 | 쉬움 | ✅ |
| %prec | 중간 | ❌ 버그 있음 | 어려움 | ❌ |

## When to Use Which

| 상황 | 권장 방법 |
|------|----------|
| FsYacc 사용 | 방법 2 (Grammar Stratification) |
| Bison/Yacc 사용 | 방법 1 또는 3 가능 |
| 교육/학습 목적 | 방법 2 (원리 이해에 좋음) |
| 우선순위 레벨이 많음 (5개 이상) | 방법 1 고려 (단, FsYacc 아닐 때) |

## Example: Complete Parser

**방법 2 (권장) 사용:**

```fsharp
%{
open Ast
%}

%token <int> NUMBER
%token PLUS MINUS STAR SLASH
%token LPAREN RPAREN
%token EOF

%start start
%type <Ast.Expr> start

%%

start:
    | Expr EOF           { $1 }

Expr:
    | Expr PLUS Term     { Add($1, $3) }
    | Expr MINUS Term    { Subtract($1, $3) }
    | Term               { $1 }

Term:
    | Term STAR Factor   { Multiply($1, $3) }
    | Term SLASH Factor  { Divide($1, $3) }
    | Factor             { $1 }

Factor:
    | NUMBER             { Number($1) }
    | LPAREN Expr RPAREN { $2 }
    | MINUS Factor       { Negate($2) }
```

**파싱 결과:**

| 입력 | 결과 | 설명 |
|------|------|------|
| `2 + 3 * 4` | `Add(2, Mul(3, 4))` = 14 | * 먼저 |
| `(2 + 3) * 4` | `Mul(Add(2, 3), 4)` = 20 | 괄호 우선 |
| `2 - 3 - 4` | `Sub(Sub(2, 3), 4)` = -5 | 좌결합 |
| `-5 + 3` | `Add(Neg(5), 3)` = -2 | 단항 먼저 |

## Checklist

- [ ] FsYacc 사용 시 `%left`/`%right` 선언을 피했는가?
- [ ] Grammar Stratification으로 우선순위를 인코딩했는가?
- [ ] 좌재귀로 좌결합을 보장했는가?
- [ ] 단항 연산자가 가장 낮은 논터미널(Factor)에 있는가?

## Related Documents

- [fsyacc-precedence-without-declarations](fsyacc-precedence-without-declarations.md) - 방법 2 상세 설명
- [write-fsyacc-parser](write-fsyacc-parser.md) - fsyacc 기본 문법
- [FsLexYacc Issue #39](https://github.com/fsprojects/FsLexYacc/issues/39) - %nonassoc 버그
- [FsLexYacc Issue #40](https://github.com/fsprojects/FsLexYacc/issues/40) - %left 버그
