# FunLang: F#로 만드는 프로그래밍 언어

F#과 fslex/fsyacc를 사용하여 함수형 프로그래밍 언어 인터프리터를 구축하는 튜토리얼입니다.

## 목표

- **Lexer/Parser**: fslex와 fsyacc로 토큰화 및 구문 분석
- **AST**: 추상 구문 트리 설계
- **Interpreter**: 트리-워킹 인터프리터 구현
- **Type System**: Hindley-Milner 타입 추론

## FunLang 특징

```
// 변수 바인딩
let x = 42

// 함수 정의
let add = fun x y -> x + y

// 재귀 함수
let rec factorial = fun n ->
  if n <= 1 then 1
  else n * factorial (n - 1)

// 패턴 매칭
let rec length = fun lst ->
  match lst with
  | [] -> 0
  | _ :: tail -> 1 + length tail

// 타입 추론
let compose = fun f g x -> f (g x)
```

## 시작하기

[Chapter 1: 프로젝트 기초](chapter-01-foundation.md)부터 시작하세요.
