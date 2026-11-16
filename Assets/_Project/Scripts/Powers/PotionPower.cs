// ------------------------------------------------------------
// Fichier : PotionPower.cs
// Autrice : SD — créé le 16/11/2026
// Rôle : pouvoir de l'étage 2. Lance une fiole en cloche
//        (Rigidbody, AddForce VelocityChange) qui explose à
//        l'impact (voir PotionProjectile et la figure 4 du TDD)
// Modifs :
// ------------------------------------------------------------
using UnityEngine;

namespace ChaosTower.Powers
{
    public class PotionPower : Power
    {
        [Header("Lancer")]
        [Tooltip("Décalage de départ par rapport à la caméra (devant, un peu en bas)")]
        [SerializeField] private Vector3 spawnOffset = new Vector3(0.2f, -0.25f, 0.6f);
        [Tooltip("Taille de la fiole provisoire")]
        [Range(0.05f, 0.5f)] [SerializeField] private float placeholderSize = 0.2f;

        public override bool Activate()
        {
            if (data == null || aimOrigin == null) return false;

            Vector3 spawn = aimOrigin.TransformPoint(spawnOffset);

            // Direction : vers l'avant à l'horizontale, relevée de l'angle de lancer
            Vector3 flatForward = Vector3.ProjectOnPlane(aimOrigin.forward, Vector3.up).normalized;
            Vector3 axis = Vector3.Cross(Vector3.up, flatForward); // axe « droite »
            Vector3 direction = Quaternion.AngleAxis(-data.LaunchAngle, axis) * flatForward;

            // Vitesse pour atteindre la portée sur sol plat : v0 = √(g·d / sin 2θ), soit 9,9 m/s pour 10 m à 45°
            float g = Mathf.Abs(Physics.gravity.y);
            float speed = Mathf.Sqrt(g * data.Range / Mathf.Sin(2f * data.LaunchAngle * Mathf.Deg2Rad));

            GameObject flask = CreateFlask(spawn);
            var body = flask.GetComponent<Rigidbody>();
            body.AddForce(direction * speed, ForceMode.VelocityChange);

            // La fiole ne doit pas rebondir sur le joueur qui la lance
            Collider flaskCollider = flask.GetComponent<Collider>();
            foreach (Collider own in GetComponentsInParent<Collider>())
                Physics.IgnoreCollision(flaskCollider, own);

            flask.GetComponent<PotionProjectile>().Init(data, transform.root.gameObject);
            PlaySound(data.CastSound);
            return true;
        }

        private GameObject CreateFlask(Vector3 position)
        {
            GameObject flask;
            if (data.ProjectilePrefab != null)
            {
                flask = Instantiate(data.ProjectilePrefab, position, Quaternion.identity);
            }
            else
            {
                // Fiole provisoire en attendant le sprite des infographistes
                flask = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flask.name = "PotionFlask";
                flask.transform.position = position;
                flask.transform.localScale = Vector3.one * placeholderSize;
                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", data.PlaceholderColor);
                flask.GetComponent<Renderer>().SetPropertyBlock(block);
            }

            if (!flask.TryGetComponent(out Rigidbody body))
                body = flask.AddComponent<Rigidbody>();
            body.mass = 0.2f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // ne traverse pas les murs

            if (!flask.TryGetComponent(out PotionProjectile _))
                flask.AddComponent<PotionProjectile>();
            return flask;
        }
    }
}
