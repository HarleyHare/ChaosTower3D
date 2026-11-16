// ------------------------------------------------------------
// Fichier : MapPalette.cs
// Autrice : SS — créé le 03/11/2026
// Rôle    : associe chaque couleur de la carte PNG à un type de case
//           et, si besoin, au prefab à poser.
// ------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine;

namespace ChaosTower.Level
{
    /// <summary>Ce que représente une couleur de la carte.</summary>
    public enum CellKind
    {
        Wall, Floor, Door, LockedDoor, SecretWall, Key,
        PlayerStart, Exit, OptionalExit, Enemy, Patrol, Pickup,
        // ajoutés en fin de liste pour ne pas décaler les valeurs déjà enregistrées
        BlockingDecor, Decor
    }

    [System.Serializable]
    public class PaletteEntry
    {
        //Nom affiché
        public string label;

        //Couleur du pixel dans la carte
        public Color32 color;
        
        //role de la case (mur, sol, porte, etc)
        public CellKind kind;
        
        //Prefab à poser
        public GameObject prefab;
        
        //Matériau
        public Material[] materials;
        
        //Sprite découpé (décor, objet) : s'il n'y a pas de prefab, le LevelBuilder crée un sprite qui fait face au joueur.
        public Sprite sprite;
        
        //Hauteur du sprite au-dessus du sol, en mètres (ex. 2,3 pour un lustre accroché au plafond)
        public float spriteHeight = 0f;
    }

    [CreateAssetMenu(menuName = "ChaosTower/Map Palette", fileName = "MapPaletteWolfenstein")]
    public class MapPaletteWolfenstein : ScriptableObject
    {
        public List<PaletteEntry> entries = new List<PaletteEntry>();

