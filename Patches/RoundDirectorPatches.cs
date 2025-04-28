using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace RemainingItemsList.Patches
{
    [HarmonyPatch(typeof(RoundDirector))]
    public static class RoundDirectorPatches
    {
        // This patch will run when the extraction is complete (map ends)
        [HarmonyPatch("ExtractionCompleted")]
        [HarmonyPostfix]
        public static void ExtractionComplete()
        {
            // Make sure we're in a level
            if (RoundDirector.instance == null || !IsInLevel())
                return;
            // Check if all extraction points are completed
            if (!RoundDirector.instance.allExtractionPointsCompleted)
                return;
            
            RemainingItemsList.Logger.LogInfo("Extraction Completed! Checking for remaining items...");
            
            // Get all remaining items
            List<ValuableObject> remainingItems = RemainingItemsList.GetRemainingItems();
            
            // Display them in the UI and log to console
            RemainingItemsList.ShowRemainingItemsUI(remainingItems);
        }
        
        // Helper to check if we're in a level
        private static bool IsInLevel()
        {
            return RoundDirector.instance != null &&
                   RoundDirector.instance.gameObject.scene.isLoaded &&
                   RoundDirector.instance.isActiveAndEnabled;
        }
    }
}
