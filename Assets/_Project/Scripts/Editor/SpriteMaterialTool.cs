// ------------------------------------------------------------
// Fichier : SpriteMaterialTool.cs
// Autrice : SS — créé le 11/11/2026
// Rôle    : outil d'éditeur. Clic droit sur des sprites découpés
//           (ex. une planche de textures de murs) > ChaosTower >
//           « Créer un matériau par sprite ». Chaque matériau affiche
//           uniquement la case de son sprite, sans découper l'image.
// ------------------------------------------------------------
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ChaosTower.EditorTools
{
    public static class SpriteMaterialTool
    {
        private const string Folder = "Assets/_Project/Art/Materials/Walls";

        [MenuItem("Assets/ChaosTower/Créer un matériau par sprite")]
        private static void CreateMaterials()
        {
            EnsureFolder(Folder);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            int count = 0;
            foreach (var obj in Selection.objects)
            {
                if (!(obj is Sprite sprite)) continue;
                Texture2D tex = sprite.texture;
                Rect r = sprite.rect;

                string path = $"{Folder}/M_{Clean(sprite.name)}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                bool isNew = mat == null;
                if (isNew) mat = new Material(shader);

                var scale = new Vector2(r.width / tex.width, r.height / tex.height);
                var offset = new Vector2(r.x / tex.width, r.y / tex.height);
                mat.SetTexture("_BaseMap", tex);
                mat.SetTextureScale("_BaseMap", scale);
                mat.SetTextureOffset("_BaseMap", offset);
                mat.mainTexture = tex;                    // pipeline intégré
                mat.mainTextureScale = scale;
                mat.mainTextureOffset = offset;
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0f);   // pas de reflet brillant

                if (isNew) AssetDatabase.CreateAsset(mat, path);
                else EditorUtility.SetDirty(mat);
                count++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[SpriteMaterialTool] {count} matériau(x) dans {Folder}. Vérifie que la planche est en Filter Point, Generate Mip Maps décoché.");
        }

        [MenuItem("Assets/ChaosTower/Créer un matériau par sprite", true)]
        private static bool CanCreate()
        {
            foreach (var obj in Selection.objects) if (obj is Sprite) return true;
            return false;
        }

        private static string Clean(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            return name.Replace(' ', '_');
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
