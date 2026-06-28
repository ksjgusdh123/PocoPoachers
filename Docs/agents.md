# AI Agent Guide

## Layout

`PocoPoachers/` Unity client · `Server/` (`Server.sln`) · `Docs/` documentation · `Tools/` generators (when present)

## Rules

- Use real repository paths only—not worktrees or temporary copies.
- Inspect existing code first; match its style; keep diffs scoped to the task.
- Prefer clear OOP; consider runtime cost in Unity gameplay and server hot paths.
- **Ask the user before applying code** (confirm scope and approach).
- Ask before broad refactors, destructive operations, or edits to generated assets.
- Do not revert unrelated user changes.

## Unity

- Edit under `PocoPoachers/Assets/`; keep `.meta` files paired with assets.
- Do not edit `Library/`, `Temp/`, `Logs/`, `.vs/`, `UserSettings/`, or hand-edit `.csproj`/`.sln`/`.slnx` unless required.

## Server & generators

- Build/test `Server/Server.sln` when changing server code.
- Sync CSV/fbs changes through generators (`Docs/development/code-generators.md`); do not hand-edit generated output.

## Verification

- Unity: prefer an editor compile or existing build workflow.
- If verification cannot run locally, say so in the response.

## Documentation

Task-based doc index: [README.md](README.md). If docs and code disagree, **code wins**.

### Token efficiency rules

Follow these when creating or editing any doc:

- **Entry point is short** — `README.md` contains only the index table; detail lives in linked files.
- **One fact, one place** — never repeat information across documents; link instead.
- **Tables and code blocks over prose** — structured data reads faster and with fewer tokens.
- **One document per task** — if a file mixes topics, the AI must read all of it for any single task.
- **Why and Where, not What** — code tells *what* it does; docs tell *why* it exists and *where* to look.
- **Scope line at top** — each doc starts with one sentence stating what it covers and what it does *not* cover, so the AI can skip it confidently when irrelevant.
