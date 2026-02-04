# Appendix B: Comments

이 부록에서는 FunLang에 주석을 추가한다. 단일행 주석(`//`)과 중첩 가능한 블록 주석(`(* *)`)을 구현한다.

## 개요

주석은 코드의 가독성을 높이고 문서화에 필수적이다:
- **단일행 주석**: `// 이 줄은 무시됨`
- **블록 주석**: `(* 여러 줄 가능 *)`
- **중첩 주석**: `(* 외부 (* 내부 *) 외부 *)`

ML 계열 언어(OCaml, F#)의 전통을 따라 블록 주석 중첩을 지원한다.

## Lexer 구현

### 단일행 주석

`//`로 시작하여 줄 끝까지가 주석이다.

```fsharp
// FunLang/Lexer.fsl

rule tokenize = parse
    // ... 다른 규칙들 ...

    // Comments (MUST come before operators to match first)
    | "//" [^ '\n' '\r']* newline  { lexbuf.EndPos <- lexbuf.EndPos.NextLine
                                      tokenize lexbuf }   // 개행 포함
    | "//" [^ '\n' '\r']*          { tokenize lexbuf }   // EOF에서 끝나는 경우
```

두 가지 패턴이 필요하다:
1. 줄바꿈으로 끝나는 주석 - 위치 추적 업데이트 필요
2. 파일 끝에서 끝나는 주석 - 줄바꿈 없음

### 블록 주석

`(*`로 시작하고 `*)`로 끝난다. 중첩을 지원하려면 깊이(depth)를 추적해야 한다.

```fsharp
rule tokenize = parse
    // ... 다른 규칙들 ...
    | "(*"          { block_comment 1 lexbuf }   // 블록 주석 시작, depth=1

// 블록 주석 핸들러 - 중첩 지원
and block_comment depth = parse
    | "(*"    { block_comment (depth + 1) lexbuf }     // 중첩: depth 증가
    | "*)"    { if depth = 1 then tokenize lexbuf      // 닫기: depth=1이면 복귀
                else block_comment (depth - 1) lexbuf } // 아니면 depth 감소
    | newline { lexbuf.EndPos <- lexbuf.EndPos.NextLine
                block_comment depth lexbuf }           // 줄바꿈 처리
    | eof     { failwith "Unterminated comment" }      // EOF 에러
    | _       { block_comment depth lexbuf }           // 나머지 문자 스킵
```

## 중첩 주석 동작

```
(* depth=1
   (* depth=2
      코드를 임시로 주석 처리
   *) depth=1
   여전히 주석 내부
*) depth=0 → tokenize로 복귀
```

중첩 주석의 장점:
- 코드 블록을 임시로 주석 처리할 때 유용
- 주석 내부에 `*)` 문자열이 있어도 안전

## 테스트

### 단일행 주석

```bash
$ dotnet run --project FunLang -- --expr "1 + 2 // 덧셈"
3
```

### 블록 주석

```bash
$ dotnet run --project FunLang -- --expr "(* 곱셈 *) 3 * 4"
12
```

### 중첩 주석

```bash
$ dotnet run --project FunLang -- --expr "(* (* 중첩 *) *) 5"
5
```

### 주석만 있는 경우

```bash
$ dotnet run --project FunLang -- --expr "// 아무것도 없음"
# 파싱 에러 (표현식 없음)
```

## 주의사항

### 규칙 순서

`//`가 `/` 보다 먼저 매칭되어야 한다. fslex는 긴 패턴을 먼저 시도하지만, 명시적 순서가 안전하다.

```fsharp
// 올바른 순서
| "//"    { ... }  // 먼저
| "/"     { ... }  // 나중

// 잘못된 순서 - "/" 두 개로 인식될 수 있음
| "/"     { ... }
| "//"    { ... }
```

### EOF 처리

블록 주석이 닫히지 않으면 에러를 발생시킨다:

```fsharp
| eof     { failwith "Unterminated comment" }
```

## 요약

| 주석 유형 | 구문 | 특징 |
|----------|------|------|
| 단일행 | `// ...` | 줄 끝까지 |
| 블록 | `(* ... *)` | 여러 줄, 중첩 가능 |

**구현 포인트:**
- 단일행: 정규식으로 줄 끝까지 매칭
- 블록: 별도 핸들러 함수로 깊이 추적
- 줄바꿈 시 위치 정보 업데이트
