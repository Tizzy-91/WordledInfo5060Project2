using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WordServer.Protos;

namespace WordServer.Services
{
    public class DailyWordService : DailyWord.DailyWordBase
    {
        private const string WordleJsonFile = "wordle.json";
        private readonly List<string> _words;
        private readonly Random _random;
        private readonly ILogger<DailyWordService> _logger;

        public DailyWordService(ILogger<DailyWordService> logger)
        {
            _logger = logger;
            _words = LoadWordsFromFile();
            _random = new Random();
        }

        private List<string> LoadWordsFromFile()
        {
            try
            {
                // Read the contents of the wordle.json file
                string json = File.ReadAllText(WordleJsonFile);

                // Deserialize the JSON content into a list of strings
                return JsonConvert.DeserializeObject<List<string>>(json)!;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error loading words from file: {0}", ex.Message);
                return new List<string>(); // Return an empty list if there's an error
            }
        }

        public override Task<WordReply> GetWord(WordRequest request, ServerCallContext context)
        {
            // TODO: make it return ONE word per day

            // Select a random word from the loaded words

            foreach (var word in _words)
            {
                Console.WriteLine(word);
            }

            string randomWord = _words.Count > 0 ? _words[_random.Next(_words.Count)] : "No words available";

            // Return the random word as the response
            return Task.FromResult(new WordReply
            {
                Word = randomWord
            });
        }

        public override Task<ValidateWordReply> ValidateWord(ValidateWordRequest request, ServerCallContext context)
        {
            // Check if the guess is in the words file
            bool isValid = _words.Contains(request.Guess);

            return Task.FromResult(new ValidateWordReply
            {
                Valid = isValid
            });
        }
    }
}
