using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

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
            __instance._deathText.text = "TO BE CONTINUED...";
        }


    }
}
