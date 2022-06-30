using OWML.Common;
using OWML.ModHelper;
using OWML.Utils;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TheVision.Utilities.ModAPIs;
using static NewHorizons.External.Modules.PropModule;
using NewHorizons.Builder.Props;
using System.Linq;
using TheVision.CustomProps;
using HarmonyLib;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TheVision.Utilities;
using System;
using NewHorizons.Utility;
using UAudioType = UnityEngine.AudioType;
using UnityEngine.Networking;

namespace TheVision
{
    public class TheVision : ModBehaviour
    {
        public static INewHorizons newHorizonsAPI;
        public static TheVision Instance;

        public OWAudioSource PlayerHeadsetAudioSource;
                

        private void Awake()
        {
            // You won't be able to access OWML's mod helper in Awake.
            // So you probably don't want to do anything here.
            // Use Start() instead.
            Instance = this;
        }

        private void Start()
        {
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());


            var newHorizonsAPI = ModHelper.Interaction.GetModApi<INewHorizons>("xen.NewHorizons");
            newHorizonsAPI.GetStarSystemLoadedEvent().AddListener(OnStarSystemLoaded);
            newHorizonsAPI.LoadConfigs(this);

            // Starting here, you'll have access to OWML's mod helper.
            ModHelper.Console.WriteLine($"My mod {nameof(TheVision)} is loaded!", MessageType.Success);

            // Example of accessing game code.
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
            // find all slides for vision1
            var pathToVisionSlidesFolder = "/images/vision1";

            string[] files = System.IO.Directory.GetFiles(TheVision.Instance.ModHelper.Manifest.ModFolderPath + pathToVisionSlidesFolder, "*.png");
            SlideInfo[] slides = files.Select(f => f.Remove(0, TheVision.Instance.ModHelper.Manifest.ModFolderPath.Length)).Select(f => new SlideInfo() { imagePath = f }).ToArray();

            // slides[0].backdropAudio = "SunStation"; // "OW_NM_SunStation";
            // slides[251].backdropAudio = "SadNomaiTheme"; // "OW NM Nomai Ruins 081718 AP";

            ProjectionInfo info = new ProjectionInfo()
            {
                position = new Vector3(-5.254965f, -70.73996f, 1.607201f),
                rotation = new Vector3(0, 0, 0),
                type = ProjectionInfo.SlideShowType.VisionTorchTarget,
                slides = slides
            };

            GameObject visionTarget = ProjectionBuilder.MakeMindSlidesTarget(Locator._quantumMoon.gameObject, Locator._quantumMoon._sector, info, TheVision.Instance);
            //GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE").transform;
            visionTarget.transform.parent =
                GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon")
                .GetComponentsInChildren<Transform>(true)
                .Where(t => t.gameObject.name == "State_EYE")
                .First(); // All because Find doesn't work on inactive game objects :/

            // make Solanum have the proper reaction after the vision ends
            //GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE/Interactables_EYEState/ConversationPivot/NomaiConversation/ResponseStone/ArcSocket/Arc_QM_SolanumConvo_Explain+Eye").GetComponent<NomaiWallText>();
            NomaiWallText responseText =
                Resources.FindObjectsOfTypeAll<NomaiWallText>()
                .Where(text => text.gameObject.name == "Arc_QM_SolanumConvo_Explain+Eye")
                .First();

            var nomaiConversationManager = Resources.FindObjectsOfTypeAll<NomaiConversationManager>().First(); //GameObject.FindObjectOfType<NomaiConversationManager>();
            var myConversationManager = nomaiConversationManager.gameObject.AddComponent<TheVision_SolanumVisionResponse>();
            myConversationManager._nomaiConversationManager = nomaiConversationManager;
            myConversationManager._solanumAnimController = nomaiConversationManager._solanumAnimController;
            myConversationManager.solanumVisionResponse = responseText;

            visionTarget.GetComponent<VisionTorchTarget>().onSlidesComplete = myConversationManager.OnVisionEnd;

        }



        // Load SolanumProps
        public void OnStarSystemLoaded(string systemName)
        {
            ModHelper.Console.WriteLine("LOADED SYSTEM " + systemName);

            if (systemName == "SolarSystem")
            {
                SpawnSolanumProps();
                SpawnVisionTorch();
                

            }
        }

        // Bars to spawn SolanumCopies
        public void SpawnSolanumCopy(INewHorizons newHorizonsAPI)
        {


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

            // Spawning Solanum 3
        }
        // Spawning Vision Torch with code
        public void SpawnVisionTorch()
        {
            var path = "DreamWorld_Body/Sector_DreamWorld/Sector_Underground/Sector_PrisonCell/Interactibles_PrisonCell/PrisonerSequence/VisionTorchWallSocket/Prefab_IP_VisionTorchItem";
            Vector3 position = new Vector3(18.06051f, -50.64357f, 183.141f);
            Vector3 rotation = new Vector3(311.8565f, 287.9388f, 254.72f);
            GameObject staff = DetailBuilder.MakeDetail(Locator._timberHearth.gameObject, Locator._timberHearth.GetRootSector(), path, position, rotation, 1, false);

            // Trying to load custom audio


            //placing orb on GD to the slot 2

            var nomaiSlot = SearchUtilities.Find("GiantsDeep_Body/Sector_GD/Sector_GDInterior/Sector_GDCore/Sector_Module_Sunken/Interactables_Module_Sunken/OrbInterface/Slots/Slot (1)");
            var nomaiSlotWithComponent = nomaiSlot.GetComponent<NomaiInterfaceSlot>();
                        

            var nomaiInterfaceOrb = SearchUtilities.Find("Prefab_NOM_InterfaceOrb");
            var correctSlot = nomaiInterfaceOrb.GetComponent<NomaiInterfaceOrb>();
            correctSlot._occupiedSlot = nomaiSlotWithComponent;



        }
        public NewHorizons.External.Modules.SignalModule.SignalInfo MakeSolanumSignalInfo(Vector3 position)
        {


            return new NewHorizons.External.Modules.SignalModule.SignalInfo()
            {
                audioFilePath = "planets/quantum.wav",
                frequency = "Quantum Consciousness",
                detectionRadius = 1000,
                identificationRadius = 500,
                sourceRadius = 2f,
                name = "Solanum",
                position = position,
                onlyAudibleToScope = false,

            };
        }

        public void SpawnSignals()

        {
            PlayerHeadsetAudioSource = GameObject.Find("Player_Body").AddComponent<OWAudioSource>();
            PlayerHeadsetAudioSource.enabled = true;
            PlayerHeadsetAudioSource.AssignAudioLibraryClip((AudioType)2400);
            PlayerHeadsetAudioSource.Play();

            //decloaking QM on signals spawn
            GameObject.Find("QuantumMoon_Body/Sector_QuantumMoon/State_EYE/Clouds_QM_EyeState").SetActive(false);


            








            SignalBuilder.Make(Locator._timberHearth.gameObject, Locator._timberHearth.GetRootSector(), MakeSolanumSignalInfo(new Vector3(48.5018f, 15.1183f, 249.9972f)), TheVision.Instance);
            SignalBuilder.Make(Locator._quantumMoon.gameObject, Locator._quantumMoonAstroObj.GetRootSector(), MakeSolanumSignalInfo(new Vector3(-5.254965f, -70.73996f, 1.607201f)), TheVision.Instance);
            SignalBuilder.Make(Locator._giantsDeep.gameObject, Locator._giantsDeep.GetRootSector(), MakeSolanumSignalInfo(new Vector3(-43.62191f, -68.5414f, -31.2553654f)), TheVision.Instance);
        }
                
        
    }
}




















