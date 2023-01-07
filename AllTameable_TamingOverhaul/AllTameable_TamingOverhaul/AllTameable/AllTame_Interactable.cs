using HarmonyLib;
using System;
//using CreatureLevelControl;
using System.Collections.Generic;
using UnityEngine;


namespace AllTameable
{
    
    
    [Serializable]
    public class TradeAmount
    {
        public string tradeItem ;
        public int tradeAmt ;
    }


    public class AllTame_Interactable : MonoBehaviour, Hoverable, Interactable
    {

        public Character m_character;
        public ZNetView m_nview;
        //public Plugin.tradeAmount[] tradelist;
        public  Dictionary<string, int> tradelist = new Dictionary<string, int>();

        public string[] tradeItems = new string[20];
        public int[] tradeAmounts = new int[20];





        public void Awake()
        {
            ZNetScene zns = ZNetScene.instance;
            m_nview = GetComponent<ZNetView>();
            m_character = GetComponent<Character>();
            createTradeList();

            foreach (KeyValuePair<string, int> item in tradelist)
            { 
                    DBG.blogDebug(m_character.name + ": Allowed trade: " + item.Key + " with amount "+ item.Value);
            }




        }
        public void createTradeList()
        {
            int tradeAmt;
            int listsize = tradeItems.Length;
            for(int i = 0; i < listsize; i++)
            {
                try
                {
                    tradeAmt = tradeAmounts[i];
                }
                catch
                {
                    tradeAmt = 1;
                }
                if (!tradelist.ContainsKey(tradeItems[i]))
                {
                    tradelist.Add(tradeItems[i], tradeAmt);
                }
                
            }
            
        }

        public virtual string GetHoverText()
        {
            Tameable component = GetComponent<Tameable>();
            if ((bool)component)
            {
                return component.GetHoverText();
            }
            return "";
        }

        public virtual string GetHoverName()
        {
            Tameable component = GetComponent<Tameable>();
            if ((bool)component)
            {
                return component.GetHoverName();
            }
            return Localization.instance.Localize(m_character.m_name);
        }
        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            if (!m_nview.IsValid())
            {
                return false;
            }
            try
            {
            foreach (KeyValuePair<string, int> item2 in tradelist)
            {
                DBG.blogDebug("Allowed Trades: " + item2.Key + " with amount " + item2.Value);
            }
                DBG.blogDebug("Tradelist size="+tradelist.Count);
                DBG.blogDebug("item.m_dropPrefab.name=" + item.m_dropPrefab.name); //item using
                if(tradelist.TryGetValue(item.m_dropPrefab.name,out int tradeAmount))
                { 
                Inventory inv = user.GetInventory(); //get players inventory
                    int numininventory = inv.CountItems(item.m_shared.m_name); // checks how many cores player has
                    if (numininventory >= tradeAmount)
                    {
                        DBG.blogDebug("Attempting Trade");
                        if (!m_character.IsTamed())
                        {
                            Tameable tame = new Tameable();
                            tame = m_character.gameObject.GetComponent<Tameable>();

                            if (tame != null)
                            {
                                tame.Tame();
                                bool itemremoved = inv.RemoveItem(item, tradeAmount);
                                DBG.blogDebug("Recruited "+m_character.m_name);
                            }
                            else
                            {
                                DBG.blogDebug("Not tameable");
                                return false;
                            }
                        }
                        else 
                        {
                            return false;
                        }
                    }
                    else
                    {
                        DBG.blogDebug("Not enough "+item.m_shared.m_name+ ", need " + tradeAmount);
                        user.Message(MessageHud.MessageType.Center, "Not enough " + item.m_shared.m_name + ", need " + tradeAmount);

                    }
                }
            else
            {
                DBG.blogDebug(item.m_dropPrefab.name + " is not in trade list");
                return false;
            }

            }
            catch (Exception ex){}
            return true;
        }

        

    }
}
