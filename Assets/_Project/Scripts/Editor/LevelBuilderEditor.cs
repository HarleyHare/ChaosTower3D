// ------------------------------------------------------------
// Fichier : LevelBuilderEditor.cs
// Autrice : SS — créé le 03/11/2026
// Rôle    : bouton « Générer le niveau » du LevelBuilder. Lit la carte PNG
//           pixel par pixel et construit l'étage dans la scène (grey-box
//           tant que les prefabs ne sont pas renseignés dans la palette).
// ------------------------------------------------------------
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ChaosTower.Level.EditorTools
{
    [CustomEditor(typeof(LevelBuilder))]
    public class LevelBuilderEditor : Editor
    {
        private const string MaterialFolder = "Assets/_Project/Levels/Generated/Materials";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var builder = (LevelBuilder)target;

            EditorGUILayout.Space();
            if (GUILayout.Button("Générer le niveau", GUILayout.Height(32))) Generate(builder);
            if (GUILayout.Button("Effacer le niveau généré")) Clear(builder);

            if (!string.IsNullOrEmpty(builder.lastReport))
                EditorGUILayout.HelpBox(builder.lastReport, MessageType.Info);
        }

        // ================================================================ génération

        private static void Generate(LevelBuilder b)
        {
            if (b.map == null || b.palette == null)
            {
                EditorUtility.DisplayDialog("LevelBuilder", "Il manque la carte ou la palette dans l'Inspector.", "OK");
                return;
            }

            Color32[] px = ReadRawPixels(b.map, out int w, out int h);

            // couleur → entrée de palette
            var lookup = new Dictionary<int, PaletteEntry>();
            foreach (var e in b.palette.entries) lookup[Key(e.color)] = e;

            var kind = new CellKind[w, h];
            var entry = new PaletteEntry[w, h];
            var unknown = new Dictionary<int, int>();
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color32 c = px[y * w + x];          // y = 0 : ligne du bas de l'image
                if (c.a < 128) { kind[x, y] = CellKind.Wall; continue; }
                if (lookup.TryGetValue(Key(c), out var e)) { kind[x, y] = e.kind; entry[x, y] = e; }
                else
                {
                    kind[x, y] = CellKind.Floor;
                    unknown.TryGetValue(Key(c), out int n);
                    unknown[Key(c)] = n + 1;
                }
            }

            Clear(b);
            var root = new GameObject(LevelBuilder.RootName);
            Undo.RegisterCreatedObjectUndo(root, "Générer le niveau");
            root.transform.SetParent(b.transform, false);

            Transform walls = Child(root, "Murs"), doors = Child(root, "Portes"), items = Child(root, "Objets");
            Transform enemies = Child(root, "Ennemis"), patrols = Child(root, "Patrouilles"), marks = Child(root, "Reperes");
            Transform decor = Child(root, "Decor");

            float s = b.cellSize, H = b.wallHeight;
            Vector3 Pos(int x, int y, float height) => new Vector3((x + 0.5f) * s, height, (y + 0.5f) * s);
            bool IsWall(int x, int y) => x < 0 || y < 0 || x >= w || y >= h || kind[x, y] == CellKind.Wall || kind[x, y] == CellKind.SecretWall;

            var counts = new Dictionary<string, int>();
            void Count(string label) { counts.TryGetValue(label, out int n); counts[label] = n + 1; }

            // ---------- 1. géométrie fixe : sol + murs (servira au NavMesh)
            // sol et plafond uniquement sous/au-dessus des cases praticables (tout sauf les murs)
            var open = new bool[w, h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                open[x, y] = kind[x, y] != CellKind.Wall;
            PaletteEntry floorEntry = null;
            foreach (var pe in b.palette.entries) if (pe.kind == CellKind.Floor) { floorEntry = pe; break; }
            Material floorMat = floorEntry != null && floorEntry.materials != null && floorEntry.materials.Length > 0 && floorEntry.materials[0] != null
                ? floorEntry.materials[0] : Mat(new Color32(185, 185, 190, 255));
            BuildSurface(root.transform, "Sol", open, w, h, s, 0f, false, floorMat, b.map.name);

            // mur par défaut : première entrée « Wall » de la palette (sert aussi pour la ceinture extérieure)
            PaletteEntry defaultWall = null;
            foreach (var pe in b.palette.entries) if (pe.kind == CellKind.Wall) { defaultWall = pe; break; }

            // x et y vont de -1 à w/h : la ligne en plus tout autour ferme les cases de sol posées au bord de l'image
            for (int y = -1; y <= h; y++)
            for (int x = -1; x <= w; x++)
            {
                bool outside = x < 0 || y < 0 || x >= w || y >= h;
                if (!outside && kind[x, y] != CellKind.Wall) continue;
                // on ne pose que les murs qui bordent une case praticable
                bool border = false;
                for (int dy = -1; dy <= 1 && !border; dy++)
                for (int dx = -1; dx <= 1 && !border; dx++)
                    if (!IsWall(x + dx, y + dy)) border = true;
                if (!border) continue;
                var we = outside ? defaultWall : entry[x, y];
                var go = Place(we, walls, "Mur", Pos(x, y, H / 2f), new Vector3(s, H, s), WallColor(we, defaultWall), PrimitiveType.Cube, 0f, true, x * 73856093 ^ y * 19349663);
                if (we != null) go.name = we.label;
                Count(outside ? "Murs (ceinture extérieure)" : "Murs");
            }
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (kind[x, y] == CellKind.SecretWall)
                {
                    // pas « static » : le mur doit pouvoir bouger en jeu
                    var go = Place(entry[x, y], walls, "MurSecret", Pos(x, y, H / 2f), new Vector3(s, H, s), new Color32(255, 242, 0, 255), PrimitiveType.Cube, 0f, false, x * 73856093 ^ y * 19349663);
                    go.name = $"MurSecret_{x}_{y}";
                    if (go.GetComponentInChildren<SecretWall>() == null) go.AddComponent<SecretWall>();
                    // le NavMesh passe sous le mur ; l'obstacle bloque les ennemis tant qu'il n'a pas glissé
                    var modifierType = System.Type.GetType("Unity.AI.Navigation.NavMeshModifier, Unity.AI.Navigation");
                    if (modifierType != null)
                        modifierType.GetProperty("ignoreFromBuild")?.SetValue(go.AddComponent(modifierType), true);
                    var obstacle = go.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                    obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
                    obstacle.center = Vector3.zero;
                    obstacle.size = Vector3.one;              // taille locale : suit l'échelle du mur
                    obstacle.carving = true;
                    obstacle.carveOnlyStationary = false;
                    Count("Murs secrets");
                }

            // décors bloquants : posés avant le NavMesh pour que les ennemis les contournent
            if (b.showDecor)
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (kind[x, y] == CellKind.BlockingDecor)
                    {
                        var e = entry[x, y];
                        var go = Place(e, decor, e.label, Pos(x, y, 0.75f), new Vector3(s * 0.45f, 0.75f, s * 0.45f), e.color, PrimitiveType.Cylinder, 0f, true);
                        go.name = $"{e.label}_{x}_{y}";
                        Count(e.label);
                    }

            string navReport = TryBuildNavMesh(root);

            // ---------- 2. éléments posés après le NavMesh (ne le bloquent pas)
            // plafond seulement au-dessus des cases praticables (et des murs secrets, qui glissent)
            if (b.buildCeiling)
                BuildSurface(root.transform, "Plafond", open, w, h, s, H, true,
                    b.ceilingMaterial != null ? b.ceilingMaterial : Mat(new Color32(80, 80, 90, 255)), b.map.name);

            Vector3? start = null;
            var enemyObjects = new Dictionary<Vector2Int, GameObject>();
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var e = entry[x, y];
                string label = e != null ? e.label : "";
                switch (kind[x, y])
                {
                    case CellKind.Door when !b.showDoors:
                    case CellKind.LockedDoor when !b.showDoors:
                    case CellKind.Pickup when !b.showItems:
                    case CellKind.Key when !b.showItems:
                    case CellKind.Enemy when !b.showEnemies:
                    case CellKind.BlockingDecor:          // déjà posé avant le NavMesh
                    case CellKind.Decor when !b.showDecor:
                        break;
                    case CellKind.Decor:
                    {
                        // décor non bloquant : sans collision, le joueur et les ennemis passent à travers
                        var go = Place(e, decor, label, Pos(x, y, 0.05f), new Vector3(s * 0.3f, 0.1f, s * 0.3f), e.color, PrimitiveType.Cube, 0f, false);
                        go.name = $"{label}_{x}_{y}";
                        foreach (var col in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);
                        Count(label);
                        break;
                    }
                    case CellKind.Patrol when !b.showPatrols:
                    case CellKind.Exit when !b.showMarkers:
                    case CellKind.OptionalExit when !b.showMarkers:
                        break;
                    case CellKind.PlayerStart when !b.showMarkers:
                        start = Pos(x, y, 0f);   // la caméra de test reste placée au départ
                        break;
                    case CellKind.Door:
                    case CellKind.LockedDoor:
                    {
                        // porte perpendiculaire au passage : murs à gauche/droite → la porte barre le passage nord-sud
                        bool wallsEastWest = IsWall(x - 1, y) && IsWall(x + 1, y);
                        Vector3 size = wallsEastWest ? new Vector3(s, H, 0.25f) : new Vector3(0.25f, H, s);
                        float rot = wallsEastWest ? 0f : 90f;
                        Color32 col = kind[x, y] == CellKind.Door ? new Color32(168, 85, 247, 255) : new Color32(203, 213, 225, 255);
                        var go = Place(e, doors, label, Pos(x, y, H / 2f), size, col, PrimitiveType.Cube, rot, false);
                        go.name = $"{label}_{x}_{y}";
                        // script d'ouverture (ajouté s'il n'est pas déjà dans le prefab)
                        var door = go.GetComponentInChildren<Door>();
                        if (door == null) door = go.AddComponent<Door>();
                        door.locked = kind[x, y] == CellKind.LockedDoor;

                        // cadre en métal : une plaque sur la seule face de chaque mur voisin qui regarde la porte
                        if (b.doorFrameMaterial != null)
                        {
                            Vector3 side = wallsEastWest ? Vector3.right : Vector3.forward;
                            foreach (float k in new[] { -1f, 1f })
                            {
                                var frame = GameObject.CreatePrimitive(PrimitiveType.Quad);
                                UnityEngine.Object.DestroyImmediate(frame.GetComponent<Collider>());   // le mur derrière a déjà son collider
                                frame.name = $"Cadre_{x}_{y}";
                                frame.transform.SetParent(doors, false);              // pas sous la porte : le cadre ne glisse pas avec elle
                                // posée contre le mur, 1 cm devant pour ne pas scintiller
                                frame.transform.position = Pos(x, y, H / 2f) + side * k * (s / 2f - 0.01f);
                                // la face visible d'un Quad regarde vers -Z : on la tourne vers le centre de la porte
                                frame.transform.rotation = Quaternion.LookRotation(side * k);   // même repère que les murs
                                frame.transform.localScale = new Vector3(s, H, 1f);
                                frame.GetComponent<MeshRenderer>().sharedMaterial = b.doorFrameMaterial;
                            }
                        }
                        Count(label);
                        break;
                    }
                    case CellKind.Pickup:
                    case CellKind.Key:
                    {
                        var go = Place(e, items, label, Pos(x, y, 0.5f), Vector3.one * 0.6f, e.color,
                            kind[x, y] == CellKind.Key ? PrimitiveType.Cube : PrimitiveType.Sphere, 0f, false);
                        go.name = $"{label}_{x}_{y}";
                        SetTrigger(go);
                        if (kind[x, y] == CellKind.Key && go.GetComponentInChildren<KeyPickup>() == null)
                            go.AddComponent<KeyPickup>();   // couleur argent par défaut, modifiable dans le prefab
                        Count(label);
                        break;
                    }
                    case CellKind.Enemy:
                    {
                        var go = Place(e, enemies, label, Pos(x, y, 0.9f), new Vector3(0.8f, 0.9f, 0.8f), e.color, PrimitiveType.Capsule, 0f, false);
                        go.name = $"{label}_{x}_{y}";
                        enemyObjects[new Vector2Int(x, y)] = go;
                        Count(label);
                        break;
                    }
                    case CellKind.PlayerStart:
                        start = Pos(x, y, 0f);
                        Place(e, marks, "Depart", Pos(x, y, 0.05f), new Vector3(s * 0.8f, 0.1f, s * 0.8f), e.color, PrimitiveType.Cylinder, 0f, false);
                        Count(label);
                        break;
                    case CellKind.Exit:
                    case CellKind.OptionalExit:
                    {
                        var go = Place(e, marks, label, Pos(x, y, 1.2f), new Vector3(s * 0.8f, 2.4f, s * 0.8f), e.color, PrimitiveType.Cube, 0f, false);
                        SetTrigger(go);
                        Count(label);
                        break;
                    }
                    case CellKind.Patrol:
                        Box(patrols, "Trace", Pos(x, y, 0.02f), new Vector3(s * 0.5f, 0.02f, s * 0.5f), new Color32(34, 211, 238, 255), false, true);
                        break;
                }
            }

            // ---------- 3. patrouilles : suivre les cases cyan depuis chaque ennemi
            var warnings = new List<string>();
            var used = new HashSet<Vector2Int>();
            Vector2Int[] dirs = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
            bool IsPatrol(Vector2Int p) => p.x >= 0 && p.y >= 0 && p.x < w && p.y < h && kind[p.x, p.y] == CellKind.Patrol;
            int patrolCount = 0;

            foreach (var kv in enemyObjects)
            {
                if (!b.showPatrols) break;
                var neighbours = new List<Vector2Int>();
                foreach (var d in dirs) if (IsPatrol(kv.Key + d) && !used.Contains(kv.Key + d)) neighbours.Add(kv.Key + d);
                if (neighbours.Count == 0) continue;     // ennemi en garde
                if (neighbours.Count > 1) warnings.Add($"{kv.Value.name} touche plusieurs trajets : il doit être au bout d'un seul.");

                var cells = new List<Vector2Int> { kv.Key };
                var cur = neighbours[0];
                while (true)
                {
                    cells.Add(cur); used.Add(cur);
                    var next = new List<Vector2Int>();
                    foreach (var d in dirs)
                        if (IsPatrol(cur + d) && !used.Contains(cur + d) && !cells.Contains(cur + d)) next.Add(cur + d);
                    if (next.Count == 0) break;
                    if (next.Count > 1) warnings.Add($"Bifurcation sur le trajet de {kv.Value.name} (case {cur.x},{cur.y}).");
                    cur = next[0];
                }

                // points de passage : départ, chaque virage, bout du trajet
                var points = new List<Vector2Int> { cells[0] };
                for (int i = 1; i < cells.Count - 1; i++)
                    if (cells[i] - cells[i - 1] != cells[i + 1] - cells[i]) points.Add(cells[i]);
                points.Add(cells[cells.Count - 1]);

                var pathGo = new GameObject($"Patrouille_{kv.Value.name}");
                pathGo.transform.SetParent(patrols, false);
                var path = pathGo.AddComponent<PatrolPath>();
                path.owner = kv.Value.transform;
                for (int i = 0; i < points.Count; i++)
                {
                    var wp = new GameObject($"Point_{i}");
                    wp.transform.SetParent(pathGo.transform, false);
                    wp.transform.position = Pos(points[i].x, points[i].y, 0f);
                    path.waypoints.Add(wp.transform);
                }
                AssignPatrolToEnemy(kv.Value, path);
                patrolCount++;
            }
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (b.showPatrols && b.showEnemies && kind[x, y] == CellKind.Patrol && !used.Contains(new Vector2Int(x, y)))
                    warnings.Add($"Case de patrouille ({x},{y}) reliée à aucun ennemi.");

            // ---------- 4. caméra de test
            if (b.placeCameraAtStart && start.HasValue && Camera.main != null)
            {
                Undo.RecordObject(Camera.main.transform, "Placer la caméra");
                Camera.main.transform.position = start.Value + Vector3.up * 1.6f;
                Vector3 look = Vector3.forward;
                int sx = Mathf.FloorToInt(start.Value.x / s), sy = Mathf.FloorToInt(start.Value.z / s);
                foreach (var d in dirs)
                    if (!IsWall(sx + d.x, sy + d.y)) { look = new Vector3(d.x, 0f, d.y); break; }
                Camera.main.transform.rotation = Quaternion.LookRotation(look);
                Camera.main.fieldOfView = Camera.HorizontalToVerticalFieldOfView(80f, 16f / 9f);
            }

            // ---------- 5. rapport
            var sb = new StringBuilder();
            sb.AppendLine($"Carte {w} × {h} générée ({w * s:0} m × {h * s:0} m).");
            foreach (var kv in counts) sb.AppendLine($"  {kv.Key} : {kv.Value}");
            sb.AppendLine($"  Patrouilles : {patrolCount}");
            sb.AppendLine(navReport);
            foreach (var kv in unknown) sb.AppendLine($"Couleur inconnue #{kv.Key:X6} sur {kv.Value} case(s) : traitée comme du sol.");
            foreach (var wmsg in warnings) sb.AppendLine("Attention : " + wmsg);
            b.lastReport = sb.ToString().TrimEnd();
            Debug.Log("[LevelBuilder] " + b.lastReport);

            EditorSceneManager.MarkSceneDirty(b.gameObject.scene);
        }

        private static void Clear(LevelBuilder b)
        {
            var t = b.transform.Find(LevelBuilder.RootName);
            if (t != null) Undo.DestroyObjectImmediate(t.gameObject);
            b.lastReport = "";
        }

        // ================================================================ outils

        /// <summary>Lit le PNG d'origine sur le disque : couleurs exactes, quels que soient les réglages d'import.</summary>
        private static Color32[] ReadRawPixels(Texture2D tex, out int w, out int h)
        {
            string path = AssetDatabase.GetAssetPath(tex);
            byte[] bytes = File.ReadAllBytes(path);
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            ImageConversion.LoadImage(t, bytes);
            w = t.width; h = t.height;
            Color32[] px = t.GetPixels32();
            Object.DestroyImmediate(t);
            return px;
        }

        private static int Key(Color32 c) => (c.r << 16) | (c.g << 8) | c.b;

        private static Transform Child(GameObject root, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            return go.transform;
        }

        /// <summary>Pose le prefab de la palette s'il existe, sinon une forme de couleur provisoire.</summary>
        /// <summary>Couleur grey-box d'un mur : gris foncé pour le mur de base, couleur de la carte pour les
        /// autres types (éclaircie si elle est très sombre, pour la distinguer du mur de base).</summary>
        private static Color32 WallColor(PaletteEntry e, PaletteEntry defaultWall)
        {
            if (e == null || e == defaultWall) return new Color32(60, 60, 66, 255);
            Color32 c = e.color;
            if (c.r < 60 && c.g < 60 && c.b < 60)
                return new Color32((byte)(c.r + 110), (byte)(c.g + 70), (byte)(c.b + 40), 255);
            return c;
        }

        private static GameObject Place(PaletteEntry e, Transform parent, string name, Vector3 pos, Vector3 size,
                                        Color32 color, PrimitiveType shape, float yRotation, bool isStatic, int seed = 0)
        {
            GameObject go;
            if (e != null && e.prefab != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(e.prefab, parent);
                go.transform.position = new Vector3(pos.x, 0f, pos.z);
                go.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
            }
            else if (e != null && e.sprite != null && e.kind != CellKind.Wall && e.kind != CellKind.SecretWall
                     && e.kind != CellKind.Door && e.kind != CellKind.LockedDoor)
            {
                go = SpriteObject(e, parent, name, pos);
            }
            else
            {
                go = GameObject.CreatePrimitive(shape);
                // le cube de Unity affiche une face à l'envers : on le remplace par un cube dont les 6 faces sont à l'endroit
                if (shape == PrimitiveType.Cube) go.GetComponent<MeshFilter>().sharedMesh = UprightCube();
                go.name = name;
                go.transform.SetParent(parent, false);
                go.transform.position = pos;
                go.transform.localScale = size;
                Material m = null;
                if (e != null && e.materials != null && e.materials.Length > 0)
                    m = e.materials[(seed & int.MaxValue) % e.materials.Length];   // variante fixe pour cette case
                go.GetComponent<Renderer>().sharedMaterial = m != null ? m : Mat(color);
            }
            if (isStatic) GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            return go;
        }

        /// <summary>Cube 1×1×1 dont chaque face montre la texture à l'endroit (ni retournée ni en miroir) vue de l'extérieur.</summary>
        private static Mesh UprightCube()
        {
            const string folder = "Assets/_Project/Levels/Generated/Meshes";
            const string path = folder + "/Cube_Endroit.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;

            var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
            Vector3[] normals = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left, Vector3.up, Vector3.down };
            foreach (Vector3 n in normals)
            {
                Vector3 up = Mathf.Abs(n.y) > 0.5f ? Vector3.forward : Vector3.up;
                Vector3 right = Vector3.Cross(up, -n);          // la droite de quelqu'un qui regarde la face
                Vector3 c = n * 0.5f;
                int i = verts.Count;
                verts.Add(c - right * 0.5f - up * 0.5f); uvs.Add(new Vector2(0, 0));   // bas gauche
                verts.Add(c - right * 0.5f + up * 0.5f); uvs.Add(new Vector2(0, 1));   // haut gauche
                verts.Add(c + right * 0.5f + up * 0.5f); uvs.Add(new Vector2(1, 1));   // haut droite
                verts.Add(c + right * 0.5f - up * 0.5f); uvs.Add(new Vector2(1, 0));   // bas droite
                tris.Add(i); tris.Add(i + 1); tris.Add(i + 2); tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
            }
            var mesh = new Mesh { name = "Cube_Endroit" };
            mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.RecalculateTangents();
            EnsureFolder(folder);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        /// <summary>Crée un décor ou objet en sprite : racine au sol (avec la collision), enfant qui affiche le sprite face au joueur.</summary>
        private static GameObject SpriteObject(PaletteEntry e, Transform parent, string name, Vector3 pos)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(pos.x, 0f, pos.z);

            var child = new GameObject("Sprite");
            child.transform.SetParent(root.transform, false);
            child.transform.localPosition = new Vector3(0f, e.spriteHeight, 0f);
            var sr = child.AddComponent<SpriteRenderer>();
            sr.sprite = e.sprite;
            child.AddComponent<Billboard>();

            if (e.kind == CellKind.BlockingDecor)
            {
                var box = root.AddComponent<BoxCollider>();      // le joueur bute dessus
                box.center = new Vector3(0f, 1f, 0f);
                box.size = new Vector3(1f, 2f, 1f);
                // un sprite n'est pas vu par le NavMesh : l'obstacle découpe le NavMesh pour que les ennemis le contournent
                var obstacle = root.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
                obstacle.center = box.center;
                obstacle.size = box.size;
                obstacle.carving = true;
            }
            else if (e.kind == CellKind.Pickup || e.kind == CellKind.Key)
            {
                var box = root.AddComponent<BoxCollider>();      // zone de ramassage
                box.center = new Vector3(0f, 0.5f, 0f);
                box.size = new Vector3(1f, 1f, 1f);
                box.isTrigger = true;
            }
            return root;
        }

        private static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Color32 color, bool isStatic, bool noCollider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = Mat(color);
            if (noCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            if (isStatic) GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            return go;
        }

        /// <summary>Crée un maillage avec un carré par case ouverte (1 répétition de texture par case).
        /// Le maillage est enregistré dans le projet pour être conservé avec la scène.</summary>
        private static GameObject BuildSurface(Transform parent, string name, bool[,] cells, int w, int h, float s,
                                               float height, bool faceDown, Material mat, string mapName)
        {
            var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (!cells[x, y]) continue;
                int i = verts.Count;
                float x0 = x * s, x1 = (x + 1) * s, z0 = y * s, z1 = (y + 1) * s;
                verts.Add(new Vector3(x0, height, z0)); verts.Add(new Vector3(x0, height, z1));
                verts.Add(new Vector3(x1, height, z1)); verts.Add(new Vector3(x1, height, z0));
                uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(0, 1)); uvs.Add(new Vector2(1, 1)); uvs.Add(new Vector2(1, 0));
                if (!faceDown) { tris.Add(i); tris.Add(i + 1); tris.Add(i + 2); tris.Add(i); tris.Add(i + 2); tris.Add(i + 3); }
                else           { tris.Add(i); tris.Add(i + 2); tris.Add(i + 1); tris.Add(i); tris.Add(i + 3); tris.Add(i + 2); }
            }
            var mesh = new Mesh { name = $"{mapName}_{name}", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();

            string folder = "Assets/_Project/Levels/Generated/Meshes";
            EnsureFolder(folder);
            string path = $"{folder}/{mapName}_{name}.asset";
            if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
            return go;
        }

        private static void SetTrigger(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        /// <summary>Un matériau par couleur, enregistré dans le projet pour qu'il survive à la sauvegarde de la scène.</summary>
        private static Material Mat(Color32 color)
        {
            EnsureFolder(MaterialFolder);
            string path = $"{MaterialFolder}/M_{color.r:X2}{color.g:X2}{color.b:X2}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            mat.SetColor("_BaseColor", color);   // URP
            mat.SetColor("_Color", color);       // pipeline intégré
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        /// <summary>Si l'ennemi a un script avec un champ de type PatrolPath (ex. EnemyAI), on le remplit.</summary>
        private static void AssignPatrolToEnemy(GameObject enemy, PatrolPath path)
        {
            foreach (var comp in enemy.GetComponents<MonoBehaviour>())
            {
                if (comp == null) continue;
                foreach (var f in comp.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (f.FieldType != typeof(PatrolPath)) continue;
                    Undo.RecordObject(comp, "Assigner la patrouille");
                    f.SetValue(comp, path);
                    EditorUtility.SetDirty(comp);
                }
            }
        }

        /// <summary>Calcule le NavMesh si le package AI Navigation est installé (sans en dépendre pour compiler).</summary>
        private static string TryBuildNavMesh(GameObject root)
        {
            var type = System.Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
            if (type == null) return "NavMesh : package AI Navigation non installé (ignoré pour l'instant).";
            var surface = root.AddComponent(type);
            var build = type.GetMethod("BuildNavMesh");
            if (build == null) return "NavMesh : méthode BuildNavMesh introuvable.";
            build.Invoke(surface, null);
            return "NavMesh : calculé.";
        }
    }
}
