using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using LiteDB;
using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Atom;

namespace QBTracker.AutomaticUpdader
{
    public class UpdaterService
    {
        private readonly ILiteDatabase liteDatabase;

        public UpdaterService(ILiteDatabase liteDatabase, Assembly mainAssembly)
        {
            this.liteDatabase = liteDatabase;
            ApplicationVersion = mainAssembly.GetName().Version;
            TempAssembliesFolder = AppDomain.CurrentDomain.BaseDirectory;
            ProcessStartFile = Process.GetCurrentProcess().MainModule?.FileName;
            RequestUri = "https://github.com/iQuarc/QBTracker/releases.atom";
        }

        public string ProcessStartFile { get; private set; }
        public string TempAssembliesFolder { get; private set; }
        public Version ApplicationVersion { get; }
        public List<Release> Releases { get; } = new List<Release>();
        public Release ReleaseToUpdate { get; private set; }
        public string RequestUri { get; set; }
        public async Task<bool> CheckForUpdate(bool force = false)
        {
            using (var cl = new HttpClient())
            {
                var response = await cl.GetAsync(RequestUri);
                response.EnsureSuccessStatusCode();
                using (var xmlReader = XmlReader.Create(await response.Content.ReadAsStreamAsync(),
                    new XmlReaderSettings
                    {
                        Async = true
                    }))
                {
                    Releases.Clear();
                    ReleaseToUpdate = null;
                    var feedReader = new AtomFeedReader(xmlReader);

                    while (await feedReader.Read())
                        switch (feedReader.ElementType)
                        {
                            case SyndicationElementType.Category:
                                await feedReader.ReadCategory();
                                break;
                            case SyndicationElementType.Image:
                                await feedReader.ReadImage();
                                break;

                            case SyndicationElementType.Item:
                                var entry = await feedReader.ReadEntry();
                                var r = new Release();
                                r.DownloadUri = entry.Links.FirstOrDefault(x => x.RelationshipType == "alternate")?.Uri;
                                if (r.DownloadUri != null)
                                {
                                    r.Updated = entry.LastUpdated.ToUniversalTime().LocalDateTime;
                                    r.ParsedVersion = ParseVersion(r.DownloadUri.AbsolutePath);
                                    if (r.ParsedVersion != null)
                                        Releases.Add(r);
                                }

                                break;
                            case SyndicationElementType.Link:
                                await feedReader.ReadLink();
                                break;

                            case SyndicationElementType.Person:
                                await feedReader.ReadPerson();
                                break;
                            default:
                                await feedReader.ReadContent();
                                break;
                        }

                    if (Releases.Count == 0)
                        return false;
                    var top = Releases.OrderByDescending(x => x.ParsedVersion).First();
                    if (top.ParsedVersion > ApplicationVersion)
                    {
                        ReleaseToUpdate = top;
                        return true;
                    }
                }
            }

            return false;
        }

        private Version ParseVersion(string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;
            var m = Regex.Match(value, @"\d+(\.\d+){2,3}");
            if (m.Success)
                return Version.Parse(m.Captures[0].Value);
            return null;
        }

        public class Release
        {
            public Version ParsedVersion { get; set; }
            public Uri DownloadUri { get; set; }
            public DateTime Updated { get; set; }
        }

        public class UpdateEntry
        {
            public int Id { get; set; }
            public DateTime LastCheck { get; set; }
        }
    }
}