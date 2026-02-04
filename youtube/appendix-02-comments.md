# 부록 B: Comments - 주석 구현하기

## 영상 정보
- **예상 길이**: 10-12분
- **난이도**: 초급
- **필요 사전 지식**: EP01 시청 (Lexer 기초)

## 인트로 (0:00)

여러분 안녕하세요! 오늘은 짧지만 중요한 기능을 추가합니다.

[화면: 주석이 있는 코드 예시]

```funlang
// 이건 주석이에요
(* 이것도 주석이에요 *)
1 + 2  // 덧셈
```

바로 **주석**입니다! 주석 없는 프로그래밍 언어는 없죠?

오늘은 단일행 주석 `//`과 ML 스타일 블록 주석 `(* *)`을 구현합니다. 특히 블록 주석은 **중첩**을 지원해요!

Let's go!

## 본문

### 섹션 1: 주석의 종류 (1:00)

FunLang에서 지원할 주석은 두 종류입니다.

[화면: 두 가지 주석 스타일]

**1. 단일행 주석 (C 스타일)**
```funlang
// 줄 끝까지 주석
1 + 2 // 여기부터 주석
```

**2. 블록 주석 (ML 스타일)**
```funlang
(* 여러 줄
   주석 가능 *)
3 * (* 인라인도 OK *) 4
```

[화면: 중첩 주석 강조]

