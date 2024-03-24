using Grpc.Core;
using Google.Protobuf.WellKnownTypes;
using WordleGameServer.Clients;
using WordleGameServer.Protos;
using System;
using System.Threading;

namespace WordleGameServer.Services
{
    public class DailyWordleService : DailyWordle.DailyWordleBase
    {
        private WordStatsResponse currentDayStats = new WordStatsResponse();
        private static Mutex mutex = new Mutex();

        public DailyWordleService()
        {
            currentDayStats = new WordStatsResponse();
            currentDayStats.GuessDistribution = new GuessDistribution();
        }

        /// <summary>
        /// runs the game of wordle
        /// </summary>
        /// <param name="requestStream"></param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task Play(IAsyncStreamReader<GuessRequest> requestStream, IServerStreamWriter<GuessResponse> responseStream, ServerCallContext context)
        {

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

                // prepare to get next word
                GuessResponse repsonse = new ()
                {
                    CorrectGuess = guess.Guess == dailyWord,
                    GameOver = numGuessed >= 6 || guess.Guess == dailyWord,
                    Feedback = feedback
                };

                // send question 
                await responseStream.WriteAsync(repsonse);

                if (guess.Guess == dailyWord || numGuessed >= 6)
                {
                    UpdateGameStats(numGuessed);
                    break;
                }
            }
        }

        /// <summary>
        /// Gets the game statistics for todays wordle
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns>currents day's game stats</returns>
        public override Task<WordStatsResponse> GetStats(WordStatsRequest request, ServerCallContext context)
        {
            currentDayStats.WinnersPercentage = (UInt32)CalculateWinnersPercentage();

            return Task.FromResult(currentDayStats);
        }

        /// <summary>
        /// Updates the game stats
        /// </summary>
        /// <param name="numGuessed"></param>
        private void UpdateGameStats(int numGuessed)
        {
            try
            {
                // Wait until it is safe to enter
                mutex.WaitOne();
            
                currentDayStats.NumPlayers += 1;

                switch (numGuessed)
                {
                    case 1:
                        currentDayStats.GuessDistribution.Guesses1 += 1;
                        break;
                    case 2:
                        currentDayStats.GuessDistribution.Guesses2 += 1;
                        break;
                    case 3:
                        currentDayStats.GuessDistribution.Guesses3 += 1;
                        break;
                    case 4:
                        currentDayStats.GuessDistribution.Guesses4 += 1;
                        break;
                    case 5:
                        currentDayStats.GuessDistribution.Guesses5 += 1;
                        break;
                    case 6:
                        currentDayStats.GuessDistribution.Guesses6 += 1;
                        break;
                    default:
                        break;
                }
            }
            finally 
            { 
                // Realease the mutex 
                mutex.ReleaseMutex(); 
            }

        }

        /// <summary>
        /// Calculates the percentage of winners based on the numbers of players and count of winners
        /// </summary>
        /// <returns>percentage of winners</returns>
        private double CalculateWinnersPercentage()
        {
            UInt32 totalWinners = currentDayStats.GuessDistribution.Guesses1 +
                                  currentDayStats.GuessDistribution.Guesses2 +
                                  currentDayStats.GuessDistribution.Guesses3 +
                                  currentDayStats.GuessDistribution.Guesses4 +
                                  currentDayStats.GuessDistribution.Guesses5 +
                                  currentDayStats.GuessDistribution.Guesses6;

            if (currentDayStats.NumPlayers > 0)
            {
                double percentage = (double)totalWinners / currentDayStats.NumPlayers * 100;
                return percentage;
            }
            else
            {
                return 0.0; // no players
            }
        }

    }
}
