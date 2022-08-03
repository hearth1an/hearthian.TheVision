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
        }

        private static void SpawnStartProps()
        {
            // Making custom text for reply
            NomaiWallText responseText = Locator.GetAstroObject(AstroObject.Name.QuantumMoon).transform.Find("Sector_QuantumMoon/State_EYE/QMResponseText").GetComponent<NomaiWallText>();
            responseText.HideTextOnStart();

            var nomaiConversationManager = Resources.FindObjectsOfTypeAll<NomaiConversationManager>().First(); //GameObject.FindObjectOfType<NomaiConversationManager>();
            var myConversationManager = nomaiConversationManager.gameObject.AddComponent<TheVision_SolanumVisionResponse>();
            myConversationManager._nomaiConversationManager = nomaiConversationManager;
            myConversationManager._solanumAnimController = nomaiConversationManager._solanumAnimController;
            myConversationManager.solanumVisionResponse = responseText;

            GameObject visionTarget = Locator.GetAstroObject(AstroObject.Name.QuantumMoon).transform.Find("Sector_QuantumMoon/State_EYE/VisionStaffDetector").gameObject;
            visionTarget.GetComponent<VisionTorchTarget>().onSlidesStart = myConversationManager.OnVisionStart;
            visionTarget.GetComponent<VisionTorchTarget>().onSlidesComplete = myConversationManager.OnVisionEnd;

            // Replacing new Hologram
            var origHologram = Locator.GetMinorAstroObject("Vessel Dimension").transform.Find("Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/VesselHologram_EyeSignal").gameObject;
            var hologramClone = GameObject.Instantiate(origHologram);
            hologramClone.name = "VesselHologram_GloamingGalaxy";
            hologramClone.transform.parent = Locator.GetMinorAstroObject("Vessel Dimension").transform.Find("Sector_VesselDimension/Sector_VesselBridge").transform;
            hologramClone.transform.position = origHologram.transform.position;
            hologramClone.transform.rotation = origHologram.transform.rotation;
            var mat = hologramClone.GetComponent<MeshRenderer>().material;
            mat.SetTexture("_MainTex", TheVision.Instance.ModHelper.Assets.GetTexture("images/NewHologram.png"));
            hologramClone.GetComponent<MeshRenderer>().sharedMaterial = mat;
            hologramClone.SetActive(false);            

            // Disabling WH on QM on the start
            Locator.GetAstroObject(AstroObject.Name.QuantumMoon).transform.Find("Sector_QuantumMoon/WhiteHole").gameObject.SetActive(false);

            // Setting green color for this one
            var GDcomputerColor = Locator.GetAstroObject(AstroObject.Name.GiantsDeep).transform.Find("Sector_GD/Prefab_NOM_Computer_GD/PointLight_NOM_Computer").GetComponent<Light>();
            GDcomputerColor.color = new Color { r = 0, g = 2, b = 1 };            

        }


        // Makes the Vision Torch more lore-friendly to pick
        public static void PickUpTorch()
        {
            // Vision Torch 1 (first room)
            var visionTorchMesh = SearchUtilities.Find("RingWorld_Body/Sector_RingWorld/Prefab_IP_VisionTorchItem/Prefab_IP_VisionTorchProjector/Props_IP_ScannerStaff");
            visionTorchMesh.SetActive(false);            
            var visionTorch = SearchUtilities.Find("RingWorld_Body/Sector_RingWorld/Prefab_IP_VisionTorchItem");            
            var visionTorchActive = SearchUtilities.Find("RingWorld_Body/Sector_RingWorld/Sector_SecretEntrance/Interactibles_SecretEntrance/Experiment_1/VisionTorchApparatus/VisionTorchRoot");
            visionTorch.transform.position = visionTorchActive.transform.position;
            visionTorch.transform.rotation = visionTorchActive.transform.rotation;
            var visionTorchTaken = visionTorch.GetComponent<OWCollider>();

            // Vision Torch 2 (third room)
            var visionTorchMesh2 = SearchUtilities.Find("RingWorld_Body/Sector_RingWorld/Sector_SecretEntrance/Prefab_IP_VisionTorchItem/Prefab_IP_VisionTorchProjector/Props_IP_ScannerStaff");
            visionTorchMesh2.SetActive(false);
            var visionTorch2 = SearchUtilities.Find("RingWorld_Body/Sector_RingWorld/Sector_SecretEntrance/Prefab_IP_VisionTorchItem");
            var visionTorchActive2 = SearchUtilities.Find("RingWorld_Body/Sector_RingWorld/Sector_SecretEntrance/Interactibles_SecretEntrance/Experiment_3/VisionTorchApparatus/VisionTorchRoot");
            visionTorch2.transform.position = visionTorchActive2.transform.position;
            visionTorch2.transform.rotation = visionTorchActive2.transform.rotation;
            var visionTorchTaken2 = visionTorch2.GetComponent<OWCollider>();

            // Vision Torch 1 picking up
            TheVision.Instance.ModHelper.Events.Unity.RunWhen(() => !visionTorchTaken._active, () =>
            {

                visionTorchMesh.SetActive(true);
                visionTorchActive.SetActive(false);

            });

            // Vision Torch 2 picking up
            TheVision.Instance.ModHelper.Events.Unity.RunWhen(() => !visionTorchTaken2._active, () =>
            {

                visionTorchMesh2.SetActive(true);
                visionTorchActive2.SetActive(false);

            });            
            
        }

        
            // Load StartProps and DisabledPropsOnStart
            public void OnStarSystemLoaded(string systemName)
        {
            ModHelper.Console.WriteLine("LOADED SYSTEM " + systemName);

            var timeLoop = TimeLoop.GetLoopCount(); // then add && timeLoop >= 2 with system name

            if (systemName == "SolarSystem" )
            {
                SpawnStartProps();
                PickUpTorch();
                DisabledPropsOnStart(false);
                ModHelper.Events.Unity.RunWhen(() => Locator.GetShipLogManager() != null, () =>
                {
                    if (Locator.GetShipLogManager().IsFactRevealed("SOLANUM_PROJECTION_COMPLETE"))
                    {
                         DisabledPropsOnStart(true);
                    }
                });
            }
            if (systemName == "GloamingGalaxy")
            {
                EndGame();
            }
        }

        // Async func to teleport ship to TH State on QM so player can continue the journey
        public static void TeleportShip()
        {
            var qmStateTH = Locator.GetAstroObject(AstroObject.Name.QuantumMoon).transform.Find("Sector_QuantumMoon/State_TH");
            ShipDamageController s_dmg = Locator.GetShipBody().GetComponent<ShipDamageController>();
            bool originalInvicibility = s_dmg._invincible;
            s_dmg._invincible = true;

            TheVision.Instance.ModHelper.Console.WriteLine("Ready to teleport ship!");

            TheVision.Instance.ModHelper.Events.Unity.RunWhen(() => qmStateTH.gameObject.activeSelf, () =>
            {
                OWRigidbody qm_rb = Locator.GetAstroObject(AstroObject.Name.QuantumMoon).GetComponent<OWRigidbody>();
                OWRigidbody s_rb = Locator.GetShipBody();

                Vector3 newPosition = qm_rb.transform.TransformPoint(new Vector3(30f, -100f, 0f));
                s_rb.SetPosition(newPosition);
                s_rb.SetRotation(Quaternion.LookRotation(qm_rb.transform.forward, -qm_rb.transform.up));
                s_rb.SetVelocity(qm_rb.GetPointVelocity(newPosition));
                s_rb.SetAngularVelocity(qm_rb.GetAngularVelocity());

                TheVision.Instance.ModHelper.Console.WriteLine("Ship teleported!");

                // TheVision.CustomProps.PlayStartSound(false);

                s_dmg._invincible = originalInvicibility;
            });
        }

        public void SpawnOnVisionEnd()
        {
            // Playing SFX
            PlayRevealSound();
            PlaySFXSound();
            PlayGaspSound();
            PlayThunderSound();

            // Enabling json props
            DisabledPropsOnStart(true);

            // Disabling QM WhiteHole
            Locator.GetAstroObject(AstroObject.Name.QuantumMoon).transform.Find("Sector_QuantumMoon/WhiteHole").gameObject.SetActive(false);

            var cameraFixedPosition = Locator.GetPlayerTransform().gameObject.GetComponent<PlayerLockOnTargeting>();
            cameraFixedPosition.BreakLock(0.5f);

            

            Locator.GetPlayerTransform().SetLocalPositionX(-1.5f);
            

           
            
           /* var rig = cameraFixedPosition.GetComponent<OWRigidbody>();
            rig.UnfreezePosition();
            rig.UnfreezeRotation();
           */
            TeleportShip();
        }

        //Props from Json files 
        public void DisabledPropsOnStart(bool isActive)
        {
            //decloaking QM on signals spawn
            Locator.GetAstroObject(AstroObject.Name.QuantumMoon).transform.Find("Sector_QuantumMoon/State_EYE/Clouds_QM_EyeState").gameObject.SetActive(!isActive);
            Locator.GetAstroObject(AstroObject.Name.QuantumMoon).transform.Find("Atmosphere_QM/FogSphere").gameObject.SetActive(!isActive);            

            //disabling recorder on QM Solanum shuttle
            Locator.GetAstroObject(AstroObject.Name.QuantumMoon).transform.Find("Sector_QuantumMoon/QuantumShuttle/Prefab_NOM_Shuttle/Sector_NomaiShuttleInterior/Interactibles_NomaiShuttleInterior/Prefab_NOM_Recorder").gameObject.SetActive(!isActive);

            var QMgroundText = Locator.GetAstroObject(AstroObject.Name.QuantumMoon).transform.Find("Sector_QuantumMoon/State_TH/QMGroundText").gameObject;
            QMgroundText.SetActive(isActive);

            var THrecorder = Locator.GetAstroObject(AstroObject.Name.TimberHearth).transform.Find("Sector_TH/Prefab_NOM_Recorder_TH").gameObject;
            THrecorder.SetActive(isActive);

            var GDrecorder = Locator.GetAstroObject(AstroObject.Name.GiantsDeep).transform.Find("Sector_GD/Prefab_NOM_Recorder_GD").gameObject;
            GDrecorder.SetActive(isActive);

            var GDcomputer = Locator.GetAstroObject(AstroObject.Name.GiantsDeep).transform.Find("Sector_GD/Prefab_NOM_Computer_GD").gameObject;
            var GDcomp = GDcomputer.GetComponent<NomaiComputer>();
            GDcomp.enabled = isActive;
            GDcomputer.SetActive(isActive);

            // Disabling common computer on GD, placing the right one to correct position
            var GDcommonComputer = Locator.GetAstroObject(AstroObject.Name.GiantsDeep).transform.Find("Sector_GD/Sector_GDInterior/Sector_GDCore/Sector_Module_Sunken/Interactables_Module_Sunken/Computers/ComputerPivot (1)").gameObject;
            GDcommonComputer.SetActive(!isActive);
            GDcomputer.transform.rotation = GDcommonComputer.transform.rotation;

            var DBrecorder = Locator.GetMinorAstroObject("Vessel Dimension").transform.Find("Sector_VesselDimension/Prefab_NOM_Recorder_DB").gameObject;
            DBrecorder.SetActive(isActive);

            var solanumDB = Locator.GetMinorAstroObject("Vessel Dimension").transform.Find("Sector_VesselDimension/Nomai_ANIM_SkyWatching_Idle").gameObject;
            solanumDB.SetActive(isActive);

            var signalDB = Locator.GetMinorAstroObject("Vessel Dimension").transform.Find("Sector_VesselDimension/Signal_Solanum").gameObject;
            signalDB.SetActive(isActive);

            var particlesTH = Locator.GetAstroObject(AstroObject.Name.TimberHearth).transform.Find("Sector_TH/Effects_NOM_WarpParticles").gameObject;
            particlesTH.SetActive(isActive);

            var particlesGD = Locator.GetAstroObject(AstroObject.Name.GiantsDeep).transform.Find("Sector_GD/Effects_NOM_WarpParticles").gameObject;
            particlesGD.SetActive(isActive);

            var particlesQM = Locator.GetAstroObject(AstroObject.Name.QuantumMoon).transform.Find("Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/Character_NOM_Solanum/Effects_NOM_WarpParticles").gameObject;
            particlesQM.SetActive(isActive);

            var particlesDB = Locator.GetMinorAstroObject("Vessel Dimension").transform.Find("Sector_VesselDimension/Effects_NOM_WarpParticles").gameObject;
            particlesDB.SetActive(isActive);

            var particlesATP = SearchUtilities.Find("TimeLoopRing_Body/Effects_TimeLoopRing/Effects_NOM_WarpParticles");
            particlesATP.SetActive(isActive);

            var signalATP = SearchUtilities.Find("TimeLoopRing_Body/Interactibles_TimeLoopRing/Signal_Solanum");
            signalATP.SetActive(isActive);

            var solanumATP = SearchUtilities.Find("TimeLoopRing_Body/Characters_TimeLoopRing/Nomai_ANIM_SkyWatching_Idle");
            solanumATP.SetActive(isActive);

            var ATPhidden = SearchUtilities.Find("TimeLoopRing_Body/Interactibles_TimeLoopRing_Hidden").transform;

            var ATPrecorder = ATPhidden.Find("Prefab_NOM_Recorder_ATP").gameObject;
            ATPrecorder.SetActive(isActive);

            ATPhidden.GetComponentInChildren<NomaiComputerSlotInterface>().gameObject.name = "Prefab_NOM_Computer_WarpCore";

            var ATPcomputerOld = ATPhidden.Find("Prefab_NOM_Computer").gameObject;
            ATPcomputerOld.SetActive(!isActive);

            var ATPcomputer = ATPhidden.Find("Prefab_NOM_Computer_ATP").gameObject;
            var ATPcomp = ATPcomputer.GetComponent<NomaiComputer>();
            ATPcomp.SetSector(SearchUtilities.Find("TowerTwin_Body/Sector_TowerTwin/Sector_TimeLoopInterior").GetComponent<Sector>());
            ATPcomp.enabled = isActive;
            ATPcomputer.SetActive(isActive);
            ATPcomputer.transform.position = ATPcomputerOld.transform.position;
            ATPcomputer.transform.rotation = ATPcomputerOld.transform.rotation;

            var THsignal = Locator.GetAstroObject(AstroObject.Name.TimberHearth).transform.Find("Sector_TH/Signal_Solanum").gameObject;
            THsignal.SetActive(isActive);

            var GDsignal = Locator.GetAstroObject(AstroObject.Name.GiantsDeep).transform.Find("Sector_GD/Signal_Solanum").gameObject;
            GDsignal.SetActive(isActive);

            var QMsignal = Locator.GetAstroObject(AstroObject.Name.QuantumMoon).transform.Find("Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/Character_NOM_Solanum/Signal_Solanum").gameObject;
            QMsignal.SetActive(isActive);

            var solanumTH = Locator.GetAstroObject(AstroObject.Name.TimberHearth).transform.Find("Sector_TH/Nomai_ANIM_SkyWatching_Idle").gameObject;
            solanumTH.SetActive(isActive);

            var solanumGD = Locator.GetAstroObject(AstroObject.Name.GiantsDeep).transform.Find("Sector_GD/Nomai_ANIM_SkyWatching_Idle").gameObject;
            solanumGD.SetActive(isActive);

            var monolith = SearchUtilities.Find("TimeLoopRing_Body/Interactibles_TimeLoopRing/MaskPlatform/Props_NOM_Monolith_group/Monolith");
            monolith.SetActive(isActive);

            if (isActive)
            {
                var statueHead = SearchUtilities.Find("TimeLoopRing_Body/Props_TimeLoopRing/OtherComponentsGroup/Props_NOM_StatueHead");

                var topRotation = new Vector3(301.73f, 90, 270);
                var bottomRotation = new Vector3(17.1f, 90, 270);

                var eyelidL = statueHead.transform.Find("eyelid_l");
                eyelidL.transform.Find("eyelid_top").localEulerAngles = topRotation;
                eyelidL.transform.Find("eyelid_bot").localEulerAngles = bottomRotation;

                var eyelidMid = statueHead.transform.Find("eyelid_mid");
                eyelidMid.transform.Find("eyelid_top 1").localEulerAngles = topRotation;
                eyelidMid.transform.Find("eyelid_bot 1").localEulerAngles = bottomRotation;

                var eyelidR = statueHead.transform.Find("eyelid_r");
                eyelidR.transform.Find("eyelid_top 2").localEulerAngles = topRotation;
                eyelidR.transform.Find("eyelid_bot 2").localEulerAngles = bottomRotation;

                var copperEyes = SearchUtilities.FindResourceOfTypeAndName<Material>("Structure_NOM_GlowingCopper_mat");
                var statueEyes = statueHead.transform.Find("Statue_Eyes").GetComponent<MeshRenderer>();
                var statueEyesOW = statueHead.transform.Find("Statue_Eyes").gameObject.GetAddComponent<OWRenderer>();
                statueEyes.sharedMaterial = copperEyes;
                statueEyesOW._renderer = statueEyes;
                statueEyesOW.SetEmissionColor(Color.black);
            }

            if (isActive)
            {
                //placing orb on GD to the slot (1)
                var nomaiSlot = Locator.GetAstroObject(AstroObject.Name.GiantsDeep).transform.Find("Sector_GD/Sector_GDInterior/Sector_GDCore/Sector_Module_Sunken/Interactables_Module_Sunken/OrbInterface/Slots/Slot (1)");
                var nomaiInterfaceOrb = Locator.GetAstroObject(AstroObject.Name.GiantsDeep).transform.Find("Sector_GD/Sector_GDInterior/Sector_GDCore/Sector_Module_Sunken/Interactables_Module_Sunken/OrbInterface/Prefab_NOM_InterfaceOrb");
                var nomaiCorrectSlot = nomaiInterfaceOrb.GetComponent<NomaiInterfaceOrb>();
                var nomaiCorrectSlot2 = nomaiCorrectSlot.GetComponent<OWRigidbody>();
                nomaiCorrectSlot.SetOrbPosition(nomaiSlot.transform.position);
                nomaiCorrectSlot._orbBody.ChangeSuspensionBody(nomaiCorrectSlot2);

                //decloaking QM on signals spawn
                Locator.GetAstroObject(AstroObject.Name.QuantumMoon).transform.Find("Sector_QuantumMoon/State_EYE/Clouds_QM_EyeState").gameObject.SetActive(!isActive);
                Locator.GetAstroObject(AstroObject.Name.QuantumMoon).transform.Find("Atmosphere_QM/FogSphere").gameObject.SetActive(!isActive);

                //decloaking QM on signals spawn
                SearchUtilities.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/Character_NOM_Solanum/ConversationZone").SetActive(false);
                SearchUtilities.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/Character_NOM_Solanum/WatchZone").SetActive(false);

            }

            // Deactivating it so it will be no sound or flickers
            Locator.GetAstroObject(AstroObject.Name.QuantumMoon).transform.Find("Sector_QuantumMoon/State_EYE/VisionStaffDetector").gameObject.SetActive(!isActive);

            //Enabling hologram on Vessel
            Locator.GetMinorAstroObject("Vessel Dimension").transform.Find("Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/VesselHologram_EyeSignal").gameObject.SetActive(!isActive);
            Locator.GetMinorAstroObject("Vessel Dimension").transform.Find("Sector_VesselDimension/Sector_VesselBridge/VesselHologram_GloamingGalaxy").gameObject.SetActive(isActive);
        }

        public void EndGame()
        {
           // DeathManager deathManager = Locator.GetDeathManager();
           // deathManager._escapedTimeLoopSequenceComplete = true;
           // deathManager.KillPlayer(DeathType.BlackHole);

        }

        public void PlayThunderSound()
        {

            PlayerHeadsetAudioSource = Locator.GetPlayerTransform().gameObject.AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.GD_Lightning); // GD_Lightning = 2007
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 8f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.PlayOneShot();
        }

        public void PlayRevealSound()
        {

            PlayerHeadsetAudioSource = Locator.GetAstroObject(AstroObject.Name.QuantumMoon).transform.Find("Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/Character_NOM_Solanum").gameObject.AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.EyeTemple_Stinger); // EyeTemple_Stinger = 2903
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 0.25f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.Play(delay: 1);

        }
        public void PlaySFXSound()
        {

            PlayerHeadsetAudioSource = Locator.GetPlayerTransform().gameObject.AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.SingularityOnPlayerEnterExit); // SingularityOnPlayerEnterExit = 2402            
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 15f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.PlayOneShot();

        }

        public void PlayGaspSound()
        {

            PlayerHeadsetAudioSource = Locator.GetPlayerTransform().gameObject.AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.PlayerGasp_Heavy); // 2400(whosh) 2407(vessel create singularity, 2408 - vessel out of sing) 2402 - getting in on BH; 2429 - reality broken // 2007 - GD lightning // 854 PlayerGasp_Heavy
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 1f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.PlayOneShot();
        }


    }
}
