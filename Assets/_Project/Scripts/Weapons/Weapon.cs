// ------------------------------------------------------------
// Fichier : Weapon.cs
// Autrice : SD — créé le 03/11/2026
// Rôle : classe de base des armes : cadence, munitions, son,
//        bruit qui alerte les ennemis. Seule Fire() change.
// Modifs :
// ------------------------------------------------------------
using System;
using ChaosTower.Player;
using UnityEngine;

namespace ChaosTower.Weapons
{
    public abstract class Weapon : MonoBehaviour
    {
        /// <summary>
        /// Un tir a fait du bruit (position, rayon en mètres). Les ennemis s'y abonnent.
        /// À déplacer dans GameEvents (OnNoise) quand il existera.
        /// </summary>
        public static event Action<Vector3, float> NoiseMade;

        [Header("Réglages")]
        [SerializeField] protected WeaponData data;

        [Header("Références")]
        [Tooltip("Caméra du joueur : point de départ et direction du tir")]
        [SerializeField] protected Transform aimOrigin;
        [Tooltip("Layers touchables : Default, Enemy, Wall (pas le joueur)")]
        [SerializeField] protected LayerMask hitMask = ~0;
        [Tooltip("Source audio du joueur (son 2D)")]
        [SerializeField] private AudioSource audioSource;
        [Tooltip("Lumière du flash du canon (facultatif)")]
        [SerializeField] private Light muzzleFlash;

        [Header("Debug")]
        [SerializeField] private bool drawDebugRays = true;

        private float _nextFireTime;
        private float _flashOffTime;
        private PlayerInventory _inventory;

        /// <summary>Déclenché à chaque tir réussi (pour l'animation de l'arme en main).</summary>
        public event Action Fired;

        public WeaponData Data => data;
        public bool IsAutomatic => data != null && data.IsAutomatic;

        protected virtual void Awake()
        {
            _inventory = GetComponentInParent<PlayerInventory>();
            if (muzzleFlash != null) muzzleFlash.enabled = false;
        }

        protected virtual void OnDisable()
        {
            if (muzzleFlash != null) muzzleFlash.enabled = false; // arme rangée pendant un flash
        }

        protected virtual void Update()
        {
            if (muzzleFlash != null && muzzleFlash.enabled && Time.time >= _flashOffTime)
                muzzleFlash.enabled = false;
        }

        /// <summary>Tente de tirer : respecte la cadence et les munitions. Renvoie true si le tir a eu lieu.</summary>
        public bool TryFire()
        {
            if (data == null || aimOrigin == null) return false;
            if (Time.time < _nextFireTime) return false;

            if (data.AmmoPerShot > 0 && (_inventory == null || !_inventory.TryConsumeAmmo(data.AmmoPerShot)))
                return false; // plus de munitions : pas de tir (son "clic vide" à ajouter)

            _nextFireTime = Time.time + data.FireInterval;
            Fire();
            PlayFeedback();
            if (data.NoiseRadius > 0f) NoiseMade?.Invoke(aimOrigin.position, data.NoiseRadius);
            Fired?.Invoke();
            return true;
        }

        /// <summary>Ce que fait l'arme au moment du tir (rayon long, coup court…).</summary>
        protected abstract void Fire();

        /// <summary>Inflige les dégâts à l'objet touché s'il est « endommageable ».</summary>
        protected void ApplyHit(RaycastHit hit)
        {
            IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
            if (target != null)
                target.TakeDamage(data.ComputeDamage(hit.distance), aimOrigin.position);
        }

        protected void DrawDebugRay(Vector3 end, bool hitSomething)
        {
            if (drawDebugRays)
                Debug.DrawLine(aimOrigin.position, end, hitSomething ? Color.red : Color.yellow, 0.5f);
        }

        private void PlayFeedback()
        {
            if (audioSource != null && data.FireSound != null)
            {
                audioSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f); // évite la répétition
                audioSource.PlayOneShot(data.FireSound);
            }

            if (muzzleFlash != null && data.AmmoPerShot > 0)
            {
                muzzleFlash.enabled = true;
                _flashOffTime = Time.time + 0.05f;
            }
        }
    }
}
