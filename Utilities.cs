using UnityEngine;
using System.Collections;
using TMPro;

namespace RemainingItemsList
{
    public static class Utilities
    {
        // Coroutine to automatically hide the UI after a delay
        public static IEnumerator HideUIAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (RemainingItemsList.listPanelInstance)
            {
                RemainingItemsList.listPanelInstance.SetActive(false);
                RemainingItemsList.isPanelVisible = false;
            }
        }
    }
} 