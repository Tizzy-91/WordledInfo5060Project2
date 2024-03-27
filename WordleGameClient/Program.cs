using Grpc.Core;
using Grpc.Net.Client;
using System;
using WordleGameServer.Protos;
using WordleGameServer.Services;

namespace WordleGameClient
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var channel = GrpcChannel.ForAddress("http://localhost:5260");
                var wordServ = new DailyWordle.DailyWordleClient(channel);

                string userGuess;
                bool isDone = false;
                int numGuesses = 1;

                PrintHelp();
                Console.Write("(1): ".PadRight(5, ' '));

                using (var call = wordServ.Play())
                {
                    do
                    {
                        userGuess = Console.ReadLine() ?? "";

                        if (userGuess.Length != 5)
                        {
                            Console.WriteLine("The word must be 5 characters long");
                        }

                        GuessRequest request = new()
                        {
                            Guess = userGuess,
                        };

                        await call.RequestStream.WriteAsync(request);

                        await call.ResponseStream.MoveNext();
                        GuessResponse response = call.ResponseStream.Current;

                        if (!response.IsValid)
                        {
                            Console.WriteLine("\nThat word is not in the wordled dictionary! Try a different word.");
                            Console.Write($"({numGuesses}): ".PadRight(5, ' '));
                            continue;
                        }
                        else if (!response.GameOver)
                        {
                            Console.WriteLine(response.Feedback.PadLeft(10, ' '));
                            Console.WriteLine("\nIncluded: " + response.Included);
                            Console.WriteLine("Available: " + response.Available);
                            Console.WriteLine("Excluded: " + response.Excluded);

                            numGuesses = (int)response.NumGuesses;
                            Console.Write($"\n({numGuesses}): ".PadRight(5, ' '));
                        }
                        else
                        {
                            Console.WriteLine(response.Feedback.PadLeft(10, ' '));
                            Console.WriteLine("\nYou win!\n");
                            await call.RequestStream.CompleteAsync();
                            isDone = true;
                        }
                    } while (!isDone);
                
                    WordStatsResponse? gameStats = wordServ.GetStats(new Google.Protobuf.WellKnownTypes.Empty());

                    Console.WriteLine("Statistics");
                    Console.WriteLine("----------");
                    Console.Write("\nPlayers: ".PadRight(25, ' '));
                    Console.Write($"{gameStats.NumPlayers}".PadLeft(10, ' '));
                    Console.Write("\nWinners:".PadRight(25, ' '));
                    Console.Write($"{gameStats.WinnersPercentage}".PadLeft(10, ' '));
                    Console.WriteLine("\nGuess Distribution...".PadRight(25, ' '));
                    Console.WriteLine($" 1: {gameStats.GuessDistribution.Guesses1}");
                    Console.WriteLine($" 2: {gameStats.GuessDistribution.Guesses2}");
                    Console.WriteLine($" 3: {gameStats.GuessDistribution.Guesses3}");
                    Console.WriteLine($" 4: {gameStats.GuessDistribution.Guesses4}");
                    Console.WriteLine($" 5: {gameStats.GuessDistribution.Guesses5}");
                    Console.WriteLine($" 6: {gameStats.GuessDistribution.Guesses6}");

                    Console.WriteLine("\nPress any key to exit.");
                    Console.ReadKey();
                }



            } catch (RpcException)
            {
                Console.WriteLine("Error The word server is currently unavaible");
            }


        }

        public static void PrintHelp()
        {
            Console.WriteLine("+-------------+");
            Console.WriteLine("|   Wordled   |");
            Console.WriteLine("+-------------+");

            Console.WriteLine("\nYou have 6 chances to guess a 5-letter word.");
            Console.WriteLine("Each guess must be a 'playable' 5 letter word.");
            Console.WriteLine("After a guess, the game will display a series of");
            Console.WriteLine("characters to show how good your guess was.");
            Console.WriteLine("\nx - means the letter above is not in the word.");
            Console.WriteLine("? - means the letter should be in another spot.");
            Console.WriteLine("* - means the letter is correct in this spot.\n");
            Console.WriteLine("\tAvailable: a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v,w,x,y,z\n");
        }
    }
}
