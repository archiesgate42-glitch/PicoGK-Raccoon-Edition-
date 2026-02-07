using PicoGK;
using System.Numerics;

Library.Go(0.5f, () => 
{
    Console.WriteLine("🦝 PicoGK Raccoon Edition - Linux Native Test");
    
    // Een simpele test-bol om te bewijzen dat de .so motor werkt
    Voxels vox = Voxels.voxSphere(Vector3.Zero, 50f);
    Mesh msh = new Mesh(vox);
    msh.SaveToStlFile("Raccoon_Test_Cube.stl");
    
    Console.WriteLine("✅ Engine works! STL generated.");
}, 
".", "PicoGK.log", false); // Headless mode voor Linux servers