using UnityEngine;

namespace RongRanChamCham
{
    public class VictoryCelebrationFX : MonoBehaviour
    {
        public static void Spawn(Vector3 center)
        {
            var root = new GameObject("FX_Victory_Confetti");
            root.transform.position = center;
            root.AddComponent<VictoryCelebrationFX>();
        }

        private struct Particle
        {
            public Transform t;
            public Vector3 vel;
            public Vector3 rotSpeed;
        }

        private Particle[] particles;
        private float lifeTimer = 0f;

        private void Start()
        {
            int count = 65;
            particles = new Particle[count];
            Color[] colors = new Color[]
            {
                new Color(1.0f, 0.85f, 0.1f), // Gold
                new Color(0.1f, 0.9f, 1.0f),  // Cyan
                new Color(1.0f, 0.2f, 0.6f),  // Pink
                new Color(0.3f, 1.0f, 0.3f),  // Lime
                new Color(1.0f, 0.5f, 0.1f)   // Orange
            };

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");

            for (int i = 0; i < count; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                go.name = "Confetti_" + i;
                go.transform.SetParent(transform, false);
                Destroy(go.GetComponent<Collider>());

                float rx = Random.Range(-3.5f, 3.5f);
                float rz = Random.Range(-2.5f, 2.5f);
                float ry = Random.Range(2.5f, 5.5f);
                go.transform.localPosition = new Vector3(rx, ry, rz);
                go.transform.localScale = new Vector3(Random.Range(0.12f, 0.22f), Random.Range(0.12f, 0.22f), 1f);

                var mat = new Material(shader);
                mat.SetInt("_Cull", 0); // Double sided
                Color col = colors[Random.Range(0, colors.Length)];
                mat.SetColor("_BaseColor", col);
                mat.color = col;
                go.GetComponent<Renderer>().sharedMaterial = mat;

                particles[i] = new Particle
                {
                    t = go.transform,
                    vel = new Vector3(Random.Range(-0.4f, 0.4f), -Random.Range(1.2f, 2.4f), Random.Range(-0.4f, 0.4f)),
                    rotSpeed = new Vector3(Random.Range(80f, 260f), Random.Range(80f, 260f), Random.Range(80f, 260f))
                };
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            lifeTimer += dt;

            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i].t == null) continue;
                Vector3 p = particles[i].t.localPosition;
                p += particles[i].vel * dt;
                // Flutter effect
                p.x += Mathf.Sin(lifeTimer * 3f + i) * 0.4f * dt;
                particles[i].t.localPosition = p;
                particles[i].t.Rotate(particles[i].rotSpeed * dt, Space.Self);
            }

            if (lifeTimer > 10f)
            {
                Destroy(gameObject);
            }
        }
    }
}
