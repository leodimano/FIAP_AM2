using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(DistanceJoint2D))]
public class Hook : MonoBehaviour
{
    public HookableInterface HookableObject;

    private Collider2D _hookHangerCollider;

    private CircleCollider2D _hookCollider;
    private DistanceJoint2D _hookRopeJoint;

    public void Awake()
    {
        _hookCollider = GetComponent<CircleCollider2D>();
        _hookRopeJoint = GetComponent<DistanceJoint2D>();
        HookableObject = GetComponent<HookableInterface>();
    }

    public void DoHooking()
    {
        _hookRopeJoint.connectedAnchor = new Vector2(_hookHangerCollider.transform.position.x, _hookHangerCollider.transform.position.y);
        _hookRopeJoint.enabled = true;
        HookableObject.HookState = HookStateEnum.Hooking;
    }

    public void UnDoHooking()
    {
        _hookRopeJoint.enabled = false;
        _hookHangerCollider = null;
        HookableObject.HookState = HookStateEnum.NotHooking;
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == Constants.TAG_HOOK_HANGER)
        {
            switch (HookableObject.HookState)
            {
                case HookStateEnum.Hooking:
                    break;
                case HookStateEnum.HookInRange:
                case HookStateEnum.NotHooking:
                    HookableObject.HookState = HookStateEnum.HookInRange;
                    _hookHangerCollider = collision;
                    break;
            }
        }
    }

    public void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.tag == Constants.TAG_HOOK_HANGER)
        {
            switch (HookableObject.HookState)
            {
                case HookStateEnum.Hooking:
                    break;
                case HookStateEnum.HookInRange:
                case HookStateEnum.NotHooking:
                    HookableObject.HookState = HookStateEnum.HookInRange;
                    _hookHangerCollider = null;
                    break;
            }
        }
    }
}
