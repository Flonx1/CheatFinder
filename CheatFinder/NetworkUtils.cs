using System.Net.Sockets;

namespace CheatFinder
{
    public static class NetworkUtils
    {
        public static bool IsPortOpen(string host, int port)
        {
            try
            {
                using (new TcpClient(host, port))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
