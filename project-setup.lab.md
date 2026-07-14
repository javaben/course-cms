# Lab 01 — Project Setup

**Duration:** ~80 minutes
**Objective:** Scaffold a full-stack CMS application (Angular + .NET Core + MS SQL) using Claude Code — zero hand-written code.

---

## Reference

| Placeholder | Meaning |
|-------------|---------|
| `{COURSE_ROOT}` | Course materials folder (e.g., c:\course\ai-full-stack) |
| `{PROJECT_ROOT}` | Your project working directory (e.g., c:\dev\cms) |

Open a **PowerShell terminal at `{PROJECT_ROOT}`** before proceeding.

---

## Step 0 — Copy Setup Files

Create your project folder, then copy the provided setup files into it. These give Claude the database schema and coding conventions it needs to generate the app.

```powershell
New-Item -ItemType Directory -Path "{PROJECT_ROOT}"
Copy-Item -Path "{COURSE_ROOT}\labs\lab01\setup\*" -Destination "{PROJECT_ROOT}\" -Recurse
```

Your project folder should now contain:

```
{PROJECT_ROOT}\
  database\
    auth.sql
    course.sql
    admin.sql    
    ... (other schema files)
  spec\
    code-gen.convention.md
    ui-sample-list.png
    ui-sample-view.png
    ui-sample-edit.png
    ui-sample-add.png
```

---

## Step 1 — Scaffold the Full-Stack App

Launch Claude Code in your terminal at `{PROJECT_ROOT}`:

```powershell
claude
```

Then enter the following prompt:

---

```
Create/Initialize a full-stack app based on existing database schema
 - Database Schema: `.\database\*.sql`
- Coding Convention: `.\spec\code-gen.convention.md`
- UI Convention (for style references only, not actual content): `.\spec\ui-sample-*.png`

- Source Code Root: `.\src\`
- Backend CMS.API: .NET Core 9 Web API
    - Dapper (no Entity Framework)
    - Async API
    - Connection String:
      "Server=.\\SQLEXPRESS;Database=CMS;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False"
    - Add Swagger (OpenAPI) support ("Swashbuckle.AspNetCore" Version="7.2.0"), UI enabled at `/swagger`
	- Support CORS on localhost
    - Local run port: 5000
    - Add an xUnit test project at `.\src\CMS.API.Tests\` (reference the API project)

- Frontend CMS.NG: Angular 20, standalone components
    - Use PrimeNG for UI controls
    - Local run port: 4200
	- Do not proxy API requests, use environment.ts files
	- Add short-hand paths compiler Options for environment files
    - Keep the default Angular unit test setup (Karma + Jasmine, `ng test`)

- First feature: CRUD AppRole
  - Database Schema: `.\database\auth.sql`
  - List (query filter), View, Edit, Add
  - Sidebar Menu: `系統管理 Admin\角色 AppRole`
  - Add tests on both sides for this feature:
      - Backend: xUnit tests covering the AppRole API endpoints (list/filter, view, add, edit)
      - Frontend: Angular unit tests for the AppRole components and data service
```

---

**What Claude will do:**
- Read the database schema and coding conventions
- Scaffold a .NET Core 9 Web API project at `src\CMS.API\`, with Swagger/OpenAPI enabled
  and the `SQLEXPRESS` connection string in `appsettings.json`
- Scaffold an Angular 20 app at `src\CMS.NG\`
- Generate a full AppRole CRUD feature (list with filter, view, edit, add)
- Wire AppRole into the sidebar navigation
- Scaffold an xUnit test project at `src\CMS.API.Tests\` and add AppRole API tests
- Add Angular unit tests (Karma + Jasmine) for the AppRole components and service

**Verify when done:**
- `src\CMS.API\` — .NET solution and project files present
- `src\CMS.NG\` — Angular project present
- AppRole components exist under `src\CMS.NG\src\app\features\app-roles\`
- Backend builds: `cd src\CMS.API && dotnet build`
- Backend tests pass: `cd src\CMS.API.Tests && dotnet test`
- Swagger UI loads at `http://localhost:5000/swagger` and lists the AppRole endpoints
- Frontend serves: `cd src\CMS.NG && ng serve`
- Frontend tests pass: `cd src\CMS.NG && ng test --watch=false`

> **Hit an error? Let Claude Code fix it.** If any command fails — during the build, `ng serve`,
> `npm start`, etc. — **copy the full error message and paste it into Claude Code** and ask it
> to troubleshoot. Claude can read the error, diagnose the cause, and apply the fix.
>
> For example, `npm start` may fail with a PowerShell execution-policy error because script
> execution is disabled:
>
> ```
> npm : 因為這個系統上已停用指令碼執行，所以無法載入 C:\Program Files\nodejs\npm.ps1 檔案。如需詳細資訊，請參閱
> about_Execution_Policies，網址為 https:/go.microsoft.com/fwlink/?LinkID=135170。
> 位於 線路:1 字元:1
> + npm start
> + ~~~
>     + CategoryInfo          : SecurityError: (:) [], PSSecurityException
>     + FullyQualifiedErrorId : UnauthorizedAccess
> ```
>
> Paste it into Claude Code — it will identify the disabled-script-execution policy and walk
> you through the fix (e.g. `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned`).

