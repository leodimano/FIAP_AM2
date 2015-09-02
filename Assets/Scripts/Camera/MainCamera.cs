using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class MainCamera : MonoBehaviour
{
    public Transform FollowObject;

    private Vector3 _offsetPosition;

    public void Awake()
    {
        _offsetPosition = new Vector3();
    }

    public void LateUpdate()
    {
        if (FollowObject != null)
        {
            _offsetPosition.Set(FollowObject.position.x, FollowObject.position.y, transform.position.z);
            transform.position = _offsetPosition;
        }
    }
}
