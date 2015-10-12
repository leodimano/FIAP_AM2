using UnityEngine;
using System.Collections;

public class Worm : MonoBehaviour
{
    public GameObject Projectile;
    public float ProjectileOffset;
    public float AttackCoolDownSeconds;

    private float _attackTime;
    private Animator _animator;
    private Light _wormLight;

    private const string ANIM_IS_ATTACKING = "IsAttacking";

    public void Awake()
    {
        _animator = GetComponent<Animator>();
        _attackTime = 0;
    }

    void Update()
    {
        if (_attackTime >= AttackCoolDownSeconds)
        {
            if (_animator != null)
                _animator.SetBool(ANIM_IS_ATTACKING, true);

            _attackTime = 0;
        }
        else
        {
            _attackTime += Time.deltaTime;
        }
    }

    public void Attack()
    {
        Instantiate(Projectile, transform.position + (Vector3.up * ProjectileOffset), transform.rotation);
        _animator.SetBool(ANIM_IS_ATTACKING, false);
    }
}
