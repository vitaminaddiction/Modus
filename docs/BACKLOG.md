# BACKLOG — 남은 작업 (다음 세션이 이어받는 지점)

> 규칙: 다음 세션은 이 파일 맨 위 청크부터. 완료하면 STATUS.md로 옮기고 여기서 제거.

## Phase 0 남은 청크 (권장: 청크당 한 세션)

### 청크 A — #6 인증 + Program.cs 배선
- `Program.cs` 재작성: 컨트롤러 방식, `AddModusInfrastructure`, CORS(AllowFrontend), 인증/인가, 미들웨어 순서(UseAuthentication → TenantResolution → UseAuthorization).
- JWT 발급(HttpOnly 쿠키 `access_token`, `tenant` 클레임 포함), CSRF double-submit 미들웨어(XSRF-TOKEN 쿠키 + 헤더 검증).
- `IPasswordHasher` DI 등록, 로그인 엔드포인트 `POST /api/web/auth/login` (테넌트+loginId+password), 로그아웃.
- 검증: demo 테넌트로 로그인 → 쿠키 발급 확인(curl/REST).

### 청크 B — #7 제네릭 CRUD 엔진
- `IRepository<T>` + 구현(ModusDbContext 기반), `CrudController<TEntity,TDto>` (목록/조회/생성/수정/삭제 + 페이징/정렬/필터).
- 감사필드 자동(SaveChanges 오버라이드: CreatedAt/By, UpdatedAt/By).
- `CodeGroupController`, `CodeController`로 검증(상속만).
- 검증: 인증+테넌트 헤더로 공통코드 CRUD (curl).

### 청크 C — #8 OpenAPI + gen:api
- Swashbuckle 스키마 정리(쿠키 인증 표기), `web/`에서 `npm run gen:api`(openapi-typescript)로 TS 타입.

### 청크 D — #9 프론트 스캐폴드
- Vite React TS, Mantine 테마(다크모드), TanStack Query, `AppLayout`(사이드바), api 래퍼(credentials:include + X-XSRF-TOKEN + X-Tenant-Code), 로그인 화면.
- 제네릭 `CrudPage` + `resources.tsx` 레지스트리 + 컬럼 헬퍼(col.text/number/code/date/ref/enabled).

### 청크 E — #10 E2E 관통 검증
- demo 로그인 → 공통코드그룹/공통코드 화면 CRUD 동작.
- "설정 1개 = 새 화면" + "테넌트 DB 격리" 최종 확인. STATUS 갱신.

## Phase 1+ (이후)
- Phase 1 기초코드 대량 생성(거래처/품목/단위/창고/공정/설비/BOM/라우팅…)
- Phase 2 영업+구매+재고수불 커널 · Phase 3 생산 · Phase 4 품질
- 상세: `docs/MES-설계계획서.md` §4~5
