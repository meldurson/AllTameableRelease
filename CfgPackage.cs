using BepInEx;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace AllTameable.RPC
{
    [Serializable]
    internal class CfgPackage
    {
        //public Dictionary<string, Plugin.TameTable> cfgList;

        //public string teststring = "teststring";

        public string[] split_keys;
        public Plugin.TameTable[] split_tmtbl;
        public string[] mate_dict;
        public ZPackage Pack()
        {
            //cfgList = Plugin.cfgList;
            SplitTameDict splitdict = createsplitdict();
            split_keys = splitdict.keys;
            split_tmtbl = splitdict.tmtbl;
            mate_dict = Plugin.rawMatesList;
            //DBG.blogDebug("split tameDict");
            DBG.blogDebug("keysnull=" + (split_keys == null));
            DBG.blogDebug("tmblnull=" + (split_tmtbl == null));
            
            foreach (string cell in split_keys)
            {
                DBG.blogDebug("key=" + cell);
            }
            foreach (Plugin.TameTable cell in split_tmtbl)
            {
                DBG.blogDebug("consume=" + cell.consumeItems);
            }
            
            ZPackage packedpkg = new ZPackage();
            using (MemoryStream memstream = new MemoryStream())
            {
                using (GZipStream serialstream = new GZipStream(memstream, CompressionLevel.Optimal))
                {
                    new BinaryFormatter().Serialize(serialstream, this);
                }
                byte[] buffer = memstream.GetBuffer();
                packedpkg.Write(buffer);
                return packedpkg;
            }
        }


        public static void Unpack(ZPackage package)
        {
            byte[] array = package.ReadByteArray();
            DBG.blogInfo($"Deserializing {array.Length} bytes of configs");
            using (MemoryStream stream = new MemoryStream(array))
            {
                using (GZipStream serializationStream = new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true))
                {
                    CfgPackage configPackage = new BinaryFormatter().Deserialize(serializationStream) as CfgPackage;
                    if (configPackage != null)
                    {
                        DBG.blogInfo("Received and deserialized config package");
                        DBG.blogInfo("Unpackaging configs.");
                        //Plugin.cfgList = configPackage.cfgList;
                        //DBG.blogWarning(configPackage.teststring);
                        string[] newsplit_keys = configPackage.split_keys;
                        Plugin.TameTable[] newsplit_tmtbl = configPackage.split_tmtbl;
                        Combinesplitdict(newsplit_keys, newsplit_tmtbl);
                        Plugin.rawMatesList = configPackage.mate_dict;
                        //ConfigurationManager.GeneralConfig = configPackage.GeneralConfig;
                        //ConfigurationManager.SimpleConfig = configPackage.SimpleConfig;
                        //ConfigurationManager.SpawnSystemConfig = configPackage.SpawnSystemConfig;
                        //ConfigurationManager.CreatureSpawnerConfig = configPackage.CreatureSpawnerConfig;
                        DBG.blogInfo("Successfully unpacked configs.");
                        DBG.blogInfo("Unpacked general config");
                        //DBG.blogWarning($"Unpacked {(ConfigurationManager.CreatureSpawnerConfig?.Subsections?.Count).GetValueOrDefault()} local spawner entries");
                        //DBG.blogWarning($"Unpacked {(ConfigurationManager.SpawnSystemConfig?.Subsections?.Values?.FirstOrDefault()?.Subsections?.Count).GetValueOrDefault()} world spawner entries");
                        //DBG.blogWarning($"Unpacked {(ConfigurationManager.SimpleConfig?.Subsections?.Count).GetValueOrDefault()} simple entries");
                    }
                    else
                    {
                        DBG.blogWarning("Received bad config package. Unable to load.");
                    }
                }
            }
        }

        public class SplitTameDict
        {
            public string[] keys { get; set; } = { };
            public Plugin.TameTable[] tmtbl { get; set; } = { };
        }
        

        public SplitTameDict createsplitdict()
        {
            //DBG.blogWarning("in splitdict");
            int listsize = Plugin.cfgList.Count;
            SplitTameDict splitdict2 = new SplitTameDict();
            splitdict2.keys = new string[listsize];
            splitdict2.tmtbl = new Plugin.TameTable[listsize];
            //DBG.blogWarning("size of keys is " + splitdict2.keys.Length +" and listsize is "+ listsize);
            int i = 0;
            foreach (KeyValuePair<string, Plugin.TameTable> item in Plugin.cfgList)
            {
                splitdict2.keys[i] = item.Key;
                splitdict2.tmtbl[i] = item.Value;
                //DBG.blogWarning(item.Key);
                i += 1;
            }

            return splitdict2;
        }

        private static void Combinesplitdict(string[] keys, Plugin.TameTable[] tmtbl)
        {
            //bool succ = false;
            //string[] loc_split_keys = split_keys;
            //DBG.blogWarning("in combinedict");
            int listsize = keys.Length;
            //SplitTameDict splitdict2 = new SplitTameDict();
            //splitdict2.keys = new string[listsize];
            //splitdict2.tmtbl = new Plugin.TameTable[listsize];
            //DBG.blogWarning("size of keys is " + splitdict2.keys.Length + " and listsize is " + listsize);
            //int i = 0;
            Plugin.cfgList.Clear();
            for (int i = 0; i < listsize; i++)
            {
                Plugin.cfgList.Add(keys[i], tmtbl[i]);
                //DBG.blogWarning(tmtbl[i].consumeItems);
                //i += 1;
                //DBG.blogDebug("Added to cfglist: " + keys[i]);
            }


        }

    }
}
