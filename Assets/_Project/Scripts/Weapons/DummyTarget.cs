// ------------------------------------------------------------
// Fichier : DummyTarget.cs
// Autrice : SD — créé le 03/11/2026
// Rôle : cible de test pour les armes (remplacée plus tard par
//        les vrais ennemis d'Emma). Rougit quand on la touche,
//        disparaît à 0 PV, peut réapparaître.
// Modifs :
// ------------------------------------------------------------
using UnityEngine;

namespace ChaosTower.Weapons
{
    public class DummyTarget : MonoBehaviour, IDamageable
    {
        [Tooltip("30 = ennemi de base, 20 = rapide, 80 = boss")]
        [Range(1f, 200f)] [SerializeField] private float maxHealth = 30f;
        [Tooltip("Réapparaît après ce délai (0 = jamais)")]
        [Range(0f, 10f)] [SerializeField] private float respawnDelay = 2f;

        private float _health;
        private Renderer _renderer;
        private Collider _collider;
        private MaterialPropertyBlock _block;
        private float _flashUntil;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _collider = GetComponent<Collider>();
            _block = new MaterialPropertyBlock();
            _health = maxHealth;
        }

        public void TakeDamage(float amount, Vector3 from)
        {
            if (_health <= 0f) return;

            _health -= amount;
            Debug.Log($"{name} : -{amount:0.0} PV (reste {Mathf.Max(_health, 0f):0.0}/{maxHealth})");
            _flashUntil = Time.time + 0.1f;
            SetColor(Color.red);

            if (_health <= 0f) Die();
        }

        private void Update()
        {
            if (_flashUntil > 0f && Time.time >= _flashUntil)
            {
                _flashUntil = 0f;
                SetColor(Color.white);
            }
        }

        private void Die()
        {
            Debug.Log($"{name} est mort");
            SetVisible(false);
            if (respawnDelay > 0f) Invoke(nameof(Respawn), respawnDelay);
        }

        private void Respawn()
        {
            _health = maxHealth;
            SetColor(Color.white);
            SetVisible(true);
        }

        private void SetVisible(bool visible)
        {
            if (_renderer != null) _renderer.enabled = visible;
            if (_collider != null) _collider.enabled = visible;
        }

        private void SetColor(Color color)
        {
            if (_renderer == null) return;
            _renderer.GetPropertyBlock(_block);
            _block.SetColor("_BaseColor", color); // propriété de couleur des matériaux URP
            _renderer.SetPropertyBlock(_block);
        }
    }
}
