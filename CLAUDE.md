# CLAUDE.md — Modus 온보딩 (세션 시작 시 자동 로드)

> 이 파일 + `docs/STATUS.md`(완료) + `docs/BACKLOG.md`(남은 일)만 읽으면 어느 세션에서든 이어서 작업할 수 있다.
> **작업 규칙: 청크 단위로 작업하고, 끝나면 커밋 + STATUS/BACKLOG 갱신.** 대화가 아니라 이 리포가 진실의 원천이다.

## 무엇
**Modus** — 제조실행시스템(MES). 사이드 프로젝트. 핵심 목표는 **강한 스캐폴드로 화면을 "거의 자동" 생성**하는 것.
(제품명은 Modus. "SmartQ MES"는 옛 명칭이니 쓰지 말 것. 네임스페이스 접두어도 `Modus.*`.)

## 스택
- 백엔드: .NET 8 (SDK 8.0.423, `global.json` 고정) · ASP.NET Core · **EF Core 8** + Npgsql · PostgreSQL 16
- 프론트: React + TS + Vite + **Mantine** + TanStack Query (아직 미착수)
- 인증: JWT + HttpOnly 쿠키 + CSRF (진행 예정)
- 멀티테넌시: **DB-per-tenant** (테넌트별 DB 물리 분리)

## 리포 구조
```
backend/
  Modus.Domain/          엔티티(단일 진실 소스): Common/, Catalog/, BaseCode/, Security/
  Modus.Application/     추상화: Multitenancy/ITenantContext, Security/IPasswordHasher
  Modus.Infrastructure/  EF: Persistence/(DbContext, Migrations), Multitenancy/, Security/, DependencyInjection
  Modus.Api/             ASP.NET Core (Middleware/TenantResolutionMiddleware). Program.cs는 아직 템플릿
  Modus.Tools/           CLI: migrate-catalog / migrate-tenants / provision
web/                     (미착수)
docker-compose.yml       PostgreSQL (host 5433)
docs/                    STATUS.md, BACKLOG.md, MES-설계계획서.md
```

## 실행법
```bash
# 1) DB
docker compose up -d                       # postgres:16, localhost:5433, modus/modus_dev_pw

# 2) 마이그레이션/테넌트 (backend/ 에서)
dotnet run --project Modus.Tools -- migrate-catalog
dotnet run --project Modus.Tools -- provision demo "데모 테넌트"   # DB생성+등록+마이그+admin시드
dotnet run --project Modus.Tools -- migrate-tenants                # 스키마 변경 후 전 테넌트 반영

# 3) 마이그레이션 추가 (엔티티 바꾼 뒤, backend/ 에서)
dotnet ef migrations add <Name> --context ModusDbContext \
  --project Modus.Infrastructure/Modus.Infrastructure.csproj \
  --startup-project Modus.Api/Modus.Api.csproj -o Persistence/Migrations/Tenant
# 카탈로그 스키마 바꿀 때만 --context CatalogDbContext ... -o Persistence/Migrations/Catalog

# 4) API / 웹
dotnet run --project Modus.Api
cd web && npm install && npm run dev
```
개발 로그인: 테넌트 `demo`, 계정 `admin` / `admin1234`.

## 멀티테넌시 동작
- 카탈로그 DB `modus_catalog`의 `tenant` 테이블이 테넌트별 접속정보 보유.
- 요청에서 테넌트 해석: **`X-Tenant-Code` 헤더 → JWT `tenant` 클레임 → 서브도메인** (`TenantResolutionMiddleware`).
- `ModusDbContext`는 요청 시점에 현재 테넌트의 커넥션으로 해석됨(`AddModusInfrastructure`).
- 새 테넌트 = `provision <code>` 한 방.

## 자동화 패턴 — "새 마스터 화면 = 엔티티 1개 + 설정 1개"
1. `Modus.Domain`에 엔티티 추가 (대개 `CodeNameBase` 상속)
2. `ModusDbContext`에 `DbSet` + 매핑 → `dotnet ef migrations add` → `migrate-tenants`
3. (백엔드 CRUD 엔진 완성 후) 얇은 컨트롤러 1개
4. (프론트 완성 후) `web/src/config/resources.tsx`에 설정 1블록
→ 마이그레이션/타입/그리드/폼은 자동.

## 규칙 & 함정
- **ID**: `long` + PG IDENTITY (EF 기본). HiLo 안 씀.
- **네이밍**: EF snake_case 컨벤션(`code_group`, `created_at`). `User`→테이블 `app_user`(PG 예약어 회피).
- **포트**: host 5433 (55432는 Windows 예약범위라 바인딩 거부됨).
- **시크릿**: 리포가 **PUBLIC**. `*.Development.json`, `.env` 등은 gitignore. dev 비번(`modus_dev_pw`)만 예외적으로 노출(개발용).
- **git**: 커밋 author `vitaminaddiction`, co-author 트레일러 유지. 리모트 `github.com/vitaminaddiction/Modus`.
- **범위 밖**: 설비 실시간 수집은 DX(`D:\Work\DX`) 경로. EasyFactory는 폐기.
