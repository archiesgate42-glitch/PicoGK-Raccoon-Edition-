# PicoGK Raccoon Edition 🦝

![Build Status](https://img.shields.io/badge/Build-Linux_x64-green) ![Platform](https://img.shields.io/badge/Platform-Ubuntu_24.04-orange) ![License](https://img.shields.io/badge/License-Apache_2.0-blue)

---

## Mission

**Computational Engineering op Linux (Ubuntu native).**

De officiële [LEAP 71 PicoGK](https://github.com/leap71/PicoGK) levert Windows (.dll) en macOS (.dylib); deze **Raccoon Edition** biedt een Linux Shared Object (.so) runtime, getest op Ubuntu. Geschikt voor workstations én headless servers.

> *"Computational Engineering should run everywhere."*

---

## Technical Stack

| Component | Detail |
|-----------|--------|
| **Runtime** | PicoGK (LEAP 71), C# .NET 9 / net10.0 |
| **Voxel kernel** | OpenVDB |
| **Native** | `libpicogk.1.7.so` (linux-x64), Ubuntu 24.04 |
| **Units** | Alle lengtes in **mm**; voxelgrootte in `Library.Go` |

---

## Orbi Shell V3 – Pipeline (Inside-Out)

Dit project bevat de **Orbi Shell V3**-pipeline: één waterdicht manifold (dome + 3 organische poten + flow-volume + EDF-inlaten + ball sockets + tapered nozzles).

### Inside-Out logica

1. **Legs first** – Drie organische poten op 120° (bulbous segments, cilindrische voeten), daarna de dome erop.
2. **Plenum op Z=20** – Centraal flow-volume (plenum) op Z=20, 3 gebogen buizen door de poten naar zij-uitlaten (≈30 mm, Z≈50).
3. **5 mm walls** – Binnenkant wordt uitgehold (5 mm wand, 6,5 mm bij nozzle-bases); flow-volume wordt afgetrokken van de shell.
4. **Dome** – Gesloten kom-vormige dome (5 mm wand), 3 verticale EDF-inlaten op de bovenrand naar het plenum.
5. **Details** – Ball sockets, tapered nozzles (Z=75 → Z=-40), optionele mounting bosses.
6. **Smoothing** – Organische smoothing (offset +/–), 1 of 2 passes naargelang config.

**Kernbestanden:** `OrbiConfig.cs` (maten & flags), `OrbiPipeline.cs` (stappen), `Program.cs` (entry point).

**Input:** `ref.files/Bodacious Snaget(1).stl` (wordt gevoxeliseerd en getrimd).  
**Output:** Na `dotnet run` → STL zoals in `OrbiConfig.OutputStlFileName` (standaard o.a. `Orbi_V3_Final_Tuned_OriginalRef.stl`). Export-bestanden (.stl) staan in `.gitignore en blijven lokaal.

---

## How to run

Vanaf de repo-root:

```bash
dotnet run
```

- **Laptop (snel):** `OrbiConfig.VoxelSizeMM = 0.35f`, `EnableLaptopMode = true` (1 smoothing pass).
- **Final / print:** `VoxelSizeMM = 0.24f`, `EnableLaptopMode = false`, `SmoothingPasses = 2`.

Headless (geen viewer):

```csharp
Library.Go(OrbiConfig.VoxelSizeMM, () => new OrbiPipeline().Run(), ".", "PicoGK.log", bShowViewer: false);
```

---

## Installatie (Ubuntu/Debian)

```bash
sudo apt update && sudo apt install -y \
  libopenvdb-dev libtbb-dev libglfw3-dev libglew-dev \
  libxinerama-dev libxcursor-dev libxrandr-dev libxi-dev
```

Clone, build, run:

```bash
git clone https://github.com/archiesgate42-glitch/PicoGK-Raccoon-Edition-.git
cd PicoGK-Raccoon-Edition-
dotnet build && dotnet run
```

**Runtime:** `libpicogk.1.7.so` in `native/linux-x64/`; wordt naar build-output gekopieerd.

---

## Credits

Raccoon Edition – community fork (niet officieel LEAP 71).  
*Built with biological creativity and artificial precision.* 🧬 + 🤖
