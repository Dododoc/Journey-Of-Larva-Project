<!-- Copilot/AI helper instructions for contributors and automated coding agents -->
# Journey-Of-Larva — AI coding instructions

Purpose: give AI coding agents concise, repo-specific guidance so suggested changes are safe, minimal, and aligned with how this Unity project is organized.

- **Big picture:** This is a Unity project (see `ProjectSettings/ProjectVersion.txt`) that stores scenes and engine assets in `Assets/` and package dependencies in `Packages/manifest.json`. Primary art/scene assets live under `Assets/` (for example `Assets/Scenes/SampleScene.unity`). The project uses Universal RP and 2D packages (`com.unity.render-pipelines.universal`, `com.unity.2d.*`).

- **When making changes:** Prefer small, focused edits. Avoid touching large binary assets (textures, .unity scenes) unless the change is required. Edits should keep the Unity project loadable with minimally changed ProjectSettings and Packages.

- **Build / run / test (discoverable facts & examples):**
  - Unity editor version string: `ProjectSettings/ProjectVersion.txt` (use the exact editor path/version found there).
  - Typical CLI patterns (replace `<UnityExe>` with the editor executable path):
    - Build (example pattern, replace method or flags as needed):
      `& '<UnityExe>' -projectPath '<repoRoot>' -batchmode -quit -buildTarget Win64 -logFile build.log`
    - Run Test Runner from CLI (uses Unity Test Framework present in `Packages`):
      `& '<UnityExe>' -projectPath '<repoRoot>' -runTests -testPlatform EditMode -batchmode -quit -logFile tests.log`
  - If adding custom build automation, add a clear `Assets/Editor/Build*` script and reference it in the instructions or CI.

- **Key files & directories to reference in changes:**
  - `Assets/Scenes/` — main scenes (e.g. `SampleScene.unity`).
  - `Assets/InputSystem_Actions.inputactions` — input mappings used by the Input System package.
  - `Packages/manifest.json` — package versions and dependencies; update via Unity Package Manager where possible.
  - `ProjectSettings/ProjectVersion.txt` — editor version to use when reproducing builds/tests.
  - `Assets/Settings/UniversalRP.asset` and `Assets/Renderer2D.asset` — render pipeline settings.

- **Project-specific patterns and constraints (discoverable):**
  - The repo is asset-heavy (Unity meta files present). Keep changes to `.meta` consistent when adding/removing assets.
  - No tracked C# runtime scripts were found at repository root at time of analysis — if you add scripts, place them under `Assets/Scripts/` or `Assets/Gameplay/` and keep namespaces consistent with the folder name.
  - Use the Unity Package Manager to update package versions rather than hand-editing `Packages/manifest.json`, unless the change is deliberate and documented.

- **PR & commit guidance for AI agents:**
  - Keep PRs scoped: one gameplay or editor change per PR and avoid unrelated binary file changes.
  - When code touches scenes or assets, include a short note explaining the asset change and the reason. For scene changes, include a quick manual verification checklist (open scene, play, confirm no console errors).

- **Examples of safe edits for automated agents:**
  - Fix a small UI text string inside a serializable ScriptableObject (if present) and update the associated `.meta`.
  - Add a small Editor script under `Assets/Editor/` that adds a menu command to run an existing build method.

- **What not to do (conservative rules):**
  - Do not reimport or modify large textures/models unless necessary.
  - Avoid changing ProjectSettings broadly (layers, quality settings) without human review.

If any part of this file is unclear or you want more/less strict rules (for example, explicit test-run scripts or a build CI snippet), say which area to expand and I will iterate.
