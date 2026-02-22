namespace OrbiShell;

// ---------------------------------------------------------------------------
// Match reference image: low wide dome, 3 organic legs with bulbous segments, cylindrical feet.
// Laptop: VoxelSizeMM = 0.35f, EnableLaptopMode = true. Final print: 0.24f, EnableLaptopMode = false.
// ---------------------------------------------------------------------------

/// <summary>
/// Orbi Shell V3 – Organic Tripod (match reference image). Physics-first flow, EDF, ball sockets, 0.3 mm tolerance.
/// </summary>
public static class OrbiConfig
{
    // ----- Build mode -----
    /// <summary>Voxel size in mm. 0.35f = laptop-friendly; use 0.24f for final print only.</summary>
    public static float VoxelSizeMM { get; set; } = 0.35f;

    /// <summary>When true: lower-detail legs + 1 smoothing pass (faster).</summary>
    public static bool EnableLaptopMode { get; set; } = true;

    /// <summary>Safe global BBox. X,Y: ±110, Z: -55..110.</summary>
    public static (float XMin, float YMin, float ZMin, float XMax, float YMax, float ZMax) GlobalBBox => (-110f, -110f, -55f, 110f, 110f, 110f);

    // ----- Dome (low wide hemisphere, 200 mm OD, ~45–50 mm tall on leg junction) -----
    public static float DomeOuterR { get; set; } = 100f;
    public static float DomeInnerR { get; set; } = 95f;
    /// <summary>Dome sits on junction; cap from this Z to top.</summary>
    public static float DomeJunctionZ { get; set; } = 50f;
    public static float DomeZTop { get; set; } = 100f;
    /// <summary>Dome cap trim Z min (junction level).</summary>
    public static float DomeZMin { get; set; } = 50f;
    /// <summary>Max height for body/leg trim (dome can go higher).</summary>
    public static float MaxHeightMM { get; set; } = 75f;

    // ----- Closed dome (bowl-shaped, wireframe match, tuned to original ref) -----
    /// <summary>Closed dome sphere radius (mm). Larger to match original ref dome (230 mm OD).</summary>
    public static float DomeRadiusMM { get; set; } = 115f;
    /// <summary>Dome sphere center Z (mm). Slightly higher so it sits perfectly on legs.</summary>
    public static float DomeCenterZMM { get; set; } = 58f;
    /// <summary>EDF inlet cylinder radius (mm) for the three top inlets.</summary>
    public static float EDFInletRadiusMM { get; set; } = 25f;
    /// <summary>EDF inlet depth (mm) from dome top down to plenum.</summary>
    public static float EDFInletDepthMM { get; set; } = 75f;
    /// <summary>EDF inlet radial position as fraction of dome radius (0.48 = closer to dome edge).</summary>
    public static float EDFInletRadialPosition { get; set; } = 0.48f;
    /// <summary>When true: use closed bowl-shaped dome + 3 EDF inlets instead of open dome.</summary>
    public static bool FixDomeOrientation { get; set; } = true;

    // ----- Organic legs (match reference: 2–3 bulbous segments, cylindrical feet) -----
    /// <summary>Number of bulbous sections per leg.</summary>
    public static int LegBulgeCount { get; set; } = 3;

    /// <summary>Radii (mm) for each bulge along leg, e.g. 18 → 28 → 18; foot tapers to 12.</summary>
    public static float[] LegBulgeRadii { get; set; } = { 18f, 28f, 18f };

    /// <summary>Leg start radial distance (mm) under dome.</summary>
    public static float LegStartRadialMM { get; set; } = 75f;

    /// <summary>Leg junction Z (mm) where dome sits.</summary>
    public static float LegJunctionZ { get; set; } = 50f;

    public static float LegCurveStrength { get; set; } = 42f;
    /// <summary>Outward curve (mm) for legs – more spread to match original ref. Used when &gt; 0.</summary>
    public static float LegOutwardCurveMM { get; set; } = 48f;
    /// <summary>Middle bulge radius (mm) – bigger “knee” to match original ref. Used when &gt; 0.</summary>
    public static float MiddleBulgeRadiusMM { get; set; } = 31f;

