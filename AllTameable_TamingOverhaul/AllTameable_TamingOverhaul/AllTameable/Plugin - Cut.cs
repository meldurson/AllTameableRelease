using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using Jotunn.Entities;
using System.Collections;
using Jotunn.Managers;


namespace AllTameable
{
    [BepInPlugin("meldurson.valheim.AllTameable", "AllTameable-Overhaul", "1.1.5")]

    [BepInDependency("com.jotunn.jotunn", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("org.bepinex.plugins.creaturelevelcontrol", BepInDependency.DependencyFlags.SoftDependency)]

    public class Plugin : BaseUnityPlugin
    {



        /*

       [HarmonyPostfix]
       [HarmonyPatch(typeof(Tameable), "Interact")]
       private static void Postfix(Humanoid user, bool hold, bool alt, Tameable __instance) 
       {
           if (__instance == null || __instance.m_character == null)
               return;
           //Show some dialog when an Talking NPC follows the player.
           NpcTalk talk = ((Component)__instance.m_character).GetComponent<NpcTalk>();
           if (talk != null)
           {
               var monsterAI = __instance.GetComponentInParent<MonsterAI>();
               if (monsterAI != null)
               {
                   if (!__instance.m_character.IsTamed())
                   {
                       talk.QueueSay(new List<string>() { "Hire me for a Black Core?", "I'd fight anything for a Black Core" }, "Greet", null);
                   }
                   else if (monsterAI.GetFollowTarget() != null)
                   {
                       talk.QueueSay(new List<string>() { "Where to boss?", "Lets do it!" }, "Greet", null);
                   }
                   else
                   {
                       talk.QueueSay(new List<string>() { "Waiting here.", "Guarding." }, "Talk", null);
                   }
               }
           }
       }

       */




        //*************Testing Recruiting*****************
        /*
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Tameable), "Interact")]
        //private static class Prefix_Player_SetLocalPlayer
        //{
        private static void Prefix(Tameable __instance, Humanoid user, bool hold, bool alt)
        {
            DBG.blogDebug("in Interact prefix");
            DBG.blogDebug("__instance=" + __instance);
            DBG.blogDebug("alt=" + alt);
        }
        */




        //*****Moved to AllTame_Interactable.cs*****

        /*
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Humanoid), "UseItem")]
        //private static class Prefix_Player_SetLocalPlayer
        //{
        private static void Prefix(Humanoid __instance, Inventory inventory, ItemDrop.ItemData item, bool fromInventoryGui)
        {
            try
            {
                int costtoRecruit = 5;
                //DBG.blogDebug("in UseItem prefix");
                //DBG.blogDebug("__instance=" + __instance);
                GameObject hoverObject = __instance.GetHoverObject(); //get go that looking at
                Humanoid hoverCreature = hoverObject.GetComponent<Humanoid>(); //get the humanoid
                Inventory inv2 = __instance.GetInventory(); //get players inventory
                DBG.blogDebug("item.m_dropPrefab.name=" + item.m_dropPrefab.name); //item using
                if (item.m_dropPrefab.name == "BlackCore")
                {
                    int numininventory = inv2.CountItems(item.m_shared.m_name); // checks how many cores player has
                    DBG.blogDebug("Number of " + item.m_dropPrefab.name + " is " + numininventory);
                    if (numininventory >= costtoRecruit)
                    {
                        DBG.blogDebug("Attempting Trade");
                        if (!hoverCreature.IsTamed())
                        {
                            Tameable tame = new Tameable();
                            tame = hoverCreature.gameObject.GetComponent<Tameable>();

                            if (tame != null)
                            {
                                tame.Tame();
                                bool itemremoved = inv2.RemoveItem(item, costtoRecruit);
                                DBG.blogDebug("Recruited Dverger");
                            }
                            else
                            {
                                DBG.blogDebug("No tameable");
                            }
                        }
                    }
                    else
                    {
                        DBG.blogDebug("Not enough Black cores, need " + costtoRecruit);
                    }
                }
            }catch(Exception ex)
            {

            }
        }

        */

        /*
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Character), "ApplyDamage")]
        //private static class Character_RPC_Heal_Patch
        //{
        private static bool Prefix(ref Character __instance, ref HitData hit)
        {
            DBG.blogDebug("In All Tameable ApplyDamage Prefix");
            try
            {
                DBG.blogDebug("__instance.name= " + __instance.name);
            }
            catch
            {
                DBG.blogDebug("__instance.name= failed");
            }
            try
            {
                DBG.blogDebug("__instance= " + __instance);
            }
            catch
            {
                DBG.blogDebug("__instance= failed");
            }

            try
            {
                DBG.blogDebug("hit is null= " +  hit == null);
            }
            catch
            {
                DBG.blogDebug("hit is null= failed");
            }
            try
            {
                DBG.blogDebug("hit.m_attacker= " + hit.m_attacker);
            }
            catch
            {
                DBG.blogDebug("hit.m_attacker= failed");
            }
            try
            {
                DBG.blogDebug("hit.m_damage= " + hit.m_damage);
            }
            catch
            {
                DBG.blogDebug("hit.m_damage= failed");
            }
            try
            {
                DBG.blogDebug("hit.GetAttacker()= " + hit.GetAttacker().name);
            }
            catch
            {
                DBG.blogDebug("hit.GetAttacker().name= failed");
            }
            DBG.blogDebug("Getting data of HitData");
            foreach (FieldInfo prop in typeof(HitData).GetFields())
            {
                try
                {
                        DBG.blogDebug(prop.Name + " is " + prop.GetValue(hit));

                }
                catch
                {
                    //DBG.blogDebug("AllTame_AnimalAI does not have " + prop.Name);
                }
            }


            if (__instance is null)
            {
                DBG.blogDebug("__instance is Null skipping Error");
                return false;
            }
            DBG.blogDebug("__instance is not Null");
            return true;

        }



        [HarmonyPrefix]
        [HarmonyPatch(typeof(Character), "Damage")]
        //private static class Character_RPC_Heal_Patch
        //{
        private static void Prefix2(ref Character __instance, ref HitData hit)
        {
            DBG.blogDebug("*******************In All Tameable Damage Prefix*************");
            try
            {
                DBG.blogDebug("__instance.name= " + __instance.name);
            }
            catch
            {
                DBG.blogDebug("__instance.name= failed");
            }
            try
            {
                DBG.blogDebug("__instance= " + __instance);
            }
            catch
            {
                DBG.blogDebug("__instance= failed");
            }
            try
            {
                DBG.blogDebug("hit is null= " + hit == null);
            }
            catch
            {
                DBG.blogDebug("hit is null= failed");
            }
            try
            {
                DBG.blogDebug("hit.m_attacker= " + hit.m_attacker);
            }
            catch
            {
                DBG.blogDebug("hit.m_attacker= failed");
            }
            try
            {
                DBG.blogDebug("hit.m_damage= " + hit.m_damage);
            }
            catch
            {
                DBG.blogDebug("hit.m_damage= failed");
            }
            try
            {
                DBG.blogDebug("hit.GetAttacker()= " + hit.GetAttacker().name);
            }
            catch
            {
                DBG.blogDebug("hit.GetAttacker().name= failed");
            }
            DBG.blogDebug("Getting data of HitData");
            foreach (FieldInfo prop in typeof(HitData).GetFields())
            {
                try
                {
                    DBG.blogDebug(prop.Name + " is " + prop.GetValue(hit));

                }
                catch
                {
                    //DBG.blogDebug("AllTame_AnimalAI does not have " + prop.Name);
                }
            }
            DBG.blogDebug("trying get attacker");
            Character attacker = hit.GetAttacker();
            DBG.blogDebug("got attacker");
            try
            {
                DBG.blogDebug("************attacker.name=" + attacker.name);
            }
            catch
            {
                DBG.blogDebug("****attacker.name=failed");
            }
            try
            {
                DBG.blogDebug("************attacker.m_name=" + attacker.m_name);
            }
            catch
            {
                DBG.blogDebug("*****attacker.m_name=failed");
            }
            try
            {
                DBG.blogDebug("attacker ==null=" + attacker == null);
            }
            catch
            {
                DBG.blogDebug("attacker ==null=failed");
            }
            foreach (FieldInfo prop2 in typeof(Character).GetFields())
            {
                try
                {
                    DBG.blogDebug(prop2.Name + " is " + prop2.GetValue(attacker));

                }
                catch
                {
                    DBG.blogDebug("*** "+prop2.Name + " failed");
                }
            }
            try
            {
                DBG.blogDebug("attacker.m_nview.IsValid()=" + attacker.m_nview.IsValid());
            }
            catch
            {
                DBG.blogDebug("**attacker.m_nview.IsValid()=failed");
            }

            try
            {
                DBG.blogDebug("attacker.GetZDOID()="+attacker.GetZDOID());
            }
            catch
            {
                DBG.blogDebug("attacker.GetZDOID()=failed");
            }
            try
            {
                DBG.blogDebug("__instance.GetComponent<Tameable>().name=" + __instance.GetComponent<Tameable>().name);
            }
            catch
            {
                DBG.blogDebug("__instance.GetComponent<Tameable>().name=failed");
            }
            try
            {
                if (attacker == __instance.GetComponent<Tameable>().GetPlayer(attacker.GetZDOID()))
                {
                    DBG.blogDebug("attacker= true");
                }
                else
                {
                    DBG.blogDebug("attacker= false");
                }
            }
            catch
            {

            }
            ZDO zDO = __instance.m_nview.GetZDO();
            Tameable component = __instance.GetComponent<Tameable>();
            DBG.blogDebug("1");
            if (__instance.IsTamed() && zDO != null && hit != null && !(component == null) && ShouldIgnoreDamage(__instance, hit, zDO))
            {
                hit = new HitData();
            }

        }

        private static bool ShouldIgnoreDamage(Character __instance, HitData hit, ZDO zdo)
        {
            DBG.blogDebug("2");
            if (true)
            {
                DBG.blogDebug("3");
                Character attacker = hit.GetAttacker();
                DBG.blogDebug("4");
                if (attacker == __instance.GetComponent<Tameable>().GetPlayer(attacker.GetZDOID()))
                {
                    DBG.blogDebug("5");
                    return false;
                }
                DBG.blogDebug("6");
            }
            return true;
        }

        */


        /* old getrninstances

            [HarmonyPatch]
        public class Reverse_SpawnSystem
        {
            [HarmonyReversePatch]
            [HarmonyPatch]

            public static int GetInstNum(GameObject prefab, Vector3 center, float maxRange, bool eventCreaturesOnly = false, bool procreationOnly = false)
            {
                // its a stub so it has no initial content
                throw new NotImplementedException("It's a stub");
            }
            static MethodBase TargetMethod()
            {
                // use normal reflection or helper methods in <AccessTools> to find the method/constructor
                // you want to patch and return its MethodInfo/ConstructorInfo
                //
                //var type = AccessTools.FirstInner(typeof(TheClass), t => t.Name.Contains("Stuff"));
                MethodInfo tryGetMethod = typeof(SpawnSystem).GetMethods().First(m => m.Name == nameof(SpawnSystem.GetNrOfInstances) && m.GetParameters().Length == 5 && m.GetParameters()[0].ParameterType == typeof(UnityEngine.GameObject));
                return tryGetMethod;
            }
        }


        //[HarmonyPatch(typeof(SpawnSystem), nameof(SpawnSystem.GetNrOfInstances),new Type[] {typeof(GameObject), typeof(Vector3) })]
        //[HarmonyPrefix]
        [HarmonyPatch]
        class Prefix_GetNrOfInstances
        {
            static MethodBase TargetMethod()
            {
                // use normal reflection or helper methods in <AccessTools> to find the method/constructor
                // you want to patch and return its MethodInfo/ConstructorInfo
                //
                //var type = AccessTools.FirstInner(typeof(TheClass), t => t.Name.Contains("Stuff"));
                MethodInfo tryGetMethod = typeof(SpawnSystem).GetMethods().First(m => m.Name == nameof(SpawnSystem.GetNrOfInstances) && m.GetParameters().Length == 5 && m.GetParameters()[0].ParameterType == typeof(UnityEngine.GameObject));
                return tryGetMethod;
            }
            private static bool Prefix(GameObject prefab, Vector3 center, float maxRange, ref int __result, bool eventCreaturesOnly = false, bool procreationOnly = false)
            {
                //DBG.blogDebug("Starting InstNum Prefix for "+prefab.name);
                // DBG.blogDebug("prefab=" + prefab.name);
                int sum = 0;
                try
                {
                    
                    sum += Reverse_SpawnSystem.GetInstNum(prefab, center, maxRange, eventCreaturesOnly, procreationOnly);
                }
                catch
                {
                    DBG.blogWarning("ERROR: Failed initial GetInstNum");
                } //if fails do not add
                if (prefab.GetComponent<Tameable>() != null)
                {
                    try
                    {
                        if (CompatMatesList.TryGetValue(prefab.name, out List<string> mates))
                        {
                            ZNetScene zns = ZNetScene.instance;
                            foreach (var mate in mates)
                            {
                                if (zns.GetPrefab(mate))
                                {
                                    int added = 0;
                                    try
                                    {
                                        added = Reverse_SpawnSystem.GetInstNum(zns.GetPrefab(mate), center, maxRange, eventCreaturesOnly, procreationOnly);
                                    }
                                    catch{added = 0;} //if fails do not add
                                    if (added > 0)
                                    {
                                        //DBG.blogDebug("Added " + added + " of " + mate + "to instnum for " + prefab.name);
                                    }
                                    sum += added;
                                }
                                else
                                {
                                    DBG.blogDebug("Failed to find mate of " + mate + "to instnum for " + prefab.name);
                                }
                            }

                        }
                    }
                    catch { DBG.blogDebug("Error in finding mate for " + prefab.name); }
                }
                __result = sum;
                return false;
            }

        }

        */ // old getnrinstances


        /*
        if(partnersDict.TryGetValue(partner.name.Replace("(Clone)", ""), out var chancesStr))
        {
            DBG.blogDebug("chancesStr=" + chancesStr);
            string[] prefchances = chancesStr.Split('/');
            float rndm = UnityEngine.Random.Range(0, 100);
            float currentchance = 0;
            foreach(string chancepkg in prefchances)
            {
                string[] pref_and_chance = chancepkg.Split(':');
                string prefname = pref_and_chance[0];
                float chance = 0;
                try {chance = float.Parse(pref_and_chance[1]);}catch { }
                currentchance = Mathf.Min(100, currentchance + chance);
                DBG.blogDebug("currentchance=" + currentchance + ", rndm=" + rndm);
                if(currentchance > rndm)
                {
                    GameObject mate_go = ZNetScene.instance.GetPrefab(prefname);
                    if (mate_go != null)
                    {
                        DBG.blogDebug("found go for " + mate_go.name);
                        Procreation mate_proc = mate_go.GetComponent<Procreation>();
                        if(mate_go.GetComponent<Procreation>() != null)
                        {

                            proc.m_offspringPrefab = mate_go.GetComponent<Procreation>().m_offspring;
                            DBG.blogDebug("proc.m_offspring=" + proc.m_offspring.name);
                            break;
                        }

                    }
                    else
                    {
                        DBG.blogWarning("could not find prefab:" + prefname + " when trying to mate with " + proc.name);
                    }
                }
            }
        }
        else
        {
            Procreation partner_proc = partner.GetComponentInParent<Procreation>();

            DBG.blogDebug("partner offspring=" + partner_proc.m_offspringPrefab.name);
        }
        */



        /*
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SpawnSystem), "GetNrOfInstances")]
        private static void Prefix_SpawnSystem_GetNrOfInstances(GameObject prefab, Vector3 center, float maxRange, bool eventCreaturesOnly, bool procreationOnly)
        {
            DBG.blogDebug("GetNrInstances:" + prefab.name);
        }
        */




        /*
        private static class Postfix_GetNrOfInstances
        {
            private static void Postfix()
            {

            }
        }
        */









        /*
        private IEnumerator jot_RPCServerReceive(long sender, ZPackage package)
        {
            DBG.blogDebug("Received Tamelist RPC Server");

            if (RPC.RPC.tamelistPkg == null)
            {
                DBG.blogDebug("Packing Tamelist Pkg");
                RPC.RPC.tamelistPkg = new RPC.CfgPackage.Pack();
            }
            if(RPC.RPC.tamelistPkg == null)
            {
                DBG.blogDebug("RPC Still Null");
            }
            yield return null;

            DBG.blogDebug("Sending now");
            jot_TamelistRPC.SendPackage(ZNet.instance.m_peers, new ZPackage(RPC.RPC.tamelistPkg.GetArray()));
            DBG.blogDebug("Sent");
        }

                    //yield return HalfSecondWait;

            string dot = string.Empty;
            for (int i = 0; i < 5; ++i)
            {
                dot += ".";
                Jotunn.Logger.LogMessage(dot);
                DBG.blogDebug("Processing=" + jot_TamelistRPC.IsProcessing);
                DBG.blogDebug("Processing Other=" + jot_TamelistRPC.IsProcessingOther);
                DBG.blogDebug("Receiving=" + jot_TamelistRPC.IsReceiving);
                DBG.blogDebug("IsSending=" + jot_TamelistRPC.IsSending);
                yield return HalfSecondWait;
            }




        // React to the RPC call on a client
        private IEnumerator jot_RPCClientReceive(long sender, ZPackage package)
        {
            DBG.blogDebug("Received Tamelist RPC Client");
            if (jot_TamelistRPC.IsProcessingOther)
            {
                yield break;
            }
            yield return null;


            if (package == null)
            {
                DBG.blogDebug("Client RPC Still Null");
            }
            if (package.GetArray().Length<1)
            {
                DBG.blogDebug("Client RPC too short");
            }
            DBG.blogDebug("Jotunn Unpack Tamelist RPC");
            //byte[] array = package.ReadByteArray();
            //DBG.blogDebug("made byte array");
            //DBG.blogDebug("Size="+ package.Size());
            //DBG.blogDebug("String=" + package.ToString());
            try
            {
                RPC.CfgPackage.Unpack(package);
                PetManager.UpdatesFromServer();
            }
            catch
            {
                DBG.blogDebug("Failed Unpack");
            }

        }
        */












    }
