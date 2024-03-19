using Grpc.Net.Client;
using WordServer.Protos;
using Grpc.Core;

namespace WordleGameServer.Clients
{
    public static class WordServiceClient
    {
        public static DailyWord.DailyWordClient? _dailyWordClient = null;

        public static string GetWord(string word)
        {
            ConnectToServer();

            WordReply? reply = _dailyWordClient?.GetWord(new WordRequest { Word = word});

            return reply?.Word ?? "";
        }

        private static void ConnectToServer()
        {
            if(_dailyWordClient == null)
            {
                GrpcChannel channel = GrpcChannel.ForAddress("https://localhost:5001");
                _dailyWordClient = new DailyWord.DailyWordClient(channel);
            }
        }
    }
}
