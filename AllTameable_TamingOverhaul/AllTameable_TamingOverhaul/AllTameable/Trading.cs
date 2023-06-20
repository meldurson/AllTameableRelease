using AllTameable;
using AllTameable.RPC;
using BepInEx;
using HarmonyLib;
using Jotunn.Managers;
using System.Collections.Generic;
//using System;
using System.Reflection;
using UnityEngine;

namespace AllTameable.Trading
{
    public class Trading : BaseUnityPlugin
    {
        //private static ZNetScene zns;

        //private static Tameable wtame;

        public static GameObject Root;

        private void Awake()
        {
            Root = new GameObject("Trades");
            Root.transform.SetParent(Plugin.prefabManager.Root.transform);

        }

        //Patch TameableUseItem to go to AllTame_Interactable if not implimented
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Tameable), "UseItem")]
        //private static class Prefix_Player_SetLocalPlayer
        //{
        private static void Postfix_Tameable_UseItem(Tameable __instance, Humanoid user, ItemDrop.ItemData item, ref bool __result)
        {
            if (!__result)
            {
                Interactable alltame_interact = __instance.gameObject.GetComponentInParent<AllTameable.AllTame_Interactable>();
                if (alltame_interact != null && alltame_interact.UseItem(user, item))
                {
                    //DBG.blogDebug("GotType");
                    __result = true;
                    return;
                }
            }
        }


    }
}
