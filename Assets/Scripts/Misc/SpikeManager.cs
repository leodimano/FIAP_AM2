using UnityEngine;
using System.Collections;

public class SpikeManager : MonoBehaviour
{
    Spike[] _spikes = new Spike[10];

    // Use this for initialization
    void Start()
    {
        _spikes = GetComponentsInChildren<Spike>();
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        for (int i = 0; i <= _spikes.Length - 1; i++)
        {
            _spikes[i].enabled = true;
        }
    }

    public void OnTriggerExit2D(Collider2D collision)
    {
        for (int i = 0; i <= _spikes.Length - 1; i++)
        {
            _spikes[i].enabled = false;
        }
    }

}

