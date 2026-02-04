---
created: 2026-01-30
description: fslit으로 CLI 파일 기반 테스트 작성 - LLVM lit 스타일 테스트
---

# fslit 파일 기반 테스트

CLI 도구의 입출력을 파일로 검증하는 방법.

## The Insight

fslit은 LLVM lit에서 영감을 받은 테스트 도구다. 테스트 케이스가 **실행 가능한 문서**가 된다. 명령어, 입력, 기대 출력을 하나의 `.flt` 파일에 선언하면, fslit이 실행하고 비교한다.

```
.flt 파일 = 명령어 + 입력 + 기대 출력
fslit = 실행 + 비교 + 결과 리포트
```

**핵심 제약: 한 파일 = 한 테스트**

## Why This Matters

- **회귀 테스트**: CLI 동작이 변경되면 즉시 감지
- **문서화**: 테스트 파일 자체가 사용 예제
- **CI 통합**: `fslit tests/` 한 줄로 전체 검증

## Recognition Pattern

- CLI 도구를 개발할 때
- "이 입력에 이 출력"을 보장해야 할 때
- 컴파일러, 인터프리터, 변환 도구 테스트

## The Approach

### Step 1: fslit 설치

```bash
# .NET 글로벌 도구로 설치
dotnet tool install -g fslit

# 또는 로컬 설치
dotnet tool install fslit
```

### Step 2: 테스트 디렉토리 구조

```
tests/
├── Makefile          # 테스트 실행 스크립트
├── cli/              # 카테고리별 디렉토리
│   ├── 01-simple.flt
│   └── 02-complex.flt
└── errors/
    └── 01-syntax.flt
```

### Step 3: .flt 파일 작성

**기본 형식:**

```flt
// 테스트 설명 (주석)
// --- Command: <실행할 명령어>
// --- Output:
<기대 출력>
```

**파일 입력이 필요한 경우:**

```flt
// 파일에서 프로그램 읽기
// --- Command: dotnet run --project MyApp -- %input
// --- Input:
2 + 3
// --- Output:
5
```

### Step 4: 변수 사용

| 변수 | 설명 |
|------|------|
| `%input` | Input 섹션을 임시 파일로 생성, 그 경로로 대체 |
| `%s` | 현재 테스트 파일 경로 |
| `%S` | 현재 테스트 파일 디렉토리 |

### Step 5: Makefile 작성

```makefile
# tests/Makefile
.PHONY: all test

all: test

test:
	@cd .. && fslit tests/

# 카테고리별 실행
cli:
	@cd .. && fslit tests/cli/

errors:
	@cd .. && fslit tests/errors/

# 빌드 후 테스트
check: build test

build:
	@cd .. && dotnet build
```

## Example

**tests/cli/01-addition.flt:**
```flt
// Test: Simple addition
// --- Command: dotnet run --project FunLang -- --expr "2 + 3"
// --- Output:
5
```

**tests/cli/02-precedence.flt:**
```flt
// Test: Operator precedence
// --- Command: dotnet run --project FunLang -- --expr "2 + 3 * 4"
// --- Output:
14
```

**tests/file-input/01-from-file.flt:**
```flt
// Test: Execute from file
// --- Command: dotnet run --project FunLang -- %input
// --- Input:
(2 + 3) * 4
// --- Output:
20
```

**tests/emit/01-tokens.flt:**
```flt
// Test: Token emission
// --- Command: dotnet run --project FunLang -- --emit-tokens --expr "2 + 3"
// --- Output:
NUMBER(2) PLUS NUMBER(3) EOF
```

## 실행

```bash
# 전체 테스트
make -C tests

# 특정 카테고리
make -C tests cli

# 단일 파일
fslit tests/cli/01-addition.flt

# 프로젝트 루트에서 직접
cd /project/root && fslit tests/
```

## 출력 예시

**성공:**
```
PASS: tests/cli/01-addition.flt
PASS: tests/cli/02-precedence.flt
PASS: tests/file-input/01-from-file.flt

Results: 3/3 passed
```

**실패:**
```
FAIL: tests/cli/01-addition.flt
  Line 1: expected "5", got "6"

Results: 0/1 passed
```

## 주의사항

1. **한 파일 = 한 테스트**: fslit은 파일당 하나의 테스트만 지원
2. **정확한 출력**: 공백, 줄바꿈까지 정확히 일치해야 함
3. **작업 디렉토리**: Command의 상대 경로는 fslit 실행 위치 기준
4. **번호 접두사**: `01-`, `02-` 형식으로 실행 순서 명시

## 테스트 추가 워크플로우

```bash
# 1. 실제 출력 확인
dotnet run --project MyApp -- --expr "new feature"

# 2. 테스트 파일 생성
cat > tests/feature/01-new.flt << 'EOF'
// Test: New feature
// --- Command: dotnet run --project MyApp -- --expr "new feature"
// --- Output:
<실제 출력 붙여넣기>
EOF

# 3. 테스트 실행
make -C tests feature
```

## 체크리스트

- [ ] fslit 설치됨 (`dotnet tool install -g fslit`)
- [ ] 테스트 디렉토리 구조 생성
- [ ] Makefile 작성 (cd .. && fslit tests/)
- [ ] 각 테스트는 별도 .flt 파일
- [ ] 기대 출력은 실제 실행 결과와 정확히 일치

## 관련 문서

- [testing-strategies](testing-strategies.md) - 전체 테스트 전략
- [fslit GitHub](https://github.com/ohama/fslit) - 공식 저장소
