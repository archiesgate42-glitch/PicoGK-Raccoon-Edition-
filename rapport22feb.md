# Rapport Orbi Shell – PicoGK Raccoon Edition  
**Datum:** 22 februari 2025  
**Project:** Orbi Shell – holle flow-structuur + dome in één manifold

---

## 1. Waar we begonnen

- **Doel:** Orbi Shell genereren met PicoGK (voxel/OpenVDB): een dome-schil met een holle flow-structuur (centrale kamer, 3 buizen naar ball joints, eventueel nozzles naar de grond).
- **Probleem:** Eerste “Inside-Out”-resultaten waren fout: de dome en het flow-systeem zaten in verschillende coördinaatruimtes (dome Z≤0, flow Z>0), waardoor de kamer niet in de holte van de dome zat.
- **Referenties:**  
  - Doelbeeld: `ref.files/Orbi-mont.stl`  
  - Flow-master: `ref.files/flow-structuurv1.stl`, later `ref.files/Orbi-flow-Structuurv3.stl`  
  - Shell-assembly: `Orbi_V1_Final_Revised.stl` (door ons gegenereerd)

---

## 2. Wat we gedaan hebben (chronologisch)

### 2.1 Coördinaten en Z-orientatie (eerste fix)

- **Probleem:** Dome getrimd op Z≤0 (holle helft in negatieve Z), flow op Z=20 en Z=75 → geen overlap.
- **Oplossing:** Flow gespiegeld naar negatieve Z: `chamberCenter = (0, 0, -20)`, `ballZ = -75`, zodat kamer en ballen binnen de dome-holte liggen.
- **Later:** Teruggegaan naar positieve Z voor de flow (chamber Z=20, ballen Z=75) en dome als **bovenste** halve bol (Z≥0) zodat alles in dezelfde ruimte zit.

### 2.2 Prototype V1.x01 – procedurele generatie

- Chamber (0, 0, 20), ballen (45, θ, 75), nozzles van Z=75 naar (70, θ, -40).
- **Inside-Out:** `voxFluid` (chamber R=27, tubes R=7, nozzle-cores) → `voxFlowWalls = voxFluid.voxOffset(3)` → `voxFlowWalls.BoolSubtract(voxFluid)` → holle 3 mm wanden.
- Dome R=100/95 (5 mm), half-space Z≥0.
- Export: `Orbi_Prototype_V1x01.stl`.

### 2.3 STL als master (geen pure procedure meer)

- Overstap naar **STL als referentie** in plaats van alles procedureel.
- **flow-structuurv1.stl** geladen als `voxFluid` (interne “vloeistof”-volume).
- Wand: `voxFlowWalls = voxFluid.voxOffset(3)`; hol: `voxFlowWalls.BoolSubtract(voxFluid)`.
- Assembly: dome + flow walls, dan fluid uit de hele assembly trekken → `Orbi_V1_Final_Hollow.stl`.

### 2.4 Final Revised – BBox-trim, geen topgat

- **Poten verwijderen:** BBox3-trim (binnen dome R=100, boven bodem Z≥0) i.p.v. sphere-intersect, zodat geometrie niet “verdwijnt”.
- 5 mm holle buizen; dome met **drie** 30 mm zijgaten (R=15), **geen** topgat.
- `voxFinal = voxDomeShell.BoolAdd(voxHollowFlow)`.
- Export: `Orbi_V1_Final_Revised.stl`.

### 2.5 Flow tube alleen (Orbi-flow-Structuurv1)

- Alleen de flow als holle buis: load STL → offset 5 mm → BoolSubtract(fluid) → open uiteinden (spheres/cylinders).
- Export: `Orbi_V1_Flow_Tube_Final.stl`.

### 2.6 Blob-fix – cilinder-boren

- “Blob” kwam doordat de boolean-logica of de uiteinden niet goed open waren.
- Expliciete stappen:  
  - `voxOuter` = offset(5 mm),  
  - `voxHollowTube = voxOuter.BoolSubtract(voxFluid)` op een **kopie** van outer.  
  - **Cilinders** om uiteinden open te boren: chamber cylinder R=27 (omhoog), ball joints R=9, landing tips (70, θ, -40) R=5 omhoog.
- Export: `Orbi_V1_Flow_Tube_Hollow_FIX.stl`.

### 2.7 Hollow Sharp – negatieve offset, hoge resolutie

