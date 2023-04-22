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


namespace AllTameable
{
    [BepInPlugin("meldurson.valheim.AllTameable", "AllTameable-Overhaul", "1.1.4")]

    [BepInDependency("com.jotunn.jotunn", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("org.bepinex.plugins.creaturelevelcontrol", BepInDependency.DependencyFlags.SoftDependency)]

    internal class Plugin : BaseUnityPlugin
    {
        [Serializable]
        public class TameTable : ICloneable //All the info that can be changed for a creature
        {
            public bool commandable { get; set; } = true;
            public float tamingTime { get; set; } = 600f;
            public float fedDuration { get; set; } = 300f;
            public float consumeRange { get; set; } = 2f;
            public float consumeSearchInterval { get; set; } = 5f;
            public float consumeHeal { get; set; } = 10f;
            public float consumeSearchRange { get; set; } = 30f;
            public string consumeItems { get; set; } = "RawMeat";
            public bool changeFaction { get; set; } = true;
            public bool procretion { get; set; } = true;
            public bool procretionOverwrite { get; set; } = false;
            public int maxCreatures { get; set; } = 5;
            public float pregnancyChance { get; set; } = 0.33f;
            public float pregnancyDuration { get; set; } = 10f;
            public float growTime { get; set; } = 60f;

            //custom features
            public bool canMateWithSelf { get; set; } = true;
            public string specificOffspringString { get; set; } = "";
            public List<specificMates> ListofRandomOffspring { get; set; } = new List<specificMates>();
            public float size { get; set; } = 1f;
            public object Clone()
            {
                return MemberwiseClone();
            }
        }

        [Serializable]
        public class specificMates : ICloneable //All the info that can be changed for a creature
        {
            public string prefabName { get; set; } = "";
            public List<chanceOffspring> possibleOffspring{ get; set; } = new List<chanceOffspring>();
            public object Clone()
            {
                return MemberwiseClone();
            }
        }

        [Serializable]
        public class chanceOffspring : ICloneable //All the info that can be changed for a creature
        {
            public GameObject offspring { get; set; } = null;
            public float chance { get; set; } = 0;
            public object Clone()
            {
                return MemberwiseClone();
            }
        }





        [HarmonyPrefix]
        [HarmonyPatch(typeof(MonsterAI), "Awake")]
        //private static class MonsterAI_Awake_Patch
        //{
        private static void Prefix(MonsterAI __instance)
        {

            if (ZNet.instance.IsServer() || PostLoadServerConfig)
            {
                if (!PetManager.isInit)
                {
                    DBG.blogDebug("Updating Creature Prefabs for Procreation");
                    PetManager.Init(__instance.gameObject);
                }
                else if (!PetManager.isInit2)
                {
                    DBG.blogDebug("Updating Procreation After Load");
                    PetManager.Init2nd(__instance.gameObject);
                }
            }
        }
        //}




        [HarmonyPrefix]
        [HarmonyPatch(typeof(Character), "RPC_Heal")] //removes healing 0 text if healing less than 0.1hp
        //private static class Character_RPC_Heal_Patch
        //{
        private static void Prefix(out bool __state, long sender, float hp, ref bool showText)
        {
            //DBG.blogDebug("inPrefix"+ hp);
            __state = showText;
            if (hp < 0.1f)
            {
                showText = false;
            }
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character), "RPC_Heal")] //adds effect around creature if healing

