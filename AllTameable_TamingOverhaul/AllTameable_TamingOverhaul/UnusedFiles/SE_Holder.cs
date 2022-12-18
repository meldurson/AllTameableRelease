
using System.Reflection;
using UnityEngine;


namespace AllTameable
{
    public class SE_Holder : MonoBehaviour
    {
        public StatusEffect effect = ScriptableObject.CreateInstance<StatusEffect>();
        //public StatusEffect effect = new StatusEffect();

        private void Awake()
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
