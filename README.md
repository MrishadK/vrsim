# 🛫 VRSIM — Virtual Reality Airport & Flight Simulation

[![Unity Version](https://img.shields.io/badge/Unity-2022.3%20LTS-blue.svg?logo=unity)](https://unity.com/)
[![Render Pipeline](https://img.shields.io/badge/URP-Universal%20Render%20Pipeline-informational.svg)](https://unity.com/srp/universal-render-pipeline)
[![XR Interaction Toolkit](https://img.shields.io/badge/XRI-3.1.2-green.svg)](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.1/)
[![Git LFS](https://img.shields.io/badge/Git%20LFS-Tracked-orange.svg)](https://git-lfs.github.com/)
[![License](https://img.shields.io/badge/License-MIT-lightgrey.svg)](LICENSE)

An immersive virtual reality airport experience and flight simulation built with **Unity 2022.3 LTS** and the **Universal Render Pipeline (URP)**. Explore an authentic international airport terminal, interact with ground crew and passengers, view detailed airliners at the gate, and step directly onto the tarmac in VR.

---

## 🌟 Key Features

### 🏢 International Terminal Architecture
- **Terminal Design & Layout**: Modeled fictional international airport terminal concourse with check-in desks, security screening zones, passport control counters, departure lounges, and boarding gates.
- **Glass Curtain Facades**: High-gloss, transparent curtain walls overlooking active aircraft ramp stands.
- **Telescopic Jetways**: Articulated passenger boarding bridges complete with pivoting rotundas, telescopic tunnel sections, and aircraft docking cabs.

### ✈️ High-Fidelity Aircraft
- **Airbus A350-900 NEO Concept**: Featuring animated Trent XWB turbofans, spinner hubs, realistic port/starboard navigation lights, and anti-collision strobes.
- **Boeing 737-800 & Boeing 747-400**: Modeled airframes with interiors, servicing gear, and authentic ramp placement.
- **Ramp Vehicles & GSE**: Passenger shuttle buses, baggage trolley trains, ground power units (GPU), and engine safety cones.

### 🛣️ Authentic ICAO / FAA Apron Markings
- High-contrast aviation-yellow taxiway lead-in centerlines with black contrast borders.
- Dedicated aircraft nosewheel stop bars for wide-body (`A350 / B787 / B777`) and narrow-body (`A320 / B737`) aircraft.
- Red aircraft safety clearance envelopes and jetway movement exclusion zones.
- Ground service equipment (GSE) staging bays and marked perimeter service roads with speed limit markings.

### 🌅 Dynamic Environment & Lighting
- **Time of Day System**: Smooth transitions from morning golden hour to high noon, sunset, and night.
- **Intelligent Lighting Rig**: Automatic activation of high-mast apron floodlights, terminal facade wash lights, and taxiway guidance lights as dusk approaches.
- **Weather Simulation**: Dynamic rainfall particle systems with dynamic road/tarmac wetness and reflection tuning.

### 🎧 Spatial 3D Audio
- Real air traffic control (ATC) radio chatter ambiance.
- Apron environmental background audio and rain audio.
- High-tech interactive UI sound effects.

### 👥 Living Terminal (AI Crowd Simulation)
- NavMesh-driven passengers and airport staff navigating check-in halls, security checkpoints, and gate lounges.
- Safe collision handling and seamless scene transitions.

---

## 🎮 Scenes

| Scene | Description |
|---|---|
| `Assets/Scenes/MainMenu.unity` | Live 3D rendered departure lounge vista overlooking Gate 2 with camera drift, parallax, time adjustments, and weather controls. |
| `Assets/Scenes/SampleScene.unity` | Full interactive airport terminal simulation featuring passenger crowds, VR exploration, and ramp operations. |

---

## 🚀 Getting Started

### Prerequisites

1. **Unity 2022.3 LTS** (tested on `2022.3.62f3` or newer).
2. **Git LFS** (Large File Storage) — **Essential** for 3D models and large textures:
   ```bash
   git lfs install
   ```
3. A compatible VR headset (Meta Quest 2/3/Pro via Link/AirLink, HTC Vive, Valve Index, or OpenXR PCVR), or standard desktop play mode with mouse and keyboard.

### Clone the Repository

Clone the repository with Git LFS enabled:

```bash
# Clone repository
git clone https://github.com/MrishadK/vrsim.git

# Navigate into project directory
cd vrsim

# Ensure all large binary assets are pulled
git lfs pull
```

### Opening in Unity

1. Launch **Unity Hub**.
2. Click **Add** > **Add project from disk**.
3. Select the cloned `vrsim` repository folder.
4. Open the project with **Unity 2022.3 LTS**.
5. When Unity finishes compiling, open `Assets/Scenes/MainMenu.unity` and click **Play**!

---

## 🛠️ Tech Stack & Packages

- **Engine**: Unity 2022.3.62f3 LTS
- **Render Pipeline**: Universal Render Pipeline (URP)
- **XR Stack**: Unity XR Interaction Toolkit (XRI 3.1.2), OpenXR Plugin, XR Plugin Management
- **Input**: Unity Input System Package (v1.7.0)
- **GUI & Text**: TextMeshPro
- **Navigation**: Unity NavMesh Agent System

---

## 📁 Repository Structure

```
vrsim/
├── Assets/
│   ├── A350NeoConcept/          # Airbus A350-900 NEO model and materials
│   ├── Audio/                   # High-fidelity ATC chatter, apron ambiance, and UI audio
│   ├── Editor/                  # Automated setup scripts (SetupMainMenu.cs)
│   ├── Prefabs/                 # Terminal components, jetways, lighting rigs, vehicles
│   ├── Scenes/
│   │   ├── MainMenu.unity       # Live 3D interactive main menu
│   │   └── SampleScene.unity    # Main airport simulation scene
│   ├── scripts/
│   │   ├── NPC/                 # Passenger AI, crowd spawner, and waypoints
│   │   ├── MainMenuController.cs
│   │   ├── MenuCinematicCameraController.cs
│   │   ├── TimeOfDayController.cs
│   │   ├── AirportWeatherController.cs
│   │   └── AirportSFXManager.cs
│   └── VRTemplateAssets/        # XR Interaction presets and controllers
├── Packages/                    # Unity Package Manager manifest and locks
├── ProjectSettings/             # Project quality, physics, and input configurations
├── .gitattributes               # Git LFS tracking rules (*.fbx, *.glb, *.psd, *.blend)
├── .gitignore                   # Comprehensive Unity cache ignore list
└── README.md
```

---

## 🤝 Contributing

Contributions, issues, and feature requests are welcome! Feel free to check the [issues page](https://github.com/MrishadK/vrsim/issues).

---

## 📄 License

This project is open-source and available under the [MIT License](LICENSE).
