# Setup

This gets you from nothing to a walkable blockout of the Winter World in Unity.

## 1. Install the tools

1. Install [Git](https://git-scm.com/downloads) and [Git LFS](https://git-lfs.com/). Then run `git lfs install` once.
2. Install the [VRChat Creator Companion (VCC)](https://vrchat.com/home/download).
3. Open the VCC. It will offer to install Unity Hub and the exact Unity version VRChat needs. Accept, and use that version for this project.
4. Install [Blender](https://www.blender.org/download/) (needed from step 2 of the plan, not yet).

## 2. Create the Unity project and connect it to this repo

1. In the VCC, click **Create New Project**, pick the **Worlds** template, name it `Winter-World-VRChat`, and create it.
2. Open a terminal in the new project folder and connect it to this repo (after the setup pull request is merged into `main`):

   ```
   git init
   git remote add origin https://github.com/SkyBluefin/Winter-World-VRChat.git
   git fetch origin
   git checkout -f main
   ```

   This pulls the repo files (including `Assets/WinterWorld`) into your project without touching Unity's own files.
3. Commit Unity's project files so the repo becomes a complete project:

   ```
   git add .
   git commit -m "Add Unity project files"
   git push -u origin main
   ```

## 3. Build the blockout scene

1. Open the project from the VCC (**Open Project**).
2. In Unity's top menu, click **Winter World > Build Blockout Scene**.
3. The script creates and saves `Assets/WinterWorld/Scenes/WinterWorld_Blockout.unity`.

What you get:

- Snowy ground, 400 x 400 m, with boundary walls.
- The village: a stone square with a campfire marker and five benches, eight cabins around it, and a lodge with a doorway.
- Inside the lodge: markers for the mirror and the music player.
- Paths from the spawn to the square, from the square to the lodge, north into the forest to a frozen lake, and on to a lookout clearing.
- About 320 placeholder pine trees, kept clear of the paths and the lake.
- Winter fog, ambient light and a low sun.
- A `VRCWorld` object with the spawn point set (if the Worlds SDK is installed).

Objects named `Marker_...` show where features from step 3 go. Running the menu item again rebuilds the scene from scratch, so don't hand-edit the blockout scene. Change the numbers at the top of `Assets/WinterWorld/Editor/BlockoutBuilder.cs` instead.

## 4. Walk around

Open **VRChat SDK > Show Control Panel > Builder** and use **Build & Test** to walk the map in VRChat. You need to be signed in to a VRChat account that can upload worlds.
