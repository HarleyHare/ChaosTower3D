// ------------------------------------------------------------
// Fichier : DebugHUD.cs
// Autrice : SD — créé le 03/11/2026
// Rôle : affichage TEMPORAIRE pour les tests (viseur, munitions,
//        arme en main). Sera remplacé par le vrai HUD de Mila.
// Modifs : 16/11/2026 SD — affiche les PV, l'invincibilité et la mort
//          16/11/2026 SD — affiche la jauge de pouvoir
// ------------------------------------------------------------
using ChaosTower.Player;
using ChaosTower.Powers;
using ChaosTower.Weapons;
using UnityEngine;

namespace ChaosTower
{
    public class DebugHUD : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private WeaponManager weapons;
        [Tooltip("Trouvée automatiquement sur le joueur si vide")]
        [SerializeField] private PlayerHealth health;
        [Tooltip("Trouvé automatiquement sur le joueur si vide")]
        [SerializeField] private PowerManager powers;

        private GUIStyle _style;

        private void Awake()
        {
            if (health == null) health = GetComponent<PlayerHealth>();
            if (powers == null) powers = GetComponent<PowerManager>();
        }

        private void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
                _style.normal.textColor = Color.white;
            }

            // Viseur au centre de l'écran
            Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);
            GUI.Label(new Rect(center.x - 8, center.y - 15, 30, 30), "+", _style);

            // Infos en bas à gauche
            string weaponName = weapons != null && weapons.CurrentWeapon != null && weapons.CurrentWeapon.Data != null
                ? weapons.CurrentWeapon.Data.DisplayName : "-";
            string ammo = inventory != null ? inventory.Ammo.ToString() : "-";
            if (powers != null && powers.ActivePower != null && powers.ActivePower.Data != null)
            {
                string next = powers.Charges < powers.MaxCharges ? $"   (+1 dans {Mathf.CeilToInt(powers.TimeToNextCharge)} s)" : "";
                GUI.Label(new Rect(20, Screen.height - 160, 900, 30),
                    $"Pouvoir : {powers.ActivePower.Data.DisplayName}   Charges : {powers.Charges}/{powers.MaxCharges}{next}   (clic droit)", _style);
            }

            if (health != null)
            {
                string pv = $"PV : {Mathf.CeilToInt(health.Health)} / {health.MaxHealth:0}";
                if (health.Invincible) pv += "   (invincible)";
                Color old = _style.normal.textColor;
                if (health.IsDead || health.IsCritical) _style.normal.textColor = Color.red;
                GUI.Label(new Rect(20, Screen.height - 130, 600, 30), pv, _style);
                if (health.IsDead)
                    GUI.Label(new Rect(center.x - 60, center.y + 30, 300, 30), "MORT (R pour revivre)", _style);
                _style.normal.textColor = old;
                GUI.Label(new Rect(20, Screen.height - 100, 900, 30),
                    "Test santé : K -10 PV · L +25 PV · R revivre · I invincible", _style);
            }

            GUI.Label(new Rect(20, Screen.height - 70, 600, 30), $"Arme : {weaponName}   Munitions : {ammo}", _style);
            GUI.Label(new Rect(20, Screen.height - 40, 900, 30),
                "ZQSD bouger · Shift courir · Souris tourner · Clic tirer · 1/2/3 armes · Échap libérer la souris", _style);
        }
    }
}
