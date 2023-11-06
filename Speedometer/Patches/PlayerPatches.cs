using HarmonyLib;

namespace Speedometer.Patches
{
    [HarmonyPatch(typeof(Reptile.Player), "ChargeAndSpeedDisplayUpdate")]
    public class ChargeAndSpeedDisplayUpdatePatch
    {
        public static void Postfix(Reptile.Player __instance)
        {
            Plugin.UpdateSpeedBar(__instance);
        }
    }
}