        // Légende
        private static readonly (string label, string hex, CellKind kind)[] Legend =
        {
            ("Mur", "171717", CellKind.Wall),
            ("Mur pierre bleue", "172554", CellKind.Wall),
            ("Mur bois", "422006", CellKind.Wall),
            ("Mur métal", "334155", CellKind.Wall),
            ("Mur étoilée", "89A9D6", CellKind.Wall),
            ("Mur griffon", "0B1626", CellKind.Wall),
            ("Mur pierre moussue", "365314", CellKind.Wall),
            ("Mur brique rouge", "7F1D1D", CellKind.Wall),
            ("Mur final bibliothèque", "2E0557", CellKind.Wall),
            ("Mur final violet", "1B0333", CellKind.Wall),
            ("Porte final ", "A6502B", CellKind.Exit),
            ("Pierre + bannière rouge", "4C0519", CellKind.Wall),
            ("Pierre + tableau soldats", "1E293B", CellKind.Wall),
            ("Pierre + panneau Attention", "3F3F46", CellKind.Wall),
            ("Pierre bleue + cellule", "1E40AF", CellKind.Wall),
            ("Pierre bleue + cellule squelette", "3730A3", CellKind.Wall),
            ("Bois + tableau crâne", "713F12", CellKind.Wall),
            ("Bois + portrait", "854D0E", CellKind.Wall),
            ("Métal + panneau Attention", "475569", CellKind.Wall),
            ("Sol", "FFFFFF", CellKind.Floor),
            ("Porte", "A855F7", CellKind.Door),
            ("Porte verrouillée", "CBD5E1", CellKind.LockedDoor),
            ("Clé", "94A3B8", CellKind.Key),
            ("Mur secret", "FFF200", CellKind.SecretWall),
            ("Départ", "5C2A14", CellKind.PlayerStart),
          
            ("Sortie optionnelle", "E87543", CellKind.OptionalExit),
            ("Animal (rapide)", "FFB561", CellKind.Enemy),
            ("Ennemi de base", "FF9A2E", CellKind.Enemy),
            ("Boss", "FF7E00", CellKind.Enemy),
            ("Trajet de patrouille", "22D3EE", CellKind.Patrol),
            ("Croix", "FBCFE8", CellKind.Pickup),
            ("Calice", "F9A8D4", CellKind.Pickup),
            ("Coffre", "EC4899", CellKind.Pickup),
            ("Couronne", "BE185D", CellKind.Pickup),
            ("Gamelle", "D9F99D", CellKind.Pickup),
            ("Assiette", "A3E635", CellKind.Pickup),
            ("Trousse de soin", "65A30D", CellKind.Pickup),
            ("Sang", "E6FABE", CellKind.Pickup),
            ("Arme rapide", "93C5FD", CellKind.Pickup),
            ("Chargeur", "1D4ED8", CellKind.Pickup),
            ("Sphère 1-UP", "990030", CellKind.Pickup),
            ("Tonneau vert (bidon)", "FAD1D1", CellKind.BlockingDecor),
            ("Table avec chaises", "F8BFBF", CellKind.BlockingDecor),
            ("Lampe sur pied", "F6ADAD", CellKind.BlockingDecor),
            ("Squelette pendu", "F49B9B", CellKind.BlockingDecor),
            ("Pilier", "F28A8A", CellKind.BlockingDecor),
            ("Plante en pot (arbre)", "F07878", CellKind.BlockingDecor),
            ("Lavabo", "EE6666", CellKind.BlockingDecor),
            ("Plante séchée (vase bleu)", "EC5454", CellKind.BlockingDecor),
            ("Vase bleu", "EA4242", CellKind.BlockingDecor),
            ("Table ronde", "E83030", CellKind.BlockingDecor),
            ("Armure", "E61E1E", CellKind.BlockingDecor),
            ("Cage vide", "D81818", CellKind.BlockingDecor),
            ("Cage avec squelette", "C71616", CellKind.BlockingDecor),
            ("Lit", "B51414", CellKind.BlockingDecor),
            ("Tonneau en bois", "A31212", CellKind.BlockingDecor),
            ("Puits avec eau", "911010", CellKind.BlockingDecor),
            ("Puits vide", "7F0E0E", CellKind.BlockingDecor),
            ("Drapeau", "6D0C0C", CellKind.BlockingDecor),
            ("Poêle", "5B0A0A", CellKind.BlockingDecor),
            ("Râtelier de lances", "490808", CellKind.BlockingDecor),
            ("Flaque d'eau", "CBF6F0", CellKind.Decor),
            ("Lustre", "B4F2EA", CellKind.Decor),
            ("Lampe de plafond", "9DEEE3", CellKind.Decor),
            ("Ustensiles suspendus", "86EADC", CellKind.Decor),
            ("Squelette allongé", "6FE6D6", CellKind.Decor),
            ("Tas de crânes", "58E2CF", CellKind.Decor),
            ("Panier", "41DEC9", CellKind.Decor),
            ("Os et sang", "2BDAC2", CellKind.Decor),
            ("Flaque de sang", "23C6B0", CellKind.Decor),
            ("Os épars 1", "1FAF9C", CellKind.Decor),
            ("Os épars 2", "1B9888", CellKind.Decor),
            ("Os épars 3", "178173", CellKind.Decor),
            ("Os épars 4", "136B5F", CellKind.Decor),
            ("Casseroles suspendues", "0F544A", CellKind.Decor),
            ("Lianes", "0B3D36", CellKind.Decor),
        };

        /// <summary>Vide la palette et la remplit avec toute la légende (efface prefabs et matériaux déjà réglés).</summary>
        [ContextMenu("Remplir avec la légende Wolfenstein")]
        private void FillDefault()
        {
            entries.Clear();
            AddMissing();
        }

        /// <summary>Ajoute seulement les couleurs de la légende qui manquent : les lignes existantes, leurs prefabs et matériaux sont gardés.</summary>
        [ContextMenu("Compléter avec les couleurs manquantes (garde les réglages)")]
        private void AddMissing()
        {
            var known = new HashSet<int>();
            foreach (var e in entries) known.Add((e.color.r << 16) | (e.color.g << 8) | e.color.b);
            foreach (var (label, hex, kind) in Legend)
            {
                ColorUtility.TryParseHtmlString("#" + hex, out Color c);
                Color32 c32 = c;
                if (known.Contains((c32.r << 16) | (c32.g << 8) | c32.b)) continue;
                entries.Add(new PaletteEntry { label = label, color = c32, kind = kind });
            }
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}
