using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace WinterWorld.EditorTools
{
    /// <summary>
    /// Builds a grey-box blockout of the Winter World from Unity primitives.
    /// Run it from the menu: Winter World > Build Blockout Scene.
    /// Units are meters. +Z is north: the village sits in the middle, the spawn is to the south
    /// and the frozen forest fills the north half of the map.
    /// </summary>
    public static class BlockoutBuilder
    {
        const string ScenePath = "Assets/WinterWorld/Scenes/WinterWorld_Blockout.unity";
        const string MaterialFolder = "Assets/WinterWorld/Materials/Blockout";

        // Map layout
        const float MapHalfSize = 200f;
        const float SquareRadius = 15f;
        const float CabinRingRadius = 30f;
        static readonly Vector3 SpawnPosition = new Vector3(0f, 0f, -70f);
        static readonly Vector3 LodgePosition = new Vector3(55f, 0f, 0f);
        static readonly Vector3 LakeCenter = new Vector3(0f, 0f, 130f);
        const float LakeRadius = 22f;
        static readonly Vector3 LookoutCenter = new Vector3(-95f, 0f, 155f);
        const float LookoutRadius = 12f;

        // Paths are lists of waypoints on the XZ plane
        static readonly Vector2[] SpawnPath = { new Vector2(0f, -70f), new Vector2(0f, -SquareRadius) };
        static readonly Vector2[] LodgePath = { new Vector2(SquareRadius, 0f), new Vector2(LodgePosition.x - 8f, 0f) };
        static readonly Vector2[] LakePath =
        {
            new Vector2(0f, SquareRadius), new Vector2(0f, 40f), new Vector2(12f, 65f),
            new Vector2(-6f, 92f), new Vector2(0f, LakeCenter.z - LakeRadius)
        };
        static readonly Vector2[] LookoutPath =
        {
            new Vector2(-6f, 92f), new Vector2(-40f, 115f), new Vector2(-70f, 140f), new Vector2(LookoutCenter.x, LookoutCenter.z)
        };
        const float PathWidth = 4f;

        // Forest
        const int TreeCount = 320;
        const int TreeSeed = 1225;
        const float ForestStartZ = 45f;

        static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        [MenuItem("Winter World/Build Blockout Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Materials.Clear();
            EnsureFolder(MaterialFolder);
            EnsureFolder("Assets/WinterWorld/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SetupLighting();

            var root = new GameObject("Blockout").transform;
            BuildGround(root);
            BuildVillage(root);
            BuildPaths(root);
            BuildForest(root);
            BuildBoundary(root);
            var spawn = BuildSpawn(root);
            BuildSceneDescriptor(spawn);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Winter World] Blockout scene saved to " + ScenePath);
        }

        static void SetupLighting()
        {
            var sun = new GameObject("Sun (Directional Light)");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.93f, 0.85f);
            light.intensity = 0.9f;
            light.shadows = LightShadows.Soft;
            light.lightmapBakeType = LightmapBakeType.Mixed;
            // Low winter sun from the south-west
            sun.transform.rotation = Quaternion.Euler(22f, 40f, 0f);

            RenderSettings.sun = light;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.72f, 0.86f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.6f, 0.68f);
            RenderSettings.ambientGroundColor = new Color(0.75f, 0.78f, 0.82f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.78f, 0.84f, 0.9f);
            RenderSettings.fogDensity = 0.008f;
        }

        static void BuildGround(Transform root)
        {
            var ground = CreatePrimitive(PrimitiveType.Plane, "Ground (Snow)", root, "Snow", new Color(0.93f, 0.95f, 0.98f));
            // A Unity plane is 10 x 10 meters
            ground.transform.localScale = new Vector3(MapHalfSize / 5f, 1f, MapHalfSize / 5f);
        }

        static void BuildVillage(Transform root)
        {
            var village = new GameObject("Village").transform;
            village.SetParent(root, false);

            var square = CreateDisc("Village Square", village, Vector3.zero, SquareRadius, "Stone", new Color(0.55f, 0.56f, 0.6f));
            square.transform.localPosition += Vector3.up * 0.02f;

            // Campfire and log benches in the middle of the square
            var campfire = CreatePrimitive(PrimitiveType.Cylinder, "Marker_Campfire", village, "Fire", new Color(1f, 0.45f, 0.1f));
            campfire.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            campfire.transform.localScale = new Vector3(2f, 0.2f, 2f);
            UseMeshCollider(campfire);

            const int benchCount = 5;
            for (int i = 0; i < benchCount; i++)
            {
                float angle = (90f + i * 360f / benchCount) * Mathf.Deg2Rad;
                var pos = new Vector3(Mathf.Cos(angle) * 4.5f, 0.25f, Mathf.Sin(angle) * 4.5f);
                var bench = CreatePrimitive(PrimitiveType.Cube, "Marker_Seat_Bench " + (i + 1), village, "Wood", new Color(0.4f, 0.27f, 0.16f));
                bench.transform.localPosition = pos;
                bench.transform.localScale = new Vector3(3f, 0.5f, 0.6f);
                bench.transform.rotation = Quaternion.LookRotation(-new Vector3(pos.x, 0f, pos.z));
            }

            // Ring of cabins around the square, with gaps for the paths
            float[] cabinAngles = { 20f, 55f, 125f, 160f, 200f, 235f, 305f, 340f };
            for (int i = 0; i < cabinAngles.Length; i++)
            {
                float angle = cabinAngles[i] * Mathf.Deg2Rad;
                var pos = new Vector3(Mathf.Cos(angle) * CabinRingRadius, 0f, Mathf.Sin(angle) * CabinRingRadius);
                CreateBuilding("Cabin " + (i + 1), village, pos, new Vector3(7f, 4f, 9f), "Cabin");
            }

            // Lodge: the big indoor hangout with mirrors and the music player
            BuildLodge(village);
        }

        static void BuildLodge(Transform parent)
        {
            const float width = 18f, height = 7f, depth = 14f, wall = 0.3f, doorway = 3f;

            var lodge = new GameObject("Lodge").transform;
            lodge.SetParent(parent, false);
            lodge.position = LodgePosition;
            // The doorway (local +Z) faces the village square
            lodge.rotation = Quaternion.LookRotation(-new Vector3(LodgePosition.x, 0f, LodgePosition.z));

            var color = new Color(0.45f, 0.3f, 0.2f);
            float frontPiece = (width - doorway) / 2f;
            var walls = new[]
            {
                ("Wall Back", new Vector3(0f, height / 2f, -depth / 2f), new Vector3(width, height, wall)),
                ("Wall Left", new Vector3(-width / 2f, height / 2f, 0f), new Vector3(wall, height, depth)),
                ("Wall Right", new Vector3(width / 2f, height / 2f, 0f), new Vector3(wall, height, depth)),
                ("Wall Front Left", new Vector3(-(doorway + frontPiece) / 2f, height / 2f, depth / 2f), new Vector3(frontPiece, height, wall)),
                ("Wall Front Right", new Vector3((doorway + frontPiece) / 2f, height / 2f, depth / 2f), new Vector3(frontPiece, height, wall)),
            };
            foreach (var (name, position, scale) in walls)
            {
                var piece = CreatePrimitive(PrimitiveType.Cube, name, lodge, "Lodge", color);
                piece.transform.localPosition = position;
                piece.transform.localScale = scale;
            }

            var roof = CreatePrimitive(PrimitiveType.Cube, "Roof (Snow)", lodge, "Snow", new Color(0.93f, 0.95f, 0.98f));
            roof.transform.localPosition = new Vector3(0f, height + 0.15f, 0f);
            roof.transform.localScale = new Vector3(width + 1f, 0.3f, depth + 1f);

            var floor = CreatePrimitive(PrimitiveType.Cube, "Floor", lodge, "Wood", new Color(0.4f, 0.27f, 0.16f));
            floor.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            floor.transform.localScale = new Vector3(width - wall, 0.1f, depth - wall);

            var mirror = CreatePrimitive(PrimitiveType.Cube, "Marker_Mirror", lodge, "Mirror", new Color(0.7f, 0.9f, 1f));
            mirror.transform.localPosition = new Vector3(0f, 1.6f, -depth / 2f + 0.25f);
            mirror.transform.localScale = new Vector3(5f, 2.6f, 0.1f);

            var music = CreatePrimitive(PrimitiveType.Cube, "Marker_MusicPlayer", lodge, "Music", new Color(0.6f, 0.3f, 0.9f));
            music.transform.localPosition = new Vector3(width / 2f - 1.5f, 0.7f, -depth / 2f + 2f);
            music.transform.localScale = new Vector3(1.2f, 1.2f, 0.6f);
        }

        static void CreateBuilding(string name, Transform parent, Vector3 position, Vector3 size, string materialName)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            group.position = position;
            // Face the village square
            var toCenter = -new Vector3(position.x, 0f, position.z);
            if (toCenter.sqrMagnitude > 0.01f)
                group.rotation = Quaternion.LookRotation(toCenter);

            var body = CreatePrimitive(PrimitiveType.Cube, "Body", group, materialName, new Color(0.45f, 0.3f, 0.2f));
            body.transform.localPosition = new Vector3(0f, size.y / 2f, 0f);
            body.transform.localScale = size;

            var roof = CreatePrimitive(PrimitiveType.Cube, "Roof (Snow)", group, "Snow", new Color(0.93f, 0.95f, 0.98f));
            roof.transform.localPosition = new Vector3(0f, size.y + 0.15f, 0f);
            roof.transform.localScale = new Vector3(size.x + 0.8f, 0.3f, size.z + 0.8f);

            var door = CreatePrimitive(PrimitiveType.Cube, "Door", group, "Door", new Color(0.25f, 0.15f, 0.08f));
            door.transform.localPosition = new Vector3(0f, 1.1f, size.z / 2f + 0.05f);
            door.transform.localScale = new Vector3(1.4f, 2.2f, 0.1f);
            Object.DestroyImmediate(door.GetComponent<Collider>());
        }

        static void BuildPaths(Transform root)
        {
            var paths = new GameObject("Paths").transform;
            paths.SetParent(root, false);
            BuildPath("Path_Spawn_To_Square", paths, SpawnPath);
            BuildPath("Path_Square_To_Lodge", paths, LodgePath);
            BuildPath("Path_Square_To_Lake", paths, LakePath);
            BuildPath("Path_Lake_To_Lookout", paths, LookoutPath);
        }

        static void BuildPath(string name, Transform parent, Vector2[] points)
        {
            var path = new GameObject(name).transform;
            path.SetParent(parent, false);
            for (int i = 0; i < points.Length - 1; i++)
            {
                var a = new Vector3(points[i].x, 0.03f, points[i].y);
                var b = new Vector3(points[i + 1].x, 0.03f, points[i + 1].y);
                var segment = CreatePrimitive(PrimitiveType.Cube, "Segment " + (i + 1), path, "Path", new Color(0.78f, 0.74f, 0.68f));
                segment.transform.position = (a + b) / 2f;
                segment.transform.rotation = Quaternion.LookRotation(b - a);
                // Overlap segments a little so corners have no gaps
                segment.transform.localScale = new Vector3(PathWidth, 0.04f, Vector3.Distance(a, b) + PathWidth);
                Object.DestroyImmediate(segment.GetComponent<Collider>());
            }
        }

        static void BuildForest(Transform root)
        {
            var forest = new GameObject("Frozen Forest").transform;
            forest.SetParent(root, false);

            CreateDisc("Frozen Lake", forest, LakeCenter + Vector3.up * 0.05f, LakeRadius, "Ice", new Color(0.65f, 0.85f, 0.95f));
            CreateDisc("Lookout Clearing", forest, LookoutCenter + Vector3.up * 0.04f, LookoutRadius, "Path", new Color(0.78f, 0.74f, 0.68f));

            var trees = new GameObject("Trees").transform;
            trees.SetParent(forest, false);

            var random = new System.Random(TreeSeed);
            int placed = 0;
            int attempts = 0;
            while (placed < TreeCount && attempts < TreeCount * 20)
            {
                attempts++;
                float x = Lerp(-MapHalfSize + 8f, MapHalfSize - 8f, (float)random.NextDouble());
                float z = Lerp(ForestStartZ, MapHalfSize - 8f, (float)random.NextDouble());
                var p = new Vector2(x, z);

                if (Vector2.Distance(p, new Vector2(LakeCenter.x, LakeCenter.z)) < LakeRadius + 5f) continue;
                if (Vector2.Distance(p, new Vector2(LookoutCenter.x, LookoutCenter.z)) < LookoutRadius + 3f) continue;
                if (DistanceToPath(p, LakePath) < PathWidth + 2f) continue;
                if (DistanceToPath(p, LookoutPath) < PathWidth + 2f) continue;

                float height = Lerp(6f, 14f, (float)random.NextDouble());
                CreateTree("Tree " + (placed + 1), trees, new Vector3(x, 0f, z), height);
                placed++;
            }
        }

        static void CreateTree(string name, Transform parent, Vector3 position, float height)
        {
            var tree = new GameObject(name).transform;
            tree.SetParent(parent, false);
            tree.position = position;

            var trunk = CreatePrimitive(PrimitiveType.Cylinder, "Trunk", tree, "Bark", new Color(0.3f, 0.2f, 0.13f));
            trunk.transform.localPosition = new Vector3(0f, height * 0.15f, 0f);
            trunk.transform.localScale = new Vector3(0.6f, height * 0.15f, 0.6f);

            var canopy = CreatePrimitive(PrimitiveType.Capsule, "Canopy", tree, "Pine", new Color(0.16f, 0.32f, 0.26f));
            canopy.transform.localPosition = new Vector3(0f, height * 0.6f, 0f);
            canopy.transform.localScale = new Vector3(height * 0.35f, height * 0.4f, height * 0.35f);
            Object.DestroyImmediate(canopy.GetComponent<Collider>());
        }

        static void BuildBoundary(Transform root)
        {
            var boundary = new GameObject("Boundary").transform;
            boundary.SetParent(root, false);
            float size = MapHalfSize * 2f;
            var walls = new[]
            {
                (new Vector3(0f, 5f, MapHalfSize), new Vector3(size, 10f, 1f)),
                (new Vector3(0f, 5f, -MapHalfSize), new Vector3(size, 10f, 1f)),
                (new Vector3(MapHalfSize, 5f, 0f), new Vector3(1f, 10f, size)),
                (new Vector3(-MapHalfSize, 5f, 0f), new Vector3(1f, 10f, size)),
            };
            for (int i = 0; i < walls.Length; i++)
            {
                var wall = CreatePrimitive(PrimitiveType.Cube, "Wall " + (i + 1), boundary, "Boundary", new Color(0.4f, 0.45f, 0.5f));
                wall.transform.position = walls[i].Item1;
                wall.transform.localScale = walls[i].Item2;
            }
        }

        static Transform BuildSpawn(Transform root)
        {
            var spawn = new GameObject("SpawnPoint").transform;
            spawn.SetParent(root, false);
            spawn.position = SpawnPosition + Vector3.up * 0.1f;
            // Face the village
            spawn.rotation = Quaternion.LookRotation(Vector3.forward);

            var pad = CreateDisc("Spawn Pad", spawn, spawn.position, 3f, "Spawn", new Color(0.3f, 0.55f, 1f));
            pad.transform.position = SpawnPosition + Vector3.up * 0.04f;
            return spawn;
        }

        static void BuildSceneDescriptor(Transform spawn)
        {
            var world = new GameObject("VRCWorld");
            world.transform.position = spawn.position;
#if VRC_SDK_VRCSDK3
            var descriptor = world.AddComponent<VRC.SDK3.Components.VRCSceneDescriptor>();
            descriptor.spawns = new[] { spawn };
            descriptor.RespawnHeightY = -50f;
#else
            Debug.LogWarning("[Winter World] VRChat Worlds SDK not found. Add a VRCSceneDescriptor to the VRCWorld object by hand.");
#endif
        }

        // ---------- Helpers ----------

        static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, string materialName, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.GetComponent<Renderer>().sharedMaterial = GetMaterial(materialName, color);
            GameObjectUtility.SetStaticEditorFlags(go,
                StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            return go;
        }

        /// <summary>A flat cylinder with a mesh collider, since the default capsule collider does not fit a flat shape.</summary>
        static GameObject CreateDisc(string name, Transform parent, Vector3 position, float radius, string materialName, Color color)
        {
            var disc = CreatePrimitive(PrimitiveType.Cylinder, name, parent, materialName, color);
            disc.transform.position = position;
            disc.transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);
            UseMeshCollider(disc);
            return disc;
        }

        static void UseMeshCollider(GameObject go)
        {
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var collider = go.AddComponent<MeshCollider>();
            collider.sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
        }

        static Material GetMaterial(string name, Color color)
        {
            if (Materials.TryGetValue(name, out var cached))
                return cached;

            string path = MaterialFolder + "/Blockout_" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            material.SetFloat("_Glossiness", name == "Ice" || name == "Mirror" ? 0.85f : 0.1f);
            EditorUtility.SetDirty(material);
            Materials[name] = material;
            return material;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }

        static float DistanceToPath(Vector2 p, Vector2[] points)
        {
            float best = float.MaxValue;
            for (int i = 0; i < points.Length - 1; i++)
            {
                var a = points[i];
                var b = points[i + 1];
                var ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
            }
            return best;
        }

        static float Lerp(float a, float b, float t) => a + (b - a) * t;
    }
}
