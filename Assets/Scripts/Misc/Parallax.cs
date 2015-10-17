using UnityEngine;
using System.Collections;

public class Parallax : MonoBehaviour
{

    Renderer _renderer;
    public float OffSetXVelocity;

    // Use this for initialization
    void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {
        _renderer.material.SetTextureOffset("_MainTex", new Vector2((OffSetXVelocity * Time.time), 0));
    }
}
