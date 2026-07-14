# Lab 01 — 專案建置 Project Setup

**時長:** 約 80 分鐘
**目標:** 使用 Claude Code 建立一個全端 CMS 應用程式(Angular + .NET Core + MS SQL) — 完全不需親手寫程式碼。

---

## Reference 參考

| Placeholder | 說明 |
|-------------|---------|
| `{COURSE_ROOT}` | 課程教材資料夾(例如 c:\course\ai-full-stack) |
| `{PROJECT_ROOT}` | 你的專案工作目錄(例如 c:\dev\cms) |

開始前,請在 **`{PROJECT_ROOT}` 開啟一個 PowerShell 終端機**。

---

## Step 0 — 複製設定檔

建立你的專案資料夾,然後把提供的設定檔複製進去。這些檔案讓 Claude 取得產生應用程式所需的資料庫
schema 與編碼慣例。

```powershell
New-Item -ItemType Directory -Path "{PROJECT_ROOT}"
Copy-Item -Path "{COURSE_ROOT}\labs\lab01\setup\*" -Destination "{PROJECT_ROOT}\" -Recurse
```

你的專案資料夾現在應該包含:

```
{PROJECT_ROOT}\
  database\
    auth.sql
    course.sql
    admin.sql    
    ... (其他 schema 檔案)
  spec\
    code-gen.convention.md
    ui-sample-list.png
    ui-sample-view.png
    ui-sample-edit.png
    ui-sample-add.png
```

---

## Step 1 — 建置全端應用程式骨架

在 `{PROJECT_ROOT}` 的終端機中啟動 Claude Code:

```powershell
claude
```

然後輸入以下提示:

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

**Claude 將會:**
- 讀取資料庫 schema 與編碼慣例
- 在 `src\CMS.API\` 建置一個 .NET Core 9 Web API 專案,啟用 Swagger/OpenAPI,
  並在 `appsettings.json` 中寫入 `SQLEXPRESS` 連線字串
- 在 `src\CMS.NG\` 建置一個 Angular 20 應用程式
- 產生完整的 AppRole CRUD 功能(含篩選的清單、檢視、編輯、新增)
- 把 AppRole 接入側邊欄導覽
- 在 `src\CMS.API.Tests\` 建置一個 xUnit 測試專案並加入 AppRole API 測試
- 為 AppRole 元件與服務加入 Angular 單元測試(Karma + Jasmine)

**完成後驗證:**
- `src\CMS.API\` — 存在 .NET 解決方案與專案檔
- `src\CMS.NG\` — 存在 Angular 專案
- AppRole 元件位於 `src\CMS.NG\src\app\features\app-roles\` 之下
- 後端可建置:`cd src\CMS.API && dotnet build`
- 後端測試通過:`cd src\CMS.API.Tests && dotnet test`
- Swagger UI 可在 `http://localhost:5000/swagger` 載入,並列出 AppRole 端點
- 前端可啟動:`cd src\CMS.NG && ng serve`
- 前端測試通過:`cd src\CMS.NG && ng test --watch=false`

> **遇到錯誤?讓 Claude Code 修。** 若任何命令失敗 — 例如建置、`ng serve`、`npm start` 等 —
> **複製完整的錯誤訊息並貼進 Claude Code**,請它排除問題。Claude 能讀取錯誤、診斷原因並套用修正。
>
> 舉例來說,`npm start` 可能因為 PowerShell 執行原則停用了指令碼執行而失敗:
>
> ```
> npm : 因為這個系統上已停用指令碼執行,所以無法載入 C:\Program Files\nodejs\npm.ps1 檔案。如需詳細資訊,請參閱
> about_Execution_Policies,網址為 https:/go.microsoft.com/fwlink/?LinkID=135170。
> 位於 線路:1 字元:1
> + npm start
> + ~~~
>     + CategoryInfo          : SecurityError: (:) [], PSSecurityException
>     + FullyQualifiedErrorId : UnauthorizedAccess
> ```
>
> 把它貼進 Claude Code — 它會辨識出指令碼執行被停用的原則,並引導你完成修正
>(例如 `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned`)。

---

## Step 2 — 撰寫 CLAUDE.md

請 Claude 記錄剛才建置的內容:

---

```
Document updates in CLAUDE.md
```

---

**Claude 將會:**
- 檢視產生的程式碼庫
- 在 `{PROJECT_ROOT}` 撰寫 `CLAUDE.md`,記錄專案結構、技術堆疊、編碼慣例與模式,供未來的工作階段使用

**完成後驗證:**
- 專案根目錄存在 `CLAUDE.md`
- 它描述了技術堆疊、資料夾結構與關鍵慣例

---

