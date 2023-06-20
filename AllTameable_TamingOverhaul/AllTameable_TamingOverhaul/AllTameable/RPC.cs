using BepInEx;
using HarmonyLib;
using Steamworks;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using Jotunn.Entities;
using System.Collections;
using Jotunn.Managers;

namespace AllTameable.RPC
{
    public class RPC
    {
        public static ZPackage tamelistPkg = null;
        /*
        [HarmonyPatch(typeof(Game), "Start")]3
        public static class GameStartPatch
        {
            private static void Prefix()
            {
                DBG.blogWarning("RPCS instantiated");
                ZRoutedRpc.instance.Register("RequestServerAnnouncement", new Action<long, ZPackage>(RPC.RPC_RequestGetTameableList)); // Our Mock Server Handler
                ZRoutedRpc.instance.Register("EventServerAnnouncement", new Action<long, ZPackage>(RPC.RPC_EventGetTameableList)); // Our Client Function
                ZRoutedRpc.instance.Register("BadRequestMsg", new Action<long, ZPackage>(RPC.RPC_BadRequestMsg)); // Our Error Handler
            }
        }
        */
        //****************************Start New Code**************************

        /*
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ZNet), "OnNewConnection")]
        //private static class Postfix_ZNet_OnNewConnection
        //{
            private static void PrefixOnNewConnection(ref ZNet __instance, ZNetPeer peer)
            {
                DBG.blogDebug("RPC_OnNewConnection");
                if (ZNet.instance.IsServer())
                {
                    DBG.blogDebug("RPC_Received Registered"); //Add rpc to client allowing request data
                    //peer.m_rpc.Register("RPC_RequestConfigsAllTameable", (RPC.RPC_RequestConfigsAllTameable));
                    return;
                }
                DBG.blogDebug("RPC_registered in client"); //add rpc to server allowing to send data
                peer.m_rpc.Register<ZPackage>("RPC_ReceiveConfigsAllTameable", (RPC.RPC_ReceiveConfigsAllTameable));
            }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ZNet), "OnNewConnection")]
        private static void PostfixOnNewConnection(ref ZNet __instance, ZNetPeer peer)
        {
            if (!ZNet.instance.IsServer())
            {
                DBG.blogDebug("Invoking Request");
                peer.m_rpc.Invoke("RPC_RequestConfigsAllTameable");
            }
        }
       
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ZNet), "Connect", new Type[] { typeof(ISocket) })]
        private static void PostfixConnect(ref ZNet __instance,ISocket socket, ref ZNetPeer __result)
        {
            DBG.blogDebug("ZNet Connect");
            
            if (__result != null)
            {
                DBG.blogDebug("Connect not Null");
                if (ZNet.instance.IsServer())
                {
                    DBG.blogDebug("Is Server");
                    __result.m_rpc.Register("RPC_RequestConfigsAllTameable", (RPC.RPC_RequestConfigsAllTameable));
                    return;
                }
                __result.m_rpc.Register<ZPackage>("RPC_ReceiveConfigsAllTameable", (RPC.RPC_ReceiveConfigsAllTameable));
                if (!Plugin.ReceivedServerConfig)
                {
                    DBG.blogDebug("Invoking Request2");
                    __result.m_rpc.Invoke("RPC_RequestConfigsAllTameable");
                }
                else
                {
                    DBG.blogDebug("Already Loaded Request");
                }
                
            }

        }
        */
        //*****Jotunn code

