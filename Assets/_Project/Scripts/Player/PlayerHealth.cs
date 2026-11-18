// ------------------------------------------------------------
// Fichier : PlayerHealth.cs
// Autrice : SD — créé le 16/11/2026
// Rôle : points de vie du joueur (100 max) : dégâts reçus,
//        soins, invincibilité de test, mort
// Modifs : 18/11/2026 SD — son de blessure
// ------------------------------------------------------------
using System;
using ChaosTower.Weapons;
using UnityEngine;

namespace ChaosTower.Player
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        /// <summary>
        /// Le joueur a perdu tous ses PV. Le GameManager (Mila) s'y abonne pour retirer une vie,
        /// recharger l'étage ou passer en Game Over. À déplacer dans GameEvents (OnPlayerDied) quand il existera.
        /// </summary>
        public static event Action PlayerDied;

        [Header("Santé (MGDD)")]
        [Range(1f, 200f)] [SerializeField] private float maxHealth = 100f;
        [Tooltip("En dessous de cette part de PV, la santé est critique (vignette rouge du HUD)")]
        [Range(0f, 1f)] [SerializeField] private float criticalThreshold = 0.2f;

        [Header("Difficulté")]
        [Tooltip("Multiplie les dégâts reçus. Sera lu dans DifficultySettings quand il existera.")]
        [Range(0.1f, 3f)] [SerializeField] private float damageMultiplier = 1f;

        [Header("Mort")]
        [Tooltip("Scripts désactivés à la mort (déplacement, armes) pour figer le joueur")]
        [SerializeField] private Behaviour[] disableOnDeath;

        [Header("Sons")]
        [Tooltip("Joué à chaque blessure (SFX_Player_Hurt)")]
        [SerializeField] private AudioClip hurtSound;
        [Tooltip("Source audio du joueur (son 2D). Trouvée automatiquement si vide.")]
        [SerializeField] private AudioSource audioSource;

        [Header("Test")]
        [Tooltip("Le joueur ne perd plus de PV (menu de débogage)")]
        [SerializeField] private bool invincible;

        private float _health;
        private bool _isDead;

        /// <summary>PV actuels et PV max. Le HUD de Mila s'y abonne.</summary>
        public event Action<float, float> HealthChanged;
        /// <summary>Dégâts réellement subis et position de l'attaquant (flash rouge, son de blessure).</summary>
        public event Action<float, Vector3> Damaged;

        public float Health => _health;
        public float MaxHealth => maxHealth;
        public bool IsDead => _isDead;
        public bool IsCritical => !_isDead && _health <= maxHealth * criticalThreshold;
        public bool Invincible { get => invincible; set => invincible = value; }

        private void Awake()
        {
            _health = maxHealth;
            if (audioSource == null) audioSource = GetComponent<AudioSource>();

            // Par défaut, on fige le déplacement et les armes à la mort
            if (disableOnDeath == null || disableOnDeath.Length == 0)
                disableOnDeath = new Behaviour[] { GetComponent<PlayerController>(), GetComponent<WeaponManager>() };
        }

        private void Start()
        {
            HealthChanged?.Invoke(_health, maxHealth); // valeur initiale pour le HUD
        }

        /// <summary>Reçoit des dégâts (tir ennemi, morsure, explosion).</summary>
        public void TakeDamage(float amount, Vector3 from)
        {
            if (_isDead || invincible || amount <= 0f) return;

            float damage = amount * damageMultiplier;
            _health = Mathf.Max(_health - damage, 0f);
            if (audioSource != null && hurtSound != null) audioSource.PlayOneShot(hurtSound);
            Damaged?.Invoke(damage, from);
            HealthChanged?.Invoke(_health, maxHealth);

            if (_health <= 0f) Die();
        }

        /// <summary>
        /// Soigne le joueur (pickups de Serena). Renvoie false s'il a déjà tous ses PV :
        /// le pickup reste alors au sol (TDD).
        /// </summary>
        public bool Heal(float amount)
        {
            if (_isDead || amount <= 0f || _health >= maxHealth) return false;

            _health = Mathf.Min(_health + amount, maxHealth);
            HealthChanged?.Invoke(_health, maxHealth);
            return true;
        }

        /// <summary>Remet le joueur en vie avec tous ses PV (appelé au rechargement de l'étage).</summary>
        public void ResetHealth()
        {
            _isDead = false;
            _health = maxHealth;
            SetControlsEnabled(true);
            HealthChanged?.Invoke(_health, maxHealth);
        }

        private void Die()
        {
            _isDead = true;
            SetControlsEnabled(false);
            Debug.Log("[PlayerHealth] Le joueur est mort");
            PlayerDied?.Invoke();
        }

        private void SetControlsEnabled(bool value)
        {
            foreach (Behaviour b in disableOnDeath)
                if (b != null) b.enabled = value;
        }
    }
}
