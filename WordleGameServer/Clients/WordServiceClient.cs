using Grpc.Net.Client;
using WordServer.Protos;
using Grpc.Core;

namespace WordleGameServer.Clients
{
    public static class WordServiceClient
    {
        public static DailyWord.DailyWordClient? _dailyWordClient = null;

        /// <summary>
        /// Gets the daily word from WordServer
        /// </summary>
        /// <returns></returns>
        public static string GetWord()
        {

            ConnectToServer();

            WordReply? reply = _dailyWordClient?.GetWord(new WordRequest { });

            return reply?.Word ?? "";
        }

        /// <summary>
        /// Validates the user's guess if it matches a word in the text file
        /// </summary>
        /// <param name="guess"></param>
        /// <returns>bool representing if the guess is valid</returns>
        public static bool ValidateGuess(string guess)
        {
            ConnectToServer();

            ValidateWordReply? reply = _dailyWordClient?.ValidateWord(new ValidateWordRequest { Guess = guess });

            return reply?.Valid ??  false;
        }

        /// <summary>
        /// Connects to the DailyWordServer
        /// </summary>
        private static void ConnectToServer()
        {
            if(_dailyWordClient == null)
            {
                GrpcChannel channel = GrpcChannel.ForAddress("https://localhost:7118");
                _dailyWordClient = new DailyWord.DailyWordClient(channel);
            }
        }
    }
}
