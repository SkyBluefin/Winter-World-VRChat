# Winter World (VRChat)

A public VRChat world with a snowy village and a frozen forest to explore.

**Features planned:** falling snow, a campfire with benches, mirrors, seats, and a music player in the lodge.

## Plan

1. **Project setup and blockout** (in progress). Unity project via the VRChat Creator Companion, Git LFS, and a grey-box layout of the whole map.
2. **Art.** Blender scripts that generate low-poly cabins, pine trees, rocks and snowy terrain, plus free CC0 assets (Poly Haven, Kenney).
3. **Features.** Snow particles, campfire light and sound, mirror toggles, seats, music player, fog and lighting.
4. **Optimize and publish.** Baked lighting, occlusion culling, performance checks, then a public upload. PC first, with Quest-friendly budgets.

## Map layout

Units are meters. North is +Z. The playable area is 400 x 400 m.

```
                 N (+Z)
   +-----------------------------------+
   |  Frozen Forest            trees   |
   |   (Lookout)                       |
   |      \                            |
   |       \___   (Frozen Lake)        |
   |           \       |               |
   |            \______|               |
   |                   |  forest path  |
   |- - - - - - - - - -|- - - - - - - -|  forest starts at z = 45
   |        cabins  [square]--[Lodge]  |  campfire + benches in the square
   |                   |               |  mirror + music in the lodge
   |                   |               |
   |               (Spawn)             |  spawn at z = -70
   +-----------------------------------+
```

## Getting started

See [docs/SETUP.md](docs/SETUP.md) to create the Unity project and build the blockout scene.
