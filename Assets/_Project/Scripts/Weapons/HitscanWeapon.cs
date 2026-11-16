// ------------------------------------------------------------
// Fichier : HitscanWeapon.cs
// Autrice : SD — créé le 03/11/2026
// Rôle : arme à distance à tir instantané (pistolet, mitraillette,
//        baguettes) : un rayon part du centre de la caméra
// Modifs :
// ------------------------------------------------------------
using UnityEngine;

namespace ChaosTower.Weapons
{
    public class HitscanWeapon : Weapon
    {
        protected override void Fire()
        {
            Ray ray = new Ray(aimOrigin.position, aimOrigin.forward);

            // Le premier objet touché arrête le tir : un mur protège l'ennemi derrière.
            if (Physics.Raycast(ray, out RaycastHit hit, data.Range, hitMask, QueryTriggerInteraction.Ignore))
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
