using UnityEngine;
using System.Collections;

public class WormAcidSpit : MonoBehaviour
{
    public float Velocity;
    public float LifeTime;

    // Update is called once per frame
    void Update()
    {
        transform.Translate(Vector3.down * Velocity * Time.deltaTime);
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        Destroy(gameObject);
    }
}
