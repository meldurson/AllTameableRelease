﻿using HarmonyLib;
using System;


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
                            if (_Proc != null)
                            {
                                if (_Proc.IsDue())
                                {
                                    taming_text += "Is Due";
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

                }
            }
        }
        public static int getInstNum(Procreation _proc)
        {
            try
            {
                int nrOfInstances = SpawnSystem.GetNrOfInstances(_proc.m_myPrefab, _proc.gameObject.transform.position, _proc.m_totalCheckRange);
                //DBG.blogDebug(nrOfInstances);
                int nrOfInstances2 = SpawnSystem.GetNrOfInstances(_proc.m_offspringPrefab, _proc.gameObject.transform.position, _proc.m_totalCheckRange);
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
            catch
            {
                return -1; //error
            }
        }
        public static string GetPregStats(Procreation _proc)
        {
            string ret_string = "";
            bool isvalid = !_proc.m_nview.IsValid() || !_proc.m_nview.IsOwner() || !_proc.m_character.IsTamed();

            ret_string += "\nisValid: " + !isvalid;
            ret_string += "\nisNotPreg: " + !_proc.IsPregnant();
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
    }


}
