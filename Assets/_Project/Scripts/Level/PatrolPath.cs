// ------------------------------------------------------------
// Fichier : PatrolPath.cs
// Autrice : SS — créé le 03/11/2026
// Rôle    : trajet de patrouille d'un ennemi (points de passage),
//           créé par le LevelBuilder à partir des cases cyan.
// ------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine;

namespace ChaosTower.Level
{
    public class PatrolPath : MonoBehaviour
    {
        [Tooltip("Ennemi qui suit ce trajet.")]
        public Transform owner;

        [Tooltip("Points de passage dans l'ordre : départ (ennemi), virages, bout du trajet.")]
        public List<Transform> waypoints = new List<Transform>();

        private void OnDrawGizmos()
        {
            if (waypoints == null) return;
            Gizmos.color = new Color(0.13f, 0.83f, 0.93f);
            for (int i = 0; i < waypoints.Count; i++)
            {
                if (waypoints[i] == null) continue;
                Gizmos.DrawSphere(waypoints[i].position, 0.25f);
                if (i > 0 && waypoints[i - 1] != null)
                    Gizmos.DrawLine(waypoints[i - 1].position, waypoints[i].position);
            }
        }
    }
}
