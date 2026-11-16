// ------------------------------------------------------------
// Fichier : Billboard.cs
// Rôle    : fait toujours face à la caméra (rotation autour de l'axe
//           vertical seulement), pour les décors et objets en sprite 2D.
//           Fonctionne aussi hors Play : dans l'éditeur, le sprite se
//           tourne vers la Main Camera (vue Game).
// À mettre sur l'objet qui porte le SpriteRenderer.
// ------------------------------------------------------------
using UnityEngine;

namespace ChaosTower.Level
{
    [ExecuteAlways]
    public class Billboard : MonoBehaviour
    {
        private void LateUpdate() => Face();

        private void OnEnable() => Face();

        private void Face()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            Vector3 forward = cam.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(forward);
        }
    }
}
