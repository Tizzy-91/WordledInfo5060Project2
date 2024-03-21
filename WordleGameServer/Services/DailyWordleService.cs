using Grpc.Core;
using Google.Protobuf.WellKnownTypes;
using WordleGameServer.Clients;
using WordleGameServer.Protos;

namespace WordleGameServer.Services
{
    public class DailyWordleService : DailyWordle.DailyWordleBase
    {
        public DailyWordleService()
        {

        }

        public override Task<DailyWordleResponse> GetDailyWordle(Empty request, ServerCallContext context)
        {
            string word = "";
            try
            {
                word = WordServiceClient.GetWord("Tests");
            }
            catch (RpcException)
            {
                Console.WriteLine("Error The word server is currently unavaible");
            }

            return Task.FromResult(new DailyWordleResponse { Word = word });
        }

        public override async Task Play(IAsyncStreamReader<GuessRequest> requestStream, IServerStreamWriter<GuessResponse> responseStream, ServerCallContext context)
        {
            // TODO: bidirectional stream to run a complete game of Wordle

        }

        public override Task<WordStatsResponse> GetStats(WordStatsRequest request, ServerCallContext context)
        {
            // TODO: returns all the stored user-statistics for the current daily word 
            return Task.FromResult(new WordStatsResponse { });
        }
    }
}
