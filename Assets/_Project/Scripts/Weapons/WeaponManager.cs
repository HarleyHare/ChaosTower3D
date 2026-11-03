// ------------------------------------------------------------
// Fichier : WeaponManager.cs
// Autrice : SD — créé le 03/11/2026
// Rôle : emplacements d'armes du joueur (corps à corps, arme de
//        poing, arme rafale), changement d'arme et tir
// Modifs :
// ------------------------------------------------------------
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChaosTower.Weapons
{
    public class WeaponManager : MonoBehaviour
    {
        public const int MeleeSlot = 0;
        public const int HandgunSlot = 1;
        public const int AutomaticSlot = 2;

        [Header("Références")]
        [Tooltip("Fichier d'actions ChaosTowerControls")]
        [SerializeField] private InputActionAsset controls;
        [Tooltip("0 = corps à corps, 1 = arme de poing, 2 = arme rafale")]
        [SerializeField] private Weapon[] slots = new Weapon[3];

        [Header("Départ (MGDD)")]
        [Tooltip("Armes possédées au début de l'étage. L'arme rafale se ramasse.")]
        [SerializeField] private bool[] startUnlocked = { true, true, false };
        [Range(0, 2)] [SerializeField] private int startSlot = HandgunSlot;

        private bool[] _unlocked;
        private int _current = -1;
        private InputAction _fireAction;
        private InputAction _nextAction;
        private InputAction _previousAction;
        private InputAction[] _slotActions;

        /// <summary>Nouvelle arme en main (emplacement, arme). Le HUD et WeaponView s'y abonnent.</summary>
        public event Action<int, Weapon> WeaponChanged;

        public Weapon CurrentWeapon => _current >= 0 ? slots[_current] : null;
        public int CurrentSlot => _current;

        private void Awake()
        {
            InputActionMap map = controls.FindActionMap("Player", throwIfNotFound: true);
            _fireAction = map.FindAction("Fire", true);
            _nextAction = map.FindAction("NextWeapon", true);
            _previousAction = map.FindAction("PreviousWeapon", true);
            _slotActions = new[]
            {
                map.FindAction("Weapon1", true),
                map.FindAction("Weapon2", true),
                map.FindAction("Weapon3", true)
            };

            _unlocked = new bool[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                _unlocked[i] = i < startUnlocked.Length && startUnlocked[i] && slots[i] != null;
                if (slots[i] != null) slots[i].gameObject.SetActive(false);
            }
        }

        private void OnEnable() => controls.FindActionMap("Player").Enable();

        private void Start()
        {
            if (!Equip(startSlot)) EquipFirstAvailable();
        }

        private void Update()
        {
            if (Time.timeScale == 0f) return; // jeu en pause

            HandleSwitching();
            HandleFiring();
        }

        private void HandleSwitching()
        {
            for (int i = 0; i < _slotActions.Length; i++)
                if (_slotActions[i].WasPressedThisFrame()) Equip(i);

            if (_nextAction.WasPressedThisFrame()) Cycle(+1);
            if (_previousAction.WasPressedThisFrame()) Cycle(-1);
        }

        private void HandleFiring()
        {
            Weapon weapon = CurrentWeapon;
            if (weapon == null) return;

            // Arme rafale : tant que le bouton est maintenu. Autres armes : un tir par appui.
            bool wantsToFire = weapon.IsAutomatic ? _fireAction.IsPressed() : _fireAction.WasPressedThisFrame();
            if (wantsToFire) weapon.TryFire();
        }

        /// <summary>Met en main l'arme d'un emplacement si le joueur la possède.</summary>
        public bool Equip(int slot)
        {
            if (slot < 0 || slot >= slots.Length || !_unlocked[slot] || slot == _current) return false;

            if (_current >= 0) slots[_current].gameObject.SetActive(false);
            _current = slot;
            slots[_current].gameObject.SetActive(true);
            WeaponChanged?.Invoke(_current, slots[_current]);
            return true;
        }

        /// <summary>
        /// Appelé par le pickup d'arme. Première fois : l'arme est débloquée et mise en main.
        /// Déjà possédée : seules les munitions sont données (MGDD).
        /// Renvoie true si le pickup a servi.
        /// </summary>
        public bool GiveWeapon(int slot, int ammo)
        {
            if (slot < 0 || slot >= slots.Length || slots[slot] == null) return false;

            var inventory = GetComponentInParent<Player.PlayerInventory>();
            bool gotAmmo = inventory != null && inventory.AddAmmo(ammo);

            if (_unlocked[slot]) return gotAmmo;

            _unlocked[slot] = true;
            Equip(slot);
            return true;
        }

        private void Cycle(int direction)
        {
            for (int step = 1; step < slots.Length; step++)
            {
                int candidate = ((_current + direction * step) % slots.Length + slots.Length) % slots.Length;
                if (Equip(candidate)) return;
            }
        }

        private void EquipFirstAvailable()
        {
            for (int i = 0; i < slots.Length; i++)
                if (Equip(i)) return;
        }
    }
}
