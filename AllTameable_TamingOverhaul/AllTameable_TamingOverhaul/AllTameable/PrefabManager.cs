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
        public static string t1foodname = Plugin.t1foodPrefabName;
        public static string t2foodname = Plugin.t2foodPrefabName;
        public StatusEffect effect = ScriptableObject.CreateInstance<StatusEffect>();
        public GameObject Root;
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

        public static void ItemReg()
        {
            addTameStick();
            addT1TameFood();
            addT2TameFood();

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
            tamestickConfig.AddRequirement(new RequirementConfig("Resin", 1));

            CustomItem tamestick = new CustomItem(tametoolname, "Club", tamestickConfig);
            DBG.blogDebug("Adding TamingStick");
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
            //tamefoodT1Config.AddRequirement(new RequirementConfig("LeatherScraps", 1));

            CustomItem tamefood = new CustomItem(t2foodname, "Bread", tamefoodT2Config);
            ItemManager.Instance.AddItem(tamefood);
            Transform tamefoodTransform = Jotunn.Managers.PrefabManager.Instance.GetPrefab(t2foodname).transform.Find("attach").transform;
            var id_tamefood = tamefood.ItemDrop;
            var id2_tamefood = id_tamefood.m_itemData;
            id2_tamefood.m_shared.m_name = "Superior Taming Food";
            id2_tamefood.m_shared.m_description = "Food that can be fed to creatures you are attempting to tame to give a reasonable reduction in taming time";
            id2_tamefood.m_shared.m_icons[0] = iconspritesfoodT2[1] as Sprite;
            GameObject itemBaseMush = Jotunn.Managers.PrefabManager.Instance.GetPrefab("DragonTear");

            try
            {
                GameObject itemBase = Jotunn.Managers.PrefabManager.Instance.GetPrefab("DragonTear");
                GameObject clone = Jotunn.Managers.PrefabManager.Instance.CreateClonedPrefab("DragonTear" + "_at", itemBaseMush);
                DBG.blogDebug("added " + clone.name + " as a prefab");
                var attachbase = clone.GetComponent<Transform>().Find("attach");
                for (int i = 0; i < attachbase.childCount; i++)
                {
                    DBG.blogDebug("T2child#" + i + "=" + attachbase.transform.GetChild(i).name);
                    var attachcopy =AttachChild(tamefoodTransform, attachbase.transform.GetChild(i), "DragonTear");
                    switch (attachcopy.name)
                    {
                        case "flare_at":
                            DBG.blogDebug("In flare case");
                            ParticleSystem flarePart = attachcopy.GetComponent<ParticleSystem>();
                            if (flarePart != null)
                            {
                                flarePart.startSize = 2f;
                                flarePart.startColor = new Color(1, 0.28f, 0.28f, 0.05f);
                            }
                            break;
                        case "smoke_expl_at":
                            DBG.blogDebug("In smoke_expl_at case");
                            ParticleSystem smokePart = attachcopy.GetComponent<ParticleSystem>();
                            if (smokePart != null)
                            {
                                //smokePart.startSize = 1.6f;
                                smokePart.startColor = new Color(1, 0.55f, 0, 0.04f);
                                smokePart.startLifetime = 1;
                            }
                            break;
                        case "pixel flakes_at":
                            DBG.blogDebug("In pixel flakes_at case");
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
                            DBG.blogDebug("In model_at case");
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
                            DBG.blogDebug("In Point light_at case");
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
                DBG.blogDebug("i=" + i);
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



            DBG.blogDebug("Adding T1TameFood");
            DBG.blogDebug("itemnames.Length=" + itemnames.Length);
            DBG.blogDebug("itemnames=" + itemnames);
            DBG.blogDebug("itemnames.ToString()=" + itemnames.ToString());
            GameObject itemBaseMush = Jotunn.Managers.PrefabManager.Instance.GetPrefab("MushroomYellow");
            for (int j = 0; j < itemnames.Length; j++)
            {
                try
                {
                    GameObject clone = Jotunn.Managers.PrefabManager.Instance.CreateClonedPrefab(itemnames[j] + "_at", itemBaseMush);
                    DBG.blogDebug("added " + itemnames[j] + "_at as a prefab");
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
                DBG.blogDebug("added FreezeGland_at as a prefab");
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
                    DBG.blogDebug("child#" + i + "=" + attachcopy.transform.GetChild(i).name);
                    Light lightToDestroy = attachcopy.transform.GetChild(i).GetComponent<Light>();
                    if (lightToDestroy != null) { lightToDestroy.transform.parent = null; }
                }
            }
            catch
            {
                Debug.LogWarning("Food Failed to add FreezeGland");
            }

            DBG.blogDebug("tamefoodT1Transform.GetChild(0).childCount=" + tamefoodT1Transform.childCount);
            DBG.blogDebug("tamefoodT1Transform.name=" + tamefoodT1Transform.name);
            for (int i = 0; i < tamefoodT1Transform.childCount; i++)
            {
                DBG.blogDebug("child#" + i + "=" + tamefoodT1Transform.GetChild(i).name);
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
            attachbase.name = attachbase.name + "_at";
            var attachcopy = CopyIntoParent(attachbase, trans);
            var Lod = attachcopy.GetComponent<LODGroup>();
            if (Lod != null) { Destroy(Lod); }
            Lod = attachcopy.GetComponentInChildren<LODGroup>();
            if (Lod != null) { Destroy(Lod); }
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
