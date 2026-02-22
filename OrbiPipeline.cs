using PicoGK;
using System.Diagnostics;
using System.Numerics;

namespace OrbiShell;

/// <summary>
/// Orbi Shell V3 – Organic Tripod (match reference image). Legs first, dome on junction, flow inside legs.
/// Physics-first flow, EDF inlets, ball sockets, tapered nozzles. EnableLaptopMode → 1 smoothing pass.
/// </summary>
public sealed class OrbiPipeline
{
    /// <summary>Run: Load → Organic legs → Dome on junction → Flow volume → Hollow → Merge → EDF/sockets/nozzles → bosses/thickening → Smoothing → Export.</summary>
    public void Run()
    {
        var swTotal = Stopwatch.StartNew();
        Console.WriteLine("🦝 Orbi Shell V3 (Raccoon Edition) – Organic Tripod Match Image 1");
        Library.Log($"Voxel size (mm): {OrbiConfig.VoxelSizeMM}");

        try
        {
            // 1. Load & voxelize input STL
            using (var voxInput = LoadAndHeal(out int triCount))
            {
                if (voxInput == null) return;

                // 2. Create organic tripod legs only (bulges + feet)
                using (var voxShell = CreateOrganicTripodLegs())
                {
                    // 3. Dome: closed bowl (wireframe match) or legacy open dome
                    if (OrbiConfig.FixDomeOrientation)
                        AddCorrectDomeAndInlets(voxShell);
                    else
                        IntegrateDomeOnLegJunction(voxShell);

                    // 4. Flow volume (plenum + 3 curved tubes inside legs) – physics-first
                    using (var voxFlowVolume = CreateFlowVolume())
                    {
                        // 5. Hollow flow system (5 mm walls, 6.5 at nozzle bases)
                        using (var voxHollowFlow = HollowFlowSystem(voxFlowVolume))
                        {
                            voxShell.BoolAdd(voxHollowFlow);

                            // 6. EDF inlets (only if not already cut by AddCorrectDomeAndInlets), ball sockets, tapered nozzles
                            if (!OrbiConfig.FixDomeOrientation)
                                AddVerticalEDFInlets(voxShell);
                            else
                                Console.WriteLine("  [FixDomeOrientation] EDF inlets already cut in dome.");
                            AddBallSocketsAndTaperedNozzles(voxShell);

                            if (!OrbiConfig.PreviewMode)
                            {
                                if (OrbiConfig.EnableNozzleBaseThickening)
                                    AddNozzleBaseThickening(voxShell);
                                if (OrbiConfig.EnableMountingBosses)
                                    AddMountingBosses(voxShell);
                            }
                            else
                                Console.WriteLine("  [PreviewMode] Skipping mounting bosses and nozzle base thickening.");

                            // 7. Organic smoothing (1 pass if EnableLaptopMode, else SmoothingPasses)
                            ApplyOrganicSmoothing(voxShell);

                            // 8. Export
                            Export(voxShell, triCount);
                        }
                    }
                }
            }

            swTotal.Stop();
            Console.WriteLine($"  [Total] {swTotal.ElapsedMilliseconds} ms");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR: {ex.Message}");
            Library.Log("Error: " + ex.Message);
        }
    }

    private Voxels? LoadAndHeal(out int triangleCount)
    {
        triangleCount = 0;
        var sw = Stopwatch.StartNew();
        Console.WriteLine("- Step 1: Load & voxelize input STL (heal holes / non-manifold)...");

        var msh = Mesh.mshFromStlFile(OrbiConfig.SourceStlPath);
        triangleCount = msh.nTriangleCount();
        if (triangleCount == 0)
        {
            Console.WriteLine("❌ STL empty or failed: " + OrbiConfig.SourceStlPath);
            msh.Dispose();
            return null;
        }

        var vox = new Voxels(msh);
        msh.Dispose();

        var (xMin, yMin, zMin, xMax, yMax, zMax) = OrbiConfig.GlobalBBox;
        vox.Trim(new BBox3(xMin, yMin, zMin, xMax, yMax, zMax));
        vox.CalculateProperties(out _, out BBox3 bbox);
        sw.Stop();
        Console.WriteLine($"  Triangles: {triangleCount}, bbox Z: {bbox.vecMin.Z:F1} .. {bbox.vecMax.Z:F1}, [{sw.ElapsedMilliseconds} ms]");
        return vox;
    }

