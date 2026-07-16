# SmartQ MES (Greenfield) — 설계·계획서

> 사이드 프로젝트. 핵심 목표는 **"거의 자동으로" 화면을 찍어내는 것**.
> 처음부터 전 기능을 만들지 않고 기초코드 → 영업 → 구매 → 생산 → 품질 순으로 확장한다.
> 작성 2026-07-16 · 상태: **확정** · 리포 github.com/vitaminaddiction/Modus (public)

---

## 0. 핵심 전략 — "거의 자동"의 실체

AI에게 "MES 만들어줘"라고 던지면 **일관성 없는 버릴 코드**가 나온다.
정답은 **한 번만 강한 스캐폴드(생성 엔진)를 만들고, 나머지를 선언적으로 찍어내는 것**이다.

```
C# 엔티티 1개 정의 ─┬─▶ EF Core 마이그레이션 (DB 스키마 자동)
                   ├─▶ 제네릭 CRUD API (등록 1줄)
                   ├─▶ OpenAPI → TS 타입/클라이언트 (자동생성)
                   └─▶ 프론트 리소스 설정 1개 → 제네릭 CrudPage가 그리드+폼 렌더
```

**새 마스터 화면 = C# 엔티티 1개 + 리소스 설정 1개.** 마이그레이션/API/타입은 전부 자동.
→ **스캐폴드에 시간을 몰아넣는 게 곧 자동화.**

| 모듈 | 성격 | 자동화 |
|---|---|---|
| 기초코드 | 순수 CRUD 마스터 | ~100% 생성 |
| 영업 | 헤더-상세 문서 + 상태흐름 + 재고이동 | ~70% |
| 구매 | 영업과 대칭 | ~70% |
| 생산 | BOM전개·재공·LOT 실로직 | ~50% |
| 품질 | 마스터 + 검사판정 로직 | ~50% |

---

## 1. 기술 스택 (확정)

