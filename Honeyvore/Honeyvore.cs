using System.Collections.Generic;
using System.Linq;
using BepInEx;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Random = UnityEngine.Random;

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
        
        private static HashSet<string> HoneyItems = new();

        private void Awake()
        {
            Localization.AddTranslation("English", new Dictionary<string, string>
            {
                { "honeymessage1", "There is no honey in {item_name}, hun" },
                { "honeymessage2", "No beeswax, no {item_name}" },
                { "honeymessage3", "You can't find the Honey in {item_name}" },
                { "honeymessage4", "Error 404: Honey not found" },
                { "honeymessage5", "{item_name} lacks honey" },
            });

            ItemManager.OnItemsRegistered += OnItemsRegistered;

            Harmony.CreateAndPatchAll(typeof(Honeyvore), PluginGUID);
        }

        private static void OnItemsRegistered()
        {
            // Get all prefabs and find all CookingStation and Fermenter conversions
            var prefabs = new HashSet<GameObject>(ZNetScene.instance.m_prefabs);
            prefabs.UnionWith(ZNetScene.instance.m_namedPrefabs.Values);
            prefabs.Remove(null);
            
            var conversions = new List<(string fromItem, string toItem)>();
            foreach (var prefab in prefabs)
            {
                if (prefab.TryGetComponent<CookingStation>(out var cookingStation))
                {
                    foreach (var conversion in cookingStation.m_conversion)
                    {
                        conversions.Add((conversion.m_from.m_itemData.m_shared.m_name,
                            conversion.m_to.m_itemData.m_shared.m_name));
                    }
                }

                if (prefab.TryGetComponent<Fermenter>(out var fermenter))
                {
                    foreach (var conversion in fermenter.m_conversion)
                    {
                        conversions.Add((conversion.m_from.m_itemData.m_shared.m_name,
                            conversion.m_to.m_itemData.m_shared.m_name));
                    }
                }
            }
            
            // Build HoneyItem cache
            // Add honey to the HashSet and loop as long as we find something
            // made out of honey or its descends
            HoneyItems.Clear();
            HoneyItems.Add("$item_honey");

            bool changed = true;
            while (changed)
            {
                changed = false;

                foreach (var recipe in ObjectDB.instance.m_recipes)
                {
                    if (recipe.m_item == null) continue;
                    var outputName = recipe.m_item.m_itemData.m_shared.m_name;
                    if (HoneyItems.Contains(outputName)) continue;

                    foreach (var req in recipe.m_resources)
                    {
                        if (HoneyItems.Contains(req.m_resItem.m_itemData.m_shared.m_name))
                        {
                            changed = HoneyItems.Add(outputName);
                            break;
                        }
                    }
                }

                foreach (var conversion in conversions)
                {
                    if (HoneyItems.Contains(conversion.fromItem)
                        && HoneyItems.Add(conversion.toItem))
                    {
                        changed = true;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.CanConsumeItem)), HarmonyPrefix]
        private static bool PrefixPlayerCanConsumeItem(Player __instance, ItemDrop.ItemData item)
        {
            if (!Player.m_localPlayer || Player.m_localPlayer != __instance)
                return true;

            if (item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Consumable)
                return true;

            if (!HoneyItems.Contains(item.m_shared.m_name))
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
            var rand = Random.Range(0, keys.Length);
            var msg = $"${keys[rand]}";
            return Localization.TryTranslate(msg).Replace("{item_name}", itemName);
        }
    }
}