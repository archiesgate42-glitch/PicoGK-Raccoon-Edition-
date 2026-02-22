# Orbi Shell V1.x01 – Technical Overview v0.1 (for Grok AI)

**Purpose:** This document gives Grok AI (or any downstream optimizer) a complete technical snapshot of the Orbi Shell Raccoon Edition so it can propose concrete upgrades (performance, geometry, printability, code structure).

---

## 1. Executive summary

- **Project:** Orbi Shell – a single hollow manifold (dome + internal chamber + 3 flow tubes + 3 landing nozzles) for airflow/fluid applications.
- **Engine:** PicoGK (LEAP 71), C# .NET 9/10, OpenVDB voxel kernel; Linux build via “Raccoon Edition” (libpicogk.1.7.so).
- **Current source mesh:** `Bodacious Snaget(1).stl` – repaired via voxelization (holes, non-manifold edges, flipped triangles become a solid SDF, then re-meshed).
- **Output:** `Orbi_V1_Final_Airflow_Ready.stl` – target: watertight, 5 mm walls, 200 mm OD dome envelope, Z=0..100, chamber at Z=20, three 30 mm side holes, nozzles Z=75 → Z=-40.
- **Printer target:** Anycubic Kobra S1 Pro; voxel resolution tuned for ~30 min print time at 5 mm wall.

---

## 2. Tech stack (hard facts)

| Component | Detail |
|-----------|--------|
| **Repo** | PicoGK-Raccoon-Edition- (this workspace) |
| **App** | Single C# exe; entry: `Library.Go(VoxelSizeMM, callback, ".", "PicoGK.log", bShowViewer: false)` |
| **PicoGK ref** | ProjectReference to `../../PicoGK-Main/PicoGK.csproj` (external) |
| **Target framework** | net10.0 (or net9.0 for PicoGK DLL) |
| **Native** | `native/linux-x64/libpicogk.1.7.so` copied to output |
| **Units** | All lengths in **mm**; voxel size set once in `Library.Go` (no per-object resolution) |
| **Mesh → voxels** | `new Voxels(Mesh msh)` → RenderMesh (solid fill) |
| **Voxels → mesh** | `new Mesh(Voxels vox)` → marching cubes on level set (watertight by construction) |

---

## 3. Current pipeline (Program.cs – Bodacious V1.x01)

### 3.1 High-level steps

1. **Load & heal:** `Mesh.mshFromStlFile("ref.files/Bodacious Snaget(1).stl")` → `new Voxels(msh)` (voxelization closes holes, resolves non-manifold, “fixes” flipped tris by producing one solid volume).
2. **Dome envelope:** `voxOuter.Trim(BBox3(-100,-100,0, 100,100,100))` – 200 mm OD, Z=0..100.
3. **5 mm hollow:** `voxInner = voxOuter.voxOffset(-5)`; `voxOuter.BoolSubtract(voxInner)` (in-place). Result: single hollow shell, 5 mm wall.
4. **Chamber vent:** Subtract Z-aligned cylinder R=25, center Z=60, height ~90 (opens at chamber Z=20 upward).
5. **Three 30 mm side holes:** Subtract 3× Z-cylinder R=15 at (radius ~90, Z=50), 120° apart.
6. **Landing nozzles:** Add 3 hollow beams (outer R=8, inner R=3) from (45, θ, 75) to (70, θ, -40); merged into `voxOuter`.
7. **Export:** `GC.Collect()` + `GC.WaitForPendingFinalizers()`; `new Mesh(voxOuter)`; `SaveToStlFile("Orbi_V1_Final_Airflow_Ready.stl")`.

### 3.2 Constants (single source of truth in code)

```text
VoxelSizeMM       = 0.35f
WallThicknessMM   = 5f
DomeOuterR        = 100f    // 200 mm OD
DomeInnerR        = 95f     // used for side hole radius
ChamberZ          = 20f
SideHoleR         = 15f     // 30 mm diameter
BallR             = 45f
BallZ             = 75f
NozzleTipR        = 70f
NozzleTipZ        = -40f
NozzleOuterR      = 8f
NozzleInnerR      = 3f
SourceStlPath     = "ref.files/Bodacious Snaget(1).stl"
```

### 3.3 Key file paths

| Path | Role |
|------|------|
| `Program.cs` | Only entry; all logic inline in `Library.Go` callback |
| `ref.files/Bodacious Snaget(1).stl` | Input mesh (broken: 68 holes, 1371 non-manifold edges, 2697 flipped tris) |
| `ref.files/Orbi-mont.stl` | Visual reference / target look |
| `ref.files/Orbi-flow-Structuurv3.stl` | Alternative flow master (used in earlier pipelines) |
| `Orbi_V1_Final_Airflow_Ready.stl` | Output (written in current working directory) |
| `PicoGK.log` | Engine log (same dir as run) |

---

## 4. Geometry and coordinates (for upgrades)

