using UnityEngine;
using System.Collections;

public class MainScene : MonoBehaviour
{

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.anyKey)
        {
            StartCoroutine(StartGameScene());
        }
    }


    IEnumerator StartGameScene()
    {
        yield return new WaitForSeconds(1);
        Application.LoadLevel("GameScene");
    }
}
