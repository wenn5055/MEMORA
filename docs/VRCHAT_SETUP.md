# VRChat Setup and Dependency Recovery

This project uses VRChat packages through VPM. The source of truth for those packages is `Packages/vpm-manifest.json`.

`Packages/manifest.json` does not permanently store the full VRChat Worlds SDK set in the repo. Instead, VRChat Creator Companion and the embedded VPM resolver restore the required packages into your local working copy.

## Required Tools

- Unity `2022.3.22f1`
- VRChat Creator Companion
- Git LFS

## First-Time Setup

1. Install Git LFS once on your machine.
2. Clone the repository.
3. Run `git lfs pull` in the cloned repository.
4. Open VRChat Creator Companion.
5. Add or open the existing project folder at the repository root.
6. Let VCC restore the VRChat Worlds SDK packages before using Unity.
7. Open the project in Unity `2022.3.22f1`.

## How Dependency Restore Works

- `Packages/vpm-manifest.json` declares the expected VRChat packages for this project.
- `Packages/com.vrchat.core.vpm-resolver` is checked into the repo so Unity can trigger package restore locally.
- After restore succeeds, Unity should resolve packages such as `com.vrchat.base` and `com.vrchat.worlds`.
- `com.vrchat.worlds` supplies the UdonSharp types used across this project.

## If You See UdonSharp or VRC Namespace Errors

Symptoms usually look like:

- `The type or namespace name 'UdonSharp' could not be found`
- `The type or namespace name 'VRC' could not be found`
- Many scripts fail at once, especially under `Assets/Scripts/` and `Assets/Examples/PersistentPen/`

That usually means the VRChat Worlds SDK packages were not restored into this local checkout yet.

Use this recovery path:

1. Close Unity.
2. Re-open the repository through VRChat Creator Companion.
3. Let VCC finish restoring packages.
4. Re-open Unity `2022.3.22f1`.
5. If the project still reports missing VRChat packages, use `Tools/MEMORA/Restore VRChat Dependencies` inside Unity to trigger the embedded resolver again.

## Validation Checklist

After restore, verify all of the following:

- `Packages/manifest.json` now includes the required `com.vrchat.*` package entries for your local checkout
- Scripts using `UdonSharpBehaviour`, `VRC.SDKBase`, and `VRC.SDK3.*` compile cleanly
- Representative files like `Assets/Scripts/CarVehicleController.cs` and `Assets/Examples/PersistentPen/UdonPen.cs` no longer report missing namespace errors

## Repo Policy

- Keep `Packages/vpm-manifest.json` in source control
- Do not commit `Library/`
- Do not commit `Library/PackageCache`
- Do not vendor full `com.vrchat.*` package directories into git unless the team explicitly decides to manage licensing and distribution that way
