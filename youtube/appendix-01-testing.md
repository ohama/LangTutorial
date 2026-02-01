# 부록 A: Testing - 테스트 전략 완벽 가이드

## 영상 정보
- **예상 길이**: 18-20분
- **난이도**: 초급-중급
- **필요 사전 지식**: F# 기초, 커맨드라인 사용법

## 인트로 (0:00)

여러분 안녕하세요!

언어를 만들 때 가장 무서운 순간이 뭔지 아세요? 바로 "어? 이거 왜 갑자기 안 되지?" 하는 순간입니다. 어제까지 잘 되던 덧셈이 오늘 갑자기 곱셈처럼 동작한다면? 새 기능을 추가했는데 기존 기능이 망가진다면?

[화면: 패닉에 빠진 개발자 이미지 또는 빨간색 에러 메시지들]

오늘은 이런 악몽을 방지하는 완벽한 테스트 전략을 알려드리겠습니다. FunLang 프로젝트에서 실제로 사용하는 세 가지 무기를 소개합니다.

[화면: fslit, Expecto, FsCheck 로고/타이틀 순차 표시]

이 세 도구를 제대로 활용하면, 자신감 있게 코드를 리팩토링하고, 새 기능을 추가하고, 밤에 편하게 잘 수 있습니다.

## 본문

### 섹션 1: 테스트 피라미드 - 왜 세 가지 도구인가? (1:30)

먼저 큰 그림부터 보겠습니다.

[화면: 테스트 피라미드 다이어그램]

```
        ┌─────────┐
        │  fslit  │  CLI 통합 테스트 (느림, 넓음)
        └────┬────┘
        ┌────┴────┐
        │ Expecto │  단위 테스트 (빠름)
        └────┬────┘
   ┌─────────┴─────────┐
   │     FsCheck       │  속성 기반 테스트 (자동 생성)
   └───────────────────┘
```

이 피라미드가 중요한 이유는 각 레벨이 다른 목적을 가지고 있기 때문입니다.

**맨 위, fslit**은 사용자 관점에서 테스트합니다. "커맨드라인에서 실행했을 때 정말 제대로 동작하나?" 느리지만 가장 현실적입니다.

**중간, Expecto**는 내부 로직을 테스트합니다. "이 함수가 제대로 된 값을 리턴하나?" 빠르고 정확합니다.

**맨 아래, FsCheck**는 수학적 성질을 검증합니다. "덧셈 교환법칙이 모든 경우에 성립하나?" 개발자가 놓칠 수 있는 엣지 케이스를 자동으로 찾아줍니다.

[화면: 표 - 도구별 특징 비교]

| 도구 | 목적 | 언제 사용 |
|------|------|-----------|
| **fslit** | CLI 통합 테스트 | 회귀 방지, E2E 검증 |
| **Expecto** | 단위 테스트 | 모듈별 로직 검증 |
| **FsCheck** | 속성 기반 테스트 | 수학적 불변식 검증 |

세 도구가 함께 작동할 때 최강의 안전망이 됩니다.

### 섹션 2: fslit - 파일 기반 CLI 테스트의 마법 (4:00)

첫 번째 도구, fslit부터 시작하겠습니다.

[화면: fslit GitHub 페이지 또는 로고]

fslit의 철학은 아주 간단합니다. "커맨드를 실행하고, 출력을 파일에 저장한다. 다음에 또 실행해서 같은 출력이 나오면 성공."

설치는 한 줄이면 됩니다.

[화면: 터미널]

```bash
dotnet tool install -g fslit
```

자, 이제 실제 테스트 파일을 볼까요?

[화면: .flt 파일 예시]

```flt
// Test: Simple addition
// --- Command: dotnet run --project FunLang -- --expr "2 + 3"
// --- Output:
5
```

정말 직관적이죠? 주석처럼 보이지만 이게 전부입니다.

- `Command:` 뒤에 실행할 커맨드
- `Output:` 뒤에 기대하는 출력

fslit이 이 파일을 읽고, 커맨드를 실행하고, 출력이 일치하는지 확인합니다.

[화면: 프로젝트 구조 트리]

실제 프로젝트에서는 이렇게 구성합니다.

```
tests/
├── Makefile
├── cli/              # 기본 평가 테스트
│   ├── 01-simple-add.flt
│   └── 02-precedence.flt
├── emit-tokens/      # --emit-tokens 테스트
├── emit-ast/         # --emit-ast 테스트
└── file-input/       # 파일 입력 테스트
```

