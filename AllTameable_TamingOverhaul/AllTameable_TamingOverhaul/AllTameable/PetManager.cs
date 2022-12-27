using AllTameable;
using AllTameable.RPC;
using Jotunn.Managers;
using System.Collections.Generic;
//using System;
using System.Reflection;
using UnityEngine;

namespace AllTameable
{
    internal class PetManager : MonoBehaviour
    {
        private static ZNetScene zns;

        private static Tameable wtame;

        public static GameObject Root;

        public static bool isInit;
        public static bool isInit2;
        public static bool fixedDeerAI;
        public static GameObject heartEffectGO;

        public static GameObject DragonEgg;

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
                    DBG.blogDebug("Failed to Post added " + (tot - succ) + "/" + tot);
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
                InitDrakeEgg();
            }
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


        public static void Init()
        {
            isInit = true;
            zns = ZNetScene.instance;
            wtame = zns.GetPrefab("Wolf").GetComponent<Tameable>();
            if (Plugin.HatchingEgg.Value)
            {
                InitDrakeEgg();
            }
            DBG.blogDebug("Making HeartEffect");
            heartEffectGO = Object.Instantiate(wtame.m_petEffect.m_effectPrefabs[0].m_prefab, Root.transform);
            DBG.blogDebug("removing child");
            heartEffectGO.transform.GetChild(1).parent = null;
            DBG.blogDebug("Creating new effect");
            EffectList.EffectData effectData2 = new EffectList.EffectData();
            DBG.blogDebug("setting prefab to go");
            effectData2.m_prefab = heartEffectGO;
            DBG.blogDebug("Created HeartEffect");




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
                if (!go.TryGetComponent<Tameable>(out var component))
                {
                    component = go.AddComponent<Tameable>();
                }

                MonsterAI component2 = go.GetComponent<MonsterAI>();

                if (tb.changeFaction == false)
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


                component.m_sootheEffect = wtame.m_sootheEffect;
                component.m_commandable = tb.commandable;
                component.m_tamingTime = tb.tamingTime;
                component.m_fedDuration = tb.fedDuration;
                component2.m_consumeRange = tb.consumeRange;
                component2.m_consumeSearchInterval = tb.consumeSearchInterval;
                //component2.m_consumeHeal = tb.consumeHeal;
                component2.m_consumeSearchRange = tb.consumeSearchRange;
                List<string> list = new List<string>();
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
                else { DBG.blogDebug("Cannot be tamed by food"); }

                if (tb.procretion)
                {
                    //DBG.blogDebug("has procreation");
                    bool flag = true;
                    if (!go.TryGetComponent<Procreation>(out var component3))
                    {
                        component3 = go.AddComponent<Procreation>();
                        flag = false;
                    }
                    component3.m_maxCreatures = tb.maxCreatures * 2;
                    component3.m_pregnancyChance = tb.pregnancyChance;
                    if (component3.m_pregnancyChance > 1) { component3.m_pregnancyChance *= 0.01f; }
                    component3.m_pregnancyDuration = tb.pregnancyDuration;
                    component3.m_partnerCheckRange = 30f;
                    component3.m_totalCheckRange = 30f;
                    if (flag && component3.m_offspring != null && !Plugin.CheckHuman(go))
                    {
                        //DBG.blogDebug("already has offspring");

                        Growup component4 = component3.m_offspring.GetComponent<Growup>();
                        component4.m_growTime = tb.growTime;
                    }
                    else if (go.name == "Hatchling" && Plugin.HatchingEgg.Value)
                    {
                        component3.m_offspring = DragonEgg;
                    }
                    else if ((go.name == "Seeker" | go.name == "SeekerBrute") && Plugin.SeekerBroodOffspring.Value)
                    {
                        DBG.blogDebug("Setting " + go.name + " Offspring to Seeker Brood");
                        component3.m_offspring = zns.GetPrefab("SeekerBrood");

                        if (component3.m_offspring.GetComponent<Tameable>() == null)
                        {
                            DBG.blogDebug("Making Seeker Brood only tameable by offspring");
                            string tameList = "SeekerBrood,true,1,90,2,10,30,20,,false,false,10,0,150,400";
                            string[] arr = tameList.Split(',');
                            Plugin.TameTable seekerbrood_tamtetable = TameListCfg.ArrToTametable(arr);
                            AddTame(component3.m_offspring, seekerbrood_tamtetable);
                        }

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
                        DBG.blogDebug("*******prefabName = " + serverfab.name + " ********");
                        DBG.blogDebug("istameable = " + serverfab.GetComponent<Tameable>().enabled);
                    }
                }
                else
                {
                    DBG.blogDebug("Added ability to tame to " + go.name);
                }
            }
            catch
            {
                DBG.blogWarning("Failed to add tame to prefab: " + go.name + ", Make sure config files are formatted correctly");
            }
        }

        private static GameObject SpawnMini(GameObject prefab)
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
            growup.m_growTime = Plugin.cfgList[text].growTime;

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

        private static void InitDrakeEgg()
        {
            GameObject prefab = ZNetScene.instance.GetPrefab("DragonEgg");
            GameObject gameObject = Object.Instantiate(prefab, Root.transform);
            gameObject.name = "HatchingDragonEgg";
            Object.DestroyImmediate(gameObject.GetComponent<ItemDrop>());
            //ItemDrop itdrop = prefab.gameObject.GetComponent<ItemDrop>();
            //gameObject.AddComponent<ItemDrop>(itdrop);
            Hatch hatch = gameObject.AddComponent<Hatch>();
            hatch.m_name = prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
            hatch.m_grownPrefab = zns.GetPrefab("Hatchling");
            hatch.m_growTime = Plugin.HatchingTime.Value;
            DragonEgg = gameObject;
            try
            {
                //PrefabManager.PostRegister(gameObject);
                Jotunn.Managers.PrefabManager.Instance.RegisterToZNetScene(gameObject);
                DBG.blogInfo("Succesfully registered HatchingDragonEgg");
            }
            catch
            {
                DBG.blogInfo("Already Registered DragonEgg");
            }
        }
    }
}
