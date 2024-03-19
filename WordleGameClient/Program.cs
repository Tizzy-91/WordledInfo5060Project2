using Grpc.Core;
using Grpc.Net.Client;
using System;
using WordServer.Protos;

namespace WordleGameClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var channel = GrpcChannel.ForAddress("http://localhost:5260");
                var wordServ = new DailyWord.DailyWordClient(channel);

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
                        var reply = wordServ.GetWord(new WordRequest { Word = word });
                        while(word != reply.Word)
                        {
                            Console.WriteLine("Try again");
                            word = Console.ReadLine() ?? "";
                            reply = wordServ.GetWord(new WordRequest { Word = word });
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