        /*
        // React to the RPC call on a server
        private IEnumerator jot_RPCServerReceive(long sender, ZPackage package)
        {
            Jotunn.Logger.LogMessage($"Received blob, processing");

            string dot = string.Empty;
            for (int i = 0; i < 5; ++i)
            {
                dot += ".";
                Jotunn.Logger.LogMessage(dot);
                yield return OneSecondWait;
            }

            Jotunn.Logger.LogMessage($"Broadcasting to all clients");
            Plugin.jot_TamelistRPC.SendPackage(ZNet.instance.m_peers, new ZPackage(package.GetArray()));
        }

        public static readonly WaitForSeconds HalfSecondWait = new WaitForSeconds(0.5f);
        public static readonly WaitForSeconds OneSecondWait = new WaitForSeconds(1f);

        // React to the RPC call on a client
        private IEnumerator jot_RPCClientReceive(long sender, ZPackage package)
        {
            Jotunn.Logger.LogMessage($"Received blob, processing");
            yield return null;

            string dot = string.Empty;
            for (int i = 0; i < 10; ++i)
            {
                dot += ".";
                Jotunn.Logger.LogMessage(dot);
                yield return HalfSecondWait;
            }
        }

        */
        /*

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ZNet), "OnNewConnection")]
        private static void PostfixOnNewConnection(ref ZNet __instance, ZNetPeer peer)
        {
            if (ZNet.instance.IsServer())
            {
                if (tamelistPkg == null)
                {
                    DBG.blogDebug("Packing Tamelist Pkg");
                    tamelistPkg = new CfgPackage().Pack();
                }
                //peer.m_uid
                DBG.blogDebug("Sending Tamelist RPC");
                Plugin.jot_TamelistRPC.SendPackage(peer.m_uid, tamelistPkg);
                //Plugin.jot_TamelistRPC.Initiate();
            }


        }
        */


            /*

            //****retry code*****
            [HarmonyPrefix]
            [HarmonyPatch(typeof(ZNet), "OnNewConnection")]
            private static void PrefixOnNewConnection(ref ZNet __instance, ZNetPeer peer)
            {
                DBG.blogDebug("RPC_OnNewConnection");
                //if server, register rpc in client to be able to request data and receive data
                //if client, register rpc to be able send data to client
                RegisterListRPC(__instance, peer);

            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(ZNet), "OnNewConnection")]
            private static void PostfixOnNewConnection(ref ZNet __instance, ZNetPeer peer)
            {
                if (ZNet.instance.IsServer())
                {
                    DBG.blogDebug("Postfix Asking Client to Request Server Config");
                    RPC_RequestConfigsAllTameable(peer.m_rpc);
                    //peer.m_rpc.Invoke("RPC_RequestConfigsAllTameable");
                }
                else
                {

                    if (!Plugin.ReceivedServerConfig)
                    {
                        DBG.blogDebug("Requesting Server Config OnNewConnection");
                        peer.m_rpc.Invoke("RPC_RequestConfigsAllTameable");
                    }
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(ZNet), "Connect", new Type[] { typeof(ISocket) })]
            private static void PostfixConnect(ref ZNet __instance, ISocket socket, ref ZNetPeer __result)
            {
                if (!ZNet.instance.IsServer())
                {
                    DBG.blogDebug("Connected");
                    __result.m_rpc.Register<ZNetPeer>("RPC_SelfRegisterListRPC", (RPC.SelfRegisterListRPC));
                    __result.m_rpc.Invoke("RPC_SelfRegisterListRPC", __result);
                }
            }

            */


            private static void RegisterListRPC(ZNet znet, ZNetPeer peer)
        {
            //if server, register rpc in client to be able to request data and receive data
            //if client, register rpc to be able send data to client
            DBG.blogDebug("RPC_Register");
            if (ZNet.instance.IsServer())
            {
                DBG.blogDebug("RPC is Server");
                peer.m_rpc.Register("RPC_RequestConfigsAllTameable", (RPC.RPC_RequestConfigsAllTameable));
                
            }
            else
            {
                DBG.blogDebug("RPC is Client");
                peer.m_rpc.Register<ZPackage>("RPC_ReceiveConfigsAllTameable", (RPC.RPC_ReceiveConfigsAllTameable));
            }
        }

        private static void SelfRegisterListRPC(ZRpc rpc, ZNetPeer peer)
        {
            DBG.blogDebug("Self Registering");
            rpc.Register<ZPackage>("RPC_ReceiveConfigsAllTameable", (RPC.RPC_ReceiveConfigsAllTameable));
        }