    /// <summary>Foot cylinder radius (mm).</summary>
    public static float FootCylinderR { get; set; } = 13f;

    /// <summary>Foot cylinder height (mm). Flat on ground.</summary>
    public static float FootHeightMM { get; set; } = 22f;

    /// <summary>Ground Z (foot bottom).</summary>
    public static float FootGroundZ { get; set; } = -40f;

    /// <summary>Foot radial distance (mm).</summary>
    public static float FootRadialMM { get; set; } = 70f;

    public static float WallThicknessMM { get; set; } = 5f;
    public static float WallThicknessNozzleBaseMM { get; set; } = 6.5f;

    // ----- Plenum & flow (physics-first, inside legs) -----
    public static float PlenumRadius { get; set; } = 35f;
    public static float PlenumZ { get; set; } = 20f;
    public static float PlenumZMin { get; set; } = 15f;
    public static float PlenumZMax { get; set; } = 25f;
    public static float FlowTubeR { get; set; } = 15f;
    public static float FlowWallThicknessMM { get; set; } = 5f;
    public static float FlowSideExitRadialMM { get; set; } = 90f;
    public static float FlowSideExitZ { get; set; } = 50f;
    public static float FlowCurveMidZ { get; set; } = 35f;
    public static float FlowCurveMidRadialMM { get; set; } = 70f;

    // ----- EDF inlets -----
    public static float EDFInletRadius { get; set; } = 25f;
    public static float EDFInletZTop { get; set; } = 100f;
    public static float EDFInletZBottom { get; set; } = 20f;
    public static float EDFInletRadialMM { get; set; } = 25f;

    // ----- Ball joints & nozzles (0.3 mm tolerance) -----
    public static float BallR { get; set; } = 45f;
    public static float BallZ { get; set; } = 75f;
    public static float BallDiameterMM { get; set; } = 24f;
    public static float BallRadiusMM => BallDiameterMM * 0.5f;
    public static float SocketToleranceMM { get; set; } = 0.3f;
    public static float SocketRadiusMM => BallRadiusMM + SocketToleranceMM;
    public static float NozzleTipR { get; set; } = 70f;
    public static float NozzleTipZ { get; set; } = -40f;
    public static float NozzleOuterRBase { get; set; } = 8f;
    public static float NozzleOuterRTip { get; set; } = 6f;
    public static float NozzleInnerRBase { get; set; } = 3f;
    public static float NozzleInnerRTip { get; set; } = 2f;
    public static float NozzleBaseExtraRMM { get; set; } = 1.5f;
    public static float NozzleBaseThickenHeightMM { get; set; } = 10f;

    // ----- Smoothing (tuned: stronger offset + 2 passes for ultra-organic blend) -----
    public static float SmoothingOffsetMM { get; set; } = 2.2f;
    public static int SmoothingPasses { get; set; } = 2;
    public static bool EnableHeavySmoothing { get; set; } = false;

    // ----- Optional (always on for final; PreviewMode can skip in pipeline) -----
    public static bool EnableMountingBosses { get; set; } = true;
    public static bool EnableNozzleBaseThickening { get; set; } = true;
    public static bool PreviewMode { get; set; } = false;

    // ----- Mounting bosses -----
    public static float BossRadiusMM { get; set; } = 4f;
    public static float BossHeightMM { get; set; } = 8f;
    public static float BossHoleRadiusMM { get; set; } = 1.5f;
    public static float BossZ { get; set; } = 35f;
    public static float BossRadialMM { get; set; } = 22f;
    public static float BossAngleDeg { get; set; } = 45f;

    // ----- Paths -----
    public static string SourceStlPath { get; set; } = "ref.files/Bodacious Snaget(1).stl";
    public static string OutputStlFileName { get; set; } = "Orbi_V3_Final_Tuned_OriginalRef.stl";

    public static float Deg120Rad => MathF.PI * 2f / 3f;
}
