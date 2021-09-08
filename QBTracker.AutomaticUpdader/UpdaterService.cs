using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private readonly ILiteRepository liteRepository;
        private UpdateEntry updateEntry;

        public UpdaterService(ILiteRepository liteRepository = null)
        {
            if (liteRepository == null)
            {
#if DEBUG
                var file = @"App_Data\QBData.db";
#else
            var appDAta = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var file = Path.Combine(appDAta, @"QBTracker\QBData.db"); 
#endif
                this.liteRepository = new LiteRepository(file);
            }
            else
            {
                this.liteRepository = liteRepository;
                ProcessStartFile = Process.GetCurrentProcess().MainModule?.FileName;
                EnsureUpdateEntry();
                updateEntry.ExeFile = ProcessStartFile;
                SaveUpdateEntry();
            }

            ApplicationVersion = Assembly.GetExecutingAssembly().GetName().Version;
            TempAssembliesFolder = AppDomain.CurrentDomain.BaseDirectory;
            ReleaseUpdateUri = "https://github.com/iQuarc/QBTracker/releases.atom";
        }

        public string ProcessStartFile { get; private set; }
        public string TempAssembliesFolder { get; private set; }
        public Version ApplicationVersion { get; set; }
        public List<Release> Releases { get; } = new List<Release>();
        public Release ReleaseToUpdate { get; private set; }
        public string ReleaseUpdateUri { get; set; }
        public bool UpdateReady { get; private set; }
        /// <summary>
        /// Reads the Atom feed at <see cref="ReleaseUpdateUri"/> and fills the <see cref="Releases"/>
        /// collection with information. If a release with higher version number than <see cref="ApplicationVersion"/>
        /// is found then the <see cref="ReleaseToUpdate"/> is set to this release.
        /// </summary>
        /// <param name="force"></param>
        /// <returns></returns>
        public async Task<bool> CheckForUpdate(bool force = false)
        {
            EnsureUpdateEntry();
            if (updateEntry.LastCheck == DateTime.Today && !force)
                return false;
            try
            {
                using (var cl = new HttpClient())
                {
                    var response = await cl.GetAsync(ReleaseUpdateUri);
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

            }
            catch (Exception ex)
            {
                LogException(ex);
                return false;
            }
            updateEntry.LastCheck = DateTime.Today;
            SaveUpdateEntry();
            return false;
        }

        public void ExtractUpdater(Assembly assembly)
        {
            try
            {
                var names = assembly.GetManifestResourceNames();
                using var stream = assembly.GetManifestResourceStream("QBTracker.Embeded.QBTrackerAutomaticUpdader.exe");
                using var fs = File.Create(Path.Combine(TempAssembliesFolder, "QBTracker.AutomaticUpdader.exe"));
                stream.CopyTo(fs);
            }
            catch (Exception ex)
            {
                LogMessage(ex.Message);
            }
        }

        public void LogMessage(string message)
        {
            liteRepository.Insert(new LogEntry
                { Category = Category.Info, Message = message, Date = DateTime.UtcNow }, "Logs");
        }

        public void LogException(Exception ex)
        {
            liteRepository.Insert(new LogEntry
                {Category = Category.Error, Message = ex.Message, Details = ex.ToString(), Date = DateTime.UtcNow}, "Logs");
        }

        private void EnsureUpdateEntry()
        {
            if (this.updateEntry == null)
            {
                this.updateEntry = liteRepository.SingleOrDefault<UpdateEntry>(x => x.Id == 1, "Update");
                if (this.updateEntry == null)
                {
                    this.updateEntry = new UpdateEntry();
                    liteRepository.Insert(this.updateEntry, "Update");
                }
            }
        }

        private void SaveUpdateEntry()
        {
            liteRepository.Update(updateEntry, "Update");
        }

        /// <summary>
        /// Downloads current update. If <see cref="ReleaseToUpdate"/> is <c>null</c> then Download simply returns
        /// </summary>
        /// <returns></returns>
        public async Task<bool> DownloadUpdate(Action<float> progressCallback = null)
        {
            try
            {
                var release = ReleaseToUpdate;
                if (release == null)
                    return false;
                UpdateReady = false;
                var fileUri = release.DownloadUri + "/QBTracker.exe";
                fileUri = fileUri.Replace("/tag/", "/download/");
                using (var cl = new HttpClient())
                {
                    var downloadedFile = new FileInfo(Path.Combine(TempAssembliesFolder, $"QBTracker.exe.{release.ParsedVersion}.download"));
                    if (!downloadedFile.Exists || downloadedFile.CreationTimeUtc != release.Updated)
                    {
                        await using (var fs = downloadedFile.Create())
                        {
                            var result = await cl.GetAsync(fileUri);
                            result.EnsureSuccessStatusCode();
                            var stream = await result.Content.ReadAsStreamAsync();
                            const int bufferSize = 8192;
                            var buffer = new byte[bufferSize];
                            int read = 0;
                            float totalRead = 0;
                            while ((read = await stream.ReadAsync(buffer, 0, bufferSize)) > 0)
                            {
                                totalRead += read;
                                await fs.WriteAsync(buffer, 0, read);
                                progressCallback?.Invoke(totalRead / stream.Length);
                            }
                        }

                        downloadedFile.CreationTimeUtc = release.Updated;
                    }
                    this.UpdateReady = true;
                    updateEntry.UpdateFile = downloadedFile.FullName;
                    SaveUpdateEntry();
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogException(ex);
                return false;
            }
        }

        public void StartUpdater()
        {
            this.ExtractUpdater(Assembly.GetCallingAssembly());
            var p = Process.Start(new ProcessStartInfo(Path.Combine(TempAssembliesFolder, "QBTracker.AutomaticUpdader.exe"), "--updateAndRestart")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
            Debug.WriteLine($"AutoUpdaterHandle:{p.Handle}");
        }

        internal void PerformUpdateSwap(bool restart)
        {
            EnsureUpdateEntry();
            bool success = false;
            if (updateEntry.UpdateFile != null)
            {
                try
                {
                    var processes = Process.GetProcessesByName("QBTracker.exe");
                    foreach (var process in processes)
                    {
                        if (process.MainModule?.FileName != updateEntry.ExeFile)
                            continue;
                        if (!process.HasExited)
                            process.Close();
                    }
                    File.Copy(updateEntry.UpdateFile, updateEntry.ExeFile, true);
                    updateEntry.UpdateFile = null;
                    SaveUpdateEntry();
                    success = true;
                }
                catch (Exception ex)
                {
                    Debugger.Launch();
                    LogException(ex);
                }
            }
            if (restart)
                Process.Start(updateEntry.ExeFile, success ? "" : "--updateFailed");
            Environment.Exit(0);
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
            public string UpdateFile { get; set; }
            public string ExeFile { get; set; }
        }
    }
}