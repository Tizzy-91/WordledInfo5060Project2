using Grpc.Core;
using WordleGameServer.Protos;

namespace WordleGameServer.Services
{
    public class DailyWordleService : DailyWordle.DailyWordleBase
    {
        public override Task<DailyWordleResponse> GetDailyWordle(DailyWordleRequest request, ServerCallContext context)
        {
            List<char> word = new(request.Word.ToCharArray());
            return Task.FromResult(new DailyWordleResponse { Word = word.ToString()});
        }
    }
}
