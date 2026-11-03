// ------------------------------------------------------------
// Fichier : MeleeWeapon.cs
// Autrice : SD — créé le 03/11/2026
// Rôle : arme de corps à corps (couteau, dent de basilic, sabre
//        laser) : coup court, sans munitions
// Modifs :
// ------------------------------------------------------------
using UnityEngine;

namespace ChaosTower.Weapons
{
    public class MeleeWeapon : Weapon
    {
        [Header("Corps à corps")]
        [Tooltip("Épaisseur du coup : un rayon épais pardonne un peu la visée")]
        [Range(0f, 0.5f)] [SerializeField] private float hitRadius = 0.3f;

        protected override void Fire()
        {
            Ray ray = new Ray(aimOrigin.position, aimOrigin.forward);

            if (Physics.SphereCast(ray, hitRadius, out RaycastHit hit, data.Range, hitMask, QueryTriggerInteraction.Ignore))
            {
                ApplyHit(hit);
                DrawDebugRay(hit.point, true);
            }
            else
            {
                DrawDebugRay(ray.GetPoint(data.Range), false);
            }
        }
    }
}
