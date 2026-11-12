using UnityEngine;

// Lá phong rơi (màn 2)
public class LeafFall : MonoBehaviour
{
    float speed, wobble;
    void Start() { Respawn(true); }
    void Respawn(bool any)
    {
        speed = 0.5f + Random.value * 0.6f; wobble = Random.value * 6f;
        transform.position = new Vector3((Random.value - 0.5f) * 30f, any ? Random.value * 8f : 7f + Random.value * 2f, (Random.value - 0.5f) * 16f - 2f);
    }
    void Update()
    {
        wobble += Time.deltaTime * 2f;
        transform.position += new Vector3(Mathf.Sin(wobble) * 0.6f, -speed, 0) * Time.deltaTime;
        transform.rotation = Quaternion.Euler(wobble * 57f, wobble * 40f, 0);
        if (transform.position.y < 0.05f) Respawn(false);
    }
}

// Tuyết rơi (màn 4)
public class SnowFall : MonoBehaviour
{
    float speed, seed;
    void Start() { seed = Random.value * 10f; speed = 1f + Random.value * 0.5f; transform.position = new Vector3((Random.value - 0.5f) * 44f, Random.value * 14f, (Random.value - 0.5f) * 30f - 3f); }
    void Update()
    {
        var p = transform.position;
        p.y -= speed * Time.deltaTime; p.x += Mathf.Sin(Time.time + seed) * 0.2f * Time.deltaTime;
        if (p.y < 0) p.y = 14f;
        transform.position = p;
    }
}

// Lửa phun từ miệng rồng theo nhịp (màn 5)
public class FireParticle : MonoBehaviour
{
    public Vector3 origin, direction; public float k;
    void Update()
    {
        float t = Time.time, cyc = t % 6f; bool on = cyc < 2.2f;
        float u = (t * 1.4f + k) % 1f;
        transform.position = origin + direction * u * 5f + new Vector3(0, (k - 0.5f) * 0.8f * u, (k - 0.5f) * 1.2f * u);
        transform.localScale = Vector3.one * (on ? (0.6f + u * 1.6f) * (1f - u) * 0.7f : 0f);
    }
}
