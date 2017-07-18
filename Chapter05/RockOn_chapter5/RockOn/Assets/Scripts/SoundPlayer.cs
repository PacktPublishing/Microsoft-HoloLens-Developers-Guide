using UnityEngine;

public class SoundPlayer : MonoBehaviour
{
    public void PlaySounds()
    {
        var audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            return;

        audioSource.Play();
    }
}