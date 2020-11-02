using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GsmLib;

namespace GSMlib_Test
{
    class Program
    {
        static void Main()
        {

            Gsm gsm = new Gsm();

            gsm.RaiseGsmEvent += HandleCustomEvent;

            gsm.RaiseSmsRecievedEvent += HandleSmsRecievedEvent;

            Console.WriteLine("Mobilfunktnetzerk " + (gsm.IsSimRegiserd() ? "registriert": "kein Empfang"));

            int sig = gsm.GetSignalQuality();

            Console.WriteLine("Signalqualität " + sig + "%");

            Console.WriteLine("Lese alle SMS:");
            gsm.ReadMessage();

            Console.WriteLine("Beliebige Taste zum beenden...");
            Console.ReadKey();
            gsm.ClosePort();
            Console.WriteLine("beendet.");
            Console.ReadKey();
        }

        // Define what actions to take when the event is raised.
        static void HandleCustomEvent(object sender, GsmEventArgs e)
        {
            Console.WriteLine($"*#*\r\n{e.Id}: {e.Message}\n\r#*#*#*#*#*#*#*#*#*#*#*#*#*#*#");
        }

        // Define what actions to take when the event is raised.
        static void HandleSmsRecievedEvent(object sender, ShortMessageArgs e)
        {
            Console.WriteLine(
                "***" +
                $"Index:\t\t{e.Index}\n\r" +
                $"Sent:\t\t{e.Sent}\n\r" +
                $"Sender:\t{e.Sender}\n\r" +
                $"Message:\t{e.Message}\n\r" +
                "***");
        }

    }

   
}
