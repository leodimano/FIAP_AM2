﻿using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class Character : MonoBehaviour, HookableInterface
{
    /* Estados do personagem */
    public CharacterStateEnum CharacterState;

    /* Variaveis para controle da fisica */
    public float Velocity;
    public float DeaccelerationTime;

    public float JumpForce;
    public float H_StairJumpForce;
    private bool StartJumping;

    public LayerMask FloorMask;
    public bool OnFloor;
    public bool OnTop;
    public bool OnLeft;
    public bool OnRight;
    public bool FacingRight;

    public bool OnStair;
    public float ClimbingXVelocity;
    public float ClimbingYVelocity;
    private Collider2D StairCollider;

    /* Variáveis do pulo na parede */
    public bool OnJumpinWall;

    /* Variaveis de Input */
    public bool DoJump;
    public float MovingToX;
    public float MovingToY;
    public float EnableMovementInSeconds;
    public bool IsMovementEnabled;

    /* RayCast Floor Collision Check */
    public int VerticalRaycastCount = 4;
    public int HorizontalRaycastCount = 8;
    public float VerticalRaycastDistance = 0.15f;
    public float HorizontalRaycastDistance = 0.15f;

    /* Variaveis de controle do Gancho */
    public float HookingVelocity;
    public float HookingDeaccelerationRate;
    private HookStateEnum _hookState;

    const float _skinWidth = .015f;
    float _verticalRaycastSpacing;
    float _horizontalRaycastSpacing;
    RaycastOrigin _raycastOrigin;
    BoxCollider2D _boxCollider2D;

    /* Required Components */
    private Rigidbody2D _rigidBody;
    private Hook _hookWeapon;
    private Animator _animatorSprite;
    private SpriteRenderer _mainSprite;
    private AudioSource _audioSource;

    /* Variaveis de memoria Stacked */
    private Vector2 _velocity;
    private Vector2 _jumpForce;

    /* AudioClips */
    public AudioClip AudioFootStep;
    public AudioClip AudioJump;
    public AudioClip AudioDeath;

    /* LastCheck Point */
    public Vector3 LastCheckPoint;

    const string ANIM_IS_RUNNING = "IsRunning";
    const string ANIM_IS_ON_STAIR = "IsOnStair";
    const string ANIM_IS_CLIMBING_STAIR = "IsClimbingStair";
    const string ANIM_IS_ON_FLOOR = "OnFloor";
    const string ANIM_IS_VERTICAL_VELOCITY = "VerticalVelocity";
    const string ANIM_IS_HORIZONTAL_VELOCITY = "HorizontalVelocity";
    const string ANIM_IS_HOOKING = "IsHooking";
    const string ANIM_IS_DEATH = "IsDeath";
    const string ANIM_ON_JUMPING_WALL = "OnJumpingWall";

    public void Awake()
    {
        _velocity = new Vector2();
        _jumpForce = new Vector2();

        _rigidBody = GetComponent<Rigidbody2D>();
        _hookWeapon = GetComponent<Hook>();
        _boxCollider2D = GetComponent<BoxCollider2D>();
        _raycastOrigin = new RaycastOrigin();
        _animatorSprite = GetComponent<Animator>();
        _mainSprite = GetComponent<SpriteRenderer>();
        _audioSource = GetComponent<AudioSource>();

        IsMovementEnabled = true;
        FacingRight = true;

        LastCheckPoint = transform.position;
    }

    public void Update()
    {
        CheckCollision();

        ChangeDirection();

        SetCharacterAnimation();
    }

    public void FixedUpdate()
    {
        SetCharacterState();

        switch (CharacterState)
        {
            case CharacterStateEnum.Idle:
                _rigidBody.drag = 0;
                _rigidBody.gravityScale = 1;
                Move();
                Jump();
                break;
            case CharacterStateEnum.Running:
                _rigidBody.drag = 0;
                _rigidBody.gravityScale = 1;
                Move();
                Jump();
                break;
            case CharacterStateEnum.Jumping:
                _rigidBody.drag = 0;
                _rigidBody.gravityScale = 1;

                switch (_hookState)
                {
                    case HookStateEnum.HookInRange:
                        Move();
                        if (DoJump)
                        {
                            _hookWeapon.DoHooking();
                            DoJump = false;
                        }
                        break;
                    case HookStateEnum.Hooking:

                        _rigidBody.drag = HookingDeaccelerationRate;

                        if (DoJump)
                        {
                            if (_hookWeapon != null)
                            {
                                _hookWeapon.UnDoHooking();
                                DoJump = false;
                            }
                        }
                        else
                        {
                            MoveHooking();
                        }
                        break;
                    default:
                        Move();
                        break;
                }

                break;
            case CharacterStateEnum.JumpingWall:
                _rigidBody.drag = 0;
                _rigidBody.gravityScale = 1;
                MoveJumpingWall();
                JumpWall();
                break;
            case CharacterStateEnum.Climbing:
                _rigidBody.velocity = new Vector3();
                _rigidBody.drag = 0;
                _rigidBody.gravityScale = 0;
                Moveclimbing();
                JumpClimbing();
                break;
            case CharacterStateEnum.Dead:
                _rigidBody.drag = 0;
                _rigidBody.gravityScale = 1;
                break;
        }
    }

    /// <summary>
    /// Metodo responsabel por gerenciar as animacoes do personagem
    /// </summary>
    private void SetCharacterAnimation()
    {
        _animatorSprite.SetBool(ANIM_IS_DEATH, false);
        _animatorSprite.SetBool(ANIM_IS_ON_FLOOR, OnFloor);
        _animatorSprite.SetFloat(ANIM_IS_VERTICAL_VELOCITY, _rigidBody.velocity.y);
        _animatorSprite.SetFloat(ANIM_IS_HORIZONTAL_VELOCITY, _rigidBody.velocity.x);
        _animatorSprite.SetBool(ANIM_IS_HOOKING, false);
        _animatorSprite.SetBool(ANIM_ON_JUMPING_WALL, false);

        switch (CharacterState)
        {
            case CharacterStateEnum.Idle:
                _animatorSprite.SetBool(ANIM_IS_RUNNING, false);
                _animatorSprite.SetBool(ANIM_IS_ON_STAIR, false);
                _animatorSprite.SetBool(ANIM_IS_CLIMBING_STAIR, false);
                break;
            case CharacterStateEnum.Running:
                _animatorSprite.SetBool(ANIM_IS_RUNNING, true);
                break;
            case CharacterStateEnum.Jumping:

                switch (HookState)
                {
                    case HookStateEnum.Hooking:
                        _animatorSprite.SetBool(ANIM_IS_ON_FLOOR, true);
                        _animatorSprite.SetBool(ANIM_IS_HOOKING, true);
                        break;
                    default:
                        _animatorSprite.SetBool(ANIM_IS_RUNNING, false);
                        _animatorSprite.SetBool(ANIM_IS_ON_STAIR, false);
                        _animatorSprite.SetBool(ANIM_IS_CLIMBING_STAIR, false);
                        break;
                }

                break;
            case CharacterStateEnum.Climbing:
                _animatorSprite.SetBool(ANIM_IS_ON_FLOOR, true);
                _animatorSprite.SetBool(ANIM_IS_RUNNING, false);
                _animatorSprite.SetBool(ANIM_IS_ON_STAIR, true);

                if (MovingToY == 1 || MovingToY == -1)
                {
                    _animatorSprite.SetBool(ANIM_IS_CLIMBING_STAIR, true);
                }
                else
                {
                    _animatorSprite.SetBool(ANIM_IS_CLIMBING_STAIR, false);
                }
                break;
            case CharacterStateEnum.Dead:
                _animatorSprite.SetBool(ANIM_IS_DEATH, true);
                break;
            case CharacterStateEnum.JumpingWall:
                _animatorSprite.SetBool(ANIM_IS_ON_FLOOR, true);
                _animatorSprite.SetBool(ANIM_ON_JUMPING_WALL, true);
                break;
        }
    }

    /// <summary>
    /// Metodo responsavel por alterar a direcao do sprite do personagem
    /// </summary>
    private void ChangeDirection()
    {
        if (FacingRight && _rigidBody.velocity.x < -1)
        {
            FacingRight = false;
            _mainSprite.transform.localScale = new Vector3(_mainSprite.transform.localScale.x * -1, _mainSprite.transform.localScale.y, _mainSprite.transform.localScale.z);
        }
        else if (!FacingRight && _rigidBody.velocity.x > 1)
        {
            FacingRight = true;
            _mainSprite.transform.localScale = new Vector3(_mainSprite.transform.localScale.x * -1, _mainSprite.transform.localScale.y, _mainSprite.transform.localScale.z);
        }
    }

    /// <summary>
    /// Metodo responsable por gerenciar o estado do personagem
    /// </summary>
    private void SetCharacterState()
    {
        if (CharacterState != CharacterStateEnum.Dead)
        {
            if (!OnFloor && StartJumping)
            {
                StartJumping = false;
                CharacterState = CharacterStateEnum.Jumping;
                return;
            }

            // Check if the player is on a Stair
            if (OnStair && CharacterState == CharacterStateEnum.Climbing)
            {
                return;
            }
            else if (OnStair && MovingToY == 1)
            {
                CharacterState = CharacterStateEnum.Climbing;

                // Corrige a posicao do personagem em relacao a escada
                transform.position = new Vector3(StairCollider.bounds.center.x, transform.position.y, transform.position.z);
                return;
            }

            if (OnJumpinWall)
            {
                CharacterState = CharacterStateEnum.JumpingWall;
                return;
            }

            if (!OnFloor)
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
    }

    /// <summary>
    /// Metodo responsavel por gerenciar a colisao dos raycasts
    /// </summary>
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

            if (_topRaycast)
                OnTop = true;
        }

        for (int i = 0; i < HorizontalRaycastCount; i++)
        {
            _rightRaycastOrigin = _raycastOrigin.TopRight + (Vector2.down * _horizontalRaycastSpacing * i);
            _leftRaycastOrigin = _raycastOrigin.TopLeft + (Vector2.down * _horizontalRaycastSpacing * i);

            RaycastHit2D _rightRaycast = Physics2D.Raycast(_rightRaycastOrigin, Vector2.right, HorizontalRaycastDistance, FloorMask);
            RaycastHit2D _leftRaycast = Physics2D.Raycast(_leftRaycastOrigin, Vector2.left, HorizontalRaycastDistance, FloorMask);

            if (_rightRaycast)
                OnRight = true;

            if (_leftRaycast)
                OnLeft = true;
        }
    }

    /// <summary>
    /// Metodo responsavel por habilitar o movimento do personagem
    /// </summary>
    /// <param name="seconds">Segundos para habilitar o movimento</param>
    /// <returns></returns>
    public IEnumerator EnableMovement(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        IsMovementEnabled = true;
    }

    /// <summary>
    /// Metodo responsavel por executar o movimento do personagem
    /// </summary>
    private void Move()
    {
        if (IsMovementEnabled)
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
    }

    private void MoveJumpingWall()
    {
        Move();
        transform.Translate(new Vector3(0, -0.5f, 0) * Time.deltaTime);
    }

    /// <summary>
    /// Metodo responsavel por gerenciar o movimento do personagem na corda
    /// </summary>
    private void MoveHooking()
    {
        if (MovingToX != 0)
        {
            _velocity.Set(MovingToX * HookingVelocity, 0);
            _rigidBody.AddForce(_velocity);
        }

        if (MovingToY != 0)
        {
            _hookWeapon.ChangeRopeSize(!(MovingToY > 0));
        }
    }

    /// <summary>
    /// Metodo responsavel por executar o movimento do personagem na escada
    /// </summary>
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

        // Bloqueia o movimento do personagem alem dos limites da escada
        if ((MovingToY == 1 && _boxCollider2D.bounds.max.y >= StairCollider.bounds.max.y) ||
            (MovingToY == -1 && _boxCollider2D.bounds.min.y <= StairCollider.bounds.min.y))
            MovingToY = 0;

        transform.Translate(0, ClimbingYVelocity * MovingToY * Time.deltaTime, 0);
    }

    /// <summary>
    /// Metodo responsavel por executar o pulo do personagem na escada
    /// </summary>
    private void JumpClimbing()
    {
        if (DoJump)
        {
            // Seta o estado do personagem
            OnStair = false;
            DoJump = false;
            StartJumping = true;

            // Adiciona a forca do pulo a partir da escada
            _jumpForce.Set(H_StairJumpForce * MovingToX, JumpForce);
            _rigidBody.AddForce(_jumpForce, ForceMode2D.Impulse);

            PlayJump();
        }
    }

    /// <summary>
    /// Metodo responsavel por executar o pulo do personagem
    /// </summary>
    private void Jump()
    {
        if (DoJump)
        {
            _jumpForce.Set(0, JumpForce);
            _rigidBody.AddForce(_jumpForce, ForceMode2D.Impulse);
            DoJump = false;
            StartJumping = true;
            PlayJump();
        }
    }

    /// <summary>
    /// Metodo responsavel por executar o pulo do personagem na parede
    /// </summary>
    private void JumpWall()
    {
        if (DoJump && MovingToX != 0)
        {
            IsMovementEnabled = false;
            StartCoroutine(EnableMovement(EnableMovementInSeconds));
            _jumpForce.Set(H_StairJumpForce * (MovingToX * -1), JumpForce);
            _rigidBody.AddForce(_jumpForce, ForceMode2D.Impulse);
            DoJump = false;
            StartJumping = true;
            PlayJump();
        }
    }

    /// <summary>
    /// Metodo responsavel por gerenciar a morte do personagem
    /// </summary>
    public void DoDie()
    {
        _hookWeapon.UnDoHooking();
        CharacterState = CharacterStateEnum.Dead;
		_audioSource.Stop ();
		_audioSource.clip = AudioDeath;
		_audioSource.Play ();
	}

    /// <summary>
    /// Metodo responsavel por gerenciar o respawn do personagem
    /// </summary>
    public void Respawn()
    {
        CharacterState = CharacterStateEnum.Idle;
        transform.position = LastCheckPoint;
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == Constants.TAG_JUMPING_WALL)
        {
            OnJumpinWall = true;
        }
    }

    public void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.tag == Constants.TAG_JUMPING_WALL)
        {
            OnJumpinWall = false;
        }
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == Constants.TAG_DEATH_TRAP)
        {
            DoDie();
        }

        if (collision.tag == Constants.TAG_CHECK_POINT)
        {
			LastCheckPoint = new Vector3(collision.transform.position.x, collision.transform.position.y, transform.position.z);
        }

		if (collision.tag == Constants.TAG_ENDGAME) {
			Application.LoadLevel("CreditsScene");
		}
    }

    public void OnTriggerStay2D(Collider2D collision)
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

    public HookStateEnum HookState
    {
        get
        {
            return _hookState;
        }
        set
        {
            _hookState = value;
        }
    }

    public Rigidbody2D HookableBody
    {
        get
        {
            return _rigidBody;
        }
    }

    public float HookVelocity
    {
        get
        {
            return HookingVelocity;
        }
        set
        {
            HookingVelocity = value;
        }
    }

    #region AudioManager

    public void PlayFootStep()
    {
        _audioSource.Stop();
        _audioSource.clip = AudioFootStep;
        _audioSource.Play();
    }

    public void PlayJump()
    {
        _audioSource.Stop();
        _audioSource.clip = AudioJump;
        _audioSource.Play();
    }

    #endregion
}

public enum CharacterStateEnum
{
    Idle,
    Running,
    Jumping,
    JumpingWall,
    Climbing,
    Dead,
}