기능별로 디렉토리를 나누고, 테스트는 `01-`, `02-` 같은 번호로 순서를 명시합니다.

[화면: 파일 입력 예시]

더 복잡한 테스트도 가능합니다. `%input`을 사용하면 임시 파일을 만들어줍니다.

```flt
// Test: Evaluate from file
// --- Command: dotnet run --project FunLang -- %input
// --- Input:
(2 + 3) * 4
// --- Output:
20
```

이제 여러 줄 프로그램도 테스트할 수 있습니다!

[화면: 터미널에서 테스트 실행]

실행은 Makefile로 관리합니다.

```bash
make -C tests           # 전체 테스트
make -C tests cli       # cli 디렉토리만
fslit tests/cli/01-simple-add.flt  # 단일 파일
```

[화면: 초록색 PASS 메시지들]

모두 통과하면 이렇게 기분 좋은 초록색을 보게 됩니다.

**핵심 규칙 세 가지:**

1. **한 파일 = 한 테스트** (fslit의 제약사항)
2. **출력은 정확히 일치해야 함** (공백 하나, 줄바꿈 하나도 틀리면 실패)
3. **번호 접두사로 순서 명시** (테스트가 논리적 순서대로 실행됨)

fslit의 장점은 회귀 방지입니다. 새 기능을 추가한 후 `make -C tests`만 실행하면, 기존 기능이 망가졌는지 즉시 알 수 있습니다.

### 섹션 3: Expecto - 단위 테스트의 정석 (8:30)

두 번째 도구, Expecto입니다.

[화면: Expecto 로고 또는 문서]

fslit이 외부에서 테스트한다면, Expecto는 내부를 들여다봅니다. Lexer, Parser, Eval 같은 개별 모듈을 테스트하는 거죠.

먼저 프로젝트를 만듭니다.

[화면: 터미널]

```bash
dotnet new console -lang F# -n FunLang.Tests -f net10.0
cd FunLang.Tests
dotnet add package Expecto
dotnet add reference ../FunLang/FunLang.fsproj
```

이제 테스트 코드를 볼까요?

[화면: 코드 에디터 - Expecto 테스트]

```fsharp
module FunLang.Tests

open Expecto
open FSharp.Text.Lexing

[<Tests>]
let lexerTests =
    testList "Lexer" [
        test "tokenizes number" {
            let lexbuf = LexBuffer<char>.FromString "42"
            let token = Lexer.tokenize lexbuf
            Expect.equal token (Parser.NUMBER 42) "should be NUMBER(42)"
        }
    ]
```

F#을 아시는 분들은 바로 이해하실 겁니다. `test` 안에 테스트 로직을 쓰고, `Expect.equal`로 결과를 확인합니다.

[화면: 세 가지 테스트 카테고리 강조]

실제로는 세 레이어를 모두 테스트합니다.

**1. Lexer 테스트** - "42"라는 문자열이 NUMBER(42) 토큰으로 변환되는가?

**2. Parser 테스트** - "2 + 3"이 `Add(Number 2, Number 3)` AST로 파싱되는가?

**3. Eval 테스트** - `Add(Number 2, Number 3)`이 5로 평가되는가?

[화면: 터미널 - 테스트 실행]

```bash
dotnet run --project FunLang.Tests
```

[화면: 테스트 결과]

```
[EXPECTO] 12 tests run - 12 passed, 0 failed
```

필터링도 가능합니다.

```bash
dotnet run --project FunLang.Tests -- --filter "Lexer"
```

이러면 Lexer 관련 테스트만 실행됩니다. TDD(Test-Driven Development) 할 때 유용하죠.

[화면: Expect 함수 표]

Expecto의 주요 함수들입니다.

| 함수 | 용도 |
|------|------|
| `Expect.equal actual expected msg` | 값 동등성 비교 |
| `Expect.isTrue condition msg` | 불린 조건 검증 |
| `Expect.throws<ExnType> (fun () -> ...) msg` | 예외 발생 확인 |
| `Expect.isSome option msg` | Option이 Some인지 확인 |

특히 `Expect.throws`는 에러 처리를 테스트할 때 필수입니다. "0으로 나누면 예외가 발생해야 한다" 같은 거죠.

Expecto의 진짜 힘은 **빠른 피드백**입니다. fslit은 전체 프로그램을 실행하지만, Expecto는 함수 하나만 테스트하니까 속도가 월등히 빠릅니다. 개발 중에 계속 돌려도 부담이 없어요.

### 섹션 4: FsCheck - 속성 기반 테스트의 마법 (12:30)

