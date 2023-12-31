﻿using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Globalization;

namespace AllTameable
{
    internal class TameListCfg_old
    {
        public static Dictionary<string, Plugin.TameTable> cfgList2 = new Dictionary<string, Plugin.TameTable>();

        public static string pathcfg = Path.GetDirectoryName(Paths.BepInExConfigPath) + Path.DirectorySeparatorChar + "AllTameable_TameList.cfg";
        public static bool Init()
        {
            cfgList2.Clear();
            if (!File.Exists(pathcfg))
            {
                DBG.blogWarning("Failed to find TameList file");
                return false;
            }
            DBG.blogInfo("Found TameList file");
            StreamReader streamreader = new StreamReader(pathcfg);
            string sline;
            while ((sline = streamreader.ReadLine()) != null)
            {
                sline = sline.Trim();
                if (sline.Length == 0 || sline.StartsWith("#"))
                {
                    continue;
                }
                sline = sline.Replace("TRUE", "true").Replace("FALSE", "false").Replace(";", "");
                string[] arr = sline.Split(',');
                try
                {
                    Plugin.TameTable temptbl = ArrToTametable(arr);
                    string[] prefablist = SplitMates(arr[0]);
                    //DBG.blogDebug("prefablist="+string.Join(",", prefablist));
                    if (prefablist.Count() >1)
                    {
                        //DBG.blogDebug("Count >0: "+ arr[0]);
                        Plugin.rawMatesList.Add(arr[0]);
                        
                        SetCompatMates(prefablist);
                        
                        for (int i = 0; i < prefablist.Count(); i++)
                        {
                            if (!cfgList2.ContainsKey(prefablist[i]))
                            {
                                cfgList2.Add(prefablist[i], temptbl);
                                DBG.blogInfo("succesfully added " + prefablist[i] + " with mates " + string.Join(", ", prefablist));
                            }
                        }
                        /*
                        for (int i = 0; i < prefablist.Count(); i++)
                        {
                        
                            List<string> otherPrefabs = new List<string>();
                            for (int j = 0; j < prefablist.Count(); j++)
                            {
                                
                                if (i != j)
                                {
                                    otherPrefabs.Add(prefablist[j]);
                                }
                                
                            }
                            Plugin.CompatMatesList.Add(prefablist[i], otherPrefabs);
                            if (!cfgList2.ContainsKey(prefablist[i]))
                            {
                                cfgList2.Add(prefablist[i], temptbl);
                                DBG.blogInfo("succesfully added " + prefablist[i] + " with mates " + string.Join(", ", otherPrefabs));
                            }
                            else
                            {
                                DBG.blogInfo("Modified " + prefablist[i] + " with mates " + string.Join(", ", otherPrefabs));
                            }
                            
                        }
                        */

                    }
                    else
                    {
                        if (!cfgList2.ContainsKey(arr[0]))
                        {
                            cfgList2.Add(arr[0], temptbl);
                            DBG.blogInfo("succesfully added " + arr[0] + " to the tametable");
                        }
                    }
                    
                }
                catch
                {
                    DBG.blogWarning("Failed to add tametablecfg for " + arr[0]);
                }
                

            }
            DBG.blogDebug("rawmateslist=" + string.Join(",", Plugin.rawMatesList));
            DBG.blogDebug("rawtradelist= " + string.Join(",", Plugin.rawTradesList));
            Plugin.cfgList = cfgList2;
            return true;
        }

        public static void SetCompatMates(string[] matelist)
        {
            for (int i = 0; i < matelist.Count(); i++)
            {

                List<string> otherPrefabs = new List<string>();
                for (int j = 0; j < matelist.Count(); j++)
                {

                    if (i != j)
                    {
                        otherPrefabs.Add(matelist[j]);
                    }

                }
                Plugin.CompatMatesList.Add(matelist[i], otherPrefabs);
                DBG.blogInfo("succesfully Mated " + matelist[i] + " with mates " + string.Join(", ", matelist));

            }
        }

