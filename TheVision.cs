using OWML.Common;
using OWML.ModHelper;
using UnityEngine;
using TheVision.Utilities.ModAPIs;
using static NewHorizons.External.Modules.PropModule;
using NewHorizons.Builder.Props;
using System.Linq;
using TheVision.CustomProps;
using HarmonyLib;
using System.Reflection;
using NewHorizons.Utility;
using System.Threading.Tasks;
using System.Threading;
using OWML.Utils;

namespace TheVision
{
    public class TheVision : ModBehaviour
    {
        public static INewHorizons newHorizonsAPI;
        public static TheVision Instance;

        public OWAudioSource PlayerHeadsetAudioSource;


        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

            var newHorizonsAPI = ModHelper.Interaction.GetModApi<INewHorizons>("xen.NewHorizons");
            newHorizonsAPI.GetStarSystemLoadedEvent().AddListener(OnStarSystemLoaded);
            newHorizonsAPI.LoadConfigs(this);


            ModHelper.Console.WriteLine($"{nameof(TheVision)} is loaded!", MessageType.Success);

            LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
            {
                if (loadScene != OWScene.SolarSystem) return;
                var playerBody = FindObjectOfType<PlayerBody>();
                ModHelper.Console.WriteLine($"Found player body, and it's called {playerBody.name}!",
                MessageType.Success);

            };

        }

        private static void SpawnStartProps()
        {

            GameObject visionTarget = GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon/VisionStaffDetector");

            visionTarget.transform.parent =
                GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon")
                .GetComponentsInChildren<Transform>(true)
                .Where(t => t.gameObject.name == "State_EYE")
                .First(); // All because Find doesn't work on inactive game objects :/


            //parenting particles to Solanum
            var QMparticles = GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon/Effects_NOM_WarpParticles");
            QMparticles.transform.parent = GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot").transform.Find("Character_NOM_Solanum");

            //renaming TH recorder idk why but I needed it for some reason
            var THrecorder = GameObject.Find("TimberHearth_Body/Sector_TH/Prefab_NOM_Recorder(Clone)");
            THrecorder.transform.name = "Prefab_NOM_Recorder(Clone)_TH";

            // Making custom text for reply           
            NomaiWallText responseText = GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon/NomaiWallText").GetComponent<NomaiWallText>();
            responseText.HideTextOnStart();
            responseText.transform.parent = GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon").transform.Find("State_EYE");

            //parenting QM ground text to TH state
            var QMgroundText = GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon/NomaiWallText");
            QMgroundText.transform.parent = GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon").transform.Find("State_TH");

            var nomaiConversationManager = Resources.FindObjectsOfTypeAll<NomaiConversationManager>().First(); //GameObject.FindObjectOfType<NomaiConversationManager>();
            var myConversationManager = nomaiConversationManager.gameObject.AddComponent<TheVision_SolanumVisionResponse>();
            myConversationManager._nomaiConversationManager = nomaiConversationManager;
            myConversationManager._solanumAnimController = nomaiConversationManager._solanumAnimController;
            myConversationManager.solanumVisionResponse = responseText;

            visionTarget.GetComponent<VisionTorchTarget>().onSlidesComplete = myConversationManager.OnVisionEnd;

            // Replacing new Hologram
            var origHologram = GameObject.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/VesselHologram_EyeSignal");
            var hologramClone = GameObject.Instantiate(origHologram);
            hologramClone.transform.parent = GameObject.Find("DB_VesselDimension_Body/Sector_VesselDimension").transform.Find("Sector_VesselBridge");
            hologramClone.transform.position = origHologram.transform.position;
            hologramClone.transform.rotation = origHologram.transform.rotation;
            var mat = hologramClone.GetComponent<MeshRenderer>().material;
            mat.SetTexture("_MainTex", TheVision.Instance.ModHelper.Assets.GetTexture("images/NewHologram.png"));
            hologramClone.GetComponent<MeshRenderer>().sharedMaterial = mat;
            hologramClone.SetActive(false);

            // I don't like it. It's better to place vision torch here
            GameObject.Find("RingWorld_Body/Sector_RingWorld/Sector_SecretEntrance/Props_SecretEntrance/OtherComponentsGroup/Props_IP_WrenchStaff").SetActive(false);

            // Disabling WH on QM on the start
            GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon/WhiteHole").SetActive(false);            

            //setting green color for this one
            var GDcomputerColor = GameObject.Find("GiantsDeep_Body/Sector_GD/Prefab_NOM_Computer(Clone)/PointLight_NOM_Computer").GetComponent<Light>();           
            GDcomputerColor.color = new Color { r = 0, g = 2, b = 1 };

            //setting red color for this one
            var ATPcomputerColor = SearchUtilities.Find("TimeLoopRing_Body/Interactibles_TimeLoopRing_Hidden/Prefab_NOM_Computer/PointLight_NOM_Computer").GetComponent<Light>();
            ATPcomputerColor.color = new Color { r = 1, g = 0, b = 0 };

            //parenting QM signal to Solanum (otherwise it will be heard on every QM sector)
            var QMsignal = GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon/Signal_Solanum");
            QMsignal.transform.parent = GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot").transform.Find("Character_NOM_Solanum");
                        
        }

