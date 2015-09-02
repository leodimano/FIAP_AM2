using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Character))]
public class InputController : MonoBehaviour
{
    private Character CharacterObject;

    private const string INPUT_HORIZONTAL = "Horizontal";
    private const string INPUT_JUMP = "Jump";

    public void Awake()
    {
        CharacterObject = GetComponent<Character>();
    }

    // Update is called once per frame
    void Update()
    {
        CharacterObject.MovingToX = Input.GetAxisRaw(INPUT_HORIZONTAL);
        CharacterObject.DoJump = Input.GetButtonDown(INPUT_JUMP);
    }

}
