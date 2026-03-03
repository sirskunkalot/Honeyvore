using System.Collections.Generic;
using System.Linq;
using BepInEx;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Honeyvore
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class Honeyvore : BaseUnityPlugin
    {
        public const string PluginGUID = "de.sirskunkalot.Honeyvore";
        public const string PluginName = "Honeyvore";
        public const string PluginVersion = "0.0.1";
        
        private static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        private static List<KeyValuePair<string, string>> Conversions = new List<KeyValuePair<string, string>>();

        private void Awake()
        {
            Localization.AddTranslation("English", new Dictionary<string, string>
            {
                {"honeymessage1", "There is no honey in {item_name}, hun"},
                {"honeymessage2", "No beeswax, no {item_name}"},
                {"honeymessage3", "You can't find the Honey in {item_name}"},
                {"honeymessage4", "Error 404: Honey not found"},
                {"honeymessage5", "{item_name} lacks honey"},
            });
            
            ItemManager.OnItemsRegistered += OnItemsRegistered;
            
            Harmony.CreateAndPatchAll(typeof(Honeyvore), PluginGUID);
        }

        private static void OnItemsRegistered()
        {
            var prefabs = new HashSet<GameObject>(ZNetScene.instance.m_prefabs);
            var namedPrefabs = new HashSet<GameObject>(ZNetScene.instance.m_namedPrefabs.Values);
            var combinedPrefabs = prefabs.Union(namedPrefabs).ToList();
            combinedPrefabs.RemoveAll(prefab => !prefab);
            
            foreach (var prefab in combinedPrefabs)
            {
                if (prefab.TryGetComponent<CookingStation>(out var cookingStation))
                {
                    foreach (var conversion in cookingStation.m_conversion)
                    {
                        Jotunn.Logger.LogDebug($"added from {conversion.m_from.m_itemData.m_shared.m_name} to {conversion.m_to.m_itemData.m_shared.m_name}");
                        Conversions.Add(new KeyValuePair<string, string>(conversion.m_from.m_itemData.m_shared.m_name, conversion.m_to.m_itemData.m_shared.m_name));
                    }
                }
                if (prefab.TryGetComponent<Fermenter>(out var fermenter))
                {
                    foreach (var conversion in fermenter.m_conversion)
                    {
                        Jotunn.Logger.LogDebug($"added from {conversion.m_from.m_itemData.m_shared.m_name} to {conversion.m_to.m_itemData.m_shared.m_name}");
                        Conversions.Add(new KeyValuePair<string, string>(conversion.m_from.m_itemData.m_shared.m_name, conversion.m_to.m_itemData.m_shared.m_name));
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.CanConsumeItem)), HarmonyPrefix]
        private static bool PrefixPlayerCanConsumeItem(Player __instance, ItemDrop.ItemData item)
        {
            if (Player.m_localPlayer && Player.m_localPlayer == __instance && !HasHoney(item.m_shared.m_name))
            {
                __instance.Message(MessageHud.MessageType.Center, GetMessage(item.m_shared.m_name));
                return false;
            }

            return true;
        }
        
        private static string GetMessage(string itemName)
        {
            // var trans = Localization.GetTranslations(PlatformPrefs.GetString("language"));
            // if (trans.Count == 0)
            // {
            //     trans = Localization.GetTranslations("English");
            // }
            var trans = Localization.GetTranslations("English");
            var keys = trans.Keys.ToArray();
            var rand = Random.Range(0, keys.Length-1);
            var msg = $"${keys[rand]}";
            return Localization.TryTranslate(msg).Replace("{item_name}", itemName);
        }

        private static bool HasHoney(string itemName)
        {
            Jotunn.Logger.LogDebug(itemName);
            foreach (var conversion in Conversions)
            {
                if (conversion.Value.Equals(itemName) && !conversion.Key.Equals(itemName) && HasHoney(conversion.Key)))
                {
                    return true;
                }
            }

            foreach (var recipe in ObjectDB.instance.m_recipes)
            {
                if (recipe.m_item != null && recipe.m_item.m_itemData.m_shared.m_name.Equals(itemName))
                {
                    foreach (var req in recipe.m_resources)
                    {
                        if (HasHoney(req.m_resItem.m_itemData.m_shared.m_name))
                        {
                            return true;
                        }
                    }
                }
            }
            
            return itemName.Equals("$item_honey");
        }
    }
}