        public static void UnpackAndOverwriteMates()
        {
            DBG.blogDebug("Unpacking Mates");
            Plugin.CompatMatesList = new Dictionary<string, List<string>>();
            string[] rawmates = Plugin.rawMatesList.ToArray();
            foreach (string str in rawmates)
            {
                //DBG.blogDebug(rawmates);
                SetCompatMates(SplitMates(str));
            }
            DBG.blogDebug("Mates Unpacked");
        }

        public static void UnpackAndOverwriteTrades()
        {
            DBG.blogDebug("Unpacking Trades");
            Plugin.RecruitList = new Dictionary<string, List<TradeAmount>>();
            List<string> rawtrades = Plugin.rawTradesList;

            foreach (string str in rawtrades)
            {
                DBG.blogDebug(rawtrades.ToString());
                AddTradeList(str.Split(',')[0], str.Split(',')[1],true);
            }
            DBG.blogDebug("Trades Unpacked");
        }
        public static string[] SplitMates(string fullstr)
        {
            string[] strArr = { };
            if (!fullstr.Contains(":"))
            {
                strArr.Append(fullstr);
                return strArr;
            }
            strArr = fullstr.Split(':');
            return strArr;
        }

        public static Plugin.TameTable ArrToTametable(string[] arr)
        {
            Plugin.TameTable tmtbl;
            try 
            {
                if (float.Parse(arr[1]) == -1)
                {
                    tmtbl = arr.Select(Array => new Plugin.TameTable
                    { tamingTime = float.Parse(arr[1]) }).ToList()[0];
                    return tmtbl;
                }

            }
            catch
            {
            }
            try
            {
                if (arr[1].ToLower() == "trade")
                {
                    tmtbl = new Plugin.TameTable();
                    tmtbl.tamingTime = -2;
                    tmtbl.consumeItems = "";
                    tmtbl.procretion = false;
                    AddTradeList(arr[0],arr[2],false);
                    return tmtbl;
                }

            }
            catch
            {
            }
            /*
            tmtbl = arr.Select(Array => new Plugin.TameTable
            {
                commandable = (arr[1] == "true")
                ,
                tamingTime = float.Parse(arr[2])
                ,
                fedDuration = float.Parse(arr[3])
                ,
                consumeRange = float.Parse(arr[4])
                ,
                consumeSearchInterval = float.Parse(arr[5])
                ,
                consumeHeal = float.Parse(arr[6])
                ,
                consumeSearchRange = float.Parse(arr[7])
                ,
                consumeItems = arr[8]
                ,
                changeFaction = (arr[9] == "true")
                ,
                procretion = (arr[10] == "true")
                ,
                maxCreatures = (int.Parse(arr[11]))
                ,
                pregnancyChance = float.Parse(arr[12])
                ,
                pregnancyDuration = float.Parse(arr[13])
                ,
                growTime = float.Parse(arr[14])

            }).ToList()[0];
            */

            tmtbl = new Plugin.TameTable();
            String strFailed = "Failed Setting: ";
            String strbase = strFailed;
            try { tmtbl.commandable = (arr[1] != "false"); } catch { strFailed += "commandable, "; }
            
            try { tmtbl.tamingTime = float.Parse(arr[2]); } catch { strFailed += "tamingtime, "; }
            try { tmtbl.fedDuration = float.Parse(arr[3]); } catch { strFailed += "fedduration, "; }
            try { tmtbl.consumeRange = float.Parse(arr[4]); } catch { strFailed += "consumerange, "; }
            try { tmtbl.consumeSearchInterval = float.Parse(arr[5]); } catch { strFailed += "consumesearchinterval, "; }
            try { tmtbl.consumeHeal = float.Parse(arr[6]); } catch { strFailed += "consumeheal, "; }
            try { tmtbl.consumeSearchRange = float.Parse(arr[7]); } catch { strFailed += "consumesearchrange, "; }
            try { tmtbl.consumeItems = arr[8]; } catch { strFailed += "consumeitems, "; }
            try { tmtbl.changeFaction = (arr[9] == "true"); } catch { strFailed += "changefaction, "; }
            if (arr[10] == "overwrite")
            {
                tmtbl.procretion = true;
                tmtbl.procretionOverwrite = true;
            }
            else
            {
                try { tmtbl.procretion = (arr[10] == "true"); } catch { strFailed += "procreation, "; }
            }
            
            try { tmtbl.maxCreatures = (int.Parse(arr[11])); } catch { strFailed += "tamingtime, "; }
            try { tmtbl.pregnancyChance = float.Parse(arr[12], CultureInfo.InvariantCulture.NumberFormat); } 
            catch { strFailed += "pregchance, "; }
            try { tmtbl.pregnancyDuration = float.Parse(arr[13]); } catch { strFailed += "changefaction, "; }
            try { tmtbl.growTime = float.Parse(arr[14]); } catch { strFailed += "procreation, "; }
            if (!isValidBool(arr[1])) { strFailed += "commandable(not true or false), "; }
            if (!isValidBool(arr[9])) { strFailed += "changeFaction(not true or false), "; }
            if (!isValidBool(arr[10])) { strFailed += "procretion(not true or false), "; }
            if (strFailed != strbase)
            {
                DBG.blogWarning(arr[0] + ": "+ strFailed);
            }

            return tmtbl;
        }

