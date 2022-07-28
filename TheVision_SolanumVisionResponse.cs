using UnityEngine;
using TheVision.Utilities.ModAPIs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewHorizons.External.Modules;
using NewHorizons.Utility;
using OWML.Common;
using NewHorizons.Builder.Atmosphere;
using System.Collections;


namespace TheVision.CustomProps
{
    public class TheVision_SolanumVisionResponse : MonoBehaviour
    {
        public NomaiConversationManager _nomaiConversationManager;
        public SolanumAnimController _solanumAnimController;
        public NomaiWallText solanumVisionResponse;
        public OWAudioSource PlayerHeadsetAudioSource;


        private static readonly int MAX_WAIT_FRAMES = 20;
        private int waitFrames = 0;
        private bool visionEnded = false;
        private bool doneHijacking = false;
        private bool hasStartedWriting = false;


        public void Update()
        {
            if (!visionEnded) return;
            if (doneHijacking) return;
            if (waitFrames > 0) { waitFrames--; return; }

            if (!hasStartedWriting)
            {
                NomaiWallText responseText = Locator.GetAstroObject(AstroObject.Name.QuantumMoon).transform.Find("Sector_QuantumMoon/State_EYE/QMResponseText").GetComponent<NomaiWallText>();

                // one-time code that runs after waitFrames are up
                _solanumAnimController.OnWriteResponse += (int unused) => responseText.Show();
                _solanumAnimController.StartWritingMessage();
                hasStartedWriting = true;

            }

            if (!_solanumAnimController.isStartingWrite && !solanumVisionResponse.IsAnimationPlaying())
            {
                // drawing custom text                
                // var customResponse = Locator.GetAstroObject(AstroObject.Name.QuantumMoon).transform.Find("Sector_QuantumMoon/State_EYE/QMResponseText");
                // customResponse.GetAddComponent<NomaiWallText>().Show();

                _solanumAnimController.StopWritingMessage(gestureToText: false);
                _nomaiConversationManager._state = NomaiConversationManager.State.WatchingSky;
                _solanumAnimController.StopWatchingPlayer();
                doneHijacking = true;

                // Spawning SolanumCopies and Signals on vision response
                TheVision.Instance.ModHelper.Events.Unity.FireInNUpdates(TheVision.Instance.SpawnOnVisionEnd, 10);
            }
        }

        public void OnVisionStart()
        {
            if (_nomaiConversationManager._activeResponseText != null) _nomaiConversationManager._activeResponseText.Hide();
            _nomaiConversationManager._activeResponseText = null;
            _nomaiConversationManager._pendingResponseText = null;
        }

