using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using System.Text;
using System.Text.RegularExpressions;

namespace RemainingItemsList
{
    [BepInPlugin("YourName.RemainingItemsList", "RemainingItemsList", "1.0.0")]
    public class RemainingItemsList : BaseUnityPlugin
    {
        internal static RemainingItemsList Instance { get; private set; } = null!;
        internal new static ManualLogSource Logger => Instance._logger;
        private ManualLogSource _logger => base.Logger;

        public static GameObject listPanelInstance;
        public static TextMeshProUGUI itemListText;
        
        // Config options
        public static float AutoHideDelay = 30f; // Automatically hide after 30 seconds
        public static bool isPanelVisible = false;
        
        internal Harmony? Harmony { get; set; }
        
        private void Awake()
        {
            Instance = this;

            // Prevent the plugin from being deleted
            this.gameObject.transform.parent = null;
            this.gameObject.hideFlags = HideFlags.HideAndDontSave;

            Patch();

            Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
        }

        // Get all remaining valuable objects in the level
        public static List<ValuableObject> GetRemainingItems()
        {
            List<ValuableObject> valuableObjects = Object.FindObjectsOfType<ValuableObject>().ToList();
            return valuableObjects;
        }
        
        // Create and display the UI for showing remaining items
        public static void ShowRemainingItemsUI(List<ValuableObject> items)
        {
            if (items == null || items.Count == 0)
            {
                Logger.LogInfo("No items remaining in the level");
                return;
            }

            if (listPanelInstance == null)
            {
                CreateItemListUI();
            }

            // Format the items list with values
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("REMAINING ITEMS:");
            sb.AppendLine("----------------");
            
            float totalValue = 0f;
            
            // Group items by name and calculate total value for each type
            var groupedItems = items
                .GroupBy(item => CleanItemName(item.name))
                .Select(group => new 
                {
                    Name = group.Key,
                    Count = group.Count(),
                    TotalValue = group.Sum(item => item.dollarValueCurrent)
                })
                .OrderByDescending(g => g.TotalValue);
                
            foreach (var group in groupedItems)
            {
                // Format with green dollar amount and tighter spacing
                sb.Append($"{group.Name} x{group.Count}: <color=#00FF00>${group.TotalValue:F0}</color>\n");
                totalValue += group.TotalValue;
            }
            
            sb.AppendLine("----------------");
            sb.AppendLine($"TOTAL VALUE: <color=#00FF00>${totalValue:F0}</color>");
            sb.AppendLine("");
            sb.AppendLine("<color=#FFFF00>Press G to close</color>");
            
            // Update the UI text
            itemListText.text = sb.ToString();
            listPanelInstance.SetActive(true);
            isPanelVisible = true;
            
            // Calculate content height and adjust panel size if needed
            ResizePanelToFitContent();
            
            // Automatically hide after delay
            Instance.StartCoroutine(Utilities.HideUIAfterDelay(AutoHideDelay));
            
            // Also log to console
            Logger.LogInfo("\n" + sb.ToString());
        }
        
        // Helper method to clean item names
        private static string CleanItemName(string name)
        {
            // Remove "(Clone)" suffix
            string cleanName = name.Replace("(Clone)", "").Trim();
            
            // Remove "Valuable" prefix if it exists
            cleanName = Regex.Replace(cleanName, @"^Valuable\s*", "", RegexOptions.IgnoreCase);
            
            return cleanName;
        }
        
        // Resize panel to fit content
        private static void ResizePanelToFitContent()
        {
            if (itemListText == null || listPanelInstance == null)
                return;
                
            // Trigger text measuring
            itemListText.ForceMeshUpdate();
            
            // Get content height plus some padding
            float contentHeight = itemListText.preferredHeight + 20f;
            float minHeight = 250f; // Minimum height
            float maxHeight = 500f; // Maximum height
            
            // Clamp height between min and max
            float newHeight = Mathf.Clamp(contentHeight, minHeight, maxHeight);
            
            // Update panel size
            RectTransform bgRect = listPanelInstance.transform.GetChild(0).GetComponent<RectTransform>();
            RectTransform panelRect = listPanelInstance.GetComponent<RectTransform>();
            
            if (bgRect != null && panelRect != null)
            {
                bgRect.sizeDelta = new Vector2(200, newHeight);
                panelRect.sizeDelta = new Vector2(200, newHeight);
            }
        }
        
        // Create the UI elements for the items list
        private static void CreateItemListUI()
        {
            GameObject hud = GameObject.Find("Game Hud");
            if (hud == null)
            {
                Logger.LogError("Could not find Game Hud");
                return;
            }
            
            // Find a font to use
            TMP_FontAsset font = null;
            GameObject taxHaul = GameObject.Find("Tax Haul");
            if (taxHaul != null)
            {
                font = taxHaul.GetComponent<TMP_Text>()?.font;
            }
            
            // Create panel
            listPanelInstance = new GameObject("Remaining Items Panel");
            listPanelInstance.transform.SetParent(hud.transform, false);
            
            // Create background panel
            GameObject background = new GameObject("Background");
            background.transform.SetParent(listPanelInstance.transform, false);
            background.AddComponent<RectTransform>();
            background.AddComponent<CanvasRenderer>();
            
            var image = background.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0f, 0f, 0f, 0.7f);
            
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.5f, 0.5f);
            bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            bgRect.sizeDelta = new Vector2(200, 250); // Initial size
            bgRect.anchoredPosition = Vector2.zero;
            
            // Create text object
            GameObject textObj = new GameObject("ItemListText");
            textObj.transform.SetParent(background.transform, false);
            
            itemListText = textObj.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                itemListText.font = font;
            }
            itemListText.fontSize = 16;
            itemListText.color = new Color(0.7882f, 0.9137f, 0.902f, 1);
            itemListText.alignment = TextAlignmentOptions.Top;
            itemListText.enableWordWrapping = true;
            itemListText.lineSpacing = -10f; // Reduce line spacing for tighter layout
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(-20, -20);
            textRect.anchoredPosition = Vector2.zero;
            
            // Position the panel in the center of the screen
            RectTransform panelRect = listPanelInstance.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(200, 250);
            panelRect.anchoredPosition = Vector2.zero;
            
            // Initially hide the panel
            listPanelInstance.SetActive(false);
            isPanelVisible = false;
        }

        private void Update()
        {
            // Listen for G key to close panel
            if (isPanelVisible && Input.GetKeyDown(KeyCode.G))
            {
                if (listPanelInstance != null)
                {
                    listPanelInstance.SetActive(false);
                    isPanelVisible = false;
                }
            }
        }

        internal void Patch()
        {
            Harmony ??= new Harmony(Info.Metadata.GUID);
            Harmony.PatchAll();
        }

        internal void Unpatch()
        {
            Harmony?.UnpatchSelf();
        }
    }
} 