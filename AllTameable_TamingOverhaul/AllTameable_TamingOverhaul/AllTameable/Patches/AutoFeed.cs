using AutoFeed;
using UnityEngine;
using HarmonyLib;

namespace AllTameable.AutoFeedAT
{
    internal class AutoFeedAT
    {

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AutoFeed.BepInExPlugin), "ConsumeItem")]
        private static void Postfix(ItemDrop.ItemData item, MonsterAI monsterAI, Character character)
        {
            string iDropName = item.m_dropPrefab.name;
            Tameable tame = monsterAI.gameObject.GetComponent<Tameable>();
            if ((bool)tame)
            {
                DBG.blogDebug("Using Autofeed Postfix");
                Plugin.tryReduceTameTime(iDropName, tame);
            }
            
           
        }

    }
}