마지막 무기, FsCheck입니다.

[화면: FsCheck 로고]

이건 좀 특별합니다. 우리가 "42를 넣으면 42가 나온다"라고 테스트하는 게 아니라, "**모든** 숫자를 넣으면 그대로 나온다"를 테스트합니다.

설치부터 하죠.

[화면: 터미널]

```bash
dotnet add package FsCheck
dotnet add package Expecto.FsCheck
```

이제 코드를 볼까요?

[화면: 코드 에디터 - FsCheck 예시]

```fsharp
open Expecto
open FsCheck

[<Tests>]
let propertyTests =
    testList "Properties" [
        testProperty "number evaluates to itself" <| fun (n: int) ->
            Eval.eval (Ast.Number n) = n
    ]
```

여기서 `fun (n: int) ->`가 핵심입니다. FsCheck가 **랜덤한 int 값**을 자동으로 생성해서 넣어줍니다. 100번, 1000번, 원하는 만큼요.

[화면: 더 많은 속성 테스트 예시]

더 강력한 예시들을 볼까요?

```fsharp
// 덧셈 교환법칙
testProperty "addition is commutative" <| fun (a: int) (b: int) ->
    let left = Eval.eval (Ast.Add(Ast.Number a, Ast.Number b))
    let right = Eval.eval (Ast.Add(Ast.Number b, Ast.Number a))
    left = right

// 0은 덧셈의 항등원
testProperty "zero is additive identity" <| fun (n: int) ->
    Eval.eval (Ast.Add(Ast.Number n, Ast.Number 0)) = n

// 이중 부정은 원래 값
testProperty "double negation is identity" <| fun (n: int) ->
    Eval.eval (Ast.Negate(Ast.Negate(Ast.Number n))) = n
```

이게 바로 **속성 기반 테스트**입니다. 수학적 성질을 선언하면, FsCheck가 알아서 수천 개의 테스트 케이스를 만들어 검증합니다.

[화면: 조건부 속성 예시]

특정 조건이 필요한 경우도 있죠. 0으로 나누기는 안 되니까요.

```fsharp
testProperty "division by non-zero" <| fun (a: int) (b: int) ->
    b <> 0 ==> lazy (
        let result = Eval.eval (Ast.Divide(Ast.Number a, Ast.Number b))
        result = a / b
    )
```

`b <> 0 ==>`는 "b가 0이 아닐 때만 테스트하라"는 뜻입니다.

[화면: FsCheck 실패 메시지 예시]

FsCheck의 진짜 마법은 **실패 시**에 나타납니다.

```
Falsifiable, after 42 tests (3 shrinks):
Original: (1073741824, 1073741824)
Shrunk: (1, 2147483647)
```

테스트가 실패하면, FsCheck는 실패를 일으키는 **가장 작은 입력값**을 찾아줍니다. 처음에는 엄청 큰 숫자로 실패했지만, "축소(shrink)"를 통해 `(1, 2147483647)`까지 줄여주는 거죠. 디버깅이 훨씬 쉬워집니다.

**FsCheck를 써야 하는 이유:**

우리가 직접 테스트 케이스를 만들면, 보통 0, 1, 100 같은 "착한 값"만 넣잖아요? 하지만 실제 버그는 `2147483647` 같은 극단적인 값에서 터집니다. FsCheck는 그런 값들을 자동으로 시도합니다.

### 섹션 5: 실전 워크플로우 - 세 도구를 함께 사용하기 (15:30)

이제 세 도구를 실제로 어떻게 조합하는지 보겠습니다.

[화면: 워크플로우 다이어그램]

**시나리오: let 바인딩 기능 추가**

**1단계: Expecto로 TDD**

[화면: 코드 에디터]

먼저 실패하는 단위 테스트를 작성합니다.

```fsharp
test "parses let binding" {
    let lexbuf = LexBuffer<char>.FromString "let x = 5 in x"
    let ast = Parser.start Lexer.tokenize lexbuf
    // 이 시점에서는 실패할 것
    Expect.equal ast (Ast.Let("x", Ast.Number 5, Ast.Var "x")) ""
}
```

[화면: 빨간색 실패 메시지]

당연히 실패합니다. 아직 구현 안 했으니까요.

**2단계: 구현**

[화면: Lexer.fsl, Parser.fsy 편집 화면]

이제 Lexer에 LET, IN 토큰 추가하고, Parser에 let 문법 추가합니다. (세부 코드는 생략)

**3단계: Expecto 다시 실행**

