using CreatureLevelControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace AllTameable.CLLC
{
    public class ProcreationInfo : MonoBehaviour
    {
        public int dad_lvl = 1;
        public CreatureExtraEffect dad_effect { get; set; } = CreatureExtraEffect.None;
        public CreatureInfusion dad_infusion { get; set; } = CreatureInfusion.None;
        public ProcreationInfo mom_info { get; set; } = null;
        private int level = 1;
        private bool newLevel = false;


        public int GetLevel(int oldLevel)
        {
            if (newLevel)
            {
                return level;
            }
            return oldLevel;
        }

        private void Awake()
        {
            if (!GetComponentInParent<Growup>() & !GetComponentInParent<EggGrow>())
            {
                Character thischar = gameObject.GetComponent<Character>();
                if ((bool)thischar)
                {
                    ParentStart();
                }
                
            }
        }

        public void SetGrow(Character adultchar)
        {
            DBG.blogDebug("inSetGrow");
            Character thischar = gameObject.GetComponent<Character>();
            CreatureInfusion thisInf;
            CreatureExtraEffect thisEff;
            if (thischar != null)
            {
                DBG.blogDebug("Has Char");
                thisInf = API.GetInfusionCreature(thischar);
                thisEff = API.GetExtraEffectCreature(thischar);
            }
            else
            {
                DBG.blogDebug("No Char");
                ItemDrop iDrop = gameObject.GetComponent<ItemDrop>();
                thisEff = iDropGetEff(iDrop);
                thisInf = iDropGetInf(iDrop);
            }
            API.SetInfusionCreature(adultchar, thisInf);
            API.SetExtraEffectCreature(adultchar, thisEff);

        }

        public void SetInfusion(Character mother, bool isEgg)
        {
            CreatureInfusion mom_infusion = API.GetInfusionCreature(mother);
            int mom_lvl = mother.GetLevel();
            dad_lvl = mom_info.dad_lvl;
            dad_infusion = mom_info.dad_infusion;
            Character thisChar = gameObject.GetComponent<Character>();

            thisChar.m_level = Mathf.RoundToInt((mom_lvl + dad_lvl) / 2);
            int infusion_num = UnityEngine.Random.Range(0, 100);
            if (Plugin.AllowMutation.Value && (UnityEngine.Random.Range(0, 100) < (Plugin.MutationChanceInf.Value)))
            {


                for (int i = 0; i < 25; i++)
                {
                    API.SetInfusionCreature(thisChar);
                    CreatureInfusion thisInfusion = API.GetInfusionCreature(thisChar);
                    if (thisInfusion != mom_infusion && thisInfusion != dad_infusion && thisInfusion != CreatureInfusion.None)
                    {
                        DBG.blogDebug("Has Mutation in Infusion");
                        break;
                    }
                }

            }
            else if (mom_infusion != CreatureInfusion.None || dad_infusion != CreatureInfusion.None)
            {

                if (infusion_num > 60)
                {
                    API.SetInfusionCreature(thisChar, dad_infusion);
                    //DBG.blogDebug("IsDadInf");
                }
                else if (infusion_num > 20)
                {
                    API.SetInfusionCreature(thisChar, mom_infusion);
                    //DBG.blogDebug("IsMomInf");
                }
                else
                {
                    API.SetInfusionCreature(thisChar, CreatureInfusion.None);
                    //DBG.blogDebug("IsNoInf");
                }

            }
            else
            {
                //DBG.blogDebug("Parents have no infusion");
                API.SetInfusionCreature(thisChar, CreatureInfusion.None);
            }
        }

        private CreatureExtraEffect TryGetExtraEffect(Character thisChar)
        {
            if ((bool)thisChar.m_nview)
            {
                return API.GetExtraEffectCreature(thisChar);
            }
            string EffectStr = thisChar.gameObject.GetComponent<ItemDrop>().m_itemData.m_customData["ExtraEffect"];
            if(!Enum.TryParse(EffectStr, out CreatureExtraEffect effect))
            {
                effect = CreatureExtraEffect.None;
            }
            return effect;
        }

        private CreatureInfusion TryGetInfusion(Character thisChar)
        {
            if ((bool)thisChar.m_nview)
            {
                return API.GetInfusionCreature(thisChar);
            }
            string EffectStr = thisChar.gameObject.GetComponent<ItemDrop>().m_itemData.m_customData["Infusion"];
            if (!Enum.TryParse(EffectStr, out CreatureInfusion inf))
            {
                inf = CreatureInfusion.None;
            }
            return inf;
        }

        public void SetExtraEffect(Character mother, bool isEgg)
        {
            CreatureExtraEffect mom_effect = API.GetExtraEffectCreature(mother);
            int mom_lvl = mother.GetLevel();
            dad_lvl = mom_info.dad_lvl;
            dad_effect = mom_info.dad_effect;
            Character thisChar;
            if (isEgg)
            {
                DBG.blogDebug("SetEffect of Egg");
                GameObject GrowUp_go = UnityEngine.Object.Instantiate(gameObject.GetComponent<EggGrow>().m_grownPrefab, PetManager.Root.transform);
                thisChar = GrowUp_go.GetComponent<Character>();
                DBG.blogDebug("Clone Char of Egg="+(bool)thisChar);
                ZNetView m_nview = thisChar.GetComponent<ZNetView>();
                if (m_nview.IsValid() && m_nview.IsOwner())
                {
                    DBG.blogDebug("ZNet valid and ownder");
                    //m_nview.GetZDO()
                }
                else
                {
                    DBG.blogDebug("ZNet invalid or not owner");
                    if (!m_nview.IsValid()) { DBG.blogDebug("ZNet invalid"); }
                    if (!m_nview.IsOwner()) { DBG.blogDebug("ZNet now towner"); }
                    thisChar.gameObject.AddComponent<ItemDrop>();
                    DBG.blogDebug("added item Drop");
                }
            }
            else
            {
                thisChar = gameObject.GetComponent<Character>();
            }

            thisChar.m_level = Mathf.RoundToInt((mom_lvl + dad_lvl) / 2);
            //DBG.blogDebug("EffEnabled");
            int effect_num = UnityEngine.Random.Range(0, 100);
            //DBG.blogDebug("Effect number is=" + effect_num);
            CreatureExtraEffect effectToSet = CreatureExtraEffect.None;
            if (Plugin.AllowMutation.Value && (UnityEngine.Random.Range(0, 100) < (Plugin.MutationChanceEff.Value)))
            {
                DBG.blogDebug("is in Mutation of Effect");
                DBG.blogDebug("Clone Char of Egg=" + (bool)thisChar);
                for (int i = 0; i < 25; i++)
                {
                    API.SetExtraEffectCreature(thisChar);
                    CreatureExtraEffect thisEffect = TryGetExtraEffect(thisChar);
                    DBG.blogDebug("Effect(" + i + ")=" + thisEffect.ToString());
                    if (thisEffect != mom_effect && thisEffect != dad_effect && thisEffect != CreatureExtraEffect.None)
                    {
                        DBG.blogDebug("i=" + i);
                        DBG.blogDebug("Has Mutation in Effect");
                        effectToSet = thisEffect;
                        break;
                    }
                }
            }
            else
            {
                if (mom_effect != CreatureExtraEffect.None || dad_effect != CreatureExtraEffect.None)
                {
                    if (effect_num > 60)
                    {
                        effectToSet = dad_effect;
                        DBG.blogDebug("IsDadEff");
                    }
                    else if (effect_num > 20)
                    {
                        effectToSet = mom_effect;
                        DBG.blogDebug("IsMomEff");
                    }
                    else
                    {
                        DBG.blogDebug("IsNoEff");
                    }
                }
                
            }
            if (!isEgg) { API.SetExtraEffectCreature(thisChar, effectToSet); }
            else
            {
                DBG.blogDebug("Trying Set Effect ZDO");
                DBG.blogDebug("Effect=" + effectToSet);
                ItemDrop iDrop = gameObject.GetComponent<ItemDrop>();
                ItemDrop.ItemData iData = iDrop.m_itemData;
                Utils2.addOrUpdateCustomData(iData.m_customData, "ExtraEffect", effectToSet.ToString());
                //iData.m_customData.Add("ExtraEffect", effectToSet.ToString());
                iDrop.Save();
                /*
                ZNetView znv = gameObject.GetComponent<ZNetView>();
                DBG.blogDebug("ZNetView=" + (bool)znv);
                if (znv.IsValid())
                {
                    DBG.blogDebug("ZDOSet Effect Register");
                    //znv.Register<ZDOID, CreatureExtraEffect>("ZDOSetEffect", ZDOSetEff);
                }
                znv.GetZDO().Set("ExtraEffect", (int)effectToSet);
                */
                DBG.blogDebug("Set Effect ZDO");
            }


        }


        public void SetInfusionExtraEffect(Character mother, bool isEgg)
        {
            DBG.blogDebug("In infusion effect combine");
            bool setInfusion = false;
            bool setEffect = false;
            if (API.IsExtraEffectEnabled())
            {setEffect = true; }
            else{DBG.blogDebug("NoExtraEffectEnabled");}

            if (API.IsInfusionEnabled())
            {setInfusion = true;}
            else{ DBG.blogDebug("NoinfusionEnabled");}

            if(!setInfusion && !setEffect) { return; }

            CreatureExtraEffect mom_effect = API.GetExtraEffectCreature(mother);
            CreatureInfusion mom_infusion = API.GetInfusionCreature(mother);
            int mom_lvl = mother.GetLevel();
            dad_lvl = mom_info.dad_lvl;
            dad_effect = mom_info.dad_effect;
            dad_infusion = mom_info.dad_infusion;
            Character thisChar;
            if (isEgg)
            {
                DBG.blogDebug("Set Infusion/Effect of Egg");
                GameObject GrowUp_go = UnityEngine.Object.Instantiate(gameObject.GetComponent<EggGrow>().m_grownPrefab, PetManager.Root.transform);
                thisChar = GrowUp_go.GetComponent<Character>();
                DBG.blogDebug("Clone Char of Egg=" + (bool)thisChar);
                ZNetView m_nview = thisChar.GetComponent<ZNetView>();
                if (!m_nview.IsValid() || !m_nview.IsOwner())
                {
                    thisChar.gameObject.AddComponent<ItemDrop>();
                }
            }
            else
            {
                thisChar = gameObject.GetComponent<Character>();
            }

            thisChar.m_level = Mathf.RoundToInt((mom_lvl + dad_lvl) / 2);
            CreatureExtraEffect effectToSet = CreatureExtraEffect.None;
            CreatureInfusion InfToSet = CreatureInfusion.None;
            int hasNewInfusion = -1;
            int hasNewEffect = -1;
            if (Plugin.AllowMutation.Value)
            {
                if(setEffect && UnityEngine.Random.Range(0, 100) < (Plugin.MutationChanceEff.Value)) 
                {
                    hasNewEffect = 0; 
                }
                if (setInfusion && UnityEngine.Random.Range(0, 100) < (Plugin.MutationChanceInf.Value)) 
                {
                    hasNewInfusion = 0; 
                }

                for (int i = 0; i < 25; i++)
                {
                    if(hasNewEffect!=0 && hasNewInfusion!=0)
                    {
                        break;
                    }
                    DBG.blogDebug("i=" + i);
                    if (hasNewEffect==0)
                    {
                        API.SetExtraEffectCreature(thisChar);
                        CreatureExtraEffect thisEffect = TryGetExtraEffect(thisChar);
                        if (thisEffect != mom_effect && thisEffect != dad_effect && thisEffect != CreatureExtraEffect.None)
                        {
                            DBG.blogDebug("Effect(" + i + ")=" + thisEffect.ToString());
                            DBG.blogDebug("Has Mutation in Effect");
                            effectToSet = thisEffect;
                            hasNewEffect = 1;
                        }
                    }
                    if (hasNewInfusion==0)
                    {
                        API.SetInfusionCreature(thisChar);
                        CreatureInfusion thisInfusion = TryGetInfusion(thisChar);
                        if (thisInfusion != mom_infusion && thisInfusion != dad_infusion && thisInfusion != CreatureInfusion.None)
                        {
                            DBG.blogDebug("Infusion(" + i + ")=" + thisInfusion.ToString());
                            DBG.blogDebug("Has Mutation in Infusion");
                            InfToSet = thisInfusion;
                            hasNewInfusion = 1;
                        }
                    }

                }
            }

            if (hasNewEffect < 0)
            {
                DBG.blogDebug("inherit Effect");
                effectToSet = (CreatureExtraEffect)inheritValue((int)mom_effect, (int)dad_effect);
            }
            if (hasNewInfusion < 0)
            {
                DBG.blogDebug("inherit Infusion");
                InfToSet = (CreatureInfusion)inheritValue((int)mom_infusion, (int)dad_infusion);
            }

            if (!isEgg) 
            { 
                API.SetExtraEffectCreature(thisChar, effectToSet);
                API.SetInfusionCreature(thisChar, InfToSet);
            }
            else
            {

                DBG.blogDebug("Effect=" + effectToSet);
                DBG.blogDebug("Infusion=" + InfToSet);
                ItemDrop iDrop = gameObject.GetComponent<ItemDrop>();
                ItemDrop.ItemData iData = iDrop.m_itemData;
                Utils2.addOrUpdateCustomData(iData.m_customData, "ExtraEffect", effectToSet.ToString());
                Utils2.addOrUpdateCustomData(iData.m_customData, "Infusion", InfToSet.ToString());
                if (((int)effectToSet + (int)InfToSet) > 0)
                {
                    DBG.blogDebug("Set stack size 1");
                    iData.m_shared.m_maxStackSize = 1;
                }
                iDrop.Save();

            }


        }

        public int inheritValue(int mom_val,int dad_val)
        {
            if (mom_val != 0 || dad_val != 0)
            {
                int rand_num = UnityEngine.Random.Range(0, 100);
                if (rand_num > 60)
                {
                    DBG.blogDebug("IsDadVal");
                    return dad_val;
                }
                else if (rand_num > 20)
                {
                    DBG.blogDebug("IsMomVal");
                    return mom_val;
                }
                else
                {
                    DBG.blogDebug("IsNoVal");
                }
            }
            return 0;
        }









        public static CreatureExtraEffect iDropGetEff(ItemDrop iDrop)
        {
            CreatureExtraEffect effect = CreatureExtraEffect.None;
            if ((bool)iDrop)
            {
                DBG.blogDebug("Has iDrop");
                if (iDrop.m_itemData.m_customData.TryGetValue("ExtraEffect",out string effStr))
                {
                    DBG.blogDebug("Custom Value="+ effStr);
                    if (Enum.TryParse(effStr,out CreatureExtraEffect CreatureEff))
                    {
                        DBG.blogDebug("GotEffect=" + CreatureEff);
                        effect = CreatureEff;
                    }
                }
            }
            return effect;
        }

        public static CreatureInfusion iDropGetInf(ItemDrop iDrop)
        {
            CreatureInfusion Inf = CreatureInfusion.None;
            if ((bool)iDrop)
            {
                if (iDrop.m_itemData.m_customData.TryGetValue("Infusion", out string InfStr))
                {
                    DBG.blogDebug("Custom Value=" + InfStr);
                    if (Enum.TryParse(InfStr, out CreatureInfusion CreatureInf))
                    {
                        DBG.blogDebug("GotInfusion=" + CreatureInf);
                        Inf = CreatureInf;
                    }
                }
            }
            return Inf;
        }


        public static void ZDOSetEff(long sender, ZDOID characterID, CreatureExtraEffect effect)
        {
            ZNetView m_nview = ZNetScene.instance.GetComponent<ZNetView>();
            if (m_nview.IsValid() && m_nview.IsOwner())
            {
                m_nview.GetZDO().Set("ExtraEffect", (int)effect);
            }
        }

        public void ZDOSetInf(long sender, ZDOID characterID,CreatureInfusion infusion)
        {
            ZNetView m_nview = ZNetScene.instance.GetComponent<ZNetView>();
            if (m_nview.IsValid() && m_nview.IsOwner())
            {
                m_nview.GetZDO().Set("Infusion", (int)infusion);
            }
        }

        public static CreatureExtraEffect ZDOGetEff(ZNetView m_nview)
        {
            int effectnum = 0; //none
            if (m_nview.IsValid())
            {
                effectnum = m_nview.GetZDO().GetInt("ExtraEffect");
            }
            return (CreatureExtraEffect)effectnum;
        }

        public CreatureInfusion ZDOGetInfusion(ZNetView m_nview)
        {
            int infusionNum = 0; //none
            if (m_nview.IsValid())
            {
                infusionNum = m_nview.GetZDO().GetInt("Infusion");
            }
            return (CreatureInfusion)infusionNum;
        }




        public void SetCreature(Character mother)
        {
            //try
            //{
            if (!mother.gameObject.TryGetComponent<ProcreationInfo>(out var momProc))
            {
                //DBG.blogDebug("Adding Procinfo");
                momProc = mother.gameObject.AddComponent<ProcreationInfo>();
            }
            mom_info = momProc;
            mom_info.ParentStart();
            //DBG.blogDebug("Parent started");
            CreatureExtraEffect mom_effect = API.GetExtraEffectCreature(mother);
            CreatureInfusion mom_infusion = API.GetInfusionCreature(mother);
            //ProcreationInfo mom_info = mother.gameObject.GetComponent<ProcreationInfo>();
            int mom_lvl = mother.GetLevel();

            dad_lvl = mom_info.dad_lvl;
            dad_infusion = mom_info.dad_infusion;
            dad_effect = mom_info.dad_effect;

            //Test Level
            level = mom_lvl;
            //DBG.blogDebug("level="+ level);
            //DBG.blogDebug("dadlevel=" + dad_lvl);
            //DBG.blogDebug("dad_infusion=" + dad_infusion);
            //DBG.blogDebug("dad_effect=" + dad_effect);
            //DBG.blogDebug("mom_effect=" + mom_effect.ToString());
            //DBG.blogDebug("mom_infusion=" + mom_infusion);

            if (Plugin.AllowMutation.Value && (UnityEngine.Random.Range(0, 100) < Plugin.MutationChanceLvl.Value))
            {
                DBG.blogDebug("Has Mutation in Level");
                int min_lvl = Mathf.Max(Mathf.Min(mom_lvl, dad_lvl) - 1, 1);
                int max_lvl = Mathf.Max(mom_lvl, dad_lvl) + 1;
                //DBG.blogDebug("min=" + min_lvl + ", max=" + max_lvl);
                int rndm = Mathf.Min(100, UnityEngine.Random.Range(0, 100) + 10);
                //DBG.blogDebug("level_min=" + min_lvl + ", rndm=" + rndm + ", float=" + (float)(((float)min_lvl +(((float)rndm * ((float)max_lvl - (float)min_lvl)) / 100f))));
                level = min_lvl + Mathf.RoundToInt(((float)(rndm * (max_lvl - min_lvl))) / 100f);

            }
            else if (UnityEngine.Random.Range(0, 100) > 50)
            {
                //DBG.blogDebug("IsDadLvl");
                level = dad_lvl;
            }
            else
            {
                //DBG.blogDebug("IsMomLvl");
            }
            newLevel = true;

            //Sets the level
            Character thischar = gameObject.GetComponent<Character>();
            bool isEgg = (bool)gameObject.GetComponent<EggGrow>();
            if (thischar != null)
            {
                thischar.SetLevel(level);
                thischar.SetTamed(true);
            }
            if (isEgg)
            {
                gameObject.GetComponent<ItemDrop>().SetQuality(level);
            }

            SetInfusionExtraEffect(mother, isEgg);


            /*


            if (API.IsExtraEffectEnabled())
            {
                //DBG.blogDebug("Setting Extra Effect");
                SetExtraEffect(mother, isEgg);
            }
            else
            {
                DBG.blogDebug("NoExtraEffectEnabled");
            }

            if (API.IsInfusionEnabled())
            {
                //DBG.blogDebug("Setting Infusion");
                SetInfusion(mother, isEgg);
            }
            else
            {
                DBG.blogDebug("NoinfusionEnabled");
            }
            */

            /*
        }
        catch
        {
            try
            {
                DBG.blogDebug("Major failure of procreation");
                level = mother.GetLevel();
            }
            catch
            {
                DBG.blogDebug("Catastrophic failure of procreation");
                level = 0;
            }
        }
            */

        }


        private void SetInfo(Character other)
        {
            dad_lvl = other.GetLevel();
            DBG.blogDebug("dad_lvl =" + dad_lvl);
            try
            {
                dad_effect = API.GetExtraEffectCreature(other);
            }
            catch { DBG.blogWarning("Error when trying to get Effect of Father"); }
            try
            {
                dad_infusion = API.GetInfusionCreature(other);
            }
            catch{DBG.blogWarning("Error when trying to get Infusion of Father");}
            

        }




        public void ParentStart()
        {
            Character partner = null;
            float num = 999999f;
            BaseAI baseai = GetComponentInParent<BaseAI>();
            List<Character> characters = Character.GetAllCharacters();
            //DBG.blogDebug("This char is:" + baseai.gameObject.name);
            //List<string> possiblemates = Plugin.CompatMatesList[baseai.gameObject.name];
            if (!Plugin.CompatMatesList.TryGetValue(Utils.GetPrefabName(baseai.gameObject), out var possiblemates))
            {
                possiblemates = new List<string> { Utils.GetPrefabName(baseai.gameObject) };
            }
            List<string> clonemates = new List<string>();
            ZNetScene zns = ZNetScene.instance;
            clonemates.Add(baseai.gameObject.name);
            foreach (string str in possiblemates)
            {
                clonemates.Add(str + "(Clone)");
            }
            // DBG.blogDebug("clonemates= " + string.Join(":", clonemates));

            foreach (Character character in characters)
            {
                if (!(character.gameObject == baseai.gameObject) && clonemates.Contains(character.gameObject.name) && character.GetComponent<ZNetView>().IsValid())// && !(Vector3.Distance(character.transform.position, base.transform.position) > 40))
                {

                    float num2 = Vector3.Distance(character.transform.position, base.transform.position);
                    if (num2 < num)
                    {
                        //DBG.blogDebug("character with name go:" + character.gameObject.name + " is " + Vector3.Distance(character.transform.position, base.transform.position) + "m away");
                        partner = character;
                        num = num2;
                    }

                }
                //if (clonemates.Contains(character.gameObject.name))
                //{
                //    DBG.blogDebug("found clone with name " + character.gameObject.name);
                //}
            }
            DBG.blogDebug("Partner with name go:" + partner.gameObject.name + " is " + Vector3.Distance(partner.transform.position, base.transform.position) + "m away");
            if (partner != null)
            {
                try
                {
                    SetInfo(partner.GetComponent<Character>());
                }
                catch { DBG.blogWarning("Error when trying to get Level of Father, Using Mothers"); }
            }
            else
            {
                dad_lvl = gameObject.GetComponent<Character>().GetLevel();
            }
        }
    }
}
