using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace AllTameable
{
    public class PrefabManager : MonoBehaviour
    {
        public static string tametoolname = Plugin.tamingtoolPrefabName;
        public static string advtoolname = Plugin.advtoolPrefabName;
        public static string t1foodname = Plugin.t1foodPrefabName;
        public static string t2foodname = Plugin.t2foodPrefabName;
        public static string t3foodname = Plugin.t3foodPrefabName;
        public StatusEffect effect = ScriptableObject.CreateInstance<StatusEffect>();
        public GameObject Root;
        private static GameObject FlakeEffect;
        private static AssetBundle tamingAssets;


        public void Clear()
        {
            Plugin.petManager.Clear();
            //UnRegister();
        }

        private void Awake()
        {
            Root = new GameObject("PrefabList");
            Root.transform.SetParent(Plugin.Root.transform);
            Root.SetActive(value: false);
            tamingAssets = AssetUtils.LoadAssetBundleFromResources("taming_icons", Assembly.GetExecutingAssembly());
        }
        public static AssetBundle getAssetBundle()
        {
            return tamingAssets;
        }

        public static void ItemReg()
        {
            if (Plugin.useTamingTool.Value)
            {
                addTameStick();
                addAdvTameStick();
            }
            
            addT1TameFood();
            addT2TameFood();
            addT3TameFood();

            //RecipeReg();

            GameObject prefab = Jotunn.Managers.PrefabManager.Instance.GetPrefab("MeadHealthMedium");
            GameObject go = Jotunn.Managers.PrefabManager.Instance.CreateClonedPrefab("AT_HealthEffect", prefab);

            Plugin.prefabManager.EffectReg();

            Jotunn.Managers.PrefabManager.OnVanillaPrefabsAvailable -= PrefabManager.ItemReg;

        }

        public static void addTameStick()
        {
            //AssetBundle embeddedResourceBundle = AssetUtils.LoadAssetBundleFromResources("taming_icons", Assembly.GetExecutingAssembly());
            //create tame tool
            UnityEngine.Object[] iconsprites = tamingAssets.LoadAssetWithSubAssets("Assets/CustomItems/tamingtool.png");
            DBG.blogDebug("Adding recipe for TamingStick");
            ItemConfig tamestickConfig = new ItemConfig();
            tamestickConfig.AddRequirement(new RequirementConfig("RawMeat", 1));
            tamestickConfig.AddRequirement(new RequirementConfig("Mushroom", 1));
            tamestickConfig.AddRequirement(new RequirementConfig("Carrot", 1));
            tamestickConfig.AddRequirement(new RequirementConfig("DragonEgg", 1));
            //tamestickConfig.AddRequirement(new RequirementConfig("Resin", 1));
            tamestickConfig.CraftingStation = "piece_workbench";
            tamestickConfig.MinStationLevel = 1;

            CustomItem tamestick = new CustomItem(tametoolname, "Club", tamestickConfig);
            ItemManager.Instance.AddItem(tamestick);
            Transform tameTransform = Jotunn.Managers.PrefabManager.Instance.GetPrefab(tametoolname).transform.Find("attach").transform;

            var id = tamestick.ItemDrop;
            var id2 = id.m_itemData;
            id2.m_shared.m_name = "Taming Tool";
            id2.m_shared.m_description = "Hold in hand while attmpting to tame a creature to see its taming requirements";
            id2.m_shared.m_icons[0] = iconsprites[1] as Sprite;
            string[] meshtoadd = { "Mushroom", "RawMeat", "Carrot" };
            foreach (string itemname in meshtoadd)
            {
                try
                {

                    GameObject itemBase = Jotunn.Managers.PrefabManager.Instance.GetPrefab(itemname);
                    GameObject clone = Jotunn.Managers.PrefabManager.Instance.CreateClonedPrefab(itemname + "_at", itemBase);
                    //var clone = itemBase;
                    var attachbase = clone.GetComponent<Transform>().Find("attach");
                    
                    //attachbase.name = attachbase.name + "_" + itemname;
                    var attachcopy = AttachChild(tameTransform, attachbase, itemname);
                    Transform cptrans = attachcopy.transform;
                    if (itemname == "Mushroom")
                    {
                        cptrans.localPosition = new Vector3(0, 0.03f, 0.7f);
                        cptrans.localEulerAngles = new Vector3(130, 180, 180);
                        cptrans.localScale = new Vector3(0.8f, 0.8f, 0.8f);
                    }
                    else if (itemname == "RawMeat")
                    {
                        cptrans.localPosition = new Vector3(0.01f, 0.01f, 0.15f);
                        cptrans.localEulerAngles = new Vector3(8, 183, 180);
                        cptrans.localScale = new Vector3(0.8f, 0.8f, 0.9f);
                    }
                    else if (itemname == "Carrot")
                    {
                        cptrans.localPosition = new Vector3(0, -0.02f, 0.78f);
                        cptrans.localEulerAngles = new Vector3(80, 180, 180);
                        cptrans.localScale = new Vector3(0.25f, 0.6f, 0.25f);
                        cptrans.Find("blast (1)").transform.localScale = new Vector3(1, 0.6f, 1);
                    }
                }
                catch
                {
                    Debug.LogWarning("Failed to add " + itemname);
                }
            }
            try
            {
                tameTransform.Find("model").gameObject.SetActive(false);
            }
            catch
            {
                Debug.LogWarning("failed to remove model");
            }
        }

        public static void addAdvTameStick()
        {
            //AssetBundle embeddedResourceBundle = AssetUtils.LoadAssetBundleFromResources("taming_icons", Assembly.GetExecutingAssembly());
            //create tame tool
            UnityEngine.Object[] iconsprites = tamingAssets.LoadAssetWithSubAssets("Assets/CustomItems/AdvTamingTool.png");
            DBG.blogDebug("Adding recipe for Advanced TamingStick");
            ItemConfig tamestickConfig = new ItemConfig();
            tamestickConfig.AddRequirement(new RequirementConfig("Crystal", 3));
            tamestickConfig.AddRequirement(new RequirementConfig("MushroomMagecap", 2));
            tamestickConfig.AddRequirement(new RequirementConfig("Eitr", 5));
            tamestickConfig.AddRequirement(new RequirementConfig("FineWood", 10));
            tamestickConfig.AddRequirement(new RequirementConfig("MeadHealthMajor", 2));
            tamestickConfig.CraftingStation = "piece_workbench";
            tamestickConfig.MinStationLevel = 2;

            CustomItem tamewand = new CustomItem(advtoolname, "Club", tamestickConfig);
            //DBG.blogDebug("Adding TamingStick");
            ItemManager.Instance.AddItem(tamewand);
            Transform tameTransform = Jotunn.Managers.PrefabManager.Instance.GetPrefab(advtoolname).transform.Find("attach").transform;
            var id = tamewand.ItemDrop;
            var id2 = id.m_itemData;
            id2.m_shared.m_name = "Taming Wand";
            id2.m_shared.m_description = "Increased range for taming interaction";
            id2.m_shared.m_icons[0] = iconsprites[1] as Sprite;
            string[] meshtoadd = { "Eitr", "Crystal", "MushroomMagecap", "SpearCarapace" };
            string[] attachpoint = { "attach", "Cube", "attach", "attach" };
            for(int i =0; i< meshtoadd.Count();i++)
            {
                string itemname = meshtoadd[i];
                try
                {

                    GameObject itemBase = Jotunn.Managers.PrefabManager.Instance.GetPrefab(itemname);
                    GameObject clone = Jotunn.Managers.PrefabManager.Instance.CreateClonedPrefab(itemname + "_at", itemBase);
                    //var clone = itemBase;
                    var attachbase = clone.GetComponent<Transform>().Find(attachpoint[i]);

                    //attachbase.name = attachbase.name + "_" + itemname;
                    var attachcopy = AttachChild(tameTransform, attachbase, itemname);
                    Transform cptrans = attachcopy.transform;
                    if (itemname == "Eitr")
                    {
                        cptrans.localPosition = new Vector3(0, 0, 0.54f);
                        //cptrans.localEulerAngles = new Vector3(130, 180, 180);
                        cptrans.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                        Transform toRemove = attachcopy.Find("Radiator");
                        toRemove.gameObject.SetActive(false);
                        toRemove = attachcopy.Find("sparcs_world");
                        toRemove.gameObject.SetActive(false);
                        //if ((bool)toRemove) { toRemove.parent = null; }
                        if (!(bool)FlakeEffect) { setFlakes(); }
                        //DBG.blogDebug("Set Flakes");
                        if (!(bool)FlakeEffect)
                        {
                            DBG.blogDebug("Flakes still null");
                        }
                        Transform partTrans = AttachChild(attachcopy, FlakeEffect.transform, "newFlake");
                        //Transform partTrans = attachcopy.Find("sparcs_world");
                        partTrans.localEulerAngles = new Vector3(350, 0, 17);
                        partTrans.localScale = new Vector3(1, 1, 1);
                        ParticleSystem part = partTrans.GetComponent<ParticleSystem>();
                        if ((bool)part)
                        {
                            part.emissionRate = 5;
                            part.maxParticles = 20;
                            part.startSize = 0.03f;
                            part.startLifetime = 2.5f;
                            part.startColor = new Color(0.5f, 0.5f, 1, 1);
            
                            ParticleSystem.ForceOverLifetimeModule Force = part.forceOverLifetime; // new ParticleSystem.ForceOverLifetimeModule();
                            Force.enabled = true;
                            //Force.y = new ParticleSystem.MinMaxCurve(-2, 2);
                            Force.space = ParticleSystemSimulationSpace.Local;
                            Force.yMultiplier = 0.4f;
                            
                            ParticleSystem.VelocityOverLifetimeModule Velo = part.velocityOverLifetime;
                            Velo.space = ParticleSystemSimulationSpace.Local;
                            Velo.enabled = true;
                            Velo.orbitalXMultiplier = 1;
                            Velo.orbitalZMultiplier = 1;
                            Velo.orbitalYMultiplier = 6;
                            Velo.radialMultiplier = 0.03f;
                            Velo.yMultiplier = -0.3f;

                            ParticleSystem.ShapeModule Shape = part.shape;
                            Shape.radius = 0.1f;
                            ParticleSystem.NoiseModule noise = part.noise;
                            noise.enabled = false;
                            ParticleSystem.ColorOverLifetimeModule col= part.colorOverLifetime;
                            GradientColorKey key0 = new GradientColorKey(new Color(0, 0.2f, 1, 1), 0);
                            //GradientColorKey key0 = new GradientColorKey(new Color(0.2f, 0.3f, 1, 1), 0);
                            GradientColorKey key1 = new GradientColorKey(new Color(0, 0.4f, 1f, 1), 0.1f);
                            GradientColorKey key2 = new GradientColorKey(new Color(0.5f, 0.2f, 0.7f, 1), 0.6f);
                            GradientColorKey[] colKeys = { key0, key1, key2 };
                            Gradient grad = col.color.gradient;
                            grad.colorKeys = colKeys;
                            //ParticleSystem.MinMaxGradient minmaxGrad = new ParticleSystem.MinMaxGradient(grad);
                            col.color = new ParticleSystem.MinMaxGradient(grad);
                            //DBG.blogDebug("col.color.gradient.colorKeys[0].color=" + col.color.gradient.colorKeys[0].color);

                        }

                        Transform pnt_light = attachcopy.Find("Point light");
                        Light light = pnt_light.GetComponent<Light>();
                        light.intensity = 3;
                        light.range = 0.2f;
                        light.color = new Color(1, 0, 1, 1);
                        LightFlicker flick = pnt_light.GetComponent<LightFlicker>();
                        flick.m_baseIntensity = 1;

                        Transform core = attachcopy.Find("core");
                        if ((bool)core)
                        {
                            UtilColorFade colFade = core.gameObject.AddComponent<UtilColorFade>();
                            colFade.roughness = 0.1f;
                            colFade.cycletime = 10;
                            colFade.mat = core.GetComponent<MeshRenderer>().material;
                            colFade.colType = "_EmissionColor";
                            GradientColorKey col1 = new GradientColorKey(new Color(0.4f, 0, 0.4f), 0);
                            GradientColorKey col2 = new GradientColorKey(new Color(0, 0.5f, 0.5f), 0.33f);
                            GradientColorKey col3 = new GradientColorKey(new Color(0.4f, 0, 0.05f), 0.66f);
                            GradientColorKey col4 = new GradientColorKey(new Color(0.4f, 0, 0.4f), 1);
                            GradientColorKey[] colKeys = { col1, col2, col3, col4 };
                            //colFade.clrKeys = colKeys;
                            Gradient grad = new Gradient();
                            grad.colorKeys = colKeys;
                            colFade.grad = grad;
                            //DBG.blogDebug("colFade.clrKeys=" + colFade.grad.colorKeys[1].color);


                        }
                    }
                    else if (itemname == "Crystal")
                    {
                        //cptrans.localPosition = new Vector3(0, 0.01f, 0.46f);
                        //cptrans.localEulerAngles = new Vector3(20, 0, 0);
                        cptrans.localScale = new Vector3(0.015f, 0.04f, 0.015f);
                        int numcrystal = 5;
                        for(int j = 0; j < numcrystal; j++)
                        {
                            var crystal = cptrans;
                            if (j > 0) { crystal = AttachChild(tameTransform, attachcopy, itemname); }
                            crystal.localEulerAngles = new Vector3(((j * 360 / numcrystal)) % 360, 90, -40);
                            crystal.localPosition = new Vector3(0, 0.01f, 0.52f);
                        }
                    }
                    else if (itemname == "MushroomMagecap")
                    {
                        cptrans.localPosition = new Vector3(0.07f, 0, 0.24f);
                        cptrans.localEulerAngles = new Vector3(270, 148, 0);
                        cptrans.localScale = new Vector3(0.75f, 0.45f, 0.75f);
                    }
                    else if (itemname == "SpearCarapace")
                    {
                        cptrans.localPosition = new Vector3(0, 0, 0.425f);
                        cptrans.localEulerAngles = new Vector3(0, 180, 0);
                        cptrans.localScale = new Vector3(1, 1, 0.4f);
                        MeshRenderer meshRen = attachcopy.Find("carapacespear").GetComponent<MeshRenderer>();
                        if (meshRen != null)
                        {
                            //meshRen.material.color = new Color(1, 0.8f, 0, 1);
                            //meshRen.material.EnableKeyword("_EMISSION");
                            //meshRen.material.SetColor("_EmissionColor", new Color(0.05f, 0.02f, 0.04f, 0));
                            meshRen.material.color = new Color(2, 1.7f, 1.7f, 0.6f);
                        }
                    }
                }
                catch
                {
                    Debug.LogWarning("Failed to add " + itemname);
                }
            }
            try
            {
                tameTransform.Find("model").gameObject.SetActive(false);
            }
            catch
            {
                Debug.LogWarning("failed to remove model");
            }
        }


        public static void addT3TameFood()
        {
            UnityEngine.Object[] iconspritesfood = tamingAssets.LoadAssetWithSubAssets("Assets/CustomItems/T3TamingFood.png");
            DBG.blogDebug("Adding recipe for TameFoodT3");
            ItemConfig tamefoodConfig = new ItemConfig();
            tamefoodConfig.AddRequirement(new RequirementConfig("MeadEitrMinor", 4));
            tamefoodConfig.AddRequirement(new RequirementConfig("MeadHealthMajor", 2));
            tamefoodConfig.AddRequirement(new RequirementConfig("Fish9", 1)); //fish9=anglerfish
            tamefoodConfig.AddRequirement(new RequirementConfig("MushroomMagecap", 5));
            
            tamefoodConfig.CraftingStation = "piece_cauldron";
            tamefoodConfig.MinStationLevel = 5;
            tamefoodConfig.Amount = 1;
            //tamefoodT1Config.AddRequirement(new RequirementConfig("LeatherScraps", 1));

            CustomItem tamefood = new CustomItem(t3foodname, "MushroomMagecap", tamefoodConfig);
            ItemManager.Instance.AddItem(tamefood);
            Transform tamefoodTransform = Jotunn.Managers.PrefabManager.Instance.GetPrefab(t3foodname).transform; //.Find("attach").transform;
            var id_tamefood = tamefood.ItemDrop;
            var id2_tamefood = id_tamefood.m_itemData;
            id2_tamefood.m_shared.m_name = "Excellent Taming Food";
            id2_tamefood.m_shared.m_description = "Food that can be fed to creatures you are attempting to tame to give a significant reduction in taming time";
            id2_tamefood.m_shared.m_icons[0] = iconspritesfood[1] as Sprite;
            tamefoodTransform.Find("attach").parent = null;
            try
            {
                GameObject itemBase = Jotunn.Managers.PrefabManager.Instance.GetPrefab("Fish9");
                GameObject clone = Jotunn.Managers.PrefabManager.Instance.CreateClonedPrefab("fish9" + "_at", itemBase);
                var attachbase = clone.GetComponent<Transform>().Find("attachobj");
                var attachcopy = AttachChild(tamefoodTransform, attachbase, "AnglerFish");
                Transform toremove = attachcopy.Find("DeadSpeak_Base");
                attachcopy.localScale = new Vector3(0.6f, 0.6f, 0.6f);
                attachcopy.GetChild(0).gameObject.layer = 12; //item
                if ((bool)toremove) { toremove.parent = null; }
                try
                {
                    GameObject flametalPrefab = Jotunn.Managers.PrefabManager.Instance.GetPrefab("FlametalOre");
                    MeshRenderer fametalMesh = Jotunn.Managers.PrefabManager.Instance.CreateClonedPrefab("FlametalOreCopy", flametalPrefab).transform.Find("stone").GetComponent<MeshRenderer>();
                    if (fametalMesh != null)
                    {
                        MeshRenderer fishmesh = attachcopy.GetChild(0).GetComponent<MeshRenderer>();
                        fishmesh.material = fametalMesh.material;
                        fishmesh.material.color = new Color(0.5f, 0, 0.7f, 1);
                        fishmesh.material.SetFloat("_BumpScale", 0.8f);
                        fishmesh.material.SetFloat("_Metallic", 0.1f);
                        fishmesh.material.SetColor("_EmissionColor", new Color(0.6f, 0, 2, 0));
                    }
                } catch{}


            }
            catch
            {
                Debug.LogWarning("Food Failed to add " + "fish9");
            }
            try
            {
                GameObject itemBase = Jotunn.Managers.PrefabManager.Instance.GetPrefab("DragonTear");
                GameObject clone = Jotunn.Managers.PrefabManager.Instance.CreateClonedPrefab("DragonTear2" + "_at", itemBase);
                
                var attachbase = clone.GetComponent<Transform>().Find("attach");
                var attachcopy = AttachChild(tamefoodTransform, attachbase.transform.Find("Point light"), "DragonTear_Light1");
                var attachflare = AttachChild(attachcopy, attachbase.transform.Find("flare"), "flare");
                
                attachcopy.name = "Light_1";
                Light PLPart = attachcopy.GetComponent<Light>();
                if (PLPart != null)
                {
                    //smokePart.startSize = 1.6f;
                    PLPart.color = new Color(0.7f, 0.1f, 1, 1);
                    PLPart.intensity = 5;
                    PLPart.range = 0.6f;
                }
                else
                {
                    DBG.blogDebug("Light is null");
                }
                LightFlicker flickPart = attachcopy.GetComponent<LightFlicker>();
                if (flickPart != null)
                {
                    //smokePart.startSize = 1.6f;
                    flickPart.m_baseIntensity = 3; ;
                    //flickPart.m_basePosition = new Vector3(0, 0.2f, 0);
                    flickPart.m_flickerSpeed = 2.8f;
                    flickPart.m_movement = 0.15f;
                }
                else
                {
                    DBG.blogDebug("Flicker is null");
                }
                ParticleSystem flarePart = attachflare.GetComponent<ParticleSystem>();
                if (flarePart != null)
                {
                    flarePart.startSize = 0.8f;
                    flarePart.startColor = new Color(1, 0, 1, 0.19f);
                }
                //var attachcopy2 = AttachChild(tamefoodTransform, attachcopy, "DragonTear_Light2");
                var attachcopy2 = Component.Instantiate(attachcopy, tamefoodTransform);
                
                attachcopy2.name = "Light_2";
                Light PLPart2 = attachcopy2.GetComponent<Light>();
                if (PLPart2 != null)
                {
                    PLPart2.color = new Color(1f, 0, 0, 1);
                }
                LightFlicker flickPart2 = attachcopy.GetComponent<LightFlicker>();
                if (flickPart2 != null)
                {
                    flickPart2.m_flickerSpeed = 3.2f;
                }
                /*
                for (int i = 0; i < attachcopy2.childCount; i++)
                {
                    DBG.blogDebug("T3child#" + i + "=" + attachcopy2.transform.GetChild(i).name);
                }
                */
                    ParticleSystem flarePart2 = attachcopy2.Find("flare_at").GetComponent<ParticleSystem>();
                if (flarePart2 != null)
                {
                    flarePart2.startColor = new Color(1, 0, 0, 0.19f);
                }
            }
            catch
            {
                Debug.LogWarning("Food Failed to add " + "DragonTear_Light");
            }
            try
            {
                GameObject itemBase = Jotunn.Managers.PrefabManager.Instance.GetPrefab("vfx_Potion_eitr_minor");
                GameObject clone = Jotunn.Managers.PrefabManager.Instance.CreateClonedPrefab("eitrpotion" + "_at", itemBase);
                var attachbase = clone.GetComponent<Transform>();
                var attachcopy = AttachChild(tamefoodTransform, attachbase.Find("trails"), "EitrPotion");
                attachcopy.localScale = new Vector3(0.45f, 0.45f, 0.45f);
                attachcopy.gameObject.AddComponent<UtilAlignUp>();
                ParticleSystem trailsPart = attachcopy.GetComponent<ParticleSystem>();
                trailsPart.loop = true;
                trailsPart.emissionRate = 10;
                trailsPart.gravityModifier = 0.1f;

            }
            catch
            {
                Debug.LogWarning("Food Failed to add " + "fish9");
            }
            //tamefoodTransform.gameObject.layer = 12; //item



        }

        private static void setFlakes()
        {
            GameObject itemBase = Jotunn.Managers.PrefabManager.Instance.GetPrefab("DragonTear");
            GameObject clone = Jotunn.Managers.PrefabManager.Instance.CreateClonedPrefab("DragonTear" + "_at", itemBase);
            FlakeEffect = clone.transform.Find("attach").Find("pixel flakes").gameObject;
        }


        public static void addT2TameFood()
        {
            UnityEngine.Object[] iconspritesfoodT2 = tamingAssets.LoadAssetWithSubAssets("Assets/CustomItems/T2TamingFood.png");
            DBG.blogDebug("Adding recipe for TameFoodT2");
            ItemConfig tamefoodT2Config = new ItemConfig();
            tamefoodT2Config.AddRequirement(new RequirementConfig("MeadHealthMedium", 6));
            tamefoodT2Config.AddRequirement(new RequirementConfig("BarleyFlour", 10));
            tamefoodT2Config.AddRequirement(new RequirementConfig("DragonTear", 1));
            tamefoodT2Config.CraftingStation = "piece_cauldron";
            tamefoodT2Config.MinStationLevel = 3;
            tamefoodT2Config.Amount = 3;
            //tamefoodT1Config.AddRequirement(new RequirementConfig("LeatherScraps", 1));

            CustomItem tamefood = new CustomItem(t2foodname, "Bread", tamefoodT2Config);
            ItemManager.Instance.AddItem(tamefood);
            Transform tamefoodTransform = Jotunn.Managers.PrefabManager.Instance.GetPrefab(t2foodname).transform.Find("attach").transform;
            var id_tamefood = tamefood.ItemDrop;
            var id2_tamefood = id_tamefood.m_itemData;
            id2_tamefood.m_shared.m_name = "Superior Taming Food";
            id2_tamefood.m_shared.m_description = "Food that can be fed to creatures you are attempting to tame to give a reasonable reduction in taming time";
            id2_tamefood.m_shared.m_icons[0] = iconspritesfoodT2[1] as Sprite;
            GameObject itemBase = Jotunn.Managers.PrefabManager.Instance.GetPrefab("DragonTear");

            try
            {
                //GameObject itemBase = Jotunn.Managers.PrefabManager.Instance.GetPrefab("DragonTear");
                GameObject clone = Jotunn.Managers.PrefabManager.Instance.CreateClonedPrefab("DragonTear" + "_at", itemBase);
                //DBG.blogDebug("added " + clone.name + " as a prefab");
                var attachbase = clone.GetComponent<Transform>().Find("attach");
                for (int i = 0; i < attachbase.childCount; i++)
                {
                    //DBG.blogDebug("T2child#" + i + "=" + attachbase.transform.GetChild(i).name);
                    var attachcopy =AttachChild(tamefoodTransform, attachbase.transform.GetChild(i), attachbase.transform.GetChild(i).name);
                    switch (attachcopy.name)
                    {
                        case "flare_at":
                            //DBG.blogDebug("In flare case");
                            ParticleSystem flarePart = attachcopy.GetComponent<ParticleSystem>();
                            if (flarePart != null)
                            {
                                flarePart.startSize = 2f;
                                flarePart.startColor = new Color(1, 0.28f, 0.28f, 0.05f);
                            }
                            break;
                        case "smoke_expl_at":
                            //DBG.blogDebug("In smoke_expl_at case");
                            ParticleSystem smokePart = attachcopy.GetComponent<ParticleSystem>();
                            if (smokePart != null)
                            {
                                //smokePart.startSize = 1.6f;
                                smokePart.startColor = new Color(1, 0.55f, 0, 0.04f);
                                smokePart.startLifetime = 1;
                            }
                            break;
                        case "pixel flakes_at":
                            //DBG.blogDebug("In pixel flakes_at case");
                            FlakeEffect = attachcopy.gameObject;
                            ParticleSystem flakesPart = attachcopy.GetComponent<ParticleSystem>();
                            if (flakesPart != null)
                            {
                                flakesPart.startColor = new Color(1, 0.3f, 0.3f, 1);
                                flakesPart.startSpeed = 0.5f;
                                flakesPart.startLifetime = 0.0001f;
                                flakesPart.maxParticles = 4;
                                flakesPart.emissionRate = 6;
                                flakesPart.gravityModifier = -0.2f;
                            }
                            break;
                        case "model_at":
                            //DBG.blogDebug("In model_at case");
                            attachcopy.localEulerAngles = new Vector3(0, 0, 0);
                            attachcopy.localPosition = new Vector3(0, 0.14f, 0);
                            attachcopy.localScale = new Vector3(0.73f, 0.7f, 1f);

                            try { attachcopy.Find("inner").gameObject.SetActive(false); }
                            catch {Debug.LogWarning("failed to remove inner");}
                            Transform hull_trans = attachcopy.Find("hull");
                            try
                            {
                                MeshRenderer meshRen = hull_trans.GetComponent<MeshRenderer>();
                                if (meshRen != null){meshRen.material.color = new Color(1, 0.9f, 0.9f, 0.48f);}
                                try
                                {
                                    Destroy(hull_trans.GetComponent<SphereCollider>());
                                }catch { Debug.LogWarning("failed to remove spherecollider"); }

                            }
                            catch { Debug.LogWarning("failed to modify hull"); }

                            break;
                        case "Point light_at":
                            //DBG.blogDebug("In Point light_at case");
                            Light PLPart = attachcopy.GetComponent<Light>();
                            if (PLPart != null)
                            {
                                //smokePart.startSize = 1.6f;
                                PLPart.color = new Color(1, 0.5f, 0.5f, 1);
                                PLPart.intensity = 3;
                                PLPart.range = 0.8f;
                            }
                            LightFlicker flickPart = attachcopy.GetComponent<LightFlicker>();
                            if (PLPart != null)
                            {
                                //smokePart.startSize = 1.6f;
                                flickPart.m_baseIntensity = 2; ;
                                flickPart.m_basePosition = new Vector3(0,0.2f,0);
                                flickPart.m_flickerSpeed = 2;
                            }
                            break;

                        default:
                            DBG.blogDebug("Did not modify " + attachcopy.name);
                            break;
                    }
                }

            }
            catch
            {
                Debug.LogWarning("Food Failed to add " + "DragonTear");
            }

            try
            {
                var loaf = tamefoodTransform.GetComponent<Transform>().Find("loaf");
                if (loaf != null)
                {
                    loaf.localEulerAngles = new Vector3(0, 0, 0);
                    loaf.localPosition = new Vector3(0, 0, 0);
                    loaf.localScale = new Vector3(1.3f, 2f, 0.8f);
                    MeshRenderer meshRen = loaf.GetComponent<MeshRenderer>();
                    if (meshRen != null) 
                    {
                        meshRen.material.color = new Color(1, 0.8f, 0, 1);
                        meshRen.material.EnableKeyword("_EMISSION");
                        meshRen.material.SetColor("_EmissionColor", new Color(0.45f,0.15f,0.15f, 1)); 
                    }

                }
            }
            catch { }

            

        }

            public static void addT1TameFood()
        {
            //create taming food
            UnityEngine.Object[] iconspritesfoodT1 = tamingAssets.LoadAssetWithSubAssets("Assets/CustomItems/T1TamingFood.png");
            DBG.blogDebug("Adding recipe for TameFoodT1");
            ItemConfig tamefoodT1Config = new ItemConfig();
            tamefoodT1Config.AddRequirement(new RequirementConfig("MeadTasty", 2));
            tamefoodT1Config.AddRequirement(new RequirementConfig("MushroomYellow", 5));
            tamefoodT1Config.AddRequirement(new RequirementConfig("FreezeGland", 2));
            tamefoodT1Config.AddRequirement(new RequirementConfig("LeatherScraps", 1));
            tamefoodT1Config.CraftingStation = "piece_cauldron";
            tamefoodT1Config.MinStationLevel = 2;
            CustomItem tamefoodT1 = new CustomItem(t1foodname, "MushroomYellow", tamefoodT1Config);
            ItemManager.Instance.AddItem(tamefoodT1);
            Transform tamefoodT1Transform = Jotunn.Managers.PrefabManager.Instance.GetPrefab(t1foodname).transform.Find("attach").transform;
            var id_tamefoodT1 = tamefoodT1.ItemDrop;
            var id2_tamefoodT1 = id_tamefoodT1.m_itemData;
            id2_tamefoodT1.m_shared.m_name = "Basic Taming Food";
            id2_tamefoodT1.m_shared.m_description = "Food that can be fed to creatures you are attempting to tame to give a minor reduction in taming time";
            id2_tamefoodT1.m_shared.m_icons[0] = iconspritesfoodT1[1] as Sprite;
            Vector3[] itemlocations = new Vector3[5];
            Vector3[] itemrotations = new Vector3[5];
            string[] itemnames = new string[5];
            Vector3 baselocation = new Vector3(0, -0.5f, 0.5f);
            Vector3 baserotation = new Vector3(0, 0, 0);
            int mushnumber = 5;
            double radperstep = (2 * Math.PI / mushnumber);
            float distancefromCenter = 0.07f;
            float[] r = { 0, 0.4f, 0.2f, -0.2f, 0.2f };
            float[] g = { 0, 0, -0.1f, 0.1f, -0.3f };
            float[] offset = { 0.01f, 0.02f, -0.03f, 0, -0.02f };
            float[] scales = { 1, 0.95f, 1.05f, 0.975f, 0.925f };
            float[] rot = { 0, 3, -3, 6, -5 }; // max +-20
            for (double i = 0; i < mushnumber; i++)
            {
                //DBG.blogDebug("i=" + i);
                itemlocations[(int)i] = new Vector3((float)((1 - 3 * offset[(int)i]) * distancefromCenter * Math.Cos(i * radperstep)), -0.1f + offset[(int)i], (float)((1 - 3 * offset[(int)i]) * distancefromCenter * Math.Sin(i * radperstep)));
                //new rotations
                itemrotations[(int)i] = (new Vector3(180, 180 - (float)(i * radperstep * 180 / Math.PI), 162));
                //old rotations
                //itemrotations[(int)i] = (new Vector3(0, -(float)(i*radperstep * 180 / Math.PI), -18f));
                itemnames[(int)i] = ("MushroomYellow");
            }
            //itemlocations[mushnumber] = (new Vector3(0, 0, 0));
            //itemrotations[mushnumber] = (Quaternion.  new Vector3(0, 0, 0));
            //itemnames[mushnumber] = ("FreezeGland");
            //float[] r = { 0, 0.1f, 0, -0.1f, -0.05f };
            //float[] g = { 0, 0, -0.01f, 0.05f, -0.05f };



           // DBG.blogDebug("Adding T1TameFood");
           // DBG.blogDebug("itemnames.Length=" + itemnames.Length);
           // DBG.blogDebug("itemnames=" + itemnames);
           // DBG.blogDebug("itemnames.ToString()=" + itemnames.ToString());
            GameObject itemBaseMush = Jotunn.Managers.PrefabManager.Instance.GetPrefab("MushroomYellow");
            for (int j = 0; j < itemnames.Length; j++)
            {
                try
                {
                    GameObject clone = Jotunn.Managers.PrefabManager.Instance.CreateClonedPrefab(itemnames[j] + "_at", itemBaseMush);
                    //DBG.blogDebug("added " + itemnames[j] + "_at as a prefab");
                    var attachbase = clone.GetComponent<Transform>().Find("attach");
                    var attachcopy = AttachChild(tamefoodT1Transform, attachbase.transform.GetChild(0), itemnames[j]);
                    Transform cptrans = attachcopy.transform;
                    cptrans.localEulerAngles = itemrotations[j] + new Vector3(rot[j], 5 * rot[j], Math.Abs(rot[j]));
                    cptrans.localPosition = itemlocations[j];
                    MeshRenderer meshRen = attachcopy.GetComponent<MeshRenderer>();
                    if (meshRen != null) { meshRen.material.SetColor("_EmissionColor", new Color(0.8f + r[j], 0.5f + g[j], 0, 1)); }

                }
                catch
                {
                    Debug.LogWarning("Food Failed to add " + itemnames[j] + ", (" + j + ")");
                }
            }
            try
            {
                GameObject itemBaseFreeze = Jotunn.Managers.PrefabManager.Instance.GetPrefab("FreezeGland");
                GameObject clone = Jotunn.Managers.PrefabManager.Instance.CreateClonedPrefab("FreezeGland" + "_at", itemBaseFreeze);
                //DBG.blogDebug("added FreezeGland_at as a prefab");
                var attachbase = clone.GetComponent<Transform>().Find("attach");
                var attachcopy = AttachChild(tamefoodT1Transform, attachbase.transform.GetChild(1), "FreezeGland");

                Transform cptrans = attachcopy.transform;
                cptrans.localPosition = (new Vector3(0, 0, 0));
                cptrans.localEulerAngles = (new Vector3(0, 0, 0));
                cptrans.localScale = new Vector3(0.5f, 0.3f, 0.4f);

                MeshRenderer meshRen = attachcopy.GetComponent<MeshRenderer>();
                if (meshRen != null) { meshRen.material.SetColor("_EmissionColor", Color.magenta); }

                for (int i = 0; i < attachcopy.childCount; i++)
                {
                    //DBG.blogDebug("child#" + i + "=" + attachcopy.transform.GetChild(i).name);
                    Light lightToDestroy = attachcopy.transform.GetChild(i).GetComponent<Light>();
                    if (lightToDestroy != null) { lightToDestroy.transform.parent = null; }
                }
            }
            catch
            {
                Debug.LogWarning("Food Failed to add FreezeGland");
            }

            //DBG.blogDebug("tamefoodT1Transform.GetChild(0).childCount=" + tamefoodT1Transform.childCount);
            //DBG.blogDebug("tamefoodT1Transform.name=" + tamefoodT1Transform.name);
            for (int i = 0; i < tamefoodT1Transform.childCount; i++)
            {
                //DBG.blogDebug("child#" + i + "=" + tamefoodT1Transform.GetChild(i).name);
                Light lightTochange = tamefoodT1Transform.GetChild(i).GetComponent<Light>();
                if (lightTochange != null)
                {
                    lightTochange.color = Color.red;
                }
            }

            GameObject itemBaseStaff = Jotunn.Managers.PrefabManager.Instance.GetPrefab("DvergerStaffFire");
            GameObject clone_staff = Jotunn.Managers.PrefabManager.Instance.CreateClonedPrefab("DvergerStaffFire" + "_at", itemBaseStaff);
            var flare = AttachChild(tamefoodT1Transform, clone_staff.transform.GetChild(1).GetChild(0), "Flare");

            ParticleSystem flarePart = flare.GetComponent<ParticleSystem>();
            if (flarePart != null)
            {
                flarePart.startSize = 1.1f;
            }

            var sparks = AttachChild(tamefoodT1Transform, clone_staff.transform.GetChild(1).GetChild(1), "Sparks");

            ParticleSystem sparkPart = sparks.GetComponent<ParticleSystem>();
            if (sparkPart != null)
            {
                sparkPart.startLifetime = 0.4f;
                sparkPart.startSpeed = 0.05f;
                sparkPart.emissionRate = 3;
            }
            sparks.localScale = new Vector3(0.6f, 0.6f, 0.6f);

            try
            {
                GameObject clone = Jotunn.Managers.PrefabManager.Instance.CreateClonedPrefab("MushroomYellow" + "_at", itemBaseMush);
                var attachbase = clone.GetComponent<Transform>().Find("attach");
                var attachcopy = AttachChild(tamefoodT1Transform, attachbase.transform.GetChild(0), "MushroomYellow");
                Transform cptrans = attachcopy.transform;
                cptrans.localPosition = new Vector3(-0.01f, 0.02f, 0);
                cptrans.localEulerAngles = new Vector3(0, 0, 180);
                cptrans.localScale = new Vector3(0.65f, 0.8f, 0.6f);

                MeshRenderer meshRen = attachcopy.GetComponent<MeshRenderer>();
                if (meshRen != null) { meshRen.material.SetColor("_EmissionColor", new Color(0.8f, 0.72f, 0, 0)); }

            }
            catch
            {
                Debug.LogWarning("Food Failed to add " + "MushroomYellow");
            }
        }

            
        

        public static Transform AttachChild(Transform trans, Transform attachbase, string name)
        {
            //var attachbase = clone.GetComponent<Transform>().Find("attach");
            attachbase.name = name + "_at";
            var attachcopy = CopyIntoParent(attachbase, trans);
            var Lod = attachcopy.GetComponent<LODGroup>();
            if (Lod != null) { Destroy(Lod); }
            Lod = attachcopy.GetComponentInChildren<LODGroup>();
            if (Lod != null) { Destroy(Lod); }
            var ZSync = attachcopy.GetComponent<ZSyncTransform>();
            if (ZSync != null) { Destroy(ZSync); }
            ZSync = attachcopy.GetComponentInChildren<ZSyncTransform>();
            if (ZSync != null) { Destroy(ZSync); }
            return attachcopy;
        }

        public static T CopyIntoParent<T>(T go, T parent) where T : Component
        {
            var CompCopy = InstantiatePrefab.Instantiate(go);
            CompCopy.name = go.name;
            CompCopy.transform.parent = parent.transform;
            CompCopy.transform.localPosition = new Vector3(0, 0, 0);
            return CompCopy;
        }
        /*
        public static void RecipeReg()
        {
            CustomRecipe tamingstickRecipe = new CustomRecipe(new RecipeConfig()
            {
                Item = tametoolname,                    // Name of the item prefab to be crafted
                Requirements = new RequirementConfig[]  // Resources and amount needed for it to be crafted
				{
                new RequirementConfig { Item = "RawMeat", Amount = 1 },
                new RequirementConfig { Item = "Mushroom", Amount = 1 },
                new RequirementConfig { Item = "Carrot", Amount = 1 },
                new RequirementConfig { Item = "Resin", Amount = 3 }
                }
            });
            ItemManager.Instance.AddRecipe(tamingstickRecipe);
        }
        */

        
        private void EffectReg()
        {
            //ZNetScene zns = ZNetScene.instance;
            GameObject prefab = Jotunn.Managers.PrefabManager.Instance.GetPrefab("MeadHealthMedium");

            StatusEffect baseEffect = prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_consumeStatusEffect;

            effect.name = "SE_SmallHealth";
            effect.m_ttl = 20f;

            GameObject effect_prefab = UnityEngine.Object.Instantiate(baseEffect.m_startEffects.m_effectPrefabs[0].m_prefab, PetManager.Root.transform);
            //GameObject effect_prefab = baseEffect.m_startEffects.m_effectPrefabs[0].m_prefab;
            Jotunn.Managers.PrefabManager.Instance.RegisterToZNetScene(effect_prefab);
            //DBG.blogDebug("instaniated new object");
            effect_prefab.name = "Effect_SmallHealth";
            Vector3 scale = new Vector3(0.5f, 0.3f, 0.5f);
            Vector3 offset = new Vector3(0f, -0.2f, 0f);

            for (int i = 0; i < effect_prefab.transform.childCount; i++)
            {
                Transform ch_trans = effect_prefab.transform.GetChild(i);
                ch_trans.localScale = Vector3.Scale(ch_trans.localScale, scale); //(side,vertical,forward)
                ch_trans.localPosition = ch_trans.localPosition + offset;
            }

            //point light
            try
            {
                GameObject pointlight_go = effect_prefab.transform.Find("Point light").gameObject;
                Light light = pointlight_go.GetComponent<Light>();
                light.color = Color.green;
            }
            catch
            {
                DBG.blogDebug("failed point light");
            }

            //trails
            try
            {
                GameObject trails_go = effect_prefab.transform.Find("trails").gameObject;
                ParticleSystem ps_trails = trails_go.GetComponent<ParticleSystem>();
                ps_trails.startColor = Color.green;
                ps_trails.gravityModifier = -0.0002f;
            }
            catch
            {
                DBG.blogDebug("failed trails");
            }
            //flare
            try
            {
                GameObject flare_go = effect_prefab.transform.Find("flare").gameObject;
                ParticleSystem ps_flare = flare_go.GetComponent<ParticleSystem>();
                ps_flare.transform.parent = null;
            }
            catch
            {
                DBG.blogDebug("failed flare");
            }

            effect.m_startEffects.m_effectPrefabs = new EffectList.EffectData[1];
            effect.m_startEffects.m_effectPrefabs[0] = new EffectList.EffectData();
            EffectList.EffectData tempeffect = effect.m_startEffects.m_effectPrefabs[0];
            tempeffect.m_attach = true;
            tempeffect.m_inheritParentRotation = true;
            tempeffect.m_inheritParentScale = true;
            tempeffect.m_scale = true;
            effect.m_startEffects.m_effectPrefabs[0].m_prefab = effect_prefab;

        }

    }
}
