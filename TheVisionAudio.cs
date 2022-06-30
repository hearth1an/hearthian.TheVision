using UnityEngine;
using OWML.Common;
using OWML.ModHelper;


namespace TheVision.AudioOneShot
{
    public class AudioTheVision : MonoBehaviour
    {
        public AudioSource source;
        public AudioClip clip;

        public void PlayAudio()
        {

            {
                clip = Resources.Load<AudioClip>("quantum_collapse");
                source.PlayOneShot(clip);                
                
            }
        }
    }
}
