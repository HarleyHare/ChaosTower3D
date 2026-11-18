// ------------------------------------------------------------
// Fichier : SandboxSceneBuilder.cs
// Autrice : SD — créé le 03/11/2026
// Rôle : outil d'éditeur. Menu ChaosTower > Créer la scène de test :
//        construit Sandbox_Sonja (sol, murs, joueur, armes, cibles)
//        pour tester le déplacement et le tir sans attendre les maps.
// Modifs : 03/11/2026 SD — sol en bloc épais, joueur posé à 0,1 m
//          16/11/2026 SD — ajoute PlayerHealth et ses touches de test
//          16/11/2026 SD — ajoute la potion (+ menu pour une scène existante)
//          18/11/2026 SD — menu pour brancher les sons de test
// ------------------------------------------------------------
using ChaosTower.Player;
using ChaosTower.Powers;
using ChaosTower.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChaosTower.EditorTools
{
    public static class SandboxSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Sandbox/Sandbox_Sonja.unity";
        private const string ControlsPath = "Assets/_Project/Data/ChaosTowerControls.inputactions";
        private const string WeaponDataFolder = "Assets/_Project/Data/Weapons";
        private const string PowerDataFolder = "Assets/_Project/Data/Powers";
        private const string SfxFolder = "Assets/_Project/Audio/SFX";
        private const string MaterialFolder = "Assets/_Project/Art/Materials";
        private const float Cell = 2f;      // une case = 2 m (TDD)
        private const float WallHeight = 3f;

        // # = mur · . = sol · P = départ du joueur · T = cible de test
        private static readonly string[] Map =
        {
            "############",
            "#..........#",
            "#..T...#...#",
            "#......#.T.#",
            "#..###.....#",
            "#..........#",
            "#.T....##..#",
            "#......#...#",
            "#....P.....#",
            "############"
        };

        [MenuItem("ChaosTower/Créer la scène de test (Sandbox_Sonja)")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                !EditorUtility.DisplayDialog("Sandbox_Sonja existe déjà",
                    "Remplacer la scène de test par une nouvelle ?", "Remplacer", "Annuler"))
                return;

            var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            if (controls == null)
            {
                EditorUtility.DisplayDialog("Contrôles introuvables", $"Fichier attendu : {ControlsPath}", "OK");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateLight();
            Material wallMat = GetOrCreateMaterial("MAT_Sandbox_Wall", new Color(0.35f, 0.42f, 0.55f));
            Material floorMat = GetOrCreateMaterial("MAT_Sandbox_Floor", new Color(0.3f, 0.3f, 0.3f));
            Vector3 spawn = BuildLevel(wallMat, floorMat);
            CreatePlayer(spawn, controls);
            WireTestSounds();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[Sandbox] Scène créée : {ScenePath}. Appuie sur Play pour tester.");
        }

        [MenuItem("ChaosTower/Ajouter le terrain de test à la scène ouverte")]
        public static void BuildInOpenScene()
        {
            var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            if (controls == null)
            {
                EditorUtility.DisplayDialog("Contrôles introuvables", $"Fichier attendu : {ControlsPath}", "OK");
                return;
            }

            if (GameObject.Find("Level") != null || GameObject.Find("Player") != null)
            {
                EditorUtility.DisplayDialog("Déjà présent",
                    "La scène contient déjà « Level » ou « Player ». Supprime-les d'abord pour reconstruire.", "OK");
                return;
            }

            // La caméra du joueur remplace la caméra d'origine : on désactive celle-ci (sans la supprimer)
            var oldCam = GameObject.Find("Main Camera");
            if (oldCam != null) oldCam.SetActive(false);

            if (Object.FindAnyObjectByType<Light>() == null) CreateLight();

            Material wallMat = GetOrCreateMaterial("MAT_Sandbox_Wall", new Color(0.35f, 0.42f, 0.55f));
            Material floorMat = GetOrCreateMaterial("MAT_Sandbox_Floor", new Color(0.3f, 0.3f, 0.3f));
            Vector3 spawn = BuildLevel(wallMat, floorMat);
            CreatePlayer(spawn, controls);
            WireTestSounds();

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[Sandbox] Terrain de test ajouté. Appuie sur Play pour tester.");
        }

        // ---------------------------------------------------------------- Niveau

        private static void CreateLight()
        {
            var go = new GameObject("Directional Light");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static Vector3 BuildLevel(Material wallMat, Material floorMat)
        {
            var root = new GameObject("Level");
            int rows = Map.Length;
            int cols = Map[0].Length;
            Vector3 spawn = Vector3.zero;

            // Sol : un bloc épais de 0,5 m dont le dessus est à y = 0
            // (un Plane sans épaisseur laisse parfois passer le joueur au travers)
            const float floorThickness = 0.5f;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(root.transform);
            floor.transform.position = new Vector3((cols - 1) * Cell / 2f, -floorThickness / 2f, (rows - 1) * Cell / 2f);
            floor.transform.localScale = new Vector3(cols * Cell, floorThickness, rows * Cell);
            floor.GetComponent<Renderer>().sharedMaterial = floorMat;
            floor.isStatic = true;

            var walls = new GameObject("Walls");
            walls.transform.SetParent(root.transform);
            var targets = new GameObject("Targets");
            targets.transform.SetParent(root.transform);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    // La première ligne de la carte est au fond (z le plus grand)
                    Vector3 cellPos = new Vector3(c * Cell, 0f, (rows - 1 - r) * Cell);
                    switch (Map[r][c])
                    {
                        case '#':
                            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            wall.name = $"Wall_{c}_{r}";
                            wall.transform.SetParent(walls.transform);
                            wall.transform.position = cellPos + Vector3.up * WallHeight / 2f;
                            wall.transform.localScale = new Vector3(Cell, WallHeight, Cell);
                            wall.GetComponent<Renderer>().sharedMaterial = wallMat;
                            wall.isStatic = true;
                            break;
                        case 'T':
                            var target = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                            target.name = $"DummyTarget_{c}_{r}";
                            target.transform.SetParent(targets.transform);
                            target.transform.position = cellPos + Vector3.up;
                            target.AddComponent<DummyTarget>();
                            break;
                        case 'P':
                            spawn = cellPos + Vector3.up * 0.1f; // légèrement au-dessus du sol
                            break;
                    }
                }
            }
            return spawn;
        }

        // ---------------------------------------------------------------- Joueur

        private static void CreatePlayer(Vector3 spawn, InputActionAsset controls)
        {
            var player = new GameObject("Player") { tag = "Player" };
            player.transform.position = spawn;

            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            var audio = player.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 0f; // sons du joueur en 2D (TDD)

            // Caméra à hauteur des yeux
            var camGo = new GameObject("PlayerCamera") { tag = "MainCamera" };
            camGo.transform.SetParent(player.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            camGo.AddComponent<AudioListener>();

            // Flash du canon
            var flashGo = new GameObject("MuzzleFlash");
            flashGo.transform.SetParent(camGo.transform, false);
            flashGo.transform.localPosition = new Vector3(0f, -0.2f, 0.6f);
            var flash = flashGo.AddComponent<Light>();
            flash.type = LightType.Point;
            flash.color = new Color(1f, 0.85f, 0.5f);
            flash.range = 8f;
            flash.intensity = 3f;
            flash.enabled = false;

            // Déplacement
            var controller = player.AddComponent<PlayerController>();
            Set(controller, "controls", controls);
            Set(controller, "playerCamera", cam);

            // Inventaire : plein de munitions pour tester
            var inventory = player.AddComponent<PlayerInventory>();
            SetInt(inventory, "startAmmo", PlayerInventory.MaxAmmo);

            // Armes
            int noHitPlayer = ~(1 << 2); // tout sauf le layer « Ignore Raycast »
            var holder = new GameObject("Weapons");
            holder.transform.SetParent(camGo.transform, false);

            Weapon knife = CreateWeapon<MeleeWeapon>(holder, "Knife",
                GetOrCreateWeaponData("WD_EP1_Knife", "Couteau", 25f, 0.5f, false, 1.5f, 0, 0f),
                camGo.transform, audio, null, noHitPlayer);
            Weapon pistol = CreateWeapon<HitscanWeapon>(holder, "Pistol",
                GetOrCreateWeaponData("WD_EP1_Pistol", "Pistolet", 15f, 0.35f, false, 40f, 1, 20f),
                camGo.transform, audio, flash, noHitPlayer);
            Weapon machineGun = CreateWeapon<HitscanWeapon>(holder, "MachineGun",
                GetOrCreateWeaponData("WD_EP1_MachineGun", "Mitraillette", 10f, 0.1f, true, 40f, 1, 20f),
                camGo.transform, audio, flash, noHitPlayer);

            var manager = player.AddComponent<WeaponManager>();
            var so = new SerializedObject(manager);
            so.FindProperty("controls").objectReferenceValue = controls;
            var slots = so.FindProperty("slots");
            slots.arraySize = 3;
            slots.GetArrayElementAtIndex(0).objectReferenceValue = knife;
            slots.GetArrayElementAtIndex(1).objectReferenceValue = pistol;
            slots.GetArrayElementAtIndex(2).objectReferenceValue = machineGun;
            // Dans la scène de test, les 3 armes sont débloquées pour pouvoir tout essayer
            var unlocked = so.FindProperty("startUnlocked");
            unlocked.arraySize = 3;
            for (int i = 0; i < 3; i++) unlocked.GetArrayElementAtIndex(i).boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Santé + touches de test (K, L, R, I)
            player.AddComponent<PlayerHealth>();
            player.AddComponent<PlayerHealthDebug>();

            // Pouvoir : la potion de l'étage 2, pour pouvoir la tester
            AddPotion(player);

            // Affichage de test
            var hud = player.AddComponent<DebugHUD>();
            Set(hud, "inventory", inventory);
            Set(hud, "weapons", manager);
        }

        [MenuItem("ChaosTower/Brancher les sons de test (armes, joueur, cibles)")]
        public static void WireTestSounds()
        {
            AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>($"{SfxFolder}/{name}.wav");
            AudioClip[] Clips(string prefix, int count)
            {
                var list = new AudioClip[count];
                for (int i = 0; i < count; i++) list[i] = Clip($"{prefix}{i + 1}");
                return list;
            }
            void SetClip(Object target, string field, AudioClip clip)
            {
                var so = new SerializedObject(target);
                so.FindProperty(field).objectReferenceValue = clip;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            void SetClips(Object target, string field, AudioClip[] clips)
            {
                var so = new SerializedObject(target);
                var prop = so.FindProperty(field);
                prop.arraySize = clips.Length;
                for (int i = 0; i < clips.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Réglages des armes (fichiers .asset partagés par toutes les scènes)
            var pistol = AssetDatabase.LoadAssetAtPath<WeaponData>($"{WeaponDataFolder}/WD_EP1_Pistol.asset");
            var machineGun = AssetDatabase.LoadAssetAtPath<WeaponData>($"{WeaponDataFolder}/WD_EP1_MachineGun.asset");
            var knife = AssetDatabase.LoadAssetAtPath<WeaponData>($"{WeaponDataFolder}/WD_EP1_Knife.asset");
            if (pistol != null) { SetClip(pistol, "fireSound", Clip("SFX_Pistol_Shot")); SetClip(pistol, "emptySound", Clip("SFX_Weapon_Empty")); EditorUtility.SetDirty(pistol); }
            if (machineGun != null) { SetClip(machineGun, "fireSound", Clip("SFX_MachineGun_Shot")); SetClip(machineGun, "emptySound", Clip("SFX_Weapon_Empty")); EditorUtility.SetDirty(machineGun); }
            if (knife != null) { SetClip(knife, "fireSound", Clip("SFX_Knife_Swing")); SetClips(knife, "impactSounds", Clips("SFX_Knife_Hit_", 3)); EditorUtility.SetDirty(knife); }
            AssetDatabase.SaveAssets();

            // Joueur et cibles de la scène ouverte
            int players = 0, targets = 0;
            foreach (var health in Object.FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
            {
                SetClip(health, "hurtSound", Clip("SFX_Player_Hurt"));
                players++;
            }
            foreach (var dummy in Object.FindObjectsByType<DummyTarget>(FindObjectsSortMode.None))
            {
                SetClips(dummy, "hurtSounds", new[] { Clip("SFX_EP1_Guard_Hurt") });
                SetClips(dummy, "deathSounds", Clips("SFX_EP1_Guard_Death_", 8));
                targets++;
            }
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log($"[Sandbox] Sons branchés : 3 armes, {players} joueur(s), {targets} cible(s). Enregistre la scène (Ctrl + S).");
        }

        [MenuItem("ChaosTower/Ajouter la potion au joueur de la scène ouverte")]
        public static void AddPotionToOpenScene()
        {
            var player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                EditorUtility.DisplayDialog("Joueur introuvable", "Aucun objet avec le tag « Player » dans la scène ouverte.", "OK");
                return;
            }
            if (player.GetComponent<PowerManager>() != null)
            {
                EditorUtility.DisplayDialog("Déjà présent", "Le joueur a déjà un PowerManager.", "OK");
                return;
            }

            AddPotion(player);
            EditorSceneManager.MarkSceneDirty(player.scene);
            Debug.Log("[Sandbox] Potion ajoutée au joueur. Enregistre la scène (Ctrl + S) puis Play : clic droit pour lancer.");
        }

        private static void AddPotion(GameObject player)
        {
            var controls = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            var cam = player.GetComponentInChildren<Camera>();

            var potion = player.AddComponent<PotionPower>();
            var so = new SerializedObject(potion);
            so.FindProperty("data").objectReferenceValue = GetOrCreatePowerData("PD_EP2_Potion");
            so.FindProperty("aimOrigin").objectReferenceValue = cam != null ? cam.transform : null;
            so.FindProperty("audioSource").objectReferenceValue = player.GetComponent<AudioSource>();
            so.ApplyModifiedPropertiesWithoutUndo();

            var manager = player.AddComponent<PowerManager>();
            Set(manager, "controls", controls);
            Set(manager, "activePower", potion);
        }

        private static PowerData GetOrCreatePowerData(string fileName)
        {
            if (!AssetDatabase.IsValidFolder(PowerDataFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Data", "Powers");

            string path = $"{PowerDataFolder}/{fileName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<PowerData>(path);
            if (existing != null) return existing; // on ne touche pas aux réglages déjà faits

            // Les valeurs par défaut de PowerData sont celles du TDD (5 charges, 60 s, 10 m, 4 m, 60 dégâts)
            var data = ScriptableObject.CreateInstance<PowerData>();
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        private static Weapon CreateWeapon<T>(GameObject holder, string name, WeaponData data,
            Transform aim, AudioSource audio, Light flash, int mask) where T : Weapon
        {
            var go = new GameObject(name);
            go.transform.SetParent(holder.transform, false);
            var weapon = go.AddComponent<T>();
            var so = new SerializedObject(weapon);
            so.FindProperty("data").objectReferenceValue = data;
            so.FindProperty("aimOrigin").objectReferenceValue = aim;
            so.FindProperty("audioSource").objectReferenceValue = audio;
            so.FindProperty("muzzleFlash").objectReferenceValue = flash;
            so.FindProperty("hitMask").intValue = mask;
            so.ApplyModifiedPropertiesWithoutUndo();
            return weapon;
        }

        // ---------------------------------------------------------------- Assets

        private static WeaponData GetOrCreateWeaponData(string fileName, string displayName, float damage,
            float interval, bool automatic, float range, int ammoPerShot, float noise)
        {
            string path = $"{WeaponDataFolder}/{fileName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            if (existing != null) return existing; // on ne touche pas aux réglages déjà faits

            var data = ScriptableObject.CreateInstance<WeaponData>();
            var so = new SerializedObject(data);
            so.FindProperty("displayName").stringValue = displayName;
            so.FindProperty("baseDamage").floatValue = damage;
            so.FindProperty("fireInterval").floatValue = interval;
            so.FindProperty("isAutomatic").boolValue = automatic;
            so.FindProperty("range").floatValue = range;
            so.FindProperty("ammoPerShot").intValue = ammoPerShot;
            so.FindProperty("noiseRadius").floatValue = noise;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        private static Material GetOrCreateMaterial(string fileName, Color color)
        {
            string path = $"{MaterialFolder}/{fileName}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Universal Render Pipeline/Lit");
            var mat = new Material(shader);
            mat.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        // ---------------------------------------------------------------- Outils

        private static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
