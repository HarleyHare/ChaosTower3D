// ------------------------------------------------------------
// Fichier : WeaponData.cs
// Autrice : SD — créé le 03/11/2026
// Rôle : réglages d'une arme (un fichier .asset par arme et par
//        univers), modifiables par les game designers sans code
// Modifs :
// ------------------------------------------------------------
using UnityEngine;

namespace ChaosTower.Weapons
{
    [CreateAssetMenu(fileName = "WD_EP1_NewWeapon", menuName = "ChaosTower/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Header("Identité")]
        [Tooltip("Nom affiché (ex. Pistolet, Baguette marron, Sabre laser)")]
        [SerializeField] private string displayName = "Pistolet";

        [Header("Dégâts")]
        [Tooltip("Dégâts à bout portant, avant variation")]
        [Range(1f, 200f)] [SerializeField] private float baseDamage = 15f;
        [Tooltip("Part des dégâts restant à portée maximale (0,5 = moitié)")]
        [Range(0.1f, 1f)] [SerializeField] private float damageAtMaxRange = 0.5f;
        [Tooltip("Variation aléatoire de chaque tir (0,1 = ±10 %)")]
        [Range(0f, 0.5f)] [SerializeField] private float damageVariance = 0.1f;

        [Header("Tir")]
        [Tooltip("Temps minimum entre deux tirs, en secondes")]
        [Range(0.02f, 2f)] [SerializeField] private float fireInterval = 0.35f;
        [Tooltip("Tire en continu tant que le bouton est maintenu (arme rafale)")]
        [SerializeField] private bool isAutomatic;
        [Tooltip("Portée en mètres (1,5 pour le corps à corps)")]
        [Range(0.5f, 100f)] [SerializeField] private float range = 40f;
        [Tooltip("Munitions consommées par tir (0 pour le corps à corps)")]
        [Range(0, 5)] [SerializeField] private int ammoPerShot = 1;
        [Tooltip("Rayon du bruit qui alerte les ennemis, en mètres")]
        [Range(0f, 50f)] [SerializeField] private float noiseRadius = 20f;

        [Header("Retours")]
        [SerializeField] private AudioClip fireSound;
        [Tooltip("Sprites de l'arme en main : [0] = repos, puis l'animation de tir")]
        [SerializeField] private Sprite[] viewSprites;

        public string DisplayName => displayName;
        public float BaseDamage => baseDamage;
        public float DamageAtMaxRange => damageAtMaxRange;
        public float DamageVariance => damageVariance;
        public float FireInterval => fireInterval;
        public bool IsAutomatic => isAutomatic;
        public float Range => range;
        public int AmmoPerShot => ammoPerShot;
        public float NoiseRadius => noiseRadius;
        public AudioClip FireSound => fireSound;
        public Sprite[] ViewSprites => viewSprites;

        /// <summary>Dégâts d'un tir à une distance donnée : baisse linéaire avec la distance, puis variation aléatoire.</summary>
        public float ComputeDamage(float distance)
        {
            float t = Mathf.Clamp01(distance / range);
            float falloff = Mathf.Lerp(1f, damageAtMaxRange, t);
            float variation = Random.Range(1f - damageVariance, 1f + damageVariance);
            return baseDamage * falloff * variation;
        }
    }
}