        private static void Postfix(bool __state, Character __instance, long sender, float hp, ref bool showText)
        {
            //DBG.blogDebug("inPostfix");
            if (__state && hp < 0.1)
            {
                if (!__instance.m_nview.IsOwner())
                {
                    return;
                }
                float num = Mathf.Min(__instance.GetHealth() + hp, __instance.GetMaxHealth());
                if (num > __instance.GetHealth())
                {
                    //StatusEffect SE = se_holder.effect;
                    __instance.m_seman.AddStatusEffect(prefabManager.effect, resetTime: false);
                }
            }
        }
        //}

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Tameable), "OnConsumedItem")]
        //private static class Tameable_OnConsumedItem_Patch
        //{
        private static void Postfix(ItemDrop item, Tameable __instance) //Heals amount set in config
        {
            if (HealOnConsume.Value)
            {
                Character character = __instance.m_character;
                float hp = character.GetMaxHealth() / 10f;
                if (OverrideHealValue.Value)
                {

                    string key = __instance.name.Replace("(Clone)", "");
                    if (cfgList.ContainsKey(key))
                    {
                        hp = cfgList[key].consumeHeal;
                    }
                }
                //DBG.blogDebug("hp=" + hp);
                character.Heal(hp, true);

            }

        }
        //}

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

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) // Tries to initialise tames as late as possible before game loaded as to allow for mods to add their creatures
        {
            //DBG.blogWarning("in on scene loaded");
            if (scene.name == "main")
            {
                DBG.blogDebug("In main Load");
                if (ZNet.instance.IsServer())
                {
                    DBG.blogDebug("Copying Creature Prefabs for Procreation");
                    if (cfgList.Count() < 1)
                    {
                        if (TameListCfg.Init() == false) { initCfg(); }
                    }
                    if (!PetManager.isInit)
                    {
                        PetManager.Init();
                    }
                }
            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(ZNetScene), "Shutdown")]
        //private static class Postfix_ZNetScene_ShutDown
        //{
        private static void Postfix()
        {
            DBG.blogInfo("Clearing TameLists");
            prefabManager.Clear();
            cfgList.Clear();
            cfgListFailed.Clear();
            cfgPostList.Clear();
            CompatMatesList.Clear();
            RecruitList.Clear();
            rawMatesList = new List<string> { };
            rawTradesList = new List<string> { };
            PostMakeList.Clear();
            PostLoadServerConfig = false;
            PreSetMinis = true;


        }
        //}
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Tameable), "Tame")]
        //private static class Patch_Tameable_Tame
        //{
        private static void Postfix(Tameable __instance) //changes the faction based on config
        {
            string key = __instance.name.Replace("(Clone)", "");
            if (cfgList.ContainsKey(key) && cfgList[key].changeFaction)
            {
                Humanoid humanoid = __instance.GetComponent<Humanoid>();
                humanoid.m_faction = Character.Faction.Players;
                humanoid.m_group = "";
                //DBG.blogDebug("Changed Faction");
                //DBG.blogDebug(cfgList[key].changeFaction);
            }
        }
        //}



        [HarmonyPrefix]
        [HarmonyPatch(typeof(Fireplace), "UseItem")]
        //private static class Prefix_Fireplace_UseItem
        //{
        private static bool Prefix(Fireplace __instance, Humanoid user, ItemDrop.ItemData item, ref bool __result)
        {
            //DBG.blogDebug("Used item on fireplace " + item.GetTooltip());
            if (!HatchingEgg.Value)
            {
                return true;
            }
            if (item.m_dropPrefab.name == "DragonEgg")
            {
                //DBG.blogDebug("Is Dragonegg");
                if (!__instance.IsBurning())
                {
                    user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("You need to add more fuel before you are hatch the egg"));
                    __result = true;
                    return false;
                }
                Inventory inventory = user.GetInventory();
                user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("The egg is hatching"));
                Vector3 midpos = user.transform.position + (__instance.transform.position - user.transform.position) / 2;
                //Vector3 eggpos = user.transform.position + new Vector3(0.3f, 0.5f, 0f);
                midpos += new Vector3(0f, 0.1f, 0f);
                GameObject gameObject = UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("HatchingDragonEgg"), midpos, Quaternion.LookRotation(user.transform.position));
                //gameObject.transform.localPosition = user.transform.position + new Vector3(0f, 2f, 0f);
                //DBG.blogDebug(gameObject.transform.localPosition);
                bool itemremoved = inventory.RemoveItem(item, 1);
                __result = true;
                return false;
            }
            return true;
        }
        //}
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), "OnSpawned")]
        //private static class Prefix_Player_SetLocalPlayer
        //{
        private static void Prefix()
        {
            string playerName = Player.m_localPlayer.GetPlayerName();
            if (ThxList.Contains(playerName.ToLower()))
            {
                SetPlayerSpwanEffect();
            }
        }
        //}

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

        public static int Safe_GetNrOfInstances(GameObject prefab, Vector3 center, float maxRange, bool isError, bool eventCreaturesOnly =false, bool procreationOnly=false)
        {
            if (isError)
            {
                DBG.blogWarning("****Failed Default Number of Instances****");
                DBG.blogDebug("Prefab is null= " + !(prefab ?? false));
                try { DBG.blogDebug("center: = " + center); } catch { }
                try { DBG.blogDebug("maxRange: = " + maxRange); } catch { }
                try { DBG.blogDebug("eventCreaturesOnly: = " + eventCreaturesOnly); } catch { }
                try { DBG.blogDebug("procreationOnly: = " + procreationOnly); } catch { }
                try { DBG.blogDebug("prefab.transform: = " + prefab.transform); } catch { }
                try { DBG.blogDebug("prefab.name: = " + prefab.name); } catch { }
                try { DBG.blogDebug("prefab.gameObject.name: = " + prefab.gameObject.name); } catch { }
                try { DBG.blogDebug("prefab.ToString(): = " + prefab.ToString()); } catch { }
                try { DBG.blogDebug("prefab.GetComponent<BaseAI>() != null: = " + prefab.GetComponent<BaseAI>() != null);} catch { }
                


            }
            string text = prefab.name + "(Clone)";
            if (prefab.GetComponent<BaseAI>() != null)
            {
                
                List<BaseAI> allInstances = BaseAI.GetAllInstances();
                int num = 0;
                if (isError)
                {
                    try { DBG.blogWarning("allInstances:= " + allInstances.Count); } catch { }
                }
                
                foreach (BaseAI item in allInstances)
                {
                    if (isError)
                    {
                        try { DBG.blogDebug("item.gameObject.name:= " + item.gameObject.name); } catch { }
                        try { DBG.blogDebug("Vector3.Distance(center, item.transform.position:= " + Vector3.Distance(center, item.transform.position)); } catch { }
                        MonsterAI monsterAI = item as MonsterAI;
                        Procreation component = item.GetComponent<Procreation>();
                        try {DBG.blogDebug("MonsterAI:= " + monsterAI); } catch { }
                        try { DBG.blogDebug("!monsterAI.IsEventCreature():= " + !monsterAI.IsEventCreature()); } catch { }
                        try { DBG.blogDebug("component:= " + component); } catch { }
                        try { DBG.blogDebug("!component.ReadyForProcreation():= " + !component.ReadyForProcreation()); } catch { }
                    }
                    if (item.gameObject.name != text || (maxRange > 0f && Vector3.Distance(center, item.transform.position) > maxRange))
                    {
                        continue;
                    }
                    if (eventCreaturesOnly)
                    {
                        MonsterAI monsterAI = item as MonsterAI;
                        if ((bool)monsterAI && !monsterAI.IsEventCreature())
                        {
                            continue;
                        }
                    }
                    if (procreationOnly)
                    {
                        Procreation component = item.GetComponent<Procreation>();
                        if ((bool)component && !component.ReadyForProcreation())
                        {
                            continue;
                        }
                    }
                    num++;
                }
                return num;
                
            }
            GameObject[] array = GameObject.FindGameObjectsWithTag("spawned");
            if (isError)
            {
                try { DBG.blogWarning("array:= " + array); } catch { }
            }
            int num2 = 0;
            GameObject[] array2 = array;
            foreach (GameObject gameObject in array2)
            {
                if (isError)
                {
                    try { DBG.blogDebug("gameObject.name:= " + gameObject.name); } catch { }
                    try { DBG.blogDebug("Vector3.Distance(center, gameObject.transform.position:= " + Vector3.Distance(center, gameObject.transform.position)); } catch { }
                }
                if (gameObject.name.StartsWith(text) && (!(maxRange > 0f) || !(Vector3.Distance(center, gameObject.transform.position) > maxRange)))
                {
                    num2++;
                }
            }
            return num2;
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Procreation), "Procreate")]

        private static void PrefixProcreation(Procreation __instance)
        {
            if (!(__instance.m_myPrefab ?? false) | !(__instance.m_offspringPrefab ?? false)) //prefab is null
            {
                //DBG.blogDebug("Procreation Initialised Prefabs");
                InitProcPrefabs(__instance);
            }
            else
            {

            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Procreation), "MakePregnant")]

        private static void PostfixMakePregnant(Procreation __instance)
        {
            string prefname = __instance.name.Replace("(Clone)", ""); ;
            //DBG.blogDebug("prefname=" + prefname);
            //get offspring
            if (cfgList.TryGetValue(prefname, out TameTable cfgfile))
            {
                DBG.blogDebug("found prefab, " +prefname);
                if ((cfgfile.specificOffspringString + "") != "")
                {
                    //DBG.blogDebug("specificOffspringString=" + cfgfile.specificOffspringString);
                    if (cfgfile.ListofRandomOffspring.Count() == 0)
                    {
                        initRandomOffspring(cfgfile, __instance);
                    }
                    changeOffspring(__instance, cfgfile.ListofRandomOffspring);
                }
            }
        }

        private static void initRandomOffspring(TameTable tmtbl, Procreation proc)
        {
            //List<string[]> partnerList = new List<string[]>();
            DBG.blogDebug("initOffspring");
            
            List<specificMates> specMates = new List<specificMates>();
            Dictionary<string, string> partnersDict = new Dictionary<string, string>();
            string[] partners = tmtbl.specificOffspringString.Split(',');
            //DBG.blogDebug(partners.ToString());
            partners = partners.Skip(1).ToArray();
            //DBG.blogDebug(partners.ToString());
            foreach (string combinedValue in partners)
            {
                string[] splitValue = combinedValue.Replace(")", "").Split('(');
                partnersDict.Add(splitValue[0], splitValue[1]);
                //DBG.blogDebug("key=" + splitValue[0] + ", value=" + splitValue[1]);
                specificMates addPartner = new specificMates();
                addPartner.prefabName = splitValue[0];
                string[] prefchances = splitValue[1].Split('/');
                float totalchance = 0;
                foreach (string chancepkg in prefchances)
                {
                    chanceOffspring chanceoff = new chanceOffspring();
                    string[] pref_and_chance = chancepkg.Split(':');
                    string prefname = pref_and_chance[0];
                    GameObject mate_go = ZNetScene.instance.GetPrefab(prefname);
                    if (mate_go != null)
                    {
                        DBG.blogDebug("found go for " + mate_go.name);
                        //Procreation mate_proc = mate_go.GetComponent<Procreation>();
                        if (mate_go.GetComponent<Procreation>() != null)
                        {
                            chanceoff.offspring = mate_go.GetComponent<Procreation>().m_offspring;
                            DBG.blogDebug("chanceoff.offspring=" + chanceoff.offspring.name);
                        }
                        else
                        {
                            Growup this_growup = proc.m_offspring.GetComponent<Growup>();
                            if(this_growup != null)
                            {
                                chanceoff.offspring = PetManager.SpawnMini(mate_go, this_growup.m_growTime);
                            }
                            else
                            {
                                DBG.blogDebug("growup null");
                                chanceoff.offspring = PetManager.SpawnMini(mate_go);
                            }
                            
                            DBG.blogDebug("chanceoff.offspring=" + chanceoff.offspring.name);
                        }
                        try { chanceoff.chance = float.Parse(pref_and_chance[1]); }
                        catch { DBG.blogWarning("Not a valid float for chance for "+proc.name); }
                        //DBG.blogDebug("chanceoff.chance=" + chanceoff.chance);
                        totalchance += chanceoff.chance;
                        addPartner.possibleOffspring.Add(chanceoff);
                    }
                    else
                    {
                        DBG.blogWarning("could not find prefab:" + prefname + " when trying to mate with " + proc.name);
                    }
                    
                }
                DBG.blogDebug("totalchance=" + totalchance);
                if (totalchance < 100)
                {
                    chanceOffspring defaultOff = new chanceOffspring();
                    defaultOff.chance = 100 - totalchance;
                    defaultOff.offspring = proc.m_offspring;
                    addPartner.possibleOffspring.Add(defaultOff);
                }
                specMates.Add(addPartner);

            }
            tmtbl.ListofRandomOffspring = specMates;
            foreach (specificMates specmates in tmtbl.ListofRandomOffspring)
            {
                DBG.blogDebug("specmates.prefabName=" + specmates.prefabName);
                foreach (chanceOffspring chancepkg in specmates.possibleOffspring)
                {
                    DBG.blogDebug("     chancepkg.offspring.name=" + chancepkg.offspring.name);
                    DBG.blogDebug("     chancepkg.chance=" + chancepkg.chance);
                }
            }
        }
        private static void changeOffspring(Procreation proc, List<specificMates> mates)
        {
            DBG.blogDebug("changeOffspring");
            /*
            //List<string[]> partnerList = new List<string[]>();
            DBG.blogDebug("changeOffspring");
            Dictionary<string, string> partnersDict = new Dictionary<string, string>();
            string[] partners = spec_str.Split(',');
            //DBG.blogDebug(partners.ToString());
            partners = partners.Skip(1).ToArray();
            //DBG.blogDebug(partners.ToString());
            foreach(string combinedValue in partners)
            {
                string[] splitValue = combinedValue.Replace(")", "").Split('(');
                partnersDict.Add(splitValue[0], splitValue[1]);
                DBG.blogDebug("key=" + splitValue[0]+", value="+ splitValue[1]);
            }
            */
            Character partner = getPartner(proc.GetComponentInParent<BaseAI>());
            string prefname = partner.name.Replace("(Clone)", "");
            DBG.blogDebug("partner=" + prefname);
            specificMates foundMate = null;
            foreach (specificMates mate in mates)
            {
                if(mate.prefabName == prefname)
                {
                    foundMate = mate;
                    break;
                }
            }
            if (foundMate != null)
            {
                //foundMate.possibleOffspring
                float rndm = UnityEngine.Random.Range(0f, 100f);
                float currentchance = 0;
                
                foreach(chanceOffspring chanceOff in foundMate.possibleOffspring)
                {
                    currentchance += chanceOff.chance;
                    if(currentchance>= rndm)
                    {
                        DBG.blogDebug("currentchance=" + currentchance + ", rndm=" + rndm);
                        proc.m_offspring = chanceOff.offspring;
                        proc.m_offspringPrefab = chanceOff.offspring;
                        DBG.blogDebug("proc.m_offspring=" + proc.m_offspring.name);
                        break;
                    }
                }
            }
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
            
        }

        public static Character getPartner(BaseAI baseAI)
        {
            Character partner = null;
            float num = 999999f;
            //BaseAI baseAI = GetComponentInParent<BaseAI>();
            List<Character> characters = Character.GetAllCharacters();
            //DBG.blogDebug("This char is:" + baseai.gameObject.name);
            //List<string> possiblemates = Plugin.CompatMatesList[baseai.gameObject.name];
            if (!Plugin.CompatMatesList.TryGetValue(Utils.GetPrefabName(baseAI.gameObject), out var possiblemates))
            {
                possiblemates = new List<string> { Utils.GetPrefabName(baseAI.gameObject) };
            }
            List<string> clonemates = new List<string>();
            ZNetScene zns = ZNetScene.instance;
            clonemates.Add(baseAI.gameObject.name);
            foreach (string str in possiblemates)
            {
                clonemates.Add(str + "(Clone)");
            }
            // DBG.blogDebug("clonemates= " + string.Join(":", clonemates));

            foreach (Character character in characters)
            {
                if (!(character.gameObject == baseAI.gameObject) && clonemates.Contains(character.gameObject.name) && character.GetComponent<ZNetView>().IsValid())// && !(Vector3.Distance(character.transform.position, base.transform.position) > 40))
                {

                    float num2 = Vector3.Distance(character.transform.position, baseAI.transform.position);
                    if (num2 < num)
                    {
                        DBG.blogDebug("character with name go:" + character.gameObject.name + " is " + Vector3.Distance(character.transform.position, baseAI.transform.position) + "m away");
                        partner = character;
                        num = num2;
                    }

                }
                //if (clonemates.Contains(character.gameObject.name))
                //{
                //    DBG.blogDebug("found clone with name " + character.gameObject.name);
                //}
            }
            return partner;
        }


        public static void InitProcPrefabs(Procreation _proc)
        {
            string prefabName = Utils.GetPrefabName(_proc.m_offspring);
            _proc.m_offspringPrefab = ZNetScene.instance.GetPrefab(prefabName);
            int prefab = _proc.m_nview.GetZDO().GetPrefab();
            _proc.m_myPrefab = ZNetScene.instance.GetPrefab(prefab);
            //DBG.blogDebug("Initialised Proc Prefabs");
            if (!(_proc.m_myPrefab ?? false))
            {
                DBG.blogDebug("m_myPrefab is still Null, trying again");
                DBG.blogDebug(_proc.name);

                string proc_name = _proc.name.Replace("(Clone)", "");
                DBG.blogDebug("proc_name=" + proc_name);
                if (ZNetScene.instance.GetPrefab(proc_name) == null)
                {
                    DBG.blogDebug("proc_name failed");
                }
                else
                {
                    _proc.m_myPrefab = ZNetScene.instance.GetPrefab(proc_name);
                }
                if (!(_proc.m_myPrefab ?? false))
                {
                    DBG.blogDebug("m_myPrefab backup failed");
                }
                else
                {
                    DBG.blogDebug("m_myPrefab backup success");
                }
            }
            if (!(_proc.m_offspringPrefab ?? false))
            {
                DBG.blogDebug("m_offspringPrefab is still Null, trying again");
                string proc_offspring_name = _proc.m_offspring.name;
                DBG.blogDebug("Failed prefabName=" + prefabName);
                //_proc.m_offspringPrefab = ZNetScene.instance.GetPrefab(proc_offspring_name);

                _proc.m_offspringPrefab = _proc.m_offspring;
                if (!(_proc.m_offspringPrefab ?? false))
                {

                    DBG.blogDebug("m_offspringPrefab backup failed :"+ (_proc.m_offspringPrefab==null));
                }
                else
                {
                    DBG.blogDebug("m_offspringPrefab backup success");
                }

            }
        }



        [HarmonyPostfix]
        [HarmonyPatch(typeof(SpawnSystem), "GetNrOfInstances", new Type[] { typeof(GameObject), typeof(Vector3),typeof(float),typeof(bool),typeof(bool) })]
        //private static class Prefix_Player_SetLocalPlayer
        //{
        private static void Postfix_SpawnSystem_GetNrOfInstances(GameObject prefab, Vector3 center, float maxRange, bool eventCreaturesOnly, bool procreationOnly, ref int __result)
        {
            int sum = __result;
            if (prefab.GetComponent<Tameable>() != null)
            {
                try
                {
                    if (CompatMatesList.TryGetValue(prefab.name, out List<string> mates))
                    {
                        //DBG.blogDebug("Has more mates");
                        if (cfgList.TryGetValue(prefab.name, out TameTable cfgfile)) 
                        {
                            if (!cfgfile.canMateWithSelf)
                            {
                                int previous_sum = sum-1;
                                sum = 1;
                                //DBG.blogDebug("Cannot Mate with same prefab, " + previous_sum + " removed from total nearby, " +
                                //    "newsum ="+ sum);
                            }
                        }
                        ZNetScene zns = ZNetScene.instance;
                        foreach (var mate in mates)
                        {
                            if (zns.GetPrefab(mate))
                            {
                                int added = 0;
                                try
                                {
                                    added = Safe_GetNrOfInstances(zns.GetPrefab(mate), center, maxRange, false, eventCreaturesOnly, procreationOnly);
                                }
                                catch 
                                { 
                                    added = 0;
                                    DBG.blogDebug("Failed to add "+mate+":error");
                                } //if fails do not add
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
            return;
        }

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

        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<string> cfg;

        public static ConfigEntry<bool> HatchingEgg;

        public static ConfigEntry<int> HatchingTime;

        public static ConfigEntry<bool> useTamingTool;

        public static ConfigEntry<bool> debugout;

        public static ConfigEntry<bool> UseCLLC_Config;

        public static ConfigEntry<string> LvlProb;

        public static ConfigEntry<bool> SeekerBroodOffspring;

        public static ConfigEntry<bool> UseCustomProcreation;

        public static ConfigEntry<bool> AllowMutation;

        public static ConfigEntry<int> MutationChance;

        public static ConfigEntry<bool> HealOnConsume;

        public static ConfigEntry<bool> OverrideHealValue;

        public static ConfigEntry<float> TamedFedMultiplier;

        public static ManualLogSource logger;

        public static Dictionary<string, TameTable> cfgList = new Dictionary<string, TameTable>();
        public static Dictionary<string, TameTable> cfgPostList = new Dictionary<string, TameTable>();
        public static Dictionary<string, List<string>> CompatMatesList = new Dictionary<string, List<string>>();
        public static Dictionary<string, List<TradeAmount>> RecruitList = new Dictionary<string, List<TradeAmount>>();
        public static List<string> rawMatesList = new List<string> { };
        public static List<string> rawTradesList = new List<string> { };

        public static List<string> PostMakeList = new List<string>();

        public static TameTable CfgTable;
        public static bool PostLoadServerConfig = false;

        public static List<string> ThxList = new List<string> { "deftesthawk", "buzz", "lordbugx", "hawksword" };

        public static EffectList.EffectData firework = new EffectList.EffectData();

        public static bool loaded = false;

        public static bool listloaded = false;
        public static bool PreSetMinis = true;

        public static String tamingtoolPrefabName = "el_TamingTool";

        public static Dictionary<string, TameTable> cfgListFailed = new Dictionary<string, TameTable>();

        public static GameObject Root;

        public static PrefabManager prefabManager;


        public static bool TameUpdate = false;



        public static PetManager petManager;

        public static bool UseCLLC = false;




        private void Awake()
        {
            logger = base.Logger;
            nexusID = base.Config.Bind("Nexus", "NexusID", 1571, "Nexus mod ID for updates");
            HatchingTime = base.Config.Bind("2DragonEgg", "hatching time", 300, new ConfigDescription("how long will egg become a drake", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            //HatchingTimeSync = base.Config.Bind("2DragonEgg", "hatching time Server", 300,new ConfigDescription("how long will egg become a drake", null,new ConfigurationManagerAttributes { IsAdminOnly = true }));
            HatchingEgg = base.Config.Bind("2DragonEgg", "enable egg hatching", defaultValue: true, new ConfigDescription("this alse enable tamed drake spawn eggs", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            cfg = base.Config.Bind("1General", "Settings", "Hatchling,true,600,300,30,10,300,10,RawMeat,true,true,5,0.33,10,300"
                , "name,commandable,tamingTime,fedDuration,consumeRange,consumeSearchInterval,consumeHeal,consumeSearchRange,consumeItem:consumeItem,changeFaction,procretion,maxCreatures,pregnancyChance,pregnancyDuration,growTime,;next one;...;last one");
            useTamingTool = base.Config.Bind("1General", "useTamingTool", defaultValue: true, "Use a taming tool to have an overlay when hovering over a creature");
            debugout = base.Config.Bind("1General", "Debug Output", defaultValue: false, "Determines if debug is output to bepinex log");
            UseCLLC_Config = base.Config.Bind("2DragonEgg", "Use CLLC Level", defaultValue: true,
                new ConfigDescription("Determines if you want to attempt to use CLLC to determine the level of your hatchling", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            LvlProb = base.Config.Bind("2DragonEgg", "Hatchling Level Probabilities", "75,25,5",
                new ConfigDescription("List of the probabilities for a hatchling to spawn at a specific level ex: 75,25,5 will have a 75% chance to have 0 stars, 25% to have 1, and 5% to have two", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            SeekerBroodOffspring = base.Config.Bind("3Custom Procreation", "SeekerBrood as offspring of Seekers", defaultValue: true,
                new ConfigDescription("Determines if you want to attempt to have the seeker offspring be the Seeker Broods instead of Mini Seekers (Seeker Broods will not grow into Seekers)", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            UseCustomProcreation = base.Config.Bind("3Custom Procreation", "Is Custom Procreation Enabled", defaultValue: true,
                new ConfigDescription("Determines if you want to attempt to use CLLC integration for level/effects/infusion", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            AllowMutation = base.Config.Bind("3Custom Procreation", "Allow Mutation", defaultValue: true,
                new ConfigDescription("Determines if you want to allow for infusion/effects not from parents", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            MutationChance = base.Config.Bind("3Custom Procreation", "Mutation Chance", 5,
                new ConfigDescription("Determines chance of a mutation occuring 0 has no chance, 100 will always mutate", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            HealOnConsume = base.Config.Bind("1General", "Heal on consume", defaultValue: false,
                new ConfigDescription("Determines if you want to have tames heal a set amount on consume(pre H&H), or leave default with only regen occurring when not hungry ", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            OverrideHealValue = base.Config.Bind("1General", "UseListHealValues", defaultValue: true,
                new ConfigDescription("Determines if you want to use the consumeheal values from the TameList, if set to false will heal 10% of max health when consuming", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            TamedFedMultiplier = base.Config.Bind("1General", "TamedFedMultiplier", defaultValue: 1f,
                new ConfigDescription("Determines if after being tamed how much longer the creature will stay fed", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));

            listloaded = TameListCfg.Init();
            if (!listloaded)
            {
                DBG.blogWarning("No Tamelist Config found, using default config for creature tames");
                loaded = initCfg();
            }
            else
            {
                DBG.blogDebug("Found Tamelist Config, using this file");
                loaded = true;
            }
            if (debugout.Value == true)
            {
                DBG.blogWarning("Debug Enabled");
            }
            //loaded = initCfg();
            CfgTable = new TameTable();
            string text = "Your list has: ";
            foreach (string key in cfgList.Keys)
            {
                text = text + key + ",  ";
            }
            DBG.blogInfo("AllTameable:" + text);
            Root = new GameObject("AllTameable Root");
            prefabManager = Root.AddComponent<PrefabManager>();
            petManager = Root.AddComponent<PetManager>();

            SceneManager.sceneLoaded += OnSceneLoaded;
            UnityEngine.Object.DontDestroyOnLoad(Root);
            PerformPatches();
            // Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

            //scanMethods();
            //applyPatches();


            if (UseCLLC_Config.Value)
            {
                try
                {
                    UseCLLC = CLLC.CLLC.CheckCLLC();
                    DBG.blogInfo("Check CLLC Returned " + UseCLLC);
                }
                catch
                {
                    DBG.blogInfo("Check CLLC Failed: CLLC=" + UseCLLC);
                }
            }
            else
            {
                UseCLLC = false;
                //DBG.blogDebug("CLLC Disabled");
            }
            //clientInit();
            DBG.blogInfo("AllTameable Loaded");
            if (useTamingTool.Value)
            {
                Jotunn.Managers.PrefabManager.OnVanillaPrefabsAvailable += PrefabManager.ItemReg;
            }
            
            Jotunn.Managers.SynchronizationManager.OnConfigurationSynchronized += (obj, attr) =>
            {
                TameUpdate = true;
            };
        }
        public static bool CheckHuman(GameObject go)
        {
            try
            {
                return RRRCoreTameable.RRRCoreTameable.CheckHuman(go);
            }
            catch
            {
                return false;
            }
        }

        public void PerformPatches()
        {
            var harmony = new Harmony("meldurson.valheim.AllTameable");
            //bool startedPatching = false;

            if (Chainloader.PluginInfos.ContainsKey("com.alexanderstrada.rrrcore"))
            {
                DBG.blogInfo("Patching RRRCore");
                //harmony.PatchAll(Assembly.GetExecutingAssembly());
                harmony.PatchAll(typeof(global::RRRCoreTameable.RRRCoreTameable));
            }

            DBG.blogInfo("Patching Select");
            harmony.PatchAll(typeof(global::AllTameable.Plugin));
            DBG.blogDebug("Patched Plugin");
            harmony.PatchAll(typeof(global::AllTameable.BetterTameHover));
            DBG.blogDebug("Patched Bettertamehover");
            harmony.PatchAll(typeof(global::AllTameable.AllTame_AnimalAI));
            DBG.blogDebug("Patched animalAi");
            harmony.PatchAll(typeof(global::AllTameable.RPC.RPC));
            DBG.blogDebug("Patched RPC");
            harmony.PatchAll(typeof(global::AllTameable.CLLC.CLLCPatches));
            DBG.blogDebug("Patched CLLCPatches");
            /*
            harmony.PatchAll(typeof(global::AllTameable.Plugin.Reverse_SpawnSystem));
            DBG.blogDebug("Patched ReverseSpawn");
            harmony.PatchAll(typeof(global::AllTameable.Plugin.Prefix_GetNrOfInstances));
            DBG.blogDebug("Patched Prefix_GetNrOfInstances");
            */
            harmony.PatchAll(typeof(global::AllTameable.CLLC.CLLCPatches.InterceptProcreation));
            DBG.blogDebug("Patched InterceptProcreation");
            harmony.PatchAll(typeof(global::AllTameable.CLLC.CLLCPatches.InterceptGrowup));
            DBG.blogDebug("Patched InterceptProcreation");

            var myOriginalMethods = harmony.GetPatchedMethods();
            foreach (var method in myOriginalMethods)
            {
                DBG.blogDebug(method.ReflectedType + ":" + method.Name + " is patched");
            }


        }

        public void scanMethods()
        {
            DBG.blogDebug("In Scan Methods");
            MethodInfo[] allmethods = typeof(SpawnSystem).GetMethods();
            int j = 0;
            foreach (MethodInfo method in allmethods)
            {
                DBG.blogDebug("method " + j + ": " + method);
                if (method.Name == nameof(SpawnSystem.GetNrOfInstances))
                {
                    DBG.blogDebug("method " + j + ": " + method);
                    try
                    {
                        DBG.blogDebug("method a =:" + method.GetParameters()[0].Name);
                    }
                    catch { }
                    try
                    {
                        DBG.blogDebug("method b =:" + method.GetParameters().Length);
                    }
                    catch { }
                    try
                    {
                        DBG.blogDebug("method c =:" + method.GetParameters()[0].ParameterType);
                    }
                    catch { }
                    try
                    {
                        DBG.blogDebug("method d =:" + method.ContainsGenericParameters);
                    }
                    catch { }

                }
                j++;
            }
        }



        public bool initCfg()
        {
            if (cfg.Value == "")
            {
                DBG.blogWarning("CFG is empty");
                return false;
            }
            const string reduceMultiSpaceCFG = @"[ ]{2,}";
            //string cfg_string = (string)cfg.Value;
            var cfg_string = Regex.Replace(cfg.Value.Replace("\t", " "), reduceMultiSpaceCFG, "");
            cfg_string = cfg_string.Replace("TRUE", "true").Replace("FALSE", "false");
            DBG.blogWarning(cfg_string);
            string[] array = cfg_string.Split(';');
            string[] array2 = array;
            foreach (string text in array2)
            {
                string[] array3 = text.Split(',');
                if (array3.Length == 8 || array3.Length == 9)
                {
                    DBG.blogWarning("Upadate your cfg : " + text);
                    return false;
                }
                if (array3.Length != 15)
                {
                    DBG.blogWarning("Not enought args : " + text);
                    return false;
                }
                TameTable tameTable = new TameTable();
                string key = array3[0];
                if (array3[1] == "true" || array3[1] == "TRUE")
                {
                    tameTable.commandable = true;
                }
                else
                {
                    tameTable.commandable = false;
                }
                try
                {
                    tameTable.tamingTime = float.Parse(array3[2]);
                    tameTable.fedDuration = float.Parse(array3[3]);
                    tameTable.consumeRange = float.Parse(array3[4]);
                    tameTable.consumeSearchInterval = float.Parse(array3[5]);
                    tameTable.consumeHeal = float.Parse(array3[6]);
                    tameTable.consumeSearchRange = float.Parse(array3[7]);
                }
                catch (Exception data)
                {
                    DBG.blogWarning("wrong syntax : " + text);
                    logger.LogError(data);
                    return false;
                }
                tameTable.consumeItems = array3[8];
                if (array3[9] == "false")
                {
                    tameTable.changeFaction = false;
                }
                if (array3[10] == "false")
                {
                    tameTable.procretion = false;
                }
                try
                {
                    float result = 0.33f;
                    tameTable.maxCreatures = int.Parse(array3[11]);
                    if (float.TryParse(array3[12], out result))
                    {
                        tameTable.pregnancyChance = result;
                    }
                    tameTable.pregnancyDuration = float.Parse(array3[13]);
                    tameTable.growTime = float.Parse(array3[14]);
                }
                catch (Exception data2)
                {
                    DBG.blogWarning("wrong syntax : " + text);
                    logger.LogError(data2);
                    return false;
                }
                cfgList.Add(key, tameTable);
            }
            DBG.blogInfo("TameTable Loaded :" + cfgList.Count);
            return true;
        }

        private static void SetPlayerSpwanEffect()
        {
            if (!Player.m_localPlayer.m_spawnEffects.m_effectPrefabs.Contains(firework))
            {
                Array.Resize(ref Player.m_localPlayer.m_spawnEffects.m_effectPrefabs, 1);
                Player.m_localPlayer.m_spawnEffects.m_effectPrefabs[0] = firework;
            }
        }

    }
}
