# Analyse PicoGK Raccoon Edition — Voxels & Geometrie

Volledige analyse van de PicoGK-stack (C# API + native OpenVDB-kernel) met suggesties om voxels en geometrie nog beter te maken.

---

## 1. Architectuuroverzicht

| Laag | Wat | Waar |
|------|-----|------|
| **Applicatie** | `Program.cs` (Raccoon test) | Deze repo |
| **C# API** | `Voxels`, `Mesh`, `Lattice`, `Library`, … | PicoGK-Main (project reference) |
| **Native runtime** | OpenVDB-gebaseerde kernel | `libpicogk.1.7.so` (Linux) |
| **Coördinaten** | Alles in **mm**; voxelgrootte vast bij `Library.Go(fVoxelSizeMM)` | Globale `Library.fVoxelSizeMM` |

De voxelgrootte is **éénmaal** bij opstart vastgelegd; er is geen per-object of adaptieve voxelgrootte in de huidige API.

---

## 2. Huidige voxel-capaciteiten

### 2.1 Creëren van voxelvelden

- **Leeg veld**: `new Voxels()`
- **Kopie**: `new Voxels(vox)` / `voxDuplicate()`
- **Van mesh**: `new Voxels(msh)` → `RenderMesh(msh)`
- **Van Lattice**: `new Voxels(lat)` → `RenderLattice(lat)` (spheres, beams)
- **Van implicit (SDF)**: `new Voxels(iimplicit, bounds)` of `new Voxels(iBoundedImplicit)`
- **Van ScalarField**: `new Voxels(scalarField)`
- **Primitieven**: `Voxels.voxSphere(center, radius)` (via Lattice + RenderLattice)
- **Combinatie**: `Voxels.voxCombine(vox1, vox2)` / `voxCombineAll(avoxList)`

### 2.2 Boolean & bewerkingen

- **Union**: `BoolAdd` / `voxBoolAdd` / `+` operator
- **Smooth union**: `BoolAddSmooth(fSmoothDistance)` (native)
- **Subtract**: `BoolSubtract` / `voxBoolSubtract` / `-`
- **Intersect**: `BoolIntersect` / `voxBoolIntersect` / `&`
- **Trim**: `Trim(BBox3)` (intersect met bbox)

### 2.3 Offset & oppervlakte

- **Enkelvoudig**: `Offset(fDistMM)` / `voxOffset` (positief = naar buiten)
- **Dubbel**: `DoubleOffset(f1, f2)` / `voxDoubleOffset`
- **Triple (smoothen)**: `TripleOffset(fDistMM)` / `voxSmoothen` — verwijdert detail onder drempel
- **OverOffset / Fillet**: `OverOffset(fFirst, fFinal)` / `Fillet(fRoundingMM)` / `voxFillet`
- **Shell**: `voxShell(fOffset)` of `voxShell(fNeg, fPos, fSmoothInner)`

### 2.4 Filters (experimenteel in API)

- **Gaussian**, **Median**, **Mean** met kernelgrootte in mm

### 2.5 Implicit & slicing

- **Render implicit**: `RenderImplicit(iimplicit, bbox)` (overschrijft inhoud)
- **Intersect implicit**: `IntersectImplicit(iimp)` / `voxIntersectImplicit` (mask met bestaande voxels)
- **Z-slice projectie**: `ProjectZSlice(fStartZMM, fEndZMM)`

### 2.6 Query’s

- **Dimensies**: `GetVoxelDimensions` (origin + size in voxels)
- **Slices**: `GetVoxelSlice`, `GetInterpolatedVoxelSlice` (+ ESliceMode: SignedDistance, BlackWhite, Antialiased)
- **Volume & bbox**: `CalculateProperties(out volume, out bbox)`
- **Oppervlak**: `vecSurfaceNormal`, `bClosestPointOnSurface`, `bRayCastToSurface`
- **Vergelijken**: `bIsEqual(voxOther)`

### 2.7 I/O

- **OpenVDB**: `Voxels.voxFromVdbFile(path)`, `VdbFile` om te schrijven

---

## 3. Huidige mesh-capaciteiten

- **Van voxels**: `new Mesh(vox)` — native marching cubes (of equivalent) op level set
- **Lege mesh**: `new Mesh()` + `nAddVertex` / `nAddTriangle`
- **Transformaties**: `mshCreateTransformed(scale, offset)`, `mshCreateTransformed(matrix)`, `mshCreateMirrored`
- **Mesh → voxels (hol)**: `msh.voxVoxelizeHollow(fThickness)` via `ImplicitMesh` (per-driehoek SDF)
- **I/O**: o.a. `SaveToStlFile`

De kern van het meshen zit in de native library (`Mesh_hCreateFromVoxels`); kwaliteit en opties (bijv. adaptiviteit, decimation) zijn daar bepaald.

---

## 4. Sterke punten

1. **OpenVDB** — sparse, hierarchische voxels; efficiënt voor grote volumes.
2. **Eénduidige eenheden** — alles in mm; voxelgrootte expliciet.
3. **Rijke voxel-API** — booleans, offset, shell, fillet, triple-offset smoothing, implicit intersect.
4. **SDF-gericht** — level set / signed distance; geschikt voor clean meshing en offset.
5. **Lattice** — snelle primitieven (sphere, beam) voor concepten en basisvormen.
6. **C#-kant** — immutable-style helpers (`voxOffset`, `voxShell`, …) naast in-place (`Offset`, …).

---

## 5. Suggesties om PicoGK nog beter te maken (voxels & geometrie)

### 5.1 Voxelgrootte & resolutie

- **Probleem**: Eén globale `fVoxelSizeMM`; geen adaptieve of lokale resolutie.
- **Suggesties**:
  - **Documenteer** in README/FAQ: aanbevolen bandbreedte (bijv. 0.1–1.0 mm) en impact op geheugen/tijd.
  - **Optioneel (native)**: Ondersteuning voor *adaptieve* voxelgrootte (zoals OpenVDB level sets) waar de kernel het toelaat.
  - **C#-helper**: Kleine utility die voor een gegeven `BBox3` en gewenste “voxels per mm” een geschikte `fVoxelSizeMM` voorstelt en waarschuwt bij extreem hoge resolutie.

### 5.2 Meer primitieven & Lattice-uitbreiding

- **Nu**: Alleen `AddSphere` en `AddBeam` op de Lattice.
- **Suggesties**:
  - **C#-primitieven** (als `IImplicit`/`IBoundedImplicit`): bv. `voxBox`, `voxCylinder`, `voxCone` (allemaal via bestaande `RenderImplicit` + bbox), zodat gebruikers geen eigen implicit hoeven te schrijven.
  - **Lattice (native of C#)**: `AddCylinder`, `AddBox`, `AddCone` — handig voor snelle conceptmodellen en tests.

### 5.3 Mesh → Voxels (voxelisatie)

- **Nu**: `new Voxels(msh)` voor solid; `voxVoxelizeHollow(fThickness)` voor hol (via `ImplicitMesh`).
- **Suggesties**:
  - **Dikte 0** of zeer dunne shells: duidelijke docs of aparte pad (bijv. “surface voxelization” vs “solid”) om verrassingen te voorkomen.
  - **Performance**: De gecommentarieerde parallelle voxelisatie per driehoek (in `PicoGK_TriangleVoxelization.cs`) is een goed idee voor een toekomstige multithreaded kernel; als de native kant ooit per-triangle of per-chunk werkt, kan de C#-wrapper dat weer aanbieden.

### 5.4 Meshing (voxels → mesh)

- **Nu**: Eén pad: `new Mesh(vox)`; geen zichtbare parameters (bijv. iso-waarde, adaptatie).
- **Suggesties**:
  - **Documentatie**: Welk algoritme (marching cubes, dual contouring, …) en welke default iso-waarde de native library gebruikt.
  - **Optioneel (native + C#)**: Parameters voor mesh-kwaliteit (bijv. target edge length, decimation, smoothing) als de kernel ze ondersteunt.
  - **C#-postprocessing**: Hulpklassen of voorbeelden voor mesh-cleanup (duplicate vertices, degenerate triangles) als dat nog niet in de kernel zit.

### 5.5 Implicit- en SDF-gebruik

- **Nu**: `IImplicit` / `IBoundedImplicit` met `RenderImplicit` en `IntersectImplicit`; goede basis.
- **Suggesties**:
  - **Voorbeelden**: Gyroid, Schwarz P, torus, box, cylinder als `IImplicit` in voorbeelden of een kleine “PicoGK.Primitives” helper.
  - **Bounding box**: In docs benadrukken dat een strakke `oBounds` voor `RenderImplicit` belangrijk is voor snelheid en kwaliteit.
  - **C#-utilities**: `Utils` of aparte class met standaard-bounds voor veel gebruikte primitieven (sphere, box, cylinder).

### 5.6 Performance & geheugen

- **Copy-avoidance**: Waar mogelijk in-place gebruiken (`BoolAdd`, `Offset`) in plaats van `voxBoolAdd`/`voxOffset` als je het origineel toch weggooit.
- **Dispose**: `Voxels`/`Mesh`/`Lattice` zijn `IDisposable`; in lange pipelines expliciet `using` of `Dispose()` om native handles tijdig vrij te geven.
- **Grote volumes**: Bounds trimmen met `Trim(oBox)` vóór dure operaties; werken in subregions (als je dat in je ontwerp kunt inbouwen).

### 5.7 API-consistentie & ergonomie

- **Library.Go vs headless**: De README noemt een 5e parameter `false` om de viewer uit te zetten; de huidige `Library.Go` in PicoGK-Main heeft geen `bShowViewer`. Voor headless:
  - Of **`using (new Library(fVoxelSizeMM)) { ... }`** gebruiken (geen viewer),
  - Of een **overload** `Go(..., bool bShowViewer = true)` toevoegen die de viewer niet opent als `bShowViewer == false`.
- **Naming**: Overal `vox*` voor “return new Voxels” en in-place zonder prefix is al consistent; dat zo houden.
- **Units**: Overal “MM” in parameter- en doc-tekst houden; eventueel in README één keer een “Units: mm” sectie.

### 5.8 Geometrische kwaliteit

- **Sluitende meshes**: Bij `new Voxels(msh)` moet de mesh gesloten zijn; dit in docs en (indien mogelijk) in foutmeldingen benoemen.
- **Shells**: Bij `voxShell` met kleine offsets kunnen voxel-artefacten optreden; aanbeveling in docs: voxelgrootte klein genoeg ten opzichte van wanddikte (bijv. minstens 2–3 voxels voor wanddikte).
- **Fillet/OverOffset**: Duidelijke uitleg wanneer je `voxFillet` vs `voxOverOffset` gebruikt (leesbaarheid vs flexibiliteit).

### 5.9 Test & validatie (Raccoon Edition)

- **Program.cs**:
  - Naast een sphere: een **box** (via `Utils.mshCreateCube` + `new Voxels(msh)`) of een **Lattice** met sphere + beam.
  - Een **shell** maken (`vox.voxShell(2f).mshAsMesh().SaveToStlFile(...)`) om de pipeline te testen.
  - Optioneel: **volume/bbox** printen via `vox.CalculateProperties(out vol, out bbox)` om te controleren of waarden kloppen.
- **CI**: Eenvoudige test die `Library.Go` of headless `Library` aanroept, een klein voxelobject maakt, naar mesh gaat en controleert op o.a. `nTriangleCount() > 0` en (optioneel) bounds binnen verwachting.

### 5.10 Documentatie in deze repo

- **README**: Korte sectie “Voxels & resolutie” met aanbevolen `fVoxelSizeMM` en link naar dit analyse-document.
- **Headless**: Eenduidige uitleg: “Zonder viewer: `using (new PicoGK.Library(0.5f)) { ... }`” en eventueel toekomstige `Go(..., false)`.
- **Dependencies**: Vermelding dat de geometriekernel op **OpenVDB** (en TBB, etc.) leunt; handig voor wie de native .so zelf bouwt.

---

## 6. Samenvatting prioriteiten

| Prioriteit | Actie |
|-----------|--------|
| **Hoog** | Headless-gebruik documenteren of `Library.Go(..., bShowViewer)` toevoegen; README en dependency-uitleg bijwerken. |
| **Hoog** | Aanbevolen voxelgrootte en impact op geheugen/performance in docs zetten. |
| **Midden** | C#-primitieven (box, cylinder, cone) als implicit of via Lattice; voorbeelden voor Gyroid/box/cylinder. |
| **Midden** | Program.cs uitbreiden met shell + eventueel volume/bbox-check; optionele basis-CI. |
| **Laag** | Native mesh-parameters (als beschikbaar); adaptieve voxelgrootte (als de kernel het ondersteunt). |

Deze analyse is gebaseerd op de PicoGK C# API (PicoGK-Main) en het gebruik in de Raccoon Edition; de native kernel (`.so`) wordt als black box beschouwd. Aanpassingen in de C++/OpenVDB-kant zouden extra mogelijkheden kunnen geven (betere meshing, adaptieve resolutie, meer Lattice-primitieven); die zijn het meest impactvol als ze in de officiële PicoGK-kernel worden toegevoegd en daarna in de Raccoon-runtime worden meegenomen.