- **Convention:** Z up; dome “lid” from Z=0 to Z=100; chamber low at Z=20 (low CoG).
- **Chamber:** Central opening at (0, 0, 20); cylinder subtracted along +Z.
- **Side holes:** 30 mm diameter at azimuth 0°, 120°, 240°; radial position ~90 mm, Z=50.
- **Ball joints (logical):** (45, θ, 75) with θ = 0°, 120°, 240°.
- **Nozzle ends (landing):** (70, θ, -40). Nozzles pierce through dome (Z=75 down to Z=-40).
- **Clearance:** Flow exits at chamber Z=20; nozzles are at ball Z=75 and tips Z=-40 – no geometric clash at Z=20.

---

## 5. PicoGK API usage (relevant subset)

- **Voxels:** `new Voxels(Mesh)`, `new Voxels(Voxels)`, `voxOffset(float)` (negative = inward), `BoolSubtract`, `BoolAdd`, `Trim(BBox3)`.
- **Primitives:** `Voxels.voxCylinder(center, radius, height)` (Z-aligned), `Voxels.voxSphere(center, radius)`.
- **Lattice:** `AddBeam(vecA, vecB, radA, radB, bRoundCap)`; `new Voxels(Lattice)`.
- **Mesh:** `Mesh.mshFromStlFile(path)`, `new Mesh(Voxels)`, `msh.SaveToStlFile(path)`.
- **No adaptive voxel size:** One global value for entire pipeline.

---

## 6. Known limitations and risks

- **Voxel size fixed:** 0.35 mm everywhere; thin or detailed regions cannot get finer resolution without global increase (memory/cost).
- **No mesh healing before voxelization:** STL is loaded as-is; healing is purely “voxelize → solid”. Very bad normals or degenerate tris could still affect voxelization quality.
- **No explicit manifold check:** We assume PicoGK’s marching cubes output is watertight; no post-export validation (e.g. MeshLab, netfabb, or custom check).
- **Single thread / single run:** No parallelism or multi-resolution stages described; one `Library.Go` run produces one STL.
- **Memory:** Large voxel grids (e.g. 0.2 mm) can be heavy; only mitigation is `GC.Collect()` before meshing.
- **Side hole position:** Currently at fixed Z=50 and radius ~90 mm; not derived from actual mesh geometry (e.g. where tubes meet shell).
- **Nozzle cross-section:** Simple circular beams (no airfoil or aerodynamic shaping).

---

## 7. Suggested areas for Grok AI upgrades

Use this section to drive concrete improvements.

1. **Performance / print time**
   - Propose voxel size vs. print-time vs. wall-thickness trade-offs (e.g. 0.3 vs 0.35 vs 0.4 mm) for Anycubic Kobra S1 Pro.
   - Suggest adaptive or multi-pass strategies if the engine or wrapper can support them (e.g. coarse shell + fine critical regions).

2. **Geometry and robustness**
   - Suggest placement of the three 30 mm holes from mesh/skeleton (e.g. intersection of tube centerlines with shell) instead of fixed Z=50 and R=90.
   - Propose optional pre-voxel mesh cleanup (e.g. duplicate vertex merge, degenerate tri removal, normal consistency) if we add a step before `mshFromStlFile` or a small mesh utility.
   - Consider chamber shape (e.g. cylindrical vs spherical cap) for better airflow or stress.

3. **Manifold and quality assurance**
   - Recommend a post-export manifold/watertight check (tool or library) and where to plug it in (e.g. after `SaveToStlFile`).
   - Suggest STL validation (e.g. min thickness, overhang angles) for the target printer.

4. **Code and maintainability**
   - Propose splitting `Program.cs` into config (constants), pipeline steps (load/heal, hollow, vents, nozzles, export), and a small runner.
   - Suggest logging/metrics (timing, voxel count, triangle count, bounding box) for regression and tuning.

5. **Physics and airflow**
   - Suggest simple CFD-friendly tweaks (e.g. fillets at tube junctions, chamfers at hole edges) using PicoGK’s `voxFillet` or offset chains if available.
   - Propose nozzle inner/outer radii or taper for desired flow vs. strength.

6. **Printer-specific**
   - Recommend Anycubic Kobra S1 Pro best practices (layer height, line width, infill) that align with 5 mm wall and 30 min target.
   - Suggest orientation and support strategy for the exported STL.

---

## 8. References inside this repo

- `rapport22feb.md` – Narrative history (what was tried, current state).
- `docs/ANALYSE_VOXELS_GEOMETRIE.md` – PicoGK voxel/mesh capabilities and API summary.
- `README.md` – Build and run (Linux, dependencies, native lib).

---

**Document version:** overviewv0.1  
**Last pipeline:** Orbi Shell V1.x01 (Raccoon Edition), source `Bodacious Snaget(1).stl` → `Orbi_V1_Final_Airflow_Ready.stl`.
