using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace QBTracker.AutomaticUpdader
{
    class Program
    {
        private static Mutex mutex;

        static async Task Main(string[] args)
        {
            if (args.Length == 0)
                return;
            if (args[0] == "--debug")
            {
                var srv = new UpdaterService();
                Console.WriteLine(await srv.CheckForUpdate());
                await srv.DownloadUpdate(p => Console.WriteLine($" Download:{p:P}"));
                foreach (var release in srv.Releases.OrderByDescending(x => x.ParsedVersion))
                {
                    Console.WriteLine($"Release: {release.ParsedVersion}");
                    Console.WriteLine($"Uri: {release.DownloadUri}");
                }
                return;
            }

            const string mutexName = @"Global\QBTracker";
            mutex = new Mutex(false, mutexName, out var createdNew);
            if (!createdNew)
                if (!mutex.WaitOne(TimeSpan.FromSeconds(60)))
                    return;

            if (args[0] == "--updateAndRestart")
            {
                var srv = new UpdaterService();
                srv.PerformUpdateSwap(true);
            }

            if (args[0] == "--updateOnly")
            {
                var srv = new UpdaterService();
                srv.PerformUpdateSwap(false);
            }
        }
    }
}
