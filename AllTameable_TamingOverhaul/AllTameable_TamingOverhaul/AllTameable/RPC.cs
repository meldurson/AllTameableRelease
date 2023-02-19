using BepInEx;
using HarmonyLib;
using Steamworks;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace AllTameable.RPC
{
    public static class RPC
    {
        /*
        [HarmonyPatch(typeof(Game), "Start")]
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
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ZNet), "OnNewConnection")]
        //private static class Postfix_ZNet_OnNewConnection
        //{
            private static void Prefix(ref ZNet __instance, ZNetPeer peer)
            {
                DBG.blogDebug("RPC_OnNewConnection");
                if (ZNet.instance.IsServer())
                {
                    DBG.blogDebug("RPC_Received Registered");
                    peer.m_rpc.Register("RPC_RequestConfigsAllTameable", (RPC.RPC_RequestConfigsAllTameable));
                    return;
                }
                DBG.blogDebug("RPC_registered in client");
                peer.m_rpc.Register<ZPackage>("RPC_ReceiveConfigsAllTameable", (RPC.RPC_ReceiveConfigsAllTameable));
                peer.m_rpc.Invoke("RPC_RequestConfigsAllTameable");
            }
        //}
        private static void RPC_ReceiveConfigsAllTameable(ZRpc rpc, ZPackage pkg)
        {
            DBG.blogDebug("RPC_received");
            if (!ZNet.instance.IsServer())
            {
                if (pkg != null && pkg.Size() > 0)
                { // Confirm our Server is sending the RPC
                  //string announcement = pkg.ReadString();
                    CfgPackage.Unpack(pkg);
                    PetManager.UpdatesFromServer();
                }
            }
        }

        private static void RPC_RequestConfigsAllTameable(ZRpc rpc)
        {
            DBG.blogDebug("RPC_requested");
            if (rpc != null)
            {
                ZPackage zPackage = new CfgPackage().Pack();
                rpc.Invoke("RPC_ReceiveConfigsAllTameable", zPackage);
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
