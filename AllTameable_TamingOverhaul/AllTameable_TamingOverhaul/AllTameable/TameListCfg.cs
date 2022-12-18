using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllTameable
{
    internal class TameListCfg
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
                        Plugin.rawMatesList.Append(arr[0]);
                        //DBG.blogDebug("rawmateslist=" + string.Join(",", Plugin.rawMatesList));
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

        public void UnpackRawMates()
        {
            string[] rawmates = Plugin.rawMatesList;
            foreach (string str in rawmates)
            {
                SetCompatMates(SplitMates(str));
            }
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
            return tmtbl;
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