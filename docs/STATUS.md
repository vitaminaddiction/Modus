# STATUS — 완료된 것 (검증 기준)

> 갱신 규칙: 청크를 끝내고 커밋할 때마다 여기 반영. "검증됨"은 실제로 돌려서 확인한 것만.

## Phase 0 — 스캐폴드 (진행중, 6/10)

| # | 항목 | 상태 | 검증 |
|---|---|---|---|
| 1 | 솔루션 골격 + 5개 프로젝트(`Modus.*`) + `global.json`(SDK8) | ✅ | `dotnet build` 그린 |
| 2 | docker PostgreSQL 16 (host 5433) | ✅ | 컨테이너 healthy, `select version()` |
| 3 | 도메인: EntityBase/CodeNameBase + Tenant·CodeGroup·Code·User | ✅ | 빌드 그린 |
| 4 | 멀티테넌시: Catalog/Modus DbContext + 테넌트 해석 미들웨어 | ✅ | 빌드 그린 |
| 5 | EF 마이그레이션 + `Modus.Tools`(migrate/provision) | ✅ | `provision demo` 실행, DB/테이블/admin 확인 |
| 6 | 인증: JWT 쿠키 + CSRF + 로그인 + Program 배선 | ✅ | API 기동, 로그인/me/실패케이스 curl 검증 |
| 7 | 제네릭 CRUD 엔진 (`CrudController<T>`, `IRepository<T>`) | ⬜ | |
| 8 | OpenAPI + `gen:api` 타입 생성 | ⬜ | |
| 9 | 프론트 스캐폴드 (Vite+React+Mantine+CrudPage+resources+로그인) | ⬜ | |
| 10 | E2E 관통: demo 테넌트 공통코드 CRUD | ⬜ | |

## 검증된 사실 (2026-07-16)
- DB 2개 물리 분리: `modus_catalog`, `modus_t_demo`.
- `modus_t_demo` 테이블: `app_user`, `code`, `code_group`, `__EFMigrationsHistory` (snake_case).
- 카탈로그 `tenant`: `demo / 데모 테넌트 / modus_t_demo / enabled`.
- admin 계정 시드됨: `admin` / `admin1234` (Role=Admin). PBKDF2-SHA256 해시.
- API `http://localhost:5176`(http 프로필). 인증 E2E 검증(2026-07-16):
  - `POST /api/web/auth/login` (X-Tenant-Code 헤더 필요) → 200 + access_token(HttpOnly)/XSRF-TOKEN 쿠키
  - `GET /api/web/auth/me` 쿠키 인증 → 200, `POST logout` → 204
  - 틀린 비번 401, 테넌트 없음/모름 400(깔끔한 JSON). 전 `/api`는 테넌트 필수(미들웨어 검증).
  - dev JWT 서명키는 미설정 시 코드 폴백(운영은 `Jwt:SigningKey` 필수).

## 주요 커밋 (main)
- `d7096e3` 설계계획서 + 리포 초기
- `feat` 솔루션 골격 / PostgreSQL+도메인 / 멀티테넌시 / 마이그레이션+Tools
- (최신은 `git log --oneline` 참조)