        public static bool isValidBool(string bool_str)
        {
            if (bool_str == "true" | bool_str == "false")
            {
                return true;
            }
            return false;
        }

        public static void AddTradeList(string creaturename, string rawstr, bool fromServer)
        {
            
            string[] trade_combined = rawstr.Split(':');
            string[] split_trade;
            List<TradeAmount> trdList = new List<TradeAmount>();
            foreach ( string str_group in trade_combined)
            {
                split_trade = str_group.Split('=');
                //DBG.blogDebug(split_trade[0] + " with amount of " + split_trade[1]);
                TradeAmount trdAmt = new TradeAmount();
                trdAmt.tradeItem = split_trade[0];
                try { trdAmt.tradeAmt = (int.Parse(split_trade[1])); } catch {
                    trdAmt.tradeAmt = 1;
                    DBG.blogWarning(split_trade[1] +" is not a valid amount for trade, setting amount to 1"); }
                trdList.Add(trdAmt);
            }
            string[] prefablist = SplitMates(creaturename);
            if(prefablist.Count() == 0)
            {
                prefablist = new string[1] { creaturename };
            }
            //DBG.blogDebug("creaturename=" + creaturename);
            //DBG.blogDebug("prefablist.Count()=" + prefablist.Count());
            for (int i = 0; i < prefablist.Count(); i++)
            {
                if (!Plugin.RecruitList.ContainsKey(prefablist[i]))
                {
                    Plugin.RecruitList.Add(prefablist[i], trdList);
                    if (!fromServer) { Plugin.rawTradesList.Add(prefablist[i] + "," + rawstr); }
                    DBG.blogDebug("Trades:"+prefablist[i] + "," + rawstr);
                }
                else
                {
                    DBG.blogWarning("Already tradelist set for "+prefablist[i]);
                }
            }
            
        }
    }
}

/*
            public bool commandable { get; set; } = true;
			public float tamingTime { get; set; } = 600f;
			public float fedDuration { get; set; } = 300f;
			public float consumeRange { get; set; } = 2f;
			public float consumeSearchInterval { get; set; } = 5f;
			public float consumeHeal { get; set; } = 10f;
			public float consumeSearchRange { get; set; } = 30f;
			public string consumeItems { get; set; } = "RawMeat";
			public bool changeFaction { get; set; } = true;
			public bool procretion { get; set; } = true;
			public int maxCreatures { get; set; } = 5;
			public float pregnancyChance { get; set; } = 0.33f;
			public float pregnancyDuration { get; set; } = 10f;
			public float growTime { get; set; } = 60f;
			public object Clone()
			{
				return MemberwiseClone();
			}
*/