---
name: sync-design-docs
description: Use when the user asks to sync, refresh, import, update, or compare the designer-maintained design documents for 九宫牌局.
---

# Sync Design Docs

Use the root script. Do not manually copy folders.

## Workflow

1. Run from repository root:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File ".\sync-design-docs.ps1"
```

2. Read the latest index report:

```powershell
docs/design-sync/latest-sync-report.md
```

3. Open the per-run diff report path named inside that index under `docs/design-sync/runs/`.

4. Inspect scoped git changes:

```powershell
git status --short -- "Assets/Docs" "docs/design-sync/latest-sync-report.md" "docs/design-sync/runs"
git diff --name-status -- "Assets/Docs" "docs/design-sync/latest-sync-report.md" "docs/design-sync/runs"
git diff -- "Assets/Docs" "docs/design-sync/latest-sync-report.md" "docs/design-sync/runs"
```

5. Summarize added, updated, deleted, and notable content changes for the user.

6. Stop before changing gameplay code, infrastructure, scenes, prefabs, or assets unless the user explicitly asks for follow-up implementation.

## Notes

- Source defaults to `C:\Users\jinji\Desktop\文档\MyNote\游戏开发项目\九宫牌局`.
- Target defaults to `Assets\Docs`.
- `docs/design-sync/latest-sync-report.md` is a small rolling index that points at the newest run report.
- Per-run reports are written under `docs/design-sync/runs/` so cross-thread handoff can reference a single immutable diff document.
- The script preserves retained Unity `.meta` files and deletes `.meta` files for removed documents.
- The script does not commit changes.
