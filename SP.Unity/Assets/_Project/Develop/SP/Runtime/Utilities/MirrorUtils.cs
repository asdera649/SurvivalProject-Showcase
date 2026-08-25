using Mirror;
using UnityEngine;

namespace SP.Runtime.Utilities
{
    public static class MirrorUtils
    {
        // Нам необходим был способ определить запущен ли метод остановки сервера,
        // по результату которого, мы бы например, в методе OnDeath(который вызывается в OnStopServer),
        // класса Character, определили, стоит нам вызывать метод Drop у Inventory,
        // потому что если вызвать метод Drop во время остановки сервера,
        // это вызовет спавн Network объектов, которых в последующем NetworkServer, не сможет разрушить,
        // так как NetworkServer.active будет false, и консоль заполнится множеством ошибок.
        // Это конечно костыльный способ определить запущен ли процесс Shutdown в NetworkServer,
        // но он работает(пока)
        public static bool IsShutdownProcessRunning =>
            !NetworkServer.dontListen && !NetworkServer.isLoadingScene &&
            NetworkServer.actualTickRate == 0 && NetworkServer.localConnection == null &&
            NetworkServer.connections.Count == 0;

        public static bool IsHeadless => Application.platform == RuntimePlatform.LinuxServer ||
                                         Application.platform == RuntimePlatform.WindowsServer;
    }
}