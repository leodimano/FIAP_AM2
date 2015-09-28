using UnityEngine;
using System.Collections;

public interface HookableInterface
{
    HookStateEnum HookState { get; set; }

    Rigidbody2D HookableBody { get; }
}

public enum HookStateEnum
{
    NotHooking,
    HookInRange,
    Hooking
}



