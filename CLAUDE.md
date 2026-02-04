# FunLang Project Context

F# 언어 구현 튜토리얼 프로젝트. fslex/fsyacc를 사용한 인터프리터 구축.

## 핵심 문서

- `.planning/STATE.md` - 현재 진행 상태, 다음 작업
- `.planning/ROADMAP.md` - Phase별 요구사항, 성공 기준
- `TESTING.md` - **테스트 확장 가이드** (fslit 템플릿 포함)

## 테스트 실행

```bash
make -C tests                      # fslit 테스트 (66개)
dotnet run --project FunLang.Tests # Expecto 테스트 (129개)
```

## 현재 상태

- Phase 1-5, 7 완료 (86%)
- Phase 6 보류
- **Turing-complete 언어 달성**

## 문서 사이트

```bash
./scripts/mdbook-setup   # 초기 설정 (docs/ 백업 후 빌드)
mdbook serve book --open # 로컬 미리보기
mdbook build book        # 빌드
```

- 소스: `book/src/`
- 출력: `docs/` (GitHub Pages)

## 작업 시 참고

1. 테스트 추가: `TESTING.md` 참조
2. howto 문서: `docs.backup/howto/README.md`
