using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Character))]
public class InputController : MonoBehaviour
{
    private Character CharacterObject;

    public void Awake()
    {
        CharacterObject = GetComponent<Character>();
    }

    // Update is called once per frame
    void Update()
    {
        CharacterObject.MovingToX = Input.GetAxisRaw(Constants.INPUT_HORIZONTAL);
        CharacterObject.MovingToY = Input.GetAxisRaw(Constants.INPUT_VERTICAL);
        CharacterObject.DoJump = Input.GetButtonDown(Constants.INPUT_JUMP);
    }

}
