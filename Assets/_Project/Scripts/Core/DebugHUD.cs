// ------------------------------------------------------------
// Fichier : DebugHUD.cs
// Autrice : SD — créé le 03/11/2026
// Rôle : affichage TEMPORAIRE pour les tests (viseur, munitions,
//        arme en main). Sera remplacé par le vrai HUD de Mila.
// Modifs :
// ------------------------------------------------------------
using ChaosTower.Player;
using ChaosTower.Weapons;
using UnityEngine;

namespace ChaosTower
{
    public class DebugHUD : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private WeaponManager weapons;

        private GUIStyle _style;

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
            GUI.Label(new Rect(20, Screen.height - 70, 600, 30), $"Arme : {weaponName}   Munitions : {ammo}", _style);
            GUI.Label(new Rect(20, Screen.height - 40, 900, 30),
                "ZQSD bouger · Shift courir · Souris tourner · Clic tirer · 1/2/3 armes · Échap libérer la souris", _style);
        }
    }
}
