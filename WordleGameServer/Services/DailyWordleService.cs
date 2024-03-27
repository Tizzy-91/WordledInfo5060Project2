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
        private WordStatsResponse? currentDayStats = null;
        private static Mutex mutex = new Mutex();

        private static uint NumPlayers = 0;
        private static uint oneGuess = 0;
        private static uint twoGuess = 0;
        private static uint threeGuess = 0;
        private static uint fourGuess = 0;
        private static uint fiveGuess = 0;
        private static uint sixGuess = 0;
        private static uint winnersPercentage = 0;


        public DailyWordleService()
        {
            //currentDayStats = new WordStatsResponse();
            //currentDayStats.GuessDistribution = new GuessDistribution();
        }

        /// <summary>
        /// Runs the game of wordle
        /// </summary>
        /// <param name="requestStream"></param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task Play(IAsyncStreamReader<GuessRequest> requestStream, IServerStreamWriter<GuessResponse> responseStream, ServerCallContext context)
        {
            ++NumPlayers;

            char[] alphabet = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 
                'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z' };

            // populate available letters (alphabet)
            SortedSet<char> availableLetters = new SortedSet<char>(alphabet);
            SortedSet<char> includedLetters = new();
            SortedSet<char> excludedLetters = new();

            // TODO: change this back to being generated
            //string dailyWord = WordServiceClient.GetWord();
            string dailyWord = "spoon";

            int turnsUsed = 0;
            bool gameWon = false;
            char[] results = new char[5];

            // wait for client to send next message asynchronously
            // exit if there are no more messages or the client closes the stream
            // exit at 6 turns or if game is won
            while (await requestStream.MoveNext() && !context.CancellationToken.IsCancellationRequested && turnsUsed < 6 && !gameWon)
            {
                GuessRequest guess = requestStream.Current; 
                GuessResponse? response = null;

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
                response.NumGuesses = (uint)turnsUsed + 1;

                if (guess.Guess == dailyWord && turnsUsed <= 6)
                {
                    UpdateGameStats(turnsUsed);
                }

                // send response 
                await responseStream.WriteAsync(response);
            } 
        }

        /// <summary>
        /// Gets the game statistics for todays wordle
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns>currents day's game stats</returns>
        public override Task<WordStatsResponse> GetStats(Empty request, ServerCallContext context)
        {
            WordStatsResponse dailyStats = new WordStatsResponse();


            dailyStats!.WinnersPercentage = (uint)CalculateWinnersPercentage();
            dailyStats!.GuessDistribution = new GuessDistribution
            {
                Guesses1 = oneGuess,
                Guesses2 = twoGuess,
                Guesses3 = threeGuess,
                Guesses4 = fourGuess,
                Guesses5 = fiveGuess,
                Guesses6 = sixGuess,

            };
            dailyStats!.NumPlayers = NumPlayers;

            return Task.FromResult(dailyStats);
        }
        
        /// <summary>
        /// Returns the frequency of a letter occuring in a given word.
        /// </summary>
        /// <param name="word"></param>
        /// <param name="letter"></param>
        /// <returns>int frequency</returns>
        public static int CountFrequency(string word, char letter)
        {
            return word.Count(c => c == letter);
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

                switch (numGuessed)
                {
                    case 1:
                        oneGuess++;
                        break;
                    case 2:
                        twoGuess++;
                        break;
                    case 3:
                        threeGuess++;
                        break;
                    case 4:
                        fourGuess++;
                        break;
                    case 5:
                        fiveGuess++;
                        break;
                    case 6:
                        sixGuess++;
                        break;
                    default:
                        break;
                }

                //winnersPercentage = (uint)CalculateWinnersPercentage();
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
            UInt32 totalWinners = oneGuess +
                                  twoGuess +
                                  threeGuess +
                                  fourGuess +
                                  fiveGuess +
                                  sixGuess;

            if (NumPlayers > 0)
            {
                double percentage = (double)totalWinners / NumPlayers * 100;
                return percentage;
            }
            else
            {
                return 0.0; // no players
            }
        }
    }
}
