# Echoes of the Mind 👁️‍🕳️  
*A VR psychological horror prototype with adaptive fear, voice input, and AI-driven ghosts.*

---

## Table of Contents

- [Overview](#overview)
- [Narrative & Gameplay Loop](#narrative--gameplay-loop)
- [Core Features](#core-features)
  - [Fear & Jumpscare System](#fear--jumpscare-system)
  - [Lighting & Fuse Box](#lighting--fuse-box)
  - [Voodoo Dolls & Hell Door](#voodoo-dolls--hell-door)
  - [Ghost AI (FSM)](#ghost-ai-fsm)
  - [Voice Input & Scream Detection](#voice-input--scream-detection)
  - [UI & Player Feedback](#ui--player-feedback)
- [Technical Overview](#technical-overview)
- [How to Run the Project](#how-to-run-the-project)
- [Gameplay Videos](#gameplay-videos)
- [Playtesting & Evaluation](#playtesting--evaluation)
- [Limitations & Future Work](#limitations--future-work)
- [Research Context & Dissertation](#research-context--dissertation)
- [Credits & Asset Attributions](#credits--asset-attributions)

---

## Overview

**Echoes of the Mind** is a single-player VR psychological horror game built in Unity for the Meta Quest platform.  
The project explores how **adaptive systems** – fear tracking, dynamic ghost AI, and **voice-based scream detection** – can make horror feel less scripted and more personal.

Key ideas:

- The game reacts to how you **play** (staying in darkness, moving, using light).
- The ghost reacts to how you **sound** (talking, shouting, screaming into the mic).
- Progression is framed through **symbolic rituals**: burning cursed voodoo dolls in a Hell Door to weaken a demonic presence.

This repository contains:

- The full Unity project for the prototype.
- The final dissertation paper (`Dissertation Paper (240406999).pdf`).
- A short in-engine escape clip (`Final-Escape.mp4`).
- Draft design documentation.

---

## Narrative & Gameplay Loop

You play as **Sha**, a police officer investigating the disappearance of a friend inside the abandoned **Ravenswood Asylum**.  
Rumours speak of a former patient who performed occult rituals, leaving behind **cursed voodoo dolls** and a demonic entity bound to the building.

### Core Loop

Each run follows a repeated ritual loop:

1. **Enter the asylum** and restore power via the **fuse box**.
2. **Find a cursed voodoo doll** – guided by escalating whispers.
3. Once picked up, the **ghost begins a hunt** with increased aggression.
4. A **Hell Door** spawns at a random location. Reach it while being chased.
5. **Throw the doll into the Hell Door** to destroy it.
   - The ghost is briefly banished.
   - Lights reset; fear and tension ease slightly.
   - The next doll is spawned.
6. With each destroyed doll:
   - The ghost’s **movement speed increases by 10%**.
   - When fewer than three dolls remain, the ghost also gains **periodic teleportation** to keep pressure high.
7. After the final doll:
   - An **endless hunt** begins with all ghost abilities active.
   - Survive long enough for a glowing **exit door** to appear and escape.

Failure occurs if the ghost catches you – often triggered by high fear, darkness, or a badly timed scream.

---

## Core Features

### Fear & Jumpscare System

- The game tracks a hidden **Fear Level** from **0–100**, influenced mainly by:
  - Time spent in **darkness**.
  - Exposure to specific **haunted zones** and events.
- Fear is split into three tiers:
  - **Tier 1 (≥ 33)** – subtle audio and visual disturbances.
  - **Tier 2 (≥ 66)** – heavy jumpscares and stronger disturbances.
  - **Tier 3 (≥ 85)** – full **ghost hunts** are triggered.
- A dedicated `FearManager`:
  - Continually updates fear based on lighting and triggers.
  - Notifies the `JumpscareManager` and `GhostAIController` when thresholds are crossed.
- Jumpscares:
  - Spawn short-lived ghost apparitions near the player.
  - Use post-processing glitches, camera shake, and scream SFX.
  - Are gated with cooldowns so the game feels tense, not spammy.

### Lighting & Fuse Box

- **Flashlight**:
  - Narrow cone of light controlled by `TorchLightController`.
  - Creates a small “bubble of safety” while leaving most of the asylum in shadow.
- **Environmental lights**:
  - Controlled by `RoomLightController` components.
  - Can be turned back on via a **fuse box interaction** (`FuseBoxController` + `FuseBoxLever`).
- **Room zones**:
  - `RoomZoneTrigger` volumes tell the `FearManager` whether the player is currently in light or darkness.
  - Staying in the dark accelerates fear and can invite jumpscares and hunts.

### Voodoo Dolls & Hell Door

- The `DollManager` spawns **one active voodoo doll at a time** in the environment.
- Picking up a doll (`DollPickup`):
  - Immediately pushes the ghost into **aggressive hunt mode**.
  - Intensifies whispering and ambient audio.
- The `HellManager` then spawns a **Hell Door** at a random location:
  - Marked with red light and distorted visuals.
  - The player must reach it and **throw the doll inside** to complete the ritual.
- Each destroyed doll:
  - Increases ghost speed by **10%**.
  - Unlocks **teleportation** behaviour later in the run.
- The ritual system is both:
  - A **mechanical progression** system.
  - A **symbolic narrative device** around cleansing cursed artefacts.

### Ghost AI (FSM)

The ghost is driven by a **Finite State Machine** in `GhostAIController.cs`, balancing clarity and unpredictability.

Core states:

- **Patrolling**  
  - Uses Unity **NavMesh** + **NavMesh Links** to wander the asylum.
  - Chooses random destinations on walkable surfaces.
- **ChasingPlayer**  
  - Triggered when:
    - The player enters the ghost’s **field of view** (raycasts / distance checks).
    - The player is within a close range (~3.5 units).
  - Ghost continually follows the player using NavMesh navigation.
- **HearingPlayer**  
  - Triggered by voice events (see below).
  - Normal loud talking pulls the ghost **towards the sound source**.
  - Screams can cause an immediate **TeleportVeryCloseToPlayer()**.

Additional behaviour:

- **Blinking**:  
  The ghost briefly disables its `SkinnedMeshRenderer`, “vanishing” without stopping navigation, then reappears – adding disorientation.
- **Lost sight logic**:  
  If the ghost loses line of sight for more than **5 seconds**, it returns to **Patrolling** to avoid endless chasing and to feel more believable.

### Voice Input & Scream Detection

The **Scream Detection** system connects your real-world voice directly to game events.

- Implemented in the `ScreamDetector` script:
  - Captures microphone input every frame.
  - Computes loudness using **RMS (root mean square)**.
- Two thresholds:
  - **Talk Threshold** – normal loud speech.
  - **Scream Threshold** – intense, sudden loudness.
- Events:
  - **OnLoudTalk** – ghost moves towards the player’s current position.
  - **OnScream** – ghost immediately **teleports very close to the player**, often triggering a chase or death.
- Cooldowns:
  - Talking: ~0.5s
  - Screaming: ~2s  
  These prevent constant re-triggering and keep jumpscares impactful.

The system is **event-driven** and decoupled from the ghost’s FSM, making it easy to reuse for other reactive systems (e.g. environment reacting to noise).

### UI & Player Feedback

- Minimal **diegetic UI** to keep immersion:
  - A glowing **magical book** appears in the world and provides cryptic hints (implemented with world-space canvases).
- **Screen overlays** (via `ScreenOverlayController`):
  - Intro text, game-over screens, and ending messages.
  - Occasional flashes during jumpscares or fear spikes.
- Combined with audio, whispers, and lighting changes, these systems communicate state without heavy HUD elements.

---

## Technical Overview

- **Engine:** Unity (URP, PC/VR build)  
- **Platform:** Meta Quest / Oculus (prototype tested on Quest hardware)  
- **Language:** C#  
- **Key Systems / Scripts:**
  - `GameDirector` – central coordinator for managers, state, and high-level flow.
  - `FearManager` – tracks fear value and triggers thresholds.
  - `JumpscareManager` – spawns and coordinates jumpscares.
  - `GhostAIController` – FSM logic, patrolling, chasing, teleporting, blinking.
  - `ScreamDetector` – microphone input, talk/scream thresholds, events.
  - `DollManager`, `DollPickup`, `HellManager` – ritual loop.
  - `TorchLightController`, `RoomLightController`, `FuseBoxController` – lighting and power systems.
  - `ScreenOverlayController` – intro/outro text, game-over, and overlays.

#### Repository Layout (high level)

- `Assets/` – Unity project assets, scenes, scripts, prefabs, shaders.
- `Packages/` – Unity package manifest and dependencies.
- `ProjectSettings/` – Unity project configuration.
- Root files:
  - `Dissertation Paper (240406999).pdf` – full academic write-up.
  - `Final-Escape.mp4` – short climax / escape clip.
  - `Game Design Document Draft - Mohammad Inshaul Haque (1).docx`
  - `Asset-Links.docx` – asset references.

---

## How to Run the Project

> ⚠️ Note: Exact setup may vary slightly depending on your Unity + XR plugin versions.

1. **Clone or download** this repository  
   ```bash
   git clone https://github.com/Inshaul/EchoesOfTheMind.git
