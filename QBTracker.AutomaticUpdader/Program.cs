using System;
using System.Linq;
using System.Reflection;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace QBTracker.AutomaticUpdader
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var srv = new UpdaterService(null, Assembly.GetEntryAssembly());
            Console.WriteLine(await srv.CheckForUpdate());
            foreach (var release in srv.Releases.OrderByDescending(x => x.ParsedVersion))
            {
                Console.WriteLine($"Release: {release.ParsedVersion}");
                Console.WriteLine($"Uri: {release.DownloadUri}");
            }
        }
    }
}
