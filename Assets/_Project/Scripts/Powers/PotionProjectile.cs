// ------------------------------------------------------------
// Fichier : PotionProjectile.cs
// Autrice : SD — créé le 16/11/2026
// Rôle : fiole en vol. Explose au premier contact :
//        OverlapSphere (4 m), dégâts = 60 × (1 − distance / 4),
//        bruit qui alerte les ennemis, effet visuel provisoire
// Modifs :
// ------------------------------------------------------------
using System.Collections.Generic;
using ChaosTower.Weapons;
using UnityEngine;

namespace ChaosTower.Powers
{
    public class PotionProjectile : MonoBehaviour
    {
        [Tooltip("Explose seule après ce délai si elle ne touche rien")]
        [SerializeField] private float maxLifetime = 5f;

        private PowerData _data;
        private GameObject _owner;
        private bool _exploded;

        public void Init(PowerData data, GameObject owner)
        {
            _data = data;
            _owner = owner;
            Invoke(nameof(ExplodeHere), maxLifetime);
        }

        private void OnCollisionEnter(Collision collision)
        {
            Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            Explode(point);
        }

        private void ExplodeHere() => Explode(transform.position);

        private void Explode(Vector3 center)
        {
            if (_exploded || _data == null) return;
            _exploded = true;

            // Chaque cible n'est touchée qu'une fois, même si elle a plusieurs colliders
            var alreadyHit = new HashSet<IDamageable>();
            Collider[] hits = Physics.OverlapSphere(center, _data.Radius, _data.HitMask, QueryTriggerInteraction.Ignore);
            foreach (Collider hit in hits)
            {
                IDamageable target = hit.GetComponentInParent<IDamageable>();
                if (target == null || !alreadyHit.Add(target)) continue;

                bool isOwner = _owner != null && hit.transform.root.gameObject == _owner;
                if (isOwner && !_data.DamagesPlayer) continue;

                float distance = Vector3.Distance(center, hit.ClosestPoint(center));
                float damage = _data.ComputeDamage(distance);
                if (damage > 0f)
                {
                    target.TakeDamage(damage, center);
                    Debug.Log($"[Potion] {hit.name} : -{damage:0.0} PV à {distance:0.0} m");
                }
            }

            if (_data.NoiseRadius > 0f) Weapon.MakeNoise(center, _data.NoiseRadius);
            if (_data.ImpactSound != null) AudioSource.PlayClipAtPoint(_data.ImpactSound, center);

            ExplosionFlash.Spawn(center, _data.Radius, _data.PlaceholderColor);
            Destroy(gameObject);
        }
    }
}
