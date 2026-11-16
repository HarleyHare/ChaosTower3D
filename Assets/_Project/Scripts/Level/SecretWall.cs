// ------------------------------------------------------------
// Fichier : SecretWall.cs
// Autrice : SS — créé le 11/11/2026, modifié le 14/11/2026
// Rôle    : mur secret façon Wolfenstein. Il ressemble à un mur normal ;
//           quand le joueur appuie sur Interact devant lui, il recule
//           lentement de 3 cases et ouvre le passage. S'il rencontre un
//           autre mur secret, il le pousse devant lui (passage « jaune,
//           sol, jaune » caché des deux côtés). Il ne sert qu'une fois.
// Posé automatiquement par le LevelBuilder sur les cases jaunes.
// ------------------------------------------------------------
using System.Collections;
using UnityEngine;

namespace ChaosTower.Level
{
    [RequireComponent(typeof(Collider))]
    public class SecretWall : MonoBehaviour, IInteractable
    {
        [Tooltip("Nombre de cases parcourues.")]
        [Range(1, 4)] public int cells = 3;
        [Tooltip("Temps pour avancer d'une case, en secondes.")]
        [Range(0.3f, 3f)] public float secondsPerCell = 1f;
        [Tooltip("Son quand le mur se met à glisser (facultatif).")]
        public AudioClip pushSound;

        private bool _used;
        private bool _moving;
        private Collider _collider;

        // aucun texte : le joueur ne doit pas savoir que c'est un passage secret
        public string Prompt => "";

        private void Awake() => _collider = GetComponent<Collider>();

        public void Interact(GameObject player)
        {
            if (_used) return;

            // le mur recule dans la direction où le pousse le joueur, alignée sur la grille
            Vector3 away = transform.position - player.transform.position;
            away.y = 0f;
            Vector3 dir = Mathf.Abs(away.x) >= Mathf.Abs(away.z)
                ? new Vector3(Mathf.Sign(away.x), 0f, 0f)
                : new Vector3(0f, 0f, Mathf.Sign(away.z));

            _used = true;
            if (pushSound != null) AudioSource.PlayClipAtPoint(pushSound, transform.position);
            StartCoroutine(Push(dir));
        }

        private IEnumerator Push(Vector3 dir)
        {
            for (int i = 0; i < cells; i++)
            {
                if (!TryStep(dir)) yield break;              // bloqué : il s'arrête là
                yield return new WaitWhile(() => _moving);
                yield return null;                           // laisse le mur poussé devant finir son pas
                yield return null;
            }
        }

        /// <summary>Avance d'une case. Si un autre mur secret est devant, il est poussé en même temps.</summary>
        private bool TryStep(Vector3 dir)
        {
            float cell = _collider.bounds.size.x;            // une case = largeur du mur
            Vector3 to = transform.position + dir * cell;

            foreach (var hit in Physics.OverlapBox(to, _collider.bounds.extents * 0.9f, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit == _collider) continue;
                var other = hit.GetComponentInParent<SecretWall>();
                if (other != null && other != this && !other._moving)
                {
                    if (!other.TryStep(dir)) return false;   // le mur de devant est bloqué : on ne bouge pas
                    other._used = true;
                    continue;
                }
                if (other == this) continue;
                return false;                                // vrai mur, décor, ennemi ou joueur
            }
            StartCoroutine(Move(transform.position, to));
            return true;
        }

        private IEnumerator Move(Vector3 from, Vector3 to)
        {
            _moving = true;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / secondsPerCell;
                transform.position = Vector3.Lerp(from, to, t);
                yield return null;
            }
            transform.position = to;
            _moving = false;
        }
    }
}
