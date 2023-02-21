﻿using HarmonyLib;
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
                            if (Plugin.debugout.Value)
                            {
                                taming_text += ("Prefab is: " + __instance.name +", is commandable= "+ _tm.m_commandable +"\n").Replace("(Clone)","");
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
                                        if (Plugin.debugout.Value)
                                        {
                                            taming_text += GetPregStats(_Proc);
                                            __result = __result + taming_text;
                                            return;
                                        }
                                        else
                                        {
                                            int instnum = getInstNum(_Proc);
                                            if (instnum != 0 )
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
                                    int line_len_trade = 20;
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


                        int line_len = 20;
                        if (notend)
                        {
                            foreach (ItemDrop item in _mAI.m_consumeItems)
                            {
                                taming_text = taming_text + item.name + ", ";
                                if (taming_text.Length - line_len > 50)
                                {
                                    line_len = taming_text.Length;
                                    taming_text = taming_text + "\n";
                                }
                            }
                            taming_text = taming_text.Remove(taming_text.Length - 2);
                        }
                        __result = __result + taming_text;
                    }
                    else // not tameable
                    {
                        if (Plugin.debugout.Value)
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
        public static int getInstNum(Procreation _proc)
        {
            int nrOfInstances = -10;
            int nrOfInstances2 = -10;
            try
            {
                

                nrOfInstances = SpawnSystem.GetNrOfInstances(_proc.m_myPrefab, _proc.gameObject.transform.position, _proc.m_totalCheckRange);
                //DBG.blogDebug(nrOfInstances);
                nrOfInstances2 = SpawnSystem.GetNrOfInstances(_proc.m_offspringPrefab, _proc.gameObject.transform.position, _proc.m_totalCheckRange);
                //DBG.blogDebug("n1=" + nrOfInstances + ", n2= " + nrOfInstances2);
                //DBG.blogDebug("_proc.m_maxCreatures=" + _proc.m_maxCreatures);
                bool n_instcheck = nrOfInstances + nrOfInstances2 < _proc.m_maxCreatures && SpawnSystem.GetNrOfInstances(_proc.m_myPrefab, _proc.transform.position, _proc.m_partnerCheckRange, eventCreaturesOnly: false, procreationOnly: true) >= 2;
               if (n_instcheck == true)
                {
                    return 0; //true
                }
                else if (nrOfInstances + nrOfInstances2 > _proc.m_maxCreatures)
                {
                    return 1;//Too crowded
                }
                else
                {
                    return 2; //Needs Mate
                }
                    
            }
            catch (Exception e)
            {
                DBG.blogDebug("Error: "+ e.Message);
                DBG.blogDebug("Error: " + e.StackTrace);
                return -1; //error
            }
        }



        public static string GetPregStats(Procreation _proc)
        {
            string ret_string = "";
            bool isvalid = !_proc.m_nview.IsValid() || !_proc.m_nview.IsOwner() || !_proc.m_character.IsTamed();
            ret_string += "\nPrefab Name: " + _proc.gameObject.name.Replace("(Clone)","");
            ret_string += "\nisValid: " + !isvalid;
            //ret_string += "\nisNotPreg: " + !_proc.IsPregnant();
            ret_string += "\nPregchance= " + _proc.m_pregnancyChance;
            ret_string += "\nIsNotAlerted= " + !_proc.m_baseAI.IsAlerted();
            ret_string += "\nIsNotHungry= " + !_proc.m_tameable.IsHungry();
            ret_string += "\nCheckRandomVal: " + (UnityEngine.Random.value > _proc.m_pregnancyChance) + "<-if not steady False, then valid";
            bool ispregchance = UnityEngine.Random.value <= _proc.m_pregnancyChance || _proc.m_baseAI.IsAlerted() || _proc.m_tameable.IsHungry();
            ret_string += "\nislessThanPregchance: " + !ispregchance + "<-if not steady False, then valid";

            int instcheck = getInstNum(_proc);
            if (instcheck==0)
            {
                ret_string += "\nLess than max instance: True";
            }
            else if (instcheck==1)
            {
                ret_string += "\nLess than max instance: False";
            }
            else if (instcheck == 2)
            {
                ret_string += "\nLess than max instance: False (needs mate)";
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
            Character partner = null;
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
