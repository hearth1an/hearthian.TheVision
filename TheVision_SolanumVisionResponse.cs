using UnityEngine;
using TheVision.Utilities.ModAPIs;




namespace TheVision.CustomProps
{
    class TheVision_SolanumVisionResponse : MonoBehaviour



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
        

        void Update()
        {
            if (!visionEnded) return;
            if (doneHijacking) return;
            if (waitFrames > 0) { waitFrames--; return; }

            if (!hasStartedWriting)
            {
                // one-time code that runs after waitFrames are up
                _solanumAnimController.OnWriteResponse += (int unused) => solanumVisionResponse.Show();
			    _solanumAnimController.StartWritingMessage();
                hasStartedWriting = true;
            }

            if (!_solanumAnimController.isStartingWrite && !solanumVisionResponse.IsAnimationPlaying())
            {
                _solanumAnimController.StopWritingMessage(gestureToText: false);
                _solanumAnimController.StopWatchingPlayer();
                doneHijacking = true;


                // Spawning SolanumCopies and Signals on vision response
                TheVision.Instance.ModHelper.Events.Unity.FireInNUpdates(
          () => TheVision.Instance.SpawnSolanumCopy(TheVision.Instance.ModHelper.Interaction.GetModApi<INewHorizons>("xen.NewHorizons")), 2000);
                TheVision.Instance.SpawnSignals();

                
            }

        }

        public void OnVisionEnd()

        {
            // SFX on QM after Solanumptojection
            PlayerHeadsetAudioSource = GameObject.Find("Player_Body").AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip((AudioType)2401);
            PlayerHeadsetAudioSource.Play();

            

            TheVision.Instance.ModHelper.Console.WriteLine("PROJECTION COMPLETE");
            _nomaiConversationManager.enabled = false;
            visionEnded = true;
            waitFrames = MAX_WAIT_FRAMES;
           

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
}
