// ------------------------------------------------------------
// Fichier : LevelBuilder.cs
// Autrice : SS — créé le 03/11/2026
// Rôle    : réglages de la génération d'un étage à partir d'une carte PNG
//           (1 pixel = 1 case). Le bouton « Générer le niveau » est dans
//           l'Inspector (voir Editor/LevelBuilderEditor.cs).
// ------------------------------------------------------------
using UnityEngine;

namespace ChaosTower.Level
{
    public class LevelBuilder : MonoBehaviour
    {
        public const string RootName = "Generated";

        [Header("Carte")]
        public Texture2D map; // carte png
       
        public MapPaletteWolfenstein palette; // palette de couleurs

        [Header("Dimensions")]
        [Range(1f, 4f)] public float cellSize = 2.5f;
        [Range(2f, 6f)] public float wallHeight = 2.5f;
        public bool buildCeiling = true;
        public Material ceilingMaterial;
        public Material doorFrameMaterial;

        [Header("Afficher (décocher pour ne voir que la carte)")]
        public bool showDoors = true;
        public bool showEnemies = true;
        public bool showItems = true;
        public bool showPatrols = true;
        [Tooltip("Départ et sorties.")]
        public bool showMarkers = true;
        [Tooltip("Décors (bloquants et non bloquants).")]
        public bool showDecor = true;

        [Header("Test")]
        public bool placeCameraAtStart = true;

        [HideInInspector] public string lastReport;
    }
}
