using IngameDebugConsole;
using Mirror;
using SP.Runtime.Meta.Services.NetworkService;

namespace SP.Runtime.Utilities.ConsoleCommands
{
    public class MirrorCommands
    {
        [ConsoleMethod( "mirror.host", "Start the host." )]
        public static void StartHost()
        {
            NetworkService.singleton.StartHost();
        }
        
        [ConsoleMethod( "mirror.client", "Start the client." )]
        public static void StartClient(string ip, ushort port)
        {
            NetworkService.singleton.networkAddress = ip;

            if (NetworkService.singleton.transport is PortTransport portTransport)
            {
                portTransport.Port = port;
            }
            
            NetworkService.singleton.StartClient();
        }
        
        [ConsoleMethod( "mirror.server", "Start the server." )]
        public static void StartServer()
        {
            NetworkService.singleton.StartServer();
        }
    }
}