---

## Step 2 — Document CLAUDE.md

Ask Claude to capture what was built:

---

```
Document updates in CLAUDE.md
```

---

**What Claude will do:**
- Inspect the generated codebase
- Write `CLAUDE.md` at `{PROJECT_ROOT}` documenting the project structure, tech stack, coding conventions, and patterns for future sessions

**Verify when done:**
- `CLAUDE.md` exists at project root
- It describes the stack, folder layout, and key conventions

---

## Step 3 — Customize Sidebar Menu Style

Refine the sidebar to fit your need (e.g., match the Ultima PrimeNG design):

---

```
The sidebar menu UI style, refers to https://ultima.primeng.org/dashboards/analytics
```

---

> **Note:** More PrimeNG templates to draw style references from are available at
> <https://primeng.org/templates>.

**What Claude will do:**
- Browse the Ultima analytics dashboard as a visual reference
- Update the Angular sidebar styles to match

**Verify when done:**
- Sidebar in the running app reflects the updated style
- No regressions on the AppRole list page

---

## Step 4 — Push to GitHub

Put your project under version control and publish it to GitHub.

**First**, create a new **empty** repository on GitHub (no README, no `.gitignore`, no license) at:

```
https://github.com/USERNAME/REPO.git
```

**Then**, authenticate the GitHub CLI from your terminal at `{PROJECT_ROOT}` (one-time per machine):

```powershell
gh auth login
```

Follow the prompts (GitHub.com → HTTPS → authenticate via browser).

**Finally**, in Claude Code, enter the following prompt — replace `USERNAME/REPO` with your repo:

---

```
init git and push to https://github.com/USERNAME/REPO.git , an empty repo
```

---

**What Claude will do:**
- Initialize a git repository at `{PROJECT_ROOT}` (`git init`)
- Add a sensible `.gitignore` (build output, `bin/`, `obj/`, `node_modules/`, etc.)
- Stage and commit the generated project
- Add the remote and push to your empty GitHub repo

**Verify when done:**
- `git status` is clean (working tree committed)
- `git remote -v` shows your `origin` pointing at `https://github.com/USERNAME/REPO.git`
- Your code is visible on GitHub at `https://github.com/USERNAME/REPO`

> **Note — repo isn't empty?** If you accidentally created the repo *with* a README,
> `.gitignore`, or license, the first push is rejected (`! [rejected] ... fetch first`).
> Reconcile the remote's history into yours, then push again:
>
> ```powershell
> git pull --rebase origin main
> git push -u origin main
> ```
>
> (If your default branch is `master`, substitute it for `main`.)

---

## Step 5 — Create a `develop` Branch and Make It the Default

Day-to-day work in this course happens on a **`develop`** branch; `main` stays clean as the
release branch. Create `develop` from `main`, publish it, and make it the repo's **default
branch** on GitHub so new clones and PRs target it automatically.

In Claude Code, enter the following prompt:

---

```
create a develop branch based on main, push it, and set develop as the default branch on GitHub
```

---

**What Claude will do:**
- Create the branch locally from `main` (`git switch -c develop main`) and check it out
- Push it with upstream tracking (`git push -u origin develop`)
- Set it as the repository's default branch via the GitHub CLI
  (`gh repo edit --default-branch develop` — uses the `gh auth login` from Step 4)

**Verify when done:**
- `git branch` shows you are **on `develop`**
- `git branch -r` lists `origin/develop`
- On GitHub, the repo's branch dropdown shows **`develop` as the default**
  (or check from the terminal: `gh repo view --json defaultBranchRef`)

> **Note:** from here on, the course assumes you work on `develop` (or feature branches
> based on it) — `main` only moves when something is released.

---

## Lab Checkpoint

| # | Item | Done? |
|---|------|-------|
| 0 | Setup files copied (`database\`, `spec\`) | ☐ |
| 1 | `src\CMS.API\` — .NET Core 9 Web API builds | ☐ |
| 1 | Swagger UI loads at `http://localhost:5000/swagger` | ☐ |
| 1 | `src\CMS.NG\` — Angular 20 app serves | ☐ |
| 1 | AppRole CRUD working (list, view, edit, add) | ☐ |
| 2 | `CLAUDE.md` at project root documenting conventions | ☐ |
| 3 | Sidebar styled to match Ultima PrimeNG analytics | ☐ |
| 4 | Project pushed to GitHub (`origin` set, code visible on GitHub) | ☐ |
| 5 | `develop` branch created from `main`, pushed, and set as the GitHub default | ☐ |
