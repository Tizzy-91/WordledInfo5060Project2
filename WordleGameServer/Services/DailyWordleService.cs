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

        public override async Task Play(IAsyncStreamReader<GuessRequest> requestStream, IServerStreamWriter<GuessResponse> responseStream, ServerCallContext context)
        {
            Console.WriteLine(
                 "+-------------------+" +
                 "\n|   W O R D L E D   | +" +
                 "\n+-------------------+\n" +
                 "\nYou have 6 chances to guess a 5-letter word." +
                 "\nEach guess must be a 'playable' 5 letter word." +
                 "\nAfter a guess the game will display a series of" +
                 "\ncharacters to show you how good your guess was." +
                 "\nx - means the letter above is not in the word." +
                 "\n? - means the letter should be in another spot." +
                 "\n* - means the letter is correct in this spot.\n"
            );

            
            string alphabet = "abcdefghijklmnopqrstuvwxyz";

            // populate available letters (alphabet)
            List<string> availableLetters = new List<string>(alphabet.Select(c => c.ToString()));
            List<string> includedLetters = new List<string>();
            List<string> excludedLetters = new List<string>();

            int numGuessed = 0;
            bool isDone = false;
            string dailyWord = WordServiceClient.GetWord();


            while (await requestStream.MoveNext())
            {
                GuessRequest guess = requestStream.Current;

                // check if it is a valid guess
                bool isValid = WordServiceClient.ValidateGuess(guess.Guess);
                if (!isValid)
                {
                    Console.WriteLine("Invalid guess. Please try again.");
                    continue;
                }

                numGuessed++;
                string feedback = "";
                int charIndex = 0;

                // update letters lists and generate feedback
                foreach (char letter in guess.Guess)
                {
                    if (!dailyWord.Contains(letter.ToString()))
                    {
                        if (!excludedLetters.Contains(letter.ToString()))
                        {
                            excludedLetters.Add(letter.ToString());
                        }
                        feedback += 'x'; // letter is not in the word
                    }
                    else // letter is in dailyWord
                    {
                        if (!includedLetters.Contains(letter.ToString()))
                        {
                            includedLetters.Add(letter.ToString());  
                        }

                        if (charIndex == dailyWord.IndexOf(letter.ToString())) 
                        { 
                            feedback += '*'; // letter is in correct spot
                        }
                        else
                        {
                           feedback += '?'; // letter is in another spot
                        }
  
                    }

                    // remove letter from available letters if it has not been guessed yet
                    if (availableLetters.Contains(letter.ToString()))
                    {
                        availableLetters.Remove(letter.ToString());
                    }
                }


                if (guess.Guess == dailyWord || numGuessed >= 6)
                {
                    isDone = true;
                    break;
                }

                // TODO: prepare to get next word


               
            }
        }

        public override Task<WordStatsResponse> GetStats(WordStatsRequest request, ServerCallContext context)
        {
            // TODO: returns all the stored user-statistics for the current daily word 
            return Task.FromResult(new WordStatsResponse { });
        }

    }
}
