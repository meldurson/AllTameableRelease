using BepInEx;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

//using On;
namespace AllTameable
{
    internal interface InManager
    {
        void Init();
    }

    public class PrefabInManager : InManager
    {
        private static PrefabInManager _instance;
        internal GameObject PrefabContainer;
        public static PrefabInManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PrefabInManager();
                }
                return _instance;
            }
        }
        public void Init()
        {
            PrefabContainer = new GameObject("Prefabs");
            PrefabContainer.transform.parent = Plugin.Root.transform;
            PrefabContainer.SetActive(value: false);
            //On.ZNetScene.Awake += RegisterAllToZNetScene;
            //SceneManager.sceneUnloaded += delegate;

        }

        public void ZnetSceneReg(GameObject go)
        {
            ZNetScene zns = ZNetScene.instance;
            if (!zns)
            {
                return;
            }
            string name = go.name;
            int hash = name.GetStableHashCode();
            if (zns.m_namedPrefabs.ContainsKey(hash))
            {
                DBG.blogWarning("Prefab " + name + " already is registered");

            }
            if (go.GetComponent<ZNetView>() != null)
            {
                zns.m_prefabs.Add(go);
            }
            else
            {
                zns.m_nonNetViewPrefabs.Add(go);
            }
            zns.m_namedPrefabs.Add(hash, go);
            DBG.blogWarning("Prefab " + name + " registered");
        }
    }
}