# FunLang YouTube 시리즈

F#으로 프로그래밍 언어 만들기 튜토리얼 유튜브 대본.

## 시리즈 개요

### 본편 (Chapters)

| EP | 제목 | 파일 | 예상 길이 |
|----|------|------|-----------|
| 01 | Foundation & Pipeline | [01-foundation.md](01-foundation.md) | 12-15분 |
| 02 | 사칙연산 계산기 | [02-arithmetic.md](02-arithmetic.md) | 15-18분 |
| 03 | 변수와 스코프 | [03-variables.md](03-variables.md) | 15-18분 |
| 04 | 조건문과 Boolean | [04-conditionals.md](04-conditionals.md) | 18-20분 |
| 05 | 함수와 클로저 | [05-functions.md](05-functions.md) | 20-25분 |
| 06 | 튜플 | [06-tuples.md](06-tuples.md) | 15-18분 |
| 07 | 리스트 | [07-lists.md](07-lists.md) | 18-20분 |
| 08 | 패턴 매칭 | [08-pattern-matching.md](08-pattern-matching.md) | 18-20분 |
| 09 | Prelude | [09-prelude.md](09-prelude.md) | 18-20분 |
| 10 | Hindley-Milner 타입 시스템 | [10-type-system.md](10-type-system.md) | 20-22분 |

### 부록 (Appendix)

| EP | 제목 | 파일 | 예상 길이 |
|----|------|------|-----------|
| A1 | 테스트 전략 | [appendix-01-testing.md](appendix-01-testing.md) | 15-18분 |

**총 예상 길이:** 약 3시간

## 대본 형식

각 대본에는 다음이 포함됩니다:

- **영상 정보** - 예상 길이, 난이도, 사전 지식
- **타임스탬프 구간** - 예상 시간대
- **화면 지시** - [화면: 설명] 형식
- **나레이션** - 실제 말할 내용
- **코드 블록** - 화면에 보여줄 코드
- **핵심 키워드** - SEO용 태그

## 촬영 팁

### 화면 구성

1. **코딩 화면**: VS Code + 터미널 분할
2. **다이어그램**: draw.io 또는 Excalidraw
3. **슬라이드**: 요약/개념 설명용

### 녹화 설정

- 해상도: 1920x1080 (Full HD)
- 폰트 크기: 터미널 18pt, 에디터 16pt
- 테마: 어두운 배경 권장 (눈 피로 감소)

### 편집 포인트

- 코드 타이핑은 실시간 또는 2x 속도
- 빌드/실행 대기 시간은 컷
- 핵심 포인트에 "핵심!", "중요!" 팝업

## 시리즈 플레이리스트 설명

```
F#과 fslex/fsyacc로 프로그래밍 언어를 직접 만들어 봅니다.

10개 에피소드에 걸쳐:
- 숫자만 인식하는 파서에서 시작
- 사칙연산 계산기로 발전
- 변수, 조건문, 함수, 재귀 추가
- 튜플, 리스트, 패턴 매칭 구현
- Self-hosted Prelude 표준 라이브러리
- Hindley-Milner 타입 추론 시스템
- 최종적으로 정적 타입의 Turing-complete 언어 완성!

컴파일러 이론 수업 없이도, 실습 중심으로 언어 구현의 핵심을 배울 수 있습니다.

전체 코드: https://github.com/ohama/LangTutorial
```

## 관련 자료

- `tutorial/` - 텍스트 튜토리얼 (참고 자료)
- `FunLang/` - 실제 구현 코드
- `tests/` - 테스트 케이스 (데모용)
