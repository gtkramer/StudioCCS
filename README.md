# StudioCCS — CCS Model Viewer

> **Early alpha.** Many of the planned features are currently unimplemented, and
> behavior may be rough around the edges. Source code and/or a full write-up on
> the CCS format will be released eventually.

StudioCCS is a viewer and exporter for the **CCS** asset files used by the
`.hack` games on the PlayStation 2. It loads CCS files, lets you browse and
preview the data they contain, and can export models, textures, and dummies for
use in other tools such as Blender.

## Table of Contents

- [CCS Generations](#ccs-generations)
- [Requirements](#requirements)
- [Building & Running](#building--running)
- [Viewport Modes](#viewport-modes)
- [What's Inside a CCS File](#whats-inside-a-ccs-file)
  - [Clumps](#clumps)
  - [Materials](#materials)
  - [Textures](#textures)
  - [HitMeshes](#hitmeshes)
  - [Bounding Boxes](#bounding-boxes)
  - [Dummies](#dummies)
  - [Animations](#animations)
- [Controls](#controls)
  - [Display Options](#display-options)
- [Usage](#usage)
- [Exporting Models](#exporting-models)
  - [Dumping a Character / Monster / Anything with Bendy Parts](#dumping-a-character--monster--anything-with-bendy-parts)
  - [Notes on Ripping Gen 1 Models](#notes-on-ripping-gen-1-models)
- [Known Bugs & Limitations](#known-bugs--limitations)
- [FAQ](#faq)
- [License](#license)

## CCS Generations

CCS files are grouped into three "generations" based on the games they ship
with:

| Generation | Games                              |
| ---------- | ---------------------------------- |
| 1          | IMOQ / F                           |
| 2          | GU / Link                          |
| 3          | GU:LR (new or updated files)       |

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or newer
- A GPU/driver supporting **OpenGL 3.3** or higher (the app requests a 4.6
  context and falls back to a 3.3 floor)

StudioCCS is built on [Avalonia](https://avaloniaui.net/) and runs on Linux,
Windows, and macOS.

## Building & Running

```sh
# Restore dependencies and build
dotnet build

# Run the app
dotnet run
```

## Viewport Modes

There are three viewport modes — **Preview**, **Scene**, and **All** — selectable
from the toolbar. Each has its own independent camera, so changing the view in
one mode will not affect the others.

- **Preview mode** lists all the CCS files that have been loaded and categorizes
  the interesting stuff, rendering whichever item you select in the tree. See
  [What's Inside a CCS File](#whats-inside-a-ccs-file) for a rundown.
- **Scene mode** renders the animations you've added to the scene via the **Add
  to Scene** right-click option on an animation, so it's geared toward building
  up a scene rather than browsing a single file.
- **All mode** just attempts to render everything in all of the loaded CCS files
  it can, so it may or may not show you something coherent. Collision meshes,
  bounding boxes, dummies, and light helpers (toggled in the **View** menu) are
  drawn here.

## What's Inside a CCS File

Here's a brief rundown of the interesting things, just to get a sense of how the
CCS format is laid out.

### Clumps

A **Clump** is a list of **Object** nodes, which more or less form a skeletal
structure. Starting in Generation 2 files, clumps also store a list of Position,
Rotation, and Scale vectors for the bind pose of each Object. Each Object has a
slot for a Model and a Shadow Model; both slots are optional. Generation 2 files
appear to add additional parameters whose purpose is currently unknown.

Rudimentary support for visualizing Objects has been implemented. It can look
kinda buggy at times.

Each model may have 0 or more **Sub Models**. There are four main types of
model, spread across several different model type codes:

| Model Type       | Description                                                                                                                                                                                  |
| ---------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Rigid**        | Vertices are not deformable.                                                                                                                                                                  |
| **Deformable**   | Usually a list of sub models with single-weighted vertices, followed by a sub model containing all of the multi-weighted vertices, or "bendy parts".                                          |
| **Shadow**       | For projecting (or whatever black magic that is) shadows onto. Not currently rendered.                                                                                                        |
| **Morph Target** | Holds vertex positions for a morph target. As far as I can tell, morph-target animation is only supported on Rigid models. They usually have a material or texture assignment but no texture coordinates. Some Gen 2 files even have vertex colors. |

**Selecting items in the tree:**

- Clicking a **Clump** renders every Model (and Sub Model) for every Object
  listed in the clump.
- Clicking an **Object** or **Model** renders the Model (and its Sub Models) for
  that Object or Model.
- Clicking a **Sub Model** renders only that sub model.

> **Note:** When viewing Characters, Monsters, or anything that uses Deformable
> models, you may notice that most of the Object nodes have empty Models except
> for one near the end, which usually has "body" somewhere in its name. I believe
> this is because they grouped all of those sub models together to make sending
> them — and the matrices — through to the VU easier. In the future I may fix it
> so that clicking on the empty model renders the correct sub model.

### Materials

Materials contain a texture assignment, an alpha value, and a texture coordinate
offset. Generation 2 files appear to add additional parameters whose purpose is
currently unknown.

Clicking a Material in the Preview tree currently does nothing.

### Textures

Two texture types are known to be used in Generation 1 files:

- 4-bit indexed color
- 8-bit indexed color

An additional format has been spotted in Generation 2:

- 32-bit RGBA color

Generation 3 adds two more:

- DXT1
- DXT5

More texture types are apparently supported in Gen 1 & 2 games, but I've yet to
see them. If I or anyone else runs across them I'll implement them; otherwise,
support is low priority for now.

Other notes:

- Textures may contain mipmaps, but these are currently discarded.
- `.hack//Link` added support for non-power-of-two textures, but these are
  usually swizzled and are currently unsupported. I've only ever really seen
  them in the comic cutscene files anyway.

Clicking a Texture shows that texture mapped to a plane in the Preview viewport.

### HitMeshes

HitMeshes contain collision meshes. Each hit mesh might have a color assigned to
it, presumably for coloring a model that collides with it — a cheap way to do
shading with the baked lighting.

Clicking a HitMesh or SubMesh in the Preview tree currently does nothing.

### Bounding Boxes

Contain a Min and Max value, not much else.

Clicking a Bounding Box in the Preview tree currently does nothing, but they can
be drawn in All mode via **View → Draw Bounding Boxes**.

### Dummies

Dummies contain a position, and sometimes a rotational value. They're useful
for, well, being dummies that hold the position and rotation of things.

Clicking a Dummy in the Preview tree currently does nothing, but they are
visualized as little green wireframe boxes in All mode (toggle with **View →
Draw Dummies**).

### Animations

All Camera parameters, and all parameters but the type for Lights, are stored in
animations. Animations are a pretty complex topic, and support for them is
currently in progress.

- Clicking an Animation in the Preview tree renders it in the Preview viewport.
  Playback isn't implemented yet, so you'll only ever see its first frame.
- Right-clicking an Animation gives you two options:
  - **Add to Scene** adds it to the scene tree so it renders in **Scene mode**.
    Right-clicking it there offers **Remove** to take it back out.
  - **Set Pose** attempts to set everything referenced in its first frame. This
    will look buggy until animation support is implemented properly.

> There are many other types of objects contained in CCS files, but the ones
> listed above currently have higher priority for support.

## Controls

Right-click and drag in the Preview and Scene viewports to rotate the arcball
camera. Use the mouse wheel to zoom in and out.

| Action               | Input                                       |
| -------------------- | ------------------------------------------- |
| Rotate camera        | Right-click + drag                          |
| Zoom in / out        | Mouse wheel, or `-` / `+` on the number row |
| Move along Z axis    | `W` / `S`                                   |
| Move along X axis    | `A` / `D`                                   |
| Move along Y axis    | `X` / `Z`                                   |
| Reset camera to default view | `R`                                 |

The following modifier keys can be held while moving or zooming:

| Modifier | Effect                                                              |
| -------- | ------------------------------------------------------------------ |
| `Shift`  | Toggles between world-axis and camera-relative movement, and slows keyboard zoom for finer control |
| `Ctrl`   | In camera-relative movement, frees vertical motion that is otherwise locked to the horizontal plane |

Notes:

- `R` resets the camera of the active viewport only (each viewport has its own).
- By default, movement is camera-relative (and `Shift` flips it to world-axis).
  Enable **Options → Default to Axis Movement** to swap that around so plain
  movement is world-axis aligned and `Shift` gives you camera-relative instead.
- Each viewport has an independent camera. There may be an option to disable
  this in the future if it ends up becoming annoying.
- Keys cannot currently be remapped; support for that may be added in the future.

### Display Options

The **View** menu toggles what gets drawn in the viewport. These apply to the
active viewport's render:

| Toggle                            | Effect                                            |
| --------------------------------- | ------------------------------------------------- |
| Wireframe                         | Render models as wireframe                         |
| Vertex Colors                     | Shade models with their baked vertex colors        |
| Vertex Normals                    | Visualize model normals                            |
| Textured                          | Apply textures to models                           |
| Backface Culling                  | Cull back-facing triangles instead of drawing double-sided |
| Draw Grid                         | Show the ground grid                               |
| Draw Collision Meshes             | Draw collision meshes (All mode)                   |
| Draw Bounding Boxes               | Draw bounding boxes (All mode)                     |
| Draw Dummies                      | Draw dummies as green wireframe boxes (All mode)   |
| Draw Light Helpers                | Draw light-position helpers (All mode)             |
| Draw Axis Marker (Viewport)       | Show the orientation axis marker in the corner     |
| Draw Axis Marker (World Center)   | Show an axis marker at the world origin            |

## Usage

1. Use **File → Load CCS...** to load a CCS file. The Log Window will spit out any
   information it feels necessary, along with whether or not the file loaded
   successfully.
2. If the file loaded successfully, it will be added to the Preview tree under
   its **internal name**, not the file name.
3. To unload a file, right-click it in the Preview tree and select **Unload** (or
   **Unload All** to clear every loaded file at once).

Right-clicking a CCS file in the Preview tree also offers **View Info Report**,
which opens a window with a report of some information that may or may not be
useful to you. I use it for debugging — some information may be missing or
inaccurate, and more will be added as I feel like it.

Use **Scene → Dump to .OBJ** to dump all models out to an `.OBJ` file (with an
included `.MTL` file) and dump all textures to PNG.

> **Note:** DXT1 and DXT5 textures from Gen 3 files will dump to `.DDS`. I'll get
> around to writing a DXT1/5 decoder later. Maybe.

### Companion Blender Script

A companion Python script (`Extras/blenderDummyImport.py`) is included for
importing Dummies into Blender:

1. Open a text editor in Blender and open the script.
2. Hit `Alt` + `P`.
3. Go back to the 3D view and look at the tool shelf. You should see a new tab
   labeled **.hack** with a panel for importing the dummies.

This is useful for working with towns and stuff.

## Exporting Models

### Dumping a Character / Monster / Anything with Bendy Parts

Now, the part I'm sure you're really wanting to know.

1. **Load up the model.**

   - **Generation 1:** You'll see it collapsed into the center. Right-click an
     animation and click **Set Pose**. I suggest you choose one with the word
     **"nut"** in the name — these are idle animations, which should have the
     least amount of movement. That matters because the animations do much more
     than just rotate the bones. This will set the model into a weird pose, but
     that's okay; what we care about is the positions of the bones right now.
   - **Generation 2 & 3:** You'll see it in some weird-ass pose. This is
     (usually) fine. Apply an animation pose at your own risk — I've seen it
     stretch the shit out of some characters.

2. **Right-click the clump** in the Preview tree and select **Edit Bones**.

3. In the Bone Edit window, click the **Edit** menu and select **Clear Rotation
   Values**. This gives you the infamous `.hack`//Lawn Chair pose, which is much
   easier to work with than whatever random pose you had before.

4. **Work your way through the tree**, typing in rotation values and hitting the
   **Update** button.

   > I had originally intended to use spin box controls for adjusting the values,
   > but C# doesn't appear to have one that handles floating-point values, and it
   > felt like a time suck to code one myself. I may add sliders in a future
   > version.
   >
   > You may notice that you set a bone rotation axis to a nice round `90` and go
   > back to it later to see `90.0085` or something like that. This has to do with
   > the conversion between degrees and radians. You can keep trying to fix it,
   > but it's going to keep happening. Blame whoever taught us to think in
   > degrees.

5. **Optionally save your work.** The **Edit** menu of the Bone Edit window has
   two options:

   - **Save Pose** saves the current Position, Rotation, and Scale values for all
     bones in the clump to a binary file. This is literally just a flat dump of
     the raw values in bone order — no names, no matching algorithm.
   - **Load Pose** loads those values back. Again, it's just a flat dump, so
     loading a pose from one model onto another will probably not work how you
     want it to.

   I implemented these for debugging, but it's kind of nice if you export a model
   and later realize you messed something up and want to go back and fix it.

   > A **Load Matrix...** option is also available by right-clicking the clump in
   > the Preview tree. It's a separate debug tool that reads a flat binary dump of
   > one 4x4 transform matrix per bone and applies it to the clump directly — a
   > different format from the Save/Load Pose values above, so the two aren't
   > interchangeable.

6. **Export.** Once the model is posed how you like, click **Dump to .OBJ...** in
   the **Scene** menu of the main window and it'll dump the model out in that
   pose to `.OBJ` format. It *should* export the model exactly how you see it.

   > Aside from the also-famous flipped-triangle problem, that is. So far I
   > haven't found anything that fixes it 100% of the time. IMOQ/F and GU both
   > render everything double-sided, so it may be that there's just no way to fix
   > it. I'll take a look at this again later, but for now you're just going to
   > have to manually flip them yourself.

### Notes on Ripping Gen 1 Models

Cutscenes in Generation 1 are going to be a massive pain to rip from right now.
Their animation is stored differently, and there is currently no support for
loading that data. When I get rigged exporting and animations working, I'll work
on at least supporting enough of that data to make dumping those models easier.

For now, the only way to get those models out is to manually note the bone
positions for the in-game model and then set them for the cutscene model.
Otherwise you'll just eyeball it. Either way, it's going to be a massive pain.

## Known Bugs & Limitations

### Collision Meshes

- There's an odd bug with collision mesh rendering that was causing a
  `NullReferenceException`. This never happens when run through the debugger, so
  I haven't been able to fix it yet. As a result, only some of the collision sub
  meshes may render in All mode.
- Collision meshes do not render in Preview mode yet — haven't gotten around to
  adding that code in.

### Models

- Vertices are stored in local bone space. Speculation is that this is because of
  the half-float format, which — combined with a model's scaling value — lets
  them somewhat overcome the loss of precision.
- **Generation 1:** Deformable models will render, but due to the way the format
  works, everything collapses into the center by default. This is because the
  bind pose for the skeletons is non-existent.
- **Generation 2 & 3:** The parts may or may not be in the correct
  position/orientation.
- Shadow models do not render. There's really no need to, so this will probably
  remain unimplemented for now.
- All models will probably have the infamous flipped-triangles issue. The games
  render everything double-sided, so there may not be a reliable way to fix this.

### Textures

The texture preview just renders to a plane that can be rotated in the Preview
viewport. Not sure yet whether I'll change this to a view-aligned plane, but it
seems to work fine as is for now.

### Dummies

Dummies cannot currently be previewed, nor is there a way to tell which dummy is
which in All mode. They can be exported to a simple ASCII format for importing
into Blender with the included companion Python script.

### Bounding Boxes

Drawn in All mode (toggle with **View → Draw Bounding Boxes**), but not in
Preview or Scene mode, and they can't be selected or labeled.

### Animations

Cannot currently be played. There are some weird things happening with the
rotations, so until I get that sorted, there's no real animation support.

### Lights

Due to the way lights are implemented in the files, animation playback support is
needed to utilize them.

### Rendering

- Only some model types have vertex colors. In the games, the colors are used for
  baked lighting. For models with no vertex colors, they're set to
  `rgba(0.5, 0.5, 0.5, 1.0)`, which may make some models look funny or dark. May
  need to change the rendering to deal with this in the future.
- I believe model normals are read correctly, but there's currently no support
  for using them in either Preview or Scene mode. There's an option to export
  them to OBJ, but they mess with the lighting in Blender. More investigation is
  needed — it probably doesn't help that I'm not re-calculating them with bone
  transforms. Will work on that later.
- I had no idea dealing with transparency was such a pain. Texture transparency
  is all jacked up, and I'm not writing a super-advanced rendering engine just to
  make it look right. You're all just going to export this stuff into other game
  engines anyway, so it seems like more work than it's worth right now.

### Exporting

If exported vertices all end up with coordinates of `0, 0, 0`, try going into
Scene mode before exporting. There was a bug where the matrices weren't getting
updated properly, but it should be gone now.

### Viewports

- In both Preview and Scene mode, the mouse wheel may not work for zooming. I
  have no idea what causes this — it only happens on the one machine I have for
  testing but not development — which is why I added the zoom keys.
- Zooming is not very smooth right now. Will work on that in coming builds.

### General

- Some files will either fail to load or crash the program. I haven't checked
  compatibility with every single CCS file in existence. If you come across a
  file that doesn't load, let me know so I can look at it.
- Currently this thing only **reads** files — it cannot edit, replace, or inject
  things in them. Support for those features is planned, but the current priority
  is reading files properly.

## FAQ

**Q: X or Y file doesn't load!**
A: Not all files will currently load, though a vast majority will. Again, if you
come across one that doesn't read, let me know so I can look into it.

**Q: When is feature X or Y going to be added? When will source code be
released?**
A: When I get around to it.

**Q: Do you have a Patreon? PayPal donation?**
A: No, all that sounds like too much work. The more time I spend messing with
that, the less time I get to spend working on anything else. If you absolutely
must spend your money, go donate to a local no-kill animal shelter.

**Q: Why do some of the bones look so messed up?**
A: Long story short, visualizing the bones is a complex process and we don't
have all the data we need to do it properly. If a bone looks messed up, it's
because it's not rotated in the direction of its first child. If it's that big of
an issue, I'll make a setting to switch the bone rendering to nub mode.

## License

Released under the [MIT License](LICENSE). Copyright © 2017 NCDyson.
