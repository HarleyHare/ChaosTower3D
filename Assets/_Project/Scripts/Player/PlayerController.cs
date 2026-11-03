// ------------------------------------------------------------
// Fichier : PlayerController.cs
// Autrice : SD — créé le 03/11/2026
// Rôle : déplacement FPS du joueur (marche, course, rotation
//        horizontale uniquement) et réglage du champ de vision
// Modifs :
// ------------------------------------------------------------
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChaosTower.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Références")]
        [Tooltip("Fichier d'actions ChaosTowerControls")]
        [SerializeField] private InputActionAsset controls;
        [Tooltip("Caméra enfant du joueur, placée à 1,6 m de hauteur")]
        [SerializeField] private Camera playerCamera;

        [Header("Déplacement (MGDD)")]
        [Range(1f, 15f)] [SerializeField] private float walkSpeed = 6f;
        [Range(1f, 20f)] [SerializeField] private float sprintSpeed = 9f;
        [Tooltip("Gravité constante pour rester collé au sol")]
        [SerializeField] private float gravity = -20f;

        [Header("Rotation")]
        [Tooltip("Degrés par pixel de souris")]
        [Range(0.01f, 1f)] [SerializeField] private float mouseSensitivity = 0.15f;
        [Tooltip("Degrés par seconde, stick à fond")]
        [Range(30f, 360f)] [SerializeField] private float gamepadTurnSpeed = 180f;

        [Header("Caméra")]
        [Tooltip("FOV horizontal du MGDD, converti en FOV vertical pour Unity")]
        [Range(60f, 110f)] [SerializeField] private float horizontalFov = 80f;

        private CharacterController _controller;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _sprintAction;
        private float _verticalVelocity;
        private float _lastAspect;

        public bool IsMoving { get; private set; }   // lu plus tard par WeaponView (head bob)
        public bool IsSprinting { get; private set; }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();

            InputActionMap map = controls.FindActionMap("Player", throwIfNotFound: true);
            _moveAction = map.FindAction("Move", true);
            _lookAction = map.FindAction("Look", true);
            _sprintAction = map.FindAction("Sprint", true);

            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        }

        private void OnEnable()
        {
            controls.FindActionMap("Player").Enable();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            controls.FindActionMap("Player").Disable();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            UpdateFov();
            Rotate();
            Move();
        }

        // Rotation autour de l'axe vertical uniquement (pas de visée haut/bas)
        private void Rotate()
        {
            float lookX = _lookAction.ReadValue<Vector2>().x;
            bool fromMouse = _lookAction.activeControl?.device is Mouse;

            // La souris donne un déplacement en pixels depuis la dernière image :
            // pas de Time.deltaTime. Le stick donne une position entre -1 et 1 :
            // on le multiplie par une vitesse et par Time.deltaTime.
            float yaw = fromMouse
                ? lookX * mouseSensitivity
                : lookX * gamepadTurnSpeed * Time.deltaTime;

            transform.Rotate(0f, yaw, 0f);
        }

        private void Move()
        {
            Vector2 input = _moveAction.ReadValue<Vector2>();
            IsSprinting = _sprintAction.IsPressed();
            IsMoving = input.sqrMagnitude > 0.01f;

            Vector3 direction = transform.forward * input.y + transform.right * input.x;
            if (direction.sqrMagnitude > 1f) direction.Normalize(); // la diagonale n'est pas plus rapide

            float speed = IsSprinting ? sprintSpeed : walkSpeed;

            if (_controller.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f; // petite valeur pour rester plaqué au sol
            else
                _verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = direction * speed + Vector3.up * _verticalVelocity;
            _controller.Move(velocity * Time.deltaTime);
        }

        // Recalcule le FOV vertical seulement si le format de l'écran change
        private void UpdateFov()
        {
            if (playerCamera == null || Mathf.Approximately(playerCamera.aspect, _lastAspect)) return;
            _lastAspect = playerCamera.aspect;
            playerCamera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(horizontalFov, _lastAspect);
        }
    }
}
