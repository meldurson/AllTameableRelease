using AllTameable;
using AllTameable.RPC;
using static AllTameable.Plugin;
using BepInEx;
using HarmonyLib;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;
//using System;
using System.Reflection;
using UnityEngine;
using System;

namespace AllTameable.Genetics
{
    public class Genetics// : BaseUnityPlugin
    {
        //private static ZNetScene zns;

        //private static Tameable wtame;

        public static GameObject Root;

        private void Awake()
        {
            Root = new GameObject("Genetics");
            Root.transform.SetParent(Plugin.prefabManager.Root.transform);

        }






        [HarmonyPostfix]
        [HarmonyPatch(typeof(Procreation), "MakePregnant")]

        private static void PostfixMakePregnant(Procreation __instance)
        {
            string prefname = __instance.name.Replace("(Clone)", ""); ;
            //DBG.blogDebug("prefname=" + prefname);
            //get offspring
            DBG.blogDebug("Make Pregnant");
            //__instance.m_nview.GetZDO().Set("OffspringName", __instance.m_offspring.name);
            initRandomOffspring(prefname, __instance, true);

            if (Plugin.cfgList.TryGetValue(prefname, out Plugin.TameTable tmtbl))
            {
                DBG.blogDebug("Tameable exists, " + prefname);
                DBG.blogDebug("canmatewithself=" + tmtbl.canMateWithSelf);

            }
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(Procreation), "ResetPregnancy")]

