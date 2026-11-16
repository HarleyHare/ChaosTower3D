// ------------------------------------------------------------
// Fichier : PlayerHealthDebug.cs
// Autrice : SD — créé le 16/11/2026
// Rôle : touches de TEST pour la santé du joueur, en attendant
//        les ennemis et les pickups. À retirer du joueur final.
//        K = -10 PV · L = +25 PV · R = ressusciter · I = invincible
// Modifs :
// ------------------------------------------------------------
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChaosTower.Player
{
    [RequireComponent(typeof(PlayerHealth))]
    public class PlayerHealthDebug : MonoBehaviour
    {
        [SerializeField] private float damagePerPress = 10f;
        [SerializeField] private float healPerPress = 25f;

        private PlayerHealth _health;

        private void Awake() => _health = GetComponent<PlayerHealth>();

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            // Position de l'attaquant fictive : devant le joueur
            if (kb.kKey.wasPressedThisFrame) _health.TakeDamage(damagePerPress, transform.position + transform.forward);
            if (kb.lKey.wasPressedThisFrame) Debug.Log($"[Test] Soin accepté : {_health.Heal(healPerPress)}");
            if (kb.rKey.wasPressedThisFrame) _health.ResetHealth();
            if (kb.iKey.wasPressedThisFrame)
            {
                _health.Invincible = !_health.Invincible;
                Debug.Log($"[Test] Invincible : {_health.Invincible}");
            }
        }
    }
}