### 백엔드 — .NET 8 + PostgreSQL
- **ASP.NET Core Web API** (C#) · 타깃 `net8.0` (SDK 8.0.423 확인)
- **ORM: Entity Framework Core 8** *(NHibernate 아님)* + **Npgsql**
  - `dotnet ef migrations`로 스키마 자동생성, 제네릭 리포지토리·`DbSet` 리플렉션으로 CRUD 엔진.
- **DB: PostgreSQL** (개발은 Docker)
- **인증: JWT + HttpOnly 쿠키 + CSRF(double-submit)**
  - access_token=HttpOnly 쿠키, XSRF-TOKEN=비HttpOnly, CSRF 미들웨어.
- **API 문서: Swashbuckle(OpenAPI)** → 프론트 타입 생성 소스

### 프론트엔드 — React + TypeScript
- **UI: Mantine** ✅ (데이터테이블·폼·모달·알림·날짜·다크모드 내장 → 최소 시간에 예쁨)
- **그리드**: `mantine-datatable`
- **서버상태**: TanStack Query · **빌드**: Vite
- **타입생성**: `npm run gen:api` (openapi-typescript) — 백엔드 OpenAPI → TS

---

## 2. 멀티테넌시 — DB-per-tenant (확정, Phase 0부터)

테넌트별로 **DB를 물리 격리**한다.

```
[Catalog DB: modus_catalog]           [Tenant DBs]
  Tenant(Id, Code, Name,        ┌────▶ modus_t_demo   (테넌트 A 전체 데이터)
         Host, DbName,          ├────▶ modus_t_acme   (테넌트 B)
         User, Enabled) ────────┘      ...
```

- **테넌트 해석**: 요청에서 `서브도메인` 또는 `X-Tenant-Code` 헤더 또는 JWT `tenant` 클레임
  → 미들웨어가 `ITenantContext`(AsyncLocal)에 현재 테넌트 세팅.
- **DbContext**: 요청 시점에 카탈로그에서 커넥션스트링 해석 → 테넌트 DB에 연결.
- **사용자/데이터**: 각 테넌트 DB 안에 존재. 로그인은 (테넌트 + 계정) 조합.
- **마이그레이션**: 카탈로그 DB는 자체 마이그레이션. 마이그레이터 툴이 **전 테넌트 DB를 순회**하며 MES 마이그레이션 적용.
- **테넌트 프로비저닝**: DB 생성 → 마이그레이션 → admin 시드 (툴 명령 1개).
- 개발: docker postgres에 `modus_catalog` + `modus_t_demo` 두 개.

---

## 3. 아키텍처 / 리포 구조

```
Modus/
├─ backend/
│  ├─ Mes.Api/            # ASP.NET Core 진입점, 컨트롤러, 인증, 미들웨어
│  ├─ Mes.Domain/         # 엔티티(단일 진실 소스), 열거형
│  ├─ Mes.Infrastructure/ # DbContext(Tenant/Catalog), EF 설정, 마이그레이션, 리포지토리
│  ├─ Mes.Application/    # 서비스/유스케이스 — 생산·품질 로직
│  └─ Mes.Tools/          # 마이그레이터/테넌트 프로비저닝 CLI
├─ web/
│  ├─ src/crud/CrudPage.tsx      # 제네릭: 툴바 + 그리드 + 필드모달
│  ├─ src/config/resources.tsx   # 리소스 레지스트리(화면=설정 1개)
│  ├─ src/api/                   # gen:api 산출물 + fetch 래퍼
│  └─ src/layout/AppLayout.tsx   # 사이드바 + 라우팅
├─ docker-compose.yml            # PostgreSQL
└─ docs/
```

### 백엔드 CRUD 엔진 (자동화 핵심)
- `EntityBase`(Id, 생성/수정 일시·자), `CodeNameBase`(Code, Name, Enabled) 공통 베이스.
- 제네릭 `CrudController<TEntity, TDto>` + `IRepository<T>` → 목록/조회/생성/수정/삭제 표준.
- 엔티티별 컨트롤러는 상속 후 라우트만: `class ItemController : CrudController<Item> {}`.
- 페이징·정렬·필터 쿼리스트링 공통 규약.

### 프론트 CRUD 엔진
- `resources.tsx`에 `{ title, path, columns[] }` 블록 등록 → 사이드바·라우트·그리드·폼 자동.
- 컬럼 헬퍼: `col.text/number/code/date/ref(resource)/enabled`.

---

## 4. 도메인 모델 — 모듈별 엔티티 (⚙️ 순수CRUD / 🔧 로직)

### 4.1 기초코드
- ⚙️ 공통코드그룹/공통코드 · 사업장·공장 · 부서
- 🔧 사용자 / 역할·권한 (인증 연계)
- ⚙️ 거래처(고객/공급사) · 품목분류 · 품목 · 단위/단위환산 · 창고/로케이션 · 공정 · 작업장·라인 · 설비 · 불량코드 · 검사항목/기준
- 🔧 BOM/BOM상세 · 라우팅(품목별 공정순서)

### 4.2 공유 커널 — 재고/수불 🔧 (모든 트랜잭션의 등뼈)
- 재고(Stock: 품목×창고×LOT) · 재고수불(StockTransaction: 입고·출고·투입·실적·조정 원장)

### 4.3 영업
- ⚙️ 판매단가 · 매출 / 🔧 수주·상세(등록→확정→출하→마감) · 출하지시 · 출하(→재고 출고)

### 4.4 구매 (영업과 대칭)
- ⚙️ 구매단가·매입 / 🔧 발주·상세 · 입고(→재고 입고)

### 4.5 생산 🔧
- 생산계획 · 작업지시 · 자재투입(BOM 소요전개) · 작업실적(양품/불량) · 재공 · LOT추적
- ℹ️ 설비 실시간 수집은 **DX(D:\Work\DX)** 경로 (EasyFactory 아님). 범위 밖, 후순위.

### 4.6 품질 🔧
- 수입/공정/출하검사 · 검사성적서(항목별 측정·판정) · 부적합 · 판정(합격/불합격/특채)

---

## 5. 단계별 로드맵

### Phase 0 — 스캐폴드 (⭐ 최우선)
목표: **엔티티 1개를 DB→API→화면까지 관통** + **멀티테넌시 동작**.
- [ ] 솔루션 구조(backend 5프로젝트 + web Vite)
- [ ] docker-compose PostgreSQL
- [ ] Catalog/Tenant DbContext + 테넌트 해석 미들웨어 + 마이그레이터 툴
- [ ] 인증(JWT 쿠키 + CSRF) + 로그인(테넌트+계정) + admin 시드
- [ ] 제네릭 `CrudController<T>` + `IRepository<T>`
- [ ] OpenAPI → `npm run gen:api`
- [ ] 프론트 CrudPage + resources.tsx + AppLayout + Mantine 테마
- [ ] **관통 검증**: `modus_t_demo`에 공통코드 화면 CRUD E2E
- **완료 정의**: "설정 1개 추가 → 새 화면 뜬다" + "테넌트별 DB 격리 확인".

### Phase 1 — 기초코드 (대량 생성) · Phase 2 — 영업+구매+재고수불 · Phase 3 — 생산 · Phase 4 — 품질

### 이후 확장
설비 실시간 수집(DX), 대시보드/리포트, 견적/PR, 원가, 다국어 등.

---

## 6. 확정된 결정
1. UI: **Mantine** ✅
2. 멀티테넌시: **DB-per-tenant, Phase 0부터** ✅
3. 형상관리: **git, github.com/vitaminaddiction/Modus (public)** ✅ — 시크릿 gitignore 필수
4. 인증: 단순 로그인+역할로 시작, 세밀 권한 후순위 ✅