    /// <summary>Three organic legs at 120° with bulbous segments (LegBulgeRadii) and cylindrical feet. No dome.</summary>
    private Voxels CreateOrganicTripodLegs()
    {
        var sw = Stopwatch.StartNew();
        int nBulge = Math.Max(1, Math.Min(OrbiConfig.LegBulgeCount, OrbiConfig.LegBulgeRadii?.Length ?? 3));
        float[] radii = OrbiConfig.LegBulgeRadii ?? new float[] { 18f, 28f, 18f };
        Console.WriteLine($"- Step 2: Create organic tripod legs ({nBulge} bulges, feet R={OrbiConfig.FootCylinderR} mm)...");

        float rStart = OrbiConfig.LegStartRadialMM;
        float zJunc = OrbiConfig.LegJunctionZ;
        // Outward curve: LegOutwardCurveMM for more spread (original ref), else LegCurveStrength
        float curve = OrbiConfig.LegOutwardCurveMM > 0f
            ? OrbiConfig.LegOutwardCurveMM * 0.25f
            : OrbiConfig.LegCurveStrength * 0.12f;
        float rMid1 = rStart + curve;
        float rMid2 = rStart - 2f;
        float zMid1 = zJunc - 17f;
        float zMid2 = 8f;
        float rFoot = OrbiConfig.FootRadialMM;
        float zAboveFoot = OrbiConfig.FootGroundZ + OrbiConfig.FootHeightMM;
        float zFootCenter = OrbiConfig.FootGroundZ + OrbiConfig.FootHeightMM * 0.5f;
        float footR = OrbiConfig.FootCylinderR;
        float footH = OrbiConfig.FootHeightMM;
        float R0 = radii.Length > 0 ? radii[0] : 18f;
        float R1 = OrbiConfig.MiddleBulgeRadiusMM > 0f ? OrbiConfig.MiddleBulgeRadiusMM : (radii.Length > 1 ? radii[1] : 28f);
        float R2 = radii.Length > 2 ? radii[2] : 18f;
        float R3 = 12f; // taper to foot

        using (var latLegs = new Lattice())
        {
            for (int i = 0; i < 3; i++)
            {
                float a = i * OrbiConfig.Deg120Rad;
                float cx = MathF.Cos(a);
                float cy = MathF.Sin(a);

                var p0 = new Vector3(rStart * cx, rStart * cy, zJunc);
                var p1 = new Vector3(rMid1 * cx, rMid1 * cy, zMid1);
                var p2 = new Vector3(rMid2 * cx, rMid2 * cy, zMid2);
                var p3 = new Vector3(rFoot * cx, rFoot * cy, zAboveFoot);

                latLegs.AddBeam(p0, p1, R0, R1, bRoundCap: true);
                latLegs.AddBeam(p1, p2, R1, R2, bRoundCap: true);
                latLegs.AddBeam(p2, p3, R2, R3, bRoundCap: true);
                latLegs.AddSphere(p1, R1);
                latLegs.AddSphere(p2, R2);
            }

            using (var voxLegs = new Voxels(latLegs))
            {
                for (int i = 0; i < 3; i++)
                {
                    float a = i * OrbiConfig.Deg120Rad;
                    float x = OrbiConfig.FootRadialMM * MathF.Cos(a);
                    float y = OrbiConfig.FootRadialMM * MathF.Sin(a);
                    var footCenter = new Vector3(x, y, zFootCenter);
                    using (var voxFoot = Voxels.voxCylinder(footCenter, footR, footH))
                        voxLegs.BoolAdd(voxFoot);
                }

                var (xMin, yMin, zMin, xMax, yMax, zMax) = OrbiConfig.GlobalBBox;
                voxLegs.Trim(new BBox3(xMin, yMin, zMin, xMax, yMax, zMax));
                sw.Stop();
                voxLegs.CalculateProperties(out float v, out _);
                Console.WriteLine($"  Legs: {v:F0} mm³ [{sw.ElapsedMilliseconds} ms]");
                return new Voxels(voxLegs);
            }
        }
    }

