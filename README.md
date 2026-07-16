# Modus

제조실행시스템(MES). 사이드 프로젝트로, **강한 스캐폴드 + 선언적 엔티티**로 화면을 거의 자동으로 찍어내는 것을 목표로 한다.

- **백엔드**: .NET 8 · ASP.NET Core Web API · EF Core 8 · PostgreSQL
- **프론트**: React + TypeScript · Vite · Mantine · TanStack Query
- **멀티테넌시**: DB-per-tenant (테넌트별 DB 격리)
- **인증**: JWT + HttpOnly 쿠키 + CSRF

## 모듈 로드맵
기초코드 → 영업 → 구매 → 생산 → 품질 → (확장)

## 문서
- [`docs/MES-설계계획서.md`](docs/MES-설계계획서.md) — 설계·아키텍처·엔티티·로드맵
- `docs/STATUS.md` — 진행 상세 (작성 예정)
- `docs/BACKLOG.md` — 남은 작업 (작성 예정)

## 개발 (예정)
```
# DB
docker compose up -d
# 백엔드
dotnet run --project backend/Mes.Api
# 프론트
cd web && npm install && npm run dev
```
