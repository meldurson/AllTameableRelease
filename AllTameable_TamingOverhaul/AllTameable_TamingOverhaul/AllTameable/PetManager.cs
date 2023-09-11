using AllTameable;
using AllTameable.RPC;
using Jotunn.Managers;
using System.Collections.Generic;
//using System;
using System.Reflection;
using UnityEngine;
using System.Globalization;

namespace AllTameable
{
    public class PetManager : MonoBehaviour
    {
        private static ZNetScene zns;

        private static Tameable wtame;

        public static GameObject Root;

        public static bool isInit;
        public static bool isInit2;
        public static bool fixedDeerAI;
        public static GameObject heartEffectGO;

        public static GameObject DragonEgg;
        public static GameObject DrakeEggPrefab;
        public static GameObject ChickenEggPrefab;

        private void Awake()
        {
            Root = new GameObject("MiniOnes");
            Root.transform.SetParent(Plugin.prefabManager.Root.transform);
        }

        public static void Init2nd(GameObject init_go) //attempts to init tames that failed before spawning, common with creatures added with mods
        {
            isInit2 = true;
            zns = ZNetScene.instance;
            Plugin.PreSetMinis = false;
            Dictionary<string, Plugin.TameTable> cfgList = Plugin.cfgListFailed;
            int succ = 0;
            int tot = 0;
            foreach (KeyValuePair<string, Plugin.TameTable> item in cfgList)
            {
                tot += 1;
                string key = item.Key;
                if (zns.GetPrefab(key) != null)
                {
                    AddTame(zns.GetPrefab(key), item.Value);
                    if (Utils.GetPrefabName(init_go) == key)
                    {
                        AddTame(init_go, item.Value);
                        DBG.blogDebug("Post Added First Spawn: " + init_go.name);
                    }
                    //Plugin.cfgListFailed.Remove(key);
                    succ += 1;
                    DBG.blogDebug("Post Added " + key);
                }

            }
            if (tot > 0)
            {
                if (succ == tot)
                {
                    DBG.blogDebug("Succesfully Post added " + succ + "/" + tot);
                }
                else
                {
                    DBG.blogWarning("Failed to Post added " + (tot - succ) + "/" + tot);
                }
            }
            List<string> LateSetMini = new List<string>(Plugin.PostMakeList);
            //DBG.blogDebug(string.Join(",", LateSetMini));

            foreach (string prefname in LateSetMini)
            {
                GameObject go = zns.GetPrefab(prefname);
                Procreation proc = go.GetComponent<Procreation>();
                proc.m_offspring = SpawnMini(go);

            }


        }

        public static void UpdatesFromServer() //Reinitializes after receiving config from server
        {
            isInit = true;

            zns = ZNetScene.instance;
            //Plugin.PreSetMinis = false;
            wtame = zns.GetPrefab("Wolf").GetComponent<Tameable>();
            if (Plugin.HatchingEgg.Value)
            {
                DBG.blogDebug("init Dragonegg");
                AlterEggs();
                //InitEgg(zns.GetPrefab("Hatchling"));
                //AddHatchDragonEgg();
            }
            TameListCfg.UnpackAndOverwriteMates();
            TameListCfg.UnpackAndOverwriteTrades();
            createHeartEffect();
            Dictionary<string, Plugin.TameTable> cfgList = Plugin.cfgList;
            foreach (KeyValuePair<string, Plugin.TameTable> item in cfgList)
            {
                string key = item.Key;
                //DBG.blogDebug("key= " + key);
                if (zns.GetPrefab(key) != null)
                {
                    AddTame(zns.GetPrefab(key), item.Value);
                    //Plugin.cfgListFailed.Remove(key);
                }
                else
                {
                    Plugin.PostLoadServerConfig = true;
                    DBG.blogDebug("failed to find prefab:" + key + ", will attempt to load later");
                    Plugin.cfgListFailed.Add(key, item.Value);
                }

            }
            //cfgList.TryGetValue(Utils.GetPrefabName(init_go.gameObject), out Plugin.TameTable cfgvalue);
            //AddTame(init_go, cfgvalue);
            DBG.blogInfo("Succesfully Loaded Config from server");
        }


