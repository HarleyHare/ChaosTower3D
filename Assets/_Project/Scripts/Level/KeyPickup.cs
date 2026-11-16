// ------------------------------------------------------------
// Fichier : KeyPickup.cs
// Autrice : SS — créé le 11/11/2026
// Rôle    : clé ramassée quand le joueur passe dessus.
// ------------------------------------------------------------
using ChaosTower.Player;
using UnityEngine;

namespace ChaosTower.Level
{
    public class KeyPickup : MonoBehaviour
    {
        public KeyColor color = KeyColor.Silver;
        public AudioClip pickupSound;

        private void OnTriggerEnter(Collider other)
        {
            var inventory = other.GetComponentInParent<PlayerInventory>();
            if (inventory == null) return;
            inventory.AddKey(color);
            if (pickupSound != null) AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            Destroy(gameObject);
        }
    }
}
