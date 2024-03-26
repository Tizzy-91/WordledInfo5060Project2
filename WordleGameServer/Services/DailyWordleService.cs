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

            char[] alphabet = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 
                'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z' };

            // populate available letters (alphabet)
            SortedSet<char> availableLetters = new SortedSet<char>(alphabet);
            SortedSet<char> includedLetters = new();
            SortedSet<char> excludedLetters = new();

            //string dailyWord = WordServiceClient.GetWord();
            string dailyWord = "spoon";

            int turnsUsed = 1;
            bool gameWon = false;
            char[] results = new char[5];

            // wait for client to send next message asynchronously
            // exit if there are no more messages or the client closes the stream
            // exit at 6 turns or if game is won
            while (await requestStream.MoveNext() && !context.CancellationToken.IsCancellationRequested && turnsUsed < 6 && !gameWon)
            {
                GuessRequest guess = requestStream.Current; 
                GuessResponse response = null;

                // check if it is a valid guess
                bool isValid = WordServiceClient.ValidateGuess(guess.Guess);
                if (!isValid)
                {
                    response = new()
                    {
                        IsValid = false,
                    };


                    await responseStream.WriteAsync(response);
                    continue;
                }

                turnsUsed++;

                if (guess.Guess == dailyWord)
                {
                    gameWon = true;

                    for (int i = 0; i < results.Length; i++)
                        results[i] = '*';
                }
                else
                {
                    Dictionary<char, int> matches = new Dictionary<char, int>()
                    {
                        { 'a', 0 }, { 'b', 0 }, { 'c', 0 }, { 'd', 0 }, { 'e', 0 },
                        { 'f', 0 }, { 'g', 0 }, { 'h', 0 }, { 'i', 0 }, { 'j', 0 },
                        { 'k', 0 }, { 'l', 0 }, { 'm', 0 }, { 'n', 0 }, { 'o', 0 },
                        { 'p', 0 }, { 'q', 0 }, { 'r', 0 }, { 's', 0 }, { 't', 0 },
                        { 'u', 0 }, { 'v', 0 }, { 'w', 0 }, { 'x', 0 }, { 'y', 0 },
                        { 'z', 0 }
                    };

                    // search word played for letters that are in correct position
                    for (int i = 0; i < guess.Guess.Length; i++)
                    {
                        char letter = guess.Guess[i];
                        if (letter == dailyWord[i])
                        {
                            results[i] = '*';
                            matches[letter] = matches[letter]++;

                            if (!includedLetters.Contains(letter))
                                includedLetters.Add(letter);
                            if (availableLetters.Contains(letter))
                                availableLetters.Remove(letter);
                        }
                    }

                    // search word played for additional correct letters that are not in correct position
                    for (int i = 0; i < guess.Guess.Length; i++)
                    {
                        char letter = guess.Guess[i];
                        if (CountFrequency(dailyWord, letter) == 0)
                        {
                            results[i] = 'x';

                            if (!excludedLetters.Contains(letter))
                                excludedLetters.Add(letter);

                            if (availableLetters.Contains(letter))
                                availableLetters.Remove(letter);
                        }
                        else if (letter != dailyWord[i])
                        {
                            if (matches[letter] < CountFrequency(dailyWord, letter))
                            {
                                results[i] = '?';
                                matches[letter] = matches[letter]++;
                                
                                if (!includedLetters.Contains(letter))
                                    includedLetters.Add(letter);
                                if (availableLetters.Contains(letter))
                                    availableLetters.Remove(letter);
                            }
                        }
                    }
                }

                // prepare to get next word
                if (response is null)
                {
                    response = new()
                    {
                        IsValid = true,
                    };

                }

                response.CorrectGuess = guess.Guess == dailyWord;
                response.GameOver = turnsUsed >= 6 || guess.Guess == dailyWord;
                response.Feedback = new string(results);
                response.Included = String.Join(",", includedLetters);
                response.Available = String.Join(",", availableLetters);
                response.Excluded = String.Join(",", excludedLetters);
                response.NumGuesses = (uint)turnsUsed;

                if (guess.Guess == dailyWord || turnsUsed >= 6)
                {
                    //UpdateGameStats(turnsUsed);
                    //break;
                }

                // send response 
                await responseStream.WriteAsync(response);
            }
        }

        public static int CountFrequency(string word, char letter)
        {
            return word.Count(c => c == letter);
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
