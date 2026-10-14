#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace A350NeoConcept.Editor
{
    /// <summary>
    /// Builds a detailed, interactive, A350-900-based neo concept as a Unity prefab,
    /// a standalone showcase scene and a static Wavefront OBJ export.
    /// </summary>
    public static class A350NeoConceptBuilder
    {
        private const string BasePath = "Assets/A350NeoConcept";
        private const string GeneratedPath = BasePath + "/Generated";
        private const string MaterialsPath = BasePath + "/Materials";
        private const string PrefabPath = BasePath + "/A350-900_NEO_Concept.prefab";
        private const string ScenePath = BasePath + "/A350-900_NEO_Showcase.unity";
        private const string ObjPath = BasePath + "/A350-900_NEO_Concept.obj";

        private static int meshIndex;
        private static Mesh boxMesh;
        private static Mesh sphereMesh;
        private static Mesh cylinderMesh;
        private static WingStation[] mainWing;
        private static WingStation[] tailWing;
        private static Material white;
        private static Material paleGrey;
        private static Material navy;
        private static Material cyan;
        private static Material glass;
        private static Material frame;
        private static Material metal;
        private static Material rubber;
        private static Material cabinWall;
        private static Material cabinFloor;
        private static Material seatBlue;
        private static Material seatPremium;
        private static Material seatFrame;
        private static Material screen;
        private static Material lightMat;
        private static Material engineMat;
        private static Material fanMat;
        private static Material runwayMat;

        [MenuItem("A350 NEO Concept/Build or Rebuild Model and Showcase")]
        public static void BuildProject()
        {
            EnsureFolders();
            Scene previousShowcase = SceneManager.GetSceneByPath(ScenePath);
            bool hasOtherLoadedScene = false;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene loaded = SceneManager.GetSceneAt(i);
                if (loaded.isLoaded && !string.IsNullOrEmpty(loaded.path) && loaded.path != ScenePath) hasOtherLoadedScene = true;
            }
            if (previousShowcase.IsValid() && previousShowcase.isLoaded && hasOtherLoadedScene)
                EditorSceneManager.CloseScene(previousShowcase, true);
            if (AssetDatabase.IsValidFolder(GeneratedPath)) AssetDatabase.DeleteAsset(GeneratedPath);
            AssetDatabase.CreateFolder(BasePath, "Generated");
            meshIndex = 0;
            InitializePrimitives();
            CreateMaterials();

            NewSceneMode sceneMode = hasOtherLoadedScene ? NewSceneMode.Additive : NewSceneMode.Single;
            Scene showcase = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, sceneMode);
            SceneManager.SetActiveScene(showcase);
            GameObject aircraft = BuildAircraft();
            SetupShowcase(showcase, aircraft.transform);
            ExportObj(aircraft.transform, ObjPath);

            bool saved;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(aircraft, PrefabPath, out saved);
            if (!saved || prefab == null) throw new InvalidOperationException("Unity could not save the A350 prefab.");
            UnityEngine.Object.DestroyImmediate(aircraft);
            var instance = PrefabUtility.InstantiatePrefab(prefab, showcase) as GameObject;
            if (instance == null) throw new InvalidOperationException("Unity could not place the A350 prefab in the showcase scene.");

            EditorSceneManager.MarkSceneDirty(showcase);
            if (!EditorSceneManager.SaveScene(showcase, ScenePath))
                throw new InvalidOperationException("Unity could not save the A350 showcase scene.");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            Debug.Log("Built the A350-900 neo concept. Prefab: " + PrefabPath + " | Showcase: " + ScenePath + " | OBJ: " + ObjPath);
        }

        [MenuItem("A350 NEO Concept/Open Showcase Scene")]
        public static void OpenShowcase()
        {
            if (!File.Exists(ScenePath))
            {
                Debug.LogWarning("Build the A350 showcase first from A350 NEO Concept > Build or Rebuild Model and Showcase.");
                return;
            }
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(BasePath)) AssetDatabase.CreateFolder("Assets", "A350NeoConcept");
            if (!AssetDatabase.IsValidFolder(BasePath + "/Runtime")) AssetDatabase.CreateFolder(BasePath, "Runtime");
            if (!AssetDatabase.IsValidFolder(BasePath + "/Editor")) AssetDatabase.CreateFolder(BasePath, "Editor");
            if (!AssetDatabase.IsValidFolder(MaterialsPath)) AssetDatabase.CreateFolder(BasePath, "Materials");
        }

        private static void InitializePrimitives()
        {
            boxMesh = SaveMesh(PrimitiveMesh(PrimitiveType.Cube, "A350 Cube Source"));
            sphereMesh = SaveMesh(PrimitiveMesh(PrimitiveType.Sphere, "A350 Sphere Source"));
            cylinderMesh = SaveMesh(PrimitiveMesh(PrimitiveType.Cylinder, "A350 Cylinder Source"));
            mainWing = new[]
            {
                new WingStation(2.25f, 4.8f, 13.4f, .65f),
                new WingStation(7.0f, 2.2f, 11.2f, .57f),
                new WingStation(14.0f, -1.2f, 8.1f, .43f),
                new WingStation(22.0f, -5.1f, 5.05f, .29f),
                new WingStation(28.0f, -9.0f, 2.9f, .17f),
                new WingStation(32.375f, -11.7f, 1.25f, .08f)
            };
            tailWing = new[]
            {
                new WingStation(1.4f, -23.4f, 7.6f, .34f),
                new WingStation(5.5f, -25.2f, 6.1f, .27f),
                new WingStation(9.7f, -27.2f, 4.35f, .18f),
                new WingStation(12.3f, -28.6f, 3.0f, .10f)
            };
        }

        private static Mesh PrimitiveMesh(PrimitiveType type, string name)
        {
            GameObject temporary = GameObject.CreatePrimitive(type);
            Mesh mesh = UnityEngine.Object.Instantiate(temporary.GetComponent<MeshFilter>().sharedMesh);
            mesh.name = name;
            UnityEngine.Object.DestroyImmediate(temporary);
            return mesh;
        }

        private static void CreateMaterials()
        {
            white = MaterialAsset("Airframe Ivory", new Color(.91f, .94f, .95f), .25f, .42f);
            paleGrey = MaterialAsset("Wing Ceramic Grey", new Color(.68f, .76f, .79f), .3f, .42f);
            navy = MaterialAsset("Concept Navy", new Color(.025f, .12f, .22f), .34f, .38f);
            cyan = MaterialAsset("Aqua Accent", new Color(.0f, .72f, .76f), .24f, .4f);
            glass = MaterialAsset("Aircraft Glazing", new Color(.015f, .055f, .10f), .5f, .72f);
            frame = MaterialAsset("Charcoal Trim", new Color(.075f, .11f, .14f), .45f, .4f);
            metal = MaterialAsset("Brushed Titanium", new Color(.50f, .60f, .65f), .8f, .62f);
            rubber = MaterialAsset("Tyre Rubber", new Color(.035f, .042f, .048f), .0f, .28f);
            cabinWall = MaterialAsset("Cabin Warm Grey", new Color(.78f, .81f, .80f), .06f, .27f);
            cabinFloor = MaterialAsset("Cabin Carpet", new Color(.14f, .19f, .23f), .0f, .3f);
            seatBlue = MaterialAsset("Economy Upholstery", new Color(.035f, .20f, .31f), .03f, .42f);
            seatPremium = MaterialAsset("Premium Upholstery", new Color(.29f, .14f, .075f), .04f, .38f);
            seatFrame = MaterialAsset("Seat Structure", new Color(.28f, .33f, .36f), .55f, .44f);
            screen = MaterialAsset("Seatback Screens", new Color(.008f, .025f, .035f), .15f, .74f);
            lightMat = MaterialAsset("Cabin Light", new Color(.87f, .91f, .85f), .0f, .2f, new Color(.82f, .9f, .82f) * 1.3f);
            engineMat = MaterialAsset("Engine Nacelle", new Color(.74f, .80f, .82f), .55f, .55f);
            fanMat = MaterialAsset("Fan Blades", new Color(.16f, .23f, .26f), .72f, .58f);
            runwayMat = MaterialAsset("Display Apron", new Color(.24f, .29f, .32f), .08f, .3f);
        }

        private static Material MaterialAsset(string name, Color color, float metallic, float smoothness, Color? emission = null)
        {
            string path = MaterialsPath + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.name = name;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);
            if (material.HasProperty("_CullMode")) material.SetFloat("_CullMode", 0f);
            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", emission.Value);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject BuildAircraft()
        {
            var root = new GameObject("A350-900 NEO Concept | 1:1 scale");
            var exterior = Child("Exterior", root.transform);
            Transform fuselageSkin = Child("Fuselage Pressure Shell | hidden in cutaway", exterior.transform).transform;
            var portSkin = Child("Port Fuselage Skin", fuselageSkin);
            var starboardSkin = Child("Starboard Fuselage Skin", fuselageSkin);
            CreateFuselageHalf(portSkin.transform, true);
            CreateFuselageHalf(starboardSkin.transform, false);
            AddFuselageStripe(portSkin.transform, -1);
            AddFuselageStripe(starboardSkin.transform, 1);
            AddSideWindows(portSkin.transform, -1);
            AddSideWindows(starboardSkin.transform, 1);
            CreateCockpitWindows(exterior.transform);
            CreateWing(exterior.transform, 1, mainWing, 8.65f, white, "Starboard Wing");
            CreateWing(exterior.transform, -1, mainWing, 8.65f, white, "Port Wing");
            CreateWingControls(exterior.transform, mainWing, out Transform[] flaps, out Transform[] ailerons, out Transform[] spoilers);
            CreateTail(exterior.transform, out Transform[] elevators, out Transform rudder);
            CreateEngines(exterior.transform);
            CreateWinglets(exterior.transform);
            Transform doorRoot = Child("Passenger Doors", exterior.transform).transform;
            Transform[] doors;
            CreatePassengerDoors(doorRoot, out doors);
            Transform gear = CreateLandingGear(exterior.transform);
            CreateExteriorDetails(exterior.transform);
            GameObject cabinLining;
            BuildCabin(root.transform, out cabinLining);

            var controls = root.AddComponent<A350NeoConceptControls>();
            controls.fuselageSkin = fuselageSkin.gameObject;
            controls.cabinLining = cabinLining;
            controls.landingGear = gear.gameObject;
            controls.passengerDoors = doors;
            controls.flapHinges = flaps;
            controls.aileronHinges = ailerons;
            controls.spoilerHinges = spoilers;
            controls.elevatorHinges = elevators;
            controls.rudderHinge = rudder;
            return root;
        }

        private static void CreateFuselageHalf(Transform parent, bool port)
        {
            float[,] stations =
            {
                {33.40f,.06f},{32.75f,.34f},{31.40f,.82f},{29.30f,1.52f},{26.20f,2.34f},
                {22.60f,2.91f},{16.00f,2.98f},{0.00f,2.98f},{-18.00f,2.98f},{-24.00f,2.83f},
                {-28.00f,2.38f},{-31.50f,1.48f},{-33.40f,.07f}
            };
            int angleSteps = 48;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            float start = port ? Mathf.PI * .5f : -Mathf.PI * .5f;
            float sign = port ? 1f : -1f;
            // Both halves meet at the keel and crown. The slightly overlapped seam avoids visible gaps.
            for (int s = 0; s < stations.GetLength(0); s++)
            {
                float z = stations[s, 0];
                float r = stations[s, 1];
                for (int a = 0; a <= angleSteps; a++)
                {
                    float angle = start + sign * Mathf.PI * a / angleSteps;
                    vertices.Add(new Vector3(r * Mathf.Cos(angle), 9.05f + r * Mathf.Sin(angle), z));
                    uv.Add(new Vector2(a / (float)angleSteps, s / (float)(stations.GetLength(0) - 1)));
                }
            }
            int ring = angleSteps + 1;
            for (int s = 0; s < stations.GetLength(0) - 1; s++)
            {
                for (int a = 0; a < angleSteps; a++)
                {
                    int p = s * ring + a;
                    if (port)
                    {
                        triangles.Add(p); triangles.Add(p + ring); triangles.Add(p + ring + 1);
                        triangles.Add(p); triangles.Add(p + ring + 1); triangles.Add(p + 1);
                    }
                    else
                    {
                        triangles.Add(p); triangles.Add(p + ring + 1); triangles.Add(p + ring);
                        triangles.Add(p); triangles.Add(p + 1); triangles.Add(p + ring + 1);
                    }
                }
            }
            Mesh hull = new Mesh { name = port ? "A350 Port Lofted Pressure Shell" : "A350 Starboard Lofted Pressure Shell", indexFormat = IndexFormat.UInt32 };
            hull.SetVertices(vertices); hull.SetUVs(0, uv); hull.SetTriangles(triangles, 0); hull.RecalculateNormals(); hull.RecalculateBounds();
            MeshFilterAndRenderer(parent.gameObject, SaveMesh(hull), white);
        }

        private static void AddFuselageStripe(Transform parent, int side)
        {
            var v = new List<Vector3>();
            var t = new List<int>();
            float[] z = { 25f, 22.5f, 16f, 8f, 0f, -8f, -16f, -23f, -27f };
            for (int i = 0; i < z.Length; i++)
            {
                float r = RadiusAt(z[i]);
                float angle = Mathf.Asin(-.48f / r);
                for (int k = 0; k < 2; k++)
                {
                    float a = angle + (k == 0 ? -.027f : .027f);
                    v.Add(new Vector3(side * (r + .018f) * Mathf.Cos(a), 9.05f + (r + .018f) * Mathf.Sin(a), z[i]));
                }
            }
            for (int i = 0; i < z.Length - 1; i++)
            {
                int p = i * 2;
                t.Add(p); t.Add(p + 2); t.Add(p + 3); t.Add(p); t.Add(p + 3); t.Add(p + 1);
            }
            var mesh = new Mesh { name = "Aqua fuselage pinstripe" };
            mesh.SetVertices(v); mesh.SetTriangles(t, 0); mesh.RecalculateNormals();
            MeshFilterAndRenderer(parent.gameObject, SaveMesh(mesh), cyan);
        }

        private static void AddSideWindows(Transform parent, int side)
        {
            float y = 9.05f + 1.12f;
            int index = 1;
            for (float z = 23.2f; z >= -21.8f; z -= 1.13f)
            {
                float radius = RadiusAt(z);
                float x = side * (Mathf.Sqrt(Mathf.Max(.1f, radius * radius - 1.12f * 1.12f)) + .035f);
                var window = MeshObject("Cabin window " + index.ToString("00"), parent, sphereMesh, glass,
                    new Vector3(x, y, z), Quaternion.identity, new Vector3(.075f, .16f, .245f));
                window.transform.localScale = new Vector3(.075f, .16f, .245f);
                index++;
            }
        }

        private static void CreateCockpitWindows(Transform parent)
        {
            Transform group = Child("Wraparound cockpit glazing", parent).transform;
            float[] centers = { -1.25f, -.75f, -.25f, .25f, .75f, 1.25f };
            for (int i = 0; i < centers.Length; i++)
            {
                float x = centers[i];
                float half = .225f;
                Vector3 p = new Vector3(x, 10.58f, 27.80f - .12f * Mathf.Abs(x));
                MeshObject("Cockpit windscreen " + (i + 1), group, boxMesh, glass, p,
                    Quaternion.Euler(-19f, 0f, x * 2f), new Vector3(half * 2f, .52f, .055f));
                MeshObject("Cockpit frame " + (i + 1), group, boxMesh, frame, p + new Vector3(0f, .275f, -.005f),
                    Quaternion.Euler(-19f, 0f, x * 2f), new Vector3(half * 2f + .035f, .04f, .07f));
            }
            MeshObject("Flight deck eyebrow", group, boxMesh, navy, new Vector3(0f, 10.91f, 27.48f),
                Quaternion.identity, new Vector3(3.75f, .09f, .16f));
        }

        private static void CreateWing(Transform parent, int side, WingStation[] stations, float baseY, Material material, string name)
        {
            Mesh mesh = WingMesh(side, stations, baseY, name);
            MeshFilterAndRenderer(Child(name, parent).gameObject, SaveMesh(mesh), material);
        }

        private static Mesh WingMesh(int side, WingStation[] stations, float baseY, string name)
        {
            float[] chordSample = { 0f, .025f, .08f, .18f, .32f, .48f, .64f, .78f, .90f, .97f, 1f };
            int loop = chordSample.Length * 2;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int s = 0; s < stations.Length; s++)
            {
                WingStation st = stations[s];
                for (int i = 0; i < chordSample.Length; i++)
                {
                    float u = chordSample[i];
                    float thick = AirfoilThickness(u) * st.thickness;
                    float z = st.leadingZ - u * st.chord;
                    float camber = .018f * st.chord * Mathf.Sin(Mathf.PI * u);
                    vertices.Add(new Vector3(side * st.x, baseY + camber + thick, z));
                }
                for (int i = chordSample.Length - 1; i >= 0; i--)
                {
                    float u = chordSample[i];
                    float thick = AirfoilThickness(u) * st.thickness;
                    float z = st.leadingZ - u * st.chord;
                    float camber = .018f * st.chord * Mathf.Sin(Mathf.PI * u);
                    vertices.Add(new Vector3(side * st.x, baseY + camber - thick, z));
                }
            }
            for (int s = 0; s < stations.Length - 1; s++)
            {
                for (int j = 0; j < loop; j++)
                {
                    int a = s * loop + j;
                    int d = s * loop + (j + 1) % loop;
                    int b = (s + 1) * loop + j;
                    int c = (s + 1) * loop + (j + 1) % loop;
                    if (side > 0) { triangles.Add(a); triangles.Add(b); triangles.Add(c); triangles.Add(a); triangles.Add(c); triangles.Add(d); }
                    else { triangles.Add(a); triangles.Add(c); triangles.Add(b); triangles.Add(a); triangles.Add(d); triangles.Add(c); }
                }
            }
            Vector3 rootCenter = Vector3.zero, tipCenter = Vector3.zero;
            for (int j = 0; j < loop; j++)
            {
                rootCenter += vertices[j];
                tipCenter += vertices[(stations.Length - 1) * loop + j];
            }
            int rootCap = vertices.Count; vertices.Add(rootCenter / loop);
            int tipCap = vertices.Count; vertices.Add(tipCenter / loop);
            for (int j = 0; j < loop; j++)
            {
                int next = (j + 1) % loop;
                int tipA = (stations.Length - 1) * loop + j;
                int tipB = (stations.Length - 1) * loop + next;
                if (side > 0)
                {
                    triangles.Add(rootCap); triangles.Add(next); triangles.Add(j);
                    triangles.Add(tipCap); triangles.Add(tipA); triangles.Add(tipB);
                }
                else
                {
                    triangles.Add(rootCap); triangles.Add(j); triangles.Add(next);
                    triangles.Add(tipCap); triangles.Add(tipB); triangles.Add(tipA);
                }
            }
            var mesh = new Mesh { name = name + " swept cambered airfoil", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private static float AirfoilThickness(float u)
        {
            float x = Mathf.Clamp01(u);
            return 5f * .105f * (.2969f * Mathf.Sqrt(Mathf.Max(.00001f, x)) - .1260f * x - .3516f * x * x + .2843f * x * x * x - .1036f * x * x * x * x);
        }

        private static void CreateWingControls(Transform parent, WingStation[] stations, out Transform[] flaps, out Transform[] ailerons, out Transform[] spoilers)
        {
            var flapList = new List<Transform>();
            var aileronList = new List<Transform>();
            var spoilerList = new List<Transform>();
            for (int side = -1; side <= 1; side += 2)
            {
                flapList.Add(CreateWingPanel(parent, "Inboard flap", side, 4.4f, 12.7f, .72f, stations, 8.65f, paleGrey));
                flapList.Add(CreateWingPanel(parent, "Outboard flap", side, 12.8f, 21.5f, .74f, stations, 8.65f, paleGrey));
                aileronList.Add(CreateWingPanel(parent, "Aileron", side, 23.0f, 29.6f, .76f, stations, 8.65f, white));
                for (int i = 0; i < 3; i++)
                {
                    float x = 6.1f + i * 5.2f;
                    float lead = InterpolateWing(x, stations).leadingZ;
                    float chord = InterpolateWing(x, stations).chord;
                    Vector3 pivot = new Vector3(side * x, 8.91f, lead - chord * .46f);
                    Transform spoiler = Child((side < 0 ? "Port" : "Starboard") + " spoiler " + (i + 1), parent).transform;
                    spoiler.localPosition = pivot;
                    spoilerList.Add(spoiler);
                    Vector3 center = new Vector3(0f, .025f, -chord * .08f);
                    MeshObject("Spoiler panel", spoiler, boxMesh, paleGrey, center, Quaternion.identity, new Vector3(1.45f, .09f, 1.05f));
                }
            }
            flaps = flapList.ToArray(); ailerons = aileronList.ToArray(); spoilers = spoilerList.ToArray();
        }

        private static Transform CreateWingPanel(Transform parent, string name, int side, float startX, float endX, float hingeFraction, WingStation[] stations, float y, Material material)
        {
            WingStation start = InterpolateWing(startX, stations);
            Transform pivot = Child((side < 0 ? "Port " : "Starboard ") + name, parent).transform;
            float hingeZ = start.leadingZ - start.chord * hingeFraction;
            pivot.localPosition = new Vector3(side * startX, y, hingeZ);
            const int count = 9;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int i = 0; i <= count; i++)
            {
                float f = i / (float)count;
                float x = Mathf.Lerp(startX, endX, f);
                WingStation st = InterpolateWing(x, stations);
                float xLocal = side * (x - startX);
                float hinge = st.leadingZ - st.chord * hingeFraction;
                float trailing = st.leadingZ - st.chord;
                vertices.Add(new Vector3(xLocal, .045f, hinge - hingeZ));
                vertices.Add(new Vector3(xLocal, .045f, trailing - hingeZ));
                vertices.Add(new Vector3(xLocal, -.095f, hinge - hingeZ));
                vertices.Add(new Vector3(xLocal, -.095f, trailing - hingeZ));
            }
            for (int i = 0; i < count; i++)
            {
                int p = i * 4, q = (i + 1) * 4;
                triangles.Add(p); triangles.Add(q); triangles.Add(q + 1); triangles.Add(p); triangles.Add(q + 1); triangles.Add(p + 1);
                triangles.Add(p + 2); triangles.Add(p + 3); triangles.Add(q + 3); triangles.Add(p + 2); triangles.Add(q + 3); triangles.Add(q + 2);
                triangles.Add(p); triangles.Add(p + 2); triangles.Add(q + 2); triangles.Add(p); triangles.Add(q + 2); triangles.Add(q);
                triangles.Add(p + 1); triangles.Add(q + 1); triangles.Add(q + 3); triangles.Add(p + 1); triangles.Add(q + 3); triangles.Add(p + 3);
            }
            Mesh mesh = new Mesh { name = name + " articulated mesh" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            MeshFilterAndRenderer(pivot.gameObject, SaveMesh(mesh), material);
            return pivot;
        }

        private static WingStation InterpolateWing(float x, WingStation[] stations)
        {
            if (x <= stations[0].x) return stations[0];
            for (int i = 0; i < stations.Length - 1; i++)
            {
                if (x <= stations[i + 1].x)
                {
                    float t = Mathf.InverseLerp(stations[i].x, stations[i + 1].x, x);
                    return new WingStation(x, Mathf.Lerp(stations[i].leadingZ, stations[i + 1].leadingZ, t),
                        Mathf.Lerp(stations[i].chord, stations[i + 1].chord, t), Mathf.Lerp(stations[i].thickness, stations[i + 1].thickness, t));
                }
            }
            return stations[stations.Length - 1];
        }

        private static void CreateTail(Transform parent, out Transform[] elevators, out Transform rudder)
        {
            CreateWing(parent, 1, tailWing, 10.95f, paleGrey, "Starboard Horizontal Stabiliser");
            CreateWing(parent, -1, tailWing, 10.95f, paleGrey, "Port Horizontal Stabiliser");
            var elevatorList = new List<Transform>();
            elevatorList.Add(CreateWingPanel(parent, "Elevator", 1, 2.0f, 11.6f, .68f, tailWing, 10.95f, white));
            elevatorList.Add(CreateWingPanel(parent, "Elevator", -1, 2.0f, 11.6f, .68f, tailWing, 10.95f, white));
            elevators = elevatorList.ToArray();

            Vector3[] outline =
            {
                new Vector3(0f, 9.50f, -23.4f), new Vector3(0f, 16.98f, -29.7f),
                new Vector3(0f, 16.98f, -32.0f), new Vector3(0f, 10.0f, -33.1f)
            };
            Mesh fin = ExtrudedOutline(outline, "A350 vertical tail fin");
            Transform finRoot = Child("Vertical stabiliser | aqua/navy", parent).transform;
            MeshFilterAndRenderer(finRoot.gameObject, SaveMesh(fin), navy);
            CreateTailStripe(finRoot);

            rudder = Child("Rudder hinge", parent).transform;
            rudder.localPosition = new Vector3(0f, 13.2f, -31.95f);
            MeshObject("Rudder", rudder, boxMesh, paleGrey, new Vector3(0f, .45f, -.32f), Quaternion.identity, new Vector3(.20f, 3.5f, .72f));
            MeshObject("Rudder seam", rudder, boxMesh, cyan, new Vector3(0f, .45f, -.025f), Quaternion.identity, new Vector3(.215f, 3.42f, .035f));
        }

        private static void CreateTailStripe(Transform parent)
        {
            Vector3[] stripe =
            {
                new Vector3(-.071f,10.0f,-25.0f), new Vector3(-.071f,10.2f,-25.25f), new Vector3(-.071f,16.7f,-31.55f),
                new Vector3(-.071f,16.7f,-31.28f), new Vector3(-.071f,10.4f,-25.0f)
            };
            var mesh = PolygonMesh(stripe, "Diagonal aqua fin flash");
            MeshFilterAndRenderer(Child("Aqua fin flash", parent).gameObject, SaveMesh(mesh), cyan);
        }

        private static void CreateWinglets(Transform parent)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3[] pts =
                {
                    new Vector3(side * 31.65f,8.65f,-10.0f), new Vector3(side * 32.25f,10.2f,-10.35f),
                    new Vector3(side * 32.40f,11.9f,-12.6f), new Vector3(side * 32.15f,11.65f,-13.5f),
                    new Vector3(side * 31.35f,8.65f,-12.8f)
                };
                Transform winglet = Child((side < 0 ? "Port" : "Starboard") + " blended wingtip fence", parent).transform;
                MeshFilterAndRenderer(winglet.gameObject, SaveMesh(ExtrudedOutline(pts, "A350 blended winglet")), navy);
                MeshObject("Wingtip fairing", winglet, sphereMesh, cyan, new Vector3(side * 32.1f, 10.9f, -12.7f), Quaternion.identity, new Vector3(.16f, .38f, .52f));
            }
        }

        private static void CreateEngines(Transform parent)
        {
            float[] profileZ = { 3.15f, 2.94f, 2.55f, 1.85f, .55f, -.95f, -2.10f, -2.68f, -2.92f, -3.04f };
            float[] profileR = { 1.30f, 1.58f, 1.75f, 1.77f, 1.68f, 1.52f, 1.36f, 1.22f, .98f, .72f };
            for (int side = -1; side <= 1; side += 2)
            {
                float[] xStations = { 8.2f, 19.2f };
                for (int e = 0; e < xStations.Length; e++)
                {
                    Transform engine = Child((side < 0 ? "Port" : "Starboard") + " Trent-XWB-style turbofan " + (e + 1), parent).transform;
                    engine.localPosition = new Vector3(side * xStations[e], 6.85f, e == 0 ? -.2f : -4.5f);
                    Mesh shell = Lathe(profileZ, profileR, 48, "High-bypass turbofan nacelle");
                    MeshFilterAndRenderer(engine.gameObject, SaveMesh(shell), engineMat);
                    CreateEngineInlet(engine);
                    CreatePylon(parent, side, xStations[e], e == 0 ? -.2f : -4.5f);
                    MeshObject("Exhaust nozzle", engine, SaveMesh(Annulus(.70f, .95f, -3.04f, 48, "Turbofan exhaust rim")), metal,
                        Vector3.zero, Quaternion.identity, Vector3.one);
                }
            }
        }

        private static void CreateEngineInlet(Transform engine)
        {
            MeshObject("Fan face", engine, cylinderMesh, frame, new Vector3(0f, 0f, 2.83f),
                Quaternion.Euler(90f,0f,0f), new Vector3(2.5f, .08f, 2.5f));
            MeshObject("Spinner", engine, SaveMesh(Lathe(new[] { 2.90f, 2.46f, 2.12f }, new[] { .13f, .24f, .42f }, 24, "Engine spinner")),
                metal, Vector3.zero, Quaternion.identity, Vector3.one);
            Transform fan = Child("Fan blades", engine).transform;
            for (int i = 0; i < 22; i++)
            {
                float angle = i * (360f / 22f);
                float r = .84f;
                float rad = angle * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Cos(rad) * r, Mathf.Sin(rad) * r, 2.79f);
                MeshObject("Curved fan blade", fan, boxMesh, fanMat, p, Quaternion.Euler(0f, 0f, angle - 8f), new Vector3(.11f, .91f, .075f));
            }
            MeshObject("Inlet lip", engine, SaveMesh(Annulus(1.46f, 1.69f, 2.98f, 48, "Titanium intake lip")), metal,
                Vector3.zero, Quaternion.identity, Vector3.one);
            MeshObject("Spinner nose", engine, sphereMesh, metal, new Vector3(0f, 0f, 2.94f), Quaternion.identity, new Vector3(.32f, .32f, .43f));
        }

        private static void CreatePylon(Transform parent, int side, float x, float z)
        {
            Transform pylon = Child("Engine pylon", parent).transform;
            pylon.localPosition = new Vector3(side * x, 0f, z);
            MeshObject("Pylon core", pylon, boxMesh, paleGrey, new Vector3(0f, 7.8f, -.1f), Quaternion.identity, new Vector3(1.25f, 1.2f, 3.15f));
            MeshObject("Pylon upper fairing", pylon, sphereMesh, white, new Vector3(0f, 8.25f, -.1f), Quaternion.identity, new Vector3(1.3f, .45f, 1.75f));
        }

        private static void CreatePassengerDoors(Transform parent, out Transform[] doors)
        {
            float[] positions = { 20.4f, 6.3f, -8.4f, -21.0f };
            var list = new List<Transform>();
            for (int i = 0; i < positions.Length; i++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    float z = positions[i];
                    float r = RadiusAt(z);
                    float x = side * (Mathf.Sqrt(r * r - .20f * .20f) + .08f);
                    Transform hinge = Child((side < 0 ? "Port" : "Starboard") + " passenger door " + (i + 1), parent).transform;
                    hinge.localPosition = new Vector3(x, 9.16f, z + .71f);
                    MeshObject("Raised door frame", hinge, boxMesh, frame, new Vector3(0f, 0f, -.71f), Quaternion.identity, new Vector3(.18f, 2.36f, 1.56f));
                    MeshObject("Pressure door leaf", hinge, boxMesh, white, new Vector3(side * .10f, 0f, -.71f), Quaternion.identity, new Vector3(.14f, 2.22f, 1.42f));
                    MeshObject("Door window", hinge, sphereMesh, glass, new Vector3(side * .18f, .57f, -.71f), Quaternion.identity, new Vector3(.045f, .26f, .22f));
                    MeshObject("Door handle", hinge, boxMesh, metal, new Vector3(side * .19f, -.36f, -.19f), Quaternion.identity, new Vector3(.045f, .20f, .06f));
                    list.Add(hinge);
                }
            }
            doors = list.ToArray();
        }

        private static Transform CreateLandingGear(Transform parent)
        {
            Transform gear = Child("Deployable landing gear", parent).transform;
            Transform nose = Child("Nose gear | twin wheels", gear).transform;
            nose.localPosition = new Vector3(0f, 0f, 19.6f);
            MeshObject("Nose strut", nose, boxMesh, metal, new Vector3(0f, 3.25f, 0f), Quaternion.identity, new Vector3(.19f, 5.0f, .23f));
            MeshObject("Nose torque link", nose, boxMesh, frame, new Vector3(0f, 2.1f, .05f), Quaternion.identity, new Vector3(.42f, 1.15f, .10f));
            for (int side = -1; side <= 1; side += 2) AddWheel(nose, new Vector3(side * .36f, .62f, 0f), .39f, .26f, side);
            MeshObject("Nose gear axle", nose, cylinderMesh, metal, new Vector3(0f, .62f, 0f), Quaternion.Euler(0f, 0f, 90f), new Vector3(.22f, .18f, .22f));

            for (int side = -1; side <= 1; side += 2)
            {
                Transform main = Child((side < 0 ? "Port" : "Starboard") + " main bogie | six wheels", gear).transform;
                main.localPosition = new Vector3(side * 6.2f, 0f, -7.7f);
                MeshObject("Shock strut", main, boxMesh, metal, new Vector3(0f, 3.3f, .25f), Quaternion.identity, new Vector3(.28f, 5.0f, .33f));
                MeshObject("Bogie beam", main, boxMesh, frame, new Vector3(0f, .90f, -.2f), Quaternion.identity, new Vector3(.30f, .22f, 2.15f));
                MeshObject("Torque links", main, boxMesh, metal, new Vector3(0f, 2.2f, -.05f), Quaternion.Euler(18f, 0f, 0f), new Vector3(.18f, 2.7f, .12f));
                for (int axle = -1; axle <= 1; axle++)
                {
                    float z = -.2f + axle * .73f;
                    MeshObject("Axle", main, cylinderMesh, metal, new Vector3(0f, .82f, z), Quaternion.Euler(0f, 0f, 90f), new Vector3(.22f, .30f, .22f));
                    for (int wheelSide = -1; wheelSide <= 1; wheelSide += 2)
                        AddWheel(main, new Vector3(wheelSide * .36f, .82f, z), .54f, .28f, wheelSide);
                }
            }
            return gear;
        }

        private static void AddWheel(Transform parent, Vector3 position, float radius, float width, int outerSign)
        {
            MeshObject("Black radial tyre", parent, cylinderMesh, rubber, position, Quaternion.Euler(0f, 0f, 90f),
                new Vector3(radius * 2f, width * .5f, radius * 2f));
            MeshObject("Titanium wheel hub", parent, cylinderMesh, metal,
                position + new Vector3(outerSign * width * .31f, 0f, 0f), Quaternion.Euler(0f, 0f, 90f),
                new Vector3(radius * .82f, width * .18f, radius * .82f));
        }

        private static void CreateExteriorDetails(Transform parent)
        {
            Transform details = Child("Antennas, strobes and service details", parent).transform;
            MeshObject("Upper VHF aerial", details, boxMesh, white, new Vector3(0f, 11.94f, 8f), Quaternion.identity, new Vector3(.10f, .22f, .56f));
            MeshObject("Satellite dome", details, sphereMesh, white, new Vector3(0f, 11.91f, -9.4f), Quaternion.identity, new Vector3(.75f, .18f, 1.12f));
            MeshObject("Tail beacon", details, sphereMesh, lightMat, new Vector3(0f, 11.96f, -19.5f), Quaternion.identity, new Vector3(.19f, .11f, .19f));
            MeshObject("APU outlet", details, cylinderMesh, frame, new Vector3(0f, 9.05f, -33.1f), Quaternion.Euler(90f, 0f, 0f), new Vector3(.66f, .07f, .66f));
            MeshObject("Port navigation lamp", details, sphereMesh, MaterialAsset("Port Nav Lamp", new Color(.72f,.02f,.04f),.1f,.3f,new Color(.9f,.005f,.01f)), new Vector3(-32.20f,8.73f,-12.5f), Quaternion.identity, new Vector3(.25f,.18f,.32f));
            MeshObject("Starboard navigation lamp", details, sphereMesh, MaterialAsset("Starboard Nav Lamp", new Color(.02f,.63f,.20f),.1f,.3f,new Color(.02f,.8f,.08f)), new Vector3(32.20f,8.73f,-12.5f), Quaternion.identity, new Vector3(.25f,.18f,.32f));
            for (int side = -1; side <= 1; side += 2)
            {
                MeshObject("Wing strobe", details, sphereMesh, lightMat, new Vector3(side * 31.8f, 8.93f, -11.6f), Quaternion.identity, new Vector3(.16f,.09f,.19f));
                MeshObject("Static wick", details, boxMesh, metal, new Vector3(side * 31.5f,8.66f,-13.2f), Quaternion.Euler(0f,0f,side * -12f), new Vector3(.025f,.06f,.68f));
                MeshObject("Pitot probe", details, cylinderMesh, metal, new Vector3(side * 2.58f,9.25f,27.0f), Quaternion.Euler(0f,0f,side * -90f), new Vector3(.055f,.055f,.68f));
            }
            for (int i = 0; i < 14; i++)
            {
                float z = 16.0f - i * 2.2f;
                MeshObject("Belly access panel", details, boxMesh, paleGrey, new Vector3(0f, 6.12f, z), Quaternion.identity, new Vector3(1.35f,.055f,.82f));
            }
        }

        private static void BuildCabin(Transform root, out GameObject cabinLining)
        {
            Transform interior = Child("Interior | cockpit, 295-seat cabin and service areas", root.transform).transform;
            var batch = new MeshBatch(interior, "Cabin Fittings");
            Transform lining = Child("Cabin lining | hidden in cutaway", interior).transform;
            var liningBatch = new MeshBatch(lining, "Cabin Lining");
            cabinLining = lining.gameObject;
            batch.Box(cabinFloor, new Vector3(0f, 7.58f, -.7f), Quaternion.identity, new Vector3(5.05f, .20f, 46.2f));
            liningBatch.Box(cabinWall, new Vector3(0f, 10.97f, -.7f), Quaternion.identity, new Vector3(4.95f,.18f,45.3f));
            for (int side = -1; side <= 1; side += 2)
            {
                liningBatch.Box(cabinWall, new Vector3(side * 2.40f, 9.10f, -.7f), Quaternion.identity, new Vector3(.17f, 2.9f, 45.3f));
                batch.Box(cabinWall, new Vector3(side * 2.13f, 10.18f, -.7f), Quaternion.identity, new Vector3(.56f,.67f,39.0f));
                batch.Box(cabinWall, new Vector3(side * 2.18f, 9.80f, -.7f), Quaternion.identity, new Vector3(.57f,.12f,38.4f));
                batch.Box(lightMat, new Vector3(side * .78f, 10.86f, -.7f), Quaternion.identity, new Vector3(.085f,.055f,42.2f));
                liningBatch.Box(cabinWall, new Vector3(side * 2.40f, 9.96f, 20.8f), Quaternion.identity, new Vector3(.17f,1.95f,1.45f));
                liningBatch.Box(cabinWall, new Vector3(side * 2.40f, 9.96f, -22.0f), Quaternion.identity, new Vector3(.17f,1.95f,1.45f));
            }
            liningBatch.Box(cabinWall, new Vector3(0f, 9.35f, 22.15f), Quaternion.identity, new Vector3(4.75f,3.05f,.20f));
            liningBatch.Box(cabinWall, new Vector3(0f, 9.35f, -23.05f), Quaternion.identity, new Vector3(4.75f,3.05f,.20f));

            BuildBusinessSeats(batch);
            BuildPremiumSeats(batch);
            BuildEconomySeats(batch);
            BuildCockpit(batch);
            BuildServiceAreas(batch);
            liningBatch.Flush();
            batch.Flush();

            // Cabin reading lamps and exit signs are separate emissive pieces for a readable cutaway.
            for (int row = 0; row < 29; row++)
            {
                float z = 4.6f - row * .97f;
                MeshObject("Economy row locator light", interior, sphereMesh, lightMat, new Vector3(0f, 10.82f, z), Quaternion.identity, new Vector3(.06f,.03f,.08f));
            }
            foreach (float z in new[] { 20.4f, 6.3f, -8.4f, -21.0f })
            {
                MeshObject("Illuminated exit indicator", interior, boxMesh, lightMat, new Vector3(0f, 10.78f, z), Quaternion.identity, new Vector3(.72f,.12f,.08f));
            }
        }

        private static void BuildBusinessSeats(MeshBatch batch)
        {
            float[] rows = { 18.0f, 16.55f, 15.10f, 13.65f, 12.20f, 10.75f };
            float[] x = { -2.03f, -.69f, .69f, 2.03f };
            foreach (float z in rows)
            {
                foreach (float cx in x)
                {
                    SeatPod(batch, cx, z, seatPremium, 1f, true);
                }
            }
        }

        private static void BuildPremiumSeats(MeshBatch batch)
        {
            float[] x = { -2.05f,-1.55f,-.47f,0f,.47f,1.55f,2.05f };
            for (int row = 0; row < 4; row++)
            {
                float z = 9.15f - row * .99f;
                foreach (float cx in x) Seat(batch, cx, z, seatPremium, .43f, .82f);
            }
        }

        private static void BuildEconomySeats(MeshBatch batch)
        {
            float[] x = { -2.03f,-1.57f,-1.11f,-.47f,0f,.47f,1.11f,1.57f,2.03f };
            for (int row = 0; row < 27; row++)
            {
                float z = 4.45f - row * .97f;
                foreach (float cx in x) Seat(batch, cx, z, seatBlue, .405f, .78f);
            }
        }

        private static void SeatPod(MeshBatch batch, float x, float z, Material upholstery, float width, bool business)
        {
            float w = business ? .86f : width;
            batch.Box(seatFrame, new Vector3(x, 7.89f, z), Quaternion.identity, new Vector3(w * .93f,.17f,.86f));
            batch.Box(upholstery, new Vector3(x, 8.12f, z - .10f), Quaternion.identity, new Vector3(w * .82f,.46f,.46f));
            batch.Box(upholstery, new Vector3(x, 8.40f, z - .39f), Quaternion.Euler(-8f,0f,0f), new Vector3(w * .76f,.88f,.22f));
            batch.Box(upholstery, new Vector3(x, 8.88f, z - .37f), Quaternion.identity, new Vector3(w * .56f,.19f,.23f));
            batch.Box(upholstery, new Vector3(x, 7.99f, z + .46f), Quaternion.identity, new Vector3(w * .68f,.18f,.56f));
            batch.Box(seatFrame, new Vector3(x - w * .50f, 8.18f, z - .02f), Quaternion.identity, new Vector3(.055f,.38f,.93f));
            batch.Box(seatFrame, new Vector3(x + w * .50f, 8.18f, z - .02f), Quaternion.identity, new Vector3(.055f,.38f,.93f));
            batch.Box(screen, new Vector3(x,8.35f,z-.265f), Quaternion.identity, new Vector3(w*.47f,.30f,.035f));
        }

        private static void Seat(MeshBatch batch, float x, float z, Material upholstery, float width, float scale)
        {
            float y = 7.82f;
            batch.Box(seatFrame, new Vector3(x, y + .18f, z), Quaternion.identity, new Vector3(width * .82f,.10f,.44f));
            batch.Box(upholstery, new Vector3(x, y + .30f, z + .035f), Quaternion.identity, new Vector3(width,.18f,.45f));
            batch.Box(upholstery, new Vector3(x, y + .64f, z - .18f), Quaternion.Euler(-5f,0f,0f), new Vector3(width,.62f,.14f));
            batch.Box(upholstery, new Vector3(x, y + .98f, z - .17f), Quaternion.identity, new Vector3(width * .79f,.18f,.16f));
            batch.Box(seatFrame, new Vector3(x - width*.62f, y + .37f, z + .015f), Quaternion.identity, new Vector3(.045f,.18f,.48f));
            batch.Box(seatFrame, new Vector3(x + width*.62f, y + .37f, z + .015f), Quaternion.identity, new Vector3(.045f,.18f,.48f));
            batch.Box(seatFrame, new Vector3(x - width*.27f,y+.09f,z-.12f), Quaternion.identity, new Vector3(.038f,.19f,.06f));
            batch.Box(seatFrame, new Vector3(x + width*.27f,y+.09f,z-.12f), Quaternion.identity, new Vector3(.038f,.19f,.06f));
            batch.Box(screen, new Vector3(x,y+.65f,z-.095f), Quaternion.identity, new Vector3(width*.58f,.23f,.025f));
            batch.Box(cabinWall, new Vector3(x,y+.44f,z+.27f), Quaternion.identity, new Vector3(width*.86f,.31f,.035f));
        }

        private static void BuildCockpit(MeshBatch batch)
        {
            batch.Box(cabinWall, new Vector3(0f, 8.70f, 22.15f), Quaternion.identity, new Vector3(4.45f,2.05f,.15f));
            batch.Box(frame, new Vector3(0f, 9.65f, 23.12f), Quaternion.identity, new Vector3(4.1f,.36f,.82f));
            batch.Box(cabinWall, new Vector3(0f, 9.27f, 23.05f), Quaternion.identity, new Vector3(4.0f,.16f,.48f));
            batch.Box(frame, new Vector3(-1.15f,9.70f,23.0f), Quaternion.identity, new Vector3(.16f,.55f,.76f));
            batch.Box(frame, new Vector3(1.15f,9.70f,23.0f), Quaternion.identity, new Vector3(.16f,.55f,.76f));
            for (int pilot = -1; pilot <= 1; pilot += 2)
            {
                float x = pilot * .78f;
                batch.Box(seatFrame, new Vector3(x,7.93f,24.25f), Quaternion.identity, new Vector3(.62f,.17f,.57f));
                batch.Box(seatBlue, new Vector3(x,8.35f,24.02f), Quaternion.Euler(-8f,0f,0f), new Vector3(.64f,.82f,.18f));
                batch.Box(frame, new Vector3(x,8.75f,25.05f), Quaternion.Euler(-12f,0f,0f), new Vector3(.88f,.48f,.94f));
                batch.Box(screen, new Vector3(x-.22f,8.88f,24.56f), Quaternion.identity, new Vector3(.32f,.21f,.035f));
                batch.Box(screen, new Vector3(x+.22f,8.88f,24.56f), Quaternion.identity, new Vector3(.32f,.21f,.035f));
            }
            batch.Box(frame, new Vector3(0f,10.74f,24.50f), Quaternion.Euler(-15f,0f,0f), new Vector3(3.35f,.32f,1.14f));
            batch.Box(screen, new Vector3(0f,10.55f,24.3f), Quaternion.Euler(-15f,0f,0f), new Vector3(1.55f,.08f,.70f));
        }

        private static void BuildServiceAreas(MeshBatch batch)
        {
            // Forward galley and two lavatories, with recognizable counters and partitions.
            foreach (float x in new[] { -1.77f, 1.77f })
            {
                batch.Box(cabinWall, new Vector3(x,8.75f,19.25f), Quaternion.identity, new Vector3(1.20f,2.30f,1.62f));
                batch.Box(white, new Vector3(x,9.95f,19.25f), Quaternion.identity, new Vector3(1.20f,.16f,1.62f));
                batch.Box(frame, new Vector3(x,8.78f,18.40f), Quaternion.identity, new Vector3(.70f,1.82f,.07f));
                batch.Box(metal, new Vector3(x+.30f,8.82f,18.35f), Quaternion.identity, new Vector3(.05f,.19f,.06f));
            }
            for (int i = 0; i < 8; i++)
            {
                float x = -1.85f + i * .53f;
                batch.Box(cabinWall, new Vector3(x,8.72f,-21.65f), Quaternion.identity, new Vector3(.49f,2.25f,1.68f));
                batch.Box(white, new Vector3(x,9.89f,-21.65f), Quaternion.identity, new Vector3(.48f,.16f,1.66f));
            }
            batch.Box(cabinWall, new Vector3(0f,9.15f,-20.7f), Quaternion.identity, new Vector3(4.45f,2.4f,.16f));
            batch.Box(cabinWall, new Vector3(-2.08f,9.02f,5.45f), Quaternion.identity, new Vector3(.14f,2.6f,.20f));
            batch.Box(cabinWall, new Vector3(2.08f,9.02f,5.45f), Quaternion.identity, new Vector3(.14f,2.6f,.20f));
            // Overhead bins have individual lower doors and latch details along both cabin sides.
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 21; i++)
                {
                    float z = 18.6f - i * 1.83f;
                    batch.Box(white, new Vector3(side*2.17f,9.84f,z), Quaternion.identity, new Vector3(.57f,.075f,1.65f));
                    batch.Box(metal, new Vector3(side*2.0f,9.83f,z), Quaternion.identity, new Vector3(.025f,.05f,.14f));
                }
            }
        }

        private static void SetupShowcase(Scene scene, Transform aircraft)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Wide apron display floor";
            ground.transform.position = new Vector3(0f, -.45f, 0f);
            ground.transform.localScale = new Vector3(500f, .9f, 500f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = runwayMat;
            ground.GetComponent<Collider>().enabled = false;
            SceneManager.MoveGameObjectToScene(ground, scene);

            GameObject keyLight = new GameObject("Key light | soft morning sun");
            Light directional = keyLight.AddComponent<Light>();
            directional.type = LightType.Directional;
            directional.intensity = 1.25f;
            directional.color = new Color(1f,.91f,.78f);
            keyLight.transform.rotation = Quaternion.Euler(45f,-34f,0f);
            SceneManager.MoveGameObjectToScene(keyLight, scene);

            GameObject fill = new GameObject("Cool studio fill");
            Light fillLight = fill.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.intensity = .55f;
            fillLight.color = new Color(.62f,.79f,1f);
            fill.transform.rotation = Quaternion.Euler(25f,145f,0f);
            SceneManager.MoveGameObjectToScene(fill, scene);

            GameObject cameraGo = new GameObject("A350 Showcase Camera");
            Camera camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 40f;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 500f;
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(68f, 42f, 78f);
            cameraGo.transform.LookAt(new Vector3(0f,8f,0f));
            SceneManager.MoveGameObjectToScene(cameraGo, scene);
            var lightprobe = new GameObject("Aircraft reflection probe");
            ReflectionProbe probe = lightprobe.AddComponent<ReflectionProbe>();
            probe.size = new Vector3(95f,42f,95f);
            probe.intensity = .65f;
            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            SceneManager.MoveGameObjectToScene(lightprobe, scene);
        }

        private static void ExportObj(Transform root, string path)
        {
            var filters = root.GetComponentsInChildren<MeshFilter>(true);
            var materials = new Dictionary<string, Material>();
            foreach (var f in filters)
            {
                var renderer = f.GetComponent<MeshRenderer>();
                if (renderer == null) continue;
                foreach (var m in renderer.sharedMaterials) if (m != null && !materials.ContainsKey(ObjSafe(m.name))) materials.Add(ObjSafe(m.name), m);
            }
            var mtl = new StringBuilder();
            foreach (var pair in materials)
            {
                Color c = pair.Value.HasProperty("_BaseColor") ? pair.Value.GetColor("_BaseColor") : pair.Value.color;
                mtl.AppendLine("newmtl " + pair.Key);
                mtl.AppendLine("Kd " + F(c.r) + " " + F(c.g) + " " + F(c.b));
                mtl.AppendLine("Ka 0.04 0.04 0.04");
                mtl.AppendLine("Ks 0.15 0.15 0.15");
                mtl.AppendLine("Ns 48");
                mtl.AppendLine();
            }
            var obj = new StringBuilder();
            obj.AppendLine("# A350-900 NEO concept, 1:1 scale, static geometry export");
            obj.AppendLine("# Concept model based on public A350-900 dimensions; not an official Airbus neo variant.");
            obj.AppendLine("mtllib " + Path.GetFileNameWithoutExtension(path) + ".mtl");
            int offset = 1;
            foreach (var filter in filters)
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh == null) continue;
                MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null) continue;
                Matrix4x4 matrix = root.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                Matrix4x4 normalMatrix = matrix.inverse.transpose;
                Vector3[] verts = mesh.vertices;
                Vector3[] normals = mesh.normals;
                if (normals == null || normals.Length != verts.Length) { mesh.RecalculateNormals(); normals = mesh.normals; }
                obj.AppendLine("o " + ObjSafe(filter.name));
                for (int i = 0; i < verts.Length; i++)
                {
                    Vector3 p = matrix.MultiplyPoint3x4(verts[i]);
                    obj.AppendLine("v " + F(p.x) + " " + F(p.y) + " " + F(p.z));
                }
                for (int i = 0; i < normals.Length; i++)
                {
                    Vector3 n = normalMatrix.MultiplyVector(normals[i]).normalized;
                    obj.AppendLine("vn " + F(n.x) + " " + F(n.y) + " " + F(n.z));
                }
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    Material mat = renderer.sharedMaterials.Length > sub ? renderer.sharedMaterials[sub] : renderer.sharedMaterial;
                    if (mat != null) obj.AppendLine("usemtl " + ObjSafe(mat.name));
                    int[] tris = mesh.GetTriangles(sub);
                    for (int i = 0; i < tris.Length; i += 3)
                    {
                        int a = offset + tris[i], b = offset + tris[i + 1], c = offset + tris[i + 2];
                        obj.AppendLine("f " + a + "//" + a + " " + b + "//" + b + " " + c + "//" + c);
                    }
                }
                offset += verts.Length;
            }
            File.WriteAllText(path, obj.ToString(), new UTF8Encoding(false));
            File.WriteAllText(Path.ChangeExtension(path, ".mtl"), mtl.ToString(), new UTF8Encoding(false));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        private static string F(float value) => value.ToString("0.######", CultureInfo.InvariantCulture);
        private static string ObjSafe(string name) => string.IsNullOrEmpty(name) ? "Material" : name.Replace(" ", "_").Replace("|", "_").Replace("/", "_");

        private static float RadiusAt(float z)
        {
            if (z >= 24f) return Mathf.Lerp(2.98f, 2.34f, Mathf.InverseLerp(24f, 26.2f, z));
            if (z >= -18f) return 2.98f;
            if (z >= -24f) return Mathf.Lerp(2.98f, 2.83f, Mathf.InverseLerp(-18f, -24f, z));
            return Mathf.Lerp(2.83f, 1.48f, Mathf.InverseLerp(-24f, -31.5f, z));
        }

        private static Mesh Lathe(float[] z, float[] r, int radial, string name)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i < z.Length; i++)
                for (int j = 0; j <= radial; j++)
                {
                    float a = j * Mathf.PI * 2f / radial;
                    v.Add(new Vector3(r[i] * Mathf.Cos(a), r[i] * Mathf.Sin(a), z[i]));
                }
            for (int i = 0; i < z.Length - 1; i++)
                for (int j = 0; j < radial; j++)
                {
                    int p = i * (radial + 1) + j;
                    t.Add(p); t.Add(p + radial + 1); t.Add(p + radial + 2);
                    t.Add(p); t.Add(p + radial + 2); t.Add(p + 1);
                }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(v); mesh.SetTriangles(t,0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh Annulus(float inner, float outer, float z, int steps, string name)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i <= steps; i++)
            {
                float a = i * Mathf.PI * 2f / steps;
                v.Add(new Vector3(Mathf.Cos(a)*inner, Mathf.Sin(a)*inner,z));
                v.Add(new Vector3(Mathf.Cos(a)*outer, Mathf.Sin(a)*outer,z));
            }
            for (int i = 0; i < steps; i++)
            {
                int p=i*2; t.Add(p);t.Add(p+1);t.Add(p+3);t.Add(p);t.Add(p+3);t.Add(p+2);
            }
            var m = new Mesh { name = name }; m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;
        }

        private static Mesh ExtrudedOutline(Vector3[] points, string name)
        {
            int n = points.Length;
            Vector3 center = Vector3.zero;
            for (int i = 0; i < n; i++) center += points[i];
            center /= n;
            Vector3 axis = Vector3.zero;
            // Determine the plane normal from the first non-collinear edges.
            for (int i = 1; i < n - 1 && axis.sqrMagnitude < 1e-5f; i++) axis = Vector3.Cross(points[i] - points[0], points[i + 1] - points[0]).normalized;
            float thickness = .13f;
            var v = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i < n; i++) v.Add(points[i] + axis * thickness * .5f);
            for (int i = 0; i < n; i++) v.Add(points[i] - axis * thickness * .5f);
            for (int i = 1; i < n - 1; i++)
            {
                t.Add(0);t.Add(i);t.Add(i+1); t.Add(n);t.Add(n+i+1);t.Add(n+i);
            }
            for (int i = 0; i < n; i++)
            {
                int next=(i+1)%n; t.Add(i);t.Add(next);t.Add(n+next);t.Add(i);t.Add(n+next);t.Add(n+i);
            }
            var mesh = new Mesh { name = name }; mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }

        private static Mesh PolygonMesh(Vector3[] points, string name)
        {
            var t = new List<int>();
            for (int i = 1; i < points.Length - 1; i++) { t.Add(0);t.Add(i);t.Add(i+1); }
            var mesh = new Mesh { name = name }; mesh.SetVertices(points);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }

        private static Mesh SaveMesh(Mesh mesh)
        {
            mesh.name = mesh.name.Replace("/", "-");
            string path = GeneratedPath + "/Mesh_" + meshIndex.ToString("0000") + "_" + ObjSafe(mesh.name) + ".asset";
            meshIndex++;
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static GameObject Child(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject MeshObject(string name, Transform parent, Mesh mesh, Material material, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var go = Child(name, parent);
            go.transform.localPosition = position; go.transform.localRotation = rotation; go.transform.localScale = scale;
            MeshFilterAndRenderer(go, mesh, material);
            return go;
        }

        private static void MeshFilterAndRenderer(GameObject go, Mesh mesh, Material material)
        {
            var filter = go.GetComponent<MeshFilter>(); if (filter == null) filter = go.AddComponent<MeshFilter>();
            var renderer = go.GetComponent<MeshRenderer>(); if (renderer == null) renderer = go.AddComponent<MeshRenderer>();
            filter.sharedMesh = mesh; renderer.sharedMaterial = material;
        }

        private sealed class MeshBatch
        {
            private readonly Transform parent;
            private readonly string name;
            private readonly Dictionary<Material, List<CombineInstance>> batches = new Dictionary<Material, List<CombineInstance>>();
            public MeshBatch(Transform parent, string name) { this.parent = parent; this.name = name; }
            public MeshBatch(Scene scene, string name) { this.name = name; var go = new GameObject(name); SceneManager.MoveGameObjectToScene(go, scene); parent = go.transform; }
            public void Box(Material mat, Vector3 pos, Quaternion rot, Vector3 scale) => Add(mat, boxMesh, pos, rot, scale);
            public void Sphere(Material mat, Vector3 pos, Quaternion rot, Vector3 scale) => Add(mat, sphereMesh, pos, rot, scale);
            private void Add(Material mat, Mesh mesh, Vector3 pos, Quaternion rot, Vector3 scale)
            {
                if (!batches.TryGetValue(mat, out var list)) { list = new List<CombineInstance>(); batches.Add(mat,list); }
                list.Add(new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(pos,rot,scale) });
            }
            public void Flush()
            {
                foreach (var pair in batches)
                {
                    var combined = new Mesh { name = name + " | " + pair.Key.name, indexFormat = IndexFormat.UInt32 };
                    combined.CombineMeshes(pair.Value.ToArray(), true, true);
                    combined.RecalculateBounds();
                    var go = Child(name + " | " + pair.Key.name, parent);
                    MeshFilterAndRenderer(go, SaveMesh(combined), pair.Key);
                }
            }
        }

        private struct WingStation
        {
            public float x, leadingZ, chord, thickness;
            public WingStation(float x, float leadingZ, float chord, float thickness) { this.x=x;this.leadingZ=leadingZ;this.chord=chord;this.thickness=thickness; }
        }
    }
}
#endif