        // Load StartProps and DisabledPropsOnStart
        public void OnStarSystemLoaded(string systemName)
        {
            ModHelper.Console.WriteLine("LOADED SYSTEM " + systemName);

            if (systemName == "SolarSystem")
            {
                SpawnStartProps();               
                DisabledPropsOnStart(false);
            }
            if (systemName == "GloamingGalaxy")
            {
                EndGame();
            }
        }

        // Async func to teleport ship to TH State on QM so player can continue the journey
        public static async Task TeleportShip()
        {
            var qmStateTH = GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon/State_TH");            
            ShipDamageController s_dmg = Locator.GetShipBody().GetComponent<ShipDamageController>();
            s_dmg.ToggleInvincibility();            

            while (qmStateTH.activeSelf == false)
            {
                await Task.Delay(5000);
                TheVision.Instance.ModHelper.Console.WriteLine("Ready to teleport ship!");
                await Task.Yield();
            };

            while (qmStateTH.activeSelf != false)
            {
                OWRigidbody qm_rb = Locator.GetAstroObject(AstroObject.Name.QuantumMoon).GetComponent<OWRigidbody>();
                OWRigidbody s_rb = Locator.GetShipBody();

                Vector3 newPosition = qm_rb.transform.TransformPoint(new Vector3(30f, -100f, 0f));
                s_rb.SetPosition(newPosition);
                s_rb.SetRotation(Quaternion.LookRotation(qm_rb.transform.forward, -qm_rb.transform.up));
                s_rb.SetVelocity(qm_rb.GetPointVelocity(newPosition));
                s_rb.SetAngularVelocity(qm_rb.GetAngularVelocity());

                await Task.Yield();

                TheVision.Instance.ModHelper.Console.WriteLine("Ship teleported!");

                // TheVision.CustomProps.PlayStartSound(false);
                break; // or it will teleport it forever
            }
        }       
        
        
       

        public void SpawnOnVisionEnd()
        {
            // Playing SFX
            PlayRevealSound();
            PlaySFXSound();
            PlayGaspSound();
            PlayThunderSound();

            // Disabling QM WhiteHole
            GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon/WhiteHole").SetActive(false);

            // Enabling json props
            DisabledPropsOnStart(true);

            //decloaking QM on signals spawn
            GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE/Clouds_QM_EyeState").SetActive(false);
            GameObject.Find("QuantumMoon_Body/Atmosphere_QM/FogSphere").SetActive(false);                       

            //placing orb on GD to the slot (1)
            var nomaiSlot = SearchUtilities.Find("GiantsDeep_Body/Sector_GD/Sector_GDInterior/Sector_GDCore/Sector_Module_Sunken/Interactables_Module_Sunken/OrbInterface/Slots/Slot (1)");
            var nomaiInterfaceOrb = SearchUtilities.Find("GiantsDeep_Body/Sector_GD/Sector_GDInterior/Sector_GDCore/Sector_Module_Sunken/Interactables_Module_Sunken/OrbInterface/Prefab_NOM_InterfaceOrb");
            var nomaiCorrectSlot = nomaiInterfaceOrb.GetComponent<NomaiInterfaceOrb>();
            var nomaiCorrectSlot2 = nomaiCorrectSlot.GetComponent<OWRigidbody>();
            nomaiCorrectSlot.SetOrbPosition(nomaiSlot.transform.position);
            nomaiCorrectSlot._orbBody.ChangeSuspensionBody(nomaiCorrectSlot2);            

            //disabling recorder on QM Solanum shuttle
            GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon/QuantumShuttle/Prefab_NOM_Shuttle/Sector_NomaiShuttleInterior/Interactibles_NomaiShuttleInterior/Prefab_NOM_Recorder").SetActive(false);

            // Disabling common computer on GD, placing the right one to correct position
            var GDcommonComputer = GameObject.Find("GiantsDeep_Body/Sector_GD/Sector_GDInterior/Sector_GDCore/Sector_Module_Sunken/Interactables_Module_Sunken/Computers/ComputerPivot (1)");
            GDcommonComputer.SetActive(false);
            GameObject.Find("GiantsDeep_Body/Sector_GD/Prefab_NOM_Computer(Clone)").transform.rotation = GDcommonComputer.transform.rotation;

            TeleportShip();

            // Deactivating it so it will be no sound or flickers
            SearchUtilities.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE/VisionStaffDetector").SetActive(false);

            // Disabling music on QM
            SearchUtilities.Find("QuantumMoon_Body/Volumes/AudioVolume_QM_Music").SetActive(false);

            //Enabling hologram on Vessel
            GameObject.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/VesselHologram_EyeSignal").SetActive(false);
            GameObject.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/VesselHologram_EyeSignal(Clone)").SetActive(true);            

        }

