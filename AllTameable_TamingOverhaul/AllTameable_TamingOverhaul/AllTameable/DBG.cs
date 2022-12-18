
namespace AllTameable
{
    public static class DBG
    {
        public static void cprt(string s)
        {
            Console.instance.Print(s);
        }

        public static void InfoTL(string s)
        {
            Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, s);
        }

        public static void InfoCT(string s)
        {
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, s);
        }

        public static void blogInfo(object o)
        {
            Plugin.logger.LogInfo(o);
        }

        public static void blogWarning(object o)
        {
            Plugin.logger.LogWarning(o);
        }

        public static void blogDebug(object o)
        {
            if (Plugin.debugout.Value)
            {
                Plugin.logger.LogWarning(o);
            }
        }
    }
}
