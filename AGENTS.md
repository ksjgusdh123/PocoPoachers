# AGENTS.md

Guidance for Codex agents working in this repository.

## Project Layout

- `PocoPoachers/`: Unity client project.
- `Server/`: server-side solution and related projects.
- `Tools/`: generator and tooling projects.

## General Rules

- Work from the real repository paths, not copied or temporary paths.
- Before changing behavior, inspect the relevant existing code and follow its style.
- Keep changes scoped to the requested task.
- Do not revert user changes or unrelated local modifications.
- Prefer clear object-oriented design where it matches the surrounding code.
- Consider runtime performance, especially in Unity gameplay code and server hot paths.
- Ask before making broad architectural changes, destructive operations, or changes that affect generated assets.

## Unity Client

- Main Unity project path: `PocoPoachers/`.
- Edit source assets under `PocoPoachers/Assets/`.
- Avoid editing generated Unity folders such as `Library/`, `Temp/`, `Logs/`, `.vs/`, and `UserSettings/`.
- Keep `.meta` files paired with their assets when adding, moving, or deleting Unity assets.
- Avoid unnecessary changes to `ProjectSettings/` and `Packages/manifest.json`.
- Do not hand-edit generated `.csproj`, `.sln`, or `.slnx` files unless the task explicitly requires it.

## Server And Tools

- Server solution: `Server/Server.sln`.
- Keep protocol, packet, and table generator changes synchronized with generated outputs when applicable.
- Prefer using existing generator workflows over manually editing generated code.

## Verification

- For Unity C# changes, prefer a compile check through the Unity editor or existing project build workflow when available.
- For server changes, build or test the affected solution/project when practical.
- If a verification step cannot be run locally, state that clearly in the final response.
