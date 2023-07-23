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
        [Serializable]
        public class TameTable : ICloneable //All the info that can be changed for a creature
        {
            public bool commandable { get; set; } = true;
            public float tamingTime { get; set; } = 1800f;
            public float fedDuration { get; set; } = 600f;
            public float consumeRange { get; set; } = 2f;
            public float consumeSearchInterval { get; set; } = 10f;
            public float consumeHeal { get; set; } = 30f;
            public float consumeSearchRange { get; set; } = 10f;
            public string consumeItems { get; set; } = "RawMeat";
            public bool changeFaction { get; set; } = false;
            public bool procretion { get; set; } = true;
            public bool procretionOverwrite { get; set; } = false;
            public int maxCreatures { get; set; } = 5;
            public float pregnancyChance { get; set; } = 0.33f;
            public float pregnancyDuration { get; set; } = 60f;
            public float growTime { get; set; } = 3000f;

            //custom features
            public bool canMateWithSelf { get; set; } = true;
            public string specificOffspringString { get; set; } = "";
            public List<specificMates> ListofRandomOffspring { get; set; } = new List<specificMates>();
            public float size { get; set; } = 1f;
            public bool offspringOnly { get; set; } = false;
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
            //DBG.blogDebug("in consume");
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
            if (!__instance.m_character.IsTamed())
            {
                string prefname = __instance.name.Replace("(Clone)", ""); ;
                //DBG.blogDebug("prefname=" + prefname);
                //get offspring
                if (cfgList.TryGetValue(prefname, out TameTable cfgfile))
                {
                    __instance.m_fedDuration = cfgfile.fedDuration;
                    //DBG.blogDebug("new fed duration is " + cfgfile.fedDuration);
                }

                    
            }
            DBG.blogDebug("item.name=" + item.name.Replace("(Clone)", ""));
            if (hidden_foodNames.Contains(item.name.Replace("(Clone)", "")))
            {
                if(item.name.Replace("(Clone)", "") == t1foodPrefabName)
                {
                    __instance.DecreaseRemainingTime(45f);  
                    DBG.blogDebug("Consumed T1Food");
                    
                }
                else if(item.name.Replace("(Clone)", "") == t2foodPrefabName)
                {
                    __instance.DecreaseRemainingTime(90f);
                    DBG.blogDebug("Consumed T2Food");
                }
                GameObject go = ZNetScene.instance.GetPrefab("fx_guardstone_permitted_add");
                UnityEngine.Object.Instantiate(go, __instance.transform.position, __instance.transform.rotation);
            }

        }
        //}

       

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
                        if (TameListCfg.Init() == false) { DBG.blogWarning("Failed second attempt at setting the tamelist, all tames may not work"); }
                    }
                    if (!PetManager.isInit)
                    {
                        PetManager.Init();
                    }
                }
            }
        }


        public static int Safe_GetNrOfInstances(GameObject prefab, Vector3 center, float maxRange, bool isError, bool eventCreaturesOnly = false, bool procreationOnly = false)
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
                try { DBG.blogDebug("prefab.GetComponent<BaseAI>() != null: = " + prefab.GetComponent<BaseAI>() != null); } catch { }



            }
            //DBG.blogDebug("GetNum Prefab is null= " + !(prefab ?? false));
            //isError = true;
            string text = prefab.name + "(Clone)";
            if (prefab.GetComponent<BaseAI>() != null)
            {
                //try { DBG.blogDebug("prefab.name: = " + prefab.name); } catch { }
                //DBG.blogDebug("Prefab is null= " + !(prefab ?? false));
                List<BaseAI> allInstances = BaseAI.Instances;
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
                        try { DBG.blogDebug("MonsterAI:= " + monsterAI); } catch { }
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


        [HarmonyPostfix]
        [HarmonyPatch(typeof(ZNetScene), "Shutdown")]
        //private static class Postfix_ZNetScene_ShutDown
        //{
        private static void Postfix()
        {
            DBG.blogInfo("Clearing TameLists");
            //prefabManager.Clear();
            //cfgList.Clear();
            cfgListFailed.Clear();
            cfgPostList.Clear();
            CompatMatesList.Clear();
            RecruitList.Clear();
            rawMatesList = new List<string> { };
            rawTradesList = new List<string> { };
            PostMakeList.Clear();
            PostLoadServerConfig = false;
            PreSetMinis = true;
            ReceivedServerConfig = false;


        }

        //}
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Tameable), "Tame")]
        //private static class Patch_Tameable_Tame
        //{
        private static void Postfix(Tameable __instance) //changes the faction based on config
        {
            string key = __instance.name.Replace("(Clone)", "");
            if (cfgList.ContainsKey(key) )
            {
                //DBG.blogDebug("Found Key");
                __instance.m_fedDuration = __instance.m_fedDuration * TamedFedMultiplier.Value;
                if (cfgList[key].changeFaction)
                {
                    Humanoid humanoid = __instance.GetComponent<Humanoid>();
                    humanoid.m_faction = Character.Faction.Players;
                    humanoid.m_group = "";
                    //DBG.blogDebug("Changed Faction");
                    //DBG.blogDebug(cfgList[key].changeFaction);
                }
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
            BetterTameHover.max_interact_default = Player.m_localPlayer.m_maxInteractDistance;
            DBG.blogDebug("interact distance= " + BetterTameHover.max_interact_default);
            string playerName = Player.m_localPlayer.GetPlayerName();
            if (ThxList.Contains(playerName.ToLower()))
            {
                SetPlayerSpwanEffect();
            }
          
        }
       



       

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

        public static ConfigEntry<bool> UseSimple;

        public static ManualLogSource logger;

        public static Dictionary<string, TameTable> cfgList = new Dictionary<string, TameTable>();
        public static Dictionary<string, TameTable> cfgPostList = new Dictionary<string, TameTable>();
        public static Dictionary<string, List<string>> CompatMatesList = new Dictionary<string, List<string>>();
        public static Dictionary<string, List<TradeAmount>> RecruitList = new Dictionary<string, List<TradeAmount>>();
        public static List<string> rawMatesList = new List<string> { };
        public static List<string> rawTradesList = new List<string> { };

        public static List<string> PostMakeList = new List<string>();

        public static TameTable CfgTable;
        public static bool ReceivedServerConfig = false;
        public static bool PostLoadServerConfig = false;

        public static List<string> ThxList = new List<string> { "deftesthawk", "buzz", "lordbugx", "hawksword" };

        public static EffectList.EffectData firework = new EffectList.EffectData();

        public static bool loaded = false;

        public static bool is_Basic = true;

        public static bool listloaded = false;
        public static bool PreSetMinis = true;

        public static String tamingtoolPrefabName = "el_TamingTool";
        public static String t1foodPrefabName = "el_T1Food";
        public static String t2foodPrefabName = "el_T2Food";

        public static List<string> hidden_foodNames = new List<string> { t1foodPrefabName, t2foodPrefabName };

        public static Dictionary<string, TameTable> cfgListFailed = new Dictionary<string, TameTable>();

        public static GameObject Root;

        public static PrefabManager prefabManager;


        public static bool TameUpdate = false;

        public static string cfgPath;

        public static PetManager petManager;

        public static bool UseCLLC = false;



        public static CustomRPC jot_TamelistRPC;


        public static readonly WaitForSeconds OneSecondWait = new WaitForSeconds(1f);

        public static readonly WaitForSeconds HalfSecondWait = new WaitForSeconds(0.5f);


        private void Awake()
        {
            logger = base.Logger;
            nexusID = base.Config.Bind("Nexus", "NexusID", 1571, "Nexus mod ID for updates");
            HatchingTime = base.Config.Bind("2DragonEgg", "hatching time", 300, new ConfigDescription("how long will egg become a drake", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            //HatchingTimeSync = base.Config.Bind("2DragonEgg", "hatching time Server", 300,new ConfigDescription("how long will egg become a drake", null,new ConfigurationManagerAttributes { IsAdminOnly = true }));
            HatchingEgg = base.Config.Bind("2DragonEgg", "enable egg hatching", defaultValue: true, new ConfigDescription("this alse enable tamed drake spawn eggs", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            //cfg = base.Config.Bind("1General", "Settings", ""
            //    , "OBSOLETE, CHANGING WILL NOT DO ANYTHING: name,commandable,tamingTime,fedDuration,consumeRange,consumeSearchInterval,consumeHeal,consumeSearchRange,consumeItem:consumeItem,changeFaction,procretion,maxCreatures,pregnancyChance,pregnancyDuration,growTime,;next one;...;last one");
            useTamingTool = base.Config.Bind("1General", "useTamingTool", defaultValue: true, "Use a taming tool to have an overlay when hovering over a creature");
            UseSimple = base.Config.Bind("1General", "useSimpleFeatures", defaultValue: false,
                new ConfigDescription("Choose whether to reduce the features to only allow for the taming of extra creatures although no complex procreation behavior", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            debugout = base.Config.Bind("1General", "Debug Output", defaultValue: false, "Determines if debug is output to bepinex log");
            UseCLLC_Config = base.Config.Bind("2DragonEgg", "Use CLLC Level", defaultValue: true,
                new ConfigDescription("Determines if you want to attempt to use CLLC to determine the level of your hatchling", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
            LvlProb = base.Config.Bind("2DragonEgg", "Hatchling Level Probabilities", "75,25,5",
                new ConfigDescription("List of the probabilities for a hatchling to spawn at a specific level ex: 75,25,5 will have a 75% chance to have 0 stars, 25% to have 1, and 5% to have two", null, new ConfigurationManagerAttributes { IsAdminOnly = true }));
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

            is_Basic = UseSimple.Value;
            listloaded = TameListCfg.Init();
            if (!listloaded)
            {
                DBG.blogWarning("No Tamelist Config found, using default config for creature tames");
                DBG.blogWarning("Tamelist is required as of version 1.1.5");
                DBG.blogWarning("Attempting to create Tamelist from config file");
                cfgPath = base.Config.ConfigFilePath;
                DBG.blogDebug("cfgPath="+ cfgPath);
                TameListCfg.create_TamelistCFG();
                loaded = TameListCfg.Init();
                //loaded = initCfg();
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
            PerformPatches();
            // Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

            //scanMethods();
            //applyPatches();


            
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

            jot_TamelistRPC = NetworkManager.Instance.AddRPC("jot_tamelistRPC", null, jot_RPCClientReceive);
            SynchronizationManager.Instance.AddInitialSynchronization(jot_TamelistRPC, SendInitialConfig);
        }

        public int requestcount = 0;

        private ZPackage SendInitialConfig()
        {
            DBG.blogDebug("Sent Client RPC");
            RPC.CfgPackage cfgPackage = new RPC.CfgPackage();
            return cfgPackage.PackTamelist();
        }



        private IEnumerator jot_RPCClientReceive(long sender, ZPackage package)
        {
            DBG.blogDebug("Client received RPC");
            Jotunn.Logger.LogMessage($"Received blob, processing!");
            yield return null;
            RPC.CfgPackage.Unpack(package);
            PetManager.UpdatesFromServer();
            DBG.blogDebug("!UseSimple.Value="+ !UseSimple.Value + ", is_Basic="+ is_Basic);
            if (!UseSimple.Value && is_Basic)
            {
                DBG.blogDebug("Server has complex procreation, patching Genetics and Trading");
                PerformNonBasicPatches();
            }

        }



        public static bool CheckHuman(GameObject go)
        {
            try{return RRRCoreTameable.RRRCoreTameable.CheckHuman(go);}
            catch {return false;}
        }

        public void PerformNonBasicPatches()
        {
            var harmony = new Harmony("meldurson.valheim.AllTameable");
            harmony.PatchAll(typeof(global::AllTameable.Genetics.Genetics));
            DBG.blogDebug("Patched Genetics");
            harmony.PatchAll(typeof(global::AllTameable.Trading.Trading));
            DBG.blogDebug("Patched Trading");
            if (UseCLLC)
            {
                harmony.PatchAll(typeof(global::AllTameable.CLLC.CLLCPatches));
                DBG.blogDebug("Patched CLLCPatches");
                harmony.PatchAll(typeof(global::AllTameable.CLLC.CLLCPatches.InterceptProcreation));
                DBG.blogDebug("Patched InterceptProcreation");
                harmony.PatchAll(typeof(global::AllTameable.CLLC.CLLCPatches.InterceptGrowup));
                DBG.blogDebug("Patched InterceptProcreation");
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
                harmony.PatchAll(typeof(global::AllTameable.RRRCoreTameable.RRRCoreTameable));
            }

            

            DBG.blogInfo("Patching Select");
            harmony.PatchAll(typeof(global::AllTameable.Plugin));
            DBG.blogDebug("Patched Plugin");
            if (!is_Basic)
            {
                PerformNonBasicPatches();
            }
            else
            {
                DBG.blogDebug("Use Simple enabled so did not Patch Genetics,Trading, or CLLC if enabled");
            }

            harmony.PatchAll(typeof(global::AllTameable.BetterTameHover));
            DBG.blogDebug("Patched Bettertamehover");
            harmony.PatchAll(typeof(global::AllTameable.AllTame_AnimalAI));
            DBG.blogDebug("Patched AnimalAi");
            
            //harmony.PatchAll(typeof(global::AllTameable.RPC.RPC));
            //DBG.blogDebug("Patched RPC");

            /*
            harmony.PatchAll(typeof(global::AllTameable.Plugin.Reverse_SpawnSystem));
            DBG.blogDebug("Patched ReverseSpawn");
            harmony.PatchAll(typeof(global::AllTameable.Plugin.Prefix_GetNrOfInstances));
            DBG.blogDebug("Patched Prefix_GetNrOfInstances");
            */
            
            

            var myOriginalMethods = harmony.GetPatchedMethods();
            foreach (var method in myOriginalMethods)
            {
                DBG.blogDebug(method.ReflectedType + ":" + method.Name + " is patched");
            }


        }

        private static void SetPlayerSpwanEffect()
        {
            if (!Player.m_localPlayer.m_spawnEffects.m_effectPrefabs.Contains(firework))
            {
                Array.Resize(ref Player.m_localPlayer.m_spawnEffects.m_effectPrefabs, 1);
                Player.m_localPlayer.m_spawnEffects.m_effectPrefabs[0] = firework;
            }
        }

        /*
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
        */
       
        /*
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
        */



    }
}