- **Orbi-flow-Structuurv3.stl** als master.
- **VoxelSize 0.2f** voor scherpere details (minder “marshmallow”).
- **Negatieve offset:** `voxInner = voxOuter.voxOffset(-3)` → holle schil door `voxOuter.BoolSubtract(voxInner)`.
- Vier poorten geboord (cylinder R=8), merge met shell indien `Orbi_V1_Final_Revised.stl` aanwezig.
- Export: `Orbi_V1_Final_Hollow_Sharp.stl`.

### 2.8 Airflow Ready – enkele holle manifold (huidige stand)

- **Eén** hol object: `voxOuter` = load v3 → `voxInner = voxOuter.voxOffset(-5)` → **voxOuter.BoolSubtract(voxInner)** (in-place).
- **Luchtgaten:**  
  - Kamer (0, 0, 20): cylinder R=25 omhoog.  
  - Drie buiseinden (45, θ, 75): spheres R=10 om de “doppen” open te maken.
- **Integratie:** `voxFinal = voxShell.BoolAdd(voxOuter)` als shell-STL bestaat.
- **VoxelSize 0.25f**, GC vóór export.
- Export: **`Orbi_V1_Final_Airflow_Ready.stl`**.
- **Git:** `Program.cs` lokaal gecommit; push faalde door authenticatie (moet lokaal door gebruiker).

---

## 3. Waar we nu zijn

### 3.1 Huidige Program.cs-logica (samenvatting)

| Stap | Actie |
|------|--------|
| 1 | Load `ref.files/Orbi-flow-Structuurv3.stl` als `voxOuter`. |
| 2 | `voxInner = voxOuter.voxOffset(-5)` (5 mm naar binnen). |
| 3 | `voxOuter.BoolSubtract(voxInner)` → één holle flow-manifold (uiteinden nog dicht). |
| 4 | Chamber (0,0,20): cylinder R=25 omhoog aftrekken. |
| 5 | Drie buiseinden (45, θ, 75): spheres R=10 aftrekken. |
| 6 | Indien `Orbi_V1_Final_Revised.stl` bestaat: load als `voxShell`, `voxFinal = voxShell.BoolAdd(voxOuter)`. Anders: `voxFinal = voxOuter`. |
| 7 | GC, meshing, save **Orbi_V1_Final_Airflow_Ready.stl**. |

### 3.2 Parameters (vastgelegd in code)

- **VoxelSize:** 0.25 mm  
- **Wanddikte flow:** 5 mm (erode -5 mm)  
- **Chamber Z:** 20  
- **Ballen:** straal 45 mm, Z=75, 120° uit elkaar  
- **Shell:** optioneel uit `Orbi_V1_Final_Revised.stl`

### 3.3 Belangrijke bestanden

| Bestand | Rol |
|--------|-----|
| `Program.cs` | Orbi V1 Airflow Ready-pipeline (gecommit). |
| `ref.files/Orbi-flow-Structuurv3.stl` | Master flow-vorm (buitenkant). |
| `ref.files/Orbi-mont.stl` | Visuele referentie doelbeeld. |
| `ref.files/flow-structuurv1.stl` | Eerdere flow-referentie. |
| `Orbi_V1_Final_Revised.stl` | Shell (dome + eerdere flow); moet in projectroot staan voor merge. |
| **Output** | `Orbi_V1_Final_Airflow_Ready.stl` (na run). |

### 3.4 Wat jij nog kunt doen

1. **Run:** Vanuit de projectroot: `dotnet run` (of IDE). Zorg dat `ref.files/Orbi-flow-Structuurv3.stl` (en eventueel `Orbi_V1_Final_Revised.stl`) aanwezig zijn.
2. **Push:** `git push` lokaal uitvoeren (met jouw GitHub-credentials).
3. **STL in repo:** Na een geslaagde run eventueel `Orbi_V1_Final_Airflow_Ready.stl` toevoegen en committen/pushen.

---

## 4. Technische notities

- **PicoGK:** VoxelSize wordt eenmalig in `Library.Go(...)` gezet; alle eenheden in mm.
- **Offset:** Positief = naar buiten, negatief = naar binnen; negatieve offset voorkomt “marshmallow” door eerst de buitenkant van het STL te behouden.
- **Boolean-volgorde:** Eerst hol maken (outer − inner), dan vent-openingen boren, daarna pas samenvoegen met shell.
- **Z-orientatie:** Chamber op Z=20, ballen op Z=75; shell (dome) is bovenste halve bol Z≥0, afgestemd op Orbi_V1_Final_Revised.stl.

---

*Einde rapport – 22 feb 2025*
