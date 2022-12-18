using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace AllTameable
{
    public class PrefabManager : MonoBehaviour
    {
        public static string tametoolname = Plugin.tamingtoolPrefabName;
        public StatusEffect effect = ScriptableObject.CreateInstance<StatusEffect>();
        public GameObject Root;


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
        }

        public static void ItemReg()
        {

            AssetBundle embeddedResourceBundle = AssetUtils.LoadAssetBundleFromResources("taming_tool", Assembly.GetExecutingAssembly());
            Object[] iconsprites = embeddedResourceBundle.LoadAssetWithSubAssets("Assets/CustomItems/tamingtool.png");
            DBG.blogDebug("Adding recipe for TamingStick");
            ItemConfig tamestickConfig = new ItemConfig();
            tamestickConfig.AddRequirement(new RequirementConfig("RawMeat", 1));
            tamestickConfig.AddRequirement(new RequirementConfig("Mushroom", 1));
            tamestickConfig.AddRequirement(new RequirementConfig("Carrot", 1));
            tamestickConfig.AddRequirement(new RequirementConfig("Resin", 1));

            CustomItem tamestick = new CustomItem(tametoolname, "Club",tamestickConfig);
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
                    attachbase.name = attachbase.name + "_" + itemname;
                    var attachcopy = CopyIntoParent(attachbase, tameTransform);
                    var Lod = attachcopy.GetComponent<LODGroup>();
                    if (Lod != null) { Destroy(Lod); }
                    Lod = attachcopy.GetComponentInChildren<LODGroup>();
                    if (Lod != null) { Destroy(Lod); }
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

            //RecipeReg();
            
            GameObject prefab = Jotunn.Managers.PrefabManager.Instance.GetPrefab("MeadHealthMedium");
            GameObject go = Jotunn.Managers.PrefabManager.Instance.CreateClonedPrefab("AT_HealthEffect", prefab);

            Plugin.prefabManager.EffectReg();

            Jotunn.Managers.PrefabManager.OnVanillaPrefabsAvailable -= PrefabManager.ItemReg;

        }
        public static T CopyIntoParent<T>(T go, T parent) where T : Component
        {
            var CompCopy = InstantiatePrefab.Instantiate(go);
            CompCopy.name = go.name;
            CompCopy.transform.parent = parent.transform;
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

            GameObject effect_prefab = Object.Instantiate(baseEffect.m_startEffects.m_effectPrefabs[0].m_prefab, PetManager.Root.transform);
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
