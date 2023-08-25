using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AllTameable
{
    //[HarmonyPatch]
    internal static class CommandGroup
    {
        public static int commandtype = Plugin.DefaultCommandType.Value; //0=Just same prefab, 1=creatures that can mate with, 2 = any tamed in area
        public static string[] commandstrings = new string[3];
        public static string[] cyclestrings = new string[3];
        public static KeyCode commandKey = KeyCode.G;
        public static KeyCode cycleKey = KeyCode.H;





        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character), "GetHoverText")]
        private static void GetHoverText(Character __instance, ref string __result)
        {
            if (Plugin.useTamingTool.Value)
            {
                
                Player plr = Player.m_localPlayer;
                //plr = ZNetScene.FindObjectOfType<Player>;
                if (plr.GetCurrentWeapon().m_dropPrefab == ZNetScene.instance.GetPrefab(Plugin.advtoolPrefabName))
                {
                    if (__instance.m_tamed)
                    {
                        try { commandKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), Plugin.CommandKey.Value.ToUpper()); }
                        catch { DBG.blogWarning("Command key setting " + Plugin.CommandKey.Value + " is not a valid key in this context, defaulting to G"); }
                        try { cycleKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), Plugin.CycleKey.Value.ToUpper()); }
                        catch { DBG.blogWarning("Cycle key setting " + Plugin.CycleKey.Value + " is not a valid key in this context, defaulting to H"); }
                        if (Input.GetKeyDown(cycleKey))
                        {
                            changeCommand();
                            plr.Message(MessageHud.MessageType.Center, cycleMessage());
                        }
                        if (Input.GetKeyDown(commandKey))
                        {
                            sendCommand(__instance, plr);
                        }
                        //string taming_text = "\n";
                        //taming_text += "commandType=" + commandTypetoStr();
                        addExtraCommands(ref __result);
                        //__result += taming_text;
                    }
                    

                }
                
            }
        }

        private static void sendCommand(Character thisChar, Player plr)
        {
            Tameable thisTame = tameablefromChar(thisChar);
            if (!(bool)thisTame)
            {
                DBG.blogWarning("Could not find Tameable in creature, exiting command");
                return;
            }
            if (!thisTame.m_commandable)
            {
                thisTame.m_petEffect.Create(thisTame.transform.position, thisTame.transform.rotation);
                plr.Message(MessageHud.MessageType.Center, thisTame.GetHoverName() + " $hud_tamelove");
                return;
            }
            List<Character> commandList = getCommandList(thisTame);
            string commandstr = "";
            foreach(Character cr in commandList)
            {
                commandstr += cr.name + ",";
            }
            DBG.blogDebug("commandstr=" + commandstr);
            MonsterAI mAI = thisChar.gameObject.GetComponent<MonsterAI>();
            if ((bool)mAI)
            {
                bool setStay = getTameFollow(mAI, plr);
                int numNotCommand = commandGroup(commandList, plr, setStay);
                plr.Message(MessageHud.MessageType.Center, getCommandMessage(thisChar, commandList.Count - numNotCommand, setStay));

            }
            
        }
        private static string getCommandMessage(Character thisChar, int numCommand, bool setStay)
        {
            string prefName = thisChar.GetHoverName();// removeClone(thisChar.name);
            string returnstr = prefName + " and " + (numCommand - 1);
            string cmdstr = "Follow";
            if (setStay) { cmdstr = "Stay"; }
            if (numCommand == 1)
            {
                return prefName + " was asked to " + cmdstr;
            }
            switch (commandtype)
            {
                case 0: //only same prefab
                    returnstr += " others have been asked to " + cmdstr;
                    break;
                case 1: //with mates
                    returnstr += " mates have been asked to " + cmdstr;
                    break;
                case 2: //any in range
                default:
                    returnstr += " tames have been asked to " + cmdstr;
                    break;
            }
            return returnstr;
        }

        private static string commandTypetoStr()
        {
            if (commandstrings[0] + "" == "")
            {
                DBG.blogDebug("command strings made");
                commandstrings[0] = "(<color=yellow><b>" + "Same" + "</b></color>) ";
                commandstrings[1] = "(<color=#5CEB59><b>" + "Mates" + "</b></color>) ";
                commandstrings[2] = "(<color=#2b82ed><b>" + "Any" + "</b></color>) ";
            }
            return commandstrings[commandtype];

        }
        private static string cycleMessage()
        {
            if (cyclestrings[0] + "" == "")
            {
                DBG.blogDebug("cycle strings made");
                cyclestrings[0] = "Shout at only this type of creature";
                cyclestrings[1] = "Shout at this creature and any that it can breed with";
                cyclestrings[2] = "Shout for any tamed creature in range";
            }
            return cyclestrings[commandtype];

        }

        private static void addExtraCommands(ref string result)
        {
            //DBG.blogDebug("result1="+result);
            string toMatch = Localization.instance.Localize("$hud_pet");
            //DBG.blogDebug("toMatch=" + toMatch);
            result = result.Replace(toMatch, toMatch + " [<color=orange><b>" + cycleKey + "</b></color>] Cycle Shout "+ commandTypetoStr());
            toMatch = Localization.instance.Localize("$hud_rename");
            result = result.Replace(toMatch, toMatch + " [<color=orange><b>" + commandKey + "</b></color>] Shout");


        }

        private static int commandGroup(List<Character> chars, Player plr, bool setStay)
        {
            int numNotCommand = 0;
            foreach(Character thischar in chars)
            {
                if (!thischar.gameObject.TryGetComponent<Tameable>(out var thisTame))
                {
                    DBG.blogDebug("No Tameable in " + thischar.name);
                    numNotCommand++;
                    continue;
                }
                if (!thischar.gameObject.TryGetComponent<MonsterAI>(out var thismAI))
                {
                    DBG.blogDebug("No MonsterAI in " + thischar.name);
                    numNotCommand++;
                    continue;
                }
                GameObject followTarget = thismAI.GetFollowTarget();
                if (setStay & !(bool)followTarget)
                {
                    //creature not currently following and want to stay that way
                    DBG.blogDebug("Leaving as Stay for " + thischar.name);
                    continue;
                }
                else if (!setStay)
                {
                    thismAI.SetFollowTarget(null);
                }
                DBG.blogDebug("Sending Command to " + thischar.name);
                if (!commandSingle(thisTame, plr)){numNotCommand++;};
            }
            return numNotCommand;
            //plr.Message(MessageHud.MessageType.Center, getCommandMessage(chars[0],chars.Count-numNotCommand, setStay));

        }

        private static bool commandSingle(Tameable thisTame, Player plr)
        {
            thisTame.m_lastPetTime = Time.time;
            if (thisTame.m_commandable)
            {
                thisTame.m_petEffect.Create(thisTame.transform.position, thisTame.transform.rotation);
                thisTame.Command(plr, true);
                return true;
            }
            return false;
            
            
        }

        private static Tameable tameablefromChar(Character thisChar)
        {
            Tameable thisTame = thisChar.gameObject.GetComponent<Tameable>();
            if (!(bool)thisTame)
            {
                DBG.blogDebug("No Tameable Found");
            }
            return thisTame;
        }

        private static bool getTameFollow(MonsterAI thisMAI, Player plr) //only if tame is currently following this player, therfore wanting to stay, will return true
        {
            return thisMAI.m_follow == plr.gameObject;
        }

        private static List<Character> getCommandList(Tameable thisTame)
        {
            List<Character> fullCharList = new List<Character>();
            List<Character> selectCharList = new List<Character>();
            Character.GetCharactersInRange(thisTame.transform.position, Plugin.CommandRange.Value, fullCharList);
            if (commandtype == 2)
            {
                foreach(Character thisChar in fullCharList)
                {
                    if (thisChar.m_tamed) { selectCharList.Add(thisChar); }
                }
                return selectCharList;
                //find any tame in area
            }
            List<string> prefabList = possibleMatePrefabs(thisTame);
            foreach (Character thisChar in fullCharList)
            {
                if (thisChar.m_tamed && prefabList.Contains(removeClone(thisChar.name))) { selectCharList.Add(thisChar); }
            }
            return selectCharList;

        }

        public static void changeCommand()
        {
            commandtype = (commandtype + 1) % 3; //cycles between command types
            Plugin.DefaultCommandType.Value = commandtype;
        }

        private static List<string> possibleMatePrefabs(Tameable thisTame)
        {
            List<string> prefabList = new List<string>();
            DBG.blogDebug("thistame.name=" + thisTame.name);
            prefabList.Add(removeClone(thisTame.name));
            if (commandtype > 0)
            {
                if (Plugin.CompatMatesList.TryGetValue(removeClone(thisTame.name), out List<string> matelist))
                {
                    prefabList.AddRange(matelist);
                }
            }
            DBG.blogDebug("possibleMateprefabs=" + string.Join(",", prefabList));
            return prefabList;
        }

        public static string removeClone(string withClone)
        {
            return withClone.Replace("(Clone)", "");
        }


    }


}
