// ------------------------------------------------------------
// Fichier : ExplosionFlash.cs
// Autrice : SD — créé le 16/11/2026
// Rôle : effet d'explosion PROVISOIRE (boule + lumière, 0,3 s)
// Modifs :
// ------------------------------------------------------------
using System.Collections;
using UnityEngine;

namespace ChaosTower.Powers
{
    /// <summary>
    /// Effet d'explosion PROVISOIRE : une boule et une lumière qui grandissent puis s'éteignent en 0,3 s.
    /// Sera remplacé par un système de particules (TDD : URP/Particles/Unlit + Bloom).
    /// </summary>
    public class ExplosionFlash : MonoBehaviour
    {
        private const float Duration = 0.3f;

        public static void Spawn(Vector3 position, float radius, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "ExplosionFlash";
            Destroy(go.GetComponent<Collider>());
            go.transform.position = position;

            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            go.GetComponent<Renderer>().SetPropertyBlock(block);

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = radius * 2f;

            go.AddComponent<ExplosionFlash>().StartCoroutine(Animate(go.transform, light, radius));
        }

        private static IEnumerator Animate(Transform t, Light light, float radius)
        {
            for (float time = 0f; time < Duration; time += Time.deltaTime)
            {
                float k = time / Duration;
                t.localScale = Vector3.one * Mathf.Lerp(0.3f, radius * 0.5f, k); // reste plus petite que la zone
                light.intensity = Mathf.Lerp(8f, 0f, k);
                yield return null;
            }
            Destroy(t.gameObject);
        }
    }
}
