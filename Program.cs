using System;
using System.Diagnostics;
using System.Reflection;

namespace HomeQuest
{
    public class Program
    {
        public static bool Debug { get; set; }

        static void Main(string[] args)
        {
            Console.WriteLine(@"Home Builder v{0} for Quest", Assembly.GetExecutingAssembly().GetName().Version.ToString(2));
            Console.WriteLine();

            if (args.Length == 1 && DebugRequired(args[0]))
                Debug = true;

            var app = new Application();
            try
            {
                app.Run();
                Console.WriteLine(@"Success.");
            }
            catch(Exception e)
            {
                Console.Error.WriteLine(@"Failed with error: {0}", e.Message);

                if (Debug)
                    Console.WriteLine(e.StackTrace);
            }
            finally
            {
                app.Reset();
            }

            Console.WriteLine();
            Console.WriteLine(@"Press Enter to EXIT.");
            Console.ReadLine();
        }

        private static bool DebugRequired(string param)
        {
            return param == "-d" || param == "--debug" || param == "/D" || param == "/d";
        }
    }
}