ML 계열 언어(OCaml, F#)의 전통을 따라 **중첩 주석**도 지원합니다.

```funlang
(* 외부
   (* 내부 주석 *)
   외부 계속 *)
```

이게 왜 유용하냐면, 코드 블록을 임시로 주석 처리할 때 편해요!

### 섹션 2: 단일행 주석 구현 (2:30)

Lexer에서 `//`를 만나면 줄 끝까지 무시하면 됩니다.

[화면: Lexer.fsl 파일]

```fsharp
// FunLang/Lexer.fsl

rule tokenize = parse
    // ... 다른 규칙들 ...

    // 단일행 주석 (개행으로 끝나는 경우)
    | "//" [^ '\n' '\r']* newline
        { lexbuf.EndPos <- lexbuf.EndPos.NextLine
          tokenize lexbuf }

    // 단일행 주석 (EOF로 끝나는 경우)
    | "//" [^ '\n' '\r']*
        { tokenize lexbuf }
```

[화면: 정규식 설명]

`[^ '\n' '\r']*` - 줄바꿈이 아닌 모든 문자를 0개 이상 매칭

**두 가지 패턴이 필요한 이유:**
1. 줄바꿈으로 끝나는 주석 - 위치 추적 업데이트 필요
2. 파일 끝에서 끝나는 주석 - 줄바꿈 없음

[화면: 위치 추적 업데이트]

```fsharp
lexbuf.EndPos <- lexbuf.EndPos.NextLine
```

줄바꿈을 만나면 위치 정보를 업데이트해야 에러 메시지가 정확해요!

### 섹션 3: 블록 주석 구현 (4:30)

블록 주석은 좀 더 복잡합니다. **중첩**을 지원해야 하거든요.

[화면: 중첩 깊이 시각화]

```
(* depth=1
   (* depth=2
      주석 내용
   *) depth=1
   주석 계속
*) depth=0 → 끝!
```

깊이(depth)를 추적해야 합니다.

[화면: block_comment 핸들러]

```fsharp
rule tokenize = parse
    // ...
    | "(*"    { block_comment 1 lexbuf }  // depth=1로 시작

// 블록 주석 핸들러 - 재귀적으로 호출
and block_comment depth = parse
    | "(*"    { block_comment (depth + 1) lexbuf }  // 중첩: depth 증가
    | "*)"    { if depth = 1 then tokenize lexbuf   // 마지막 닫기
                else block_comment (depth - 1) lexbuf }
    | newline { lexbuf.EndPos <- lexbuf.EndPos.NextLine
                block_comment depth lexbuf }        // 줄바꿈 처리
    | eof     { failwith "Unterminated comment" }   // EOF 에러
    | _       { block_comment depth lexbuf }        // 나머지 문자 스킵
```

[화면: 동작 흐름 애니메이션]

```
"(* a (* b *) c *)"
tokenize → "(*" 발견 → block_comment(1)
   → "a" 스킵
   → "(*" 발견 → block_comment(2)
      → "b" 스킵
      → "*)" → depth=2, block_comment(1)로
   → "c" 스킵
   → "*)" → depth=1, tokenize로 복귀!
```

**핵심**: `depth`가 인자로 전달되면서 재귀적으로 중첩을 추적합니다.

### 섹션 4: 규칙 순서 주의 (7:00)

fslex에서 규칙 순서가 중요합니다!

[화면: 규칙 충돌 예시]

```fsharp
// 잘못된 순서 - "/"가 먼저 매칭될 수 있음
| "/"     { SLASH }
| "//"    { ... }

// 올바른 순서 - 긴 패턴 먼저
| "//"    { ... }
| "/"     { SLASH }
```

fslex는 기본적으로 longest match를 시도하지만, 명시적으로 `//`를 먼저 두는 게 안전해요.

[화면: `(*` vs `(` 충돌]

마찬가지로:

```fsharp
// 올바른 순서
| "(*"    { block_comment 1 lexbuf }  // 먼저
| "("     { LPAREN }                   // 나중
```

`(*`를 `(` 다음에 두면 `(`만 매칭될 수 있습니다!

### 섹션 5: 에러 처리 (8:30)

블록 주석이 닫히지 않으면 에러를 발생시켜야 해요.

[화면: EOF 에러 케이스]

```fsharp
| eof { failwith "Unterminated comment" }
```

```funlang
(* 닫히지 않은 주석
여기서 파일 끝
```

→ "Unterminated comment" 에러!

[화면: 향후 개선 가능한 에러 메시지]

더 나은 에러 메시지:
```
error: Unclosed comment starting at <expr>:1:0
```

위치 정보를 추가하면 더 유용하겠죠? (향후 개선 포인트!)

### 섹션 6: 테스트 (9:30)

테스트해봅시다!

[화면: 터미널 데모]

```bash
# 단일행 주석
$ dotnet run --project FunLang -- --expr "1 + 2 // 덧셈"
3

# 블록 주석
$ dotnet run --project FunLang -- --expr "(* 곱셈 *) 3 * 4"
12

# 중첩 주석
$ dotnet run --project FunLang -- --expr "(* (* 중첩 *) *) 5"
5

# 인라인 블록 주석
$ dotnet run --project FunLang -- --expr "1 (* + 2 *) + 3"
4
```

[화면: 에러 테스트]

```bash
# 닫히지 않은 주석
$ dotnet run --project FunLang -- --expr "(* 미완성"
Error: Unterminated comment
```

## 아웃트로 (10:30)

[화면: 요약 표]

| 주석 유형 | 구문 | 특징 |
|----------|------|------|
| 단일행 | `// ...` | 줄 끝까지 |
| 블록 | `(* ... *)` | 여러 줄, 중첩 가능 |

[화면: 핵심 구현 포인트]

**구현 포인트:**
- 단일행: 정규식으로 줄 끝까지 매칭
- 블록: 별도 핸들러 함수로 depth 추적
- 줄바꿈 시 위치 정보 업데이트

주석은 간단해 보이지만, 중첩 지원과 에러 처리를 제대로 하면 꽤 복잡해요.

[화면: 다음 부록 예고]

다음 부록에서는 **문자열**을 구현합니다. 이스케이프 시퀀스도 지원해요!

질문이나 제안은 댓글로 남겨주세요. 좋아요와 구독 잊지 마시고, 다음 영상에서 만나요!

## 핵심 키워드

- Comments
- 주석
- 단일행 주석
- 블록 주석
- 중첩 주석
- Lexer
- fslex
- depth tracking
- ML 스타일
- FunLang
- 언어 구현
