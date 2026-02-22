using OrbiShell;
using PicoGK;

// -----------------------------------------------------------------------
// ORBI SHELL V3 (Raccoon Edition) – Entry point
// -----------------------------------------------------------------------
// Config: OrbiConfig.cs (dome, legs, flow, EDF, sockets, FixDomeOrientation).
// Pipeline: OrbiPipeline.cs – legs → closed dome + 3 EDF inlets → flow → hollow → nozzles/sockets → smooth → export.
// Output: Orbi_V3_Final_Tuned_OriginalRef.stl (watertight manifold, tuned to original ref images).

Library.Go(
    OrbiConfig.VoxelSizeMM,
    () => new OrbiPipeline().Run(),
    ".",
    "PicoGK.log",
    bShowViewer: false);