    /// <summary>Add low wide dome (200 mm OD, ~50 mm tall) on top of leg junction; merge into voxShell.</summary>
    private void IntegrateDomeOnLegJunction(Voxels voxShell)
    {
        var sw = Stopwatch.StartNew();
        Console.WriteLine("- Step 3: Integrate dome on leg junction (R=100 mm, Z 50..100)...");

        float zCap = OrbiConfig.DomeJunctionZ;
        float zTop = OrbiConfig.DomeZTop;
        float rOut = OrbiConfig.DomeOuterR;
        float rIn = OrbiConfig.DomeInnerR;

        using (var voxOuter = Voxels.voxSphere(Vector3.Zero, rOut))
        using (var voxInner = Voxels.voxSphere(Vector3.Zero, rIn))
        {
            voxOuter.Trim(new BBox3(-rOut, -rOut, zCap, rOut, rOut, zTop));
            voxInner.Trim(new BBox3(-rIn, -rIn, zCap, rIn, rIn, zTop));
            using (var voxDome = voxOuter - voxInner)
                voxShell.BoolAdd(voxDome);
        }
        sw.Stop();
        Console.WriteLine($"  [{sw.ElapsedMilliseconds} ms]");
    }

    /// <summary>Add closed bowl-shaped dome (tuned: larger, 5 mm wall) opening upwards and subtract 3 vertical EDF inlets on dome top rim.</summary>
    private void AddCorrectDomeAndInlets(Voxels vox)
    {
        if (!OrbiConfig.FixDomeOrientation) return;

        var sw = Stopwatch.StartNew();
        float rOut = OrbiConfig.DomeRadiusMM;
        float rIn = rOut - OrbiConfig.FlowWallThicknessMM;
        float zCenter = OrbiConfig.DomeCenterZMM;
        float zTrim = zCenter - 18f; // rim just below junction for clean sit on legs

        Console.WriteLine($"- Step 3: Closed dome + 3 EDF inlets (R={rOut} mm, center Z={zCenter}, 5 mm wall)...");

        // 1. Bowl = upper part of sphere (outer minus inner) for seamless blend
        using (var voxOuter = Voxels.voxSphere(new Vector3(0, 0, zCenter), rOut))
        using (var voxInner = Voxels.voxSphere(new Vector3(0, 0, zCenter), rIn))
        {
            voxOuter.Trim(new BBox3(-rOut - 2f, -rOut - 2f, zTrim, rOut + 2f, rOut + 2f, 125f));
            voxInner.Trim(new BBox3(-rIn - 2f, -rIn - 2f, zTrim, rIn + 2f, rIn + 2f, 125f));
            using (var voxDome = voxOuter - voxInner)
                vox.BoolAdd(voxDome);
        }

        // 2. Subtract three vertical EDF inlets on dome top rim (radial position from config)
        float inletRadial = OrbiConfig.DomeRadiusMM * OrbiConfig.EDFInletRadialPosition;
        float zTop = OrbiConfig.DomeCenterZMM + 52f;
        float zBottom = OrbiConfig.DomeCenterZMM - 28f;
        for (int i = 0; i < 3; i++)
        {
            float angle = i * 120f * MathF.PI / 180f;
            float cx = MathF.Cos(angle);
            float cy = MathF.Sin(angle);
            var start = new Vector3(inletRadial * cx, inletRadial * cy, zTop);
            var end = new Vector3(inletRadial * cx, inletRadial * cy, zBottom);
            using (var latInlet = new Lattice())
            {
                latInlet.AddBeam(start, end, OrbiConfig.EDFInletRadiusMM, OrbiConfig.EDFInletRadiusMM, bRoundCap: true);
                using (var voxInlet = new Voxels(latInlet))
                    vox.BoolSubtract(voxInlet);
            }
        }

        sw.Stop();
        Console.WriteLine($"  [{sw.ElapsedMilliseconds} ms]");
    }

