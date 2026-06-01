using UnityEngine;

public class CallSoundFrom : MonoBehaviour
{
    public Transform soundTransform;
    public void CallSound()
    {
        if(soundTransform==null) soundTransform = this.transform;
        AISoundManager.MakeSound(soundTransform.position, 35f);
    }
}
