# FunLang Project Context

F# 언어 구현 튜토리얼 프로젝트. fslex/fsyacc를 사용한 인터프리터 구축.

## 핵심 문서

- `.planning/STATE.md` - 현재 진행 상태, 다음 작업
- `.planning/ROADMAP.md` - Phase별 요구사항, 성공 기준
- `TESTING.md` - **테스트 확장 가이드** (fslit 템플릿 포함)

## 테스트 실행

```bash
make -C tests        # 전체 테스트 (21개)
make -C tests check  # 빌드 후 테스트
```

## 현재 상태

- Phase 1, 2, 7 완료 (43%)
- 다음: Phase 3 (Variables & Binding)

## 작업 시 참고

1. 새 Phase 시작: `/gsd:plan-phase N`
2. 테스트 추가: `TESTING.md` 참조
3. howto 문서: `docs/howto/README.md`
