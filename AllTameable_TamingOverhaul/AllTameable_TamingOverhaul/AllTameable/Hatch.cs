using System;
using UnityEngine;

//using CreatureLevelControl;

namespace AllTameable
{
    public class Hatch : Growup, Hoverable, Interactable
    {
        public string m_name;

        private new ZNetView m_nview;

        private int growStats;

        private new void Start()
        {
            //DBG.blogDebug("Start");
            m_nview = base.gameObject.GetComponent<ZNetView>();
            InvokeRepeating("GrowUpdate", UnityEngine.Random.Range(10f, 15f), 10f);
            //DBG.blogDebug("Start2");
        }

        private new void GrowUpdate()
        {
            if (!m_nview.IsValid() || !m_nview.IsOwner())
            {
                DBG.blogDebug("Invalid Egg Growupdate");
                return;
            }
            growStats = Mathf.FloorToInt((float)(GetTimeSinceSpawned().TotalSeconds / (double)m_growTime * 100.0));
            //DBG.blogDebug("growstat=" + growStats);
            if (GetTimeSinceSpawned().TotalSeconds > (double)m_growTime)
            {
                GameObject hatchling = UnityEngine.Object.Instantiate(m_grownPrefab, base.transform.position + new Vector3(0f, 1f, 0f), base.transform.rotation);
                Tameable component = hatchling.GetComponent<Tameable>();
                Character hatch_char = hatchling.GetComponent<Character>();
                if ((bool)component)
                {
                    bool CLLCSucc = false;
                    try
                    {
                        if (Plugin.UseCLLC)
                        {
                            hatchling.GetComponent<Character>().SetLevel(CLLC.CLLC.Hatchlevel(hatch_char));
                            DBG.blogDebug("CLLC Level");
                            CLLCSucc = true;
                        }
                    }
                    catch { }
                    if (!CLLCSucc)
                    {
                        try
                        {

                            float[] lvlprob = ConvertConfigProbs();
                            hatchling.GetComponent<Character>().SetLevel(Utils2.LevelFromProb(lvlprob));
                            DBG.blogDebug("Config Level");
                        }
                        catch
                        {
                            DBG.blogDebug("Default Level");
                            float[] lvlprob = { 75, 20, 5 };
                            hatchling.GetComponent<Character>().SetLevel(Utils2.LevelFromProb(lvlprob));
                        }
                    }
                    component.Tame();
                }

                DBG.blogDebug("Destroying");
                m_nview.Destroy();
            }
        }

        public string GetHoverText()
        {
            string text = m_name;
            text += $"\nHatching Progress:<color=yellow><b>{growStats.ToString()}%</b></color>";
            text += "\n[<color=yellow><b>$KEY_Use</b></color>] to Cancel Hatching";
            return Localization.instance.Localize(text);
        }

        public string GetHoverName()
        {
            return "HatchingEgg";
        }

        private TimeSpan GetTimeSinceSpawned()
        {
            long num = m_nview.GetZDO().GetLong("spawntime", 0L);
            if (num == 0)
            {
                num = ZNet.instance.GetTime().Ticks;
                m_nview.GetZDO().Set("spawntime", num);
            }
            DateTime dateTime = new DateTime(num);
            return ZNet.instance.GetTime() - dateTime;
        }

        public bool Interact(Humanoid character, bool repeat, bool alt)
        {
            if (repeat)
            {
                return false;
            }
            //Vector3 eggpos = m_nview.transform.position;// + new Vector3(0f, 0.05f, 0f);
            //Quaternion eggrot = m_nview.transform.localRotation;
            //newpos.position+=new Vector3(0f, 2f, 0f);

            m_nview.Destroy();

            if (!m_nview.IsValid())
            {
                GameObject gameObject = UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("DragonEgg"), m_nview.transform.position, m_nview.transform.localRotation);
                gameObject.GetComponent<ItemDrop>().Pickup(character);
            }
            else
            {
                DBG.blogWarning("Failed to pickup Hatching Dragon Egg");
                DBG.blogWarning("Current Growth is " + Mathf.FloorToInt((float)(GetTimeSinceSpawned().TotalSeconds / (double)m_growTime * 100.0)) + "%");
            }
            
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData itdata)
        {
            return false;
        }

        public float[] ConvertConfigProbs()
        {
            string[] strArr = Plugin.LvlProb.Value.Split(',');
            float[] probArr = new float[strArr.Length];
            for (int i = 0; i < strArr.Length; i++)
            {
                probArr[i] = float.Parse(strArr[i]);
                //DBG.blogDebug("prob(" + i + ")=" + probArr[i]);
            }
            return probArr;
        }
    }
}
