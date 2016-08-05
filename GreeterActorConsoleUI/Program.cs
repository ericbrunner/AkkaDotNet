using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Akka;
using Akka.Actor;
using System.Threading;
using Akka.Configuration.Hocon;
using System.Configuration;
using System.Diagnostics;

namespace GreeterActorConsoleUI
{
    // Immutable message type the actor will respond to.
    public class Greet
    {
        public Greet(string who, ConsoleColor color)
        {
            Who = who;
            Color = color;
        }

        public ConsoleColor Color { get; private set; }
        public string Who { get; private set; }
    }


    public class GreetingActor: ReceiveActor
    {
        public GreetingActor()
        {
            // tell the actor to respond to the Greet message
            Receive<Greet>(greet =>
            {
                Console.ForegroundColor = greet.Color;
                Console.WriteLine($"Hello {greet.Who}");
            });
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // See: http://getakka.net/docs/Serialization#how-to-setup-wire-as-default-serializer . Without that config a note is emitted in the console
            var akkaConfiguration = ((AkkaConfigurationSection)ConfigurationManager.GetSection("akka")).AkkaConfig;

            // Create a new actor system (a container for the GreetingActor)
            var system = ActorSystem.Create("MySystem", akkaConfiguration);


            /* 
             * Create the Greetingactor and get a reference to it.
             * This will be an ActorRef, which is not a reference
             * to the actual GreetingActor instance but rather a
             * client or proxy to it.
            */
            var greeter = system.ActorOf<GreetingActor>("greeter");


            // Send a Message to the actor
            greeter.Tell(new Greet($"World from MainThread:{Thread.CurrentThread.ManagedThreadId}", ConsoleColor.Green));


            Console.ForegroundColor = ConsoleColor.Cyan;
            var tasks = new List<Task>();

            for (int i = 0; i < 1000; i++)
            {
                int colorindex = i;
                var task = Task.Run(async () =>
                {
                    ConsoleColor color;

                    int index = colorindex % 16;
                    Enum.TryParse(index.ToString(), out color);

                    greeter.Tell(new Greet($"World from Task:{Task.CurrentId} running on Thread:{Thread.CurrentThread.ManagedThreadId}", color));

                    // simulate some longer task running
                    await Task.Delay(500);
                });
                tasks.Add(task);
            }

            // prevent the app from exiting before the async work is done.
            Task.WaitAll(tasks.ToArray());
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.WriteLine("All async Tasks are finished.");
            Console.ReadKey();
        }
    }
}
