---
created: 2026-01-30
description: FsLexYacc에서 Parser before Lexer 빌드 순서 설정
---

# FsLexYacc 빌드 순서 설정

Lexer가 Parser 토큰을 참조하므로 Parser.fsy를 먼저 빌드해야 한다.

## The Insight

**런타임 순서 ≠ 빌드 순서.**

런타임에는 Lexer → Parser 순서로 실행되지만, 빌드 시에는 Parser → Lexer 순서로 처리해야 한다. Lexer.fsl이 `open Parser`로 토큰 타입(NUMBER, EOF 등)을 참조하기 때문.

## Why This Matters

빌드 순서가 틀리면:

```
error FS0039: The namespace or module 'Parser' is not defined
```

또는:

```
error FS0039: The value or constructor 'NUMBER' is not defined
```

이 에러가 나면 대부분 .fsproj에서 FsLex가 FsYacc보다 먼저 정의되어 있다.

## Recognition Pattern

- FsLexYacc 프로젝트 설정 시
- "Parser is not defined" 에러 발생 시
- Lexer.fsl에서 토큰 타입 접근 실패 시

## The Approach

.fsproj 파일에서 **FsYacc가 FsLex보다 위에** 있어야 한다.

### Step 1: .fsproj 구조 확인

```xml
<ItemGroup>
  <!-- 1. 수동 작성 파일 -->
  <Compile Include="Ast.fs" />

  <!-- 2. Parser 먼저 (토큰 타입 생성) -->
  <FsYacc Include="Parser.fsy">
    <OtherFlags>--module Parser</OtherFlags>
  </FsYacc>

  <!-- 3. Lexer 다음 (Parser 토큰 참조) -->
  <FsLex Include="Lexer.fsl">
    <OtherFlags>--module Lexer --unicode</OtherFlags>
  </FsLex>

  <!-- 4. 생성된 파일 참조 -->
  <Compile Include="Parser.fsi" />
  <Compile Include="Parser.fs" />
  <Compile Include="Lexer.fs" />

  <!-- 5. 메인 프로그램 -->
  <Compile Include="Program.fs" />
</ItemGroup>
```

### Step 2: Lexer.fsl에서 Parser 참조

```fsharp
{
open Parser  // Parser 토큰 타입 사용
}

rule tokenize = parse
  | digit+  { NUMBER (Int32.Parse(lexeme lexbuf)) }  // Parser.NUMBER
  | eof     { EOF }                                   // Parser.EOF
```

### Step 3: 빌드 테스트

```bash
dotnet clean && dotnet build
```

에러 없이 빌드되면 순서가 올바른 것.

## Example

```xml
<!-- ❌ BAD: Lexer가 Parser보다 먼저 -->
<ItemGroup>
  <FsLex Include="Lexer.fsl" />   <!-- Parser 없이 컴파일 시도 -->
  <FsYacc Include="Parser.fsy" /> <!-- 너무 늦음 -->
</ItemGroup>

<!-- ✅ GOOD: Parser가 Lexer보다 먼저 -->
<ItemGroup>
  <FsYacc Include="Parser.fsy" /> <!-- 토큰 타입 먼저 생성 -->
  <FsLex Include="Lexer.fsl" />   <!-- 이제 Parser 참조 가능 -->
</ItemGroup>
```

## 체크리스트

- [ ] .fsproj에서 FsYacc가 FsLex보다 위에 있는가?
- [ ] Lexer.fsl에 `open Parser` 있는가?
- [ ] `dotnet clean && dotnet build` 성공하는가?

## 관련 문서

- [FsLexYacc 공식 문서](https://fsprojects.github.io/FsLexYacc/)
- [JSON Parser Example](https://fsprojects.github.io/FsLexYacc/content/jsonParserExample.html)
