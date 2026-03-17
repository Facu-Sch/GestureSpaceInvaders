# GestureSpaceInvaders 👾🤚

A Space Invaders for Meta Quest controlled entirely through XR microgestures.


<img width="327.5" height="361" alt="image" src="https://github.com/user-attachments/assets/b61e9709-e661-4198-9051-807b171d5edb" />

## 🎯 Overview

Built in the downtime between two larger projects to keep microgesture skills fresh and test one specific thing: tying the game canvas to the left hand throughout a full session. The canvas stays on a sphere around the player's eye so it always faces you, and its movement scales with depth, the farther the panel, the more it moves per unit of hand displacement, so it always ends up where your hand suggests it should be.

Everything else is straightforward Space Invaders.

## ✨ Features

- **Floating world space canvas:** The entire game lives on a repositionable 2D panel in your XR environment
- **Hand-tethered tracking:** Toggle the canvas to follow your left hand movement scales with depth so it always feels proportional regardless of how far the panel is
- **Dual-hand microgesture controls:** Left hand manages the game world, right hand controls the ship
- **Dynamic difficulty:** Enemies speed up as their numbers drop
- **Keyboard fallback:** Full PC/editor support for testing without the headset

## 🕹️ Gesture Controls

### Left Hand — World

| Gesture | Action |
|---|---|
| Swipe Up | Start game |
| Swipe Down | Restart *(game over only)* |
| Thumb Tap | Tether / untether canvas to hand |

### Right Hand — Ship

| Gesture | Action |
|---|---|
| Swipe Left / Right | Move ship |
| Swipe Down | Stop ship |
| Thumb Tap | Shoot |

### Keyboard (PC / Editor)

| Key | Action |
|---|---|
| Enter | Start game |
| Arrow Left / Right | Move ship |
| Space | Shoot |

## 🛠️ Technologies

- **Unity:** 6000.0.3f1
- **Meta XR SDK:** `com.meta.xr.sdk.all`
- **Platform:** Meta Quest 3
- **Language:** C#

## 📋 Requirements

- Unity 6000.0.3f1 or higher
- Meta XR All-in-One SDK
- Meta Quest 3 with hand tracking enabled

## 🚀 Setup

**1. Clone the repository**
```bash
git clone https://github.com/Facu-Sch/GestureSpaceInvaders.git
cd GestureSpaceInvaders
```

**2. Open in Unity**
- Open Unity Hub
- Add project from disk
- Select the cloned folder
- Ensure Unity 6000.0.3f1+ is installed

**3. Install Meta XR SDK** *(if not already installed)*
- Open Package Manager (`Window > Package Manager`)
- Add package from git URL: `com.meta.xr.sdk.all`

**4. Configure Build Settings**
- Open the "SampleScene"
- `File > Build Settings`
- Switch platform to Android
- `Player Settings > XR Plug-in Management` → Enable Oculus
- Add the "SampleScene" to the build

**5. Run**
- Connect Meta Quest 3 via USB with Developer Mode enabled
- Press Play in the Unity Editor, or build and deploy to the headset

## 📂 Project Structure

```
Assets/
├── Scripts/
│   ├── XRGameController.cs    # Microgesture input, canvas placement & hand tethering
│   ├── GameManager.cs         # Game state, start/end/reset
│   ├── EnemyGrid.cs           # Enemy spawning, movement, dynamic difficulty
│   ├── Player.cs              # Ship movement and shooting
│   ├── Bullet.cs              # Projectile logic
│   ├── AudioManager.cs        # Simultaneous audio playback
│   └── GameSpaceScaler.cs     # Runtime canvas scaling
└── Scenes/
    └── SampleScene.unity
```

## 🎓 Development Context

Made at GTI (FIUNER) while waiting on feedback from other projects. Two things to verify: that Meta XR microgestures were still fresh, and whether continuous hand-tethered canvas tracking was viable for a full game session without feeling broken. It worked out, the depth-proportional movement ended up being the key piece to make it feel consistent.

## 🗺️ Roadmap

- [ ] IMU-based control (replace or complement microgestures with inertial input from the headset)
- [ ] Keep keyboard support alongside IMU controls

## 🤝 Contributing

Contributions, issues, and feature requests are welcome!

## 👨‍💻 Author

**Facundo Nahuel Schneider**

- LinkedIn: [linkedin.com/in/facundo-schneider](https://linkedin.com/in/facundo-schneider)
- Email: facundoschneider5@gmail.com
- University: FIUNER — Immersive Technologies Group