        private static void RPC_ReceiveConfigsAllTameable(ZRpc rpc, ZPackage pkg)
        {
            DBG.blogDebug("RPC_received");
            if (!Plugin.ReceivedServerConfig)
            {
                if (!ZNet.instance.IsServer())
                {
                    if (pkg != null && pkg.Size() > 0)
                    { // Confirm our Server is sending the RPC
                      //string announcement = pkg.ReadString();
                        CfgPackage.Unpack(pkg);
                        PetManager.UpdatesFromServer();
                        Plugin.ReceivedServerConfig = true;
                    }
                }
                else
                {
                    DBG.blogDebug("Is Server, not needed");
                }
            }
            else
            {
                DBG.blogDebug("Already Loaded");
            }
        }
        public static void RPC_RequestConfigsAllTameable(ZRpc rpc)
        {
            DBG.blogDebug("RPC_requested");
            if (rpc != null)
            {
                if (tamelistPkg == null)
                {
                    DBG.blogDebug("Packing Tamelist Pkg");
                    tamelistPkg = new CfgPackage().Pack();
                }
                //ZPackage zPackage = new CfgPackage().Pack();
                rpc.Invoke("RPC_ReceiveConfigsAllTameable", tamelistPkg);
            }
        }

       

        //****************************End New Code**************************
        /*
        public static void RPC_RequestGetTameableList(long sender, ZPackage pkg)
        {
            DBG.blogWarning("RPC_received1");
            if (ZNet.instance.IsServer())
            {
                DBG.blogWarning("I am Server");
                if (pkg != null && pkg.Size() > 0)
                { // Check that our Package is not null, and if it isn't check that it isn't empty.
                    DBG.blogWarning("pkg not null");
                    ZNetPeer peer = ZNet.instance.GetPeer(sender); // Get the Peer from the sender, to later check the SteamID against our Adminlist.
                    if (peer != null)
                    { // Confirm the peer exists
                        DBG.blogWarning("peer not null");
                        ZPackage newpkg = new CfgPackage().Pack();
                        //newpkg.Write(Pack());
                        //string msg = pkg.ReadString(); // Read the message from the user.
                        //pkg.SetPos(0); // Reset the position of our cursor so the client's can re-read the package.
                        ZRoutedRpc.instance.InvokeRoutedRPC(sender, "EventServerAnnouncement", new object[] { newpkg }); // Send our Event to all Clients. 0L specifies that it will be sent to everybody

                    }
                }
            }
            else
            {
                return;
            }
        }


        public static void RPC_EventGetTameableList(long sender, ZPackage pkg)
        {
            DBG.blogWarning("RPC_received2");
            if (!ZNet.instance.IsServer())
            {
                DBG.blogWarning("RPC_in client");
                if (sender == ZRoutedRpc.instance.GetServerPeerID() && pkg != null && pkg.Size() > 0)
                { // Confirm our Server is sending the RPC
                    //string announcement = pkg.ReadString();
                    CfgPackage.Unpack(pkg);
                    PetManager.UpdatesFromServer();
                    DBG.blogWarning("announcement");
                }
            }
            else
            {
                return;
            }

        }

        public static void RPC_BadRequestMsg(long sender, ZPackage pkg)
        {
            DBG.blogWarning("RPC_received3");
            if (sender == ZRoutedRpc.instance.GetServerPeerID() && pkg != null && pkg.Size() > 0)
            { // Confirm our Server is sending the RPC
                string msg = pkg.ReadString(); // Get Our Msg
                DBG.blogWarning(msg);
                if (msg != "")
                { // Make sure it isn't empty
                    Chat.instance.AddString("Server", "<color=\"red\">" + msg + "</color>", Talker.Type.Normal); // Add to chat with red color because it's an error
                }
            }
        }
        */
    }


}
