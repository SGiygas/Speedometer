using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Speedometer.Patches
{
    [HarmonyPatch(typeof(Reptile.GameplayUI), nameof(Reptile.GameplayUI.Init))]
    public class GameplayUIInit
    {
        public static void Postfix(Image ___chargeBar, TextMeshProUGUI ___tricksInComboLabel)
        {
            GameObject chargeBarBackground = ___chargeBar.transform.parent.gameObject;

            // Need to make sure the bar renders below the boost bar
            Image speedBarBackground = Object.Instantiate(chargeBarBackground, chargeBarBackground.transform.parent).GetComponent<Image>();
            speedBarBackground.transform.SetSiblingIndex(chargeBarBackground.transform.GetSiblingIndex());

            // Delete chargebar chunk, it's not needed
            Object.DestroyImmediate(speedBarBackground.transform.GetChild(0).gameObject);

            Image speedBar = null;
            foreach (Transform t in speedBarBackground.transform)
            {
                speedBar = t.GetComponent<Image>();
                if (speedBar != null)
                {
                    break;
                }
            }

            // Don't want the swirl on the speed bar
            speedBar.material = speedBarBackground.material;


            Plugin.InitializeUI(speedBarBackground.transform, speedBar, ___tricksInComboLabel);
        }
    }
}
