# FunLang YouTube 시리즈

F#으로 프로그래밍 언어 만들기 튜토리얼 유튜브 대본.

## 시리즈 개요

| 에피소드 | 제목 | 예상 길이 | 주요 내용 |
|----------|------|-----------|-----------|
| 1 | 프로젝트 설정 | 12-15분 | fslex/fsyacc 설정, 빌드 순서 |
| 2 | 사칙연산 계산기 | 15-18분 | Expr/Term/Factor 패턴, Evaluator |
| 3 | 변수와 스코프 | 15-18분 | Environment, 섀도잉 |
| 4 | 조건문과 Boolean | 18-20분 | Value 타입, 타입 검사, 단락 평가 |
| 5 | 함수와 클로저 | 20-25분 | Lambda, 재귀, Turing-complete |

**총 예상 길이:** 80-96분 (약 1시간 30분)

## 대본 파일

- [episode-01-foundation.md](episode-01-foundation.md) - 프로젝트 설정
- [episode-02-arithmetic.md](episode-02-arithmetic.md) - 사칙연산 계산기
- [episode-03-variables.md](episode-03-variables.md) - 변수와 스코프
- [episode-04-conditionals.md](episode-04-conditionals.md) - 조건문과 Boolean
- [episode-05-functions.md](episode-05-functions.md) - 함수와 클로저

## 대본 형식

각 대본에는 다음이 포함됩니다:

- **썸네일 텍스트** - 영상 썸네일용
- **타임스탬프 구간** - 예상 시간대
- **화면 지시** - [화면: 설명] 형식
- **나레이션** - 실제 말할 내용
- **코드 블록** - 화면에 보여줄 코드
- **B-roll 제안** - 화면 전환/효과 아이디어
- **태그** - 유튜브 태그

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

5개 에피소드에 걸쳐:
- 숫자만 인식하는 파서에서 시작
- 사칙연산 계산기로 발전
- 변수와 조건문 추가
- 함수와 재귀까지 구현
- 최종적으로 Turing-complete 언어 완성!

컴파일러 이론 수업 없이도, 실습 중심으로 언어 구현의 핵심을 배울 수 있습니다.

전체 코드: https://github.com/ohama/LangTutorial
```

## 관련 자료

- `tutorial/` - 텍스트 튜토리얼 (참고 자료)
- `FunLang/` - 실제 구현 코드
- `tests/` - 테스트 케이스 (데모용)
