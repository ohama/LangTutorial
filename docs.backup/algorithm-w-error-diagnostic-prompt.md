# Algorithm W 에서 정확한 에러 위치 표현을 위한 구현 프롬프트

이 문서는 **Algorithm W 기반 타입 추론기**에서 에러 위치를 정확히 표현하고,  
향후 **Bidirectional Typing**으로 확장 가능하도록 설계하기 위한 **Claude Code 지시용 프롬프트**입니다.

아래 내용을 그대로 Claude Code에 입력하여 사용하세요.

---

## Claude Code 프롬프트

너는 F#로 작성된 소형 함수형 언어의 타입체커를 개선하는 작업을 한다. 현재는 Algorithm W 기반의 Hindley–Milner 타입 추론이 있고, 에러 메시지가 부정확하다. 목표는 “bidirectional typing으로 넘어가기 전”에 **에러 위치/원인을 정확히 표현하는 인프라**를 설계·구현하는 것이다. 이 인프라는 이후 bidirectional typing으로 전환해도 재사용 가능해야 한다.

### 0) 핵심 목표
1) 타입 에러가 났을 때 **가능한 한 작은 원인(sub-expression)** 을 가리키는 메시지를 만든다.  
2) 에러 보고는 “W 전용”이 아니라, **타입체커 전반에서 공통으로 쓰는 Diagnostic 레이어**로 만든다.  
3) 나중에 bidirectional typing 적용 시에도 같은 Diagnostic 레이어(스팬, 트레이스, 기대/실제 타입 표현, 컨텍스트 스택)를 그대로 쓴다.

---

### 1) 현재 코드 파악
- AST 타입(Expr), 위치 정보(Span/Range), 타입(Type), 스키마(Scheme), 환경(Env), unify/instantiate/generalize 함수의 위치를 파악한다.
- 현재 타입 에러가 어디서, 어떤 방식으로 발생하는지 흐름을 정리한다.

---

### 2) 공통 인프라 설계: Diagnostic / TypeError

#### 2.1 Span/Range
- 모든 Expr 노드에 span(파일, 시작/끝 라인·컬럼)을 포함시킨다.
- 파서/lexer 단계에서 생성하여 AST에 저장한다.

#### 2.2 Diagnostic 타입
- Diagnostic 구조 예시:
  - code
  - message
  - primarySpan
  - secondarySpans
  - notes
  - hint
  - related
- TypeError는 문자열이 아니라 다음 정보를 포함한다:
  - kind (UnifyMismatch | OccursCheck | UnboundVar | NotAFunction | …)
  - span
  - expected / actual
  - term (관련 expr)
  - contextStack
  - trace

TypeError → Diagnostic 변환 레이어를 분리한다.

#### 2.3 Context Stack
- 타입 추론 중 하위 expr로 내려갈 때마다 context를 push한다.
- 예시:
  - InIfCond(span)
  - InIfThen(span)
  - InIfElse(span)
  - InAppFun(span)
  - InAppArg(span)
  - InLetRhs(name, span)
- 에러 발생 시 contextStack을 함께 저장한다.

#### 2.4 Unification Trace
- unify 실패 시 단순 실패가 아니라 “충돌 경로”를 기록한다.
- 예시:
  - AtFunctionReturn
  - AtTupleIndex(i)
  - AtListElement
  - AtRecordField(name)

---

### 3) Algorithm W 적용 전략

#### 3.1 Unify 호출 정보 확장
- unify 호출 시 “어떤 expr 때문에 이 unify가 발생했는지”를 함께 전달한다.

#### 3.2 Primary / Secondary Span 선택
- 가장 직접적인 원인 expr의 span을 primarySpan으로 설정한다.
- 상대 expr는 secondarySpan으로 설정한다.

#### 3.3 대표 규칙 (Blame Assignment)
- NotAFunction → fun expr span
- ArgMismatch → arg expr span
- BranchMismatch → then/else span 모두 표시

가능하면 **가장 안쪽(span이 짧은)** expr를 우선한다.

---

### 4) 에러 출력 포맷
에러 메시지에는 반드시 다음을 포함한다:
- 에러 코드 (예: E0301)
- primary 위치 (라인/컬럼)
- 기대 타입 vs 실제 타입
- context stack 요약
- 힌트 1개

타입 pretty-printer는 타입 변수를 a,b,c…로 정규화한다.

---

### 5) 테스트 (필수)

xUnit + Expecto 기반 골든 테스트 작성:
- 입력 소스 → 타입체크 → Diagnostic 반환
- Diagnostic.code, primarySpan, message 일부를 검증

필수 테스트 케이스:
1) if 조건이 int인 경우
2) 함수가 아닌 값 호출
3) 인자 타입 불일치
4) let rhs 에러
5) occurs check

---

### 6) Bidirectional Typing 확장 포인트
- expected type을 TypeError에 포함 가능하도록 설계
- context stack / trace / span 구조는 그대로 재사용
- W의 unify 실패와 bidirectional check 실패를 같은 Diagnostic으로 렌더링

---

### 7) 산출물
- 변경/추가된 파일 목록
- Diagnostic / TypeError / Span 설계 요약 문서
- 테스트 실행 방법

작업은 단계별로 진행하고, 각 단계마다 컴파일과 테스트가 통과하도록 한다.

---

## 메모
이 구조는 Algorithm W의 한계를 보완하면서,  
Bidirectional Typing으로 자연스럽게 확장하기 위한 **중간 단계 인프라**를 만드는 것이 핵심이다.
