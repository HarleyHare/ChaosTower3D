// ------------------------------------------------------------
// Fichier : PowerManager.cs
// Autrice : SD — créé le 16/11/2026
// Rôle : jauge de pouvoir du joueur : 5 charges, une charge
//        regagnée toutes les 60 s, touche Pouvoir (clic droit / Y)
// Modifs :
// ------------------------------------------------------------
using System;
using ChaosTower.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChaosTower.Powers
{
    public class PowerManager : MonoBehaviour
    {
        [Header("Références")]
        [Tooltip("Fichier d'actions ChaosTowerControls")]
        [SerializeField] private InputActionAsset controls;
        [Tooltip("Pouvoir de l'étage : vide à l'étage 1, PotionPower à l'étage 2, TeleportPower à l'étage 3")]
        [SerializeField] private Power activePower;

        private InputAction _powerAction;
        private PlayerHealth _health;
        private int _charges;
        private float _rechargeTimer;

        /// <summary>Charges actuelles et maximum. Le HUD de Mila s'y abonne.</summary>
        public event Action<int, int> ChargesChanged;
        /// <summary>Un pouvoir vient d'être utilisé (animation, son du HUD).</summary>
        public event Action<Power> PowerUsed;

        public Power ActivePower => activePower;
        public int Charges => _charges;
        public int MaxCharges => activePower != null && activePower.Data != null ? activePower.Data.MaxCharges : 0;
        /// <summary>Secondes avant la prochaine charge (0 si la jauge est pleine).</summary>
        public float TimeToNextCharge => IsFull || activePower == null ? 0f : activePower.Data.RechargeTime - _rechargeTimer;
        /// <summary>Avancement de la recharge en cours, de 0 à 1 (pour une barre du HUD).</summary>
        public float RechargeProgress => IsFull || activePower == null ? 1f : _rechargeTimer / activePower.Data.RechargeTime;
        private bool IsFull => _charges >= MaxCharges;

        private void Awake()
        {
            _powerAction = controls.FindActionMap("Player", throwIfNotFound: true).FindAction("Power", true);
            _health = GetComponent<PlayerHealth>();
            _charges = MaxCharges; // jauge pleine en début d'étage
        }

        private void OnEnable() => controls.FindActionMap("Player").Enable();

        private void Start() => ChargesChanged?.Invoke(_charges, MaxCharges);

        private void Update()
        {
            if (Time.timeScale == 0f || activePower == null || activePower.Data == null) return;
            if (_health != null && _health.IsDead) return;

            Recharge();

            if (_powerAction.WasPressedThisFrame() && _charges > 0 && activePower.Activate())
            {
                _charges--;
                ChargesChanged?.Invoke(_charges, MaxCharges);
                PowerUsed?.Invoke(activePower);
            }
        }

        private void Recharge()
        {
            if (IsFull)
            {
                _rechargeTimer = 0f;
                return;
            }

            _rechargeTimer += Time.deltaTime;
            if (_rechargeTimer >= activePower.Data.RechargeTime)
            {
                _rechargeTimer = 0f;
                _charges++;
                ChargesChanged?.Invoke(_charges, MaxCharges);
            }
        }

        /// <summary>
        /// Change le pouvoir (appelé au chargement d'un étage d'après son LevelData).
        /// Les pouvoirs ne se cumulent pas (MGDD) : la jauge repart pleine.
        /// </summary>
        public void SetPower(Power power)
        {
            activePower = power;
            _charges = MaxCharges;
            _rechargeTimer = 0f;
            ChargesChanged?.Invoke(_charges, MaxCharges);
        }

        /// <summary>Remplit la jauge (menu de débogage, début d'étage).</summary>
        public void Refill()
        {
            _charges = MaxCharges;
            _rechargeTimer = 0f;
            ChargesChanged?.Invoke(_charges, MaxCharges);
        }
    }
}
