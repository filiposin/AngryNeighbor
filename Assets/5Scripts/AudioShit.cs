using UnityEngine;

public class AudioShit : MonoBehaviour
{
    public AudioSource source;
    public void CallAudio(AudioClip clip) => source.PlayOneShot(clip);
}
