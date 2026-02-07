using PicoGK;
using System.Numerics;

// -----------------------------------------------------------------------
// PICO GK - RACCOON EDITION TEST
// -----------------------------------------------------------------------

// We gebruiken hier maar 4 argumenten:
// 1. Voxel grootte (0.5f)
// 2. De functie (() => { ... })
// 3. Log map (".")
// 4. Log bestandsnaam ("PicoGK.log")
// GEEN 5e argument!

Library.Go(0.5f, () => 
{
    Console.WriteLine("🦝 PicoGK Raccoon Edition - Linux Native Test");
    
    try 
    {
        // 1. Maak een simpele bol (Radius 50mm)
        Console.WriteLine("- Genereren Test Bol...");
        Voxels vox = Voxels.voxSphere(Vector3.Zero, 50f);

        // 2. Zet om naar Mesh
        Console.WriteLine("- Meshing...");
        Mesh msh = new Mesh(vox);

        // 3. Sla op
        Console.WriteLine("- Opslaan...");
        msh.SaveToStlFile("Raccoon_Test_Sphere.stl");
        
        Console.WriteLine("✅ SUCCES! Raccoon_Test_Sphere.stl is gemaakt.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ ERROR: {ex.Message}");
    }

}, 
".", 
"PicoGK.log");