        public void OnVisionEnd()
        {
            // sfx             
            PlayWindSound();
            PlayStartSound();
            PlayEnergySound();
            PlayFadeInSound();

            // wh parameters
            var whiteHoleOptions = Locator.GetAstroObject(AstroObject.Name.QuantumMoon).transform.Find("Sector_QuantumMoon/WhiteHole/AmbientLight").GetComponent<Light>();
            whiteHoleOptions.color = new Color(1, 1, 2, 1);
            whiteHoleOptions.range = 30;
            whiteHoleOptions.intensity = 3;
            whiteHoleOptions.enabled = true;

            var qmWhiteHole = Locator.GetAstroObject(AstroObject.Name.QuantumMoon).transform.Find("Sector_QuantumMoon/WhiteHole").gameObject;
            qmWhiteHole.SetActive(true);

            /*
            var cameraFixedPosition = Locator.GetPlayerTransform().gameObject.GetComponent<PlayerLockOnTargeting>();
            cameraFixedPosition.LockOn(qmWhiteHole.transform, 1, false, 5);
            cameraFixedPosition.BreakLock(10f);
            */

            // Camera shaking amount
            var cameraShake = Locator.GetPlayerTransform().gameObject.AddComponent<CameraShake>();
            Vector3 orignalPosition = transform.position;
            StartCoroutine(cameraShake.Shake(5.5f, 0.05f));
            transform.position = orignalPosition;


            // var playerCrash = Locator.GetPlayerTransform().gameObject.GetComponent<PlayerCrushedController>();
            // playerCrash.CrushPlayer()


            //GameObject lightningGO = Locator.GetAstroObject(AstroObject.Name.GiantsDeep).transform.Find("Sector_GD/Clouds_GD/LightningGenerator_GD/LightningGenerator_GD_CloudLightningInstance").InstantiateInactive();
            //lightningGO.

            // actually adding working lightninng but none of them is showing
            // var lightning = Locator.GetAstroObject(AstroObject.Name.QuantumMoon).transform.Find("Sector_QuantumMoon/State_EYE/Clouds_QM_EyeState/Effects_QM_EyeVortex/EyeVortex_Cloudlayer_Interior").gameObject.AddComponent<CloudLightningGenerator>();                        
            // Vector3 localVector = new Vector3(4.2105f, -0.1138f, 1.9017f);
            // lightning.SpawnLightning(localVector);

            /* GameObject lightning = Locator.GetAstroObject(AstroObject.Name.GiantsDeep).transform.Find("Sector_GD/Clouds_GD/LightningGenerator_GD").gameObject;
            GameObject QMsphere = Locator.GetAstroObject(AstroObject.Name.QuantumMoon).transform.Find("Atmosphere_QM/FogSphere").gameObject;
            var lightnings = lightning.InstantiateInactive();
            lightnings.transform.parent = QMsphere.transform;
            lightnings.transform.position = QMsphere.transform.position;

            var lightningGenerator = lightnings.GetComponent<CloudLightningGenerator>();

            lightnings.SetActive(true);
            // lightningGenerator._audioSector = _vortexAudio; */

            TheVision.Instance.ModHelper.Console.WriteLine("PROJECTION COMPLETE");
            _nomaiConversationManager.enabled = false;
            visionEnded = true;
            waitFrames = MAX_WAIT_FRAMES;

            // flicker 
            var effect = Locator.GetPlayerCamera().transform.Find("ScreenEffects/LightFlickerEffectBubble").GetComponent<LightFlickerController>();
            effect.FlickerOffAndOn(offDuration: 6f, onDuration: 1f);
        }

        public void PlayWindSound()
        {
            // SFX on QM after Solanumptojection
            PlayerHeadsetAudioSource = Locator.GetAstroObject(AstroObject.Name.QuantumMoon).transform.Find("Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/Character_NOM_Solanum").gameObject.AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.EyeVortex_LP); // shattering sound 2428 //2697 - station flicker // 2252 - EyeVortex_LP wind // 2005 - electric core
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 0.5f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.Play();
        }
        public void PlayFadeInSound()
        {
            PlayerHeadsetAudioSource = Locator.GetAstroObject(AstroObject.Name.QuantumMoon).transform.Find("Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/NomaiConversation").gameObject.AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.MemoryUplink_Start); // shattering sound 2428 //2697 - station flicker // 2252 -wind // 2005 - electric core // 2460 MemoryUplink_Start
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 5f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.PlayDelayed(4.5f);

        }
        public void PlayEnergySound()
        {
            PlayerHeadsetAudioSource = Locator.GetPlayerTransform().gameObject.AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.ToolFlashlightFlicker); ; // StationFlicker_RW = 2696// 2005 - electric core //502 -ToolFlashlightFlicker
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 6f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.PlayOneShot();
        }


        public void PlayStartSound()
        {
            PlayerHeadsetAudioSource = Locator.GetAstroObject(AstroObject.Name.QuantumMoon).transform.Find("Sector_QuantumMoon/State_EYE").gameObject.AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.GD_IslandLiftedByTornado); // shattering sound 2428 //2697 - station flicker // 2252 -wind // 2005 - electric core // 2011 - GD_IslandLiftedByTornado
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 5f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.PlayOneShot();
        }
    }
}




// hijacking Solanum's conversation controller:

//         // under NomaiConversationManager
// _activeResponseText.Show();
// nomaiConversationManager.enabled = false;
//_solanumAnimController.StartWritingMessage();
//         // then every frame,
//if (!_solanumAnimController.isStartingWrite && !_activeResponseText.IsAnimationPlaying())
//{
//	_solanumAnimController.StopWritingMessage(gestureToText: true);
//  _solanumAnimController.StopWatchingPlayer();
//}

