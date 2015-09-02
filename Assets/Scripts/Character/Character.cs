using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class Character : MonoBehaviour
{
    /* States */
    public CharacterStateEnum CharacterState;

    /* Physics Variables */
    public float Velocity;
    public float VelocityInAir;
    public float JumpForce;

    /* Input Variables */
    public bool DoJump;
    public float MovingToX;

    /* Required Components */
    private Rigidbody2D rigidBody;

    private Vector3 _velocity;
    private Vector2 _jumpForce;

    public void Awake()
    {
        _velocity = new Vector3();
        _jumpForce = new Vector2();

        rigidBody = GetComponent<Rigidbody2D>();
    }

    public void FixedUpdate()
    {
        switch (CharacterState)
        {
            case CharacterStateEnum.Idle:
                Run();
                Jump();
                break;
            case CharacterStateEnum.Running:
                Run();
                Jump();
                break;
            case CharacterStateEnum.Jumping:
                MoveWhileJumping();
                break;
            case CharacterStateEnum.Dead:

                break;
        }

        MovingToX = 0;
    }

    private void Run()
    {
        _velocity.Set(MovingToX * Velocity, rigidBody.velocity.y, 0);
        rigidBody.velocity = _velocity;
    }

    private void Jump()
    {
        if (DoJump)
        {
            _jumpForce.Set(0, JumpForce);
            rigidBody.AddForce(_jumpForce, ForceMode2D.Impulse);
            DoJump = false;
        }
    }

    private void MoveWhileJumping()
    {
        if (MovingToX != 0)
        {
            _velocity.Set(MovingToX * VelocityInAir, rigidBody.velocity.y, 0);
            rigidBody.velocity = _velocity;
        }
    }
}

public enum CharacterStateEnum
{
    Idle,
    Running,
    Jumping,
    Dead
}