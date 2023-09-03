using UnityEngine;

public class AmbiencePlayer : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip ambience;

    private void Start()
    {
        PlayAmbientNoise();
    }

    void PlayAmbientNoise()
    {
        if (audioSource != null && ambience != null)
        {
            audioSource.clip = ambience;
            audioSource.loop = true; // Set the audio to loop
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("AudioSource or Ambience AudioClip not assigned!");
        }
    }
}
