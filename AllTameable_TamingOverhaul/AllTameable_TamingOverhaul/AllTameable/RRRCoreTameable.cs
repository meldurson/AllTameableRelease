using RRRCore;
using RRRCore.prefabs._0_2_0;
using UnityEngine;
using HarmonyLib;
using RRRNpcs;
namespace AllTameable.RRRCoreTameable
{
    internal class RRRCoreTameable
    {/*
        [HarmonyPatch(typeof(RRRMobData), "Make")]
        private static class Postfix_Make
        {

            private static void Postfix(ref GameObject __result)
            {
                if (__result.name.Contains("RRRN"))
                {
                    DBG.blogDebug("in Make Postfix");
                    DBG.blogDebug(__result.name);

                    Transform trans = __result.transform;
                    int childcount = trans.childCount;
                    for(int i = 0; i < childcount; i++)
                    {
                        DBG.blogDebug(trans.GetChild(i).name);
                    }
                    string prefName = __result.name;
                    if (prefName.Contains("(Clone"))
                    {
                        prefName = Utils.GetPrefabName(__result);
                    }
                    if (Plugin.PostMakeList.Contains(prefName))
                    {
                        DBG.blogDebug("in PostMakeList");
                        PetManager.LateSetMini(__result);
                        Procreation proc = __result.GetComponent<Procreation>();
                        if (proc != null)
                        {
                            if (proc.m_offspring != null)
                            {
                                Plugin.PostMakeList.Remove(prefName);
                                DBG.blogDebug("PostList=" + string.Join(",",Plugin.PostMakeList));
                            }
                        }
                    }
                }

            }
        }

        [HarmonyPatch(typeof(RRRNpcs.npc.Npc), "DesignNpcBase")]
        private static class Prefix_Character_SetLevel
        {

            private static void Postfix(ref GameObject clone)
            {
                DBG.blogDebug("in DesignNpcBase Postfix");
                DBG.blogDebug(clone.name);

                Transform trans = clone.transform;
                int childcount = trans.childCount;
                for (int i = 0; i < childcount; i++)
                {
                    DBG.blogDebug(trans.GetChild(i).name);
                }
            }
        }
        */

        [HarmonyPrefix]
        [HarmonyPatch(typeof(RRRMobCustomization), "SetupTameable")]
        private static bool Prefix(ref RRRMobCustomization __instance)
        {
            GameObject go = __instance.gameObject;
            if (go.GetComponent<Tameable>() != null)
            {
                DBG.blogDebug("RRRCore atempting to add Tameable for "+go.name+" although already added, skipping RRR Setup");
                return false;
            }
            if (go.GetComponent<Growup>() != null)
            {
                DBG.blogDebug("isChild, not adding tameable");
                return false;
            }
            return true;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(RRRMobCustomization), "Start")]

        private static void Prefix(ref RRRMobCustomization __instance, ref bool __state)
        {
            __state = false;
            //DBG.blogDebug("in Start");
            //try { DBG.blogDebug("mobDataNotNull=" + __instance.mobData.IsNotNull()); } catch { }
            //try { DBG.blogDebug("CatSpecialNotNull=" + __instance.mobData.Category_Special.IsNotNull()); } catch { }
            if (__instance.mobData.IsNotNull() && __instance.mobData.Category_Special.IsNotNull())
            {
                if (!__instance.monsterAI.IsNotNull())
                {
                    __instance.monsterAI = new MonsterAI();
                    //DBG.blogDebug("Creating Temp MonsterAI");
                    //__state = true;
                }
            }
        }

        public static bool CheckHuman(GameObject go)
        {
            try{ return go.GetComponent<RRRNpcCustomization>().IsNotNull(); }
            catch{return false;}
        }
        /*
        public static bool CheckRRRData(GameObject go)
        {
            string prefName = go.name;
            if (prefName.Contains("(Clone"))
            {
                prefName = Utils.GetPrefabName(go);
            }
            return RRRCustomMobs.GetMobData(prefName).IsNotNull();
        }
        */

        public static GameObject HotFixHuman(GameObject go)
        {
            AllTameable.DBG.blogDebug("in HotfixHuman for "+ go.name);

            Transform trans = go.transform;
            int childcount = trans.childCount;
            bool hasVisual = false;
            bool hasEyePos = false;
            string childName;
            //DBG.blogDebug("Start child count = " + childcount);
            for (int i = childcount-1; i >= 0; i--)
            {
                childName = trans.GetChild(i).name;
                //DBG.blogDebug(trans.GetChild(i).name);
                if (childName == "EyePos")
                {
                    if (hasEyePos){trans.GetChild(i).parent = null;}
                    else{hasEyePos = true;}
                }
                else if (childName == "Visual")
                {
                    if (hasVisual) 
                    {
                        Object.DestroyImmediate(trans.GetChild(i).gameObject);
                        //trans.GetChild(i).parent = null; 
                    }
                    else 
                    {
                        //DBG.blogDebug("inVisual");
                        CharacterAnimEvent charAnim = trans.GetChild(i).GetComponent<CharacterAnimEvent>();
                        charAnim.m_visEquipment = new VisEquipment();
                        //DBG.blogDebug("visequip=" + charAnim.m_visEquipment.IsNotNull());
                        //DBG.blogDebug("Added VisEquipment");
                        hasVisual = true; 
                    }
                }
            }

            childcount = trans.childCount;
            //DBG.blogDebug("Post child count = " + childcount);
            for (int i = childcount - 1; i >= 0; i--)
            {
                //DBG.blogDebug(trans.GetChild(i).name);
            }

            return go;
        }

        /*

            public static GameObject GetMadeNPC(GameObject go)
        {
            string prefName = go.name;
            DBG.blogDebug(prefName);
            if (prefName.Contains("(Clone"))
            {
                prefName = Utils.GetPrefabName(go);
            }
            DBG.blogDebug(prefName);
            RRRMobCustomization rRRMobCust = go.GetComponent<RRRMobCustomization>();
            DBG.blogDebug("RRRMobCust=" + rRRMobCust.IsNotNull());
            rRRMobCust.mobData = RRRCustomMobs.GetMobData(prefName);
            DBG.blogDebug("mobdata ="+rRRMobCust.mobData.IsNotNull());
            if (!rRRMobCust.mobData.IsNotNull())
            {
                DBG.blogDebug("making prefabs");
                RRRCustomMobs.MakePrefabs();
            }
            rRRMobCust.mobData = RRRCustomMobs.GetMobData(prefName);
            DBG.blogDebug("mobdata =" + rRRMobCust.mobData.IsNotNull());
            RRRMobData.Convert(go);
            DBG.blogDebug("converted go");
            return rRRMobCust.mobData.Make();
            
           // return go.GetComponent<RRRNpcCustomization>().IsNotNull();
        }
        */
    }
}
