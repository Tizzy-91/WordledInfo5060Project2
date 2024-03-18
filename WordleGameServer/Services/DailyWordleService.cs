using Grpc.Core;
using WordleGameServer.Protos;

namespace WordleGameServer.Services
{
    public class DailyWordleService : DailyWordle.DailyWordleBase
    {
        private readonly ILogger<DailyWordleService> _logger;

        public DailyWordleService(ILogger<DailyWordleService> logger)
        {
            _logger = logger;

        }
    }
}
