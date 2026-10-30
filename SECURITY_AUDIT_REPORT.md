# SECURITY_AUDIT_REPORT — AchievHub + SteamSync

**Date:** 2026-10-30 (pre-remediation baseline)  
**Repos:** Audit-only then fix Critical → High → Medium. Auth decision: **hybrid** (keep password+JWT; add Steam OpenID 2.0).  
**Scope:** `achiev-hub` + `steam-sync`.

---

## Executive summary

The application authenticates with **local Steam ID + password → JWT**, not Steam OpenID. The highest demonstrated risks are: **Steam Web API key committed in both repositories**, **unscoped CRUD IDOR** on user and library tables, **unbound `steamId` query parameters** that burn Steam quota and proxy third-party profiles, **JWT stored in `localStorage`**, and **worker jobs that do not bind `UserId` to `SteamId`**. No automated security tests existed in achiev-hub at audit start.

| Severity | Open (pre-fix) |
|----------|------------------|
| Critical | 2 |
| High | 4 |
| Medium | 4 |
| Low | 2 |
| Suspected | 1 |

---

## Findings table (by severity)

| ID | Sev | Title | Status |
|----|-----|-------|--------|
| F01 | CRÍTICO | Steam Web API key in tracked config | Open → fix |
| F02 | CRÍTICO | IDOR on `/api/users*` and related CRUDs | Open → fix |
| F03 | ALTO | No Steam OpenID / no proof of Steam ownership for guest | Open → fix (hybrid OpenID) |
| F04 | ALTO | Steam proxy `steamId` not bound to JWT | Open → fix |
| F05 | ALTO | JWT in `localStorage` | Open → fix |
| F06 | ALTO | Worker sync without UserId↔SteamId binding | Open → fix |
| F07 | MÉDIO | No rate limiting on auth / Steam routes | Open → fix |
| F08 | MÉDIO | Anon provisioning status enumeration | Open → fix |
| F09 | MÉDIO | Admin role never enforced | Open → fix (with IDOR) |
| F10 | MÉDIO | Missing HSTS / CSP / security headers | Open → fix |
| F11 | BAIXO | Seed resets passwords every startup | Open → fix |
| F12 | BAIXO | Weak/default JWT & DB credentials in sample config | Partial (with F01) |
| S01 | Suspeita | Query logging / APM PII leakage | Requires runtime config validation |

---

## Detailed findings

### [CRÍTICO] F01 — Steam Web API key committed in repository

