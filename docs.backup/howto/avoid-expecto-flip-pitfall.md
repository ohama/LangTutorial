---
created: 2026-01-30
description: Expecto.Flip이 Expect 함수 인자 순서를 뒤집는 문제와 해결법
---

# Expecto.Flip 인자 순서 함정 피하기

`open Expecto.Flip`은 `Expect` 함수들의 인자 순서를 뒤집어 대량 타입 에러를 유발한다.

## The Insight

Expecto의 `Flip` 모듈은 파이프라인 친화적 API를 위해 **모든 Expect 함수의 인자 순서를 뒤집는다**. 이름에서 유추하기 어렵고, import만으로 기존 코드가 깨진다.

```fsharp
// 표준 Expecto
Expect.equal actual expected "message"  // (actual, expected, msg)

// Expecto.Flip
Expect.equal "message" expected actual  // (msg, expected, actual)
```

## Why This Matters

`open Expecto.Flip`을 추가하면:
- 기존 테스트 코드가 **컴파일 에러 없이 의미가 바뀔 수 있음**
- 타입이 다르면 100+ FS0001 에러 폭발: `expected type 'string' but here has type 'int'`
- 타입이 같으면 **컴파일은 되지만 테스트 의미가 반대로** (actual과 expected 뒤바뀜)

## Recognition Pattern

다음 상황에서 이 문제를 의심한다:

1. **Expecto 테스트에서 대량 타입 에러** — 특히 `Expect.equal`의 첫 인자가 string을 기대한다는 에러
2. **최근 Expecto 관련 import 변경** — 새 모듈 open 후 갑자기 에러
3. **테스트가 통과하는데 실패해야 할 것 같음** — actual/expected 뒤바뀜 가능성

## The Approach

### Step 1: 증상 확인

```
error FS0001: This expression was expected to have type 'string'
              but here has type 'int'
```

이 에러가 `Expect.equal` 첫 번째 인자에서 발생하면 Flip 문제다.

### Step 2: import 확인

```fsharp
open Expecto
open Expecto.Flip  // ← 이 라인이 문제
```

### Step 3: 해결

```fsharp
open Expecto
// open Expecto.Flip  ← 제거
```

## Example

```fsharp
// ❌ BAD: Flip이 열려있으면 인자 순서가 뒤집힘
open Expecto
open Expecto.Flip

test "addition" {
    Expect.equal (2 + 3) 5 "should be 5"
    // 컴파일 에러: "should be 5"가 actual 위치에 오게 됨
}

// ✅ GOOD: 표준 Expecto 사용
open Expecto

test "addition" {
    Expect.equal (2 + 3) 5 "should be 5"  // (actual, expected, msg)
}
```

## 체크리스트

- [ ] `open Expecto.Flip`이 코드에 있는지 확인
- [ ] 파이프라인 스타일이 필요하면 Flip 사용, 아니면 제거
- [ ] Flip 사용 시 모든 Expect 호출 인자 순서 변경

## 관련 문서

- `setup-expecto-test-project.md` - Expecto 프로젝트 설정
- `write-fscheck-property-tests.md` - FsCheck 속성 테스트
