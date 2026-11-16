// ------------------------------------------------------------
// Fichier : Power.cs
// Autrice : SD — créé le 16/11/2026
// Rôle : classe de base des pouvoirs (potion, téléportation).
//        Le PowerManager gère les charges ; un pouvoir ne fait
//        que son effet dans Activate().
// Modifs :
// ------------------------------------------------------------
using UnityEngine;

namespace ChaosTower.Powers
{
    public abstract class Power : MonoBehaviour
    {
        [Header("Réglages")]
        [SerializeField] protected PowerData data;

        [Header("Références")]
        [Tooltip("Caméra du joueur : point de départ et direction")]
        [SerializeField] protected Transform aimOrigin;
        [Tooltip("Source audio du joueur (son 2D)")]
        [SerializeField] protected AudioSource audioSource;

        public PowerData Data => data;

        protected virtual void Awake()
        {
            if (aimOrigin == null && Camera.main != null) aimOrigin = Camera.main.transform;
            if (audioSource == null) audioSource = GetComponentInParent<AudioSource>();
        }

        /// <summary>
        /// Déclenche l'effet. Renvoie true s'il a eu lieu : le PowerManager retire alors une charge.
        /// Renvoie false s'il est impossible (ex. téléportation sans destination) : la charge est gardée.
        /// </summary>
        public abstract bool Activate();

        protected void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
        }
    }
}