- **Local:** `achiev-hub.Server/appsettings.json:9-10`; `achiev-hub.Server/appsettings.Development.json`; `steam-sync/src/SteamSync.Worker/appsettings.json:11-13`
- **Categoria:** OWASP A02:2021 / CWE-798
- **Evidência:** `"ApiKey": "5CFB****83B5"` (full value present in git-tracked files)
- **Como explorar:** Clone repo → extract key → call Steam Web API as the publisher until quota/deny list
- **Impacto:** Abuse of publisher quota; possible temporary IP deny list; any SteamID public-data access allowed by the key
- **Correção:** Remove literals; env/user-secrets only; fail-fast if missing in non-Development; **human must rotate key in Steamworks**
- **Esforço:** P
- **Referência:** [Steamworks Web API documentation — API keys](https://partner.steamgames.com/doc/webapi_overview/auth)

### [CRÍTICO] F02 — IDOR on user and library CRUD APIs

- **Local:** `UsersController.cs:22-92`; `UsersGamesController.cs`; `UsersAchievementsController.cs`; `GoalAchievementsController.cs`
- **Categoria:** OWASP A01:2021 / CWE-639
- **Evidência:** `[Authorize]` only; `GetById`/`Update`/`Delete` without comparing resource owner to `NameIdentifier`
- **Como explorar:** Login as any user → `GET /api/users` → `DELETE /api/users/{victimId}` or `PUT` change victim email/SteamId/password
- **Impacto:** Full account takeover / data wipe of other users’ libraries and goals links
- **Correção:** Self-or-admin authorization; force `UserId` from token on link tables; catalog mutations admin-only
- **Esforço:** M

### [ALTO] F03 — Steam OpenID absent; guest JWT without ownership proof

- **Local:** No OpenID code; `AuthenticationController.cs:58-74`; `JwtTokenService.GenerateGuestToken`
- **Categoria:** OWASP A07:2021 / CWE-287
- **Evidência:** Zero matches for `openid` / `check_authentication`; guest accepts any SteamId string
- **Como explorar:** `POST /api/guest` with victim SteamId → call Steam-backed endpoints as that identity context
- **Impacto:** Impersonation of Steam context for API proxying; no cryptographic proof of Steam account control
- **Correção:** Implement OpenID 2.0 hybrid with `check_authentication`; restrict guest
- **Esforço:** G

### [ALTO] F04 — Unbound `steamId` on Steam proxy endpoints

- **Local:** `SteamGamesController.cs:38-55`, `:84-115`; `SteamPlayersController.cs`
- **Categoria:** OWASP A01:2021 / CWE-639
- **Evidência:** Query `steamId` passed to Steam client without equality check to JWT `steam_id`
- **Como explorar:** Authenticated request `GET /api/steam/games/recent?steamId=VICTIM` (public profiles)
- **Impacto:** Privacy scrape of public Steam data via app key; quota exhaustion / 429 / deny list risk
- **Correção:** Use claim only; reject mismatch
- **Esforço:** P

### [ALTO] F05 — JWT stored in localStorage

- **Local:** `achiev-hub.client/src/lib/session.js:3-4,16-48`
- **Categoria:** OWASP A07:2021 / CWE-922
- **Evidência:** `localStorage.setItem(TOKEN_KEY, accessToken)`
- **Como explorar:** Any XSS → `localStorage.getItem('achiev-hub-token')` → replay API
- **Impacto:** Session hijack until expiry/revocation
- **Correção:** HttpOnly Secure SameSite cookie; stop persisting token in JS storage
- **Esforço:** M

### [ALTO] F06 — Worker accepts mismatched UserId/SteamId

- **Local:** `steam-sync/.../SteamSyncService.cs:42-57`
- **Categoria:** OWASP A01:2021 / CWE-639
- **Evidência:** Loads user by `userId` only; fetches Steam data with message `steamId` without equality
- **Como explorar:** Publish forged RabbitMQ job `{UserId: victim, SteamId: attackerOrOther}` if broker reachable
- **Impacto:** Overwrite victim library with another Steam library; Steam API abuse
- **Correção:** Require `user.SteamId == steamId` before Steam calls
- **Esforço:** P

### [MÉDIO] F07 — No global rate limiting

- **Local:** `Program.cs` (no `AddRateLimiter`)
- **Categoria:** OWASP A04:2021 / CWE-307
- **Evidência:** Only domain cooldowns (email resend, sync)
- **Impacto:** Credential stuffing; Steam quota burn
- **Correção:** Rate limit login/guest/register/OpenID/Steam routes
- **Esforço:** P

### [MÉDIO] F08 — Anonymous register status disclosure

- **Local:** `RegistrationController.cs:37-54`
- **Categoria:** OWASP A01:2021 / CWE-200
- **Evidência:** Returns NotFound vs Ok with sync metadata by `steamId`
- **Impacto:** Account/enumeration of registered SteamIds and sync state
- **Correção:** Require auth or uniform response
- **Esforço:** P

### [MÉDIO] F09 — Admin role unused

- **Local:** `DbSeeder.cs:20`; no `[Authorize(Roles="admin")]` in controllers
- **Categoria:** OWASP A01:2021 / CWE-285
- **Impacto:** Privileged role provides no server-side privilege separation
- **Correção:** Enforce on list-all and catalog mutations
- **Esforço:** P

### [MÉDIO] F10 — Missing security headers

- **Local:** `Program.cs:162-177`
- **Categoria:** OWASP A05:2021 / CWE-693
- **Evidência:** `UseHttpsRedirection` only; no HSTS/CSP/nosniff
- **Impacto:** Weaker XSS/clickjacking defenses for SPA host
- **Correção:** Add headers middleware; tighten `AllowedHosts` in production samples
- **Esforço:** P

### [BAIXO] F11 — Seed overwrites passwords every boot

- **Local:** `DbSeeder.cs:43-48`
- **Impacto:** In shared non-prod DBs, password changes reset to `achiev****`
- **Correção:** Seed create-only; Development-only

### [BAIXO] F12 — Default credentials in tracked config

- Covered with F01 remediation for JWT; document DB/RabbitMQ defaults as local-dev only.

### [Suspeita] S01 — ORM/APM log PII

- No redaction middleware found. Confirm production log level and that connection strings / Steam URLs with `key=` are not logged. Needs runtime validation.

---

## Checklist A–I (pre-remediation)

Legend: **C** conforme · **NC** não conforme · **NA** não aplicável · **S** suspeita

### A. Steam OpenID 2.0

| Item | Status | Evidence |
|------|--------|----------|
| `check_authentication` POST | NC | No OpenID implementation |
| `openid.mode == id_res` | NC | — |
| `op_endpoint` exact Steam URL | NC | — |
| `claimed_id` prefix + SteamID64 regex | NC | — |
| `return_to` exact match | NC | — |
| `response_nonce` anti-replay | NC | — |
| App `state` CSRF binding | NC | — |
| Cannot forge callback params | NC | No callback |
| Never asks for Steam password | C | Local app password only; README documents Steam ID + app password |
| Link by immutable SteamID64 | C | `User.SteamId` is SteamID64 string |
| Re-link takeover protection | NC | `UpdateUserRequest.SteamId` writable via IDOR |
| Admin 2FA independent of Steam | NC | No 2FA |

### B. Session / tokens

| Item | Status | Evidence |
|------|--------|----------|
| Cookie HttpOnly/Secure/SameSite | NA | Auth via Bearer header, not cookie (pre-fix) |
| No token in localStorage | NC | `session.js:40` |
| Session ID rotation on login | NA/partial | New JWT each login; no server session store |
| Absolute + idle timeout | Partial | JWT `exp` ~30 min; no idle sliding server-side |
| Logout invalidates server-side | C | `TokenVersion` bump + `OnTokenValidated` |
| JWT alg fixed / iss/aud/exp | C | `Program.cs:90-99` HS256 symmetric; validates issuer/audience/lifetime |
| Signing secret ≥256-bit distinct | NC | Weak/dev key committed |
| Invalidate on email/password change | S | Not verified end-to-end |

### C. Authorization

| Item | Status | Evidence |
|------|--------|----------|
| IDOR filtered by owner | NC | Users* CRUDs; Goals OK |
| Role checks server-side | NC | Admin unused |
| Admin endpoints protected | NC | — |
| Mass assignment allowlist | Partial | DTOs; Role not client-set on create; SteamId/Email/Password updatable via IDOR |
| Horizontal escalation | NC | F02/F04 |
| 403/404 generic | Partial | Mix of NotFound messages |

### D. Injection / SSRF

| Item | Status | Evidence |
|------|--------|----------|
| SQL parameterized | C | EF Core LINQ |
| Command injection | C | No `Process.Start` with user input found |
| SSRF on image fetch | NA | Client loads Steam CDN URLs; server uses fixed Steam API hosts |
| Path traversal uploads | NA | No user file upload feature found |
| Insecure deserialization | C | No pickle/yaml.load/node-serialize |
| SSTI | C | Vue + JSON APIs |
| XXE | NA | No XML user parsing found |
| Prototype pollution | NA | .NET backend |
| Open redirect | NA (pre-OpenID) | — |
| ReDoS | S | Limited regex usage; not exhaustively fuzzed |

### E. CSRF / CORS / headers

| Item | Status | Evidence |
|------|--------|----------|
| CSRF on state-changing | Partial | Bearer-from-header mitigates classic CSRF; cookie migration needs SameSite+token design |
| Origin/Referer checks | NC | — |
| CORS `*` + credentials | C | No CORS middleware (same-origin SPA proxy) |
| HSTS | NC | — |
| CSP | NC | — |
| nosniff / Referrer / Permissions / Frame | NC | — |
| XSS sinks | C | No `v-html` / `dangerouslySetInnerHTML` found |
| eval/dynamic Function | C | Not found in app src |

### F. Data leak prevention

| Item | Status | Evidence |
|------|--------|----------|
| DTO allowlists | Partial | `UserDto` exposes email; list-all IDOR |
| Generic client errors | Partial | `HandleException`; unhandled may leak |
| Log redaction | S | No middleware |
| Telemetry PII | S | OTel in worker; labels unknown |
| Tokens encrypted at rest | NA | No third-party OAuth refresh tokens stored |
| DB encryption / backups | S | Ops |
| Data minimization | Partial | Collects email + SteamId needed for product |
| Deletion / LGPD art.18 | NC | No account self-delete/export flow documented |
| Export isolation | NA | No export feature |
| Storage buckets | NA | — |
| Secrets in query strings | NC | Steam `key=` in URL query (Steam API requirement; avoid logging URIs) |
| Cache-Control no-store auth | NC | Not set |
| LGPD incident plan | NC | Not in repo |

### G. Secrets / config

| Item | Status | Evidence |
|------|--------|----------|
| No secrets in repo/history | NC | F01 |
| `.env` gitignored + example | Partial | `.env` ignored; `.env.example` empty key only |
| Env-separated secrets | NC | Same committed key |
| Front-end public env review | C | No secret VITE_ vars |
| Steam key server-only | Partial | Server-only usage C; committed NC |
| Debug endpoints off in prod | Partial | OpenAPI Dev-only |
| DB least privilege | S | Uses default postgres superuser in samples |
| Redis auth | NA | No Redis |

### H. Abuse resilience

| Item | Status | Evidence |
|------|--------|----------|
| Rate limit login/callback/expensive | NC | F07 |
| Per-IP and per-account limits | NC | — |
| Uniform user enumeration | NC | F08 |
| External timeouts / CB | Partial | Worker has resilience handler; hub `HttpClient` basic |
| Steam response cache | Partial | Some memory cache cooldowns |
| Handle 429 / eresult 25/84 | S | Needs code confirmation per client |
| Payload limits | Partial | ASP.NET defaults |
| CAPTCHA | NA | Optional; not present |

### I. Dependencies / supply chain

| Item | Status | Evidence |
|------|--------|----------|
| Known vulns scan | S | Not run at baseline; run in remediation |
| Lockfile | C | `package-lock.json`; NuGet restore |
| Abandoned critical deps | S | Manual review |
| Postinstall scripts | S | npm audit needed |
| Container non-root / RO FS | S | Review Dockerfiles |
| CI actions pinned by SHA | NA | No workflows found |

---

## Risks accepted (conscious)

- Steam Web API requires `key` as query parameter (platform constraint); mitigate by never logging full request URIs.
- RabbitMQ message HMAC deferred if broker remains on private authenticated network (document ops requirement).
- Admin MFA independent of Steam deferred to product decision.

## Human decisions required

1. Rotate Steam Web API key in Steamworks; configure IP allowlist / method permissions.
2. Approve git history purge (BFG/filter-repo) after rotation.
3. LGPD retention periods and incident-response contacts.
4. Whether guest mode remains after OpenID (remediation will heavily restrict it).
5. Production `AllowedHosts` and cookie `__Host-` deployment (HTTPS-only).

---

---

## Post-remediation addendum (2026-10-30)

### Status after Phase 4

| Sev | Open Critical/High | Notes |
|-----|--------------------|-------|
| Critical | **0** | F01 removed from tracked configs (history still contains key — rotate); F02 ownership/admin enforced |
| High | **0** | F03 OpenID hybrid with `check_authentication`; F04 steamId bound; F05 HttpOnly cookie; F06 worker binding |
| Medium | 0 remaining of F07–F10 as filed | Rate limits, register/status auth, admin roles, HSTS/CSP/headers |
| Low | Partial | Seed Dev-only create-if-missing; JWT placeholder only in Development appsettings |

### Checklist A (OpenID) — post-fix

| Item | Status |
|------|--------|
| `check_authentication` | C |
| `openid.mode == id_res` | C |
| `op_endpoint` exact | C |
| `claimed_id` + SteamID64 regex | C |
| `return_to` exact (with state) | C |
| `response_nonce` anti-replay | C |
| App state CSRF cookie + cache | C |
| Forge callback without Steam | C (fails check_authentication) |
| No Steam password phishing | C |
| Link by SteamID64 | C |
| Re-link takeover | C (conflict if owned; non-admin cannot change SteamId via Users PUT) |
| Admin 2FA independent of Steam | NC — **human decision pending** |

### Test suites

- `achiev-hub.Server.Tests`: 18 passed
- `SteamSync.Worker.Tests`: 4 passed
- `gitleaks`: not installed on this machine (manual key grep: tracked appsettings empty; gitignored `*.local.json` still hold the old key locally)

### Remaining human actions

1. **Rotate** Steam Web API key `5CFB****83B5` in Steamworks (still in git history and local gitignored files).
2. Configure Steam key IP allowlist / method permissions.
3. Optionally purge git history (BFG/filter-repo) after rotation.
4. Decide admin MFA and guest-mode retirement.
5. Set production `SteamOpenId:ReturnTo`/`Realm` and `Jwt:Key` via secrets manager.
6. LGPD retention / incident contacts.
7. Install gitleaks/TruffleHog in CI.

### Conscious residual risks

- RabbitMQ job authenticity still trusts the broker network (no HMAC).
- `AllowedHosts: *` remains for local/Docker convenience — tighten per environment for production.
- npm advisories on transitive frontend packages (Vue/Vite toolchain) reported by JS SDK during build — track separately.
