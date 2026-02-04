---
created: 2026-01-30
description: FsYacc에서 %left/%right 대신 문법 구조로 연산자 우선순위 처리
---

# FsYacc: 문법 기반 연산자 우선순위 (Expr/Term/Factor 패턴)

FsYacc의 `%left`/`%right` 선언에는 알려진 버그가 있다. 문법 구조 자체로 우선순위를 인코딩하면 이 문제를 피할 수 있다.

## The Insight

**연산자 우선순위는 문법 계층으로 표현할 수 있다.** 낮은 우선순위 연산자를 높은 문법 레벨에, 높은 우선순위 연산자를 낮은 문법 레벨에 배치하면 파서가 자연스럽게 올바른 순서로 파싱한다.

```
Expr (+ -)     ← 낮은 우선순위 = 높은 문법 레벨
  └── Term (* /)   ← 높은 우선순위 = 낮은 문법 레벨
        └── Factor (숫자, 괄호, 단항)  ← 가장 높은 우선순위
```

## Why This Matters

FsYacc의 `%left`/`%right` 선언에는 버그가 있다:
- **Issue #39**: `%nonassoc`가 올바르게 처리되지 않음
- **Issue #40**: `%left`가 때때로 `%right`처럼 동작함

**증상:**
- `2 - 3 - 4`가 `-5`가 아닌 `3`으로 평가됨 (우결합으로 파싱)
- 예상치 못한 shift-reduce 충돌
- 단항 마이너스 `%prec`가 작동하지 않음

## Recognition Pattern

다음 상황에서 이 패턴을 사용한다:

- FsYacc로 산술/논리 표현식 파서를 구현할 때
- `%left`/`%right` 선언 후 예상과 다른 결과가 나올 때
- 연산자 우선순위가 필요한 모든 FsYacc 문법

## The Approach

**문법 계층화**: 우선순위별로 논터미널을 분리한다.

1. **Expr**: 가장 낮은 우선순위 (+, -)
2. **Term**: 중간 우선순위 (*, /)
3. **Factor**: 가장 높은 우선순위 (숫자, 괄호, 단항 연산자)

각 레벨은 자신보다 높은 우선순위의 논터미널만 참조한다.

### Step 1: 문법 계층 설계

```
start → Expr
Expr  → Expr (+ | -) Term | Term
Term  → Term (* | /) Factor | Factor
Factor → NUMBER | ( Expr ) | - Factor
```

### Step 2: Parser.fsy 작성

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

### Step 3: 핵심 포인트 확인

1. **좌재귀 유지**: `Expr: Expr PLUS Term`는 좌결합을 보장한다
2. **단항 마이너스는 Factor에서**: 가장 높은 우선순위를 자연스럽게 얻는다
3. **괄호는 Expr로 재귀**: `( Expr )`로 우선순위 오버라이드

## Example

**입력**: `2 + 3 * 4`

**파싱 과정**:
```
Expr
├── Expr → Term → Factor → 2
├── PLUS
└── Term
    ├── Term → Factor → 3
    ├── STAR
    └── Factor → 4
```

**결과 AST**: `Add(Number 2, Multiply(Number 3, Number 4))`

**입력**: `2 - 3 - 4`

**파싱 과정** (좌결합):
```
Expr
├── Expr
│   ├── Expr → Term → Factor → 2
│   ├── MINUS
│   └── Term → Factor → 3
├── MINUS
└── Term → Factor → 4
```

**결과 AST**: `Subtract(Subtract(Number 2, Number 3), Number 4)` = -5

## 비교: 선언 방식 vs 문법 방식

| 방식 | 장점 | 단점 |
|------|------|------|
| `%left`/`%right` 선언 | 간결함 | FsYacc 버그, 디버깅 어려움 |
| Expr/Term/Factor 문법 | 버그 없음, 명시적, 교육적 | 문법이 길어짐 |

**결론**: FsYacc에서는 항상 문법 방식을 사용한다.

## 체크리스트

- [ ] 각 우선순위 레벨에 별도 논터미널이 있는가?
- [ ] 좌재귀로 좌결합이 보장되는가? (`Expr: Expr PLUS Term`)
- [ ] 단항 연산자가 가장 낮은 레벨(Factor)에 있는가?
- [ ] 괄호가 가장 높은 레벨(Expr)로 재귀하는가?
- [ ] `%left`/`%right`/`%prec` 선언을 사용하지 않았는가?

## 관련 문서

- [write-fsyacc-parser](write-fsyacc-parser.md) - fsyacc 기본 문법
- [FsLexYacc Issue #39](https://github.com/fsprojects/FsLexYacc/issues/39) - %nonassoc 버그
- [FsLexYacc Issue #40](https://github.com/fsprojects/FsLexYacc/issues/40) - %left 버그
