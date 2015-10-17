using UnityEngine;
using System.Collections;

public class CreditsScript : MonoBehaviour
{
    public float MinY;
    public float MaxY;
    public Transform CreditsObject;
    public float PassingVelocity;

    public void Start()
    {

    }

    private void Spawn()
    {
        CreditsObject.transform.position = new Vector3(transform.position.x, MinY, transform.position.z);
    }

    // Update is called once per frame
    void Update()
    {
        if (CreditsObject.transform.position.y >= MaxY)
        {
            Spawn();
        }

        CreditsObject.Translate(Vector3.up * PassingVelocity * Time.deltaTime);
    }
}