        public static void FixAnimalAI(GameObject go) // Tameable is only available with MonsterAI, Creates new AI from MonsterAI and then Copies AI from Animal AI
        {
            if (go.GetComponent<AllTame_AnimalAI>() == null)
            {
                DBG.blogDebug("Fixing AI for " + go.name);
                zns = ZNetScene.instance;
                wtame = zns.GetPrefab("Wolf").GetComponent<Tameable>();
                //GameObject go = zns.GetPrefab("Deer");
                MonsterAI mAI = zns.GetPrefab("Boar").GetComponent<MonsterAI>();
                //Humanoid hum_boar = zns.GetPrefab("Boar").GetComponent<Humanoid>();

                if (!go.TryGetComponent<Humanoid>(out var humanoid))
                {
                    //component2 = deergo.AddComponent<Humanoid>(zns.GetPrefab("Boar").GetComponent<Humanoid>());
                    humanoid = go.AddComponent<Humanoid>();
                    humanoid.m_eye = go.transform.GetChild(0);
                    //DBG.blogDebug("Eye is " + humanoid.m_eye.name);
                    //component2.enabled = false;
                }

                AnimalAI anAI = go.GetComponent<AnimalAI>();
                Character anChar = go.GetComponent<Character>();
                //AllTame_AnimalAI newmAI = mAI as AllTame_AnimalAI;
                AllTame_AnimalAI allTanAI = go.AddComponent<AllTame_AnimalAI>();
                //DBG.blogDebug("Copying MonsterAI into AnimalAI");
                foreach (FieldInfo prop in typeof(MonsterAI).GetFields())
                {
                    try
                    {
                        if (prop.GetValue(allTanAI) != null & prop.GetValue(mAI) != prop.GetValue(allTanAI))
                        {
                            prop.SetValue(allTanAI, prop.GetValue(mAI));
                            //DBG.blogDebug("From MonsterAI: " + prop.Name + " set to " + prop.GetValue(allTanAI));

                        }
                    }
                    catch
                    {
                        //DBG.blogDebug("AllTame_AnimalAI does not have " + prop.Name);
                    }
                }
                foreach (FieldInfo prop in typeof(AnimalAI).GetFields())
                {
                    try
                    {
                        if (prop.GetValue(allTanAI) != null & prop.GetValue(anAI) != prop.GetValue(allTanAI))
                        {
                            prop.SetValue(allTanAI, prop.GetValue(anAI));
                            //DBG.blogDebug("From AnimalAI: " + prop.Name + " set to " + prop.GetValue(allTanAI));

                        }
                    }
                    catch
                    {
                        //DBG.blogDebug("AllTame_AnimalAI does not have " + prop.Name);
                    }
                }
                foreach (FieldInfo prop in typeof(Character).GetFields())
                {
                    try
                    {
                        if (prop.GetValue(humanoid) != null & prop.GetValue(anChar) != prop.GetValue(humanoid))
                        {
                            prop.SetValue(humanoid, prop.GetValue(anChar));
                            //DBG.blogDebug("From Humanoid: " + prop.Name + " set to " + prop.GetValue(humanoid));

                        }
                    }
                    catch
                    {
                        //DBG.blogDebug("Humanoid does not have " + prop.Name);
                    }
                }
                allTanAI.m_character = humanoid;
                Object.DestroyImmediate(go.GetComponent<AnimalAI>());
                Object.DestroyImmediate(go.GetComponent<Character>());
            }


        }

        public static void FixDeerAI()
        {
            if (fixedDeerAI == false)
            {
                zns = ZNetScene.instance;
                wtame = zns.GetPrefab("Wolf").GetComponent<Tameable>();
                GameObject deergo = zns.GetPrefab("Deer");
                MonsterAI mAI = zns.GetPrefab("Boar").GetComponent<MonsterAI>();
                //Humanoid hum_boar = zns.GetPrefab("Boar").GetComponent<Humanoid>();

                if (!deergo.TryGetComponent<Humanoid>(out var humanoid))
                {
                    //component2 = deergo.AddComponent<Humanoid>(zns.GetPrefab("Boar").GetComponent<Humanoid>());
                    humanoid = deergo.AddComponent<Humanoid>();
                    humanoid.m_eye = deergo.transform.GetChild(0);
                    DBG.blogDebug("Eye is " + humanoid.m_eye.name);
                    //component2.enabled = false;
                }

                AnimalAI anAI = deergo.GetComponent<AnimalAI>();
                Character anChar = deergo.GetComponent<Character>();
                //AllTame_AnimalAI newmAI = mAI as AllTame_AnimalAI;
                AllTame_AnimalAI allTanAI = deergo.AddComponent<AllTame_AnimalAI>();
                foreach (FieldInfo prop in typeof(MonsterAI).GetFields())
                {
                    try
                    {
                        if (prop.GetValue(allTanAI) != null & prop.GetValue(mAI) != prop.GetValue(allTanAI))
                        {
                            prop.SetValue(allTanAI, prop.GetValue(mAI));
                            DBG.blogDebug("From MonsterAI: " + prop.Name + " set to " + prop.GetValue(allTanAI));

                        }
                    }
                    catch
                    {
                        DBG.blogDebug("AllTame_AnimalAI does not have " + prop.Name);
                    }
                }
                foreach (FieldInfo prop in typeof(AnimalAI).GetFields())
                {
                    try
                    {
                        if (prop.GetValue(allTanAI) != null & prop.GetValue(anAI) != prop.GetValue(allTanAI))
                        {
                            prop.SetValue(allTanAI, prop.GetValue(anAI));
                            DBG.blogDebug("From AnimalAI: " + prop.Name + " set to " + prop.GetValue(allTanAI));

                        }
                    }
                    catch
                    {
                        DBG.blogDebug("AllTame_AnimalAI does not have " + prop.Name);
                    }
                }
                foreach (FieldInfo prop in typeof(Character).GetFields())
                {
                    try
                    {
                        if (prop.GetValue(humanoid) != null & prop.GetValue(anChar) != prop.GetValue(humanoid))
                        {
                            prop.SetValue(humanoid, prop.GetValue(anChar));
                            DBG.blogDebug("From Humanoid: " + prop.Name + " set to " + prop.GetValue(humanoid));

                        }
                    }
                    catch
                    {
                        DBG.blogDebug("Humanoid does not have " + prop.Name);
                    }
                }
                allTanAI.m_character = humanoid;
                Object.DestroyImmediate(deergo.GetComponent<AnimalAI>());
                Object.DestroyImmediate(deergo.GetComponent<Character>());


                fixedDeerAI = true;
            }


        }

