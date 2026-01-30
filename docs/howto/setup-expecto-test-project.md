---
created: 2026-01-30
description: F# 프로젝트에 Expecto 테스트 프로젝트 추가하기
---

# Expecto 테스트 프로젝트 설정

F# 프로젝트에 Expecto 기반 테스트 프로젝트를 추가하는 방법.

## The Insight

Expecto는 F#의 함수형 스타일에 맞는 테스트 프레임워크다. xUnit/NUnit과 달리 테스트가 일급 값(first-class value)이며, 속성(attribute) 대신 함수 조합으로 테스트를 구성한다.

## Why This Matters

- **F# 친화적**: `test`, `testList`, `testProperty` 등 함수형 조합
- **빠른 실행**: 병렬 실행 기본 지원
- **유연한 출력**: 컬러 출력, 다양한 포맷

## Recognition Pattern

- F# 프로젝트에 단위 테스트를 추가할 때
- 기존 Exe 프로젝트의 로직을 테스트하고 싶을 때
- xUnit/NUnit의 속성(attribute) 기반 스타일이 불편할 때

## The Approach

### Step 1: 테스트 프로젝트 생성

```bash
# 프로젝트 루트에서
dotnet new console -lang F# -n MyProject.Tests -f net10.0
```

### Step 2: Expecto 패키지 추가

```bash
cd MyProject.Tests
dotnet add package Expecto
```

### Step 3: 메인 프로젝트 참조

```bash
dotnet add reference ../MyProject/MyProject.fsproj
```

**주의**: Exe 프로젝트도 참조 가능하다. .NET은 Exe와 Library 구분 없이 어셈블리를 참조할 수 있다.

### Step 4: 테스트 코드 작성

`Program.fs`:

```fsharp
module MyProject.Tests

open Expecto

// 테스트 대상 모듈 import
open MyModule

[<Tests>]
let myTests =
    testList "My Feature" [
        test "simple case" {
            let result = myFunction 2 3
            Expect.equal result 5 "2 + 3 should be 5"
        }

        test "edge case" {
            let result = myFunction 0 0
            Expect.equal result 0 "0 + 0 should be 0"
        }
    ]

[<EntryPoint>]
let main argv =
    runTestsWithCLIArgs [] argv myTests
```

### Step 5: 테스트 실행

```bash
dotnet run --project MyProject.Tests
```

## Example

**프로젝트 구조:**
```
MyProject/
├── MyProject/
│   ├── MyProject.fsproj
│   ├── Ast.fs
│   ├── Eval.fs
│   └── Program.fs
└── MyProject.Tests/
    ├── MyProject.Tests.fsproj
    └── Program.fs
```

**MyProject.Tests.fsproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="Program.fs" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Expecto" Version="10.2.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\MyProject\MyProject.fsproj" />
  </ItemGroup>
</Project>
```

**테스트 코드:**
```fsharp
module MyProject.Tests

open Expecto
open Ast
open Eval

let evaluate input =
    // parse and eval
    input |> parse |> eval

[<Tests>]
let arithmeticTests =
    testList "Arithmetic" [
        testList "Basic Operations" [
            test "addition" {
                Expect.equal (evaluate "2 + 3") 5 "2 + 3 = 5"
            }
            test "multiplication" {
                Expect.equal (evaluate "3 * 4") 12 "3 * 4 = 12"
            }
        ]

        testList "Precedence" [
            test "mul before add" {
                Expect.equal (evaluate "2 + 3 * 4") 14 "2 + 3 * 4 = 14"
            }
        ]
    ]

[<EntryPoint>]
let main argv =
    runTestsWithCLIArgs [] argv arithmeticTests
```

**실행 결과:**
```
[13:25:00 INF] EXPECTO? Running tests...
[13:25:01 INF] EXPECTO! 4 tests run in 00:00:00.05 – 4 passed, 0 ignored, 0 failed, 0 errored. Success!
```

## Expecto 주요 함수

| 함수 | 용도 |
|------|------|
| `test "name" { ... }` | 단일 테스트 |
| `testList "name" [ ... ]` | 테스트 그룹 |
| `testCase "name" (fun () -> ...)` | 함수 스타일 테스트 |
| `testProperty "name" (fun x -> ...)` | 속성 기반 테스트 (FsCheck) |
| `Expect.equal actual expected msg` | 동등성 검증 |
| `Expect.isTrue condition msg` | 불린 검증 |
| `Expect.throws<exn> (fun () -> ...) msg` | 예외 검증 |

## 체크리스트

- [ ] 테스트 프로젝트가 콘솔 앱으로 생성되었는가?
- [ ] Expecto 패키지가 추가되었는가?
- [ ] 메인 프로젝트가 참조되었는가?
- [ ] `[<Tests>]` 속성이 테스트에 붙어있는가?
- [ ] `[<EntryPoint>]`에서 `runTestsWithCLIArgs`를 호출하는가?

## 관련 문서

- [Expecto GitHub](https://github.com/haf/expecto) - 공식 문서
- [Expecto + FsCheck](https://github.com/haf/expecto#property-based-tests) - 속성 기반 테스트
