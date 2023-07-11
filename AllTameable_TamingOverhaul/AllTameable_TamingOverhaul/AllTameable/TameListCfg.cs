using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AllTameable
{
    internal class TameListCfg
    {
        public static Dictionary<string, Plugin.TameTable> cfgList2 = new Dictionary<string, Plugin.TameTable>();

        public static string pathcfg = Path.GetDirectoryName(Paths.BepInExConfigPath) + Path.DirectorySeparatorChar + "AllTameable_TameList.cfg";

        public static Plugin.TameTable defaultTable;

        public static void print_tmtbl(string prefname, Plugin.TameTable tbl)
        {
            //commandable,tamingTime,fedDuration,consumeRange,consumeSearchInterval,consumeHeal,consumeSearchRange,consumeItem:consumeItem,changeFaction,procretion,maxCreatures,pregnancyChance,pregnancyDuration,growTime
            string tempstr = prefname + ",";
            try
            {
                
                tempstr += tbl.commandable + ",";
                tempstr += tbl.tamingTime + ",";
                tempstr += tbl.fedDuration + ",";
                tempstr += tbl.consumeRange + ",";
                tempstr += tbl.consumeSearchInterval + ",";
                tempstr += tbl.consumeHeal + ",";
                tempstr += tbl.consumeSearchRange + ",";
                tempstr += tbl.consumeItems + ",";
                tempstr += tbl.changeFaction + ",";
                tempstr += tbl.procretion + ",";
                tempstr += tbl.maxCreatures + ",";
                tempstr += tbl.pregnancyChance + ",";
                tempstr += tbl.pregnancyDuration + ",";
                tempstr += tbl.growTime + ",";
                tempstr += "canmatewithself="+tbl.canMateWithSelf + ",";
            }
            catch { }
            DBG.blogDebug(tempstr);

        }
        public static bool create_TamelistCFG()
        {
            string[] matches = Directory.GetFiles(@Path.GetDirectoryName(Paths.BepInExConfigPath), "AllTameable_TameList*.cfg");
            if (matches.Count() > 0)
            {
                DBG.blogWarning("There are already AllTameable_TameList in your config folder, check to make sure they are formatted correctly");
                DBG.blogWarning("The files are:");
                foreach (string match in matches)
                {
                    DBG.blogWarning(match);
                }

                return false;
            }
            string cfgpath = Plugin.cfgPath;
            //DBG.blogDebug("cfgpath="+ cfgpath);
            StreamReader streamreader = new StreamReader(cfgpath);
            string sline;
            bool found_setting = false;
            string[] arr = new string[0];
            DBG.blogDebug("attempt to read meldurson.valheim.AllTameable");
            while ((sline = streamreader.ReadLine()) != null && !found_setting)
            {
                sline = sline.Trim();
                if (!sline.StartsWith("Settings"))
                {
                    //DBG.blogDebug("sline="+sline);
                    continue;
                }
                sline = sline.Replace("TRUE", "true").Replace("FALSE", "false").Replace(" ", "");
                arr = sline.Split('=')[1].Split(';');
                found_setting = true;
            }

            if (arr.Count() > 0)
            {
                string createfile_path = Path.Combine(@Path.GetDirectoryName(Paths.BepInExConfigPath), Path.GetFileName("AllTameable_TameList_From_Config.cfg"));
                DBG.blogDebug("createfile_path=" + createfile_path);
                foreach (string text in arr)
                {
                    DBG.blogDebug("entry=" + text);
                }
                using (StreamWriter sw = File.AppendText(createfile_path))
                {
                    foreach (string text in arr)
                    {
                        sw.WriteLine(text);
                    }
                }
            }
            else
            {
                string createfile_path = Path.Combine(@Path.GetDirectoryName(Paths.BepInExConfigPath), Path.GetFileName("AllTameable_TameList_From_Default.cfg"));
                DBG.blogDebug("createfile_path=" + createfile_path);
                DBG.blogDebug("Did not find Setting in config, creating from default");
                ;
                using (StreamWriter sw = File.CreateText(createfile_path))
                {
                    sw.Write(loadDefaultConfig());
                }
            }

            return true;
        }

        private static string loadDefaultConfig()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = assembly.GetManifestResourceNames().Single(str => str.EndsWith("defaultTamelist.txt"));
            string result = "";
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                result = reader.ReadToEnd();
            }
            DBG.blogDebug("result=" + result);
            return result;


        }
           
        private static void readTamelist(string path)
        {
            StreamReader streamreader = new StreamReader(path);
            string sline;
            defaultTable = new Plugin.TameTable();
            while ((sline = streamreader.ReadLine()) != null)
            {
                sline = sline.Trim();
                if (sline.Length == 0 || sline.StartsWith("#"))
                {
                    continue;
                }
                sline = sline.Replace("TRUE", "true").Replace("FALSE", "false").Replace(";", "").Replace(" ", "");
                string[] arr = sline.Split(',');
                try
                {
                    Plugin.TameTable temptbl = ArrToTametable(arr);
                    //DBG.blogDebug("gotoutofarrtotmtbl");
                    if (arr[0].StartsWith("*"))
                    {
                        defaultTable = (Plugin.TameTable)temptbl.Clone(); //sets the default config
                        print_tmtbl(arr[0], temptbl);
                    }
                    else
                    {
                        Plugin.TameTable tbl_toAdd = (Plugin.TameTable)temptbl.Clone();
                        string[] prefablist = SplitMates(arr[0]);
                        
                        if (prefablist.Count() > 1)
                        {
                            DBG.blogDebug("prefablist=" + string.Join(",", prefablist));
                            //DBG.blogDebug("Count >0: "+ arr[0]);
                            Plugin.rawMatesList.Add(arr[0]);

                                SetCompatMates(prefablist);
                            if (arr.Count() > 1)
                            {
                                for (int i = 0; i < prefablist.Count(); i++)
                                {
                                    if (!cfgList2.ContainsKey(prefablist[i]))
                                    {
                                        cfgList2.Add(prefablist[i], tbl_toAdd);
                                        print_tmtbl(prefablist[i], tbl_toAdd);
                                        DBG.blogInfo("succesfully added " + prefablist[i] + " with mates " + string.Join(", ", prefablist));
                                    }
                                }
                            }
                            else
                            {
                                DBG.blogDebug("Skipping tame, added mates");
                            }
                        }
                        else
                        {
                            if (!cfgList2.ContainsKey(arr[0]))
                            {
                                cfgList2.Add(arr[0], tbl_toAdd);
                                print_tmtbl(arr[0], tbl_toAdd);
                                DBG.blogInfo("succesfully added " + arr[0] + " to the tametable");
                            }
                            else
                            {
                                DBG.blogWarning(arr[0] + " is already in the tametable");
                            }
                        }

                    }
                }
                catch
                {
                    DBG.blogWarning("Failed to add tametablecfg for " + arr[0]);
                }

                /*
                foreach (KeyValuePair<string, Plugin.TameTable> item in cfgList2)
                {
                    string key = item.Key;
                    print_tmtbl(key, item.Value);
                }
                */


            }
            
        }
        public static bool Init()
        {
            bool loaded_tamelist = false;
            defaultTable = new Plugin.TameTable();
            cfgList2.Clear();
            string[] matches = Directory.GetFiles(@Path.GetDirectoryName(Paths.BepInExConfigPath), "AllTameable_TameList*.cfg");
            foreach (string match in matches)
            {
                if (!File.Exists(match))
                {
                    DBG.blogDebug("Failed to Load: " + match);
                }
                else
                {
                    DBG.blogInfo("Loaded: " + match.Split(Path.DirectorySeparatorChar).Last());
                    loaded_tamelist = true;
                    readTamelist(match);
                }
            }
            if (!loaded_tamelist)
            {
                return false;
            }
            else
            {
                DBG.blogDebug("rawmateslist=" + string.Join(",", Plugin.rawMatesList));
                DBG.blogDebug("rawtradelist= " + string.Join(",", Plugin.rawTradesList));
                Plugin.cfgList = cfgList2;
                return true;
            }
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
                if (Plugin.CompatMatesList.ContainsKey(matelist[i]))
                {
                    foreach(string mate in otherPrefabs)
                    {
                        Plugin.CompatMatesList[matelist[i]].Add(mate);
                    }
                }
                else
                {
                    Plugin.CompatMatesList.Add(matelist[i], otherPrefabs);
                }
                DBG.blogInfo("succesfully Mated " + matelist[i] + " with mates " + string.Join(", ", otherPrefabs));

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
                //DBG.blogDebug(rawtrades.ToString());
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

            int arr_len = arr.Length;
            tmtbl = (Plugin.TameTable)defaultTable.Clone();
            String strFailed = "Failed Setting: ";
            String strbase = strFailed;
            bool started_manual = false;
            //DBG.blogDebug("arr_len="+ arr_len);
            for (int i=1;i<=arr_len; i++) // may be out of index for 0 length
            {
                string error_property ="";
                try
                {
                    //bool direct_assign = false;
                    string direct_value = arr[i];
                    if (direct_value.Length == 0)
                    {
                        //DBG.blogDebug("0length");
                    }
                    else
                    {
                        string switch_index = i.ToString(); //switch index to i
                        //DBG.blogDebug("switch_index="+ switch_index);
                        if (arr[i].Contains("="))
                        {
                            //DBG.blogDebug("is manual");
                            started_manual = true;
                            string[] split_str = arr[i].Split('=');
                            string prop_key = split_str[0];
                            //direct_assign = true;
                            direct_value = split_str[1];
                            switch_index = prop_key; //set switch to key

                            //names of each property that can be assigned as float
                            string[] float_props = new string[] { "tamingTime", "fedDuration", "consumeRange",
                            "consumeSearchInterval","consumeHeal","consumeSearchRange","maxCreatures","pregnancyChance",
                            "pregnancyDuration", "growTime","size"};
                            error_property = prop_key + ", ";
                            if (float_props.Contains(prop_key))
                            {
                                //DBG.blogDebug("prop_key=" + prop_key+ ", direct_value=" + direct_value);
                                PropertyInfo prop = tmtbl.GetType().GetProperty(prop_key, BindingFlags.Public | BindingFlags.Instance);
                                if (null != prop && prop.CanWrite) //&& prop.CanWrite
                                {
                                    prop.SetValue(tmtbl, float.Parse(direct_value, CultureInfo.InvariantCulture.NumberFormat), null);
                                }
                                else
                                {
                                    DBG.blogWarning("could not write value "+ prop_key);
                                }
                            }
                            else if (prop_key == "consumeItems")
                            {
                                tmtbl.consumeItems = direct_value;
                            }
                            else if (prop_key == "offspringOnly")
                            {
                                if (!isValidBool(direct_value)) { strFailed += "offspringOnly(not true or false), "; }
                                tmtbl.offspringOnly = (direct_value != "false");
                            }
                            else if (prop_key == "changeFaction")
                            {
                                if (!isValidBool(direct_value)) { strFailed += "changeFaction(not true or false), "; }
                                tmtbl.changeFaction = (direct_value == "true");
                            }
                            else if (prop_key == "commandable")
                            {
                                if (!isValidBool(direct_value)) { strFailed += "commandable(not true or false), "; }
                                tmtbl.commandable = (direct_value != "false");
                            }
                            else if (prop_key == "procretion")
                            {
                                if (!isValidBool(direct_value)) { strFailed += "procretion(not true or false), "; }
                                if (direct_value == "overwrite")
                                {
                                    tmtbl.procretion = true;
                                    tmtbl.procretionOverwrite = true;
                                }
                                else
                                {
                                    tmtbl.procretion = (direct_value == "true");
                                }
                            }
                            else if (prop_key == "canMateWithSelf")
                            {
                                if (!isValidBool(direct_value)) { strFailed += "canMateWithSelf(not true or false), "; }
                                tmtbl.canMateWithSelf = (direct_value == "true");
                            }
                            else if (prop_key == "specificOffspring")
                            {
                                tmtbl.specificOffspringString += ","+direct_value;
                                
                            }
                        }
                        if (!started_manual)
                        {
                            switch (switch_index)
                            {
                                case "1":
                                case "commandable":
                                    error_property = "commandable, ";
                                    tmtbl.commandable = (direct_value != "false");
                                    if (!isValidBool(direct_value)) { strFailed += "commandable(not true or false), "; }
                                    break;
                                case "2":
                                case "tamingTime":
                                    error_property = "tamingTime, ";
                                    tmtbl.tamingTime = float.Parse(direct_value);
                                    break;
                                case "3":
                                case "fedDuration":
                                    error_property = "fedDuration, ";
                                    tmtbl.fedDuration = float.Parse(direct_value);
                                    break;
                                case "4":
                                case "consumeRange":
                                    error_property = "consumeRange, ";
                                    tmtbl.consumeRange = float.Parse(direct_value);
                                    break;
                                case "5":
                                case "consumeSearchInterval":
                                    error_property = "consumeSearchInterval, ";
                                    tmtbl.consumeSearchInterval = float.Parse(direct_value);
                                    break;
                                case "6":
                                case "consumeHeal":
                                    error_property = "consumeHeal, ";
                                    tmtbl.consumeHeal = float.Parse(direct_value);
                                    break;
                                case "7":
                                case "consumeSearchRange":
                                    error_property = "consumeSearchRange, ";
                                    tmtbl.consumeSearchRange = float.Parse(direct_value);
                                    break;
                                case "8":
                                case "consumeItems":
                                    error_property = "consumeItems, ";
                                    tmtbl.consumeItems = direct_value;
                                    break;
                                case "9":
                                case "changeFaction":
                                    error_property = "changeFaction, ";
                                    tmtbl.changeFaction = (direct_value == "true");
                                    if (!isValidBool(direct_value)) { strFailed += "changeFaction(not true or false), "; }
                                    break;
                                case "10":
                                case "procretion":
                                    error_property = "procretion, ";
                                    if (direct_value == "overwrite")
                                    {
                                        tmtbl.procretion = true;
                                        tmtbl.procretionOverwrite = true;
                                    }
                                    else
                                    {
                                        tmtbl.procretion = (direct_value == "true");
                                    }
                                    if (!isValidBool(direct_value)) { strFailed += "procretion(not true or false), "; }
                                    break;
                                case "11":
                                case "maxCreatures":
                                    error_property = "maxCreatures, ";
                                    tmtbl.maxCreatures = int.Parse(direct_value);
                                    break;
                                case "12":
                                case "pregnancyChance":
                                    error_property = "pregnancyChance, ";
                                    tmtbl.pregnancyChance = float.Parse(direct_value, CultureInfo.InvariantCulture.NumberFormat);
                                    break;
                                case "13":
                                case "pregnancyDuration":
                                    error_property = "pregnancyDuration, ";
                                    tmtbl.pregnancyDuration = float.Parse(direct_value);
                                    break;
                                case "14":
                                case "growTime":
                                    error_property = "growTime, ";
                                    tmtbl.growTime = float.Parse(direct_value);
                                    break;


                                default:
                                    error_property = "Failed and set to Default";
                                    DBG.blogDebug("failed, set default");
                                    break;
                            }
                        }
                    }
                }
                catch
                {
                    strFailed += error_property;
                }
            }
            //DBG.blogDebug("isendofarray");

            /*
            if (arr_len > 1) { try { tmtbl.commandable = (arr[1] != "false"); } catch { strFailed += "commandable, "; } }
            if (arr_len > 2) { try { tmtbl.tamingTime = float.Parse(arr[2]); } catch { strFailed += "tamingtime, "; } }
            if (arr_len > 3) { try { tmtbl.fedDuration = float.Parse(arr[3]); } catch { strFailed += "fedduration, "; } }
            if (arr_len > 4) { try { tmtbl.consumeRange = float.Parse(arr[4]); } catch { strFailed += "consumerange, "; } }
            if (arr_len > 5) { try { tmtbl.consumeSearchInterval = float.Parse(arr[5]); } catch { strFailed += "consumesearchinterval, "; } }
            if (arr_len > 6) { try { tmtbl.consumeHeal = float.Parse(arr[6]); } catch { strFailed += "consumeheal, "; } }
            if (arr_len > 7) { try { tmtbl.consumeSearchRange = float.Parse(arr[7]); } catch { strFailed += "consumesearchrange, "; } }
            if (arr_len > 8) { try { tmtbl.consumeItems = arr[8]; } catch { strFailed += "consumeitems, "; } }
            if (arr_len > 9) { try { tmtbl.changeFaction = (arr[9] == "true"); } catch { strFailed += "changefaction, "; } }
            if (arr_len > 10) { if (arr[10] == "overwrite")
            {
                tmtbl.procretion = true;
                tmtbl.procretionOverwrite = true;
            }
            else
            {
                try { tmtbl.procretion = (arr[10] == "true"); } catch { strFailed += "procreation, "; }
            }
            }
            if (arr_len > 11) { try { tmtbl.maxCreatures = (int.Parse(arr[11])); } catch { strFailed += "maxCreatures, "; } }
            if (arr_len > 12) { try { tmtbl.pregnancyChance = float.Parse(arr[12], CultureInfo.InvariantCulture.NumberFormat); } 
            catch { strFailed += "pregchance, "; }}
            if (arr_len > 13) { try { tmtbl.pregnancyDuration = float.Parse(arr[13]); } catch { strFailed += "pregnancyDuration, "; } }
            if (arr_len > 14) { try { tmtbl.growTime = float.Parse(arr[14]); } catch { strFailed += "growTime, "; }}
            if (arr_len > 1) { if (!isValidBool(arr[1])) { strFailed += "commandable(not true or false), "; } }
            if (arr_len > 9) { if (!isValidBool(arr[9])) { strFailed += "changeFaction(not true or false), "; } }
            if (arr_len > 10) { if (!isValidBool(arr[10])) { strFailed += "procretion(not true or false), "; } }
            if (strFailed != strbase)
            {
                DBG.blogWarning(arr[0] + ": "+ strFailed);
            }
            */
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