        public static void createHeartEffect()
        {
            DBG.blogDebug("Making HeartEffect");
            heartEffectGO = Object.Instantiate(wtame.m_petEffect.m_effectPrefabs[0].m_prefab, Root.transform);
            DBG.blogDebug("removing child");
            heartEffectGO.transform.GetChild(1).parent = null;
            DBG.blogDebug("Creating new effect");
            EffectList.EffectData effectData2 = new EffectList.EffectData();
            DBG.blogDebug("setting prefab to go");
            effectData2.m_prefab = heartEffectGO;
            DBG.blogDebug("Created HeartEffect");
        }


        public static void Init()
        {
            isInit = true;
            zns = ZNetScene.instance;
            wtame = zns.GetPrefab("Wolf").GetComponent<Tameable>();
            if (Plugin.HatchingEgg.Value)
            {
                AlterEggs();
                //InitEgg(zns.GetPrefab("Hatchling"));
                //AddHatchDragonEgg();
            }
            createHeartEffect();

            foreach (KeyValuePair<string, List<TradeAmount>> item in Plugin.RecruitList)
            {
                string list_items = "";
                foreach(TradeAmount td_amt in item.Value)
                {
                    list_items += td_amt.tradeItem + "=" + td_amt.tradeAmt+ ";";
                }
                DBG.blogDebug("Trades: "+item.Key +": "+ list_items);
            }


            //FixDeerAI();
            Dictionary<string, Plugin.TameTable> cfgList = Plugin.cfgList;
            foreach (KeyValuePair<string, Plugin.TameTable> item in cfgList)
            {

                string key = item.Key;
                if (zns.GetPrefab(key) == null)
                {
                    DBG.blogWarning("Cant find Prefab Check your name : " + key);
                    //ConfigManager configManager = Plugin.configManager;
                    //configManager.debugInfo = configManager.debugInfo + "  Cant find Prefab Check your name : " + key;
                    Plugin.cfgListFailed.Add(key, item.Value);
                }
                else
                {
                    AddTame(zns.GetPrefab(key), item.Value);
                }
            }
        }



        public static void Init(GameObject init_go)
        {
            Init();
            Dictionary<string, Plugin.TameTable> cfgList = Plugin.cfgList;
            cfgList.TryGetValue(Utils.GetPrefabName(init_go.gameObject), out Plugin.TameTable cfgvalue);
            AddTame(init_go, cfgvalue);
        }


