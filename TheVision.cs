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
        public OWAudioSource qmAudioSourse;

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

        private static void SpawnSolanumProps()
        {

            GameObject visionTarget = GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon/VisionStaffDetector");

            visionTarget.transform.parent =
                GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon")
                .GetComponentsInChildren<Transform>(true)
                .Where(t => t.gameObject.name == "State_EYE")
                .First(); // All because Find doesn't work on inactive game objects :/


            //parenting particles to Solanum
            var QMparticles = GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon/Effects_NOM_WarpParticles(Clone)");
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


            //setting nice color for this one
            var GDcomputerColor = GameObject.Find("GiantsDeep_Body/Sector_GD/Prefab_NOM_Computer(Clone)/PointLight_NOM_Computer").GetComponent<Light>();
            Color superColor = new Color { r = 1, g = 0, b = 5 };
            GDcomputerColor.color = superColor;

            // var GDcomputerComponent = GameObject.Find("GiantsDeep_Body/Sector_GD/Prefab_NOM_Computer(Clone)");// .GetComponent<NomaiComputer>();
            // GDcomputerComponent.;


        }


        // Load SolanumProps
        public void OnStarSystemLoaded(string systemName)
        {
            ModHelper.Console.WriteLine("LOADED SYSTEM " + systemName);

            if (systemName == "SolarSystem")
            {
                SpawnSolanumProps();
                SpawnVisionTorch(); // then DELETE when everything is ready
                DisabledPropsOnStart(false);

            }
        }
        // async func to teleport ship to TH State on QM so player can continue the journey
        public static async Task TeleportShip()
        {
            var qmStateTH = GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon/State_TH");
            // qmStateTH.SetActive(false);
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

                // s_dmg.ToggleInvincibility();
                // s_dmg._invincible = true;               


                Vector3 newPosition = qm_rb.transform.TransformPoint(new Vector3(30f, -90f, 0f));
                s_rb.SetPosition(newPosition);
                s_rb.SetRotation(Quaternion.LookRotation(qm_rb.transform.forward, -qm_rb.transform.up));
                s_rb.SetVelocity(qm_rb.GetPointVelocity(newPosition));
                s_rb.SetAngularVelocity(qm_rb.GetAngularVelocity());

                await Task.Yield();


                TheVision.Instance.ModHelper.Console.WriteLine("Ship teleported!");

                break; // or it will teleport it forever
            }
        }
        //Bars to spawn SolanumCopies



        public void SpawnSolanumCopy(INewHorizons newHorizonsAPI)
        {

            TeleportShip();

            // Spawning Solanum on TH
            string path = "QuantumMoon_Body/Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/Character_NOM_Solanum/Nomai_ANIM_SkyWatching_Idle";
            Vector3 position = new Vector3(48.5018f, 15.1183f, 249.9972f);
            Vector3 rotation = new Vector3(332.5521f, 279.0402f, 275.7439f);
            newHorizonsAPI.SpawnObject(Locator._timberHearth.gameObject, Locator._timberHearth.GetRootSector(), path, position, rotation, 1, false);

            // Spawning Solanum on GD
            string path2 = "QuantumMoon_Body/Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/Character_NOM_Solanum/Nomai_ANIM_SkyWatching_Idle";
            Vector3 position2 = new Vector3(-43.62191f, -68.5414f, -31.2553654f);
            Vector3 rotation2 = new Vector3(350.740326f, 50.80401f, 261.666534f);
            newHorizonsAPI.SpawnObject(Locator._giantsDeep.gameObject, Locator._giantsDeep.GetRootSector(), path2, position2, rotation2, 1, false);

            string path3 = "QuantumMoon_Body/Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/Character_NOM_Solanum/Nomai_ANIM_SkyWatching_Idle";
            Vector3 position3 = new Vector3(4.2105f, -0.1138f, 1.9017f);
            Vector3 rotation3 = new Vector3(0f, -50f, 0f);
            newHorizonsAPI.SpawnObject(Locator._quantumMoon.gameObject, Locator._quantumMoonAstroObj.GetRootSector(), path3, position3, rotation3, 1, false);

            // Deactivating it so it will be no sound or flickers
            GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE/VisionStaffDetector").SetActive(false);

            // Disabling music on QM
            GameObject.Find("QuantumMoon_Body/Volumes/AudioVolume_QM_Music").SetActive(false);



            //var ship = GameObject.Find("Ship_Body");
            // ship.transform.parent = GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon").transform.Find("State_TH");
            // ship.GetComponent<CenterOfTheUniverseOffsetApplier>().enabled = false;
            //Vector3 zerovelo = new Vector3(0, 0, 0);
            // ship.GetComponent<OWRigidbody>().SetAngularVelocity(zerovelo);
            //ship.transform.position = new Vector3(24.1632f, -67.7431f, 16.1634f);
            // ship.GetComponent<OWRigidbody>().SetAngularVelocity(zerovelo);
            // ship.transform.rotation = new Quatern(46.2499f, -65.2599f, -5.48f);
            // var tree = SearchUtilities.Find("QuantumMoon_Body/Sector_QuantumMoon/State_TH/Interactables_THState/Crater_1/Crater_1_QSequoia");
            // tree.SetActive(false);
            //  ship.transform.position = tree.transform.position;  

        }
        // Spawning Vision Torch with code
        public void SpawnVisionTorch()
        {

            var path = "DreamWorld_Body/Sector_DreamWorld/Sector_Underground/Sector_PrisonCell/Interactibles_PrisonCell/PrisonerSequence/VisionTorchWallSocket/Prefab_IP_VisionTorchItem";
            Vector3 position = new Vector3(18.06051f, -50.64357f, 183.141f);
            Vector3 rotation = new Vector3(311.8565f, 287.9388f, 254.72f);
            GameObject staff = DetailBuilder.MakeDetail(Locator._timberHearth.gameObject, Locator._timberHearth.GetRootSector(), path, position, rotation, 1, false);

        }
        public NewHorizons.External.Modules.SignalModule.SignalInfo MakeSolanumSignalInfo(Vector3 position)
        {

            //Solanum signal parameters, reveal ship log about new quantum rule
            return new NewHorizons.External.Modules.SignalModule.SignalInfo()
            {
                audioFilePath = "planets/quantum.wav",
                frequency = "Quantum Consciousness",
                detectionRadius = 5000,
                identificationRadius = 1000,
                sourceRadius = 2f,
                name = "Solanum",
                position = position,
                onlyAudibleToScope = false,
                reveals = "WHAT_IS_NEW_QR",

            };
        }

        public void SpawnSignals()

        {
            PlayThunderSound();
            PlayRevealSound();
            PlaySFXSound();
            PlayGaspSound();

            //Enabling props that spawned with json I guess 
            DisabledPropsOnStart(true);



            //placing orb on GD to the slot (1)
            var nomaiSlot = SearchUtilities.Find("GiantsDeep_Body/Sector_GD/Sector_GDInterior/Sector_GDCore/Sector_Module_Sunken/Interactables_Module_Sunken/OrbInterface/Slots/Slot (1)");
            var nomaiInterfaceOrb = SearchUtilities.Find("GiantsDeep_Body/Sector_GD/Sector_GDInterior/Sector_GDCore/Sector_Module_Sunken/Interactables_Module_Sunken/OrbInterface/Prefab_NOM_InterfaceOrb");
            var nomaiCorrectSlot = nomaiInterfaceOrb.GetComponent<NomaiInterfaceOrb>();
            var nomaiCorrectSlot2 = nomaiCorrectSlot.GetComponent<OWRigidbody>();
            nomaiCorrectSlot.SetOrbPosition(nomaiSlot.transform.position);
            nomaiCorrectSlot._orbBody.ChangeSuspensionBody(nomaiCorrectSlot2);

            //decloaking QM on signals spawn
            GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE/Clouds_QM_EyeState").SetActive(false);
            GameObject.Find("QuantumMoon_Body/Atmosphere_QM/FogSphere").SetActive(false);

            //disabling recorder on QM Solanum shuttle
            GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon/QuantumShuttle/Prefab_NOM_Shuttle/Sector_NomaiShuttleInterior/Interactibles_NomaiShuttleInterior/Prefab_NOM_Recorder").SetActive(false);

            //disabling common computer
            var GDcommonComputer = GameObject.Find("GiantsDeep_Body/Sector_GD/Sector_GDInterior/Sector_GDCore/Sector_Module_Sunken/Interactables_Module_Sunken/Computers/ComputerPivot (1)");
            GDcommonComputer.SetActive(false);
            GameObject.Find("GiantsDeep_Body/Sector_GD/Prefab_NOM_Computer(Clone)").transform.rotation = GDcommonComputer.transform.rotation;

            //Spawning Solanum signals
            SignalBuilder.Make(Locator._timberHearth.gameObject, Locator._timberHearth.GetRootSector(), MakeSolanumSignalInfo(new Vector3(48.5018f, 15.1183f, 249.9972f)), TheVision.Instance);
            SignalBuilder.Make(Locator._quantumMoon.gameObject, Locator._quantumMoonAstroObj.GetRootSector(), MakeSolanumSignalInfo(new Vector3(-5.254965f, -70.73996f, 1.607201f)), TheVision.Instance);
            SignalBuilder.Make(Locator._giantsDeep.gameObject, Locator._giantsDeep.GetRootSector(), MakeSolanumSignalInfo(new Vector3(-43.62191f, -68.5414f, -31.2553654f)), TheVision.Instance);
            SignalBuilder.Make(Locator._darkBramble.gameObject, Locator._darkBramble.GetRootSector(), MakeSolanumSignalInfo(new Vector3(148.221f, 25.8914f, -0.2369f)), TheVision.Instance);

            //parenting QM signal to Solanum
            var QMsignal = GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon/Signal_Solanum");
            QMsignal.transform.parent = GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot").transform.Find("Character_NOM_Solanum");

            //Enabling hologram
            GameObject.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/VesselHologram_EyeSignal").SetActive(false);
            GameObject.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/VesselHologram_EyeSignal(Clone)").SetActive(true);


        }

        //Props from Json files (recorders mostly)
        public void DisabledPropsOnStart(bool isActive)
        {
            GameObject QMgroundText = GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon/State_TH/NomaiWallText");

            GameObject THrecorder = GameObject.Find("TimberHearth_Body/Sector_TH/Prefab_NOM_Recorder(Clone)_TH");

            GameObject GDrecorder = GameObject.Find("GiantsDeep_Body/Sector_GD/Prefab_NOM_Recorder(Clone)");

            GameObject GDcomputer = GameObject.Find("GiantsDeep_Body/Sector_GD/Prefab_NOM_Computer(Clone)");

            var GDcomp = GDcomputer.GetComponent<NomaiComputer>();
            GDcomp.enabled = isActive;

            GameObject DBrecorder = GameObject.Find("DB_VesselDimension_Body/Sector_VesselDimension/Prefab_NOM_Recorder(Clone)");

            GameObject solanumDB = GameObject.Find("DB_VesselDimension_Body/Sector_VesselDimension/Nomai_ANIM_SkyWatching_Idle(Clone)");

            GameObject signalDB = GameObject.Find("DB_VesselDimension_Body/Sector_VesselDimension/Signal_Solanum");

            GameObject particlesTH = GameObject.Find("TimberHearth_Body/Sector_TH/Effects_NOM_WarpParticles(Clone)");

            GameObject particlesGD = GameObject.Find("GiantsDeep_Body/Sector_GD/Effects_NOM_WarpParticles(Clone)");

            GameObject particlesQM = GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/Character_NOM_Solanum/Effects_NOM_WarpParticles(Clone)");

            GameObject particlesDB = GameObject.Find("DB_VesselDimension_Body/Sector_VesselDimension/Effects_NOM_WarpParticles(Clone)");



            //GameObject particlesATP = GameObject.Find("TimeLoopRing_Body/Effects_NOM_WarpParticles(Clone)");

            //GameObject signalATP = GameObject.Find("TimeLoopRing_Body/Signal_Solanum");

            //GameObject solanumATP = GameObject.Find("TimeLoopRing_Body/Nomai_ANIM_SkyWatching_Idle(Clone)");

            //GameObject ATPrecorder = GameObject.Find("TimeLoopRing_Body/Prefab_NOM_Recorder(Clone)");

            if (isActive == false)
            {
                QMgroundText.SetActive(false);
                THrecorder.SetActive(false);
                GDrecorder.SetActive(false);
                GDcomputer.SetActive(false);
                DBrecorder.SetActive(false);
                solanumDB.SetActive(false);
                signalDB.SetActive(false);
                particlesTH.SetActive(false);
                particlesGD.SetActive(false);
                particlesQM.SetActive(false);
                particlesDB.SetActive(false);

                // particlesATP.SetActive(false);
                // solanumATP.SetActive(false);
                // signalATP.SetActive(false);
                // ATPrecorder.SetActive(false);
            }
            else
            {
                QMgroundText.SetActive(true);
                THrecorder.SetActive(true);
                GDrecorder.SetActive(true);
                GDcomputer.SetActive(true);
                DBrecorder.SetActive(true);
                solanumDB.SetActive(true);
                signalDB.SetActive(true);
                particlesTH.SetActive(true);
                particlesGD.SetActive(true);
                particlesQM.SetActive(true);
                particlesDB.SetActive(true);

                // particlesATP.SetActive(false);
                // solanumATP.SetActive(true);
                // signalATP.SetActive(true);
                // ATPrecorder.SetActive(true);
            }
        }

        public void PlayThunderSound()
        {
            PlayerHeadsetAudioSource = GameObject.Find("Player_Body").AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip((AudioType)2007); // GD_Lightning = 2007
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 8f);
            PlayerHeadsetAudioSource.PlayOneShot();

        }
        public void PlayRevealSound()
        {
            PlayerHeadsetAudioSource = GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/Character_NOM_Solanum/").AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip((AudioType)2903); // EyeTemple_Stinger = 2903
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 0.25f);
            PlayerHeadsetAudioSource.Play(delay: 1);
        }
        public void PlaySFXSound()
        {
            PlayerHeadsetAudioSource = GameObject.Find("Player_Body").AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip((AudioType)2402); // SingularityOnPlayerEnterExit = 2402            
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 15f);
            PlayerHeadsetAudioSource.PlayOneShot();            
        }

        public void PlayGaspSound()
        {
            PlayerHeadsetAudioSource = GameObject.Find("Player_Body").AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip((AudioType)854); // 2400(whosh) 2407(vessel create singularity, 2408 - vessel out of sing) 2402 - getting in on BH; 2429 - reality broken // 2007 - GD lightning
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 1f);
            PlayerHeadsetAudioSource.PlayOneShot();            
        }



    }
}




















