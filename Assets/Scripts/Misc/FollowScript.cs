using UnityEngine;
using System.Collections;

public class FollowScript : MonoBehaviour
{


    public Transform ToFollow;
    public bool FollowX;
    public bool FollowY;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

        Vector3 _position = transform.position;

        if (FollowX)
            _position.x = ToFollow.position.x;

        if (FollowY)
            _position.y = ToFollow.position.y;


        transform.position = _position;

    }
}