        private static void AddTame(GameObject go, Plugin.TameTable tb)
        {
            //DBG.blogDebug("in AddTame");
            try
            {

                if (go.GetComponent<MonsterAI>() == null)
                {
                    if (go.GetComponent<AnimalAI>() != null)
                    {
                        FixAnimalAI(go);
                    }
                    else
                    {
                        DBG.blogWarning(go.name + " can't be added,Remove it in your cfg");
                        //ConfigManager configManager = Plugin.configManager;
                        //configManager.debugInfo = configManager.debugInfo + go.name + " can't be added,Remove it in your cfg   ";
                        return;
                    }

                }
                if (tb.tamingTime == -1)
                {
                    DBG.blogDebug("Removing Tameable from " + go.name);
                    if (go.GetComponent<Tameable>() != null)
                    {
                        Object.DestroyImmediate(go.GetComponent<Tameable>());
                    }
                    if (go.GetComponent<Procreation>() != null)
                    {
                        Object.DestroyImmediate(go.GetComponent<Procreation>());
                    }
                    return;

                }
                if (tb.tamingTime == -2)
                {
                    DBG.blogDebug("Setting Only Tradeable for  " + go.name);
                    if (go.GetComponent<Procreation>() != null)
                    {
                        Object.DestroyImmediate(go.GetComponent<Procreation>());
                    }
                }
                if (!go.TryGetComponent<Tameable>(out var component))
                {
                    component = go.AddComponent<Tameable>();
                }

                MonsterAI component2 = go.GetComponent<MonsterAI>();

                if(tb.group != "")
                {
                    if (go.GetComponent<Humanoid>() != null)
                    {
                        go.GetComponent<Humanoid>().m_group = tb.group;
                    }
                    else if (go.GetComponent<Character>() != null)
                    {
                        go.GetComponent<Character>().m_group = tb.group;
                    }
                }
                else if (tb.changeFaction == false)
                {
                    if (go.GetComponent<Humanoid>() != null)
                    {
                        go.GetComponent<Humanoid>().m_group = go.name;
                    }
                    else if (go.GetComponent<Character>() != null)
                    {
                        go.GetComponent<Character>().m_group = go.name;
                    }
                }


                if (Plugin.RecruitList.TryGetValue(go.name, out List<TradeAmount> tradelist))
                {
                    DBG.blogDebug("Found RecruitList");
                    if (!go.TryGetComponent<AllTame_Interactable>(out var AT_Int))
                    {
                        DBG.blogDebug("Adding AllTame_Interactable");
                        AT_Int = go.AddComponent<AllTame_Interactable>();
                        DBG.blogDebug("Added AllTame_Interactable");

                    }

                    int i = 0;
                    string tradelist_str = go.name + " trades:";
                    foreach (TradeAmount td_amt in tradelist)
                    {
                        GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(td_amt.tradeItem);
                        
                        if (itemPrefab == null)
                        {
                            DBG.blogWarning("Wrong item name " + td_amt.tradeItem);
                        }
                        else
                        {
                            AT_Int.tradeItems[i] = itemPrefab.name;
                            AT_Int.tradeAmounts[i] = td_amt.tradeAmt;
                            tradelist_str += td_amt.tradeItem + ", " + td_amt.tradeAmt + "; ";
                        }
                        i++;
                    }
                    DBG.blogDebug(tradelist_str);
                    System.Array.Resize(ref AT_Int.tradeItems, tradelist.Count);
                    System.Array.Resize(ref AT_Int.tradeAmounts, tradelist.Count);
                }
                

                //Change pet (follow) sound if dverger
                if (go.name.ToLower().IndexOf("dverger") > -1)
                {
                    //DBG.blogWarning("changing dverger effect:" + go.name);
                    component.m_petEffect = new EffectList();
                    EffectList.EffectData[] ea = { new EffectList.EffectData() };
                    component.m_petEffect.m_effectPrefabs = ea;
                    component.m_petEffect.m_effectPrefabs[0].m_prefab = ZNetScene.instance.GetPrefab("sfx_dverger_vo_alerted");
                }
                else
                {
                    int i = 0;
                    string effectlist = "Effect List: ";
                    EffectList.EffectData[] NewEffectList = new EffectList.EffectData[10];
                    EffectList.EffectData[] idleEffects = component2.m_idleSound.m_effectPrefabs;
                    foreach (EffectList.EffectData effectData in idleEffects)
                    {
                        try
                        {
                            effectlist += i + ":" + effectData.m_prefab.name;
                            NewEffectList[i] = effectData;
                        }
                        catch{}
                        i++;
                    }
                    //DBG.blogDebug(effectlist);
                    int numEffects = i;
                    numEffects = idleEffects.Length;
                    if (component2.m_idleSound.m_effectPrefabs.Length != 0)
                    {
                        EffectList.EffectData hearEffect = new EffectList.EffectData();
                        hearEffect.m_prefab = heartEffectGO;
                        NewEffectList[numEffects] = hearEffect;
                        System.Array.Resize(ref NewEffectList, numEffects + 1);
                        component.m_petEffect.m_effectPrefabs = NewEffectList;
                    }
                    else
                    {
                        component.m_petEffect = wtame.m_petEffect;
                    }
                }

                



                //component.m_sootheEffect = component.m_petEffect;
                component.m_sootheEffect = wtame.m_sootheEffect;
                component.m_tamedEffect = wtame.m_tamedEffect;
                component.m_commandable = tb.commandable;
                if (tb.commandable)
                {
                    DBG.blogDebug(go.name + " is commandable");
                }
                else
                {
                    DBG.blogDebug(go.name + " is not commandable");
                }

                component.m_tamingTime = tb.tamingTime;
                component.m_fedDuration = tb.fedDuration *Plugin.TamedFedMultiplier.Value;
                component2.m_consumeRange = tb.consumeRange;
                component2.m_consumeSearchInterval = tb.consumeSearchInterval;
                //component2.m_consumeHeal = tb.consumeHeal;
                component2.m_consumeSearchRange = tb.consumeSearchRange;
                List<string> list = new List<string>();
                tb.consumeItems += ":" + string.Join(":", Plugin.hidden_foodNames);
                DBG.blogDebug("tb.consumeItems="+ tb.consumeItems);
                string[] array = tb.consumeItems.Split(':');
                string[] array2 = array;
                if (component2.m_consumeItems != null)
                {
                    component2.m_consumeItems.Clear();
                }
                component2.m_consumeItems = new List<ItemDrop> { };

                foreach (string item in array2)
                {
                    list.Add(item);
                }
                if (!string.IsNullOrEmpty(list[0]) | list.Count > 1)
                {
                    foreach (string item2 in list)
                    {
                        GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(item2);
                        if (itemPrefab == null)
                        {
                            DBG.blogWarning("Wrong food name :" + item2);
                            //ConfigManager configManager2 = Plugin.configManager;
                            //configManager2.debugInfo = configManager2.debugInfo + "   Wrong food name :" + item2;
                        }
                        else
                        {
                            component2.m_consumeItems.Add(itemPrefab.GetComponent<ItemDrop>());
                        }
                    }
                }
                else { DBG.blogInfo("Cannot be tamed by food"); }


                //if (Plugin.CheckHuman(go))
                //{
                //    DBG.blogDebug("isHuman: Procreation=" + tb.procretion);
                //}

                if (tb.procretion)
                {
                    //DBG.blogDebug("has procreation");
                    bool flag = true;
                    if (!go.TryGetComponent<Procreation>(out var component3))
                    {
                        component3 = go.AddComponent<Procreation>();
                        flag = false;
                    }
                    component3.m_maxCreatures = tb.maxCreatures;
                    component3.m_pregnancyChance = tb.pregnancyChance;
                    if (component3.m_pregnancyChance > 1) { component3.m_pregnancyChance *= 0.01f; }
                    if (component3.m_pregnancyChance > 1) { component3.m_pregnancyChance =0.66f; }
                    component3.m_pregnancyDuration = tb.pregnancyDuration;
                    if (component3.m_updateInterval < 20) { component3.m_updateInterval = 20; }
                    //component3.m_updateInterval = 6; //For Debuging
                    component3.m_partnerCheckRange = 4f * tb.size;
                    component3.m_totalCheckRange = 10f * tb.size;

                    if (tb.eggValue + "" != "")
                    {
                        bool drakeEgg = true;
                        float hatchtime = 1800;
                        string colorStr = "default";
                        float size = 1;
                        parseEggValue(tb.eggValue, ref drakeEgg, ref hatchtime, ref colorStr, ref size);
                        //DBG.blogDebug(drakeEgg + "," + hatchtime + "," + colorStr + "," + size);
                        component3.m_offspring = InitEgg(go,tb, hatchtime, drakeEgg, colorStr, size);

                    }
                    else if (flag && component3.m_offspring != null && !Plugin.CheckHuman(go) && !tb.procretionOverwrite)
                    {
                        Growup component4 = component3.m_offspring.GetComponent<Growup>();
                        if ((bool)component4)
                        {
                            component4.m_growTime = tb.growTime;
                        }
                        else
                        {
                            EggGrow eggGrow = component3.m_offspring.GetComponent<EggGrow>();
                            if ((bool)eggGrow)
                            {
                                Growup hatchedGrow = eggGrow.m_grownPrefab.GetComponent<Growup>();
                                if ((bool)hatchedGrow)
                                {
                                    hatchedGrow.m_growTime = tb.growTime;
                                    DBG.blogWarning(go.name + " has egg as offspring, modified hatched prefab growtime");
                                }
                            }
                            else
                            {
                                DBG.blogWarning("Failed to set grow time for " + go.name);
                            }
                            
                        }
                        DBG.blogDebug(go.name + " already has offspring, modified tameable");
                    }
                    else if (go.name == "Hatchling" && Plugin.HatchingEgg.Value)
                    {
                        component3.m_offspring = DrakeEggPrefab;
                    }
                    else 
                    {
                        //DBG.blogDebug("setting spawnmini");

                        component3.m_offspring = SpawnMini(go);
                        
                    }
                    //DBG.blogDebug("Pregnancy duration is :"+ component3.m_pregnancyDuration);

                    if (ZNet.instance.IsServer() & ZNet.instance.IsDedicated())
                    {
                        GameObject serverfab = ZNetScene.instance.GetPrefab(go.name);
                        string istamestr = " is not Tameable";
                        if (serverfab.GetComponent<Tameable>().enabled)
                        {
                            istamestr = " is Tameable";
                        }
                        DBG.blogDebug("******* " + serverfab.name + istamestr + " ********");
                        //DBG.blogDebug("istameable = " + serverfab.GetComponent<Tameable>().enabled);
                    }
                }
                else
                {
                    if (go.GetComponent<Procreation>() != null)
                    {
                        Object.DestroyImmediate(go.GetComponent<Procreation>());
                    }
                    DBG.blogDebug("Added ability to tame to " + go.name);
                }
                if (tb.offspringOnly)
                {
                    DBG.blogDebug("Removing Tameable from " + go.name +" due to only being from procreation");
                    if (go.GetComponent<Tameable>() != null)
                    {
                        Object.DestroyImmediate(go.GetComponent<Tameable>());
                    }
                    if (go.GetComponent<Procreation>() != null)
                    {
                        Object.DestroyImmediate(go.GetComponent<Procreation>());
                    }
                }

            }
            catch{DBG.blogWarning("Failed to add tame to prefab: " + go.name + ", Make sure config files are formatted correctly");}
        }

