// ------------------------------------------------------------
// Fichier : PlayerInventory.cs
// Autrice : SD — créé le 03/11/2026
// Rôle : munitions partagées (max 99) et clés du joueur
// Modifs :
// ------------------------------------------------------------
using System;
using UnityEngine;

namespace ChaosTower.Player
{
    public enum KeyColor { Gold, Silver }

    public class PlayerInventory : MonoBehaviour
    {
        public const int MaxAmmo = 99;

        [Header("Départ")]
        [Tooltip("Munitions au début de l'étage")]
        [Range(0, MaxAmmo)] [SerializeField] private int startAmmo = 8;

        private int _ammo;
        private bool _hasGoldKey;
        private bool _hasSilverKey;

        /// <summary>Nouvelle quantité de munitions. Le HUD s'y abonne.</summary>
        public event Action<int> AmmoChanged;
        /// <summary>Clé ramassée. Le HUD s'y abonne.</summary>
        public event Action<KeyColor> KeyCollected;

        public int Ammo => _ammo;

        private void Awake()
        {
            _ammo = startAmmo;
        }

        private void Start()
        {
            AmmoChanged?.Invoke(_ammo); // valeur initiale pour le HUD
        }

        /// <summary>Ajoute des munitions. Renvoie false si le joueur est déjà au maximum (le pickup reste au sol).</summary>
        public bool AddAmmo(int amount)
        {
            if (amount <= 0 || _ammo >= MaxAmmo) return false;
            _ammo = Mathf.Min(_ammo + amount, MaxAmmo);
            AmmoChanged?.Invoke(_ammo);
            return true;
        }

        /// <summary>Retire des munitions si possible. Renvoie false s'il n'y en a pas assez.</summary>
        public bool TryConsumeAmmo(int amount)
        {
            if (amount <= 0) return true;
            if (_ammo < amount) return false;
            _ammo -= amount;
            AmmoChanged?.Invoke(_ammo);
            return true;
        }

        public bool HasKey(KeyColor color) => color == KeyColor.Gold ? _hasGoldKey : _hasSilverKey;

        public void AddKey(KeyColor color)
        {
            if (color == KeyColor.Gold) _hasGoldKey = true;
            else _hasSilverKey = true;
            KeyCollected?.Invoke(color);
        }
    }
}
