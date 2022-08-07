using HarmonyLib;
using UnityEngine;

namespace TheVision.Patches
{
    [HarmonyPatch(typeof(GameOverController))]
    internal static class GameOverPatches
    {
        private static bool skipPostCredits = false;
        private static OWAudioSource PlayerHeadsetAudioSource;

        [HarmonyPatch(nameof(GameOverController.SetupGameOverScreen))]
        [HarmonyPrefix]
        public static void SetupGameOverScreenPrefix(GameOverController __instance)
        {
            if (Locator.GetDeathManager()._timeloopEscapeType != (TimeloopEscapeType)8486) return;
            __instance._deathText.text = "TO BE CONTINUED...";

            SubmitActionLoadScene actionLoadScene = new SubmitActionLoadScene();
            actionLoadScene.SetSceneToLoad(SubmitActionLoadScene.LoadableScenes.CREDITS);
           

        }

         [HarmonyPatch(nameof(GameOverController.Update))]
         [HarmonyPrefix]
         public static void UpdatePrefix(GameOverController __instance)
         {
             if (Locator.GetDeathManager()._timeloopEscapeType != (TimeloopEscapeType)8486) return;
             if (__instance._fadedOutText && __instance._textAnimator.IsComplete() && !__instance._loading)
             {
                 LoadManager.LoadScene(OWScene.Credits_Final, LoadManager.FadeType.None, 1f, true);
                 __instance._loading = true;


                 skipPostCredits = true;
             }

         }

         [HarmonyPatch(typeof(Credits), nameof(Credits.LoadNextScene))]
         [HarmonyPrefix]
         public static void CreditsLoadNextScenePrefix(Credits __instance)
         {
             if (skipPostCredits)
             {
                 __instance._type = Credits.CreditsType.Fast;
                 skipPostCredits = false;
             }
         } 
    }
}
