using Grpc.Core;
using Grpc.Net.Client;
using System;
using WordleGameServer.Protos;
using WordleGameServer.Services;

namespace WordleGameClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var channel = GrpcChannel.ForAddress("http://localhost:5260");
                var wordServ = new DailyWordle.DailyWordleClient(channel);

                int numGuesses = 0;

                string word;
                do
                {
                    Console.WriteLine("\nWelcome to Wordle you have 6 guesses! and it needs to be 5 letters!");

                    word = Console.ReadLine() ?? "";

                    if (word.Length != 5)
                    {
                        Console.WriteLine("The word must be 5 characters long");
                    }
                    else
                    {
                        var reply = wordServ.GetDailyWordle(new DailyWordleRequest { Word = word });

                        Console.WriteLine("This is a test print out the daily word is " + reply.Word);
                        while (word != reply.Word)
                        {
                            Console.WriteLine("Try again");
                            word = Console.ReadLine() ?? "";
                        }   
                        Console.WriteLine("You got it!");
                        numGuesses++;
                    }

                } while (numGuesses != 6);


            } catch (RpcException)
            {
                Console.WriteLine("Error The word server is currently unavaible");
            }
        }
    }
}
