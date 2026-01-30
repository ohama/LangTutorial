# Tutorial Command

튜토리얼 chapter 목록을 표시하고 작성을 관리한다.

## 실행 시 동작

1. **Chapter 목록 표시**: `tutorial/` 디렉토리의 모든 chapter 파일을 나열한다.

2. **상태 표시**: 각 chapter의 완성 상태를 표시한다.
   - ✓ 완성: 파일이 존재하고 내용이 있음
   - ○ 미작성: 파일이 없음

3. **다음 chapter 제안**: 아직 작성되지 않은 다음 chapter를 추천한다.

4. **작성 명령**: 인자로 chapter 이름을 받으면 해당 chapter 작성을 시작한다.

## 실행 방법

```bash
# 목록/상태 표시
ls -la tutorial/*.md 2>/dev/null || echo "No chapters yet"
```

## Chapter 구조

| Chapter | 파일명 | 내용 |
|---------|--------|------|
| 1 | chapter-01-foundation.md | .NET 10 + FsLexYacc 프로젝트 설정 |
| 2 | chapter-02-arithmetic.md | 사칙연산 인터프리터 |
| 3 | chapter-03-variables.md | 변수 바인딩과 스코프 |
| 4 | chapter-04-conditionals.md | 조건문과 Boolean |
| 5 | chapter-05-functions.md | 함수 정의와 호출 |
| 6 | chapter-06-quality.md | 에러 처리와 REPL |

## Chapter 작성 가이드

각 chapter는 다음을 포함해야 한다:
- 개요: 이 chapter에서 추가하는 기능
- 핵심 코드: 주요 변경 사항만 설명 (Lexer, Parser, AST, Evaluator)
- 실행 예제: 입력과 출력 예시
- 소스 참조: 전체 코드는 `FunLang/` 디렉토리 참고하라고 안내

## FunLang CLI 인터페이스

```
funlang [options] [file]

Options:
  --expr <expr>      식을 직접 평가 (파일 대신)
  --emit-tokens      토큰 출력 (디버깅/학습용)
  --emit-ast         AST 출력 (디버깅/학습용)
  --interactive, -i  REPL 모드
```

- `--expr` 없으면 파일에서 프로그램 읽음
- 적절한 시점에 점진적으로 추가

## 디버깅 전략

문제 발생 시 적극 활용:
- `--emit-tokens`: 렉서 출력 확인
- `--emit-ast`: 파서 출력 확인
- Serilog: 평가 과정 추적

## 테스트 도구

- **Expecto**: 단위 테스트 프레임워크
- **FsCheck**: 속성 기반 테스트 (property-based testing)
- **fslit**: 파일 기반 테스트 (LLVM lit 스타일) - https://github.com/ohama/fslit

### fslit 테스트 형식 (.flt)

```
// --- Command: dotnet run --project FunLang -- %input
// --- Input:
2 + 3 * 4
// --- Output:
14
```

```
// --- Command: dotnet run --project FunLang -- --emit-ast %input
// --- Input:
2 + 3
// --- Output:
Add(Int 2, Int 3)
```

## 인자 처리

`$ARGUMENTS`가 있으면:
- chapter 번호나 이름으로 해당 chapter 작성 시작
- 예: `/tutorial 2` → chapter-02-arithmetic.md 작성

`$ARGUMENTS`가 없으면:
- 현재 상태 표시 및 다음 chapter 제안
