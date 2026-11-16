// ------------------------------------------------------------
// Fichier : Door.cs
// Autrice : SS — créé le 11/11/2026
// Rôle    : porte façon Wolfenstein. Le joueur appuie sur E, la porte
//           glisse sur le côté dans le mur, reste ouverte quelques
//           secondes, puis se referme si personne n'est dans le passage.
//           Une porte verrouillée demande la clé de la bonne couleur.
// ------------------------------------------------------------
using System.Collections;
using ChaosTower.Player;
using UnityEngine;

namespace ChaosTower.Level
{
    [RequireComponent(typeof(Collider))]
    public class Door : MonoBehaviour, IInteractable
    {
        [Header("Verrou")]
        [Tooltip("Cochée : il faut la clé pour ouvrir (une seule fois, la porte reste ensuite déverrouillée).")]
        public bool locked;
        public KeyColor requiredKey = KeyColor.Silver;

        [Header("Mouvement")]
        [Tooltip("Durée de l'ouverture et de la fermeture, en secondes.")]
        [Range(0.1f, 2f)] public float slideDuration = 0.6f;
        [Tooltip("Temps pendant lequel la porte reste ouverte avant d'essayer de se refermer.")]
        [Range(1f, 15f)] public float stayOpenTime = 5f;

        [Header("Sons (facultatif)")]
        public AudioClip openSound;
        public AudioClip lockedSound;

        private enum DoorState { Closed, Opening, Open, Closing }
        private DoorState _state = DoorState.Closed;
        private Vector3 _closedPos, _openPos;
        private Collider _collider;
        private AudioSource _audio;

        public bool IsOpen => _state != DoorState.Closed;

        public string Prompt
        {
            get
            {
                if (_state != DoorState.Closed) return "";
                if (locked) return requiredKey == KeyColor.Gold ? "Clé or requise" : "Clé argent requise";
                return "[E] Ouvrir";
            }
        }

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _audio = GetComponent<AudioSource>();
            _closedPos = transform.position;

            // La porte glisse le long de son côté le plus large (vers l'intérieur du mur)
            Vector3 size = _collider.bounds.size;
            Vector3 axis = size.x >= size.z ? Vector3.right : Vector3.forward;
            float distance = Mathf.Max(size.x, size.z) * 0.95f;
            _openPos = _closedPos + axis * distance;
        }

        public void Interact(GameObject player)
        {
            if (_state != DoorState.Closed) return;

            if (locked)
            {
                var inventory = player.GetComponentInParent<PlayerInventory>();
                if (inventory == null || !inventory.HasKey(requiredKey))
                {
                    Play(lockedSound);
                    return;
                }
                locked = false;
            }
            StartCoroutine(OpenAndClose());
        }

        private IEnumerator OpenAndClose()
        {
            _state = DoorState.Opening;
            Play(openSound);
            yield return Slide(_closedPos, _openPos);
            _state = DoorState.Open;

            yield return new WaitForSeconds(stayOpenTime);
            // ne se referme pas sur le joueur ou un ennemi
            while (SomethingInDoorway()) yield return new WaitForSeconds(0.5f);

            _state = DoorState.Closing;
            Play(openSound);
            yield return Slide(_openPos, _closedPos);
            _state = DoorState.Closed;
        }

        private IEnumerator Slide(Vector3 from, Vector3 to)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / slideDuration;
                transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            transform.position = to;
        }

        private bool SomethingInDoorway()
        {
            Vector3 half = _collider.bounds.extents;
            half.x = Mathf.Max(half.x, 0.9f);   // le passage fait toute la case, pas seulement l'épaisseur de la porte
            half.z = Mathf.Max(half.z, 0.9f);
            foreach (var hit in Physics.OverlapBox(_closedPos, half, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit == _collider) continue;
                if (hit is CharacterController || hit.attachedRigidbody != null || hit.GetComponentInParent<UnityEngine.AI.NavMeshAgent>() != null)
                    return true;
            }
            return false;
        }

        private void Play(AudioClip clip)
        {
            if (clip == null) return;
            if (_audio != null) _audio.PlayOneShot(clip);
            else AudioSource.PlayClipAtPoint(clip, transform.position);
        }
    }
}