        public static GameObject SpawnMini(GameObject prefab, float growup_time = 600)
        {
            //DBG.blogDebug("inspawnmini");
            bool isClone = false;
            string text = prefab.name;
            //bool temp_prefabCreated = false;
            GameObject gameObject;
            bool isHuman = false;
            if (text.Contains("(Clone"))
            {
                text = Utils.GetPrefabName(prefab);
                isClone = true;
            }
            gameObject = Object.Instantiate(zns.GetPrefab(text), Root.transform);
            if (text.Contains("RRRN"))
            {
                //DBG.blogDebug("ishuman");
                isHuman = true;
                if (RRRCoreTameable.RRRCoreTameable.CheckHuman(prefab))
                {
                    //DBG.blogDebug("isHuman");
                    if (Plugin.PreSetMinis)
                    {
                        Plugin.PostMakeList.Add(text);
                        gameObject = RRRCoreTameable.RRRCoreTameable.HotFixHuman(gameObject);
                        //gameObject  = Object.Instantiate(zns.GetPrefab("RRR_NPC"), Root.transform);
                    }
                    else
                    {
                        //DBG.blogDebug("InPostSetMinis");
                        DBG.blogDebug("Set clone of prefab in mini for " + text);
                        Plugin.PostMakeList.Remove(text);
                    }

                }
            }
            string NamePrefix;
            if (!isHuman) { NamePrefix = "Mini"; }
            else { NamePrefix = "Child"; }

            gameObject.name = "Mini" + text;
            //DBG.blogDebug(gameObject.name);
            gameObject.transform.localScale *= 0.5f;
            if (gameObject.GetComponent<Humanoid>() != null)
            {
                gameObject.GetComponent<Humanoid>().m_name = NamePrefix + " " + gameObject.GetComponent<Humanoid>().m_name;
            }
            else
            {
                gameObject.GetComponent<Character>().m_name = NamePrefix + " " + gameObject.GetComponent<Character>().m_name;
            }
            if (gameObject.GetComponent<MonsterAI>() != null)
            {
                Object.DestroyImmediate(gameObject.GetComponent<MonsterAI>());
            }
            if (gameObject.GetComponent<VisEquipment>() != null & !isHuman)
            {
                Object.DestroyImmediate(gameObject.GetComponent<VisEquipment>());
            }
            if (gameObject.GetComponent<CharacterDrop>() != null)
            {
                Object.DestroyImmediate(gameObject.GetComponent<CharacterDrop>());
            }
            if (gameObject.GetComponent<Tameable>() != null)
            {
                Object.DestroyImmediate(gameObject.GetComponent<Tameable>());
            }
            if (gameObject.GetComponent<Procreation>() != null)
            {
                Object.DestroyImmediate(gameObject.GetComponent<Procreation>());
            }
            MonsterAI component = prefab.GetComponent<MonsterAI>();
            AnimalAI comp = gameObject.AddComponent<AnimalAI>();
            comp.CopyBroComponet<AnimalAI, MonsterAI>(component);
            Growup growup = gameObject.AddComponent<Growup>();
            growup.m_grownPrefab = zns.GetPrefab(text);
            if (Plugin.cfgList.ContainsKey(text))
            {
                growup.m_growTime = Plugin.cfgList[text].growTime;
            }
            else
            {
                growup.m_growTime = growup_time;
            }
            

            if (!Plugin.PreSetMinis)
            {
                string name = gameObject.name;
                int hash = name.GetStableHashCode();

                if (zns.m_namedPrefabs.ContainsKey(hash))
                {
                    zns.m_namedPrefabs.Remove(hash);
                }
                Jotunn.Managers.PrefabManager.Instance.RegisterToZNetScene(gameObject);
            }
            else if (!isClone)
            {
                Jotunn.Managers.PrefabManager.Instance.RegisterToZNetScene(gameObject);

            }
            if (ZNetScene.instance.GetPrefab(gameObject.name) != null)
            {
                DBG.blogDebug("Successfully added Tame and Procreation to " + prefab.name);
            }

            return gameObject;
        }

