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
                if (!thischar.name.Contains("SeekerBrood") | Plugin.SeekerBroodOffspring.Value == false)
                {
                    ParentStart();
                }

            }

        }

        public void SetGrow(Character growchar)
        {
            Character thischar = gameObject.GetComponent<Character>();
            if(thischar != null)
            {
                API.SetInfusionCreature(thischar, API.GetInfusionCreature(growchar));
                API.SetExtraEffectCreature(thischar, API.GetExtraEffectCreature(growchar));
            }
        }

        public void SetInfusion(Character mother)
        {
            CreatureInfusion mom_infusion = API.GetInfusionCreature(mother);
            int mom_lvl = mother.GetLevel();
            dad_lvl = mom_info.dad_lvl;
            dad_infusion = mom_info.dad_infusion;
            Character thisChar = gameObject.GetComponent<Character>();

            thisChar.m_level = Mathf.RoundToInt((mom_lvl + dad_lvl) / 2);
            int infusion_num = UnityEngine.Random.Range(0, 100);
            if (Plugin.AllowMutation.Value && (UnityEngine.Random.Range(0, 100) < (Plugin.MutationChance.Value)))
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

                if (infusion_num > 66)
                {
                    API.SetInfusionCreature(thisChar, dad_infusion);
                    //DBG.blogDebug("IsDadInf");
                }
                else if (infusion_num > 33)
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

        public void SetExtraEffect(Character mother)
        {
            CreatureExtraEffect mom_effect = API.GetExtraEffectCreature(mother);
            int mom_lvl = mother.GetLevel();
            dad_lvl = mom_info.dad_lvl;
            dad_effect = mom_info.dad_effect;
            Character thisChar = gameObject.GetComponent<Character>();

            thisChar.m_level = Mathf.RoundToInt((mom_lvl + dad_lvl) / 2);
            //DBG.blogDebug("EffEnabled");
            int effect_num = UnityEngine.Random.Range(0, 100);
            //DBG.blogDebug("Effect number is=" + effect_num);
            if (Plugin.AllowMutation.Value && (UnityEngine.Random.Range(0, 100) < (Plugin.MutationChance.Value)))
            {


                for (int i = 0; i < 25; i++)
                {
                    API.SetExtraEffectCreature(thisChar);
                    CreatureExtraEffect thisEffect = API.GetExtraEffectCreature(thisChar);
                    //DBG.blogDebug("Effect(" + i + ")=" + thisEffect.ToString());
                    if (thisEffect != mom_effect && thisEffect != dad_effect && thisEffect != CreatureExtraEffect.None)
                    {
                        //DBG.blogDebug("i=" + i);
                        DBG.blogDebug("Has Mutation in Effect");
                        break;
                    }
                }

            }
            else if (mom_effect != CreatureExtraEffect.None || dad_effect != CreatureExtraEffect.None)
            {

                if (effect_num > 66)
                {
                    API.SetExtraEffectCreature(thisChar, dad_effect);
                    //DBG.blogDebug("IsDadEff");
                }
                else if (effect_num > 33)
                {
                    API.SetExtraEffectCreature(thisChar, mom_effect);
                    //DBG.blogDebug("IsMomEff");
                }
                else
                {
                    API.SetExtraEffectCreature(thisChar, CreatureExtraEffect.None);
                    //DBG.blogDebug("IsNoEff");
                }

            }
            else
            {
                //DBG.blogDebug("Parents have no infusion");
                API.SetExtraEffectCreature(thisChar, CreatureExtraEffect.None);
            }
        }

        public void SetCreature(Character mother)
        {
            //try
            //{
            mom_info = mother.gameObject.GetComponent<ProcreationInfo>();
            mom_info.ParentStart();

            CreatureExtraEffect mom_effect = API.GetExtraEffectCreature(mother);
            CreatureInfusion mom_infusion = API.GetInfusionCreature(mother);
            //ProcreationInfo mom_info = mother.gameObject.GetComponent<ProcreationInfo>();
            int mom_lvl = mother.GetLevel();

            dad_lvl = mom_info.dad_lvl;
            dad_infusion = mom_info.dad_infusion;
            dad_effect = mom_info.dad_effect;

            //Test Level
            level = mom_lvl;
            if (Plugin.AllowMutation.Value && (UnityEngine.Random.Range(0, 100) < Plugin.MutationChance.Value))
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

            if (API.IsInfusionEnabled())
            {
                SetInfusion(mother);
            }
            else
            {
                DBG.blogDebug("NoinfusionEnabled");
            }

            if (API.IsExtraEffectEnabled())
            {
                SetExtraEffect(mother);
            }
            else
            {
                DBG.blogDebug("NoExtraEffectEnabled");
            }

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
            //DBG.blogDebug("dad_lvl =" + dad_lvl);
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
                SetInfo(partner.GetComponent<Character>());
            }
            else
            {
                dad_lvl = gameObject.GetComponent<Character>().GetLevel();
            }
        }
    }
}
