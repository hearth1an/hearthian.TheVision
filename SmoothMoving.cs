using UnityEngine;
using System.Collections;
using NewHorizons.Utility;

namespace TheVision
{
    public class SmoothMoving : MonoBehaviour
    {
        public Vector3 targetPos;
        public float speed = 0.5f;
        bool isActive = false;
        public float delay;
        public float rotationSpeed;
        public OWAudioSource PlayerHeadsetAudioSource;
        public void Start()
        {
            Invoke("Init", delay);
            Invoke("onDestroying", 18.6f);
            Invoke("Destroying", 19f);
        }
        public void onDestroying()
        {
            PlayerHeadsetAudioSource = Locator.GetPlayerTransform().gameObject.AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.ToolFlashlightFlicker);
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 0.3f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.PlayOneShot();

            var effect = Locator.GetActiveCamera().transform.Find("ScreenEffects/LightFlickerEffectBubble").GetComponent<LightFlickerController>();
            effect.FlickerOffAndOn(offDuration: 0.5f, onDuration: 0.3f);
        }
        public void Destroying()
        {
            PlayerHeadsetAudioSource = Locator.GetPlayerTransform().gameObject.AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.EyeGalaxyBlowAway);
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 0.2f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.PlayOneShot();
            Destroy(gameObject);
        }
        public void toySFX()
        {
            PlayerHeadsetAudioSource = gameObject.AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.loop = false;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.ToolItemSharedStoneDrop);
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 0.2f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.PlayOneShot();
        }
        public void Init()
        {
            isActive = true;
            toySFX();
        }
        void Update()
        {
            if (transform.localPosition != targetPos && isActive == true)
            {
                transform.Rotate(new Vector3(rotationSpeed, rotationSpeed, 0) * Time.deltaTime);
                transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, speed * Time.deltaTime);
            }
        }
    }
}