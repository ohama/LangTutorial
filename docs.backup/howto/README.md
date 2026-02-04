# Howto Documents

| # | 문서 | 설명 | 작성일 |
|---|------|------|--------|
| 1 | [implement-bidirectional-type-checking](implement-bidirectional-type-checking.md) | Algorithm W를 양방향 타입 체킹으로 확장하여 타입 어노테이션 지원 | 2026-02-04 |
| 2 | [design-type-expression-grammar-fsyacc](design-type-expression-grammar-fsyacc.md) | FsYacc에서 %left/%right 없이 문법 계층 구조로 타입 표현식 우선순위 구현 | 2026-02-03 |
| 3 | [track-source-positions-fslexyacc](track-source-positions-fslexyacc.md) | FsLexYacc에서 소스 위치 추적하여 정확한 에러 메시지 생성 | 2026-02-03 |
| 4 | [implement-hindley-milner-algorithm-w](implement-hindley-milner-algorithm-w.md) | F#에서 Hindley-Milner 타입 추론 시스템 (Algorithm W) 구현 | 2026-02-01 |
| 5 | [implement-let-polymorphism](implement-let-polymorphism.md) | Let-polymorphism 구현 시 generalize/instantiate 순서와 substitution threading | 2026-02-01 |
| 6 | [avoid-type-variable-collision](avoid-type-variable-collision.md) | Prelude 타입 스킴과 fresh 타입 변수 간 충돌 방지 패턴 | 2026-02-01 |
| 7 | [write-type-inference-tests](write-type-inference-tests.md) | Hindley-Milner 타입 추론 테스트 작성 패턴 (Expecto + F#) | 2026-02-01 |
| 8 | [setup-argu-cli](setup-argu-cli.md) | F# CLI를 Argu로 선언적으로 정의하기 | 2026-02-01 |
| 9 | [write-fsharp-repl-loop](write-fsharp-repl-loop.md) | 환경 스레딩과 에러 복구가 있는 F# REPL 루프 구현 | 2026-02-01 |
| 10 | [resolve-function-application-vs-subtraction](resolve-function-application-vs-subtraction.md) | fsyacc에서 함수 호출과 뺄셈 연산자 문법 충돌을 Atom 비단말로 해결 | 2026-01-30 |
| 11 | [adapt-tests-for-value-type-evolution](adapt-tests-for-value-type-evolution.md) | 인터프리터의 evalExpr가 int에서 Value 타입으로 진화할 때 기존 테스트 호환성 유지 | 2026-01-30 |
| 12 | [avoid-expecto-flip-pitfall](avoid-expecto-flip-pitfall.md) | Expecto.Flip이 Expect 함수 인자 순서를 뒤집는 문제와 해결법 | 2026-01-30 |
| 13 | [implement-unary-minus](implement-unary-minus.md) | 파서에서 단항 마이너스 구현 - 이항 마이너스와 구분, 우선순위 처리 | 2026-01-30 |
| 14 | [fsyacc-operator-precedence-methods](fsyacc-operator-precedence-methods.md) | FsYacc에서 연산자 우선순위를 처리하는 3가지 방법 비교 | 2026-01-30 |
| 15 | [setup-expecto-test-project](setup-expecto-test-project.md) | F# 프로젝트에 Expecto 테스트 프로젝트 추가하기 | 2026-01-30 |
| 16 | [fsyacc-precedence-without-declarations](fsyacc-precedence-without-declarations.md) | FsYacc에서 %left/%right 대신 문법 구조로 연산자 우선순위 처리 | 2026-01-30 |
| 17 | [write-fsyacc-parser](write-fsyacc-parser.md) | fsyacc로 F# 파서 작성하기 - 개념, 구조, 예제 | 2026-01-30 |
| 18 | [write-fslex-lexer](write-fslex-lexer.md) | fslex로 F# 렉서 작성하기 - 개념, 구조, 예제 | 2026-01-30 |
| 19 | [setup-fslexyacc-build-order](setup-fslexyacc-build-order.md) | FsLexYacc에서 Parser before Lexer 빌드 순서 설정 | 2026-01-30 |
| 20 | [testing-strategies](testing-strategies.md) | FunLang 테스트 전략 - fslit, Expecto, FsCheck 활용 가이드 | 2026-01-30 |
| 21 | [write-fscheck-property-tests](write-fscheck-property-tests.md) | FsCheck로 속성 기반 테스트 작성 - 수학적 불변식 검증 | 2026-01-30 |
| 22 | [write-fslit-file-tests](write-fslit-file-tests.md) | fslit으로 CLI 파일 기반 테스트 작성 - LLVM lit 스타일 | 2026-01-30 |

---
총 22개 | 업데이트: 2026-02-04
