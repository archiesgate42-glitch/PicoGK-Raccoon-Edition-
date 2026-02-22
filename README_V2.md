# Orbi Shell V2 (Raccoon Edition)

V2 refactor: config in **OrbiConfig.cs**, build logic in **OrbiPipeline.cs**, **Program.cs** only starts the pipeline.

---

## Run

From the repo root (so `ref.files/` and the STL path resolve):

```bash
dotnet run
```

Or build then run:

```bash
dotnet build
./bin/Debug/net10.0/Orbi_PicoGK_Engine
```

**Input:** `ref.files/Bodacious Snaget(1).stl`  
**Output:** `Orbi_V2_Tapered_Nozzles.stl` (in the current working directory)

---

## Changing voxel size

Edit **OrbiConfig.cs**:

```csharp
public static float VoxelSizeMM { get; set; } = 0.32f;  // default
```

Examples:

- `0.32f` – default, good for ~30 min print (Kobra S1 Pro)
- `0.35f` – faster, slightly less detail
- `0.28f` – finer detail, longer build

Save and run again with `dotnet run`.

---

## Changing wall thickness

Edit **OrbiConfig.cs**:

```csharp
public static float WallThicknessMM { get; set; } = 5f;
```

For a thicker main wall (e.g. 6 mm), set to `6f`. Nozzle base thickness is separate:

```csharp
public static float WallThicknessNozzleBaseMM { get; set; } = 6.5f;
```

To turn off extra thickness at nozzle bases:

```csharp
public static bool EnableNozzleBaseThickening { get; set; } = false;
```

---

## Optional features (OrbiConfig.cs)

- **Smoothing:** `EnableSmoothing = true`, `SmoothingOffsetMM = 1.2f`
- **Mounting bosses:** `EnableMountingBosses = true`
- **Nozzle base thickening:** `EnableNozzleBaseThickening = true`

Set the corresponding `Enable*` to `false` to disable.

---

## File layout

| File            | Role                                      |
|-----------------|-------------------------------------------|
| `OrbiConfig.cs` | All constants and toggles (single place)  |
| `OrbiPipeline.cs` | Full build steps and private helpers   |
| `Program.cs`    | Calls `Library.Go(OrbiConfig.VoxelSizeMM, () => new OrbiPipeline().Run(), ...)` |

V3 can be added by introducing e.g. `OrbiPipelineV3` or extra steps in the same pipeline, and new options in `OrbiConfig`.
