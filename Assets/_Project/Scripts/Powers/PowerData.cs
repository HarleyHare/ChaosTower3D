// ------------------------------------------------------------
// Fichier : PowerData.cs
// Autrice : SD — créé le 16/11/2026
// Rôle : réglages d'un pouvoir (un fichier .asset par pouvoir),
//        modifiables par les game designers sans code
// Modifs :
// ------------------------------------------------------------
using UnityEngine;

namespace ChaosTower.Powers
{
    [CreateAssetMenu(fileName = "PD_EP2_NewPower", menuName = "ChaosTower/Power Data")]
    public class PowerData : ScriptableObject
    {
        [Header("Identité")]
        [SerializeField] private string displayName = "Potion explosive";
        [Tooltip("Icône du HUD (facultatif)")]
        [SerializeField] private Sprite icon;

        [Header("Jauge (MGDD)")]
        [Range(1, 10)] [SerializeField] private int maxCharges = 5;
        [Tooltip("Secondes pour regagner une charge")]
        [Range(1f, 300f)] [SerializeField] private float rechargeTime = 60f;

        [Header("Effet")]
        [Tooltip("Distance visée en mètres (potion : point d'impact, téléportation : distance max)")]
        [Range(1f, 30f)] [SerializeField] private float range = 10f;
        [Tooltip("Rayon de l'explosion en mètres")]
        [Range(0.5f, 10f)] [SerializeField] private float radius = 4f;
        [Tooltip("Dégâts au centre, nuls au bord du rayon")]
        [Range(0f, 200f)] [SerializeField] private float damage = 60f;
        [Tooltip("Angle de lancer au-dessus de l'horizontale (trajectoire en cloche)")]
        [Range(10f, 80f)] [SerializeField] private float launchAngle = 45f;
        [Tooltip("Objets touchables par l'explosion (layer Enemy quand il existera)")]
        [SerializeField] private LayerMask hitMask = ~0;
        [Tooltip("Le joueur subit-il ses propres explosions ?")]
        [SerializeField] private bool damagesPlayer;
        [Tooltip("Rayon du bruit qui alerte les ennemis, en mètres")]
        [Range(0f, 50f)] [SerializeField] private float noiseRadius = 20f;

        [Header("Retours")]
        [Tooltip("Prefab de la fiole (facultatif : une sphère colorée sinon)")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Color placeholderColor = new Color(0.6f, 0.2f, 0.9f);
        [SerializeField] private AudioClip castSound;
        [SerializeField] private AudioClip impactSound;

        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public int MaxCharges => maxCharges;
        public float RechargeTime => rechargeTime;
        public float Range => range;
        public float Radius => radius;
        public float Damage => damage;
        public float LaunchAngle => launchAngle;
        public LayerMask HitMask => hitMask;
        public bool DamagesPlayer => damagesPlayer;
        public float NoiseRadius => noiseRadius;
        public GameObject ProjectilePrefab => projectilePrefab;
        public Color PlaceholderColor => placeholderColor;
        public AudioClip CastSound => castSound;
        public AudioClip ImpactSound => impactSound;

        /// <summary>Dégâts à une distance du centre : 60 × (1 − distance / 4) avec les valeurs du TDD.</summary>
        public float ComputeDamage(float distance) => damage * Mathf.Clamp01(1f - distance / radius);
    }
}
