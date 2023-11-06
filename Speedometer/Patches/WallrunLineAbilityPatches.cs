using HarmonyLib;

namespace Speedometer.Patches
{
    [HarmonyPatch(typeof(Reptile.WallrunLineAbility), nameof(Reptile.WallrunLineAbility.FixedUpdateAbility))]
    public class WallrunLineAbilityFixedUpdate
    {
        public static void Postfix(float ___lastSpeed)
        {
            Plugin.UpdateLastSpeed(___lastSpeed);
        }
    }
    [HarmonyPatch(typeof(Reptile.WallrunLineAbility), nameof(Reptile.WallrunLineAbility.Jump))]
    public class WallrunLineAbilityJump
    {
        public static void Postfix(float ___lastSpeed)
        {
            Plugin.UpdateLastSpeed(___lastSpeed);
        }
    }
}
