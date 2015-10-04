using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class GameController : MonoBehaviour
{
    public AudioClip BackgroundSound;
    public float BackgroundVolume;
    public float EffectVolume;

    private AudioSource _backgroundVolumeAudioSource;

    private Spike[] _spikesList;

    public void Awake()
    {
        _backgroundVolumeAudioSource = GetComponent<AudioSource>();
        _backgroundVolumeAudioSource.Stop();
        _backgroundVolumeAudioSource.clip = BackgroundSound;
        _backgroundVolumeAudioSource.loop = true;
        _backgroundVolumeAudioSource.volume = BackgroundVolume;
        _backgroundVolumeAudioSource.Play();

        _spikesList = FindObjectsOfType<Spike>();
    }

    public void Update()
    {
        _backgroundVolumeAudioSource.volume = BackgroundVolume;
    }

    public void HandleSpikeActivators(bool activate_)
    {
        for (int _index = 0; _index <= _spikesList.Length - 1; _index++)
        {
            _spikesList[_index].enabled = activate_;
        }
    }
}
