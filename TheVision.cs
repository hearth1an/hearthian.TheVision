using OWML.Common;
using OWML.ModHelper;
using UnityEngine;
using TheVision.Utilities.ModAPIs;
using static NewHorizons.External.Modules.PropModule;
using static NewHorizons.External.Modules.SignalModule;
using NewHorizons.Builder.Props;
using System.Linq;
using TheVision.CustomProps;
using HarmonyLib;
using System.Reflection;
using NewHorizons.Utility;
using NewHorizons.Handlers;
 

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
            ModHelper.Events.Unity.RunWhen(() => EntitlementsManager.IsDlcOwned() != EntitlementsManager.AsyncOwnershipStatus.NotReady, () =>
            {
                if (EntitlementsManager.IsDlcOwned() != EntitlementsManager.AsyncOwnershipStatus.Owned)
                {
                    ModHelper.Console.WriteLine("The Vision requires DLC owned. DLC not found.", MessageType.Fatal);
                }
            });

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            var menuFrameworkAPI = ModHelper.Interaction.GetModApi<IMenuAPI>("_nebula.MenuFramework");
            var newHorizonsAPI = ModHelper.Interaction.GetModApi<INewHorizons>("xen.NewHorizons");
            newHorizonsAPI.GetStarSystemLoadedEvent().AddListener(OnStarSystemLoaded);
            newHorizonsAPI.LoadConfigs(this);            

            ModHelper.Console.WriteLine($"{nameof(TheVision)} is loaded!", MessageType.Success);

            TitleProps();

            LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
            {
                if (loadScene == OWScene.EyeOfTheUniverse)
                {
                    CheckEyeOfTheUniverseProps();
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
        // Spawns control
        private void TitleProps()
        {
            GameObject.Find("Scene/Background/PlanetPivot/Prefab_HEA_Campfire/Effects/Effects_HEA_SmokeColumn/Effects_HEA_SmokeColumn_Title").GetComponent<MeshRenderer>().material.color = new Color(1, 2, 2, 1);
            GameObject.Find("Scene/Background/PlanetPivot/Prefab_HEA_Campfire/Props_HEA_Campfire/Campfire_Flames").GetComponent<MeshRenderer>().material.color = new Color(0, 5, 4, 1);
            GameObject.Find("Scene/Background/PlanetPivot/Prefab_HEA_Campfire/Props_HEA_Campfire/Campfire_Embers").SetActive(false);
        }
        private void OnStarSystemLoaded(string systemName)
        {
            ModHelper.Console.WriteLine("LOADED SYSTEM " + systemName);

            if (systemName == "SolarSystem")
            {
                SpawnStartProps();                               
            }
            if (systemName == "EyeOfTheUniverse")
            {
                CheckEyeOfTheUniverseProps();
            }
            if (systemName == "GloamingGalaxy")
            {
                EndGame();
            }
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

            // Setting ship dialogue trigger

            ModHelper.Events.Unity.RunWhen(() => Locator.GetShipLogManager() != null, () =>
            {
                if (!Locator.GetShipLogManager().IsFactRevealed("SOLANUM_PROJECTION_COMPLETE"))
                {   
                    SearchUtilities.Find("Ship_Body/ShipSector/ConversationZone").SetActive(false);
                    SearchUtilities.Find("Ship_Body/ShipSector/ConversationTrigger").SetActive(false);
                }
            });
            

            SearchUtilities.Find("Ship_Body/ShipSector/VisionTorchSocket").GetComponent<SphereCollider>().radius = 1f;

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

            // Disabling Ernesto enter volume and Slate's note
            SearchUtilities.Find("TimberHearth_Body/Sector_TH/Reveal Volume (Enter)").SetActive(false);
            SearchUtilities.Find("TimberHearth_Body/Sector_TH/Prefab_HEA_Journal").SetActive(false);
            SearchUtilities.Find("TimberHearth_Body/Sector_TH/ConversationZone").SetActive(false);
            SearchUtilities.Find("TimberHearth_Body/Sector_TH/Prefab_HEA_Journal/InteractVolume").SetActive(false);

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
                    ErnestoQuestEntry();
                    SearchUtilities.Find("ScreenPromptCanvas/ScreenPromptListBottomLeft/ScreenPrompt").SetActive(true);
                }
            });

            // Particles QM and TH
            SearchUtilities.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/Character_NOM_Solanum/Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Effects_NOM_WarpParticlesWhite").transform.localPosition = new Vector3(-0.3f, -0.4f, 0f);
            SearchUtilities.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/Character_NOM_Solanum/Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Particles_2").transform.localPosition = new Vector3(-0.3f, -0.4f, 0f);


            SearchUtilities.Find("TimberHearth_Body/Sector_TH/Effects_NOM_WarpParticlesWhite").transform.parent = SearchUtilities.Find("TimberHearth_Body/Sector_TH/Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt").transform.parent;
            SearchUtilities.Find("TimberHearth_Body/Sector_TH/Particles_2").transform.parent = SearchUtilities.Find("TimberHearth_Body/Sector_TH/Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt").transform.parent;
            SearchUtilities.Find("TimberHearth_Body/Sector_TH/Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Effects_NOM_WarpParticlesWhite").transform.localPosition = new Vector3(-0.3f, -0.4f, 0f);
            SearchUtilities.Find("TimberHearth_Body/Sector_TH/Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Particles_2").transform.localPosition = new Vector3(-0.3f, -0.4f, 0f);

            // Particles GD
            SearchUtilities.Find("GiantsDeep_Body/Sector_GD/Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Effects_NOM_WarpParticlesWhite").transform.localPosition = new Vector3(-0.3f, -0.4f, 0f);
            SearchUtilities.Find("GiantsDeep_Body/Sector_GD/Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Particles_2").transform.localPosition = new Vector3(-0.3f, -0.4f, 0f);

            // Particles GD (teleported copy)
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
            SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/Prefab_NOM_Recorder_ET/PointLight_NOM_Recorder").GetComponent<Light>().intensity = 1f;
            SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/Prefab_NOM_Recorder_ET/PointLight_NOM_Recorder").transform.position = new Vector3(1.6236f, 2.8635f, 1.1965f);

            // Solanum signals in DB
            SearchUtilities.Find("DB_HubDimension_Body/Sector_HubDimension/Interactables_HubDimension/Pivot/InnerWarp_ToAnglerNest/Signal_Solanum").transform.localPosition = new Vector3(0f, 0f, 0f);
            SearchUtilities.Find("DB_AnglerNestDimension_Body/Sector_AnglerNestDimension/Interactables_AnglerNestDimension/InnerWarp_ToVessel/Signal_Solanum").transform.localPosition = new Vector3(0f, 0f, 0f);

            // Positions and rotations for BH event
            SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Prefab_NOM_Recorder_BH").transform.localPosition = new Vector3(1.3f, 1f, 1.2f);
            SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/Solanum_EmberTwin_Character/Signal_Solanum").transform.localPosition = new Vector3(0f, 0f, 0f);
            SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Signal_Solanum").transform.localPosition = new Vector3(0f, 0f, 0f);
            SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Solanum_BH_Crystal").transform.localPosition = new Vector3(-1.8564f, -0.1f, 0.7f);
            SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Solanum_BH_Crystal").transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
            SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Solanum_BH_Crystal_Cracked").transform.localPosition = new Vector3(-1.8564f, -0.1f, 0.7f);
            SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Solanum_BH_Crystal_Cracked").transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
            SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Solanum_BH_Crystal_Cracked").SetActive(false);
            SearchUtilities.Find("GiantsDeep_Body/Sector_GD/BlackHole/DestructionVolume").gameObject.SetActive(false);

            // For ET event
            SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/SandPile_1").gameObject.SetActive(false);
            SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/SandPile_2").gameObject.SetActive(false);
            SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/SandPile_3").gameObject.SetActive(false);
            SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/SandPile_4").gameObject.SetActive(false);

            // GD teleportation event
            SearchUtilities.Find("GiantsDeep_Body/Sector_GD/BlackHole").gameObject.SetActive(false);
            SearchUtilities.Find("GiantsDeep_Body/Sector_GD/WhiteHole").gameObject.SetActive(false);
            SearchUtilities.Find("GiantsDeep_Body/Sector_GD/SolanumTeleportation").gameObject.SetActive(false);
            SearchUtilities.Find("GiantsDeep_Body/Sector_GD/SolanumTeleportation/Signal_Solanum").transform.localPosition = new Vector3(0f, 0f, 0f);
            SearchUtilities.Find("GiantsDeep_Body/Sector_GD/Sector_GDInterior/Sector_GDCore/Sector_Module_Sunken/Effects_Module_Sunken/SunkenModuleWater_ExteriorStencil").gameObject.SetActive(true);
            SearchUtilities.Find("GiantsDeep_Body/Sector_GD/Nomai_ANIM_SkyWatching_Idle/Signal_Solanum").transform.localPosition = new Vector3(0f, 0f, 0f);

            PickUpTorch();
            DisabledPropsOnStart(false);

            ModHelper.Events.Unity.RunWhen(() => Locator.GetShipLogManager() != null, () =>
            {
                if (Locator.GetShipLogManager().IsFactRevealed("SOLANUM_PROJECTION_COMPLETE"))
                {
                    DisabledPropsOnStart(true);

                    PlayerData.SetPersistentCondition("MET_SOLANUM", true);
                    PlayerData.SetPersistentCondition("MET_PRISONER", true);
                    PlayerData.SetPersistentCondition("SOLANUM_PROJECTION_COMPLETE", true);

                    SearchUtilities.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/Character_NOM_Solanum/ConversationZone").GetComponent<InteractReceiver>()._hasInteracted = true;
                    SearchUtilities.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/Character_NOM_Solanum/ConversationZone").GetComponent<InteractReceiver>()._isInteractPressed = true;
                }
            });

            // Fact checker & loader on start for each event   
            TheVision.Instance.ModHelper.Events.Unity.RunWhen(() => Locator.GetShipLogManager() != null && Locator.GetShipLogManager().IsFactRevealed("SOLANUM_ET_FOUND"), () =>
            {
                SolanumGreetingsET();
            });
            TheVision.Instance.ModHelper.Events.Unity.RunWhen(() => Locator.GetShipLogManager() != null && Locator.GetShipLogManager().IsFactRevealed("SOLANUM_BH_FOUND"), () =>
            {
                SolanumGreetingsBH();
            });
            TheVision.Instance.ModHelper.Events.Unity.FireOnNextUpdate(() =>
            {
                if (Locator.GetShipLogManager().IsFactRevealed("SOLANUM_PROJECTION_COMPLETE"))
                {
                    SolanumGreetingsTH();
                    ParentBrokenCore();
                    ATPfix();
                }
                if (Locator.GetShipLogManager().IsFactRevealed("SOLANUM_PROJECTION_COMPLETE") && !Locator.GetShipLogManager().IsFactRevealed("IS_HOLOGRAM_CHANGED"))
                {
                    SolanumGreetingsDB();
                }
                if (Locator.GetShipLogManager().IsFactRevealed("SOLANUM_PROJECTION_COMPLETE") && !Locator.GetShipLogManager().IsFactRevealed("SOLANUM_GD_RECORDER"))
                {
                    SolanumGreetingsGD();
                }
                if (Locator.GetShipLogManager().IsFactRevealed("SOLANUM_PROJECTION_COMPLETE") && !Locator.GetShipLogManager().IsFactRevealed("SOLANUM_ATP_RECORDER"))
                {
                    SolanumGreetingsATP();
                }
                if (Locator.GetShipLogManager().IsFactRevealed("SOLANUM_PROJECTION_COMPLETE") && !Locator.GetShipLogManager().IsFactRevealed("SOLANUM_ATP_RECORDER_2"))
                {
                    SolanumGreetingsATP_2();
                }
            });
        }

        public void ATPfix()
        {
           // ATP memory animation link fix
            var dataStream = SearchUtilities.Find("TimeLoopRing_Body/Effects_TimeLoopRing/Effect 2/Effects_NOM_TimeLoopDataStream");
            dataStream.transform.localRotation = new Quaternion(0.8899f, 0, 0, 0.4562f);
            dataStream.SetActive(false);

            var newMask = SearchUtilities.Find("TimeLoopRing_Body/Effects_TimeLoopRing/Effect 2/centralPulse 1");
            newMask.transform.localRotation = new Quaternion(0.0274f, -0.0353f, -0.7298f, -0.6822f);
            newMask.transform.localPosition = new Vector3(22.217f, -6.8984f, 5.1757f);
            newMask.SetActive(false);

            var checkOtherPulse = SearchUtilities.Find("TimeLoopRing_Body/Effects_TimeLoopRing/centralPulse 1");
            
            TheVision.Instance.ModHelper.Events.Unity.RunWhen(() => checkOtherPulse.gameObject.activeSelf == true, () =>
            {
                dataStream.SetActive(true);
                newMask.SetActive(true);
            });
        }

        public void CheckEyeOfTheUniverseProps()
        {
            TheVision.Instance.ModHelper.Events.Unity.FireOnNextUpdate(() =>
            {
                if (PlayerData.GetPersistentCondition("SOLANUM_PROJECTION_COMPLETE") != true)
            {
                SearchUtilities.Find("Vessel_Body/Sector_VesselBridge/Nomai_ANIM_SkyWatching_Idle").SetActive(false);
                SearchUtilities.Find("Vessel_Body/Sector_VesselBridge/VesselText").SetActive(false);
                SearchUtilities.Find("Vessel_Body/Sector_VesselBridge/ConversationZone").SetActive(false);
                SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/EyeText").SetActive(false);
                SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/ConversationZone").SetActive(false);
                SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/SixthPlanet_Root/Sector_EyeSurface/Nomai_ANIM_SkyWatching_Idle").SetActive(false);
                SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Observatory/FillLight_Statue").SetActive(false);
                SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Observatory/Prefab_HEA_MuseumPlaque").SetActive(false);
                SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Observatory/SolanumObservatory").SetActive(false);
                SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Campfire/Campsite/Solanum/Character_NOM_Solanum/Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Particles_1").SetActive(false);
                SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Campfire/Campsite/Solanum/Character_NOM_Solanum/Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Particles_2").SetActive(false);

                ModHelper.Console.WriteLine("Fact is not revealed", MessageType.Warning);
                }
                if (PlayerData.GetPersistentCondition("SOLANUM_PROJECTION_COMPLETE") == true)
                {
                    EyeOfTheUniverseProps();
                    ModHelper.Console.WriteLine("Fact is revealed", MessageType.Success);
                }              

            });
        }
        public void EyeOfTheUniverseProps()
        {
            var fillLight = SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Observatory/FillLight_Statue");
            fillLight.transform.localPosition = new Vector3(10.1989f, 1.717f, -2.4711f);
            fillLight.GetComponent<Light>().range = 4f;

            Invoke("SolDialogueFix", 1f);
            TheVision.Instance.ModHelper.Events.Unity.FireOnNextUpdate(() =>
            {
                SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Particles1").transform.parent = SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Observatory/SolanumObservatory/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt").transform.parent;
                SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Particles2").transform.parent = SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Observatory/SolanumObservatory/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt").transform.parent;

                var particles_1 = SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Observatory/SolanumObservatory/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Particles1");
                var particles_2 = SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Observatory/SolanumObservatory/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Particles2");

                particles_1.DestroyAllComponents<RotateTransform>();
                particles_2.DestroyAllComponents<RotateTransform>();

                SearchUtilities.Find("Vessel_Body/Sector_VesselBridge/Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Particles_1").transform.localPosition = new Vector3(-0.2f, -0.3f, 0);
                SearchUtilities.Find("Vessel_Body/Sector_VesselBridge/Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Particles_2").transform.localPosition = new Vector3(-0.2f, -0.3f, 0);
                SearchUtilities.Find("Vessel_Body/Sector_VesselBridge/Nomai_ANIM_SkyWatching_Idle/Signal_Solanum").transform.localPosition = new Vector3(0f, 0f, 0f);

                

                SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/SixthPlanet_Root/Sector_EyeSurface/Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Particles_1").transform.localPosition = new Vector3(-0.2f, -0.3f, 0);
                SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/SixthPlanet_Root/Sector_EyeSurface/Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Particles_2").transform.localPosition = new Vector3(-0.2f, -0.3f, 0);
                SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/SixthPlanet_Root/Sector_EyeSurface/Nomai_ANIM_SkyWatching_Idle/Signal_Solanum").transform.localPosition = new Vector3(0f, 0f, 0f);

                SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/SixthPlanet_Root/Sector_EyeSurface/Nomai_ANIM_SkyWatching_Idle").transform.localPosition = new Vector3(6.7729f, -220.9961f, -2.9422f);
                SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/SixthPlanet_Root/Sector_EyeSurface/Nomai_ANIM_SkyWatching_Idle").transform.localRotation = new Quaternion(-0.1325f, 0.5714f, -0.0941f, -0.8045f);

                SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Campfire/Campsite/Solanum/Character_NOM_Solanum/Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Particles_1").transform.localPosition = new Vector3(-0.2f, -0.3f, 0);
                SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Campfire/Campsite/Solanum/Character_NOM_Solanum/Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Particles_2").transform.localPosition = new Vector3(-0.2f, -0.3f, 0);

                SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Observatory/SolanumObservatory/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Particles1").transform.localPosition = new Vector3(-0.2f, -0.3f, 0);
                SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Observatory/SolanumObservatory/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Particles2").transform.localPosition = new Vector3(-0.2f, -0.3f, 0);

                SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Observatory/SolanumObservatory").transform.localPosition = new Vector3(12.1105f, -0.5f, -2.6655f);
                SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Observatory/SolanumObservatory").transform.localRotation = new Quaternion(-0.016f, 0.5714f, -0.0941f, -0.8045f);

                SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Campfire/Campsite/Solanum/ConversationVolume_Solanum").gameObject.SetActive(false);
                SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Campfire/Campsite/Solanum/ConversationZone").gameObject.SetActive(true);

                var solVortex = SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/SixthPlanet_Root/Sector_EyeSurface/Nomai_ANIM_SkyWatching_Idle");
                solVortex.transform.localPosition = new Vector3(-8.7184f, -220.6731f, 0.5104f);
                solVortex.transform.localRotation = new Quaternion(-0.0501f, -0.8016f, 0.1135f, -0.5849f);

                SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Observatory/Prefab_HEA_MuseumPlaque/InteractVolume").SetActive(false);
                var solanumDialogue = SearchUtilities.Find("Vessel_Body/Sector_VesselBridge/ConversationZone");

                var paper = SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Observatory/Prefab_HEA_MuseumPlaque/Props_HEA_MuseumPlaque_Geo/plaque_paper_1");

                var sign = SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Observatory/Prefab_HEA_MuseumPlaque");
                sign.transform.localPosition = new Vector3(9.5914f, -0.3883f, -2.7425f);
                sign.transform.localRotation = new Quaternion(0f, 0.6984f, 0f, -0.7157f);

                SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/ConversationZone").transform.parent = paper.transform.parent;
                SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/ConversationZone").transform.position = paper.transform.position;

                var solanumCampfire = SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Campfire/Campsite/Solanum");
                var solanumCampfireDialogue = SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Campfire/Campsite/Solanum/ConversationVolume_Solanum");
                solanumCampfireDialogue.SetActive(false);
                solanumDialogue.transform.parent = solanumCampfireDialogue.transform.parent;
                solanumDialogue.transform.localPosition = solanumCampfireDialogue.transform.localPosition;

                var solObservatory = SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Observatory/SolanumObservatory");
                solObservatory.transform.localPosition = new Vector3(11.7207f, -0.4757f, -2.871f);
                solObservatory.transform.localRotation = new Quaternion(0f, 0.6984f, 0f, -0.7157f);
            });

            PlayerData.SetPersistentCondition("MET_SOLANUM", true);
            PlayerData.SetPersistentCondition("MET_PRISONER", true);
            
            ModHelper.Events.Unity.RunWhen(() => Locator.GetEyeStateManager().GetState() == EyeState.IntoTheVortex, () =>
             {
                 ModHelper.Console.WriteLine("Weeeee!", MessageType.Success);
                 var solVortex = SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/SixthPlanet_Root/Sector_EyeSurface/Nomai_ANIM_SkyWatching_Idle");

                 solVortex.AddComponent<OWRigidbody>();
                 Invoke("SolRigidbody", 0.1f);

                 SearchUtilities.Find("Nomai_ANIM_SkyWatching_Idle").transform.localRotation = new Quaternion(-0.7782f, -0.1391f, -0.6053f, 0.0934f);
                 SearchUtilities.Find("Nomai_ANIM_SkyWatching_Idle").GetComponent<Animator>().enabled = false;
                 SearchUtilities.Find("Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt").transform.localRotation = new Quaternion(0.0085f, -0.0149f, -0.3902f, 0.9206f);
                 SearchUtilities.Find("Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:LF_Arm_ClavicleSHJnt").transform.localRotation = new Quaternion(0.1752f, 0.7759f, 0.3028f, -0.5251f);
                 SearchUtilities.Find("Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:RT_Arm_ClavicleSHJnt").transform.localRotation = new Quaternion(-0.4354f, 0.5514f, -0.4156f, -0.5776f);
            });           

            ModHelper.Events.Unity.RunWhen(() => Locator.GetEyeStateManager().GetState() == EyeState.Observatory, () =>
            {
                SearchUtilities.Find("SolanumFalling").gameObject.SetActive(false);
                Invoke("StopAnimEyeScene", 3f);
            });          
        }
        

        public void SolRigidbody()
        {
            var solVortex2 = SearchUtilities.Find("Nomai_ANIM_SkyWatching_Idle");
            solVortex2.transform.name = "SolanumFalling";
            solVortex2.DestroyAllComponents<CenterOfTheUniverseOffsetApplier>();
            solVortex2.GetComponent<OWRigidbody>().AddLocalForce(new Vector3(0f, 2f, 0f));

        }
        public void SolDialogueFix()
        {
            SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Campfire/Campsite/Solanum/ConversationVolume_Solanum").SetActive(false);
            SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/SixthPlanet_Root/Sector_EyeSurface/Nomai_ANIM_SkyWatching_Idle/Signal_Solanum").transform.localPosition = new Vector3(0f, 0f, 0f);
            var newDialogue = SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Campfire/Campsite/Solanum/ConversationZone");
            newDialogue.SetActive(true);

            var campsiteController = SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Campfire").GetComponent<QuantumCampsiteController>();
            

            ModHelper.Events.Unity.RunWhen(() => campsiteController.AreAllTravelersGathered() == true, () =>
            {
                SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Campfire/Campsite/Solanum/ConversationZone").SetActive(false);
                SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Campfire/Campsite/Solanum/ConversationVolume_Solanum").SetActive(true);
               

            });

        }
        public void StopAnimEyeScene()
        {
            SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Observatory/SolanumObservatory").GetComponent<Animator>().enabled = false;
            var particles_1 = SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Observatory/SolanumObservatory/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Particles1");
            var particles_2 = SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Observatory/SolanumObservatory/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:Neck_01SHJnt/Particles2");

            SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Observatory/Prefab_HEA_MuseumPlaque/Props_HEA_MuseumPlaque_Collider").GetComponent<MeshCollider>().enabled = true;
            SearchUtilities.Find("EyeOfTheUniverse_Body/Sector_EyeOfTheUniverse/Sector_Observatory/SolanumObservatory/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt").GetComponent<CapsuleCollider>().enabled = true;


            TheVision.Instance.ModHelper.Events.Unity.FireOnNextUpdate(() =>
            {
                particles_1.GetComponent<ParticleSystem>().Pause();
                particles_2.GetComponent<ParticleSystem>().Pause();
                
            });
        }
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
            /*
            ModHelper.Events.Unity.FireOnNextUpdate(() =>
            {
                // DeathManager deathManager = Locator.GetDeathManager();
                // deathManager.BeginEscapedTimeLoopSequence((TimeloopEscapeType)8486);
            });
            */
        }

        // GD teleportation event
        public void SolanumGreetingsGD()
        {
            TheVision.Instance.ModHelper.Events.Unity.RunWhen(() => Locator.GetShipLogManager() != null && Locator.GetShipLogManager().IsFactRevealed("SOLANUM_GD_RECORDER"), () =>
            {
                SolanumGreetingsGD_Entry();
            });
        }
        public void SolanumGreetingsGD_Entry()
        {
            SolanumAnimController solanumAnimController = SearchUtilities.Find("GiantsDeep_Body/Sector_GD/Nomai_ANIM_SkyWatching_Idle").GetComponent<SolanumAnimController>();
            solanumAnimController.StartWatchingPlayer();
            solanumAnimController._playerCameraTransform = SearchUtilities.Find("GiantsDeep_Body/Sector_GD/Sector_GDInterior/Sector_GDCore/Sector_Module_Sunken/Props_Module_Sunken/Prefab_NOM_StatueHead").transform;
            
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
        public void SolanumSingularityEndGD()
        {

            SearchUtilities.Find("GiantsDeep_Body/Sector_GD/BlackHole").SetActive(false);
            SearchUtilities.Find("GiantsDeep_Body/Sector_GD/WhiteHole").SetActive(false);
        }
        public void SolanumPrepareTeleportationGD()
        {
            SolanumAnimController solanumAnimController = SearchUtilities.Find("GiantsDeep_Body/Sector_GD/Nomai_ANIM_SkyWatching_Idle").GetComponent<SolanumAnimController>();            
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

        // ATP events
        public void SolanumGreetingsATP()
        {
            TheVision.Instance.ModHelper.Events.Unity.RunWhen(() => Locator.GetShipLogManager() != null && Locator.GetShipLogManager().IsFactRevealed("SOLANUM_ATP_RECORDER"), () =>
            {
                SolanumAnimController solanumAnimController = SearchUtilities.Find("TimeLoopRing_Body/Characters_TimeLoopRing/Nomai_ANIM_SkyWatching_Idle").GetComponent<SolanumAnimController>();
                solanumAnimController.StartWatchingPlayer();
                solanumAnimController.PlayRaiseCairns();

                Invoke("SolanumGreetingsATP_DeactivateRing", 4f);
                Invoke("SolanumGreetingsATP_ShowRing", 5f);
                Invoke("SolanumGreetingsATP_OpenCore", 6f);
            });
        }
        public void SolanumGreetingsATP_2()
        {
            TheVision.Instance.ModHelper.Events.Unity.RunWhen(() => Locator.GetShipLogManager() != null && Locator.GetShipLogManager().IsFactRevealed("SOLANUM_ATP_RECORDER_2"), () =>
            {
                SolanumAnimController solanumAnimController = SearchUtilities.Find("TimeLoopRing_Body/Characters_TimeLoopRing/Nomai_ANIM_SkyWatching_Idle").GetComponent<SolanumAnimController>();
                solanumAnimController.StartWatchingPlayer();
                solanumAnimController.PlayRaiseCairns();

                Invoke("SolanumGreetingsATP_DeactivateRing", 4f);
                Invoke("SolanumGreetingsATP_ShowRing", 5f);
                Invoke("SolanumGreetingsATP_OpenCore", 6f);
            });
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
            PlayActivateRingSound();
        }
        public void SolanumGreetingsATP_DeactivateRing()
        {
            SearchUtilities.Find("TimeLoopRing_Body/Interactibles_TimeLoopRing_Hidden/Prefab_NOM_Computer_ATP/Ring6").GetComponent<NomaiComputerRing>().Deactivate(0.5f);
            PlayDeactivateRingSound();
        }

        // DB events
        public void SolanumGreetingsDB()
        {
            TheVision.Instance.ModHelper.Events.Unity.RunWhen(() => Locator.GetShipLogManager() != null && Locator.GetShipLogManager().IsFactRevealed("IS_HOLOGRAM_CHANGED"), () =>
            {
                SolanumAnimController solanumAnimController = SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Nomai_ANIM_SkyWatching_Idle").GetComponent<SolanumAnimController>();
                solanumAnimController.StartWatchingPlayer();
                solanumAnimController.StartWritingMessage();
                SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Volumes_VesselDimension/VesselDiscoveryMusicTrigger").SetActive(false);
                SearchUtilities.Find("GlobalManagers/GlobalAudio/GlobalMusicController/FinalEndTimesLoopSource").SetActive(false);

                PlaySadNomaiTheme();

                Invoke("SolanumDBEvent", 8f);
                Invoke("SolanumDBEventEnd", 10f);

                TheVision.Instance.ModHelper.Events.Unity.FireOnNextUpdate(() =>
                {
                    Invoke("DropBrokenCore", 3f);
                    Invoke("PickBrokenCore", 12.6f);
                });
            });
        }
        public void SolanumDBEvent()
        {
            PlayTextSound();
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

            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 2").GetComponent<NomaiTextLine>().SetActive(true, true, 5f);
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 3").GetComponent<NomaiTextLine>().SetActive(true, true, 6f);
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 4").GetComponent<NomaiTextLine>().SetActive(true, true, 7f);
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 5").GetComponent<NomaiTextLine>().SetActive(true, true, 8f);
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 6").GetComponent<NomaiTextLine>().SetActive(true, true, 9f);
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 7").GetComponent<NomaiTextLine>().SetActive(true, true, 10f);
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 8").GetComponent<NomaiTextLine>().SetActive(true, true, 11f);
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 9").GetComponent<NomaiTextLine>().SetActive(true, true, 12f);
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 10").GetComponent<NomaiTextLine>().SetActive(true, true, 11f);
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 11").GetComponent<NomaiTextLine>().SetActive(true, true, 10f);
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 12").GetComponent<NomaiTextLine>().SetActive(true, true, 9f);
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 13").GetComponent<NomaiTextLine>().SetActive(true, true, 8f);
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 14").GetComponent<NomaiTextLine>().SetActive(true, true, 7f);
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 15").GetComponent<NomaiTextLine>().SetActive(true, true, 6f);
            SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/Arc_DB_Vessel_IncomingMessage/Arc 16").GetComponent<NomaiTextLine>().SetActive(true, true, 5f);
        }
        public void DropBrokenCore()
        {
            var core = SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:RT_Arm_ClavicleSHJnt/Nomai_Rig_v01:RT_Arm_ShoulderSHJnt/Nomai_Rig_v01:RT_Arm_ElbowSHJnt/Prefab_NOM_WarpCoreVesselBroken");
            var vessel = SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension");

            core.transform.parent = vessel.transform.parent;
            core.transform.localPosition = new Vector3(147.5621f, 26.6017f, -0.4492f);
            core.transform.rotation = new Quaternion(0.004f, -0.942f, 0.1201f, -0.3134f);

            PlayCoreDropSound();
        }
        public void PickBrokenCore()
        {
            var vesselCore = SearchUtilities.Find("Prefab_NOM_WarpCoreVesselBroken");
            vesselCore.transform.parent = SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:RT_Arm_ClavicleSHJnt/Nomai_Rig_v01:RT_Arm_ShoulderSHJnt/Nomai_Rig_v01:RT_Arm_ElbowSHJnt/Nomai_Rig_v01:RT_Arm_WristSHJnt").transform.parent;
            vesselCore.transform.localPosition = new Vector3(0.9f, 0f, -0.1f);
            vesselCore.transform.rotation = new Quaternion(0.3842f, 0.0578f, 0.7798f, -0.4009f);

            PlayCorePickSound();

        }
        public void ParentBrokenCore()
        {
            var vesselCore = SearchUtilities.Find("Prefab_NOM_WarpCoreVesselBroken");

            vesselCore.GetComponent<WarpCoreItem>()._interactable = false;
            vesselCore.transform.parent = SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:RT_Arm_ClavicleSHJnt/Nomai_Rig_v01:RT_Arm_ShoulderSHJnt/Nomai_Rig_v01:RT_Arm_ElbowSHJnt/Nomai_Rig_v01:RT_Arm_WristSHJnt").transform.parent;
            vesselCore.transform.localPosition = new Vector3(0.8f, -0.1f, -0.2f);
            vesselCore.transform.rotation = new Quaternion(0.3842f, 0.0578f, 0.7798f, -0.4009f);
        }
        public void SolanumDBEventEnd()
        {
            SolanumAnimController solanumAnimController = SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Nomai_ANIM_SkyWatching_Idle").GetComponent<SolanumAnimController>();
            solanumAnimController.StartWatchingPlayer();
            solanumAnimController.StopWritingMessage(true);
        }

        // TH event
        public void SolanumGreetingsTH()
        {
            TheVision.Instance.ModHelper.Events.Unity.RunWhen(() => Locator.GetShipLogManager() != null && Locator.GetShipLogManager().IsFactRevealed("SOLANUM_PROJECTION_COMPLETE"), () =>
            {
                SolanumAnimController solanumAnimController2 = SearchUtilities.Find("TimberHearth_Body/Sector_TH/Nomai_ANIM_SkyWatching_Idle").GetComponent<SolanumAnimController>();
                solanumAnimController2.StartWatchingPlayer();
                solanumAnimController2.StartConversation();
            });
        }

        // BH event
        public void SolanumGreetingsBH()
        {
            SolanumAnimController solanumAnimController = SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character").GetComponent<SolanumAnimController>();
            solanumAnimController.StartWatchingPlayer();
            solanumAnimController.PlayGestureToWordStones();            

            if (Locator.GetShipLogManager().IsFactRevealed("SOLANUM_PROJECTION_COMPLETE") && !Locator.GetShipLogManager().IsFactRevealed("SOLANUM_BH_EVENT"))
            {
                TheVision.Instance.ModHelper.Events.Unity.RunWhen(() => Locator.GetShipLogManager() != null && Locator.GetShipLogManager().IsFactRevealed("SOLANUM_BH_EVENT"), () =>
                {
                    Invoke("SolanumEventBH", 3f);
                });
            }
        }  
        public void SolanumEventBH()
        {
            SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Solanum_BH_Crystal/CapsuleVolume_NOM_GravityCrystal").GetComponent<CapsuleCollider>().enabled = true;

            var crystallGravity = SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Solanum_BH_Crystal/CapsuleVolume_NOM_GravityCrystal").GetComponent<DirectionalForceVolume>();
            crystallGravity.SetFieldMagnitude(-0.2f);

            PlayBrokenCrystallSound();

            Invoke("SolanumEventBHend", 4f);
            Invoke("PlayCrackSound", 5.5f);

            SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Solanum_BH_Crystal/AudioSource_GravityCrystal").SetActive(false);
        }
        public void SolanumEventBHend()
        {
            SolanumAnimController solanumAnimController = SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character").GetComponent<SolanumAnimController>();
            solanumAnimController.PlayGestureToCairns();
        }

        // ET event
        public void SolanumGreetingsET()
        {
            SolanumAnimController solanumAnimController = SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/Solanum_EmberTwin_Character").GetComponent<SolanumAnimController>();
            solanumAnimController.StartWatchingPlayer();
            solanumAnimController.PlayGestureToWordStones();

            if (Locator.GetShipLogManager().IsFactRevealed("SOLANUM_PROJECTION_COMPLETE") && !Locator.GetShipLogManager().IsFactRevealed("SOLANUM_ET_EVENT"))
            {
                TheVision.Instance.ModHelper.Events.Unity.RunWhen(() => Locator.GetShipLogManager() != null && Locator.GetShipLogManager().IsFactRevealed("SOLANUM_ET_EVENT"), () =>
                {
                    SolanumEventET();
                });
            }
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
            pyramid.rotationSpeed = 15f;
            pyramid.Start();

            mobius.targetPos = new Vector3(0.7838f, -0.1231f, 0.6973f);
            mobius.delay = 5.5f;
            mobius.rotationSpeed = -9f;
            mobius.Start();

            ship.targetPos = new Vector3(0.4714f, 0.744f, 0.1487f);
            ship.delay = 6f;
            ship.rotationSpeed = 2f;
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

            SolanumAnimController solanumAnimController = SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/Solanum_EmberTwin_Character").GetComponent<SolanumAnimController>();
            solanumAnimController.StopWatchingPlayer();
            solanumAnimController._playerCameraTransform = SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/SandPile_4").transform;
            solanumAnimController.StartWatchingPlayer();           
        }       
        public void SolanumETPostEndEvent2()
        {
            SolanumAnimController solanumAnimController = SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/Solanum_EmberTwin_Character").GetComponent<SolanumAnimController>();
            solanumAnimController.StartWatchingPlayer();
            // SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/Solanum_EmberTwin_Character").GetComponent<Animator>().speed = 1f;
        }
        public void SolanumETEndEvent()
        {
            SolanumAnimController solanumAnimController = SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/Solanum_EmberTwin_Character").GetComponent<SolanumAnimController>();
            solanumAnimController.PlayGestureToCairns();            
        }

        // Ernesto
        public void ErnestoQuestEntry()
        {
            TheVision.Instance.ModHelper.Events.Unity.RunWhen(() => Locator.GetShipLogManager() != null && Locator.GetShipLogManager().IsFactRevealed("ERNESTO_ENTRY"), () =>
            {
                var fish = SearchUtilities.Find("TimberHearth_Body/Sector_TH/Sector_Village/Sector_StartingCamp/Characters_StartingCamp/Villager_HEA_Slate/Villager_HEA_Slate_ANIM_LogSit/Slate_Skin_01:tall_rig_b_v01:TrajectorySHJnt/Slate_Skin_01:tall_rig_b_v01:ROOTSHJnt/Slate_Skin_01:tall_rig_b_v01:Spine_01SHJnt/Slate_Skin_01:tall_rig_b_v01:Spine_02SHJnt/Slate_Skin_01:tall_rig_b_v01:Spine_TopSHJnt/Slate_Skin_01:tall_rig_b_v01:LF_Arm_ClavicleSHJnt/Slate_Skin_01:tall_rig_b_v01:LF_Arm_ShoulderSHJnt/Slate_Skin_01:tall_rig_b_v01:LF_Arm_ElbowSHJnt/Slate_Skin_01:tall_rig_b_v01:LF_Arm_WristSHJnt/Props_HEA_RoastingStick/RoastingStick_Stick/PoorErnesto");
                fish.transform.localPosition = new Vector3(0.0127f, 0.08f, 1.64f);
                fish.transform.rotation = new Quaternion(0, 0, 0, 0);
                fish.GetComponent<AnglerfishAnimController>().OnChangeAnglerState(AnglerfishController.AnglerState.Chasing);

                SearchUtilities.Find("TimberHearth_Body/Sector_TH/Reveal Volume (Enter)").SetActive(true);
                SearchUtilities.Find("TimberHearth_Body/Sector_TH/Sector_Village/Sector_StartingCamp/Characters_StartingCamp/Villager_HEA_Slate").gameObject.SetActive(true);
                SearchUtilities.Find("TimberHearth_Body/Sector_TH/Prefab_HEA_Journal").SetActive(false);
                SearchUtilities.Find("TimberHearth_Body/Sector_TH/ConversationZone").SetActive(false);
                SearchUtilities.Find("TimberHearth_Body/Sector_TH/Prefab_HEA_Journal/InteractVolume").SetActive(false);
                SearchUtilities.Find("TimberHearth_Body/Sector_TH/Sector_Village/Interactables_Village/LaunchTower/Launch_Tower/ElevatorController/Elevator/AttachPoint_LaunchTowerElevator").gameObject.SetActive(true);
                SearchUtilities.Find("ScreenPromptCanvas/ScreenPromptListBottomLeft/ScreenPrompt/Text").GetComponent<UnityEngine.UI.Text>().text = TranslationHandler.GetTranslation("THE_VISION_NEW_LAUNCH_CODES", TranslationHandler.TextType.UI);

                SearchUtilities.Find("ScreenPromptCanvas/ScreenPromptListBottomLeft/ScreenPrompt").SetActive(true);
            });

            var marshmallow = SearchUtilities.Find("TimberHearth_Body/Sector_TH/Sector_Village/Sector_StartingCamp/Props_StartingCamp/OtherComponentsGroup/Props_HEA_CampsiteLogAssets/Props_HEA_MarshmallowUncoocked1");
            marshmallow.transform.localPosition = new Vector3(-1.2897f, 0.15f, -2.0442f);
            marshmallow.transform.localRotation = new Quaternion(0.2472f, 0.1217f, 0.0551f, 0.9597f);

            var marshmallowCan = SearchUtilities.Find("TimberHearth_Body/Sector_TH/Sector_Village/Sector_StartingCamp/Props_StartingCamp/OtherComponentsGroup/Props_HEA_CampsiteLogAssets/Props_HEA_MarshmallowCanOpened");
            marshmallowCan.transform.localPosition = new Vector3(-0.3897f, 0.172f, -2.4442f);
            marshmallowCan.transform.localRotation = new Quaternion(0.633f, -0.6852f, -0.132f, 0.3353f);
            SearchUtilities.Find("TimberHearth_Body/Sector_TH/ConversationZone").SetActive(true);

            PlayerData.SetPersistentCondition("MARK_ON_HUD_TUTORIAL_COMPLETE", true);
            PlayerData.SetPersistentCondition("COMPLETED_SHIPLOG_TUTORIAL", true);            

            // Making Ernesto quest optional if disabled :((
            SearchUtilities.Find("TimberHearth_Body/Sector_TH/Sector_Village/Interactables_Village/LaunchTower/Launch_Tower/ElevatorController/Elevator/AttachPoint_LaunchTowerElevator").gameObject.SetActive(false);

            SearchUtilities.Find("TimberHearth_Body/Sector_TH/Sector_Village/Sector_StartingCamp/Characters_StartingCamp/Villager_HEA_Slate").gameObject.SetActive(false);
            SearchUtilities.Find("TimberHearth_Body/Sector_TH/Prefab_HEA_Journal").SetActive(true);

            // setting up Hornfels for dialogue
            SearchUtilities.Find("TimberHearth_Body/Sector_TH/Sector_Village/Sector_Observatory/Characters_Observatory/Villager_HEA_Hornfels (1)/ConversationZone_Hornfels").DestroyAllComponents<InteractReceiver>();
            SearchUtilities.Find("TimberHearth_Body/Sector_TH/Sector_Village/Sector_Observatory/Characters_Observatory/Villager_HEA_Hornfels (1)/ConversationZone_Hornfels").DestroyAllComponents<CharacterDialogueTree>();
            SearchUtilities.Find("TimberHearth_Body/Sector_TH/Sector_Village/Sector_Observatory/Characters_Observatory/Villager_HEA_Hornfels (1)/Villager_HEA_Hornfels_ANIM_Working/ConversationZone").transform.localPosition = new Vector3(0f, 1.9649f, 0f);
            SearchUtilities.Find("TimberHearth_Body/Sector_TH/Sector_Village/Sector_Observatory/Interactables_Observatory/AnglerFishExhibit/AnglerFishTankPivot/Beast_Anglerfish").SetActive(false);

            var hornfels = SearchUtilities.Find("TimberHearth_Body/Sector_TH/Sector_Village/Sector_Observatory/Characters_Observatory/Villager_HEA_Hornfels (1)");
            hornfels.transform.localPosition = new Vector3(-5.9667f, 0.2095f, -1.0348f);
            hornfels.transform.localRotation = new Quaternion(0f, 0f, 0f, 0f);

            SearchUtilities.Find("TimberHearth_Body/Sector_TH/Sector_Village/Sector_StartingCamp/Characters_StartingCamp/Villager_HEA_Slate/ConversationZone_RSci").DestroyAllComponents<InteractReceiver>();
            SearchUtilities.Find("TimberHearth_Body/Sector_TH/Sector_Village/Sector_StartingCamp/Characters_StartingCamp/Villager_HEA_Slate/ConversationZone_RSci").DestroyAllComponents<CharacterDialogueTree>();
            SearchUtilities.Find("TimberHearth_Body/Sector_TH/Sector_Village/Sector_StartingCamp/Characters_StartingCamp/Villager_HEA_Slate/Villager_HEA_Slate_ANIM_LogSit/ConversationZone").transform.localPosition = new Vector3(-0.2199f, 1.0245f, -0.322f);

            TheVision.Instance.ModHelper.Events.Unity.RunWhen(() => Locator.GetShipLogManager() != null && Locator.GetShipLogManager().IsFactRevealed("ERNESTO_POOR_ERNESTO"), () =>
            {
                SearchUtilities.Find("TimberHearth_Body/Sector_TH/Sector_Village/Volumes_Village/MusicVolume_Village").SetActive(false);

                Invoke("PlaySecretSound", 4f);
                Invoke("PlayErnestoSound", 1f);
                Invoke("GetTHMusicBack", 25f);

                ModHelper.Console.WriteLine("Thank you for finding Ernesto! You will never meet him again.", MessageType.Success);
                var newLaunchCodes = SearchUtilities.Find("ScreenPromptCanvas/ScreenPromptListBottomLeft/ScreenPrompt");
                newLaunchCodes.SetActive(true);

                var elevatorController = SearchUtilities.Find("TimberHearth_Body/Sector_TH/Sector_Village/Interactables_Village/LaunchTower/Launch_Tower/ElevatorController/Elevator/AttachPoint_LaunchTowerElevator").GetComponent<InteractZone>();

                TheVision.Instance.ModHelper.Events.Unity.RunWhen(() => elevatorController._isInteractPressed = true, () =>
                {
                    newLaunchCodes.SetActive(false);
                });
            });
        }
        public void GetTHMusicBack()
        {
            SearchUtilities.Find("TimberHearth_Body/Sector_TH/Sector_Village/Volumes_Village/MusicVolume_Village").SetActive(true);
        }

        // Utility
        public void PickUpTorch()
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

                SearchUtilities.Find("Ship_Body/ShipSector/ConversationZone").SetActive(true);
                SearchUtilities.Find("Ship_Body/ShipSector/ConversationTrigger").SetActive(true);

            });

            // Vision Torch 2 picking up
            TheVision.Instance.ModHelper.Events.Unity.RunWhen(() => !visionTorchTaken2._active, () =>
            {

                visionTorchMesh2.SetActive(true);
                visionTorchActive2.SetActive(false);
                SearchUtilities.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/Character_NOM_Solanum/ConversationZone").SetActive(false);

                SearchUtilities.Find("Ship_Body/ShipSector/ConversationZone").SetActive(true);
                SearchUtilities.Find("Ship_Body/ShipSector/ConversationTrigger").SetActive(true);

               
            });

        }
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

                var interiorVolumes = SearchUtilities.Find("QuantumMoon_Body/Volumes/InteriorVolumes_QM");
                interiorVolumes.SetActive(false);
                interiorVolumes.SetActive(true);

            });
        }
        public void SpawnOnVisionEnd()
        {
            ModHelper.Events.Unity.FireOnNextUpdate(() =>
            {
                PlayRevealSound();
                PlaySFXSound();
                PlayGaspSound();
                PlayThunderSound();
            });            

            ATPfix();

            // Enabling json props
            DisabledPropsOnStart(true);

            // Disabling QM WhiteHole
            Locator.GetAstroObject(AstroObject.Name.QuantumMoon).transform.Find("Sector_QuantumMoon/WhiteHole").gameObject.SetActive(false);


            Invoke("BreakLock", 0.8f);
            TeleportShip();
            ParentCore();

            SearchUtilities.Find("Ship_Body/ShipSector/ConversationZone").SetActive(false);
            SearchUtilities.Find("Ship_Body/ShipSector/ConversationTrigger").SetActive(false);

            SolanumAnimController solanumAnimController2 = SearchUtilities.Find("TimberHearth_Body/Sector_TH/Nomai_ANIM_SkyWatching_Idle").GetComponent<SolanumAnimController>();
            solanumAnimController2.StartWatchingPlayer();
            solanumAnimController2.StartConversation();


            var learnSignal = SearchUtilities.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/Character_NOM_Solanum/Signal_Solanum").GetComponent<AudioSignal>();
            learnSignal.IdentifyFrequency();
            learnSignal.IdentifySignal();


            TheVision.Instance.ModHelper.Events.Unity.RunWhen(() => Locator.GetShipLogManager() != null && Locator.GetShipLogManager().IsFactRevealed("SOLANUM_GD_RECORDER"), () =>
            {
                Invoke("SolanumGreetingsGD", 1f);

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
        public void BreakLock()
        {
            var cameraFixedPosition = Locator.GetPlayerTransform().gameObject.GetComponent<PlayerLockOnTargeting>();
            cameraFixedPosition.BreakLock(0.5f);
        }
        public void Flicker()
        {
            SearchUtilities.Find("Player_Body/PlayerCamera/ScreenEffects/UnderwaterEffectBubble").SetActive(false);
            PlayFlickerSound();
            var effect = SearchUtilities.Find("Player_Body/PlayerCamera/ScreenEffects/LightFlickerEffectBubble").GetComponent<LightFlickerController>();
            effect.FlickerOffAndOn(offDuration: 2f, onDuration: 3f);
        }
        public void ParentCore()
        {
            var vesselCore = SearchUtilities.Find("Prefab_NOM_WarpCoreVesselBroken");
            vesselCore.GetComponent<WarpCoreItem>()._interactable = false;
            vesselCore.transform.parent = SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Nomai_ANIM_SkyWatching_Idle/Nomai_Rig_v01:TrajectorySHJnt/Nomai_Rig_v01:ROOTSHJnt/Nomai_Rig_v01:Spine_01SHJnt/Nomai_Rig_v01:Spine_02SHJnt/Nomai_Rig_v01:Spine_TopSHJnt/Nomai_Rig_v01:RT_Arm_ClavicleSHJnt/Nomai_Rig_v01:RT_Arm_ShoulderSHJnt/Nomai_Rig_v01:RT_Arm_ElbowSHJnt/Nomai_Rig_v01:RT_Arm_WristSHJnt").transform.parent;
            vesselCore.transform.localPosition = new Vector3(0.8f, -0.1f, -0.2f);
            vesselCore.transform.rotation = new Quaternion(0.3842f, 0.0578f, 0.7798f, -0.4009f);
        }

        // Music and sounds
        public void CreditsMusic()
        {
            var addMusic = GameObject.Find("AudioSource").GetComponent<OWAudioSource>();
            addMusic.AssignAudioLibraryClip(AudioType.FinalCredits);
            addMusic.Play();
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
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 1f);
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
        public void PlayQuantumLightningSound()
        {
            PlayerHeadsetAudioSource = Locator.GetPlayerTransform().gameObject.AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.EyeLightning); // 2400(whosh) 2407(vessel create singularity, 2408 - vessel out of sing) 2402 - getting in on BH; 2429 - reality broken // 2007 - GD lightning // 854 PlayerGasp_Heavy
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 0.3f);
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
            SolanumAnimController solanumAnimController = SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/Solanum_EmberTwin_Character").GetComponent<SolanumAnimController>();            
            solanumAnimController.StopWatchingPlayer();
            solanumAnimController._playerCameraTransform = SearchUtilities.Find("CaveTwin_Body/Sector_CaveTwin/Solanum_EmberTwin_ToyPyramid/Solanum_EmberTwin_ToyShip").transform;
            solanumAnimController.StartWatchingPlayer();

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
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 1f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.PlayOneShot();
        }
        public void PlaySingularityCreateSound()
        {
            PlayerHeadsetAudioSource = Locator.GetPlayerTransform().gameObject.AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.SingularityCreate); // SingularityOnPlayerEnterExit = 2402            
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 1f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.PlayOneShot();
        }
        public void PlaySingularityCollapseSound()
        {
            PlayerHeadsetAudioSource = Locator.GetPlayerTransform().gameObject.AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.SingularityCollapse); // SingularityOnPlayerEnterExit = 2402            
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 1f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.PlayOneShot();
        }
        public void PlaySecretSound()
        {
            PlayerHeadsetAudioSource = Locator.GetPlayerTransform().gameObject.AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.SecretKorok); // SingularityOnPlayerEnterExit = 2402            
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 1f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.Play();
        }
        public void PlaySadNomaiTheme()
        {
            PlayerHeadsetAudioSource = Locator.GetMinorAstroObject("Vessel Dimension").gameObject.AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.SadNomaiTheme); // SingularityOnPlayerEnterExit = 2402            
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 1f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.Play();
        }
        public void PlayTextSound()
        {
            PlayerHeadsetAudioSource = SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Nomai_ANIM_SkyWatching_Idle").gameObject.AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.NomaiTextReveal_LP); // SingularityOnPlayerEnterExit = 2402            
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 1f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.PlayOneShot();
        }
        public void PlayCorePickSound()
        {
            PlayerHeadsetAudioSource = SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Nomai_ANIM_SkyWatching_Idle").gameObject.AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.ToolItemWarpCorePickUp); // SingularityOnPlayerEnterExit = 2402            
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 1f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.PlayOneShot();
        }
        public void PlayCoreDropSound()
        {
            PlayerHeadsetAudioSource = SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Nomai_ANIM_SkyWatching_Idle").gameObject.AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.ToolItemWarpCoreDrop); // SingularityOnPlayerEnterExit = 2402            
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 1f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.PlayOneShot();
        }
        public void PlayCrackSound()
        {
            SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Solanum_BH_Crystal/Props_NOM_GravityCrystal").SetActive(false);
            SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Solanum_BH_Crystal/CapsuleVolume_NOM_GravityCrystal").SetActive(false);
            SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Solanum_BH_Crystal_Cracked").SetActive(true);
            SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Solanum_BH_Crystal/AudioSource_GravityCrystal").SetActive(false);
            SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Solanum_BH_Crystal").DestroyAllComponents<OWAudioSource>();
            SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Solanum_BH_Crystal").DestroyAllComponents<AudioSource>();

            PlayerHeadsetAudioSource = SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Solanum_BH_Crystal_Cracked").AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.PlayerSuitHelmetCrack);
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 0.5f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.Play();
        }
        public void PlayBrokenCrystallSound()
        {
            SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Solanum_BH_Crystal/AudioSource_GravityCrystal").SetActive(false);

            PlayerHeadsetAudioSource = SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Solanum_BH_Character/Solanum_BH_Crystal").AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.NomaiGravCrystalFlickerAmbient_LP);
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 0.5f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.Play();
        }
        public void PlayDeactivateRingSound()
        {
            PlayerHeadsetAudioSource = SearchUtilities.Find("TimeLoopRing_Body/Interactibles_TimeLoopRing_Hidden/Prefab_NOM_Computer_ATP").gameObject.AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.NomaiComputerRingDeactivate); // SingularityOnPlayerEnterExit = 2402            
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 15f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.PlayOneShot();
        }
        public void PlayActivateRingSound()
        {
            PlayerHeadsetAudioSource = SearchUtilities.Find("TimeLoopRing_Body/Interactibles_TimeLoopRing_Hidden/Prefab_NOM_Computer_ATP").gameObject.AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.NomaiComputerRingActivate); // SingularityOnPlayerEnterExit = 2402            
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 15f);
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.PlayOneShot();
        }
        public void PlayErnestoSound()
        {
            PlayerHeadsetAudioSource = SearchUtilities.Find("TimberHearth_Body/Sector_TH/Sector_Village/Sector_StartingCamp/Characters_StartingCamp/Villager_HEA_Slate/Villager_HEA_Slate_ANIM_LogSit/Slate_Skin_01:tall_rig_b_v01:TrajectorySHJnt/Slate_Skin_01:tall_rig_b_v01:ROOTSHJnt/Slate_Skin_01:tall_rig_b_v01:Spine_01SHJnt/Slate_Skin_01:tall_rig_b_v01:Spine_02SHJnt/Slate_Skin_01:tall_rig_b_v01:Spine_TopSHJnt/Slate_Skin_01:tall_rig_b_v01:LF_Arm_ClavicleSHJnt/Slate_Skin_01:tall_rig_b_v01:LF_Arm_ShoulderSHJnt/Slate_Skin_01:tall_rig_b_v01:LF_Arm_ElbowSHJnt/Slate_Skin_01:tall_rig_b_v01:LF_Arm_WristSHJnt/Props_HEA_RoastingStick/RoastingStick_Stick/PoorErnesto").gameObject.AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip(AudioType.DBAnglerfishDetectTarget); // SingularityOnPlayerEnterExit = 2402            
            PlayerHeadsetAudioSource.SetMaxVolume(maxVolume: 0.3f);
            PlayerHeadsetAudioSource.pitch = 5f;
            PlayerHeadsetAudioSource.GetComponent<AudioSource>().playOnAwake = false;
            PlayerHeadsetAudioSource.PlayOneShot();
        }
    }
}