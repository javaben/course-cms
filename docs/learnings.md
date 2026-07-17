# Learnings

Non-obvious things about this codebase, accumulated across sessions via gstack `/learn`.
Each entry is a trap someone already fell into — the kind of thing that isn't visible from
reading the code and that unit tests don't catch.

Confidence is the recording session's own calibration, not a guarantee. Verify line numbers
before relying on them.

> Security findings are **not** kept here. This repo is public; the live audit ledger stays in
> gitignored `.gstack/security-reports/`. Run `/cso` to regenerate it.

## Patterns

### `dapper-where-builder-is-safe` (9/10)

Repositories build SQL as `$"{SelectColumns}{whereClause}"`, which trips every SQL-injection
grep — roughly 25 hits, all false positives. Every interpolated fragment is a compile-time
constant and all user values bind through `DynamicParameters` or anonymous objects. No query
DTO exposes a sort/order/paging property, so `ORDER BY` cannot be tainted either.

Before flagging interpolated SQL here, read the `where.Add()` call sites.

Files: `src/CMS.API/Repositories/CourseRepository.cs`, `src/CMS.API/Repositories/AppUserRepository.cs`

## Pitfalls

### `course-freetext-holds-legacy-html` (9/10)

The Course `Objective` / `Target` / `Outline` `nvarchar(max)` columns hold legacy HTML — 18.5% of
rows, and about 10 rows hold a full pasted `<!DOCTYPE html>` document including a `<style>` block.
The Angular detail page renders it as markup, so the problem is invisible there.

Any consumer that draws these fields as a literal string — PDF, plain-text export, email — must
flatten them first via `CoursePdfService.ToPlainText`. Every test fixture uses clean prose, so this
whole bug class passes unit tests.

Files: `src/CMS.API/Services/CoursePdfService.cs`

## Architecture

### `authorize-roles-skips-default-policy` (9/10)

ASP.NET Core's `AuthorizationPolicy.CombineAsync` sets `useDefaultPolicy=false` as soon as an
`[Authorize]` attribute specifies `Roles`, `Policy`, or `AuthenticationSchemes`. So a requirement
registered on `options.DefaultPolicy` does **not** apply to `[Authorize(Roles="Admin")]` endpoints —
it only covers bare `[Authorize]`. `FallbackPolicy`, meanwhile, only covers endpoints carrying no
authorization attribute at all.

Neither hook covers every attribute shape. For a cross-cutting per-request gate that must apply
everywhere, use middleware between `UseAuthentication()` and `UseAuthorization()` and inspect
`GetEndpoint()?.Metadata`. Verified by probe against .NET 9.

Files: `src/CMS.API/Program.cs`, `src/CMS.API/Controllers/AuthController.cs`

### `file-download-needs-blob-fetch` (9/10)

The global auth `FallbackPolicy` means a file-download endpoint can't be a plain `<a href>` or
`window.open` — the JWT bearer interceptor only attaches the token to Angular `HttpClient` calls,
so a browser-initiated navigation arrives unauthenticated and 401s. Download with
`responseType: 'blob'` and hand the user an object URL.

Files: `src/CMS.API/Program.cs`, `src/CMS.NG/src/app/core/interceptors/auth.interceptor.ts`

## Tools

### `questpdf-license-revenue-gated` (8/10)

QuestPDF's Community license is free only for organizations under roughly US$1M revenue, for
nonprofits, and for FOSS. `LicenseType.Community` is an eligibility assertion, not merely a
configuration switch — confirm eligibility before adopting it in any commercial context.

## Operational

### `browse-file-url-sandbox` (8/10)

`browse goto file://` is sandboxed to the temp directory plus the repo root. To render a downloaded
artifact (e.g. `~/Downloads/course-N.pdf`), copy it into the temp directory first, then
`goto file:///...`. Chrome's PDF viewer renders it well enough to screenshot for verification.