[화면: 초록색 통과 메시지]

```bash
dotnet run --project FunLang.Tests -- --filter "let"
```

이제 통과합니다!

**4단계: FsCheck로 속성 검증**

[화면: 속성 테스트 추가]

```fsharp
testProperty "let binding evaluates correctly" <| fun (n: int) ->
    let expr = sprintf "let x = %d in x + 1" n
    let lexbuf = LexBuffer<char>.FromString expr
    let ast = Parser.start Lexer.tokenize lexbuf
    Eval.eval ast = n + 1
```

모든 정수에 대해 let이 제대로 동작하는지 검증합니다.

**5단계: fslit으로 회귀 테스트**

[화면: .flt 파일 생성]

```bash
cat > tests/variables/01-let-simple.flt << 'EOF'
// Test: Simple let binding
// --- Command: dotnet run --project FunLang -- --expr "let x = 5 in x + 1"
// --- Output:
6
EOF
```

이제 이 테스트가 영원히 남아서 회귀를 방지합니다.

**6단계: 커밋 전 전체 검증**

[화면: 터미널 - 모든 테스트 실행]

```bash
make -C tests check              # fslit (21 tests)
dotnet run --project FunLang.Tests  # Expecto + FsCheck (129 tests)
```

[화면: 모두 초록색 통과]

모두 통과하면 자신 있게 커밋합니다!

### 섹션 6: 테스트 실패 디버깅 팁 (17:30)

마지막으로, 테스트가 실패했을 때 디버깅하는 방법입니다.

[화면: 빨간색 실패 메시지]

fslit 테스트가 실패했다고 가정해봅시다.

**1단계: 토큰 확인**

[화면: 터미널]

```bash
dotnet run --project FunLang -- --emit-tokens --expr "2 + 3"
```

[화면: 출력]

```
NUMBER(2) PLUS NUMBER(3) EOF
```

토큰이 제대로 생성되는지 확인합니다.

**2단계: AST 확인**

```bash
dotnet run --project FunLang -- --emit-ast --expr "2 + 3"
```

[화면: 출력]

```
Add (Number 2, Number 3)
```

AST가 올바른지 확인합니다.

**3단계: Expecto로 구체적 케이스 추가**

문제를 발견했으면 Expecto에 테스트 케이스를 추가합니다. 그래야 다시 같은 버그가 생기지 않습니다.

[화면: 도구별 용도 요약 표]

이런 식으로 세 도구를 레이어별로 사용하면 디버깅이 훨씬 쉬워집니다.

## 아웃트로 (18:30)

자, 정리하겠습니다.

[화면: 세 도구 로고 나란히]

**fslit** - 사용자 관점의 통합 테스트. "실제로 동작하나?"

**Expecto** - 내부 로직의 단위 테스트. "이 함수가 정확한가?"

**FsCheck** - 수학적 속성 검증. "모든 경우에 성립하나?"

[화면: 테스트 피라미드 다시 표시]

이 세 도구를 함께 사용하면 **자신감 있는 리팩토링**, **빠른 버그 발견**, **안전한 배포**가 가능합니다.

[화면: 실제 프로젝트 통계]

실제로 FunLang 프로젝트는 현재 fslit 66개, Expecto 129개 테스트로 보호받고 있습니다. 덕분에 Phase 7까지 안전하게 진행할 수 있었죠.

[화면: 관련 문서 링크]

더 자세한 내용은 튜토리얼 저장소의 `tutorial/appendix-01-testing.md`와 `docs/howto/` 디렉토리를 참고하세요. 특히:

- `write-fslit-file-tests.md` - fslit 상세 가이드
- `setup-expecto-test-project.md` - Expecto 프로젝트 설정
- `write-fscheck-property-tests.md` - FsCheck 속성 테스트
- `testing-strategies.md` - 전체 테스트 전략

[화면: 구독 및 좋아요 요청 애니메이션]

언어 구현이나 테스트 전략에 대해 궁금한 점이 있으면 댓글로 남겨주세요. 다음 영상에서는 실제로 변수와 클로저를 구현하는 과정을 다룰 예정입니다.

구독과 좋아요는 큰 힘이 됩니다. 그럼 다음 영상에서 만나요!

## 핵심 키워드

#F# #FSharp #테스트 #TDD #속성기반테스트 #fslit #Expecto #FsCheck #언어구현 #컴파일러 #인터프리터 #단위테스트 #통합테스트 #소프트웨어품질 #프로그래밍언어 #튜토리얼
