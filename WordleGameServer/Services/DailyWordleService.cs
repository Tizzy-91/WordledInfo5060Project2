using Grpc.Core;
using WordleGameServer.Protos;
using Google.Protobuf.WellKnownTypes;
using WordleGameServer.Clients;


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
            } catch (RpcException)
            {
                Console.WriteLine("Error The word server is currently unavaible");
            }

            return Task.FromResult(new DailyWordleResponse { Word =  word});
        }
    }
}