    private Voxels CreateFlowVolume()
    {
        var sw = Stopwatch.StartNew();
        Console.WriteLine("- Step 4: Create flow volume (plenum + 3 curved tubes inside legs to side exits)...");

        var plenumCenter = new Vector3(0f, 0f, OrbiConfig.PlenumZ);
        using (var voxPlenum = Voxels.voxSphere(plenumCenter, OrbiConfig.PlenumRadius))
        {
            voxPlenum.Trim(new BBox3(-OrbiConfig.PlenumRadius - 2f, -OrbiConfig.PlenumRadius - 2f, OrbiConfig.PlenumZMin,
                OrbiConfig.PlenumRadius + 2f, OrbiConfig.PlenumRadius + 2f, OrbiConfig.PlenumZMax));

            using (var latTubes = new Lattice())
            {
                float r0 = OrbiConfig.PlenumRadius;
                float rMid = OrbiConfig.FlowCurveMidRadialMM;
                float rEnd = OrbiConfig.FlowSideExitRadialMM;
                float z0 = OrbiConfig.PlenumZ;
                float zMid = OrbiConfig.FlowCurveMidZ;
                float zEnd = OrbiConfig.FlowSideExitZ;
                float tubeR = OrbiConfig.FlowTubeR;

                for (int i = 0; i < 3; i++)
                {
                    float a = i * OrbiConfig.Deg120Rad;
                    var p0 = new Vector3(r0 * MathF.Cos(a), r0 * MathF.Sin(a), z0);
                    var pMid = new Vector3(rMid * MathF.Cos(a), rMid * MathF.Sin(a), zMid);
                    var pEnd = new Vector3(rEnd * MathF.Cos(a), rEnd * MathF.Sin(a), zEnd);
                    latTubes.AddBeam(p0, pMid, tubeR, tubeR, bRoundCap: true);
                    latTubes.AddBeam(pMid, pEnd, tubeR, tubeR, bRoundCap: true);
                }

                using (var voxTubes = new Voxels(latTubes))
                using (var voxFlow = new Voxels(voxPlenum))
                {
                    voxFlow.BoolAdd(voxTubes);
                    sw.Stop();
                    voxFlow.CalculateProperties(out float v, out _);
                    Console.WriteLine($"  Flow volume: {v:F0} mm³ [{sw.ElapsedMilliseconds} ms]");
                    return new Voxels(voxFlow);
                }
            }
        }
    }

    private Voxels HollowFlowSystem(Voxels flowVol)
    {
        var sw = Stopwatch.StartNew();
        Console.WriteLine($"- Step 5: Hollow flow system (offset {OrbiConfig.FlowWallThicknessMM} mm, subtract flow)...");

        using (var voxWalls = flowVol.voxOffset(OrbiConfig.FlowWallThicknessMM))
        using (var voxHollow = new Voxels(voxWalls))
        {
            voxHollow.BoolSubtract(flowVol);
            sw.Stop();
            voxHollow.CalculateProperties(out float v, out _);
            Console.WriteLine($"  Hollow flow walls: {v:F0} mm³ [{sw.ElapsedMilliseconds} ms]");
            return new Voxels(voxHollow);
        }
    }

    private void AddVerticalEDFInlets(Voxels vox)
    {
        var sw = Stopwatch.StartNew();
        Console.WriteLine($"- Step 6a: 3× vertical EDF inlets R={OrbiConfig.EDFInletRadius} mm (Z={OrbiConfig.EDFInletZBottom}..{OrbiConfig.EDFInletZTop})...");

        float zCenter = (OrbiConfig.EDFInletZBottom + OrbiConfig.EDFInletZTop) * 0.5f;
        float height = OrbiConfig.EDFInletZTop - OrbiConfig.EDFInletZBottom + 4f;
        for (int i = 0; i < 3; i++)
        {
            float a = i * OrbiConfig.Deg120Rad;
            float x = OrbiConfig.EDFInletRadialMM * MathF.Cos(a);
            float y = OrbiConfig.EDFInletRadialMM * MathF.Sin(a);
            var center = new Vector3(x, y, zCenter);
            using (var voxInlet = Voxels.voxCylinder(center, OrbiConfig.EDFInletRadius, height))
                vox.BoolSubtract(voxInlet);
        }
        sw.Stop();
        Console.WriteLine($"  [{sw.ElapsedMilliseconds} ms]");
    }

