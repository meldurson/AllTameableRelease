using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AllTameable
{
    //[HarmonyPatch]
    internal static class BetterTameHover
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character), "GetHoverText")]
        private static void GetHoverText(Character __instance, ref string __result)
        {
            if (Plugin.useTamingTool.Value)
            {
                
                Player plr = Player.m_localPlayer;
                //plr = ZNetScene.FindObjectOfType<Player>;
                if (plr.GetCurrentWeapon().m_dropPrefab == ZNetScene.instance.GetPrefab(Plugin.tamingtoolPrefabName))
                {
                    Tameable _tm = __instance.GetComponentInParent<Tameable>();
                    if (_tm != null)
                    {
                        MonsterAI _mAI;
                        Procreation _Proc;
                        _mAI = __instance.GetComponentInParent<MonsterAI>();
                        _Proc = __instance.GetComponentInParent<Procreation>();
                        string taming_text = "\n";
                        bool notend = true;
                        //bool crowded = false;
                        if (__instance.m_tamed)
                        {
                            if (ShowDebug(plr))
                            {
                                taming_text += ("Prefab is: " + __instance.name + ", is commandable= " + _tm.m_commandable + "\n").Replace("(Clone)", "");
                            }
                            if (_Proc != null)
                            {
                                if (_Proc.IsDue())
                                {
                                    taming_text += "Is Due ";
                                    notend = false;
                                }
                                else if (_Proc.IsPregnant())
                                {
                                    taming_text += "Is Pregnant";
                                    //long timepreg = _Proc.m_nview.GetZDO().GetLong("pregnant", 0L);
                                    DateTime dtime = new DateTime(_Proc.m_nview.GetZDO().GetLong("pregnant", 0L));
                                    double timeleft = (double)_Proc.m_pregnancyDuration - (ZNet.instance.GetTime() - dtime).TotalSeconds;
                                    if (timeleft > 0) { taming_text += ": " + (int)timeleft + "s left\n"; }
                                    notend = false;
                                }
                                else
                                {
                                    if (!_Proc.ReadyForProcreation())
                                    {
                                        taming_text += "Needs Food to Procreate";
                                    }
                                    else
                                    {
                                        if (ShowDebug(plr))
                                        {
                                            taming_text += GetPregStats(_Proc);
                                            __result = __result + taming_text;
                                            return;
                                        }
                                        else
                                        {
                                            int instnum = getInstNum(_Proc)[0];
                                            if (instnum != 0)
                                            {
                                                if (instnum == 1)
                                                {
                                                    taming_text += "Too Crowded\n";
                                                }
                                                else
                                                {
                                                    taming_text += "Needs Mate\n";
                                                }

                                            }


                                            if (_Proc.m_pregnancyChance < 1)
                                            {
                                                taming_text += "Ready to Procreate";
                                            }
                                            else
                                            {
                                                taming_text += "Pregnancy chance is too high: " + _Proc.m_pregnancyChance + "\n Check the config and reduce";

                                            }
                                        }

                                    }
                                }
                            }
                            else
                            {
                                taming_text += "Not able to Procreate";
                            }
                            if (notend)
                            {
                                taming_text += "\nPossible Consumables: ";
                            }


                        }
                        else //not tamed
                        {

                            if (__instance.gameObject.TryGetComponent<AllTame_Interactable>(out var ATI))
                            {
                                if (ATI != null)
                                {
                                    taming_text += "Trade to Recruit";
                                    taming_text += "\nPossible Trades: ";
                                    int line_len_trade = 20; // was 20
                                    foreach (KeyValuePair<string, int> item in ATI.tradelist)
                                    {

                                        taming_text = taming_text + item.Value + " " + item.Key + ", ";
                                        if (taming_text.Length - line_len_trade > 50)
                                        {
                                            line_len_trade = taming_text.Length;
                                            taming_text = taming_text + "\n";
                                        }
                                    }
                                    taming_text = taming_text.Remove(taming_text.Length - 2) + "\n";

                                }
                            }
                            if (_tm.m_tamingTime > 0)
                            {
                                taming_text += "Taming Time: ";
                                if (_tm.GetRemainingTime() != _tm.m_tamingTime)
                                {
                                    taming_text += _tm.GetRemainingTime() + "s\\";
                                }
                                taming_text += _tm.m_tamingTime + "s";
                                if (notend)
                                {
                                    taming_text += "\nPossible Consumables: ";
                                }
                            }
                            else
                            {
                                notend = false;
                            }

                        }


                        int line_len = 10; //default 20
                        if (notend)
                        {
                            foreach (ItemDrop item in _mAI.m_consumeItems)
                            {
                                taming_text = taming_text + item.name + ", ";
                                /*
                                    if (taming_text.Length - line_len > 150)
                                {
                                    line_len = taming_text.Length;  
                                    taming_text = taming_text + "\n";
                                }
                                */
                            }
                            foreach(string hiddenItem in Plugin.hidden_foodNames)
                            {
                                //DBG.blogDebug("hiddenItem=X"+hiddenItem + ", X");
                                //DBG.blogDebug("hiddenItem. contains="+taming_text.Contains(hiddenItem + ", "));
                                taming_text=taming_text.Replace(hiddenItem + ", ", "");
                            }
                            taming_text = taming_text.Remove(taming_text.Length - 2);
                            //DBG.blogDebug(taming_text);
                        }
                        __result = __result + taming_text;
                    }
                    else // not tameable
                    {
                        if (ShowDebug(plr))
                        {
                            string taming_text = "\n";
                            taming_text += "Not tameable \n";
                            taming_text += "Prefab name is: " + __instance.name;
                            taming_text = taming_text.Replace("(Clone)", "");
                            __result = __result + taming_text;
                        }
                    }

                }
                
            }
        }


        public static float max_interact_default = 6f;
        //public static float max_interact_recent = 6f; //implement if there is ever an issue with other interact lengths
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), "FindHoverObject")] //removes healing 0 text if healing less than 0.1hp
        private static void Prefix(Player __instance, GameObject hover, Character hoverCreature)
        {
            if (Plugin.useTamingTool.Value && (bool)hoverCreature)
            {
                //max_interact_recent = Player.m_localPlayer.m_maxInteractDistance;
                if (__instance.GetCurrentWeapon().m_dropPrefab == ZNetScene.instance.GetPrefab(Plugin.tamingtoolPrefabName))
                {
                    __instance.m_maxInteractDistance = 20f;
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), "FindHoverObject")] //adds effect around creature if healing
        private static void Postfix(Player __instance, ref GameObject hover, ref Character hoverCreature)
        {
            __instance.m_maxInteractDistance = max_interact_default;

            //if ((bool)hoverCreature) { DBG.blogDebug("char=" + hoverCreature.name); }
            //else { DBG.blogDebug("char=null2"); }
            /*
            max_interact_recent = (float)Math.Round((max_interact_default + max_interact_recent) / 2, 1);
            __instance.m_maxInteractDistance = max_interact_recent;
            */

        }

        public static bool ShowDebug(Player plr)
        {
            if (Plugin.debugout.Value && !plr.IsCrouching())
            {
                return true;
            }
            return false;

        }
        public static int[] getInstNum(Procreation _proc)
        {
            int[] return_arr = { 0, 0, 0, 0 };
            int nrOfInstances = -10;
            int nrOfInstances2 = -10;
            int valid_partners = 0;
            bool n_instcheck = true;
            try
            {
                if (!(_proc.m_myPrefab ?? false) | !(_proc.m_offspringPrefab ?? false)) //prefab is null
                {
                    DBG.blogDebug("Hover Initialised");
                    Genetics.Genetics.InitProcPrefabs(_proc);
                    
                }

                nrOfInstances = SpawnSystem.GetNrOfInstances(_proc.m_myPrefab, _proc.gameObject.transform.position, _proc.m_totalCheckRange);
                //DBG.blogDebug(nrOfInstances);
                nrOfInstances2 = SpawnSystem.GetNrOfInstances(_proc.m_offspringPrefab, _proc.gameObject.transform.position, _proc.m_totalCheckRange);
                //DBG.blogDebug("n1=" + nrOfInstances + ", n2= " + nrOfInstances2);
                //DBG.blogDebug("_proc.m_maxCreatures=" + _proc.m_maxCreatures);
                valid_partners = SpawnSystem.GetNrOfInstances(_proc.m_myPrefab, _proc.transform.position, _proc.m_partnerCheckRange, eventCreaturesOnly: false, procreationOnly: true);
                n_instcheck = nrOfInstances + nrOfInstances2 < _proc.m_maxCreatures && valid_partners >= 2;
            }
            catch (Exception e)
            {
                try
                {
                    nrOfInstances = Plugin.Safe_GetNrOfInstances(_proc.m_myPrefab, _proc.gameObject.transform.position, _proc.m_totalCheckRange,true);
                    //DBG.blogDebug(nrOfInstances);
                    nrOfInstances2 = Plugin.Safe_GetNrOfInstances(_proc.m_offspringPrefab, _proc.gameObject.transform.position, _proc.m_totalCheckRange,true);
                    //DBG.blogDebug("n1=" + nrOfInstances + ", n2= " + nrOfInstances2);
                    //DBG.blogDebug("_proc.m_maxCreatures=" + _proc.m_maxCreatures);
                    valid_partners = Plugin.Safe_GetNrOfInstances(_proc.m_myPrefab, _proc.transform.position, _proc.m_partnerCheckRange, true, eventCreaturesOnly: false, procreationOnly: true);
                    n_instcheck = nrOfInstances + nrOfInstances2 < _proc.m_maxCreatures && valid_partners >= 2;
                }
                catch
                {
                    DBG.blogDebug("Error: " + e.Message);
                    DBG.blogDebug("Error: " + e.StackTrace);
                    return_arr[0] = -1;

                    return return_arr; //error
                }
                
            }
            return_arr[1] = nrOfInstances;
            return_arr[2] = nrOfInstances2;
            return_arr[3] = valid_partners;
            if (n_instcheck == true)
            {
                //DBG.blogDebug("Less than max mates=" + nrOfInstances + ", offspring=" + nrOfInstances2 + ", max=" + _proc.m_maxCreatures);
                return_arr[0] = 0;
                
                return return_arr; //true
            }
            else if (nrOfInstances + nrOfInstances2 > _proc.m_maxCreatures-1)
            {
                //DBG.blogDebug("Too many mates="+ nrOfInstances+", offspring="+ nrOfInstances2 +", max="+ _proc.m_maxCreatures);
                return_arr[0] = 1;
                return return_arr;//Too crowded
            }
            else
            {
                //DBG.blogDebug("Not enough mates=" + nrOfInstances + ", offspring=" + nrOfInstances2 + ", max=" + _proc.m_maxCreatures);
                return_arr[0] = 2;
                return return_arr; //Needs Mate
            }
                    
            
        }



        public static string GetPregStats(Procreation _proc)
        {
            string ret_string = "";
            bool isvalid = !_proc.m_nview.IsValid() || !_proc.m_nview.IsOwner() || !_proc.m_character.IsTamed();
            //ret_string += "\nPrefab Name: " + _proc.gameObject.name.Replace("(Clone)","");
            ret_string += "isValid: " + !isvalid;
            //ret_string += "\nisNotPreg: " + !_proc.IsPregnant();
            ret_string += "\nPregchance= " + _proc.m_pregnancyChance;
            ret_string += "\nIsNotAlerted= " + !_proc.m_baseAI.IsAlerted();
            ret_string += "\nIsNotHungry= " + !_proc.m_tameable.IsHungry();
            ret_string += "\nCheckRandomVal: " + (UnityEngine.Random.value > _proc.m_pregnancyChance) + "<-if not steady False, then valid";
            bool ispregchance = UnityEngine.Random.value <= _proc.m_pregnancyChance || _proc.m_baseAI.IsAlerted() || _proc.m_tameable.IsHungry();
            ret_string += "\nislessThanPregchance: " + !ispregchance + "<-if not steady False, then valid";

            int[] inst_arr = getInstNum(_proc);
            int instcheck = inst_arr[0];
            if (instcheck==0)
            {
                ret_string += "\nLess than max instance: True";
            }
            else if (instcheck==1)
            {
                ret_string += "\nLess than max instance: False";
                ret_string += "\nMax=" + _proc.m_maxCreatures + ", Mates=" + (inst_arr[1]-1) + ", Offspring=" + inst_arr[2];
            }
            else if (instcheck == 2)
            {
                ret_string += "\nLess than max instance: False (needs mate)";
                ret_string += "\nReady Mates=" + (inst_arr[3] - 1) + ", Offspring=" + inst_arr[2];
                ret_string += "\nClosest 3 Creatures within range of "+ _proc.m_totalCheckRange + " units are:\n";
                ret_string += GetCloseInstances(_proc.gameObject, _proc.gameObject.transform.position, _proc.m_totalCheckRange);
            }
            else
            {
                ret_string += "\nLess than max instance: Error";
            }
            int lovepoints = _proc.m_nview.GetZDO().GetInt("lovePoints");
            ret_string += "\nLovePoints: " + lovepoints + " out of " + _proc.m_requiredLovePoints;
            //ret_string+= "\n "


            return ret_string;
        }


        private static string GetCloseInstances(GameObject instance, Vector3 center, float maxRange)
        {
            //DBG.blogDebug("In GetCloseInstances");
            float num = 999999f;
            float num2 = 999999f;
            float num3 = 999999f;
            string close1 = "n/a";
            string close2 = "n/a2";
            string close3 = "n/a3";
            BaseAI baseai = instance.GetComponentInParent<BaseAI>();
            List<Character> characters = Character.GetAllCharacters();

            foreach (Character character in characters)
            {
                
                if (!(character.gameObject == baseai.gameObject) && !(character.IsPlayer()) && character.GetComponent<ZNetView>().IsValid())// && !(Vector3.Distance(character.transform.position, base.transform.position) > 40))
                {

                    float numtemp = Vector3.Distance(character.transform.position, instance.transform.position);
                    if (numtemp < num)
                    {
                        close1 = character.name;
                        num = numtemp;
                        if (character.IsTamed())
                        {
                            close1 = close1 + "(Tamed)";
                        }
                        else
                        {
                            if (character.gameObject.GetComponent<Tameable>() != null)
                            {
                                close1 = close1 + "(Untamed)";
                            }
                            else
                            {
                                close1 = close1 + "(NotTameable)";
                            }
                        }
                        
                    }
                    else if (numtemp < num2)
                    {
                        close2 = character.name;
                        num2 = numtemp;
                        if (character.IsTamed())
                        {
                            close2 = close2 + "(Tamed)";
                        }
                        else
                        {
                            if (character.gameObject.GetComponent<Tameable>() != null)
                            {
                                close2 = close2 + "(Untamed)";
                            }
                            else
                            {
                                close2 = close2 + "(NotTameable)";
                            }
                        }
                    }
                    else if (numtemp < num3)
                    {
                        close3 = character.name;
                        num3 = numtemp;
                        if (character.IsTamed())
                        {
                            close3 = close3 + "(Tamed)";
                        }
                        else
                        {
                            if (character.gameObject.GetComponent<Tameable>() != null)
                            {
                                close3 = close3 + "(Untamed)";
                            }
                            else
                            {
                                close3 = close3 + "(NotTameable)";
                            }
                        }
                    }
                }
                //if (clonemates.Contains(character.gameObject.name))
                //{
                //    DBG.blogDebug("found clone with name " + character.gameObject.name);
                //}
            }
            string threeclose = (close1 + ":" + close2 + ":" + close3).Replace("(Clone)","");
            //DBG.blogDebug("Closest Creatures are " + close1 + ":" + close2 + ":" + close3);
            //DBG.blogDebug("Partner with name go:" + partner.gameObject.name + " is " + Vector3.Distance(partner.transform.position, base.transform.position) + "m away");

            return threeclose;
        }






    }


}