        //Props from Json files 
        public void DisabledPropsOnStart(bool isActive)

        {
            var QMgroundText = SearchUtilities.Find("QuantumMoon_Body/Sector_QuantumMoon/State_TH/NomaiWallText");
            QMgroundText.SetActive(isActive);

            var THrecorder = SearchUtilities.Find("TimberHearth_Body/Sector_TH/Prefab_NOM_Recorder(Clone)_TH");
            THrecorder.SetActive(isActive);

            var GDrecorder = SearchUtilities.Find("GiantsDeep_Body/Sector_GD/Prefab_NOM_Recorder(Clone)");
            GDrecorder.SetActive(isActive);

            var GDcomputer = SearchUtilities.Find("GiantsDeep_Body/Sector_GD/Prefab_NOM_Computer(Clone)");
            var GDcomp = GDcomputer.GetComponent<NomaiComputer>();
            GDcomp.enabled = isActive;
            GDcomputer.SetActive(isActive);

            var DBrecorder = SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Prefab_NOM_Recorder(Clone)");
            DBrecorder.SetActive(isActive);

            var solanumDB = SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Nomai_ANIM_SkyWatching_Idle");
            solanumDB.SetActive(isActive);

            var signalDB = SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Signal_Solanum");
            signalDB.GetComponent<AudioSignal>();
            signalDB.SetActive(isActive);

            var particlesTH = SearchUtilities.Find("TimberHearth_Body/Sector_TH/Effects_NOM_WarpParticles");
            particlesTH.SetActive(isActive);

            var particlesGD = SearchUtilities.Find("GiantsDeep_Body/Sector_GD/Effects_NOM_WarpParticles");
            particlesGD.SetActive(isActive);

            var particlesQM = SearchUtilities.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/Character_NOM_Solanum/Effects_NOM_WarpParticles");
            particlesQM.SetActive(isActive);

            var particlesDB = SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Effects_NOM_WarpParticles");
            particlesDB.SetActive(isActive);

            var particlesATP = SearchUtilities.Find("TimeLoopRing_Body/Effects_NOM_WarpParticles");
            particlesATP.SetActive(isActive);

            var signalATP = SearchUtilities.Find("TimeLoopRing_Body/Signal_Solanum");
            signalATP.SetActive(isActive);                

            var solanumATP = SearchUtilities.Find("TimeLoopRing_Body/Character_NOM_Solanum");
            solanumATP.SetActive(isActive);

            var ATPrecorder = SearchUtilities.Find("TimeLoopRing_Body/Prefab_NOM_Recorder(Clone)");
            ATPrecorder.SetActive(isActive);

            var THsignal = SearchUtilities.Find("TimberHearth_Body/Sector_TH/Signal_Solanum");
            THsignal.SetActive(isActive);

            var GDsignal = SearchUtilities.Find("GiantsDeep_Body/Sector_GD/Signal_Solanum");
            GDsignal.SetActive(isActive);

            var QMsignal = SearchUtilities.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/Character_NOM_Solanum/Signal_Solanum");
            QMsignal.SetActive(isActive);

            var solanumTH = SearchUtilities.Find("TimberHearth_Body/Sector_TH/Character_NOM_Solanum");
            solanumTH.SetActive(isActive);

            var solanumGD = SearchUtilities.Find("GiantsDeep_Body/Sector_GD/Character_NOM_Solanum");
            solanumGD.SetActive(isActive);
        }

        public void EndGame()
        {
            DeathManager deathManager = null;
            deathManager._escapedTimeLoopSequenceComplete = true;
            deathManager.KillPlayer(DeathType.BlackHole);
                        
        }

        public void PlayThunderSound()
        {

            PlayerHeadsetAudioSource = GameObject.Find("Player_Body").AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip((AudioType)2007); // GD_Lightning = 2007
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 8f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.PlayOneShot();
        }
        public void PlayRevealSound()
        {

            PlayerHeadsetAudioSource = GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/Character_NOM_Solanum/").AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip((AudioType)2903); // EyeTemple_Stinger = 2903
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 0.25f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.Play(delay: 1);

        }
        public void PlaySFXSound()
        {

            PlayerHeadsetAudioSource = GameObject.Find("Player_Body").AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip((AudioType)2402); // SingularityOnPlayerEnterExit = 2402            
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 15f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.PlayOneShot();

        }

        public void PlayGaspSound()
        {

            PlayerHeadsetAudioSource = GameObject.Find("Player_Body").AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip((AudioType)854); // 2400(whosh) 2407(vessel create singularity, 2408 - vessel out of sing) 2402 - getting in on BH; 2429 - reality broken // 2007 - GD lightning
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 1f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.PlayOneShot();
        }


    }
}
