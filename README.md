# MEMORA — VRChat World Project

A collaborative memory-sharing VR experience built with Unity 2022.3.22f1 and VRChat SDK.

---

### 1. Tools Scene
A dedicated scene (`Assets/Scenes/ToolsScene`) showcasing all interactive tools in a placeholder environment:
- **Pen** 
- **Eraser**
- **Laser Pointer** 

### 2. Main Scene
`Assets/Scenes/MainScene` now contains:
- **Control Condition** — Big Room environment (neutral space)
- **Memory Environment 1** — Playground scene (first memory environment)


### 3. Git LFS Setup
Large files (`.blend`, `.glb`, `.fbx`) are now tracked via Git LFS to avoid GitHub's 100MB file size limit.

---

## Getting Started


### Prerequisites
- Unity **2022.3.22f1** (must use this exact version)
- VRChat Creator Companion (VCC) — download at vrchat.com/home/download
- Git LFS is installed on your machine

---

## ⚠️ IMPORTANT: Git LFS Setup

Large files like `.blend`, `.glb`, and `.fbx` are stored in Git LFS.
**You must set up Git LFS before cloning, otherwise large files will be corrupted/missing in Unity.**

### First Time Setup

```bash
# Step 1 — Install Git LFS (only needed once per machine)
git lfs install

# Step 2 — Clone the repository
git clone https://github.com/wenn5055/MEMORA.git

# Step 3 — Enter project folder
cd MEMORA

# Step 4 — Download all LFS files (blend, glb, fbx)
git lfs pull
```

---

### Already Cloned (Pulling Latest Changes)

```bash
git pull
git lfs pull
```

