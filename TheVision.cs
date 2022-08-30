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
using NewHorizons.Handlers;
using System;


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
            Harmony.CreateAndPatchAll(System.Reflection.Assembly.GetExecutingAssembly());            
        }

        private void Start()
        {     
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

            var newHorizonsAPI = ModHelper.Interaction.GetModApi<INewHorizons>("xen.NewHorizons");
            newHorizonsAPI.GetStarSystemLoadedEvent().AddListener(OnStarSystemLoaded);
            newHorizonsAPI.LoadConfigs(this);            

            ModHelper.Console.WriteLine($"{nameof(TheVision)} is loaded!", MessageType.Success);

            TitleProps();            

            LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
            {
                if (loadScene == OWScene.EyeOfTheUniverse && Locator.GetShipLogManager().IsFactRevealed("SOLANUM_PROJECTION_COMPLETE"))
                {
                    EyeOfTheUniverseProps();
                }
                if (loadScene == OWScene.TitleScreen)
                {
                    TitleProps();
                }
                
                if (loadScene == OWScene.Credits_Fast)
                {
                    CreditsMusic();
                }               

            };
        }

        private void TitleProps()
        {
            GameObject.Find("Scene/Background/PlanetPivot/Prefab_HEA_Campfire/Effects/Effects_HEA_SmokeColumn/Effects_HEA_SmokeColumn_Title").GetComponent<MeshRenderer>().material.color = new Color(1, 2, 2, 1);            
            GameObject.Find("Scene/Background/PlanetPivot/Prefab_HEA_Campfire/Props_HEA_Campfire/Campfire_Flames").GetComponent<MeshRenderer>().material.color = new Color(0, 5, 4, 1);
            GameObject.Find("Scene/Background/PlanetPivot/Prefab_HEA_Campfire/Props_HEA_Campfire/Campfire_Embers").SetActive(false);                 
        }

        public void CreditsMusic()
        {            
            var addMusic = GameObject.Find("AudioSource").GetComponent<OWAudioSource>();
            addMusic.AssignAudioLibraryClip(AudioType.FinalCredits);
            addMusic.Play();
        }

        private void SpawnStartProps()
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

            ///////////// Making Solanum anim on Ember Twin !//////////

            SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/Prefab_NOM_Recorder_ET/InteractSphere").GetComponentInParent<SphereShape>().radius = 1.5f;

            var torchFix = SearchUtilities.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE/VisionStaffDetector").GetComponent<SphereShape>();
            torchFix.enabled = true;

            var torchSocketFix = SearchUtilities.Find("Ship_Body/ShipSector/VisionTorchSocket").GetComponent<VisionTorchSocket>();
            torchSocketFix.EnableInteraction(true);
            torchSocketFix.enabled = true;

            SearchUtilities.Find("TimeLoopRing_Body/Interactibles_TimeLoopRing_Hidden/Prefab_NOM_Computer_ATP/Ring5").gameObject.SetActive(false);
            SearchUtilities.Find("TimeLoopRing_Body/Interactibles_TimeLoopRing/Prefab_NOM_Recorder_ATP_2").SetActive(false);
            SearchUtilities.Find("TimeLoopRing_Body/Interactibles_TimeLoopRing/Prefab_NOM_Recorder_ATP_2").transform.localPosition = new Vector3(26.31f, -2.63f, -7.83f);

            var vesselCore = SearchUtilities.Find("Prefab_NOM_WarpCoreVesselBroken");
            vesselCore.transform.parent = SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:RT_Arm_ClavicleSHJnt/Nomai_Rig_v01:RT_Arm_ShoulderSHJnt/Nomai_Rig_v01:RT_Arm_ElbowSHJnt/Nomai_Rig_v01:RT_Arm_WristSHJnt").transform.parent;
            vesselCore.transform.localPosition = new Vector3(0.8f, -0.1f, -0.2f);
            vesselCore.transform.rotation = new Quaternion(0.3842f, 0.0578f, 0.7798f, -0.4009f);


            // Disabling Ernesto enter volume
            SearchUtilities.Find("TimberHearth_Body/Sector_TH/Reveal Volume (Enter)").SetActive(false);

            TheVision.Instance.ModHelper.Events.Unity.FireOnNextUpdate(() =>
            {
                Locator.GetShipLogManager().RevealFact("IP_ZONE_3_ENTRANCE_X1");

                  

                if (Locator.GetShipLogManager().IsFactRevealed("STATUE_ATP_LINK"))
                {
                      
                    SearchUtilities.Find("TimeLoopRing_Body/Interactibles_TimeLoopRing/Prefab_NOM_Recorder_ATP_2").gameObject.SetActive(true);
                    SearchUtilities.Find("TimeLoopRing_Body/Interactibles_TimeLoopRing/Prefab_NOM_Recorder_ATP/InteractSphere").gameObject.SetActive(false);
                    SearchUtilities.Find("TimeLoopRing_Body/Interactibles_TimeLoopRing/Prefab_NOM_Recorder_ATP/Props_NOM_Recorder").gameObject.SetActive(false);
                    SearchUtilities.Find("TimeLoopRing_Body/Interactibles_TimeLoopRing/Prefab_NOM_Recorder_ATP/PointLight_NOM_Recorder").gameObject.SetActive(false);
                    SearchUtilities.Find("TimeLoopRing_Body/Interactibles_TimeLoopRing/Prefab_NOM_Recorder_ATP/Audio_Recorder").gameObject.SetActive(false);

                    ModHelper.Console.WriteLine("Fact Checked, Prefab_NOM_Recorder_ATP_2 loaded", MessageType.Success);
                }

                if (Locator.GetShipLogManager().IsFactRevealed("SOLANUM_PROJECTION_COMPLETE") && !Locator.GetShipLogManager().IsFactRevealed("ERNESTO_POOR_ERNESTO"))
                {
                    ErnestoQuestEntry(true);
                }

                

            });     

            ///////////// Making Solanum anim on Brittle Hollow !//////////   


            // Particles QM and TH
            SearchUtilities.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/Character_NOM_Solanum/Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Effects_NOM_WarpParticlesWhite").transform.localPosition = new Vector3(-0.3f, -0.4f, 0f);
            SearchUtilities.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/Character_NOM_Solanum/Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Particles_2").transform.localPosition = new Vector3(-0.3f, -0.4f, 0f);

            // Particles GD
            SearchUtilities.Find("GiantsDeep_Body/Sector_GD/Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Effects_NOM_WarpParticlesWhite").transform.localPosition = new Vector3(-0.3f, -0.4f, 0f);
            SearchUtilities.Find("GiantsDeep_Body/Sector_GD/Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Particles_2").transform.localPosition = new Vector3(-0.3f, -0.4f, 0f);

            SearchUtilities.Find("GiantsDeep_Body/Sector_GD/SolanumTeleportation/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Effects_NOM_WarpParticlesWhite").transform.localPosition = new Vector3(-0.3f, -0.4f, 0f);
            SearchUtilities.Find("GiantsDeep_Body/Sector_GD/SolanumTeleportation/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Particles_2").transform.localPosition = new Vector3(-0.3f, -0.4f, 0f);

            // Particles ATP
            SearchUtilities.Find("TimeLoopRing_Body/Characters_TimeLoopRing/Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Effects_NOM_WarpParticlesWhite").transform.localPosition = new Vector3(-0.3f, -0.4f, 0f);
            SearchUtilities.Find("TimeLoopRing_Body/Characters_TimeLoopRing/Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Particles_2").transform.localPosition = new Vector3(-0.3f, -0.4f, 0f);

            // Particles DB
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Effects_NOM_WarpParticlesWhite").transform.localPosition = new Vector3(-0.3f, -0.4f, 0f);
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Particles_2").transform.localPosition = new Vector3(-0.3f, -0.4f, 0f);

            // Particles BH
            SearchUtilities.Find("Sector_BH/Solanum_BH_Character/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Effects_NOM_WarpParticlesWhite").transform.localPosition = new Vector3(-0.3f, -0.4f, 0f);
            SearchUtilities.Find("Sector_BH/Solanum_BH_Character/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Particles_2").transform.localPosition = new Vector3(-0.3f, -0.4f, 0f);

            // Particles ET
            SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/Solanum_EmberTwin_Character/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Effects_NOM_WarpParticlesWhite").transform.localPosition = new Vector3(-0.3f, -0.4f, 0f);
            SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/Solanum_EmberTwin_Character/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Particles_2").transform.localPosition = new Vector3(-0.3f, -0.4f, 0f);

            // Setting positions for Solanum signals in Dark Bramble
            SearchUtilities.Find("DB_HubDimension_Body/Sector_HubDimension/Interactables_HubDimension/Pivot/InnerWarp_ToAnglerNest/Signal_Solanum").transform.localPosition = new Vector3(0f, 0f, 0f);
            SearchUtilities.Find("DB_AnglerNestDimension_Body/Sector_AnglerNestDimension/Interactables_AnglerNestDimension/InnerWarp_ToVessel/Signal_Solanum").transform.localPosition = new Vector3(0f, 0f, 0f);

            
            SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Prefab_NOM_Recorder_BH").transform.localPosition = new Vector3(1.3f, 1f, 1.2f);
            SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/Solanum_EmberTwin_Character/Signal_Solanum").transform.localPosition = new Vector3(0f, 0f, 0f);
            SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Signal_Solanum").transform.localPosition = new Vector3(0f, 0f, 0f);
            SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Solanum_BH_Crystal").transform.localPosition = new Vector3(-1.8564f, -0.1f, 0.7f);
            SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Solanum_BH_Crystal").transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
            SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Solanum_BH_Crystal_Cracked").transform.localPosition = new Vector3(-1.8564f, -0.1f, 0.7f);
            SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Solanum_BH_Crystal_Cracked").transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
            SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Solanum_BH_Crystal_Cracked").SetActive(false);
            SearchUtilities.Find("GiantsDeep_Body/Sector_GD/BlackHole/DestructionVolume").gameObject.SetActive(false);

            
            SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/SandPile_1").gameObject.SetActive(false);
            SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/SandPile_2").gameObject.SetActive(false);
            SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/SandPile_3").gameObject.SetActive(false);          


            // GD teleportation event

            SearchUtilities.Find("GiantsDeep_Body/Sector_GD/BlackHole").gameObject.SetActive(false);
            SearchUtilities.Find("GiantsDeep_Body/Sector_GD/WhiteHole").gameObject.SetActive(false);
            SearchUtilities.Find("GiantsDeep_Body/Sector_GD/SolanumTeleportation").gameObject.SetActive(false);

            SearchUtilities.Find("GiantsDeep_Body/Sector_GD/SolanumTeleportation/Signal_Solanum").transform.localPosition = new Vector3(0f, 0f, 0f);

            SearchUtilities.Find("GiantsDeep_Body/Sector_GD/Sector_GDInterior/Sector_GDCore/Sector_Module_Sunken/Effects_Module_Sunken/SunkenModuleWater_ExteriorStencil").gameObject.SetActive(true);

            SearchUtilities.Find("GiantsDeep_Body/Sector_GD/Nomai_ANIM_SkyWatching_Idle/Signal_Solanum").transform.localPosition = new Vector3(0f, 0f, 0f);

            TheVision.Instance.ModHelper.Events.Unity.RunWhen(() => Locator.GetShipLogManager() != null && Locator.GetShipLogManager().IsFactRevealed("SOLANUM_ET_FOUND"), () =>
            {
                SolanumGreetingsET();
            });

            TheVision.Instance.ModHelper.Events.Unity.RunWhen(() => Locator.GetShipLogManager() != null && Locator.GetShipLogManager().IsFactRevealed("SOLANUM_BH_FOUND"), () =>
            {
                SolanumGreetingsBH();
            });

            
        }
        
        public void ErnestoQuestEntry(bool isActive)
        {
            if (isActive == true)
            {               

                TheVision.Instance.ModHelper.Events.Unity.RunWhen(() => Locator.GetShipLogManager() != null && Locator.GetShipLogManager().IsFactRevealed("ERNESTO_ENTRY"), () =>
                {
                    var fish = SearchUtilities.Find("TimberHearth_Body/Sector_TH/Sector_Village/Sector_StartingCamp/Characters_StartingCamp/Villager_HEA_Slate/Villager_HEA_Slate_ANIM_LogSit/Slate_Skin_01:tall_rig_b_v01:TrajectorySHJnt/Slate_Skin_01:tall_rig_b_v01:ROOTSHJnt/Slate_Skin_01:tall_rig_b_v01:Spine_01SHJnt/Slate_Skin_01:tall_rig_b_v01:Spine_02SHJnt/Slate_Skin_01:tall_rig_b_v01:Spine_TopSHJnt/Slate_Skin_01:tall_rig_b_v01:LF_Arm_ClavicleSHJnt/Slate_Skin_01:tall_rig_b_v01:LF_Arm_ShoulderSHJnt/Slate_Skin_01:tall_rig_b_v01:LF_Arm_ElbowSHJnt/Slate_Skin_01:tall_rig_b_v01:LF_Arm_WristSHJnt/Props_HEA_RoastingStick/PoorErnesto");
                    fish.transform.localPosition = new Vector3(0.0127f, 0.08f, 1.64f);
                    fish.transform.rotation = new Quaternion(0, 0, 0, 0);
                    fish.GetComponent<AnglerfishAnimController>().OnChangeAnglerState(AnglerfishController.AnglerState.Chasing);

                    SearchUtilities.Find("TimberHearth_Body/Sector_TH/Reveal Volume (Enter)").SetActive(true);

                });

                // setting up Hornfels for dialigue
                SearchUtilities.Find("TimberHearth_Body/Sector_TH/Sector_Village/Sector_Observatory/Characters_Observatory/Villager_HEA_Hornfels (1)/ConversationZone_Hornfels").DestroyAllComponents<InteractReceiver>();
                SearchUtilities.Find("TimberHearth_Body/Sector_TH/Sector_Village/Sector_Observatory/Characters_Observatory/Villager_HEA_Hornfels (1)/ConversationZone_Hornfels").DestroyAllComponents<CharacterDialogueTree>();

                SearchUtilities.Find("TimberHearth_Body/Sector_TH/Sector_Village/Sector_Observatory/Characters_Observatory/Villager_HEA_Hornfels (1)/Villager_HEA_Hornfels_ANIM_Working/ConversationZone").transform.localPosition = new Vector3(0f, 1.9649f, 0f);

                SearchUtilities.Find("TimberHearth_Body/Sector_TH/Sector_Village/Sector_Observatory/Interactables_Observatory/AnglerFishExhibit/AnglerFishTankPivot/Beast_Anglerfish").SetActive(false);

                var hornfels = SearchUtilities.Find("TimberHearth_Body/Sector_TH/Sector_Village/Sector_Observatory/Characters_Observatory/Villager_HEA_Hornfels (1)");
                hornfels.transform.localPosition = new Vector3(-5.9667f, 0.2095f, -1.0348f);
                hornfels.transform.localRotation = new Quaternion(0f, 0f, 0f, 0f);

                // setting up Slate for dialigue
                SearchUtilities.Find("TimberHearth_Body/Sector_TH/Sector_Village/Sector_StartingCamp/Characters_StartingCamp/Villager_HEA_Slate/ConversationZone_RSci").DestroyAllComponents<InteractReceiver>();
                SearchUtilities.Find("TimberHearth_Body/Sector_TH/Sector_Village/Sector_StartingCamp/Characters_StartingCamp/Villager_HEA_Slate/ConversationZone_RSci").DestroyAllComponents<CharacterDialogueTree>();

                SearchUtilities.Find("TimberHearth_Body/Sector_TH/Sector_Village/Sector_StartingCamp/Characters_StartingCamp/Villager_HEA_Slate/Villager_HEA_Slate_ANIM_LogSit/ConversationZone").transform.localPosition = new Vector3(-0.2199f, 1.0245f, -0.322f);

                TheVision.Instance.ModHelper.Events.Unity.RunWhen(() => Locator.GetShipLogManager() != null && Locator.GetShipLogManager().IsFactRevealed("ERNESTO_POOR_ERNESTO"), () =>
                {
                    SearchUtilities.Find("TimberHearth_Body/Sector_TH/Sector_Village/Volumes_Village/MusicVolume_Village").SetActive(false);
                    Invoke("PlaySecretSound", 2f);
                    ModHelper.Console.WriteLine("Thank you for finding Ernesto! You will never meet again.", MessageType.Success);
                });
            }       
            
            
            
             
        }

        public void ErnestoQuestEnd()
        {
            
        }

        public void SolanumGreetingsGD()
        {
            SearchUtilities.Find("GiantsDeep_Body/Sector_GD/Sector_GDInterior/Sector_GDCore/Sector_Module_Sunken/Effects_Module_Sunken/SunkenModuleWater_ExteriorStencil").gameObject.SetActive(false);
            SearchUtilities.Find("GiantsDeep_Body/Sector_GD/Sector_GDInterior/Sector_GDCore/Sector_Module_Sunken/Effects_Module_Sunken/sunkenModuleStencil").gameObject.SetActive(false);
            Invoke("SolanumPrepareTeleportationGD", 1f);
            Invoke("SolanumSingularityStartGD", 6f);
            Invoke("SolanumSingularityEndGD", 7f);
            Invoke("PlaySingularityCollapseSound", 6.5f);
            Invoke("SolanumTeleportationGD", 7f);
        }
        public void SolanumSingularityStartGD()
        {
            PlaySingularityCreateSound();
            SearchUtilities.Find("GiantsDeep_Body/Sector_GD/BlackHole").SetActive(true);
            SearchUtilities.Find("GiantsDeep_Body/Sector_GD/WhiteHole").SetActive(true);
        }

        public void Flicker()
        {
            SearchUtilities.Find("Player_Body/PlayerCamera/ScreenEffects/UnderwaterEffectBubble").SetActive(false);           
            PlayFlickerSound();
            var effect = SearchUtilities.Find("Player_Body/PlayerCamera/ScreenEffects/LightFlickerEffectBubble").GetComponent<LightFlickerController>();
            effect.FlickerOffAndOn(offDuration: 2f, onDuration: 3f);            
        }

        public void SolanumSingularityEndGD()
        {
            
            SearchUtilities.Find("GiantsDeep_Body/Sector_GD/BlackHole").SetActive(false);
            SearchUtilities.Find("GiantsDeep_Body/Sector_GD/WhiteHole").SetActive(false);           
        }

        public void SolanumPrepareTeleportationGD()
        {
            SolanumAnimController solanumAnimController = SearchUtilities.Find("GiantsDeep_Body/Sector_GD/Nomai_ANIM_SkyWatching_Idle").GetComponent<SolanumAnimController>();
            solanumAnimController.StartWatchingPlayer();
            solanumAnimController.StartWritingMessage();
            Invoke("Flicker", 4f);
        }

        public void SolanumTeleportationGD()
        {
            SearchUtilities.Find("Player_Body/PlayerCamera/ScreenEffects/UnderwaterEffectBubble").SetActive(true);
            SearchUtilities.Find("GiantsDeep_Body/Sector_GD/Nomai_ANIM_SkyWatching_Idle").SetActive(false);
            SearchUtilities.Find("GiantsDeep_Body/Sector_GD/SolanumTeleportation").SetActive(true);
            SearchUtilities.Find("GiantsDeep_Body/Sector_GD/Sector_GDInterior/Sector_GDCore/Sector_Module_Sunken/Effects_Module_Sunken/SunkenModuleWater_ExteriorStencil").gameObject.SetActive(true);
            SearchUtilities.Find("GiantsDeep_Body/Sector_GD/Sector_GDInterior/Sector_GDCore/Sector_Module_Sunken/Effects_Module_Sunken/sunkenModuleStencil").gameObject.SetActive(true);
        }

        public void SolanumGreetingsATP()
        {
            SolanumAnimController solanumAnimController = SearchUtilities.Find("TimeLoopRing_Body/Characters_TimeLoopRing/Nomai_ANIM_SkyWatching_Idle").GetComponent<SolanumAnimController>();
            solanumAnimController.StartWatchingPlayer();
            solanumAnimController.PlayRaiseCairns();

            Invoke("SolanumGreetingsATP_DeactivateRing", 4f);
            Invoke("SolanumGreetingsATP_ShowRing", 5f);
            Invoke("SolanumGreetingsATP_OpenCore", 6f);           

        }

        public void SolanumGreetingsATP_OpenCore()
        {
            SearchUtilities.Find("TowerTwin_Body/Sector_TowerTwin/Sector_TimeLoopInterior/Interactables_TimeLoopInterior/CoreCasingController").GetComponent<TimeLoopCoreController>().OpenCore();

            SolanumAnimController solanumAnimController = SearchUtilities.Find("TimeLoopRing_Body/Characters_TimeLoopRing/Nomai_ANIM_SkyWatching_Idle").GetComponent<SolanumAnimController>();
            solanumAnimController.StopWatchingPlayer();
        }

        public void SolanumGreetingsATP_ShowRing()
        {
            var atpRing3 = SearchUtilities.Find("TimeLoopRing_Body/Interactibles_TimeLoopRing_Hidden/Prefab_NOM_Computer_ATP/Ring5").GetComponent<NomaiComputerRing>();
            atpRing3.Activate(4, 2.5f);

            SearchUtilities.Find("TimeLoopRing_Body/Interactibles_TimeLoopRing_Hidden/Prefab_NOM_Computer_ATP/Ring5").gameObject.SetActive(true); 
        }

        public void SolanumGreetingsATP_DeactivateRing()
        {
            SearchUtilities.Find("TimeLoopRing_Body/Interactibles_TimeLoopRing_Hidden/Prefab_NOM_Computer_ATP/Ring6").GetComponent<NomaiComputerRing>().Deactivate(0.5f);
        }

        public void SolanumGreetingsDB()
        {
            SolanumAnimController solanumAnimController = SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Nomai_ANIM_SkyWatching_Idle").GetComponent<SolanumAnimController>();
            solanumAnimController.StartWatchingPlayer();
            solanumAnimController.StartWritingMessage();
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Volumes_VesselDimension/VesselDiscoveryMusicTrigger").SetActive(false);

            PlaySadNomaiTheme();

            Invoke("SolanumDBEvent", 8f);
            Invoke("SolanumDBEventEnd", 10f);
        }

        public void SolanumDBEvent()
        {
            // SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:RT_Arm_ClavicleSHJnt/Nomai_Rig_v01:RT_Arm_ShoulderSHJnt/Nomai_Rig_v01:RT_Arm_ElbowSHJnt/Nomai_Rig_v01:RT_Arm_WristSHJnt").transform.parent = null;

            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 2").GetComponent<NomaiTextLine>()._state = NomaiTextLine.VisualState.UNREAD;
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 3").GetComponent<NomaiTextLine>()._state = NomaiTextLine.VisualState.UNREAD;
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 4").GetComponent<NomaiTextLine>()._state = NomaiTextLine.VisualState.UNREAD;
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 5").GetComponent<NomaiTextLine>()._state = NomaiTextLine.VisualState.UNREAD;
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 6").GetComponent<NomaiTextLine>()._state = NomaiTextLine.VisualState.UNREAD;
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 7").GetComponent<NomaiTextLine>()._state = NomaiTextLine.VisualState.UNREAD;
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 8").GetComponent<NomaiTextLine>()._state = NomaiTextLine.VisualState.UNREAD;
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 9").GetComponent<NomaiTextLine>()._state = NomaiTextLine.VisualState.UNREAD;
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 10").GetComponent<NomaiTextLine>()._state = NomaiTextLine.VisualState.UNREAD;
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 11").GetComponent<NomaiTextLine>()._state = NomaiTextLine.VisualState.UNREAD;
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 12").GetComponent<NomaiTextLine>()._state = NomaiTextLine.VisualState.UNREAD;
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 13").GetComponent<NomaiTextLine>()._state = NomaiTextLine.VisualState.UNREAD;
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 14").GetComponent<NomaiTextLine>()._state = NomaiTextLine.VisualState.UNREAD;
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 15").GetComponent<NomaiTextLine>()._state = NomaiTextLine.VisualState.UNREAD;
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 16").GetComponent<NomaiTextLine>()._state = NomaiTextLine.VisualState.UNREAD;

            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 2").GetComponent<NomaiTextLine>().SetActive(true, true, 10f);
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 3").GetComponent<NomaiTextLine>().SetActive(true, true, 10f);
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 4").GetComponent<NomaiTextLine>().SetActive(true, true, 10f);
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 5").GetComponent<NomaiTextLine>().SetActive(true, true, 10f);
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 6").GetComponent<NomaiTextLine>().SetActive(true, true, 10f);
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 7").GetComponent<NomaiTextLine>().SetActive(true, true, 10f);
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 8").GetComponent<NomaiTextLine>().SetActive(true, true, 10f);
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 9").GetComponent<NomaiTextLine>().SetActive(true, true, 10f);
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 10").GetComponent<NomaiTextLine>().SetActive(true, true, 10f);
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 11").GetComponent<NomaiTextLine>().SetActive(true, true, 10f);
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 12").GetComponent<NomaiTextLine>().SetActive(true, true, 10f);
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 13").GetComponent<NomaiTextLine>().SetActive(true, true, 10f);
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 14").GetComponent<NomaiTextLine>().SetActive(true, true, 10f);
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 15").GetComponent<NomaiTextLine>().SetActive(true, true, 10f);
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 16").GetComponent<NomaiTextLine>().SetActive(true, true, 10f);

        }
        public void SolanumDBEventEnd()
        {
            SolanumAnimController solanumAnimController = SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Nomai_ANIM_SkyWatching_Idle").GetComponent<SolanumAnimController>();
            solanumAnimController.StartWatchingPlayer();
            solanumAnimController.StopWritingMessage(true);

            
        }

        // Delete if works
        public void SolanumGreetingsTH()
        {
            SolanumAnimController solanumAnimController2 = SearchUtilities.Find("TimberHearth_Body/Sector_TH/Nomai_ANIM_SkyWatching_Idle").GetComponent<SolanumAnimController>();
            solanumAnimController2.StartWatchingPlayer();
            solanumAnimController2.StartConversation();
        }


        public void SolanumGreetingsBH()
        {
            SolanumAnimController solanumAnimController = SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character").GetComponent<SolanumAnimController>();
            solanumAnimController.StartWatchingPlayer();
            solanumAnimController.PlayGestureToWordStones();
        }

        public void SolanumEventBH()
        {
            SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Solanum_BH_Crystal/CapsuleVolume_NOM_GravityCrystal").GetComponent<CapsuleCollider>().enabled = true;

            var crystallGravity = SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Solanum_BH_Crystal/CapsuleVolume_NOM_GravityCrystal").GetComponent<DirectionalForceVolume>();
            crystallGravity.SetFieldMagnitude(-0.2f);

            Invoke("SolanumEventBHend", 4f);
            Invoke("PlayCrackSound", 5.5f);       
        }

        public void SolanumEventBHend()
        {
            SolanumAnimController solanumAnimController = SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character").GetComponent<SolanumAnimController>();
            solanumAnimController.PlayGestureToCairns();
        }

        public void SolanumGreetingsET()
        {
            SolanumAnimController solanumAnimController = SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/Solanum_EmberTwin_Character").GetComponent<SolanumAnimController>();
            solanumAnimController.StartWatchingPlayer();
            solanumAnimController.PlayGestureToWordStones();
        }
        public void SolanumStartEventET()
        {
            SolanumAnimController solanumAnimController = SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/Solanum_EmberTwin_Character").GetComponent<SolanumAnimController>();
            solanumAnimController.PlayRaiseCairns();            
        }
        public void SolanumEventET()
        {
            Invoke("SolanumStartEventET", 3f);

            var pyramid = SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/Solanum_EmberTwin_ToyPyramid").gameObject.AddComponent<SmoothMoving>();
            var mobius = SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/Solanum_EmberTwin_ToyPyramid/Solanum_EmberTwin_ToyMobius").gameObject.AddComponent<SmoothMoving>();
            var ship = SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/Solanum_EmberTwin_ToyPyramid/Solanum_EmberTwin_ToyShip").gameObject.AddComponent<SmoothMoving>();

            pyramid.targetPos = new Vector3(35.1789f, -105.9786f, -23.1699f);
            pyramid.delay = 5f;
            pyramid.rotationSpeed = 40f;
            pyramid.Start();

            mobius.targetPos = new Vector3(0.7838f, -0.1231f, 0.6973f);
            mobius.delay = 5.5f;
            mobius.rotationSpeed = -35f;
            mobius.Start();

            ship.targetPos = new Vector3(0.4714f, 0.744f, 0.1487f);
            ship.delay = 6f;
            ship.rotationSpeed = 15f;
            ship.Start();

            Invoke("PlayRaiseCairn", 4.5f);
            Invoke("PlayExitRaiseCairn", 6.3f);
            Invoke("PlayStepSound", 4f);
            Invoke("SolanumETEndEvent", 17f);           
            Invoke("SolanumEventET_SandPileEnd", 19f);
        }       

        public void SolanumEventET_SandPileEnd()
        {
            SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/SandPile_1").gameObject.SetActive(true);
            SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/SandPile_2").gameObject.SetActive(true);
            SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/SandPile_3").gameObject.SetActive(true);
        }
        public void SolanumETEndEvent()
        {
            SolanumAnimController solanumAnimController = SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/Solanum_EmberTwin_Character").GetComponent<SolanumAnimController>();
            solanumAnimController.PlayGestureToCairns(); 
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
                SearchUtilities.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/Character_NOM_Solanum/ConversationZone").SetActive(false);

            });

            // Vision Torch 2 picking up
            TheVision.Instance.ModHelper.Events.Unity.RunWhen(() => !visionTorchTaken2._active, () =>
            {

                visionTorchMesh2.SetActive(true);
                visionTorchActive2.SetActive(false);
                SearchUtilities.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/Character_NOM_Solanum/ConversationZone").SetActive(false);

            });

        }
        // Load StartProps and DisabledPropsOnStart
        public void OnStarSystemLoaded(string systemName)
        {
            ModHelper.Console.WriteLine("LOADED SYSTEM " + systemName);

            if (systemName == "SolarSystem")
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

        public void EyeOfTheUniverseProps()
        {
            var _vessel = GameObject.Find("Vessel_Body");
            var _vesselSector = GameObject.Find("Vessel_Body/Sector_VesselBridge").GetComponent<Sector>();

            string path = "EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Campfire/Campsite/Solanum/Character_NOM_Solanum/Nomai_ANIM_SkyWatching_Idle";
            Vector3 position = new Vector3(-2.836f, -7.0145f, 5.3782f);
            Vector3 rotation = new Vector3(357.0274f, 15.49f, 0f);
            DetailBuilder.Make(_vessel, _vesselSector, new DetailInfo
            {
                path = path,
                position = position,
                rotation = rotation
            });

            string path2 = "Vessel_Body/Sector_VesselBridge/Interactibles_VesselBridge/WarpController/WarpCoreSocket/Prefab_NOM_WarpCoreVessel/Effects_NOM_AdvancedWarpCore/Effects_NOM_WarpParticlesWhite";
            Vector3 position2 = new Vector3(-2.7f, -4.8872f, 5.8744f);
            Vector3 rotation2 = new Vector3(351.6485f, 10.514f, 355.3143f);
            DetailBuilder.Make(_vessel, _vesselSector, new DetailInfo
            {
                path = path2,
                position = position2,
                rotation = rotation2,
                scale = 5
            });

            string path3 = "Vessel_Body/Sector_VesselBridge/Interactibles_VesselBridge/WarpController/WarpCoreSocket/Prefab_NOM_WarpCoreVessel/Effects_NOM_AdvancedWarpCore/Effects_NOM_WarpParticlesWhite";
            Vector3 position3 = new Vector3(-2.7f, -4.8872f, 5.8744f);
            Vector3 rotation3 = new Vector3(351.6485f, 10.514f, 355.3143f);
            DetailBuilder.Make(_vessel, _vesselSector, new DetailInfo
            {
                path = path3,
                position = position2,
                rotation = rotation2,
                scale = 5
            });           

        }

        

        // Function for teleporting ship to TH State on QM so player can continue the journey
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

                Vector3 originalVelocity = qm_rb.GetAngularVelocity();

                Vector3 newPosition = qm_rb.transform.TransformPoint(new Vector3(30f, -100f, 0f));

                s_rb.SetPosition(newPosition);
                s_rb.SetRotation(Quaternion.LookRotation(qm_rb.transform.forward, -qm_rb.transform.up));
                s_rb.SetVelocity(qm_rb.GetPointVelocity(newPosition));
                s_rb.SetAngularVelocity(qm_rb.GetAngularVelocity());

                TheVision.Instance.ModHelper.Console.WriteLine("Ship teleported!");
                s_dmg._invincible = originalInvicibility;

                
            });
        }
        public void SpawnOnVisionEnd()
        {
            ModHelper.Events.Unity.FireOnNextUpdate(() =>
            {
                // Playing SFX
                PlayRevealSound();
                PlaySFXSound();
                PlayGaspSound();
                PlayThunderSound();
            });            


            // Enabling json props
            DisabledPropsOnStart(true);

            // Disabling QM WhiteHole
            Locator.GetAstroObject(AstroObject.Name.QuantumMoon).transform.Find("Sector_QuantumMoon/WhiteHole").gameObject.SetActive(false);

            var cameraFixedPosition = Locator.GetPlayerTransform().gameObject.GetComponent<PlayerLockOnTargeting>();
            cameraFixedPosition.BreakLock(0.5f);

            TeleportShip();

            SolanumAnimController solanumAnimController2 = SearchUtilities.Find("TimberHearth_Body/Sector_TH/Nomai_ANIM_SkyWatching_Idle").GetComponent<SolanumAnimController>();
            solanumAnimController2.StartWatchingPlayer();
            solanumAnimController2.StartConversation();


            var learnSignal = SearchUtilities.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/Character_NOM_Solanum/Signal_Solanum").GetComponent<AudioSignal>();
            learnSignal.IdentifyFrequency();
            learnSignal.IdentifySignal();


            TheVision.Instance.ModHelper.Events.Unity.RunWhen(() => Locator.GetShipLogManager() != null && Locator.GetShipLogManager().IsFactRevealed("SOLANUM_BH_EVENT"), () =>
            {
                Invoke("SolanumEventBH", 1f);
            });


            TheVision.Instance.ModHelper.Events.Unity.RunWhen(() => Locator.GetShipLogManager() != null && Locator.GetShipLogManager().IsFactRevealed("SOLANUM_GD_RECORDER"), () =>
            {
                Invoke("SolanumGreetingsGD", 1f);

            });

            TheVision.Instance.ModHelper.Events.Unity.RunWhen(() => Locator.GetShipLogManager() != null && Locator.GetShipLogManager().IsFactRevealed("SOLANUM_ET_EVENT"), () =>
            {
                Invoke("SolanumEventET", 3f);
            });

            TheVision.Instance.ModHelper.Events.Unity.RunWhen(() => Locator.GetShipLogManager() != null && Locator.GetShipLogManager().IsFactRevealed("SOLANUM_ATP_RECORDER"), () =>
            {
                SolanumGreetingsATP();
            });

            TheVision.Instance.ModHelper.Events.Unity.RunWhen(() => Locator.GetShipLogManager() != null && Locator.GetShipLogManager().IsFactRevealed("SOLANUM_ATP_RECORDER_2"), () =>
            {
                SolanumGreetingsATP();

            });

            TheVision.Instance.ModHelper.Events.Unity.RunWhen(() => Locator.GetShipLogManager() != null && Locator.GetShipLogManager().IsFactRevealed("IS_HOLOGRAM_CHANGED"), () =>
            {
                SolanumGreetingsDB();
            });


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

            var signalDB_body = SearchUtilities.Find("DarkBramble_Body/Sector_DB/Signal_Solanum").gameObject;
            signalDB_body.SetActive(isActive);

            var signalDB_hub = SearchUtilities.Find("DB_HubDimension_Body/Sector_HubDimension/Interactables_HubDimension/Pivot/InnerWarp_ToAnglerNest/Signal_Solanum").gameObject;
            signalDB_hub.SetActive(isActive);

            var signalDB_nest = SearchUtilities.Find("DB_AnglerNestDimension_Body/Sector_AnglerNestDimension/Interactables_AnglerNestDimension/InnerWarp_ToVessel/Signal_Solanum").gameObject;
            signalDB_nest.SetActive(isActive);            

            var particlesQM = SearchUtilities.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/Character_NOM_Solanum/Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Effects_NOM_WarpParticlesWhite").gameObject;
            particlesQM.SetActive(isActive);

            var particlesQM_2 = SearchUtilities.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/Character_NOM_Solanum/Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Particles_2").gameObject;
            particlesQM_2.SetActive(isActive);

            var signalATP = SearchUtilities.Find("TimeLoopRing_Body/Interactibles_TimeLoopRing/Signal_Solanum");
            signalATP.SetActive(isActive);

            var solanumATP = SearchUtilities.Find("TimeLoopRing_Body/Characters_TimeLoopRing/Nomai_ANIM_SkyWatching_Idle");
            solanumATP.SetActive(isActive);

            var solanumBH = SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character");
            solanumBH.SetActive(isActive);

            var BHreveal = SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Reveal Volume (Enter)");
            BHreveal.SetActive(isActive);

            var DBreveal = SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Reveal Volume (Enter)");
            DBreveal.SetActive(isActive);

            var solanumET = SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/Solanum_EmberTwin_Character");
            solanumET.SetActive(isActive);

            var ETreveal = SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/Reveal Volume (Enter)");
            ETreveal.SetActive(isActive);

            var ETrecorder = SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/Prefab_NOM_Recorder_ET").gameObject;
            ETrecorder.SetActive(isActive);

            var ATPhidden = SearchUtilities.Find("TimeLoopRing_Body/Interactibles_TimeLoopRing_Hidden").transform;

            var ATPrecorder = SearchUtilities.Find("TimeLoopRing_Body/Interactibles_TimeLoopRing/Prefab_NOM_Recorder_ATP").gameObject;
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

            
            ATPrecorder.transform.localPosition = new Vector3(26.31f, -2.63f, -7.83f);

            var THsignal = Locator.GetAstroObject(AstroObject.Name.TimberHearth).transform.Find("Sector_TH/Signal_Solanum").gameObject;
            THsignal.SetActive(isActive);           

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

               // SearchUtilities.Find("TimeLoopRing_Body/Interactibles_TimeLoopRing_Hidden/Prefab_NOM_Computer").gameObject.SetActive(true);

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

            ModHelper.Events.Unity.FireOnNextUpdate(() =>
            {                               
                GameObject.Find("Player_Body/PlayerCamera").gameObject.SetActive(false);
                DeathManager deathManager = Locator.GetDeathManager();
                deathManager.BeginEscapedTimeLoopSequence((TimeloopEscapeType)8486);
            });

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
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 0.2f);
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

        public void PlayRaiseCairn()
        {
            PlayerHeadsetAudioSource = SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/Solanum_EmberTwin_Character").AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.loop = false;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.SolanumEnterRaiseCairn);
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 0.5f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.Play(); 
        }

        public void PlayExitRaiseCairn()
        {
            PlayerHeadsetAudioSource = SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/Solanum_EmberTwin_Character").AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.SolanumExitRaiseCairn);
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 0.5f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.Play();
        }

        public void PlayStepSound()
        {
            PlayerHeadsetAudioSource = SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/Solanum_EmberTwin_Character").AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.SolanumStomp);
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 0.5f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.Play();
        }

        public void PlayFlickerSound()
        {
            PlayerHeadsetAudioSource = Locator.GetPlayerTransform().gameObject.AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.ToolFlashlightFlicker); // SingularityOnPlayerEnterExit = 2402            
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 15f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.PlayOneShot();
        }

        public void PlaySingularityCreateSound()
        {
            PlayerHeadsetAudioSource = Locator.GetPlayerTransform().gameObject.AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.SingularityCreate); // SingularityOnPlayerEnterExit = 2402            
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 15f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.PlayOneShot();
        }

        public void PlaySingularityCollapseSound()
        {
            PlayerHeadsetAudioSource = Locator.GetPlayerTransform().gameObject.AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.SingularityCollapse); // SingularityOnPlayerEnterExit = 2402            
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 15f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.PlayOneShot();
        }

        public void PlaySecretSound()
        {
            PlayerHeadsetAudioSource = Locator.GetPlayerTransform().gameObject.AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.SecretKorok); // SingularityOnPlayerEnterExit = 2402            
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 15f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.Play();
        }

        public void PlaySadNomaiTheme()
        {
            PlayerHeadsetAudioSource = Locator.GetMinorAstroObject("Vessel Dimension").gameObject.AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.SadNomaiTheme); // SingularityOnPlayerEnterExit = 2402            
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 15f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.Play();
        }

        public void PlayCrackSound()
        {
            SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Solanum_BH_Crystal/Props_NOM_GravityCrystal").SetActive(false);
            SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Solanum_BH_Crystal/CapsuleVolume_NOM_GravityCrystal").SetActive(false);
            SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Solanum_BH_Crystal_Cracked").SetActive(true);
            SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Solanum_BH_Crystal/AudioSource_GravityCrystal").SetActive(false);

            PlayerHeadsetAudioSource = SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Solanum_BH_Crystal_Cracked").AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.PlayerSuitHelmetCrack);
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 0.5f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.Play();            
        }




    }
}