    private void AddBallSocketsAndTaperedNozzles(Voxels vox)
    {
        var sw = Stopwatch.StartNew();
        Console.WriteLine($"- Step 6b: Ball sockets (Ø{OrbiConfig.BallDiameterMM} mm, tolerance {OrbiConfig.SocketToleranceMM} mm) + tapered hollow nozzles...");

        using (var latBalls = new Lattice())
        {
            for (int i = 0; i < 3; i++)
            {
                float a = i * OrbiConfig.Deg120Rad;
                var c = new Vector3(OrbiConfig.BallR * MathF.Cos(a), OrbiConfig.BallR * MathF.Sin(a), OrbiConfig.BallZ);
                latBalls.AddSphere(c, OrbiConfig.BallRadiusMM);
            }
            using (var voxBalls = new Voxels(latBalls))
                vox.BoolAdd(voxBalls);
        }

        for (int i = 0; i < 3; i++)
        {
            float a = i * OrbiConfig.Deg120Rad;
            var c = new Vector3(OrbiConfig.BallR * MathF.Cos(a), OrbiConfig.BallR * MathF.Sin(a), OrbiConfig.BallZ);
            using (var voxSocket = Voxels.voxSphere(c, OrbiConfig.SocketRadiusMM))
                vox.BoolSubtract(voxSocket);
        }

        using (var latOuter = new Lattice())
        using (var latInner = new Lattice())
        {
            for (int i = 0; i < 3; i++)
            {
                float a = i * OrbiConfig.Deg120Rad;
                var ball = new Vector3(OrbiConfig.BallR * MathF.Cos(a), OrbiConfig.BallR * MathF.Sin(a), OrbiConfig.BallZ);
                var tip = new Vector3(OrbiConfig.NozzleTipR * MathF.Cos(a), OrbiConfig.NozzleTipR * MathF.Sin(a), OrbiConfig.NozzleTipZ);
                latOuter.AddBeam(ball, tip, OrbiConfig.NozzleOuterRBase, OrbiConfig.NozzleOuterRTip, bRoundCap: true);
                latInner.AddBeam(ball, tip, OrbiConfig.NozzleInnerRBase, OrbiConfig.NozzleInnerRTip, bRoundCap: true);
            }
            using (var voxOuter = new Voxels(latOuter))
            using (var voxInner = new Voxels(latInner))
            using (var voxNozzles = new Voxels(voxOuter))
            {
                voxNozzles.BoolSubtract(voxInner);
                vox.BoolAdd(voxNozzles);
            }
        }
        sw.Stop();
        Console.WriteLine($"  [{sw.ElapsedMilliseconds} ms]");
    }