        private static void PostfixResetPregnancy(Procreation __instance) //makes sure that offspring is still valid
        {
            string prefname = __instance.name.Replace("(Clone)", ""); ;
            //DBG.blogDebug("prefname=" + prefname);
            //get offspring
            string offspringName = GetOffspring(__instance);
            if ((offspringName + "") != "")
            {
                initRandomOffspring(prefname, __instance);
                DBG.blogDebug("offspringName=" + offspringName);
                ZNetScene zns = ZNetScene.instance;
                GameObject prefab = zns.GetPrefab(offspringName);
                if (prefab != null)
                {
                    DBG.blogDebug("prefab=" + prefab.name);
                    __instance.m_offspring = prefab;
                    __instance.m_offspringPrefab = prefab;
                }
                else
                {
                    DBG.blogDebug("Failed to get random offspring");
                    initRandomOffspring(prefname, __instance, true);
                }
            }
            else
            {
                initRandomOffspring(prefname, __instance, true);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Procreation), "Awake")]

        private static void PostfixProcreationAwake(Procreation __instance)
        {
            if (__instance.m_nview.IsValid())
            {
                __instance.m_nview.Register<ZDOID, string>("SetOffspring", SetOffspring);
            }
        }


        public static void SetOffspring(long sender, ZDOID characterID, string name)
        {
            ZNetView m_nview = ZNetScene.instance.GetComponent<ZNetView>();
            if (m_nview.IsValid() && m_nview.IsOwner())
            {
                m_nview.GetZDO().Set("OffspringName", name);
            }
        }

        public static string GetOffspring(Procreation _proc)
        {
            string tempstr = "";
            ZNetView m_nview = _proc.m_nview;
            //DBG.blogDebug("tried to get ZNetView");
            if (m_nview.IsValid())
            {
                //DBG.blogDebug("ZNetView valid");
                tempstr = m_nview.GetZDO().GetString("OffspringName");
                //DBG.blogDebug("tempstr=" + tempstr);
            }
            return tempstr;
        }



        [HarmonyPrefix]
        [HarmonyPatch(typeof(Procreation), "Procreate")]

        private static void PrefixProcreation(Procreation __instance)
        {
            if (!(__instance.m_myPrefab ?? false) | !(__instance.m_offspringPrefab ?? false)) //prefab is null
            {
                //DBG.blogDebug("Procreation Initialised Prefabs");
                InitProcPrefabs(__instance);
            }
            else
            {
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Procreation), "Awake")]

        private static void PrefixProcreationAwake(Procreation __instance)
        {
            //DBG.blogDebug("Procreation Awake, " + __instance.name);
            string prefname = __instance.name.Replace("(Clone)", "");
            if (Plugin.cfgList.TryGetValue(prefname, out Plugin.TameTable tmtbl))
            {
                //DBG.blogDebug("Proc Awake, " + prefname);
                if ((tmtbl.specificOffspringString + "") != "")
                {
                    //DBG.blogDebug("specificOffspringString=" + cfgfile.specificOffspringString);
                    if (tmtbl.ListofRandomOffspring.Count() == 0 | !Plugin.CompatMatesList.ContainsKey(prefname))
                    {
                        List<string> prefnames = new List<string>();
                        DBG.blogDebug("Proc Awake, " + prefname+":  tmtbl.specificOffspringString=" + tmtbl.specificOffspringString);
                        string[] partners = tmtbl.specificOffspringString.Split(',');
                        partners = partners.Skip(1).ToArray();
                        foreach (string combinedValue in partners)
                        {
                            prefnames.Add(combinedValue.Split('(')[0]);
                        }
                        if (Plugin.CompatMatesList.ContainsKey(prefname))
                        {
                            foreach (string mate in prefnames)
                            {
                                if (!Plugin.CompatMatesList[prefname].Contains(mate))
                                {
                                    Plugin.CompatMatesList[prefname].Add(mate);
                                    DBG.blogDebug("added mate=" + mate);
                                }
                                else
                                {
                                    DBG.blogDebug("Already added mate=" + mate);
                                }
                                
                            }
                        }
                        else
                        {
                            Plugin.CompatMatesList.Add(prefname, prefnames);
                            DBG.blogDebug("added mates=" + String.Join(",", prefnames));
                        }

                    }
                    //DBG.blogDebug("Plugin.CompatMatesList[prefname]=" + String.Join(",", Plugin.CompatMatesList[prefname]));

                }
            }
        }



        private static void initRandomOffspring(string prefabname, Procreation proc, bool changeOff = false)
        {
            if (Plugin.cfgList.TryGetValue(prefabname, out Plugin.TameTable tmtbl))
            {
                DBG.blogDebug("found prefab, " + prefabname);
                if ((tmtbl.specificOffspringString + "") != "")
                {
                    //DBG.blogDebug("specificOffspringString=" + cfgfile.specificOffspringString);
                    if (tmtbl.ListofRandomOffspring.Count() == 0)
                    {
                        //List<string[]> partnerList = new List<string[]>();
                        DBG.blogDebug("initOffspring");

                        List<specificMates> specMates = new List<specificMates>();
                        Dictionary<string, string> partnersDict = new Dictionary<string, string>();
                        string[] partners = tmtbl.specificOffspringString.Split(',');
                        //DBG.blogDebug(partners.ToString());
                        partners = partners.Skip(1).ToArray();
                        //DBG.blogDebug(partners.ToString());
                        foreach (string combinedValue in partners)
                        {
                            string[] splitValue = combinedValue.Replace(")", "").Split('(');
                            partnersDict.Add(splitValue[0], splitValue[1]);
                            //DBG.blogDebug("key=" + splitValue[0] + ", value=" + splitValue[1]);
                            specificMates addPartner = new specificMates();
                            addPartner.prefabName = splitValue[0];
                            string[] prefchances = splitValue[1].Split('/');
                            float totalchance = 0;
                            foreach (string chancepkg in prefchances)
                            {
                                chanceOffspring chanceoff = new chanceOffspring();
                                string[] pref_and_chance = chancepkg.Split(':');
                                string prefname = pref_and_chance[0];
                                GameObject mate_go = ZNetScene.instance.GetPrefab(prefname);
                                if (mate_go != null)
                                {
                                    DBG.blogDebug("found go for " + mate_go.name);
                                    //Procreation mate_proc = mate_go.GetComponent<Procreation>();
                                    if (mate_go.GetComponent<Procreation>() != null)
                                    {
                                        chanceoff.offspring = mate_go.GetComponent<Procreation>().m_offspring;
                                        DBG.blogDebug("chanceoff.offspring=" + chanceoff.offspring.name);
                                    }
                                    else
                                    {
                                        Growup this_growup = proc.m_offspring.GetComponent<Growup>();
                                        if (this_growup != null)
                                        {
                                            chanceoff.offspring = PetManager.SpawnMini(mate_go, this_growup.m_growTime);
                                        }
                                        else
                                        {
                                            DBG.blogDebug("growup null");
                                            chanceoff.offspring = PetManager.SpawnMini(mate_go);
                                        }

                                        DBG.blogDebug("chanceoff.offspring=" + chanceoff.offspring.name);
                                    }
                                    try { chanceoff.chance = float.Parse(pref_and_chance[1]); }
                                    catch { DBG.blogWarning("Not a valid float for chance for " + proc.name); }
                                    //DBG.blogDebug("chanceoff.chance=" + chanceoff.chance);
                                    totalchance += chanceoff.chance;
                                    addPartner.possibleOffspring.Add(chanceoff);
                                }
                                else
                                {
                                    DBG.blogWarning("could not find prefab:" + prefname + " when trying to mate with " + proc.name);
                                }

                            }
                            DBG.blogDebug("totalchance=" + totalchance);
                            if (totalchance < 100)
                            {
                                chanceOffspring defaultOff = new chanceOffspring();
                                defaultOff.chance = 100 - totalchance;
                                defaultOff.offspring = proc.m_offspring;
                                addPartner.possibleOffspring.Add(defaultOff);
                            }
                            specMates.Add(addPartner);

                        }
                        tmtbl.ListofRandomOffspring = specMates;
                        foreach (specificMates specmates in tmtbl.ListofRandomOffspring)
                        {
                            DBG.blogDebug("specmates.prefabName=" + specmates.prefabName);
                            foreach (chanceOffspring chancepkg in specmates.possibleOffspring)
                            {
                                DBG.blogDebug("     chancepkg.offspring.name=" + chancepkg.offspring.name);
                                DBG.blogDebug("     chancepkg.chance=" + chancepkg.chance);
                            }
                        }
                    }
                    if (changeOff)
                    {
                        changeOffspring(proc, tmtbl.ListofRandomOffspring);
                    }
                }
                else
                {
                    DBG.blogDebug("No specific offspring for "+ prefabname);
                }

            }
        }
        private static void changeOffspring(Procreation proc, List<specificMates> mates)
        {
            DBG.blogDebug("changeOffspring");
            /*
            //List<string[]> partnerList = new List<string[]>();
            DBG.blogDebug("changeOffspring");
            Dictionary<string, string> partnersDict = new Dictionary<string, string>();
            string[] partners = spec_str.Split(',');
            //DBG.blogDebug(partners.ToString());
            partners = partners.Skip(1).ToArray();
            //DBG.blogDebug(partners.ToString());
            foreach(string combinedValue in partners)
            {
                string[] splitValue = combinedValue.Replace(")", "").Split('(');
                partnersDict.Add(splitValue[0], splitValue[1]);
                DBG.blogDebug("key=" + splitValue[0]+", value="+ splitValue[1]);
            }
            */
            Character partner = getPartner(proc.GetComponentInParent<BaseAI>());
            string prefname = partner.name.Replace("(Clone)", "");
            DBG.blogDebug("partner=" + prefname);
            specificMates foundMate = null;
            foreach (specificMates mate in mates)
            {
                if (mate.prefabName == prefname)
                {
                    foundMate = mate;
                    break;
                }
            }
            if (foundMate != null)
            {
                //foundMate.possibleOffspring
                float rndm = UnityEngine.Random.Range(0f, 100f);
                float currentchance = 0;

                foreach (chanceOffspring chanceOff in foundMate.possibleOffspring)
                {
                    currentchance += chanceOff.chance;
                    if (currentchance >= rndm)
                    {
                        DBG.blogDebug("currentchance=" + currentchance + ", rndm=" + rndm);
                        proc.m_offspring = chanceOff.offspring;
                        proc.m_offspringPrefab = chanceOff.offspring;
                        DBG.blogDebug("proc.m_offspring=" + proc.m_offspring.name);

                        proc.m_nview.GetZDO().Set("OffspringName", proc.m_offspring.name);
                        break;
                    }
                }
            }


        }




        public static Character getPartner(BaseAI baseAI)
        {
            Character partner = null;
            float num = 999999f;
            //BaseAI baseAI = GetComponentInParent<BaseAI>();
            List<Character> characters = Character.GetAllCharacters();
            //DBG.blogDebug("This char is:" + baseai.gameObject.name);
            //List<string> possiblemates = Plugin.CompatMatesList[baseai.gameObject.name];
            if (!Plugin.CompatMatesList.TryGetValue(Utils.GetPrefabName(baseAI.gameObject), out var possiblemates))
            {
                possiblemates = new List<string> { Utils.GetPrefabName(baseAI.gameObject) };
            }
            List<string> clonemates = new List<string>();
            ZNetScene zns = ZNetScene.instance;
            if (cfgList.TryGetValue(baseAI.gameObject.name.Replace("(Clone)", ""), out TameTable cfgfile))
            {
                if (cfgfile.canMateWithSelf)
                {
                    clonemates.Add(baseAI.gameObject.name);
                }
            }
            foreach (string str in possiblemates)
            {
                clonemates.Add(str + "(Clone)");
            }
            DBG.blogDebug("Possible mates= " + string.Join(":", clonemates));

            foreach (Character character in characters)
            {
                if (!(character.gameObject == baseAI.gameObject) && clonemates.Contains(character.gameObject.name) && character.GetComponent<ZNetView>().IsValid())// && !(Vector3.Distance(character.transform.position, base.transform.position) > 40))
                {

                    float num2 = Vector3.Distance(character.transform.position, baseAI.transform.position);
                    if (num2 < num)
                    {
                        DBG.blogDebug("character with name go:" + character.gameObject.name + " is " + Vector3.Distance(character.transform.position, baseAI.transform.position) + "m away");
                        partner = character;
                        num = num2;
                    }

                }
                //if (clonemates.Contains(character.gameObject.name))
                //{
                //    DBG.blogDebug("found clone with name " + character.gameObject.name);
                //}
            }
            return partner;
        }


        public static void InitProcPrefabs(Procreation _proc)
        {
            string prefabName = Utils.GetPrefabName(_proc.m_offspring);
            _proc.m_offspringPrefab = ZNetScene.instance.GetPrefab(prefabName);
            int prefab = _proc.m_nview.GetZDO().GetPrefab();
            _proc.m_myPrefab = ZNetScene.instance.GetPrefab(prefab);
            //DBG.blogDebug("Initialised Proc Prefabs");
            if (!(_proc.m_myPrefab ?? false))
            {
                DBG.blogDebug("m_myPrefab is still Null, trying again");
                DBG.blogDebug(_proc.name);

                string proc_name = _proc.name.Replace("(Clone)", "");
                DBG.blogDebug("proc_name=" + proc_name);
                if (ZNetScene.instance.GetPrefab(proc_name) == null)
                {
                    DBG.blogDebug("proc_name failed");
                }
                else
                {
                    _proc.m_myPrefab = ZNetScene.instance.GetPrefab(proc_name);
                }
                if (!(_proc.m_myPrefab ?? false))
                {
                    DBG.blogDebug("m_myPrefab backup failed");
                }
                else
                {
                    DBG.blogDebug("m_myPrefab backup success");
                }
            }
            if (!(_proc.m_offspringPrefab ?? false))
            {
                DBG.blogDebug("m_offspringPrefab is still Null, trying again");
                string proc_offspring_name = _proc.m_offspring.name;
                DBG.blogDebug("Failed prefabName=" + prefabName);
                //_proc.m_offspringPrefab = ZNetScene.instance.GetPrefab(proc_offspring_name);

                _proc.m_offspringPrefab = _proc.m_offspring;
                if (!(_proc.m_offspringPrefab ?? false))
                {

                    DBG.blogDebug("m_offspringPrefab backup failed :" + (_proc.m_offspringPrefab == null));
                }
                else
                {
                    DBG.blogDebug("m_offspringPrefab backup success");
                }

            }
        }



        [HarmonyPostfix]
        [HarmonyPatch(typeof(SpawnSystem), "GetNrOfInstances", new Type[] { typeof(GameObject), typeof(Vector3), typeof(float), typeof(bool), typeof(bool) })]
        //private static class Prefix_Player_SetLocalPlayer
        //{
        private static void Postfix_SpawnSystem_GetNrOfInstances(GameObject prefab, Vector3 center, float maxRange, bool eventCreaturesOnly, bool procreationOnly, ref int __result)
        {
            int sum = __result;
            if (prefab.GetComponent<Tameable>() != null)
            {
                try
                {
                    if (CompatMatesList.TryGetValue(prefab.name, out List<string> mates))
                    {
                        //DBG.blogDebug("Has more mates");
                        if (cfgList.TryGetValue(prefab.name, out TameTable cfgfile))
                        {
                            if (!cfgfile.canMateWithSelf)
                            {
                                int previous_sum = sum - 1;
                                sum = 1;
                                //DBG.blogDebug("Cannot Mate with same prefab, " + previous_sum + " removed from total nearby, " +
                                //    "newsum ="+ sum);
                            }
                        }
                        ZNetScene zns = ZNetScene.instance;
                        foreach (var mate in mates)
                        {
                            if (zns.GetPrefab(mate))
                            {
                                int added = 0;
                                try
                                {
                                    //DBG.blogDebug("Attempting to add " + mate);
                                    //try { DBG.blogDebug("zns.GetPrefab(mate): = " + zns.GetPrefab(mate)); } catch { }
                                    //try { DBG.blogDebug("center: = " + center); } catch { }
                                    //try { DBG.blogDebug("maxRange: = " + maxRange); } catch { }
                                    //try { DBG.blogDebug("eventCreaturesOnly: = " + eventCreaturesOnly); } catch { }
                                    //try { DBG.blogDebug("procreationOnly: = " + procreationOnly); } catch { }
                                    added = Safe_GetNrOfInstances(zns.GetPrefab(mate), center, maxRange, false, eventCreaturesOnly, procreationOnly);
                                }
                                catch
                                {
                                    added = 0;
                                    DBG.blogDebug("Failed to add " + mate + ":error");
                                } //if fails do not add
                                if (added > 0)
                                {
                                    //DBG.blogDebug("Added " + added + " of " + mate + "to instnum for " + prefab.name);
                                }
                                sum += added;
                            }
                            else
                            {
                                DBG.blogDebug("Failed to find mate of " + mate + "to instnum for " + prefab.name);
                            }
                        }

                    }
                } catch { DBG.blogDebug("Error in finding mate for " + prefab.name); }
            }
            __result = sum;
            return;
        }



    }
}
