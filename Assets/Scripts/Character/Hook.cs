using UnityEngine;
using System.Collections;

[RequireComponent(typeof(DistanceJoint2D))]
public class Hook : MonoBehaviour
{
    public Sprite HookSprite;
    public Sprite HookRopeSprite;
    public float HookRangeRadius;
    public float GrowRopeBy;

    public HookableInterface HookableObject;

    private Collider2D _hookHangerCollider;

    private DistanceJoint2D _hookRopeJoint;

    private GameObject _hookGameObject;
    private GameObject _hookRopeGameObject;

    private int HookHangerLayerMask;

    private const string HOOK_GO_NAME = "HookGO";
    private const string HOOK_ROPE_GO_NAME = "HookRopeGO";



    public void Awake()
    {
        _hookRopeJoint = GetComponent<DistanceJoint2D>();
        HookableObject = GetComponent<HookableInterface>();
        HookHangerLayerMask = LayerMask.GetMask("HookHanger");
    }

    public void Update()
    {
        if (_hookGameObject != null)
        {
            ManageHookSprites(true);
        }
    }

    public void FixedUpdate()
    {
        if (HookableObject.HookState == HookStateEnum.NotHooking || HookableObject.HookState == HookStateEnum.HookInRange)
        {
            Collider2D _hookCollider = Physics2D.OverlapCircle(transform.position, HookRangeRadius, HookHangerLayerMask);

            if (_hookCollider != null && _hookCollider.tag == Constants.TAG_HOOK_HANGER)
            {
                switch (HookableObject.HookState)
                {
                    case HookStateEnum.Hooking:
                        break;
                    case HookStateEnum.HookInRange:
                    case HookStateEnum.NotHooking:
                        HookableObject.HookState = HookStateEnum.HookInRange;
                        _hookHangerCollider = _hookCollider;
                        break;
                }
            }
            else
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

    public void DoHooking()
    {
        if (_hookHangerCollider != null)
        {
            Vector2 _colliderPosition = new Vector2(_hookHangerCollider.bounds.center.x, _hookHangerCollider.bounds.center.y);
            float _distance = Mathf.Abs(Vector2.Distance(transform.position, _colliderPosition));
            _hookRopeJoint.connectedAnchor = _colliderPosition;
            _hookRopeJoint.enabled = true;
            _hookRopeJoint.distance = _distance;
            HookableObject.HookState = HookStateEnum.Hooking;
            ManageHookSprites(true);
        }
    }

    public void ChangeRopeSize(bool GrowOrShrink)
    {
        if (HookableObject.HookState == HookStateEnum.Hooking)
        {
            if (GrowOrShrink)
            {
                _hookRopeJoint.distance += GrowRopeBy * Time.deltaTime;
            }
            else
            {
                _hookRopeJoint.distance -= GrowRopeBy * Time.deltaTime;
            }
        }
    }

    public void UnDoHooking()
    {
        _hookRopeJoint.enabled = false;
        _hookHangerCollider = null;
        HookableObject.HookState = HookStateEnum.NotHooking;
        ManageHookSprites(false);
    }

    private void ManageHookSprites(bool draw)
    {
        if (draw)
        {
            if (_hookGameObject == null)
            {
                _hookGameObject = new GameObject();
                _hookGameObject.name = HOOK_GO_NAME;
                _hookGameObject.transform.position = _hookHangerCollider.bounds.center;

                SpriteRenderer _hookSpriteRenderer = _hookGameObject.AddComponent<SpriteRenderer>();
                _hookSpriteRenderer.sprite = HookSprite;

                _hookRopeGameObject = new GameObject();
                _hookRopeGameObject.name = HOOK_ROPE_GO_NAME;
                _hookRopeGameObject.transform.position = _hookHangerCollider.bounds.center;

                SpriteRenderer _hookRopeSpriteRenderer = _hookRopeGameObject.AddComponent<SpriteRenderer>();
                _hookRopeSpriteRenderer.sprite = HookRopeSprite;
            }
            else
            {
                Vector3 _distance = _hookHangerCollider.bounds.center - transform.position;
                float hookerAngle = (Mathf.Atan2(_distance.x, _distance.y) * Mathf.Rad2Deg) * -1;

                _hookGameObject.transform.rotation = Quaternion.AngleAxis(hookerAngle, Vector3.forward);
                _hookRopeGameObject.transform.rotation = Quaternion.AngleAxis(hookerAngle, Vector3.forward);

                float _tileDistance = (_distance.magnitude);
                _hookRopeGameObject.transform.localScale = new Vector3(1, _tileDistance, 1);
            }
        }
        else
        {
            Destroy(_hookGameObject);
            Destroy(_hookRopeGameObject);
        }
    }
}