        public void Clear()
        {
            isInit = false;
            isInit2 = false;
        }
        private static void AlterEggs()
        {
            if (!(bool)ChickenEggPrefab)
            {
                ChickenEggPrefab = ZNetScene.instance.GetPrefab("ChickenEgg");
                ChickenEggPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_maxQuality = 999;
            }
            if (!(bool)DrakeEggPrefab)
            {
                DrakeEggPrefab = ZNetScene.instance.GetPrefab("DragonEgg");
            }
            DBG.blogDebug("Set Chicken Egg");
            GameObject eggclone = Jotunn.Managers.PrefabManager.Instance.CreateClonedPrefab("chickenEggClone" + "_at", ChickenEggPrefab);
            GameObject notGrow = PrefabManager.CopyIntoParent(eggclone.GetComponent<Transform>().Find("Not Growing"), DrakeEggPrefab.transform).gameObject;
            EggGrow eggGrow = DrakeEggPrefab.AddComponent<EggGrow>(eggclone.GetComponent<EggGrow>());
            eggGrow.m_notGrowingObject = notGrow;
            eggGrow.m_growTime = Plugin.HatchingTime.Value;
            eggGrow.m_tamed = true;
            eggGrow.tag = "spawned";
            //eggGrow.m_requireNearbyFire = false;
            //eggGrow.m_requireUnderRoof = false;

            DBG.blogDebug("Moving Item Drop");
            GameObject go = DrakeEggPrefab;
            GameObject backup_go = Object.Instantiate(go, Root.transform);
            ItemDrop backup_IDrop = backup_go.GetComponent<ItemDrop>();
            ItemDrop IDrop = go.GetComponent<ItemDrop>();
            ItemDrop iDropnew = go.AddComponent<ItemDrop>(backup_IDrop);
            iDropnew.m_itemData.m_shared = IDrop.m_itemData.m_shared;
            iDropnew.m_itemData.m_shared.m_maxQuality = 999;
            iDropnew.m_autoPickup = false;
            iDropnew.m_itemData.m_shared.m_maxStackSize = 20;
            iDropnew.Save();
            Component.Destroy(IDrop);

            ItemStand EggStand = ZNetScene.instance.GetPrefab("dragoneggcup").GetComponent<ItemStand>();
            Plugin.changeIStandIDrop(EggStand, iDropnew);

            DBG.blogDebug("Added EggGrow");
            eggGrow.m_grownPrefab = SpawnMini(ZNetScene.instance.GetPrefab("Hatchling"));

        }
        
