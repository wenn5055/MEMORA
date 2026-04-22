# MEMORA — VRChat World Project

> A collaborative memory-sharing VR experience built with Unity 2022.3.22f1 and VRChat SDK.  
> Explore nostalgic environments, catch fireflies, ride a guided vehicle, and annotate your world with friends.

---

## Table of Contents

- [Project Overview](#project-overview)
- [Project Architecture](#project-architecture)
- [Dependencies & Setup](#dependencies--setup)
  - [VRChat Dependency Recovery](#vrchat-dependency-recovery)
  - [Git LFS Setup](#️-important-git-lfs-setup)
  - [ClientSim (Local Testing)](#clientsim-local-testing)
  - [VRChat (Live Testing)](#vrchat-live-testing)
- [Entering the Testing World](#entering-the-testing-world)
- [Feature Testing Guide](#feature-testing-guide)

---

## Project Overview

MEMORA is a multi-user VRChat world that reconstructs shared memories through interactive environments. Players explore a series of themed scenes — a campsite, a playground, a firefly meadow, and a car ride — using annotation tools and social features to collaboratively revisit and discuss their experiences.

---

## Project Architecture

### Scene Structure

The project is organized into **four numbered memory environments**, each as an independent Unity scene:

| # | Scene | Description |
|---|-------|-------------|
| 1 | **Neutral Camp Site** (`Campsite.unity`) | Control condition — a neutral outdoor campsite with no specific memory cue. Serves as the baseline environment. |
| 2 | **Playground** (`Playground Final.unity`, `Demo.unity`) | Memory Environment 1 — a low-poly playground scene with a ghost prop and custom terrain/materials. |
| 3 | **Fireflies** (`Fireflies.unity`) | Memory Environment 2 — a nature scene where players catch fireflies using a player-owned catch box mechanic. |
| 4 | **Car Scene** (`CarScene`) | Memory Environment 3 — a guided vehicle ride along a predefined route through the environment. |

A standalone **Tools Scene** (`Tools.unity`) hosts and tests all interactive annotation tools in isolation.

---

### Script Architecture

All runtime logic is written in **UdonSharp** (C# compiled to VRChat's Udon bytecode). Scripts are divided into two layers:

#### Top-Level Scripts (`Assets/Scripts/`)

| Script | Role |
|--------|------|
| `CarVehicleController.cs` | Core vehicle controller. Handles both **Manual** (player-driven) and **AutoRoute** (waypoint-following) drive modes. Manages seat occupancy, speed, steering, ground-snapping, headlights, and network ownership. |
| `CarAutoRouteStarter.cs` | UI/interaction proxy that allows the instance owner or master to trigger the auto-route sequence. |
| `CarSeatStation.cs` | Wraps a `VRCStation` to track which player is seated, relaying enter/exit events back to `CarVehicleController`. |
| `CarSeatHandleInteractable.cs` | Interactable handle on each seat — allows players to enter or exit a seat via the VRChat interaction ray. |
| `LaserPointer.cs` / `LaserPointerNew.cs` | Annotation tool: casts a visible laser beam from the player's hand for pointing at objects of interest. |
| `PhaseTeleportTrigger.cs` | Trigger volume that teleports players between scene phases (environment transitions). |

#### Udon Scripts (`Assets/Scripts/Udon/`)

| Script | Role |
|--------|------|
| `FireflyCatchBoxController.cs` | **Player Object** — each player owns their own catch box. Detects firefly collisions, tracks catch count, and synchronizes state via UdonSharp serialization. |
| `FireflyCatchBoxSpawner.cs` | Spawns and assigns a `FireflyCatchBox` prefab to the local player on world join. |
| `RailingFirefliesController.cs` | Controls ambient fireflies that follow a railing/path, providing the atmospheric background glow in Scene 3. |

---

### Annotation Tools

Three annotation tools are available to all players in-world:

| Tool | Behaviour |
|------|-----------|
| **Pen** | Draw persistent ink strokes in 3D space on any surface. |
| **Eraser** | Remove previously drawn strokes. |
| **Laser Pointer** | Project a visible laser beam for pointing without leaving marks. |

These tools are spawned from the **Tools Scene** prefab set and appear in front of the player on world load.

---

### Key Design Patterns

- **VRC Player Object pattern** — The firefly catch box is instantiated per-player, ensuring individual ownership and avoiding network contention.
- **UdonSharp serialization** — `[UdonSynced]` fields on `CarVehicleController` propagate vehicle state (speed, route index, engine state) to all clients.
- **Smooth presentation rig** — A separate `rideRig` Transform interpolates the vehicle's visual position for non-owner clients, hiding network jitter.
- **Ownership gating** — Only the instance owner or master may drive the auto-route vehicle, enforced in `OnOwnershipRequest`.
- **Git LFS** — Large binary assets (`.blend`, `.glb`, `.fbx`) are tracked via Git Large File Storage to stay within GitHub's 100 MB file limit.

---

### Asset & Package Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `com.vrchat.worlds` | VCC-managed | VRChat World SDK & UdonSharp |
| `com.vrchat.base` | VCC-managed | VRChat core runtime |
| `com.unity.render-pipelines.universal` | 14.0.10 | Universal Render Pipeline (URP) |
| `com.unity.render-pipelines.high-definition` | 14.0.10 | HDRP (available but URP is primary) |
| `com.unity.shadergraph` | 14.0.10 | Custom shader authoring |
| `com.unity.textmeshpro` | 3.0.6 | UI text rendering |
| `com.unity.cloud.gltfast` | 6.14.1 | Runtime GLB/GLTF import |
| `com.unity.postprocessing` | 3.4.0 | Post-processing effects |
| Third-party: **51+ LowPolyTrees** | — | Environment foliage assets |
| Third-party: **Shaded Spectrum** | — | Additional shader/material assets |

---

## Dependencies & Setup

For the full onboarding and troubleshooting flow, see [docs/VRCHAT_SETUP.md](docs/VRCHAT_SETUP.md).

### Prerequisites

- **Unity 2022.3.22f1** -- you must use this exact version
- **VRChat Creator Companion (VCC)** -- download at [vrchat.com/home/download](https://vrchat.com/home/download)
- **Git LFS** -- must be installed before cloning

### VRChat Dependency Recovery

This repository keeps the VRChat package intent in `Packages/vpm-manifest.json`.

If you open the project before VCC restores the Worlds SDK, Unity will report missing `UdonSharp` and `VRC.*` namespaces across many scripts. That usually means the VRChat packages were not restored into your local checkout yet.

Use this recovery order:

1. Install Git LFS, clone the repo, and run `git lfs pull`.
2. Open the repository through **VRChat Creator Companion** as an existing project.
3. Let VCC finish restoring the VRChat Worlds SDK packages.
4. Open the project in **Unity 2022.3.22f1**.
5. If needed, use `Tools/MEMORA/Restore VRChat Dependencies` in Unity to trigger the embedded resolver again.

### Required Tools

- **Unity 2022.3.22f1** — you must use this exact version
- **VRChat Creator Companion (VCC)** — download at [vrchat.com/home/download](https://vrchat.com/home/download)
- **Git LFS** — must be installed before cloning

---

### ⚠️ IMPORTANT: Git LFS Setup

Large files (`.blend`, `.glb`, `.fbx`) are stored in Git LFS.  
**You must set up Git LFS before cloning; otherwise large files will be corrupted or missing in Unity.**

#### First Time Setup

```bash
# Step 1 — Install Git LFS (only needed once per machine)
git lfs install

# Step 2 — Clone the repository
git clone https://github.com/wenn5055/MEMORA.git

# Step 3 — Enter the project folder
cd MEMORA

# Step 4 — Download all LFS files (.blend, .glb, .fbx)
git lfs pull
```

#### Already Cloned (Pulling Latest Changes)

```bash
git pull
git lfs pull
```

---

### ClientSim (Local Testing)

ClientSim allows you to test the VRChat world directly inside the Unity Editor without publishing.

1. **Download and install Unity Editor `2022.3.22f1`**  
   → [Unity Archive](https://unity.com/releases/editor/archive)

2. **Download the VRChat Creator Companion (VCC)**  
   → [vrchat.com/home/download](https://vrchat.com/home/download)

3. Open VCC and add or open the cloned repository as an existing project.

4. Click **Open Project** — VCC will install all required VRChat SDK packages automatically.

5. Wait for VCC to finish restoring the VRChat SDK packages before troubleshooting any `UdonSharp` compile errors.

6. Once the project opens in Unity, open any scene from `Assets/Scenes/` and press **Play** to run a local ClientSim session.

---

### VRChat (Live Testing)

To test the world in the actual VRChat platform, you need a VRChat account and the VRChat application.

**Please set up a VRChat account** at [vrchat.com](https://vrchat.com) before proceeding.

---

## Entering the Testing World

### Solo Entry

1. Download **VRChat** on your Quest headset and sign in. You will be brought to the lobby.
2. On **PC or Mobile**, sign in to VRChat and navigate to: 
**Onboarding** 👉 https://vrchat.com/home/launch?worldId=wrld_67469b8f-8351-4a19-9437-dc30ec89d183

**Playground** 👉 https://vrchat.com/home/launch?worldId=wrld_b3bbe088-43f7-4fa1-8a69-ab07305b1af4

**Car Scene** 👉 https://vrchat.com/home/launch?worldId=wrld_0360b628-d57e-495f-9590-3077b8283360

**Fireflies** 👉 https://vrchat.com/home/launch?worldId=wrld_694f76f9-0ef7-4aab-8cc2-445aa9c4d1d3

3. Click **"Invite Me"** on that page.
4. Back in VRChat, press **Y** on the left controller to open the Menu — you should see a new notification.
5. Select the notification tab; you should see an invite from yourself.
6. **Accept the invite** to enter the playtest world.

---

### Recommended — Testing with Peers

> Testing with at least one other person is strongly recommended to evaluate the social and annotation features.

1. **Befriend your peers** in VRChat:  
   Menu → **Social** tab → **User Search** → search their username → send a friend request.

2. **Player A** enters the world first by following Steps 1–6 above.

3. **Other players** join via:  
   Menu → **Social** tab → **All Friends** → Player A's profile → **Join**

---

## Feature Testing Guide

### Annotation Tools

After entering the world, you will find **one set of annotation tools** placed in front of you.

| Tool | How to Use |
|------|------------|
| **Pen** | Pick up and draw strokes on surfaces |
| **Eraser** | Pick up and erase existing strokes |
| **Laser Pointer** | Pick up and point at objects of interest |

Interact and play around freely with all three tools. We welcome feedback on:
- **Intuitiveness** — is each tool easy to understand and pick up?
- **Usability** — does the tool behave as expected?
- **Any other observations** or suggestions for improvement.

---

### Emoji (Peer Setting Only)

> This feature requires at least two players in the same world instance.

1. **Long-press B** on the right controller to bring up the interaction menu.
2. Use the **right thumbstick** to navigate:  
   `Emoji & Stickers` → `Emoji` → `Default Emoji`  
   *(Closing and reopening this menu will return you to this specific page.)*
3. Have a chat with your friends and send some emojis!

As this is a **VRChat built-in feature**, we are primarily looking for feedback on:
- The **intuitiveness** of navigating the emoji menu.
- Any **friction or annoyances** that arise during use.
- Suggestions to help us refine the **onboarding tutorial** for new users.

---

*Thank you for playtesting MEMORA! Your feedback is invaluable in helping us create a more intuitive and memorable experience.*

Note: Passenger-view vehicle jitter in `CarScene` currently appears to be a VRChat-side limitation related to moving `VRCStation` / synced vehicle behavior rather than a local route-tuning issue. Driver-side smoothing and route logic can still be improved, but the remaining seated passenger jitter should be treated as a platform-side constraint for now.