    private void AddNozzleBaseThickening(Voxels vox)
    {
        var sw = Stopwatch.StartNew();
        float extraR = OrbiConfig.NozzleBaseExtraRMM;
        float h = OrbiConfig.NozzleBaseThickenHeightMM;
        Console.WriteLine($"- Step 6c: Nozzle base thickening (+{extraR} mm, h={h} mm)...");

        using (var latOuter = new Lattice())
        using (var latInner = new Lattice())
        {
            for (int i = 0; i < 3; i++)
            {
                float a = i * OrbiConfig.Deg120Rad;
                var ball = new Vector3(OrbiConfig.BallR * MathF.Cos(a), OrbiConfig.BallR * MathF.Sin(a), OrbiConfig.BallZ);
                var ballDown = new Vector3(OrbiConfig.BallR * MathF.Cos(a), OrbiConfig.BallR * MathF.Sin(a), OrbiConfig.BallZ - h);
                float rOuter = OrbiConfig.NozzleOuterRBase + extraR;
                latOuter.AddBeam(ball, ballDown, rOuter, rOuter, bRoundCap: true);
                latInner.AddBeam(ball, ballDown, OrbiConfig.NozzleInnerRBase, OrbiConfig.NozzleInnerRBase, bRoundCap: true);
            }
            using (var voxOuter = new Voxels(latOuter))
            using (var voxInner = new Voxels(latInner))
            using (var voxThick = new Voxels(voxOuter))
            {
                voxThick.BoolSubtract(voxInner);
                vox.BoolAdd(voxThick);
            }
        }
        sw.Stop();
        Console.WriteLine($"  [{sw.ElapsedMilliseconds} ms]");
    }

    private void AddMountingBosses(Voxels vox)
    {
        var sw = Stopwatch.StartNew();
        float r = OrbiConfig.BossRadialMM;
        float z = OrbiConfig.BossZ;
        float deg = OrbiConfig.BossAngleDeg * MathF.PI / 180f;
        Console.WriteLine($"- Step 6d: 4× mounting bosses R={OrbiConfig.BossRadiusMM} mm, hole Ø{OrbiConfig.BossHoleRadiusMM * 2} mm...");

        for (int i = 0; i < 4; i++)
        {
            float a = deg + i * (MathF.PI * 0.5f);
            float x = r * MathF.Cos(a);
            float y = r * MathF.Sin(a);
            var center = new Vector3(x, y, z);
            float hCyl = OrbiConfig.BossHeightMM;
            using (var voxBoss = Voxels.voxCylinder(center, OrbiConfig.BossRadiusMM, hCyl))
                vox.BoolAdd(voxBoss);
            using (var voxHole = Voxels.voxCylinder(center, OrbiConfig.BossHoleRadiusMM, hCyl + 4f))
                vox.BoolSubtract(voxHole);
        }
        sw.Stop();
        Console.WriteLine($"  [{sw.ElapsedMilliseconds} ms]");
    }

    /// <summary>Organic smoothing. EnableLaptopMode → 1 pass; else up to SmoothingPasses (capped at 2).</summary>
    private void ApplyOrganicSmoothing(Voxels vox)
    {
        var sw = Stopwatch.StartNew();
        float d = OrbiConfig.SmoothingOffsetMM;
        int n = OrbiConfig.EnableLaptopMode ? 1 : Math.Min(Math.Max(0, OrbiConfig.SmoothingPasses), 2);
        Console.WriteLine($"- Step 7: Organic smoothing ({n}× offset +{d} / -{d} mm, laptop={OrbiConfig.EnableLaptopMode})...");

        for (int i = 0; i < n; i++)
        {
            vox.Offset(d);
            vox.Offset(-d);
        }
        sw.Stop();
        Console.WriteLine($"  [{sw.ElapsedMilliseconds} ms]");
    }

    private void Export(Voxels vox, int inputTriCount)
    {
        var sw = Stopwatch.StartNew();
        Console.WriteLine("- Step 8: Export...");

        vox.CalculateProperties(out float volumeMM3, out BBox3 bbox);
        Console.WriteLine($"  Volume: {volumeMM3:F2} mm³");

        Console.WriteLine("  Freeing memory before meshing...");
        GC.Collect();
        GC.WaitForPendingFinalizers();

        using (var msh = new Mesh(vox))
        {
            int outTriCount = msh.nTriangleCount();
            msh.SaveToStlFile(OrbiConfig.OutputStlFileName);
            sw.Stop();
            Console.WriteLine($"  Output: {OrbiConfig.OutputStlFileName}");
            Console.WriteLine($"  Triangles: {outTriCount} (input was {inputTriCount})");
            Console.WriteLine($"  [Export] {sw.ElapsedMilliseconds} ms");
            Console.WriteLine("✅ Done (watertight manifold from marching cubes).");
        }
    }
}