        public static void parseEggValue(string eggStr, ref bool DrakeEgg,ref float hatchtime, ref string colorstr,ref float size)
        {
            string tempstr = eggStr.Replace(")", "");
            //DBG.blogDebug("tempstr=" + tempstr);
            string[] splitstr = tempstr.Split('(');
            //DBG.blogDebug("splitstr[0]=" + splitstr[0]);
            if (splitstr[0].ToLower() == "chicken")
            {
                DrakeEgg = false;
            }
            if (splitstr.Length < 2)
            {
                return;
            }
            //DBG.blogDebug("splitstr[1]=" + splitstr[1]);
            string[] splitstr2 = splitstr[1].Split(':');
            try
            {
                hatchtime = float.Parse(splitstr2[0], CultureInfo.InvariantCulture.NumberFormat);
            }
            catch
            {
                DBG.blogWarning("value for egg hatchtime, make sure the config is written correctly");
            }
            if (splitstr2.Length < 2) { return; }
            colorstr = splitstr2[1];
            if (splitstr2.Length < 3) { return; }
            try
            {
                size = float.Parse(splitstr2[2], CultureInfo.InvariantCulture.NumberFormat);
            }
            catch
            {
                DBG.blogWarning("value for egg is invalid, make sure the config is written correctly");
            }

        }



