// ------------------------------------------------------------
// Fichier : IDamageable.cs
// Autrice : SD — créé le 03/11/2026
// Rôle : contrat commun à tout ce qui peut recevoir des dégâts
//        (PlayerHealth, EnemyHealth, cibles de test)
// Modifs :
// ------------------------------------------------------------
using UnityEngine;

namespace ChaosTower
{
    public interface IDamageable
    {
        /// <param name="amount">Points de vie retirés.</param>
        /// <param name="from">Position de l'attaquant (pour un recul ou un son).</param>
        void TakeDamage(float amount, Vector3 from);
    }
}
