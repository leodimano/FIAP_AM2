﻿using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class Character : MonoBehaviour
{
    /* States */
    public CharacterStateEnum CharacterState;

    /* Physics Variables */
    public float Velocity;
    public float DeaccelerationTime;

    public float JumpForce;
    private bool StartJumping;

    public LayerMask FloorMask;
    public bool OnFloor;
    public bool OnTop;
    public bool OnLeft;
    public bool OnRight;

    public bool OnStair;
    public float ClimbingXVelocity;
    public float ClimbingYVelocity;
    private Collider2D StairCollider;

    /* Input Variables */
    public bool DoJump;
    public float MovingToX;
    public float MovingToY;

    /* RayCast Floor Collision Check */
    public int VerticalRaycastCount = 4;
    public int HorizontalRaycastCount = 8;
    public float VerticalRaycastDistance = 0.15f;
    public float HorizontalRaycastDistance = 0.15f;

    const float _skinWidth = .015f;
    float _verticalRaycastSpacing;
    float _horizontalRaycastSpacing;
    RaycastOrigin _raycastOrigin;
    BoxCollider2D _boxCollider2D;


    /* Required Components */
    private Rigidbody2D _rigidBody;

    private Vector2 _velocity;
    private Vector2 _jumpForce;

    public void Awake()
    {
        _velocity = new Vector2();
        _jumpForce = new Vector2();

        _rigidBody = GetComponent<Rigidbody2D>();
        _boxCollider2D = GetComponent<BoxCollider2D>();
        _raycastOrigin = new RaycastOrigin();
    }

    public void FixedUpdate()
    {
        SetCharacterState();

        CheckCollision();

        switch (CharacterState)
        {
            case CharacterStateEnum.Idle:
                _rigidBody.isKinematic = false;
                Move();
                Jump();
                break;
            case CharacterStateEnum.Running:
                _rigidBody.isKinematic = false;
                Move();
                Jump();
                break;
            case CharacterStateEnum.Jumping:
                _rigidBody.isKinematic = false;
                Move();
                break;
            case CharacterStateEnum.Climbing:
                _rigidBody.isKinematic = true;
                Moveclimbing();
                JumpClimbing();
                break;
            case CharacterStateEnum.Dead:
                _rigidBody.isKinematic = false;
                break;
        }

        MovingToX = 0;
        MovingToY = 0;
    }

    private void SetCharacterState()
    {
        if (OnStair && CharacterState == CharacterStateEnum.Climbing)
        {
            return;
        }
        else if (OnStair && MovingToY == 1)
        {
            CharacterState = CharacterStateEnum.Climbing;
            /* Fix Character Position relative to Stair */
            transform.position = new Vector3(StairCollider.transform.position.x, transform.position.y, transform.position.z);
            return;
        }

        if ((!OnFloor && StartJumping) || (!OnFloor))
        {
            StartJumping = false;
            CharacterState = CharacterStateEnum.Jumping;
            return;
        }

        if (OnFloor && _rigidBody.velocity.x != 0)
        {
            CharacterState = CharacterStateEnum.Running;
            return;
        }


        CharacterState = CharacterStateEnum.Idle;
    }

    private void CheckCollision()
    {
        OnFloor = false;
        OnTop = false;
        OnRight = false;
        OnLeft = false;

        Bounds _bounds = _boxCollider2D.bounds;
        _bounds.Expand(_skinWidth * -1);

        _raycastOrigin.BottomLeft = new Vector2(_bounds.min.x, _bounds.min.y);
        _raycastOrigin.BottomRight = new Vector2(_bounds.max.x, _bounds.min.y);
        _raycastOrigin.TopLeft = new Vector2(_bounds.min.x, _bounds.max.y);
        _raycastOrigin.TopRight = new Vector2(_bounds.max.x, _bounds.max.y);

        VerticalRaycastCount = Mathf.Clamp(VerticalRaycastCount, 2, int.MaxValue);
        HorizontalRaycastCount = Mathf.Clamp(HorizontalRaycastCount, 2, int.MaxValue);

        _verticalRaycastSpacing = _bounds.size.x / (VerticalRaycastCount - 1);
        _horizontalRaycastSpacing = _bounds.size.y / (HorizontalRaycastCount - 1);

        Vector2 _bottomRaycastOrigin;
        Vector2 _topRaycastOrigin;
        Vector2 _rightRaycastOrigin;
        Vector2 _leftRaycastOrigin;

        for (int i = 0; i < VerticalRaycastCount; i++)
        {
            _bottomRaycastOrigin = _raycastOrigin.BottomLeft + (Vector2.right * _verticalRaycastSpacing * i);
            _topRaycastOrigin = _raycastOrigin.TopLeft + (Vector2.right * _verticalRaycastSpacing * i);

            RaycastHit2D _bottomRaycast = Physics2D.Raycast(_bottomRaycastOrigin, Vector2.down, VerticalRaycastDistance, FloorMask);
            RaycastHit2D _topRaycast = Physics2D.Raycast(_topRaycastOrigin, Vector2.up, VerticalRaycastDistance, FloorMask);

            if (_bottomRaycast)
                OnFloor = true;
            else
                Debug.DrawRay(_bottomRaycastOrigin, Vector2.down * VerticalRaycastDistance, Color.red);

            if (_topRaycast)
                OnTop = true;
            else
                Debug.DrawRay(_topRaycastOrigin, Vector2.up * VerticalRaycastDistance, Color.red);
        }

        for (int i = 0; i < HorizontalRaycastCount; i++)
        {
            _rightRaycastOrigin = _raycastOrigin.TopRight + (Vector2.down * _horizontalRaycastSpacing * i);
            _leftRaycastOrigin = _raycastOrigin.TopLeft + (Vector2.down * _horizontalRaycastSpacing * i);

            RaycastHit2D _rightRaycast = Physics2D.Raycast(_rightRaycastOrigin, Vector2.right, HorizontalRaycastDistance, FloorMask);
            RaycastHit2D _leftRaycast = Physics2D.Raycast(_leftRaycastOrigin, Vector2.left, HorizontalRaycastDistance, FloorMask);

            if (_rightRaycast)
                OnRight = true;
            else
                Debug.DrawRay(_rightRaycastOrigin, Vector2.right * HorizontalRaycastDistance, Color.red);

            if (_leftRaycast)
                OnLeft = true;
            else
                Debug.DrawRay(_leftRaycastOrigin, Vector2.left * HorizontalRaycastDistance, Color.red);
        }
    }

    private void Move()
    {
        if (MovingToX > 0 && OnRight)
            MovingToX = 0;
        else if (MovingToX < 0 && OnLeft)
            MovingToX = 0;

        if (MovingToY > 0 && OnTop)
            MovingToY = 0;
        else if (MovingToY < 0 && OnFloor)
            MovingToY = 0;

        if (MovingToX != 0)
        {
            _velocity.Set(MovingToX * Velocity, _rigidBody.velocity.y);
            _rigidBody.velocity = _velocity;
        }
        else
        {
            _rigidBody.velocity = Vector2.Lerp(_rigidBody.velocity, new Vector2(0, _rigidBody.velocity.y), DeaccelerationTime);
        }
    }

    private void Moveclimbing()
    {
        if (MovingToX > 0 && OnRight)
            MovingToX = 0;
        else if (MovingToX < 0 && OnLeft)
            MovingToX = 0;

        if (MovingToY > 0 && OnTop)
            MovingToY = 0;
        else if (MovingToY < 0 && OnFloor)
            MovingToY = 0;

        transform.Translate(ClimbingXVelocity * MovingToX * Time.deltaTime, ClimbingYVelocity * MovingToY * Time.deltaTime, 0);
    }

    private void JumpClimbing()
    {
        if (DoJump)
        {
            _rigidBody.isKinematic = false;
            _jumpForce.Set(0, JumpForce);
            _rigidBody.AddForce(_jumpForce, ForceMode2D.Impulse);
            DoJump = false;
            StartJumping = true;
        }
    }

    private void Jump()
    {
        if (DoJump)
        {
            _jumpForce.Set(0, JumpForce);
            _rigidBody.AddForce(_jumpForce, ForceMode2D.Impulse);
            DoJump = false;
            StartJumping = true;
        }
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == Constants.TAG_STAIR)
        {
            StairCollider = collision;
            OnStair = true;
        }
    }

    public void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.tag == Constants.TAG_STAIR)
        {
            StairCollider = null;
            OnStair = false;
        }
    }

    struct RaycastOrigin
    {
        public Vector2 BottomRight, BottomLeft;
        public Vector2 TopRight, TopLeft;
    }
}

public enum CharacterStateEnum
{
    Idle,
    Running,
    Jumping,
    Climbing,
    Dead,
}