        private static GameObject InitEgg(GameObject prefab, Plugin.TameTable tb, float hatchTime, bool DrakeEgg = true, string color = "default", float size = 1f)
        {
            DBG.blogDebug("Creating Egg");
            if (!(bool)ChickenEggPrefab)
            {
                ChickenEggPrefab = ZNetScene.instance.GetPrefab("ChickenEgg");
            }
            if (!(bool)DrakeEggPrefab)
            {
                DrakeEggPrefab = ZNetScene.instance.GetPrefab("DragonEgg");
            }
            //DBG.blogDebug("past init");
            GameObject gameObject;
            if (DrakeEgg)
            {
                gameObject = Object.Instantiate(DrakeEggPrefab, Root.transform);
            }
            else
            {
                gameObject = Object.Instantiate(ChickenEggPrefab, Root.transform);
            }


            gameObject.name = (prefab.name + "Egg_AT");//.Replace("(Clone)", ""); ;

            //DBG.blogDebug("past clone");
            int newHash = gameObject.name.GetStableHashCode();
            if (!(bool)ObjectDB.instance.GetItemPrefab(newHash))
            {
                //DBG.blogDebug("Adding to ObjectDB");
                ObjectDB.instance.m_itemByHash.Add(newHash, gameObject);

            }
            ItemDrop[] MultiiDrops = gameObject.GetComponents<ItemDrop>();
            if (MultiiDrops.Length > 1)
            {
                //DBG.blogDebug("removing one iDrop");
                Component.DestroyImmediate(MultiiDrops[0]);
            }
            //DBG.blogDebug("iDrops size=" + gameObject.GetComponents<ItemDrop>().Length);
            ItemDrop iDrop = gameObject.GetComponent<ItemDrop>();
            ItemDrop.ItemData iData = iDrop.m_itemData;
            Character prefab_char = prefab.GetComponent<Character>();
            if ((bool)prefab_char)
            {
                iData.m_shared.m_name = prefab_char.m_name + " Egg";
                DBG.blogDebug("char name =" + prefab_char.m_name);
            }
            else
            {
                iData.m_shared.m_name = prefab.name + " Egg";
            }
            //iData.m_shared.m_name = prefab.name + " Egg";
            iData.m_shared.m_description = "Egg from a "+ prefab.name+ ", place it near some heat and it may hatch";
            float power = 3;
            if (size > 1) { power = 2.2f; }
            iData.m_shared.m_weight = iData.m_shared.m_weight * Mathf.Pow(size,power);
            iData.m_shared.m_teleportable = true;
            //iData.m_shared.m_maxStackSize = 20; ;
            iDrop.Save();
            //iData.m_shared.m_icons[0] = iconsprites[1] as Sprite;
            //iData.m_dropPrefab = gameObject;
            //DBG.blogDebug("dropPrefab=" + iData.m_dropPrefab);
            EggGrow eggGrow = gameObject.GetComponent<EggGrow>();
            eggGrow.m_growTime = hatchTime;
            bool createOffspring = true;
            //see if there is already an offspring to set as hatched prefab
            if (prefab.TryGetComponent<Procreation>(out Procreation proc))
            {
                if((bool)proc.m_offspring && !tb.procretionOverwrite)
                {
                    createOffspring = false;
                    eggGrow.m_grownPrefab = proc.m_offspring;
                }
            }
            if (createOffspring) { eggGrow.m_grownPrefab = SpawnMini(prefab); }

            GameObject model;
            
            if (DrakeEgg) { model = gameObject.transform.Find("attach").Find("model").transform.gameObject; }
            else { model = gameObject.transform.Find("attach").Find("default").transform.gameObject; }


            if (size != 1)
            {
                //change size of egg
                gameObject.transform.Find("attach").localScale = new Vector3(size, size, size);
                LODGroup lod = model.GetComponent<LODGroup>();
                if ((bool)lod)
                {
                   lod.size =Mathf.Max(9.3f-(2.14f*size),5f);
                }
                //DBG.blogDebug("localscale=" + gameObject.transform.Find("attach").localScale);
                DBG.blogDebug("Changed Size");
            }
            
            //change color
           
            //mat.color = Color.cyan;
            if (color != "default")
            {
                DBG.blogDebug("changing color");
                Material  mat = model.GetComponent<MeshRenderer>().material;
                if (color.Length==7 && color[0] == '#')
                {
                    DBG.blogDebug("attempting hex for " + color);
                    Color replacecol = Utils2.colFromHex(color);
                    
                    //mat.SetFloat("_BumpScale", 0.8f);
                    //mat.SetFloat("_Metallic", 0.1f);
                    string textureStr = "NewChickenEgg";
                    if (DrakeEgg) 
                    {
                        mat.color = replacecol;
                        mat.SetColor("_EmissionColor", replacecol*new Color(1,3,1));
                        textureStr = "NewDragonEgg";
                        Light light = gameObject.transform.Find("attach").Find("Point light").transform.gameObject.GetComponent<Light>();
                        light.color = replacecol;
                    }
                    else
                    {
                        mat.color = 1.2f*replacecol * new Color(0.9f*replacecol.r -0.1f, 0.95f, 1.3f);//accounts for slight yellow of base texture
                    }
                    //Texture2D newTex = iData.m_shared.m_icons[0].texture;

                    Object[] iconsprites = PrefabManager.getAssetBundle().LoadAssetWithSubAssets("Assets/CustomItems/" + textureStr + ".png");
                    Sprite baseSprite = (Sprite)iconsprites[1];
                    Texture2D newTex = baseSprite.texture;
                    //DBG.blogDebug("newTex name=" + newTex.name);
                    newTex = Utils2.changeEggTex(newTex, replacecol,DrakeEgg);
                    Sprite newSprite = Sprite.Create(newTex, baseSprite.rect, baseSprite.pivot);
                    iData.m_shared.m_icons[0] = newSprite;

                }
                else
                {
                    DBG.blogWarning("Not correctly formatted as HEX color");
                }
                
            }
            else
            {
                DBG.blogDebug("Keeping color default");
            }

            //DBG.blogDebug("chickenEggDrop="+ZNetScene.instance.GetPrefab("ChickenEgg").GetComponent<ItemDrop>().m_itemData.m_dropPrefab);

            try
            {
                //PrefabManager.PostRegister(gameObject);
                Jotunn.Managers.PrefabManager.Instance.RegisterToZNetScene(gameObject);
                DBG.blogInfo("Succesfully registered "+gameObject.name);
            }
            catch
            {
                DBG.blogInfo("Already Registered " + gameObject.name);
            }
            return gameObject;
        }
    }
}
