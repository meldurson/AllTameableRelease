using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace AllTameable
{
    [BepInPlugin("meldurson.valheim.AllTameable", "AllTameable-Overhaul", "1.1.1")]

    [BepInDependency("com.jotunn.jotunn", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("org.bepinex.plugins.creaturelevelcontrol", BepInDependency.DependencyFlags.SoftDependency)]

    internal class Plugin : BaseUnityPlugin
    {
        [Serializable]
        public class TameTable : ICloneable
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
            public int maxCreatures { get; set; } = 5;
            public float pregnancyChance { get; set; } = 0.33f;
            public float pregnancyDuration { get; set; } = 10f;
            public float growTime { get; set; } = 60f;
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
        [HarmonyPatch(typeof(Character), "RPC_Heal")]
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
        [HarmonyPatch(typeof(Character), "RPC_Heal")]

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
            private static void Postfix(ItemDrop item, Tameable __instance)
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

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
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
                rawMatesList = new string[] { };
                PostMakeList.Clear();
                PostLoadServerConfig = false;
                PreSetMinis = true;


            }
        //}
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Tameable), "Tame")]
        //private static class Patch_Tameable_Tame
        //{
            private static void Postfix(Tameable __instance)
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

               // DBG.blogDebug("prefab=" + prefab.name);
                int sum = 0;
                sum += Reverse_SpawnSystem.GetInstNum(prefab, center, maxRange, eventCreaturesOnly, procreationOnly);
                if (prefab.GetComponent<Tameable>() != null)
                {
                    if(CompatMatesList.TryGetValue(prefab.name,out List<string> mates))
                    {
                        ZNetScene zns = ZNetScene.instance;
                        foreach(var mate in mates)
                        {
                            if (zns.GetPrefab(mate))
                            {
                                int added = Reverse_SpawnSystem.GetInstNum(zns.GetPrefab(mate), center, maxRange, eventCreaturesOnly, procreationOnly);
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
                __result = sum;
                return false;
            }

        }
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

        public static ManualLogSource logger;

        public static Dictionary<string, TameTable> cfgList = new Dictionary<string, TameTable>();
        public static Dictionary<string, TameTable> cfgPostList = new Dictionary<string, TameTable>();
        public static Dictionary<string, List<string>> CompatMatesList = new Dictionary<string, List<string>>();
        public static string[] rawMatesList = new string[] { };
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
                new ConfigDescription("Determines if you want to attempt to have the seeker offspring be the Seeker Broods instead of mini Seekers", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
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
            Jotunn.Managers.PrefabManager.OnVanillaPrefabsAvailable += PrefabManager.ItemReg;
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

        public void PerformPatches ()
        {
            var harmony = new Harmony("meldurson.valheim.AllTameable");
            //bool startedPatching = false;

            if (Chainloader.PluginInfos.ContainsKey("com.alexanderstrada.rrrcore"))
            {
                DBG.blogInfo("Patching RRRCore");
                //harmony.PatchAll(Assembly.GetExecutingAssembly());
                harmony.PatchAll(typeof(global::RRRCoreTameable.RRRCoreTameable));
            }
            
                //DBG.blogInfo("Patching Select");
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
                harmony.PatchAll(typeof(global::AllTameable.Plugin.Reverse_SpawnSystem));
                DBG.blogDebug("Patched ReverseSpawn");
                harmony.PatchAll(typeof(global::AllTameable.Plugin.Prefix_GetNrOfInstances));
                DBG.blogDebug("Patched Prefix_GetNrOfInstances");
                harmony.PatchAll(typeof(global::AllTameable.CLLC.CLLCPatches.InterceptProcreation));
                DBG.blogDebug("Patched InterceptProcreation");
                harmony.PatchAll(typeof(global::AllTameable.CLLC.CLLCPatches.InterceptGrowup));
                DBG.blogDebug("Patched InterceptProcreation");

                var myOriginalMethods = harmony.GetPatchedMethods();
                foreach (var method in myOriginalMethods) 
                {
                    DBG.blogDebug(method.ReflectedType+":"+method.Name+" is patched");
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
                if (array3[9] == "true")
                {
                    tameTable.changeFaction = true;
                }
                if (array3[10] == "true")
                {
                    tameTable.procretion = true;
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
