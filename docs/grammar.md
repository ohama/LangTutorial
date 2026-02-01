# FunLang Grammar

FunLang의 공식 문법 명세. BNF (Backus-Naur Form) 표기법 사용.

## 표기법

| 기호 | 의미 |
|------|------|
| `::=` | 정의 |
| `\|` | 대안 (또는) |
| `[ ]` | 선택적 (0 또는 1회) |
| `{ }` | 반복 (0회 이상) |
| `( )` | 그룹화 |
| `"text"` | 리터럴 (키워드, 연산자) |
| `<name>` | 비단말 기호 |

## 구문 문법 (Syntactic Grammar)

### 프로그램

```bnf
<program> ::= <expr>
```

### 표현식

```bnf
<expr> ::= <let-expr>
         | <let-rec-expr>
         | <let-pat-expr>
         | <if-expr>
         | <match-expr>
         | <lambda-expr>
         | <or-expr>
```

### Let 표현식

```bnf
<let-expr>     ::= "let" <ident> "=" <expr> "in" <expr>
<let-rec-expr> ::= "let" "rec" <ident> <ident> "=" <expr> "in" <expr>
<let-pat-expr> ::= "let" <tuple-pat> "=" <expr> "in" <expr>
```

### 조건 표현식

```bnf
<if-expr> ::= "if" <expr> "then" <expr> "else" <expr>
```

### Match 표현식

```bnf
<match-expr>    ::= "match" <expr> "with" <match-clauses>
<match-clauses> ::= <match-clause> { <match-clause> }
<match-clause>  ::= "|" <pattern> "->" <expr>
```

### 람다 표현식

```bnf
<lambda-expr> ::= "fun" <ident> "->" <expr>
```

### 논리 표현식

```bnf
<or-expr>  ::= <and-expr> { "||" <and-expr> }
<and-expr> ::= <cmp-expr> { "&&" <cmp-expr> }
```

### 비교 표현식

```bnf
<cmp-expr> ::= <cons-expr> [ <cmp-op> <cons-expr> ]
<cmp-op>   ::= "=" | "<" | ">" | "<=" | ">=" | "<>"
```

### Cons 표현식

```bnf
<cons-expr> ::= <add-expr> { "::" <add-expr> }
```

우결합성: `1 :: 2 :: []` = `1 :: (2 :: [])`

### 산술 표현식

```bnf
<add-expr>  ::= <mul-expr> { ("+" | "-") <mul-expr> }
<mul-expr>  ::= <unary-expr> { ("*" | "/") <unary-expr> }
<unary-expr> ::= "-" <unary-expr>
               | <app-expr>
```

좌결합성: `2 - 3 - 4` = `(2 - 3) - 4`

### 함수 적용

```bnf
<app-expr> ::= <atom> { <atom> }
```

좌결합성: `f 1 2` = `(f 1) 2`

### 원자 표현식

```bnf
<atom> ::= <number>
         | <ident>
         | <bool>
         | <string>
         | <tuple>
         | <list>
         | "(" <expr> ")"

<bool>  ::= "true" | "false"
<tuple> ::= "(" <expr> "," <expr-list> ")"
<list>  ::= "[" "]"
          | "[" <expr-list> "]"

<expr-list> ::= <expr> { "," <expr> }
```

### 패턴

```bnf
<pattern> ::= <ident>
            | "_"
            | <number>
            | <bool>
            | <empty-list-pat>
            | <cons-pat>
            | <tuple-pat>

<empty-list-pat> ::= "[" "]"
<cons-pat>       ::= <pattern> "::" <pattern>
<tuple-pat>      ::= "(" <pattern-list> ")"

<pattern-list> ::= <pattern> "," <pattern> { "," <pattern> }
```

## 어휘 문법 (Lexical Grammar)

### 식별자와 키워드

```bnf
<ident>       ::= <ident-start> { <ident-char> }
<ident-start> ::= <letter> | "_"
<ident-char>  ::= <letter> | <digit> | "_"

<letter> ::= "a" | ... | "z" | "A" | ... | "Z"
<digit>  ::= "0" | ... | "9"

<keyword> ::= "let" | "in" | "rec"
            | "if" | "then" | "else"
            | "true" | "false"
            | "fun" | "match" | "with"
```

### 리터럴

```bnf
<number> ::= <digit> { <digit> }

<string>      ::= '"' { <string-char> } '"'
<string-char> ::= <any-char-except-quote-newline>
                | <escape-seq>
<escape-seq>  ::= "\n" | "\t" | "\\" | "\""
```

### 연산자

```bnf
<arith-op>   ::= "+" | "-" | "*" | "/"
<cmp-op>     ::= "=" | "<" | ">" | "<=" | ">=" | "<>"
<logic-op>   ::= "&&" | "||"
<special-op> ::= "::" | "->"
```

### 구분자

```bnf
<delimiter> ::= "(" | ")" | "[" | "]" | "," | "|" | "_"
```

### 공백과 주석

```bnf
<whitespace> ::= " " | "\t" | "\n" | "\r\n"

<line-comment>  ::= "//" { <any-char-except-newline> }
<block-comment> ::= "(*" { <any> | <block-comment> } "*)"
```

블록 주석은 중첩 가능.

## 연산자 우선순위

낮은 순위에서 높은 순위 순서:

| 순위 | 연산자 | 결합성 | 설명 |
|------|--------|--------|------|
| 1 | `let`, `if`, `match`, `fun` | - | 키워드 표현식 |
| 2 | `\|\|` | 좌 | 논리 OR |
| 3 | `&&` | 좌 | 논리 AND |
| 4 | `=` `<` `>` `<=` `>=` `<>` | 비결합 | 비교 |
| 5 | `::` | 우 | Cons |
| 6 | `+` `-` | 좌 | 덧셈, 뺄셈 |
| 7 | `*` `/` | 좌 | 곱셈, 나눗셈 |
| 8 | `-` (단항) | 우 | 단항 마이너스 |
| 9 | 함수 적용 | 좌 | `f x` |

## 예제

### 산술
```
2 + 3 * 4                             // 14
(2 + 3) * 4                           // 20
-5 + 3                                // -2
```

### 변수 바인딩
```
let x = 5 in x * 2                    // 10
let x = 1 in let y = 2 in x + y       // 3
```

### 조건문
```
if 5 > 3 then 10 else 20              // 10
if true && false then 1 else 0        // 0
```

### 함수
```
fun x -> x + 1                        // <function>
let f = fun x -> x * 2 in f 5         // 10
let add = fun x -> fun y -> x + y in add 3 4  // 7
```

### 재귀
```
let rec fact n = if n <= 1 then 1 else n * fact (n - 1) in fact 5  // 120
```

### 튜플
```
(1, 2)                                // (1, 2)
let (x, y) = (1, 2) in x + y          // 3
```

### 리스트
```
[]                                    // []
[1, 2, 3]                             // [1, 2, 3]
0 :: [1, 2]                           // [0, 1, 2]
```

### 패턴 매칭
```
match [1, 2, 3] with
| [] -> 0
| h :: t -> h                         // 1

match (1, 2) with
| (x, y) -> x + y                     // 3
```

### 표준 라이브러리
```
map (fun x -> x * 2) [1, 2, 3]        // [2, 4, 6]
filter (fun x -> x > 1) [1, 2, 3]     // [2, 3]
fold (fun a -> fun b -> a + b) 0 [1, 2, 3]  // 6
```

## 관련 파일

- `FunLang/Lexer.fsl` — fslex 렉서 명세
- `FunLang/Parser.fsy` — fsyacc 파서 명세
- `FunLang/Ast.fs` — AST 타입 정의
