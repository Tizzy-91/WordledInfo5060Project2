using Grpc.Net.Client;
using WordServer.Protos;

namespace WordleGameServer.Clients
{
    public static class WordServiceClient
    {
        public static DailyWord.DailyWordClient? _dailyWordClient = null;
        public static string DailyWord = "";
        public static DateTime previousFetchDate = DateTime.Today;

        /// <summary>
        /// Gets the daily word from WordServer
        /// </summary>
        /// <returns></returns>
        public static string GetWord()
        {
            ConnectToServer();
            if (DailyWord.Length == 0)
            {
                FetchNewWord();
            }
            else if (DateTime.Today != previousFetchDate.Date)
            {
                FetchNewWord();
                previousFetchDate = DateTime.Today;
            }

            return DailyWord;
        }

        /// <summary>
        /// Fetches a new word from the server
        /// </summary>
        private static void FetchNewWord()
        {
            WordReply reply = _dailyWordClient!.GetWord(new WordRequest { });
            DailyWord = reply.Word;
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

            return reply?.Valid ?? false;
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
