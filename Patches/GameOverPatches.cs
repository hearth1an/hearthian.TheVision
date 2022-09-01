using HarmonyLib;
using NewHorizons.Handlers;

namespace TheVision.Patches
{
    [HarmonyPatch(typeof(GameOverController))]
    internal static class GameOverPatches
    {       
        [HarmonyPatch(nameof(GameOverController.SetupGameOverScreen))]
        [HarmonyPrefix]
        public static void SetupGameOverScreenPrefix(GameOverController __instance)
        {
            if (Locator.GetDeathManager()._timeloopEscapeType != (TimeloopEscapeType)8486) return;
            __instance._deathText.text = TranslationHandler.GetTranslation("THE_VISION_TO_BE_CONTINUED", TranslationHandler.TextType.UI);

            SubmitActionLoadScene actionLoadScene = new SubmitActionLoadScene();
            actionLoadScene.SetSceneToLoad(SubmitActionLoadScene.LoadableScenes.CREDITS); 
        } 
    }
}
