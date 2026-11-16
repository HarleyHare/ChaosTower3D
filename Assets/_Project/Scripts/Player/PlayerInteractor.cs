// ------------------------------------------------------------
// Fichier : PlayerInteractor.cs
// Autrice : SS — créé le 11/11/2026
// Rôle    : regarde droit devant le joueur ; si l'objet visé est
//           interactif (porte…), affiche son texte et l'active quand
//           on appuie sur Interact (E / bouton A).
// À mettre sur l'objet Player, à côté du PlayerController.
// ------------------------------------------------------------
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChaosTower.Player
{
    public class PlayerInteractor : MonoBehaviour
    {
        [Tooltip("Fichier d'actions ChaosTowerControls (le même que le PlayerController).")]
        [SerializeField] private InputActionAsset controls;
        [Tooltip("Caméra du joueur (le rayon part de ses yeux).")]
        [SerializeField] private Camera playerCamera;
        [Tooltip("Distance maximale pour ouvrir une porte, en mètres.")]
        [Range(1f, 5f)] [SerializeField] private float range = 2.5f;

        private InputAction _interact;
        private IInteractable _target;

        private void Awake()
        {
            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
            _interact = controls.FindActionMap("Player", true).FindAction("Interact", true);
        }

        private void OnEnable() => _interact?.actionMap.Enable();

        private void Update()
        {
            _target = null;
            if (playerCamera == null) return;

            Transform cam = playerCamera.transform;
            if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, range, ~0, QueryTriggerInteraction.Ignore))
                _target = hit.collider.GetComponentInParent<IInteractable>();

            if (_target != null && _interact.WasPressedThisFrame())
                _target.Interact(gameObject);
        }
    }
}