## Step 3 — 自訂側邊欄選單樣式

依你的需求微調側邊欄(例如比照 Ultima PrimeNG 設計):

---

```
The sidebar menu UI style, refers to https://ultima.primeng.org/dashboards/analytics
```

---

> **注意:** 更多可作為樣式參考的 PrimeNG 範本可在 <https://primeng.org/templates> 取得。

**Claude 將會:**
- 瀏覽 Ultima analytics 儀表板作為視覺參考
- 更新 Angular 側邊欄樣式以比照之

**完成後驗證:**
- 執行中的應用程式側邊欄反映了更新後的樣式
- AppRole 清單頁面沒有出現退步(regression)

---

## Step 4 — 推送到 GitHub

把你的專案納入版本控制並發佈到 GitHub。

**首先**,在 GitHub 上建立一個全新的**空** repository(不要 README、不要 `.gitignore`、不要 license),位於:

```
https://github.com/USERNAME/REPO.git
```

**接著**,在 `{PROJECT_ROOT}` 的終端機中驗證 GitHub CLI(每台機器一次):

```powershell
gh auth login
```

依提示操作(GitHub.com → HTTPS → 透過瀏覽器驗證)。

**最後**,在 Claude Code 中輸入以下提示 — 把 `USERNAME/REPO` 換成你的 repo:

---

```
init git and push to https://github.com/USERNAME/REPO.git , an empty repo
```

---

**Claude 將會:**
- 在 `{PROJECT_ROOT}` 初始化一個 git repository(`git init`)
- 加入合理的 `.gitignore`(建置輸出、`bin/`、`obj/`、`node_modules/` 等)
- 暫存並提交產生的專案
- 加入遠端並推送到你的空 GitHub repo

**完成後驗證:**
- `git status` 是乾淨的(工作樹已提交)
- `git remote -v` 顯示你的 `origin` 指向 `https://github.com/USERNAME/REPO.git`
- 你的程式碼在 GitHub 上可見:`https://github.com/USERNAME/REPO`

> **注意 — repo 不是空的?** 如果你不小心建立 repo 時**帶有** README、`.gitignore` 或 license,
> 第一次推送會被拒絕(`! [rejected] ... fetch first`)。把遠端的歷史併進你的歷史,然後再次推送:
>
> ```powershell
> git pull --rebase origin main
> git push -u origin main
> ```
>
>(若你的預設分支是 `master`,請以它取代 `main`。)

---

## Step 5 — 建立 `develop` 分支並設為預設分支

本課程的日常工作都在 **`develop`** 分支上進行;`main` 保持乾淨,作為發行分支。從 `main` 建立
`develop`,發佈它,並在 GitHub 上把它設為 repo 的**預設分支**,讓新的 clone 與 PR 自動以它為目標。

在 Claude Code 中輸入以下提示:

---

```
create a develop branch based on main, push it, and set develop as the default branch on GitHub
```

---

**Claude 將會:**
- 從 `main` 在本機建立分支(`git switch -c develop main`)並切換過去
- 以上游追蹤推送(`git push -u origin develop`)
- 透過 GitHub CLI 把它設為 repository 的預設分支
  (`gh repo edit --default-branch develop` — 使用 Step 4 的 `gh auth login`)

**完成後驗證:**
- `git branch` 顯示你**位於 `develop`**
- `git branch -r` 列出 `origin/develop`
- 在 GitHub 上,repo 的分支下拉選單顯示 **`develop` 為預設分支**
  (或從終端機確認:`gh repo view --json defaultBranchRef`)

> **注意:** 從這裡開始,課程假設你在 `develop`(或以它為基礎的功能分支)上工作 —
> `main` 只有在有東西發行時才會移動。

---

## Lab Checkpoint 實作檢查點

| # | 項目 | 完成? |
|---|------|-------|
| 0 | 設定檔已複製(`database\`、`spec\`) | ☐ |
| 1 | `src\CMS.API\` — .NET Core 9 Web API 可建置 | ☐ |
| 1 | Swagger UI 可在 `http://localhost:5000/swagger` 載入 | ☐ |
| 1 | `src\CMS.NG\` — Angular 20 應用程式可啟動 | ☐ |
| 1 | AppRole CRUD 可運作(清單、檢視、編輯、新增) | ☐ |
| 2 | 專案根目錄有記錄慣例的 `CLAUDE.md` | ☐ |
| 3 | 側邊欄樣式已比照 Ultima PrimeNG analytics | ☐ |
| 4 | 專案已推送到 GitHub(`origin` 已設定,程式碼在 GitHub 上可見) | ☐ |
| 5 | `develop` 分支已從 `main` 建立、推送,並設為 GitHub 預設分支 | ☐ |
