using PicoGK;
using System.Numerics;

// -----------------------------------------------------------------------
// ORBI V1 – Single hollow manifold, airflow-ready (atomic subtract + vents)
// -----------------------------------------------------------------------
// 1. voxOuter = load Orbi-flow-Structuurv3.stl; voxInner = voxOuter.voxOffset(-5mm); voxOuter.BoolSubtract(voxInner).
// 2. Punch vents: chamber (0,0,20) cylinder R=25 UP; 3 tube ends (45,angle,75) spheres R=10.
// 3. voxFinal = voxShell.BoolAdd(voxOuter). Z-alignment: Chamber at Z=20.
// 4. VoxelSize 0.25f, GC, save Orbi_V1_Final_Airflow_Ready.stl.

const float VoxelSizeMM = 0.25f;
const float WallErodeMM = 5f;

const float ChamberZ = 20f;
const float BallR = 45f;
const float BallZ = 75f;

const string FlowStlPath = "ref.files/Orbi-flow-Structuurv3.stl";
const string ShellStlPath = "Orbi_V1_Final_Revised.stl";

Library.Go(VoxelSizeMM, () =>
{
    Console.WriteLine("🦝 Orbi V1 – Airflow Ready (single hollow manifold)");
    Library.Log("Voxel size (mm): " + VoxelSizeMM);

    try
    {
        // ---------- 1. Load outer, erode copy, atomic subtract (one hollow object) ----------
        Console.WriteLine("- Step A: voxOuter = voxMesh(Orbi-flow-Structuurv3.stl)...");
        using (Mesh mshOuter = Mesh.mshFromStlFile(FlowStlPath))
        {
            if (mshOuter.nTriangleCount() == 0)
            {
                Console.WriteLine("❌ STL empty or failed: " + FlowStlPath);
                return;
            }
            Console.WriteLine("  Triangles: " + mshOuter.nTriangleCount());

            using (Voxels voxOuter = new Voxels(mshOuter))
            {
                voxOuter.CalculateProperties(out _, out BBox3 outerBBox);
                Library.Log($"  Outer bbox: Z {outerBBox.vecMin.Z:F1} .. {outerBBox.vecMax.Z:F1}");

                Console.WriteLine("- Step B: voxInner = voxOuter.voxOffset(-5mm), voxOuter.BoolSubtract(voxInner)...");
                using (Voxels voxInner = voxOuter.voxOffset(-WallErodeMM))
                {
                    voxOuter.BoolSubtract(voxInner);
                }

                // ---------- 2. Punch air-vents (yellow points) ----------
                Console.WriteLine("- Step C: Chamber cylinder R=25 UP; 3 tube-end spheres R=10...");
                float chamberCylCenterZ = (ChamberZ + 100f) * 0.5f;
                float chamberCylHeight = 100f - ChamberZ + 10f;
                using (Voxels voxChamberVent = Voxels.voxCylinder(new Vector3(0f, 0f, chamberCylCenterZ), 25f, chamberCylHeight))
                    voxOuter.BoolSubtract(voxChamberVent);

                float deg120 = MathF.PI * 2f / 3f;
                for (int i = 0; i < 3; i++)
                {
                    float a = i * deg120;
                    Vector3 tubeEnd = new Vector3(BallR * MathF.Cos(a), BallR * MathF.Sin(a), BallZ);
                    using (Voxels voxCap = Voxels.voxSphere(tubeEnd, 10f))
                        voxOuter.BoolSubtract(voxCap);
                }

                // ---------- 3. Merge with shell ----------
                Voxels voxFinal;
                if (File.Exists(ShellStlPath))
                {
                    Console.WriteLine("- Step D: voxFinal = voxShell.BoolAdd(voxOuter)...");
                    using (Mesh mshShell = Mesh.mshFromStlFile(ShellStlPath))
                    {
                        if (mshShell.nTriangleCount() > 0)
                        {
                            using (Voxels voxShell = new Voxels(mshShell))
                            {
                                voxFinal = new Voxels(voxShell);
                                voxFinal.BoolAdd(voxOuter);
                            }
                        }
                        else
                        {
                            voxFinal = new Voxels(voxOuter);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("- Step D: Shell not found, voxFinal = voxOuter.");
                    voxFinal = new Voxels(voxOuter);
                }

                using (voxFinal)
                {
                    voxFinal.CalculateProperties(out float volumeMM3, out BBox3 bbox);
                    Console.WriteLine($"  Volume: {volumeMM3:F2} mm³");

                    Console.WriteLine("Freeing memory before meshing...");
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    using (Mesh msh = new Mesh(voxFinal))
                    {
                        msh.SaveToStlFile("Orbi_V1_Final_Airflow_Ready.stl");
                        Console.WriteLine("✅ Orbi_V1_Final_Airflow_Ready.stl saved.");
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ ERROR: {ex.Message}");
        Library.Log("Error: " + ex.Message);
    }
},
".",
"PicoGK.log",
bShowViewer: false);
