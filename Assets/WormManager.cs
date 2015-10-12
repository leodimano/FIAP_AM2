using UnityEngine;
using System.Collections;

public class WormManager : MonoBehaviour
{

    Worm[] _worms;

    // Use this for initialization
    void Start()
    {
        _worms = GetComponentsInChildren<Worm>();

        ChangeChildrenState(false);
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        ChangeChildrenState(true);
    }

    public void OnTriggerExit2D(Collider2D collision)
    {
        ChangeChildrenState(false);
    }

    private void ChangeChildrenState(bool state_)
    {
        for (int i = 0; i <= _worms.Length - 1; i++)
        {
            _worms[i].gameObject.SetActive(state_);
        }
    }
}
