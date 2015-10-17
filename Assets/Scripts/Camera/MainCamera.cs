using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class MainCamera : MonoBehaviour
{
    public float FollowInT;
    public Transform FollowObject;
    public Transform MinXPosition;
    public Transform MaxXPosition;
    public Transform MinYPosition;
    public Transform MaxYPosition;

    private Vector3 _offsetPosition;
    private Camera _camera;
    private Rigidbody2D _characterRigidBody;



    public void Awake()
    {
        _offsetPosition = new Vector3();
        _camera = GetComponent<Camera>();
    }

    public void LateUpdate()
    {
        if (FollowObject != null)
        {
            float bufferX, bufferY;

            if ((FollowObject.position.x - _camera.orthographicSize) > MinXPosition.position.x)
            {
                bufferX = FollowObject.position.x;
            }
            else
            {
                bufferX = MinXPosition.position.x + _camera.orthographicSize;
            }

            if ((FollowObject.transform.position.y - _camera.orthographicSize) > MinYPosition.position.y)
            {
                bufferY = FollowObject.position.y;
            }
            else
            {
                bufferY = MinYPosition.position.y + _camera.orthographicSize;
            }

            _offsetPosition.Set(bufferX, bufferY, transform.position.z);

            transform.position = Vector3.Lerp(transform.position, _offsetPosition, FollowInT);
        }
    }
}
