Markdown
# 🦝 PicoGK - The Raccoon Edition (Linux Native)

![Build Status](https://img.shields.io/badge/Build-Linux_x64-green) ![Platform](https://img.shields.io/badge/Platform-Ubuntu_24.04-orange) ![License](https://img.shields.io/badge/License-Apache_2.0-blue)

**Welcome to the Linux-native port of the PicoGK Computational Engineering Framework.**

While the official [LEAP 71 PicoGK](https://github.com/leap71/PicoGK) currently ships with Windows (.dll) and macOS (.dylib) runtimes, this **Raccoon Edition** provides the missing link: a fully compiled and tested **Linux Shared Object (.so)** runtime.

> *"Computational Engineering should run everywhere, from high-end workstations to headless Linux servers."*

---

## 🚀 Features

* **Native Linux Support**: Includes `libpicogk.1.7.so` compiled for Ubuntu 24.04 LTS (x64).
* **Headless Mode Ready**: Optimized for generating meshes on servers without a GUI/Viewer.
* **OpenVDB Power**: Full access to the voxel-based geometry kernel on Linux.

---

## 🛠️ Installation & Setup

### 1. System Dependencies (Ubuntu/Debian)
Before running, ensure your Linux machine has the required libraries to talk to the engine:

```bash
sudo apt update && sudo apt install -y \
  libopenvdb-dev \
  libtbb-dev \
  libglfw3-dev \
  libglew-dev \
  libxinerama-dev \
  libxcursor-dev \
  libxrandr-dev \
  libxi-dev
2. Clone this Repository
Bash
git clone [https://github.com/archiesgate42-glitch/PicoGK-Raccoon-Edition-.git](https://github.com/archiesgate42-glitch/PicoGK-Raccoon-Edition-.git)
cd PicoGK-Raccoon-Edition-
3. How to use in your C# Project
If you are building your own app, ensure your .csproj references the Linux runtime:

XML
<ItemGroup>
    <None Include="native/linux-x64/*.so" Pack="true" PackagePath="runtimes/linux-x64/native" />
</ItemGroup>
And ensure the libpicogk.1.7.so is copied to your output directory (bin/Debug/net10.0/).

🖥️ Headless Mode (Server/CLI)
Running on a server without a display? You can disable the graphical viewer in your Program.cs:

C#
Library.Go(0.5f, () => 
{
    // Your geometry code here
}, 
".", 
"PicoGK.log", 
false); // <--- Set this to 'false' to disable the GUI Viewer
🏗️ Build Info
Runtime Version: PicoGK 1.7

Compiler: GCC 13.3.0

OS: Ubuntu 24.04.3 LTS

Maintainer: Raccoontt & Taro-XI

This is a community fork and is not officially affiliated with LEAP 71, though we love their work!


***

### 🚀 Hoe zet je dit online?

1.  Maak het bestand aan:
    ```bash
    nano README.md
    ```
    *(Plak de tekst erin, Ctrl+O om op te slaan, Ctrl+X om te sluiten)*

2.  Push het naar GitHub:
    ```bash
    git add README.md
    git commit -m "docs: Added Raccoon Edition documentation"
    git push
    ```

    ---

## 🤝 Credits & AI Co-Creation

This "Raccoon Edition" is a product of close Human-AI collaboration, bridging the gap between C# and Linux Native C++.

* **Louis Janssens**: Lead Architect & Linux Implementation.
* **Taro-XI**: Virtual Co-Pilot & Voxel Logic Integration.
* **Gemini 3 Pro**: Technical Debugging, Compilation Support & Build Guide.

> *"Built with biological creativity and artificial precision."* 🧬 + 🤖