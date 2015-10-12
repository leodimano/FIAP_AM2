using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider2D))]
public class CullingMaskHandler : MonoBehaviour
{
    public LayerMask NewCullingLayerMask;
    public Camera TargetCamera;
    private LayerMask _currentLayerMask;

    public void Awake()
    {
        _currentLayerMask = TargetCamera.cullingMask;
    }

    // Use this for initialization
    public void OnTriggerEnter2D(Collider2D collision)
    {
        TargetCamera.cullingMask = NewCullingLayerMask;
    }

    public void OnTriggerExit2D(Collider2D collision)
    {
        TargetCamera.cullingMask = _currentLayerMask;
    }

}
