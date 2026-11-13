using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cove
{
    public class Interactable : MonoBehaviour
    {
        public string prompt = "Interact";
        public Action onInteract;

        public void Interact()
        {
            if (onInteract != null) onInteract();
        }
    }

    public static class MeshGen
    {
        public static float IslandHeight(float R, float H, float x, float z, int seed)
        {
            float r = Mathf.Sqrt(x * x + z * z) / R;
            if (r >= 1f) return 0f;
            float dome = H * Mathf.Pow(1f - r * r, 1.5f);
            float n = Mathf.PerlinNoise(x * 0.18f + seed * 7.3f, z * 0.18f + seed * 3.1f) - 0.5f;
            float roll = Mathf.PerlinNoise(x * 0.07f + seed, z * 0.07f + seed * 2f) - 0.5f;
            return dome + (n * 0.45f + roll * 0.7f) * (1f - r) * Mathf.Clamp01(H * 0.35f);
        }

        public static Mesh IslandMesh(float R, float H, int seed, float outerRadius, int rings, int segments)
        {
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            for (int ring = 0; ring <= rings; ring++)
            {
                float f = (float)ring / rings;
                float radius = Mathf.Lerp(0f, outerRadius, f);
                for (int s = 0; s < segments; s++)
                {
                    float a = (float)s / segments * Mathf.PI * 2f;
                    float x = Mathf.Cos(a) * radius;
                    float z = Mathf.Sin(a) * radius;
                    float y;
                    if (radius <= R)
                    {
                        y = IslandHeight(R, H, x, z, seed);
                    }
                    else
                    {
                        y = -(radius - R) * 1.4f;
                    }
                    verts.Add(new Vector3(x, y, z));
                    uvs.Add(new Vector2((float)s / segments, f));
                }
            }

            for (int ring = 0; ring < rings; ring++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int a = ring * segments + s;
                    int b = ring * segments + (s + 1) % segments;
                    int c = (ring + 1) * segments + (s + 1) % segments;
                    int d = (ring + 1) * segments + s;
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(a); tris.Add(c); tris.Add(d);
                }
            }

            var mesh = new Mesh();
            mesh.name = "island";
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh FlatDisc(float radius, int segments)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            verts.Add(Vector3.zero);
            for (int i = 0; i < segments; i++)
            {
                float a = (float)i / segments * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }
            for (int i = 0; i < segments; i++)
            {
                tris.Add(0);
                tris.Add(1 + (i + 1) % segments);
                tris.Add(1 + i);
            }
            var mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            return mesh;
        }
    }

    public static class Palette
    {
        public static readonly Color Sky = new Color32(0xAF, 0xE2, 0xF0, 0xFF);
        public static readonly Color SkyTop = new Color32(0x82, 0xC9, 0xE8, 0xFF);
        public static readonly Color Sun = new Color32(0xFF, 0xF1, 0xB8, 0xFF);
        public static readonly Color Sand = new Color32(0xF2, 0xDD, 0xA8, 0xFF);
        public static readonly Color Grass = new Color32(0x86, 0xC4, 0x6E, 0xFF);
        public static readonly Color GrassDark = new Color32(0x6D, 0xAB, 0x59, 0xFF);
        public static readonly Color Rock = new Color32(0xAE, 0xAA, 0x9E, 0xFF);
        public static readonly Color Wood = new Color32(0xB9, 0x8A, 0x5E, 0xFF);
        public static readonly Color WoodDark = new Color32(0x8D, 0x66, 0x45, 0xFF);
        public static readonly Color PalmTrunk = new Color32(0xA0, 0x7E, 0x55, 0xFF);
        public static readonly Color PalmLeaf = new Color32(0x4F, 0xA0, 0x5A, 0xFF);
        public static readonly Color Water = new Color32(0x3A, 0xA7, 0xC0, 0xFF);
        public static readonly Color WaterDeep = new Color32(0x2A, 0x86, 0xA8, 0xFF);
        public static readonly Color BoatHull = new Color32(0xC9, 0x6B, 0x4B, 0xFF);
        public static readonly Color BoatTrim = new Color32(0xF2, 0xE6, 0xC8, 0xFF);
        public static readonly Color Cloud = new Color32(0xFF, 0xFC, 0xF4, 0xFF);
        public static readonly Color Cream = new Color32(0xFF, 0xF4, 0xDD, 0xFF);
        public static readonly Color Teal = new Color32(0x2E, 0x8B, 0x8B, 0xFF);
        public static readonly Color Coral = new Color32(0xFF, 0x7A, 0x59, 0xFF);
        public static readonly Color Ink = new Color32(0x25, 0x38, 0x4A, 0xFF);
        public static readonly Color Gold = new Color32(0xF2, 0xB2, 0x3E, 0xFF);
    }

    public class IslandInfo
    {
        public string name;
        public Vector3 center;
        public float radius;
        public float height;
        public int seed;
    }

    public class WorldBuilder : MonoBehaviour
    {
        public Material sandMat, grassMat, grassDarkMat, rockMat, woodMat, woodDarkMat, palmTrunkMat, leafMat, waterMat, hullMat, trimMat, cloudMat, sunMat, foamMat;
        public GameObject BoatObject;
        public Boat Boat;
        public Transform StartSpawn;
        public Transform DockEnd;
        public float WaterLevel = 0.15f;
        public Font worldFont;
        public List<IslandInfo> Islands = new List<IslandInfo>();

        private Material unlitWhite;

        public bool IsInsideIsland(Vector3 worldPos, float shrink)
        {
            for (int i = 0; i < Islands.Count; i++)
            {
                var isl = Islands[i];
                float dx = worldPos.x - isl.center.x;
                float dz = worldPos.z - isl.center.z;
                if (dx * dx + dz * dz < isl.radius * shrink * isl.radius * shrink) return true;
            }
            return false;
        }

        public void Build()
        {
            GameData.Init();
            CreateMaterials();
            CreateLighting();
            CreateOcean();
            CreateSky();

            var start = CreateIsland("Cove Island", Vector3.zero, 20f, 4.2f, 11, true);
            var isle2 = CreateIsland("Isle Two", new Vector3(-74f, 0f, 52f), 14f, 3.6f, 23, false);
            var isle3 = CreateIsland("Isle Three", new Vector3(84f, 0f, -62f), 16f, 4.6f, 37, false);
            var isle4 = CreateIsland("Isle Four", new Vector3(150f, 0f, 40f), 15f, 4.0f, 41, false);
            var isle5 = CreateIsland("Isle Five", new Vector3(60f, 0f, -180f), 18f, 5.0f, 53, false);

            BuildStartVillage(start);
            BuildBossAltar(isle2, 1, 14f, 3.6f, 23);
            BuildBossAltar(isle3, 2, 16f, 4.6f, 37);
            BuildBossAltar(isle4, 3, 15f, 4.0f, 41);
            BuildBossAltar(isle5, 4, 18f, 5.0f, 53);
            BuildLavaVent(isle5, 18f, 5.0f, 53);
            BuildSeagulls();
            BuildDock();

            StartSpawn = new GameObject("StartSpawn").transform;
            StartSpawn.SetParent(transform);
            StartSpawn.position = new Vector3(0f, MeshGen.IslandHeight(20f, 4.2f, 0f, 3f, 11) + 1.2f, 3f);
            DockEnd = new GameObject("DockEnd").transform;
            DockEnd.SetParent(transform);
            DockEnd.position = new Vector3(0f, 1.3f, 27f);

            BuildBoat();
        }

        private void CreateMaterials()
        {
            sandMat = MakeMat("sand", Palette.Sand, 0.12f);
            grassMat = MakeMat("grass", Palette.Grass, 0.1f);
            grassDarkMat = MakeMat("grassDark", Palette.GrassDark, 0.1f);
            rockMat = MakeMat("rock", Palette.Rock, 0.2f);
            woodMat = MakeMat("wood", Palette.Wood, 0.15f);
            woodDarkMat = MakeMat("woodDark", Palette.WoodDark, 0.15f);
            palmTrunkMat = MakeMat("palmTrunk", Palette.PalmTrunk, 0.1f);
            leafMat = MakeMat("palmLeaf", Palette.PalmLeaf, 0.1f);
            waterMat = MakeMat("water", Palette.Water, 0.85f);
            hullMat = MakeMat("hull", Palette.BoatHull, 0.25f);
            trimMat = MakeMat("trim", Palette.BoatTrim, 0.15f);
            cloudMat = MakeMat("cloud", Palette.Cloud, 0.05f);
            cloudMat.EnableKeyword("_EMISSION");
            cloudMat.SetColor("_EmissionColor", Palette.Cloud * 0.35f);
            sunMat = MakeMat("sun", Palette.Sun, 0f);
            sunMat.EnableKeyword("_EMISSION");
            sunMat.SetColor("_EmissionColor", Palette.Sun * 1.6f);
            foamMat = MakeMat("foam", new Color32(0xF4, 0xFF, 0xFF, 0xFF), 0.4f);
            unlitWhite = MakeMat("white", Color.white, 0f);
        }

        public static Material MakeMat(string name, Color c, float smooth)
        {
            var sh = Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Legacy Shaders/Diffuse");
            var m = new Material(sh);
            m.name = name;
            m.color = c;
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smooth);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            return m;
        }

        private void CreateLighting()
        {
            var sunGo = new GameObject("Sun Light");
            sunGo.transform.SetParent(transform);
            var light = sunGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.87f);
            light.intensity = 1.05f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.55f;
            sunGo.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.58f, 0.68f, 0.72f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0022f;
            RenderSettings.fogColor = Palette.Sky;
            RenderSettings.skybox = null;
        }

        private void CreateOcean()
        {
            var go = new GameObject("Ocean");
            go.transform.SetParent(transform);
            var mesh = new Mesh();
            mesh.name = "ocean";
            int n = 40;
            float size = 320f;
            var verts = new Vector3[(n + 1) * (n + 1)];
            var uvs = new Vector2[verts.Length];
            var tris = new int[n * n * 6];
            for (int z = 0; z <= n; z++)
            {
                for (int x = 0; x <= n; x++)
                {
                    int i = z * (n + 1) + x;
                    float fx = (float)x / n;
                    float fz = (float)z / n;
                    verts[i] = new Vector3((fx - 0.5f) * size, 0f, (fz - 0.5f) * size);
                    uvs[i] = new Vector2(fx, fz);
                }
            }
            int t = 0;
            for (int z = 0; z < n; z++)
            {
                for (int x = 0; x < n; x++)
                {
                    int i = z * (n + 1) + x;
                    tris[t++] = i;
                    tris[t++] = i + n + 2;
                    tris[t++] = i + 1;
                    tris[t++] = i;
                    tris[t++] = i + n + 1;
                    tris[t++] = i + n + 2;
                }
            }
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = waterMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            var ocean = go.AddComponent<Ocean>();
            ocean.mesh = mesh;
            ocean.baseVerts = verts;
            ocean.mat = waterMat;
        }

        private void CreateSky()
        {
            var sun = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sun.name = "Sun";
            DestroyImmediate(sun.GetComponent<Collider>());
            sun.transform.SetParent(transform);
            sun.transform.position = new Vector3(130f, 95f, -170f);
            sun.transform.localScale = Vector3.one * 22f;
            var mr = sun.GetComponent<MeshRenderer>();
            mr.sharedMaterial = sunMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            var clouds = new GameObject("Clouds");
            clouds.transform.SetParent(transform);
            var rng = new System.Random(77);
            for (int i = 0; i < 9; i++)
            {
                float ang = (float)rng.NextDouble() * 360f;
                float dist = 55f + (float)rng.NextDouble() * 110f;
                float h = 32f + (float)rng.NextDouble() * 26f;
                var c = new GameObject("cloud");
                c.transform.SetParent(clouds.transform);
                c.transform.position = new Vector3(Mathf.Cos(ang * Mathf.Deg2Rad) * dist, h, Mathf.Sin(ang * Mathf.Deg2Rad) * dist);
                int puffs = 3 + rng.Next(3);
                for (int p = 0; p < puffs; p++)
                {
                    var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    DestroyImmediate(puff.GetComponent<Collider>());
                    puff.transform.SetParent(c.transform);
                    puff.transform.localPosition = new Vector3((float)rng.NextDouble() * 8f - 4f, (float)rng.NextDouble() * 1.6f, (float)rng.NextDouble() * 5f - 2.5f);
                    float sc = 4f + (float)rng.NextDouble() * 5f;
                    puff.transform.localScale = new Vector3(sc, sc * 0.42f, sc * 0.8f);
                    puff.GetComponent<MeshRenderer>().sharedMaterial = cloudMat;
                    puff.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
        }

        private Transform CreateIsland(string name, Vector3 center, float radius, float height, int seed, bool detailed)
        {
            var root = new GameObject(name);
            root.transform.SetParent(transform);
            root.transform.position = center;
            Islands.Add(new IslandInfo { name = name, center = center, radius = radius, height = height, seed = seed });

            var sand = new GameObject("Sand");
            sand.transform.SetParent(root.transform, false);
            var mesh = MeshGen.IslandMesh(radius, height, seed, radius * 1.15f, 12, 32);
            sand.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = sand.AddComponent<MeshRenderer>();
            mr.sharedMaterial = sandMat;
            var mc = sand.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;

            var grass = new GameObject("Grass");
            grass.transform.SetParent(root.transform, false);
            var gmesh = MeshGen.IslandMesh(radius, height, seed, radius * 0.72f, 10, 32);
            var gverts = gmesh.vertices;
            for (int i = 0; i < gverts.Length; i++) gverts[i].y += 0.07f;
            gmesh.vertices = gverts;
            gmesh.RecalculateNormals();
            grass.AddComponent<MeshFilter>().sharedMesh = gmesh;
            var gmr = grass.AddComponent<MeshRenderer>();
            gmr.sharedMaterial = grassMat;
            gmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var rng = new System.Random(seed * 13 + 5);
            int palms = detailed ? 6 : 4;
            string[] palmVariants = { "tree_palm", "tree_palmBend", "tree_palmDetailedTall", "tree_palmShort", "palm-straight", "palm-detailed-straight" };
            for (int i = 0; i < palms; i++)
            {
                float a2 = (float)rng.NextDouble() * Mathf.PI * 2f;
                float rr = radius * (0.2f + (float)rng.NextDouble() * 0.38f);
                float x = Mathf.Cos(a2) * rr;
                float z = Mathf.Sin(a2) * rr;
                if (detailed && (new Vector2(x, z + 5f).magnitude < 7f || new Vector2(x - 8f, z - 1f).magnitude < 3.5f || (z > 10f && Mathf.Abs(x) < 4f))) continue;
                if (!detailed && (new Vector2(x - 6f, z - 5f).magnitude < 5.5f || new Vector2(x, z - 4f).magnitude < 3.5f)) continue;
                float y = MeshGen.IslandHeight(radius, height, x, z, seed);
                string model = palmVariants[rng.Next(palmVariants.Length)];
                float targetH = 4.2f + (float)rng.NextDouble() * 2.6f;
                var palm = CC0Library.SpawnLocal(model, root.transform, new Vector3(x, y, z), (float)rng.NextDouble() * 360f, targetH);
                if (palm == null) CreatePalm(new Vector3(x, y, z), root.transform, rng);
            }

            int rocks = detailed ? 8 : 5;
            string[] rockLarge = { "rocks-sand-a", "rocks-sand-b" };
            string[] rockSmall = { "rocks-sand-a", "rocks-sand-b" };
            for (int i = 0; i < rocks; i++)
            {
                float a2 = (float)rng.NextDouble() * Mathf.PI * 2f;
                float rr = radius * (0.3f + (float)rng.NextDouble() * 0.62f);
                float x = Mathf.Cos(a2) * rr;
                float z = Mathf.Sin(a2) * rr;
                if (detailed && new Vector2(x, z + 5f).magnitude < 7f) continue;
                if (!detailed && new Vector2(x - 6f, z - 5f).magnitude < 5.5f) continue;
                float y = MeshGen.IslandHeight(radius, height, x, z, seed);
                bool big = rng.NextDouble() < 0.45;
                string model = big ? rockLarge[rng.Next(rockLarge.Length)] : rockSmall[rng.Next(rockSmall.Length)];
                float targetH = big ? 0.9f + (float)rng.NextDouble() * 1.0f : 0.3f + (float)rng.NextDouble() * 0.5f;
                var rock = CC0Library.SpawnLocal(model, root.transform, new Vector3(x, y, z), (float)rng.NextDouble() * 360f, targetH);
                if (rock != null)
                {
                    if (targetH >= 0.55f) CC0Library.AddBoxCollider(rock, 0.02f);
                }
                else
                {
                    var fallback = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    fallback.name = "Rock";
                    fallback.transform.SetParent(root.transform);
                    fallback.transform.localPosition = new Vector3(x, y + 0.05f, z);
                    fallback.transform.localScale = new Vector3(1.2f + (float)rng.NextDouble() * 1.6f, 0.7f + (float)rng.NextDouble() * 0.8f, 1.1f + (float)rng.NextDouble() * 1.4f);
                    fallback.transform.rotation = Quaternion.Euler((float)rng.NextDouble() * 20f, (float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 20f);
                    fallback.GetComponent<MeshRenderer>().sharedMaterial = rockMat;
                }
            }

            int tufts = detailed ? 22 : 12;
            string[] plantModels = { "grass", "grass_large", "plant_bush" };
            for (int i = 0; i < tufts; i++)
            {
                float a2 = (float)rng.NextDouble() * Mathf.PI * 2f;
                float rr = radius * (0.12f + (float)rng.NextDouble() * 0.55f);
                float x = Mathf.Cos(a2) * rr;
                float z = Mathf.Sin(a2) * rr;
                if (detailed && new Vector2(x, z + 5f).magnitude < 6f) continue;
                if (!detailed && new Vector2(x - 6f, z - 5f).magnitude < 4.5f) continue;
                float y = MeshGen.IslandHeight(radius, height, x, z, seed);
                float targetH = 0.2f + (float)rng.NextDouble() * 0.6f;
                var plant = CC0Library.SpawnLocal(plantModels[rng.Next(plantModels.Length)], root.transform, new Vector3(x, y, z), (float)rng.NextDouble() * 360f, targetH);
                if (plant == null)
                {
                    var tuft = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    DestroyImmediate(tuft.GetComponent<Collider>());
                    tuft.name = "Grass Tuft";
                    tuft.transform.SetParent(root.transform);
                    tuft.transform.localPosition = new Vector3(x, y + 0.02f, z);
                    tuft.transform.localScale = new Vector3(0.7f + (float)rng.NextDouble() * 0.8f, 0.22f, 0.7f + (float)rng.NextDouble() * 0.8f);
                    tuft.GetComponent<MeshRenderer>().sharedMaterial = grassDarkMat;
                }
            }

            int cliffs = 0;
            for (int i = 0; i < cliffs; i++)
            {
                float a2 = detailed ? 2.4f : (float)rng.NextDouble() * Mathf.PI * 2f + i * 2.1f;
                float rr = radius * (0.88f + (float)rng.NextDouble() * 0.06f);
                float x = Mathf.Cos(a2) * rr;
                float z = Mathf.Sin(a2) * rr;
                float y = MeshGen.IslandHeight(radius, height, x, z, seed);
                float targetH = 2.2f + (float)rng.NextDouble() * 2.2f;
                var cliff = CC0Library.SpawnLocal("cliff_rock", root.transform, new Vector3(x, y, z), a2 * Mathf.Rad2Deg + 90f, targetH);
                if (cliff != null) CC0Library.AddBoxCollider(cliff, 0.02f);
            }
            if (!detailed)
            {
                CreateSign(root.transform, new Vector3(0f, MeshGen.IslandHeight(radius, height, 0f, -radius * 0.55f, seed) + 1.9f, -radius * 0.55f), name);
            }

            return root.transform;
        }

        private void CreatePalm(Vector3 localPos, Transform parent, System.Random rng)
        {
            var palm = new GameObject("Palm");
            palm.transform.SetParent(parent);
            palm.transform.localPosition = localPos;
            float lean = (float)rng.NextDouble() * 14f - 7f;
            palm.transform.rotation = Quaternion.Euler(lean * 0.4f, (float)rng.NextDouble() * 360f, lean);

            int trunkSegs = 4;
            float trunkH = 3.4f + (float)rng.NextDouble() * 1.4f;
            float segH = trunkH / trunkSegs;
            for (int s = 0; s < trunkSegs; s++)
            {
                var seg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                DestroyImmediate(seg.GetComponent<Collider>());
                seg.transform.SetParent(palm.transform);
                seg.transform.localPosition = new Vector3(s * 0.09f, segH * (s + 0.5f), 0f);
                seg.transform.localScale = new Vector3(0.24f - s * 0.028f, segH * 0.55f, 0.24f - s * 0.028f);
                seg.transform.localRotation = Quaternion.Euler(0f, 0f, -5f);
                seg.GetComponent<MeshRenderer>().sharedMaterial = palmTrunkMat;
            }

            var crown = new GameObject("Crown");
            crown.transform.SetParent(palm.transform);
            crown.transform.localPosition = new Vector3(trunkSegs * 0.09f + 0.1f, trunkH, 0f);
            int leaves = 7;
            for (int l = 0; l < leaves; l++)
            {
                var leaf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                DestroyImmediate(leaf.GetComponent<Collider>());
                leaf.transform.SetParent(crown.transform);
                float ang = (float)l / leaves * 360f;
                leaf.transform.localRotation = Quaternion.Euler(8f, ang, -14f);
                leaf.transform.localPosition = Quaternion.Euler(0f, ang, 0f) * new Vector3(1.15f, 0.05f, 0f);
                leaf.transform.localScale = new Vector3(1.55f, 0.11f, 0.42f);
                leaf.GetComponent<MeshRenderer>().sharedMaterial = leafMat;
                leaf.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            var coconut = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            DestroyImmediate(coconut.GetComponent<Collider>());
            coconut.transform.SetParent(crown.transform);
            coconut.transform.localPosition = new Vector3(0.1f, -0.2f, 0f);
            coconut.transform.localScale = Vector3.one * 0.28f;
            coconut.GetComponent<MeshRenderer>().sharedMaterial = woodDarkMat;
        }

        private void CreateSign(Transform parent, Vector3 localPos, string text)
        {
            var sign = new GameObject("Sign");
            sign.transform.SetParent(parent);
            sign.transform.localPosition = localPos;
            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.transform.SetParent(sign.transform);
            post.transform.localPosition = new Vector3(0f, -1f, 0f);
            post.transform.localScale = new Vector3(0.1f, 1f, 0.1f);
            post.GetComponent<MeshRenderer>().sharedMaterial = woodDarkMat;
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.transform.SetParent(sign.transform);
            board.transform.localScale = new Vector3(2.6f, 0.7f, 0.1f);
            board.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
            var label = new GameObject("Label");
            label.transform.SetParent(sign.transform);
            label.transform.localPosition = new Vector3(0f, 0.02f, -0.08f);
            label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var tm = label.AddComponent<TextMesh>();
            tm.text = text;
            tm.font = worldFont;
            tm.fontSize = 48;
            tm.characterSize = 0.09f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = Palette.Ink;
            tm.GetComponent<MeshRenderer>().sharedMaterial = worldFont.material;
        }

        private void BuildStartVillage(Transform island)
        {
            if (CC0Library.Prefab("workbench") == null || CC0Library.Prefab("signpost-single") == null)
            {
                BuildStartVillageLegacy(island);
                return;
            }

            float h = MeshGen.IslandHeight(20f, 4.2f, 0f, -5f, 11);
            var shop = new GameObject("Cove Shop");
            shop.transform.SetParent(island);
            shop.transform.localPosition = new Vector3(0f, h, -5f);

            var canopy = new GameObject("Shop Canopy");
            canopy.transform.SetParent(shop.transform, false);
            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(roof.GetComponent<Collider>());
            roof.transform.SetParent(canopy.transform, false);
            roof.transform.localPosition = new Vector3(0f, 4.3f, 0f);
            roof.transform.localScale = new Vector3(7.6f, 0.2f, 3.8f);
            roof.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
            for (int side = -1; side <= 1; side += 2)
            {
                for (int zside = -1; zside <= 1; zside += 2)
                {
                    var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    Destroy(post.GetComponent<Collider>());
                    post.transform.SetParent(canopy.transform, false);
                    post.transform.localPosition = new Vector3(side * 3.5f, 2.1f, zside * 1.9f);
                    post.transform.localScale = new Vector3(0.16f, 2.1f, 0.16f);
                    post.GetComponent<MeshRenderer>().sharedMaterial = woodDarkMat;
                }
            }
            AddLabel(canopy.transform, new Vector3(0f, 3.15f, 2.72f), "", 34, 0.04f, Palette.Cream, new Vector3(0f, 180f, 0f));

            CC0Library.SpawnLocal("fence_planks", shop.transform, new Vector3(0f, 0f, -2.4f), 0f, 1.75f);
            var planks = CC0Library.SpawnLocal("resource-planks", shop.transform, new Vector3(-3.1f, 0f, -2.0f), 22f, 0.35f);
            if (planks != null) CC0Library.AddBoxCollider(planks);

            var counter = CC0Library.SpawnLocal("workbench", shop.transform, Vector3.zero, 180f, 1.0f);
            if (counter != null) CC0Library.AddBoxCollider(counter, 0.02f);

            var crate = CC0Library.SpawnLocal("crate", shop.transform, new Vector3(2.3f, 0f, 0.6f), 16f, 0.8f);
            if (crate != null) CC0Library.AddBoxCollider(crate);
            var barrel = CC0Library.SpawnLocal("barrel", shop.transform, new Vector3(2.9f, 0f, -0.9f), 0f, 1.0f);
            if (barrel != null) CC0Library.AddBoxCollider(barrel);
            var chest = CC0Library.SpawnLocal("chest", shop.transform, new Vector3(-2.4f, 0f, 0.7f), -18f, 0.55f);
            if (chest != null) CC0Library.AddBoxCollider(chest);
            CC0Library.SpawnLocal("bucket", shop.transform, new Vector3(1.6f, 0f, 1.2f), 0f, 0.45f);

            CC0Library.SpawnLocal("fish", shop.transform, new Vector3(-1.6f, 1.55f, -2.3f), 180f, 0.32f);
            CC0Library.SpawnLocal("fish-large", shop.transform, new Vector3(0.6f, 1.7f, -2.3f), 180f, 0.62f);

            var signPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            signPanel.name = "Shop Sign";
            signPanel.transform.SetParent(shop.transform);
            signPanel.transform.localPosition = new Vector3(0f, 2.65f, -0.1f);
            signPanel.transform.localScale = new Vector3(2.7f, 0.5f, 0.1f);
            signPanel.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
            AddLabel(shop.transform, new Vector3(0f, 2.65f, 0.02f), "COVE SHOP", 54, 0.07f, Palette.Cream, new Vector3(0f, 180f, 0f));

            var npc = CC0Library.SpawnLocal("character-female-a", shop.transform, new Vector3(1.7f, 0f, -0.8f), 0f, 1.65f);
            if (npc != null)
            {
                var bob = npc.AddComponent<ShopkeeperIdle>();
                bob.WaveEvery = 7f;
                bob.Speed = 1.4f;
                var cap = npc.AddComponent<CapsuleCollider>();
                Bounds nb = CC0Library.LocalBounds(npc, npc.transform);
                cap.center = new Vector3(nb.center.x, nb.size.y * 0.45f, nb.center.z);
                cap.height = nb.size.y * 0.9f;
                cap.radius = Mathf.Max(0.18f, Mathf.Min(nb.size.x, nb.size.z) * 0.28f);

                var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plate.name = "Name Plate";
                plate.transform.SetParent(shop.transform);
                plate.transform.localPosition = new Vector3(1.7f, 2.02f, -0.76f);
                plate.transform.localScale = new Vector3(1.05f, 0.24f, 0.03f);
                plate.GetComponent<MeshRenderer>().sharedMaterial = woodDarkMat;
                AddLabel(shop.transform, new Vector3(1.7f, 2.02f, -0.72f), "Mira", 40, 0.032f, Palette.Cream, new Vector3(0f, 180f, 0f));
            }

            var shopTrigger = new GameObject("Shop Trigger");
            shopTrigger.transform.SetParent(shop.transform);
            shopTrigger.transform.localPosition = new Vector3(0f, 1.2f, 1.5f);
            var sc = shopTrigger.AddComponent<BoxCollider>();
            sc.size = new Vector3(3.4f, 2.4f, 1.8f);
            sc.isTrigger = true;
            var interact = shopTrigger.AddComponent<Interactable>();
            interact.prompt = "E   Open the shop";
            interact.onInteract = () => GameBoot.I.OpenShop();

            var rack = new GameObject("Weapon Rack");
            rack.transform.SetParent(shop.transform, false);
            rack.transform.localPosition = new Vector3(5.2f, 0f, 0.2f);
            var rackCrate = CC0Library.SpawnLocal("crate", rack.transform, Vector3.zero, 12f, 1.2f);
            if (rackCrate != null) CC0Library.AddBoxCollider(rackCrate);
            var rackCol = rack.AddComponent<BoxCollider>();
            rackCol.center = new Vector3(0f, 0.8f, 0f);
            rackCol.size = new Vector3(1.8f, 1.7f, 1.4f);
            rackCol.isTrigger = true;
            var rackInteract = rack.AddComponent<Interactable>();
            rackInteract.prompt = "E   Open the weapon rack";
            rackInteract.onInteract = () => GameBoot.I.OpenWeapons();
            AddLabel(rack.transform, new Vector3(0f, 1.95f, 0f), "WEAPON RACK", 38, 0.045f, Palette.Cream, new Vector3(0f, 180f, 0f));

            var bait = new GameObject("Crab Bait");
            bait.transform.SetParent(shop.transform, false);
            bait.transform.localPosition = new Vector3(-5.2f, 0f, 7.5f);
            CC0Library.SpawnLocal("bucket", bait.transform, Vector3.zero, 0f, 0.55f);
            var baitCol = bait.AddComponent<BoxCollider>();
            baitCol.center = new Vector3(0f, 0.5f, 0f);
            baitCol.size = new Vector3(1.2f, 1.2f, 1.2f);
            baitCol.isTrigger = true;
            var baitInteract = bait.AddComponent<Interactable>();
            baitInteract.prompt = "E   Bait the Spider Crab";
            baitInteract.onInteract = () => GameBoot.I.TrySpawnBoss();
            AddLabel(bait.transform, new Vector3(0f, 1.15f, 0f), "CRAB BAIT", 34, 0.04f, Palette.Coral, new Vector3(0f, 180f, 0f));

            var grill = new GameObject("Grill");
            grill.transform.SetParent(shop.transform, false);
            grill.transform.localPosition = new Vector3(-5.2f, 0f, -0.4f);
            CC0Library.SpawnLocal("campfire-fishing-stand", grill.transform, Vector3.zero, 0f, 0.8f);
            var grillCol = grill.AddComponent<BoxCollider>();
            grillCol.center = new Vector3(0f, 0.6f, 0f);
            grillCol.size = new Vector3(1.5f, 1.4f, 1.5f);
            grillCol.isTrigger = true;
            var grillInteract = grill.AddComponent<Interactable>();
            grillInteract.prompt = "E   Use the grill";
            grillInteract.onInteract = () => GameBoot.I.OpenGrill();
            AddLabel(grill.transform, new Vector3(0f, 1.35f, 0f), "GRILL", 34, 0.04f, Palette.Gold, new Vector3(0f, 180f, 0f));

            var fortune = new GameObject("Reel of Fortune");
            fortune.transform.SetParent(shop.transform, false);
            fortune.transform.localPosition = new Vector3(5.2f, 0f, 3.8f);
            CC0Library.SpawnLocal("chest", fortune.transform, Vector3.zero, 0f, 0.9f);
            var fortuneCol = fortune.AddComponent<BoxCollider>();
            fortuneCol.center = new Vector3(0f, 0.6f, 0f);
            fortuneCol.size = new Vector3(1.5f, 1.3f, 1.5f);
            fortuneCol.isTrigger = true;
            var fortuneInteract = fortune.AddComponent<Interactable>();
            fortuneInteract.prompt = "E   Spin the Reel of Fortune";
            fortuneInteract.onInteract = () => GameBoot.I.OpenFortune();
            AddLabel(fortune.transform, new Vector3(0f, 1.45f, 0f), "REEL OF FORTUNE", 30, 0.035f, Palette.Gold, new Vector3(0f, 180f, 0f));

            var board = new GameObject("Quest Board");
            board.transform.SetParent(island);
            float bh = MeshGen.IslandHeight(20f, 4.2f, 6f, 1f, 11);
            board.transform.localPosition = new Vector3(8f, bh, 1f);
            board.transform.localRotation = Quaternion.Euler(0f, -18f, 0f);

            var sign = CC0Library.SpawnLocal("signpost-single", board.transform, Vector3.zero, 0f, 2.35f);
            if (sign != null) CC0Library.AddBoxCollider(sign, 0.02f);
            AddLabel(board.transform, new Vector3(0f, 2.42f, 0.05f), "QUESTS", 44, 0.055f, Palette.Ink, new Vector3(0f, 180f, 0f));
            AddLabel(board.transform, new Vector3(0f, 2.02f, 0.06f), "Catch and earn!", 30, 0.038f, Palette.Cream, new Vector3(0f, 180f, 0f));

            var questTrigger = board.AddComponent<BoxCollider>();
            questTrigger.center = new Vector3(0f, 1.6f, 0f);
            questTrigger.size = new Vector3(3.4f, 2.4f, 3f);
            questTrigger.isTrigger = true;
            var qInteract = board.AddComponent<Interactable>();
            qInteract.prompt = "E   Read the quest board";
            qInteract.onInteract = () => GameBoot.I.OpenQuests();

            float fy = MeshGen.IslandHeight(20f, 4.2f, 11f, 8f, 11);
            CC0Library.SpawnLocal("campfire-fishing-stand", island, new Vector3(11f, fy, 8f), 32f, 0.95f);
        }

        private void BuildBossAltar(Transform island, int type, float radius, float height, int seed)
        {
            string bossName = GameBoot.IslandBossName(type).ToUpperInvariant();
            float h = MeshGen.IslandHeight(radius, height, 0f, 4f, seed);
            var altar = new GameObject(bossName + " Altar");
            altar.transform.SetParent(island, false);
            altar.transform.localPosition = new Vector3(0f, h, 4f);

            var sign = CC0Library.SpawnLocal("signpost-single", altar.transform, Vector3.zero, 0f, 2.5f);
            if (sign == null)
            {
                var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.transform.SetParent(altar.transform, false);
                post.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                post.transform.localScale = new Vector3(0.18f, 1.2f, 0.18f);
                post.GetComponent<MeshRenderer>().sharedMaterial = woodDarkMat;
            }
            AddLabel(altar.transform, new Vector3(0f, 2.55f, 0.05f), bossName, 40, 0.05f, Palette.Coral, new Vector3(0f, 180f, 0f));
            AddLabel(altar.transform, new Vector3(0f, 2.18f, 0.06f), "E to challenge", 28, 0.036f, Palette.Cream, new Vector3(0f, 180f, 0f));

            var trigger = altar.AddComponent<BoxCollider>();
            trigger.center = new Vector3(0f, 1.3f, 0f);
            trigger.size = new Vector3(3.4f, 2.6f, 3.2f);
            trigger.isTrigger = true;
            var interact = altar.AddComponent<Interactable>();
            interact.prompt = "E   Challenge the " + GameBoot.IslandBossName(type);
            interact.onInteract = () => GameBoot.I.TrySpawnIslandBoss(type);
        }

        private void BuildSeagulls()
        {
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject("Seagull");
                go.transform.SetParent(transform, false);
                var gull = go.AddComponent<Seagull>();
                gull.Init(GameBoot.I, Vector3.zero, i * Mathf.PI);
            }
        }

        private void BuildLavaVent(Transform island, float radius, float height, int seed)
        {            float h = MeshGen.IslandHeight(radius, height, -4f, -4f, seed);
            var vent = new GameObject("Lava Vent");
            vent.transform.SetParent(island, false);
            vent.transform.localPosition = new Vector3(-4f, h, -4f);

            var pool = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(pool.GetComponent<Collider>());
            pool.transform.SetParent(vent.transform, false);
            pool.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            pool.transform.localScale = new Vector3(2.6f, 0.25f, 2.6f);
            pool.GetComponent<MeshRenderer>().sharedMaterial = MakeMat("lava", new Color32(0xFF, 0x5A, 0x1E, 0xFF), 0.2f);
            pool.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var col = vent.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.6f, 0f);
            col.size = new Vector3(3.2f, 1.6f, 3.2f);
            col.isTrigger = true;
            var interact = vent.AddComponent<Interactable>();
            interact.prompt = "E   Cook fish at the lava";
            interact.onInteract = () => GameBoot.I.CookAll(true);
            AddLabel(vent.transform, new Vector3(0f, 1.2f, 0f), "LAVA COOK", 34, 0.04f, Palette.Cream, new Vector3(0f, 180f, 0f));
        }

        private void BuildDock()
        {
            BuildDockLegacy();
            return;
            if (CC0Library.Prefab("structure-platform-dock") == null)
            {
                BuildDockLegacy();
                return;
            }

            var dock = new GameObject("Dock");
            dock.transform.SetParent(transform);
            dock.transform.position = Vector3.zero;

            float z = 16.8f;
            float maxZ = 16.8f;
            int guard = 0;
            while (z < 30.2f && guard < 24)
            {
                string model = (guard % 5 == 4) ? "structure-platform-dock-small" : "structure-platform-dock";
                var tile = CC0Library.SpawnLocal(model, dock.transform, new Vector3(0f, 0f, z), (guard % 2) * 180f, 2.7f, CCFit.Footprint, false);
                if (tile == null) break;
                CC0Library.SetTopLocalY(tile, dock.transform, 0.95f);
                Bounds b = CC0Library.LocalBounds(tile, dock.transform);
                z = b.max.z - 0.03f;
                maxZ = Mathf.Max(maxZ, b.max.z);
                guard++;
            }

            var walkway = new GameObject("Dock Walkway");
            walkway.transform.SetParent(dock.transform);
            var col = walkway.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.7f, (16.8f + maxZ) * 0.5f);
            col.size = new Vector3(2.55f, 0.5f, Mathf.Max(4f, maxZ - 16.2f));

            for (int side = -1; side <= 1; side += 2)
            {
                float rz = 17.6f;
                for (int i = 0; i < 8 && rz < maxZ - 0.7f; i++)
                {
                    var rail = CC0Library.SpawnLocal("fence_planks", dock.transform, Vector3.zero, 90f, 0.9f);
                    if (rail == null) break;
                    Bounds rb = CC0Library.LocalBounds(rail, dock.transform);
                    Vector3 lp = rail.transform.localPosition;
                    lp.x += side * 1.38f - rb.center.x;
                    lp.z += rz - rb.center.z;
                    lp.y += 0.95f - rb.min.y;
                    rail.transform.localPosition = lp;
                    rz += Mathf.Max(1.15f, rb.size.z * 0.9f);
                }
            }
        }

        private void BuildBoat()
        {
            var boatRoot = new GameObject("Rowboat");
            boatRoot.transform.SetParent(transform);
            boatRoot.transform.position = new Vector3(4.2f, 0.32f, 25.5f);
            boatRoot.transform.rotation = Quaternion.Euler(0f, 8f, 0f);

            var trigger = boatRoot.AddComponent<BoxCollider>();
            trigger.size = new Vector3(2.3f, 1.4f, 4.6f);
            trigger.center = new Vector3(0f, 0.35f, 0f);
            trigger.isTrigger = true;

            var boat = boatRoot.AddComponent<Boat>();
            var hull = new GameObject("Hull").transform;
            hull.SetParent(boatRoot.transform, false);

            var model = CC0Library.Prefab("boat-row-small") != null
                ? CC0Library.SpawnLocal("boat-row-small", hull, Vector3.zero, 0f, 4.2f, CCFit.Footprint, false)
                : null;

            if (model != null)
            {
                Bounds b = CC0Library.LocalBounds(model, hull);
                if (b.size.x > b.size.z * 1.12f)
                {
                    model.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                    b = CC0Library.LocalBounds(model, hull);
                    float s = 4.2f / Mathf.Max(0.001f, Mathf.Max(b.size.x, b.size.z));
                    model.transform.localScale *= s;
                    b = CC0Library.LocalBounds(model, hull);
                }
                Vector3 lp = model.transform.localPosition;
                lp.x -= b.center.x;
                lp.z -= b.center.z;
                lp.y += -0.22f - b.min.y;
                model.transform.localPosition = lp;

                var paddle = CC0Library.SpawnLocal("tool-paddle", hull, new Vector3(0.55f, 0.3f, -0.1f), 20f, 1.25f);
                if (paddle != null) paddle.transform.localRotation = Quaternion.Euler(18f, 200f, 14f);
            }
            else
            {
                var fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fallback.transform.SetParent(hull, false);
                fallback.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                fallback.transform.localScale = new Vector3(1.9f, 0.6f, 3.9f);
                fallback.GetComponent<MeshRenderer>().sharedMaterial = hullMat;
                DestroyImmediate(fallback.GetComponent<Collider>());
            }

            boat.hull = hull;
            boat.interact = boatRoot.AddComponent<Interactable>();
            boat.interact.prompt = "E   Board the rowboat";
            boat.interact.onInteract = () => GameBoot.I.ToggleBoat();
            BoatObject = boatRoot;
            Boat = boat;
        }
        private void BuildStartVillageLegacy(Transform island)
        {
            float h = MeshGen.IslandHeight(20f, 4.2f, 0f, -5f, 11);

            var hut = new GameObject("Shop Hut");
            hut.transform.SetParent(island);
            hut.transform.localPosition = new Vector3(0f, h, -5f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(hut.transform);
            body.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            body.transform.localScale = new Vector3(4.8f, 3.2f, 4.2f);
            body.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

            var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.transform.SetParent(hut.transform);
            door.transform.localPosition = new Vector3(0f, 1.05f, 2.13f);
            door.transform.localScale = new Vector3(1.15f, 2.1f, 0.12f);
            door.GetComponent<MeshRenderer>().sharedMaterial = woodDarkMat;

            var window = GameObject.CreatePrimitive(PrimitiveType.Cube);
            window.transform.SetParent(hut.transform);
            window.transform.localPosition = new Vector3(1.55f, 1.9f, 2.13f);
            window.transform.localScale = new Vector3(0.95f, 0.95f, 0.1f);
            window.GetComponent<MeshRenderer>().sharedMaterial = trimMat;

            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.transform.SetParent(hut.transform);
            roof.transform.localPosition = new Vector3(0f, 3.55f, 0f);
            roof.transform.localScale = new Vector3(5.6f, 0.44f, 5f);
            roof.transform.rotation = Quaternion.Euler(0f, 0f, 6f);
            roof.GetComponent<MeshRenderer>().sharedMaterial = leafMat;

            var roof2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof2.transform.SetParent(hut.transform);
            roof2.transform.localPosition = new Vector3(0f, 3.95f, 0f);
            roof2.transform.localScale = new Vector3(4.8f, 0.4f, 4.4f);
            roof2.transform.rotation = Quaternion.Euler(0f, 0f, -6f);
            roof2.GetComponent<MeshRenderer>().sharedMaterial = grassDarkMat;

            AddLabel(hut.transform, new Vector3(0f, 3.15f, 2.2f), "COVE SHOP", 56, 0.075f, Palette.Cream, new Vector3(0f, 0f, 0f));

            var shopTrigger = new GameObject("Shop Trigger");
            shopTrigger.transform.SetParent(hut.transform);
            shopTrigger.transform.localPosition = new Vector3(0f, 1.2f, 3.2f);
            var sc = shopTrigger.AddComponent<BoxCollider>();
            sc.size = new Vector3(3.2f, 2.4f, 2.2f);
            sc.isTrigger = true;
            var interact = shopTrigger.AddComponent<Interactable>();
            interact.prompt = "E   Open the shop";
            interact.onInteract = () => GameBoot.I.OpenShop();

            var board = new GameObject("Quest Board");
            board.transform.SetParent(island);
            float bh = MeshGen.IslandHeight(20f, 4.2f, 6f, 1f, 11);
            board.transform.localPosition = new Vector3(8f, bh, 1f);
            board.transform.localRotation = Quaternion.Euler(0f, -18f, 0f);

            var post1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post1.transform.SetParent(board.transform);
            post1.transform.localPosition = new Vector3(-1f, 0.9f, 0f);
            post1.transform.localScale = new Vector3(0.13f, 1.1f, 0.13f);
            post1.GetComponent<MeshRenderer>().sharedMaterial = woodDarkMat;
            var post2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post2.transform.SetParent(board.transform);
            post2.transform.localPosition = new Vector3(1f, 0.9f, 0f);
            post2.transform.localScale = new Vector3(0.13f, 1.1f, 0.13f);
            post2.GetComponent<MeshRenderer>().sharedMaterial = woodDarkMat;

            var boardPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boardPanel.transform.SetParent(board.transform);
            boardPanel.transform.localPosition = new Vector3(0f, 2.05f, 0f);
            boardPanel.transform.localScale = new Vector3(2.9f, 1.6f, 0.14f);
            boardPanel.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

            AddLabel(board.transform, new Vector3(0f, 2.2f, -0.09f), "QUESTS", 52, 0.065f, Palette.Ink, new Vector3(0f, 180f, 0f));
            AddLabel(board.transform, new Vector3(0f, 1.8f, 0.09f), "Catch and earn!", 40, 0.045f, Palette.Cream, Vector3.zero);

            var questTrigger = board.AddComponent<BoxCollider>();
            questTrigger.center = new Vector3(0f, 1.6f, 0f);
            questTrigger.size = new Vector3(3.4f, 2.4f, 3f);
            questTrigger.isTrigger = true;
            var qInteract = board.AddComponent<Interactable>();
            qInteract.prompt = "E   Read the quest board";
            qInteract.onInteract = () => GameBoot.I.OpenQuests();

            CreatePalm(new Vector3(-7f, MeshGen.IslandHeight(20f, 4.2f, -7f, 0f, 11), 0f), island, new System.Random(5));
            CreatePalm(new Vector3(7.5f, MeshGen.IslandHeight(20f, 4.2f, 7.5f, -3f, 11), -3f), island, new System.Random(6));
        }

        private void AddLabel(Transform parent, Vector3 localPos, string text, int size, float charSize, Color color, Vector3 euler)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(euler);
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.font = worldFont;
            tm.fontSize = size;
            tm.characterSize = charSize;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            tm.GetComponent<MeshRenderer>().sharedMaterial = worldFont.material;
        }

        private void BuildDockLegacy()
        {
            var dock = new GameObject("Dock");
            dock.transform.SetParent(transform);
            dock.transform.position = new Vector3(0f, 0.85f, 0f);

            for (int i = 0; i < 14; i++)
            {
                float z = 17f + i * 1.05f;
                var plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plank.transform.SetParent(dock.transform);
                plank.transform.localPosition = new Vector3(0f, 0f, z);
                plank.transform.localScale = new Vector3(2.6f, 0.16f, 0.94f);
                plank.GetComponent<MeshRenderer>().sharedMaterial = (i % 3 == 0) ? woodDarkMat : woodMat;
            }

            for (int s = -1; s <= 1; s += 2)
            {
                for (int i = 0; i < 4; i++)
                {
                    var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    post.transform.SetParent(dock.transform);
                    post.transform.localPosition = new Vector3(1.2f * s, -0.9f, 18f + i * 3.4f);
                    post.transform.localScale = new Vector3(0.22f, 1.4f, 0.22f);
                    post.GetComponent<MeshRenderer>().sharedMaterial = woodDarkMat;
                }
            }

            var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.transform.SetParent(dock.transform);
            rail.transform.localPosition = new Vector3(-1.3f, 0.55f, 22f);
            rail.transform.localScale = new Vector3(0.14f, 0.18f, 4.4f);
            rail.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
        }

        private void BuildBoatLegacy()
        {
            var boatRoot = new GameObject("Rowboat");
            boatRoot.transform.SetParent(transform);
            boatRoot.transform.position = new Vector3(4.2f, 0.32f, 25.5f);
            boatRoot.transform.rotation = Quaternion.Euler(0f, 8f, 0f);

            var hull = new GameObject("Hull");
            hull.transform.SetParent(boatRoot.transform, false);
            var hullBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hullBody.transform.SetParent(hull.transform);
            hullBody.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            hullBody.transform.localScale = new Vector3(1.9f, 0.62f, 3.9f);
            hullBody.GetComponent<MeshRenderer>().sharedMaterial = hullMat;
            DestroyImmediate(hullBody.GetComponent<Collider>());

            var inner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            inner.transform.SetParent(hull.transform);
            inner.transform.localPosition = new Vector3(0f, 0.36f, 0f);
            inner.transform.localScale = new Vector3(1.45f, 0.2f, 3.2f);
            inner.GetComponent<MeshRenderer>().sharedMaterial = woodDarkMat;
            DestroyImmediate(inner.GetComponent<Collider>());

            var bow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bow.transform.SetParent(hull.transform);
            bow.transform.localPosition = new Vector3(0f, 0.12f, 2.05f);
            bow.transform.localScale = new Vector3(1.2f, 0.5f, 0.9f);
            bow.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            bow.GetComponent<MeshRenderer>().sharedMaterial = hullMat;
            DestroyImmediate(bow.GetComponent<Collider>());

            for (int i = 0; i < 2; i++)
            {
                var bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bench.transform.SetParent(hull.transform);
                bench.transform.localPosition = new Vector3(0f, 0.42f, -0.85f + i * 1.6f);
                bench.transform.localScale = new Vector3(1.7f, 0.09f, 0.42f);
                bench.GetComponent<MeshRenderer>().sharedMaterial = trimMat;
                DestroyImmediate(bench.GetComponent<Collider>());
            }

            var rim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rim.transform.SetParent(hull.transform);
            rim.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            rim.transform.localScale = new Vector3(2.0f, 0.08f, 4.0f);
            rim.GetComponent<MeshRenderer>().sharedMaterial = trimMat;
            DestroyImmediate(rim.GetComponent<Collider>());

            var trigger = boatRoot.AddComponent<BoxCollider>();
            trigger.size = new Vector3(2.3f, 1.4f, 4.6f);
            trigger.center = new Vector3(0f, 0.35f, 0f);
            trigger.isTrigger = true;

            var boat = boatRoot.AddComponent<Boat>();
            boat.hull = hull.transform;
            boat.interact = boatRoot.AddComponent<Interactable>();
            boat.interact.prompt = "E   Board the rowboat";
            boat.interact.onInteract = () => GameBoot.I.ToggleBoat();
            BoatObject = boatRoot;
            Boat = boat;
        }
    }

    public class Ocean : MonoBehaviour
    {
        public Mesh mesh;
        public Vector3[] baseVerts;
        public Material mat;
        private Vector3[] work;
        private float t;

        private void Awake()
        {
            if (baseVerts != null) work = (Vector3[])baseVerts.Clone();
        }

        private void Update()
        {
            if (mesh == null || work == null) return;
            t += Time.deltaTime;
            for (int i = 0; i < work.Length; i++)
            {
                var v = baseVerts[i];
                float wave = Mathf.Sin(v.x * 0.18f + t * 1.1f) * 0.14f
                           + Mathf.Sin(v.z * 0.23f + t * 0.8f) * 0.1f
                           + Mathf.Sin((v.x + v.z) * 0.09f + t * 0.55f) * 0.12f;
                work[i] = new Vector3(v.x, wave + 0.12f, v.z);
            }
            mesh.vertices = work;
            mesh.RecalculateNormals();
        }
    }
}
