using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace testing
{
    internal class Program
    {
        static readonly string ProjectRoot = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.FullName;
        static readonly string PositionsFile = Path.Combine(ProjectRoot, "positions.csv");
        static readonly string PricesFile = Path.Combine(ProjectRoot, "prices.csv");

        public static Dictionary<string, int> positions = new Dictionary<string, int>();
        public static Dictionary<string, decimal> prices = new Dictionary<string, decimal>();

        public static bool isPaused = false;

        static void Main(string[] args)
        {
            // reads updates from two files
            // positions and prices

            // FileSystemWatcher and Console.ReadKey to use
            // Dictionary to track positions and prices
            Console.WriteLine("Initializing...");

            // Call the static version of LoadPositions
            LoadPositions();
            LoadPrices();
            PrintSnapshot();

            // Replace the target-typed object creation with an explicit type declaration
            Thread keyboardThread = new Thread(KeyboardChangeThread) { IsBackground = true };
            keyboardThread.Start();

            // Create a FileSystemWatcher to monitor changes in the directory
            var positionWatcher = CreateWatcher(PositionsFile, LoadPositions);
            var priceWatcher = CreateWatcher(PricesFile, LoadPrices);

            Console.CancelKeyPress += (_, e) => {
                e.Cancel = true;
                Console.WriteLine("Exiting...");
                Environment.Exit(0);
            };

            while (true)
            {
                Thread.Sleep(100);
            }
        }

        static void KeyboardChangeThread()
        {
            // This thread will listen for keyboard input
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Spacebar)
                    {
                        isPaused = !isPaused;
                        Console.WriteLine($"\n--- {(isPaused ? "PAUSED" : "RESUMED")} ---\n");

                        // Need to pause or resume the updates
                        if (!isPaused)
                        {
                            PrintSnapshot();
                        }
                    }
                }

                Thread.Sleep(100); // Sleep to avoid busy waiting
            }
        }

        static FileSystemWatcher CreateWatcher(string path, Action reloadAction)
        {
            var watcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(path),
                Filter = Path.GetFileName(path),
                NotifyFilter = NotifyFilters.LastWrite
            };

            watcher.Changed += (s, e) =>
            {
                if (isPaused) return;

                try
                {
                    //allow some time for the file to be released
                    Thread.Sleep(50);
                    reloadAction();
                    PrintSnapshot();
                }
                catch (IOException)
                {
                    
                }
            };

            watcher.EnableRaisingEvents = true;
            return watcher;
        }

        static void PrintSnapshot()
        {
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss"));

            var symbols = new HashSet<string>(positions.Keys);
            symbols.UnionWith(prices.Keys);

            foreach (var symbol in symbols)
            {
                string pos = positions.ContainsKey(symbol) ? positions[symbol].ToString() : "N/A";
                string price = prices.ContainsKey(symbol) ? prices[symbol].ToString("0.00") : "N/A";
                Console.WriteLine($"{symbol} Position:{pos} Price:{price}");
            }

            Console.WriteLine();
        }

        static void LoadPositions()
        {
            var temp = new Dictionary<string, int>();
            foreach (var line in File.ReadAllLines(PositionsFile))
            {
                var parts = line.Split(',');
                if (parts.Length == 2 && int.TryParse(parts[1], out int pos))
                {
                    temp[parts[0]] = pos;
                }
            }
            positions = temp;
        }

        static void LoadPrices()
        {
            var temp = new Dictionary<string, decimal>();
            foreach (var line in File.ReadAllLines(PricesFile))
            {
                var parts = line.Split(',');
                if (parts.Length == 2 && decimal.TryParse(parts[1], out decimal price))
                {
                    temp[parts[0]] = price;
                }
            }
            prices = temp;
        }
    }
}
