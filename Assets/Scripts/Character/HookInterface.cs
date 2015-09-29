using UnityEngine;
using System.Collections;

public interface HookableInterface
{
    float HookVelocity { get; set; }

    HookStateEnum HookState { get; set; }

    Rigidbody2D HookableBody { get; }
}

public enum HookStateEnum
{
    NotHooking,
    HookInRange,
    Hooking
}



