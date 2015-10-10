using UnityEngine;
using System.Collections;

public class BroadCastCollision : MonoBehaviour
{
    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == Constants.TAG_PLAYER &&
            (tag == Constants.TAG_SPIKE_ACTIVATOR || tag == Constants.TAG_SPIKE_DEACTIVATOR))
        {
            transform.parent.SendMessage("HandleSpikeActivators", tag == Constants.TAG_SPIKE_ACTIVATOR);
        }
    }
}
