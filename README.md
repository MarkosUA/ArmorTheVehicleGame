# Armor the Vehicle

A small arcade shooter: you control a turret mounted on a car that drives forward on its own. Enemies wait idle along the road; as the car approaches, they chase it down and try to kamikaze-detonate on contact. Drive the length of the level, shoot down as many enemies as you can, and don't let the car run out of HP.

## Controls

- **Tap/click the screen** to start the run. The car doesn't move until the first tap.
- **Hold and drag** to aim the turret left/right; it auto-fires continuously while the run is active.
- Reaching the end of the level shows **"You win"**; running out of car HP shows **"You lose"**. Tap again to restart — the car resets to the start line and waits for the next tap.
- A pause button freezes the whole game (turret, camera, enemies) until resumed.

## Tech stack

- Unity **6000.3.24f1** (URP)
- **VContainer** for dependency injection (`GameLifetimeScope` is the single composition root)
- **uGUI** (Canvas-based) for all UI
- Unity's native `Awaitable`/`async`/`await` for delayed and sequenced logic (no coroutines)
- Enemy rig and animations sourced from **Mixamo**

## Project structure

All gameplay code lives under `Assets/Scripts/`, split by feature:

| Folder | Contains |
|---|---|
| `Core/` | `GameState` — the single source of truth for run/win/lose/pause — and `GameLifetimeScope`, the VContainer DI composition root |
| `Car/` | Player-controlled vehicle: touch input, automatic forward movement, win/lose triggers |
| `Turret/` | Turret aiming (touch-driven yaw + fixed pitch) and auto-fire/laser sighting; both gate on `GameState.IsPlayable` |
| `Enemy/` | `EnemyAI`'s Idle → Wandering → Chasing → Attacking → Dead state machine, its small collaborators (`EnemyAnimatorController`, `EnemyWanderer`, `EnemyHitFlash`), and the pooling `EnemySpawner` |
| `Projectile/` | Pooled bullets (`ProjectilePool` / `Projectile`) |
| `Combat/` | Shared `Health` component and `IDamageable` interface, used by both the car and enemies |
| `Level/` | `GroundTiler` — tiles the ground prefab end-to-end to cover the configured level length |
| `Audio/` | `AudioManager` — random background-music playlist plus gameplay SFX; every clip slot is optional and never errors if left empty |
| `CameraRig/` | `ChaseCameraFollow` — smoothed behind-the-car chase camera |
| `UI/` | HUD, pause menu, win/lose overlay, and small shared helpers (`CanvasGroupFader`, `AppQuit`) |
| `Config/` | `LevelConfig`, the single tunables asset (see below) |

Non-code assets follow the same split: `Assets/Prefabs/` (gameplay objects) and `Assets/Prefabs/UI/` (UI panels/buttons), `Assets/Models/`, `Assets/Materials/`, `Assets/Animations/`, `Assets/Textures/`.

## Configuration

`Assets/Configs/LevelConfig.asset` is the one `ScriptableObject` holding every tunable value — level length, enemy count and stats, car stats, turret rotation speed, weapon fire rate/damage, and more. Balance changes belong there rather than as literals scattered through the code.

## Architecture notes

- **DI, not singletons**: every gameplay `MonoBehaviour` that needs `GameState` (and optionally `AudioManager`) gets it injected via VContainer, wired once in `GameLifetimeScope`.
- **One pause gate**: `GameState.IsPlayable` (`IsRunning && !IsPaused`) is the single condition every system checks before reacting to input or ticking — the turret, camera, and enemies all freeze and resume together.
- **Pooling over churn**: both enemies (`EnemySpawner`) and projectiles (`ProjectilePool`) reuse existing instances across a run/restart instead of destroying and re-instantiating.
- **Enemies are kamikaze**: there's no ranged enemy attack — an enemy that reaches the car detonates on contact, dealing damage once and then dying itself after its attack animation finishes.

## Running the project

1. Open the project in Unity **6000.3.x**.
2. Open `Assets/Scenes/SampleScene.unity`.
3. Press Play, then tap the Game view to start the run.
