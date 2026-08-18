using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNETCoreDiscordBot
{
    public class LinuxProcess : ServerProcess
    {
        public LinuxProcess(BotConfig botConfig, string scriptPath, ILogFile logFile) : base(botConfig, scriptPath, logFile)
        {
        }

        protected override void ExtractOSParams(string line)
        {
            string[] args = _botConfig.Linuxparams;

            if (args == null || args.Length == 0) return;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].ToLower() == "-servername" && i + 1 < args.Length)
                {
                    string servername = args[i + 1].Replace("\"", "").Trim();
                    if (!string.IsNullOrEmpty(servername))
                    {
                        _botConfig.ServerName = servername;
                    }
                    break;
                }
            }
        }

        public override void SetupProcessStartInfo(ProcessStartInfo startInfo, string serverName)
        {
            Process.Start("chmod", $"+x \"{_scriptPath}\"")?.WaitForExit();

            startInfo.FileName = "/bin/bash";
            startInfo.Arguments = $"\"{_scriptPath}\" -servername \"{serverName}\"";
        }

        public override async Task<double> GetCPUUsage()
        {
            try
            {
                var stat1 = File.ReadAllLines("/proc/stat")[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                long idle1 = long.Parse(stat1[4]);
                long total1 = stat1.Skip(1).Take(7).Select(long.Parse).Sum();

                await Task.Delay(500);

                var stat2 = File.ReadAllLines("/proc/stat")[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                long idle2 = long.Parse(stat2[4]);
                long total2 = stat2.Skip(1).Take(7).Select(long.Parse).Sum();

                long totalDelta = total2 - total1;
                if (totalDelta == 0) return 0.0;

                return (1.0 - ((double)(idle2 - idle1) / totalDelta)) * 100.0;
            }
            catch { return 0.0; }
        }

        public override double GetRAMUsage()
        {
            try
            {
                var lines = File.ReadAllLines("/proc/meminfo");
                double total = 0, available = 0;

                foreach (var line in lines)
                {
                    if (line.StartsWith("MemTotal:"))
                        double.TryParse(line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1], out total);
                    else if (line.StartsWith("MemAvailable:"))
                        double.TryParse(line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1], out available);
                }

                return total > 0 ? ((total - available) / total) * 100.0 : 0.0;
            }
            catch { return 0.0; }
        }

        public override async Task UpdateWorkshopMods()
        {
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string needupdateLocation = Path.Combine(AppContext.BaseDirectory, "needupdatefile.txt");
            string steamcmdLocation = "";

            if (!File.Exists(needupdateLocation))
            {
                _logFile.WriteLine(Messages.Get("needupdate_not_found"));
                return;
            }

            string[] modsNeedupdate = await File.ReadAllLinesAsync(needupdateLocation);

            if (modsNeedupdate.Length <= 0)
            {
                _logFile.WriteLine(Messages.Get("needupdate_not_required"));
                return;
            }

            // Clear needupdatefile.txt after stacking modsNeedupdate
            await File.WriteAllTextAsync(needupdateLocation, string.Empty);

            // try searching steamcmd location installed by package manager
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "which",
                        Arguments = "steamcmd",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                string result = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (!string.IsNullOrWhiteSpace(result))
                {
                    steamcmdLocation = result.Trim();
                }
            }
            catch
            {
            }

            // if not installed steamcmd via package manager, use some install dir nominees
            List<string> pathNominees = new List<string>
            {
                Path.Combine(homeDir, "Steam", "steamcmd.sh"),
                Path.Combine(homeDir, ".steam", "steamcmd.sh"),
                Path.Combine(homeDir, "steamcmd", "steamcmd.sh"),
                "/opt/steamcmd/steamcmd.sh",
                "/opt/Steam/steamcmd.sh",
                "/opt/pzserver/steamcmd/steamcmd.sh"
            };

            foreach (string nominee in pathNominees)
            {
                if (File.Exists(nominee))
                {
                    steamcmdLocation = nominee;
                }
            }

            if (string.IsNullOrEmpty(steamcmdLocation))
            {
                _logFile.WriteLine(Messages.Get("steamcmd_not_found"));
                return;
            }

            try
            {
                _logFile.WriteLine(Messages.Get("steamcmd_update_mods"));
                StringBuilder steamcmdArgs = new StringBuilder();
                steamcmdArgs.Append($"+force_install_dir \"{AppContext.BaseDirectory}\" +login anonymous ");

                foreach (var modId in modsNeedupdate)
                {
                    steamcmdArgs.Append($"+workshop_download_item 108600 {modId} ");
                }

                steamcmdArgs.Append("+quit");

                var processInfo = new ProcessStartInfo
                {
                    FileName = steamcmdLocation,
                    Arguments = steamcmdArgs.ToString(),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = new Process { StartInfo = processInfo })
                {
                    process.Start();

                    await process.WaitForExitAsync();
                }
            } catch(Exception e)
            {
                _logFile.WriteLine(Messages.Get("steamcmd_error").KeyFormat(("error",e.Message)));
                return;
            }
        }


        // forked algorithm of okaMi0ka(https://gist.github.com/okaMi0ka/cfd532993e80ad3f808558f1aafd5ea9). Thanks to them!!
        public override void FixCaseSensitivity()
        {
            //list for FIXED_DIRS, FIXED_FILES, SKIPPED, ERRORS count
            uint[] debugs = [0, 0, 0, 0];

            try
            {
                string workshopLocation = Path.Combine(AppContext.BaseDirectory, "steamapps/workshop/content/108600");

                if (!Directory.Exists(workshopLocation)){
                    _logFile.WriteLine(Messages.Get("workshop_path_not_found"));
                    return;
                }

                uint mod_count = 0;
                mod_count = (uint)Directory.EnumerateDirectories(workshopLocation)
                    .Select(dir => Path.GetFileName(dir))
                    .Count(name => !string.IsNullOrEmpty(name) && name.All(char.IsDigit));

                if (mod_count == 0)
                {
                    _logFile.WriteLine(Messages.Get("no_mods_found"));
                    return;
                }

                _logFile.WriteLine(Messages.Get("make_lowercase_symlink_directory"));
                uint rootDepth = (uint)workshopLocation.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                    .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length;

                var directories = Directory.EnumerateDirectories(workshopLocation, "*", SearchOption.AllDirectories)
                    .Where(dir =>
                    {
                        // check mindepth 2
                        uint curDepth = (uint)dir.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length;
                        if (curDepth - rootDepth < 2) return false;

                        // check is real link, not a symbolic link
                        FileAttributes attr = File.GetAttributes(dir);
                        return (attr & FileAttributes.ReparsePoint) == 0;
                    })
                    .OrderBy(dir => dir, StringComparer.Ordinal);

                foreach (var dir in directories)
                {
                    MakeLowerCaseSymlink(dir, ref debugs);
                }

                _logFile.WriteLine(Messages.Get("make_lowercase_symlink_file"));
                var files = Directory.EnumerateFiles(workshopLocation, "*", SearchOption.AllDirectories)
                    .Where(file =>
                    {
                        //check mindepth 2 too
                        uint curDepth = (uint)file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length;
                        if (curDepth - rootDepth < 2) return false;

                        // check is real link, not a symbolic link
                        FileAttributes attr = File.GetAttributes(file);
                        return (attr & FileAttributes.ReparsePoint) == 0;
                    })
                    .OrderBy(file => file, StringComparer.Ordinal);

                foreach(var file in files)
                {
                    MakeLowerCaseSymlink(file, ref debugs);
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine(Messages.Get("make_lowercase_symlink_result"));
                sb.AppendLine("-----------------------------");
                sb.AppendLine($"FIXED_DIRS: {debugs[0]}");
                sb.AppendLine($"FIXED_FILES: {debugs[1]}");
                sb.AppendLine($"SKIPPED: {debugs[2]}");
                sb.AppendLine($"FAILED: {debugs[3]}");
                sb.AppendLine("-----------------------------");

                _logFile.WriteLine(sb.ToString());

            }
            catch(Exception e)
            {
                _logFile.WriteLine(Messages.Get("make_lowercase_symlink_error").KeyFormat(("error", e.Message)));
            }
        }

        private void MakeLowerCaseSymlink(string location, ref uint[] debugs)
        {
            string target = location.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string parent = Path.GetDirectoryName(target);
            string name = Path.GetFileName(target);
            string lower = name.ToLowerInvariant();

            if(name == lower)
            {
                // Skipped count+=1
                debugs[2] += 1;
                return;
            }

            string symlinkPath = Path.Combine(parent, lower);

            FileSystemInfo linkInfo = Directory.Exists(symlinkPath) ? new DirectoryInfo(symlinkPath) : new FileInfo(symlinkPath);

            if(linkInfo.Exists && (linkInfo.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                FileSystemInfo targetInfo = linkInfo.ResolveLinkTarget(returnFinalTarget: false);

                if(targetInfo != null)
                {
                    //_logFile.WriteLine(Messages.Get("symlink_correct"));
                    debugs[2] += 1;
                    return;
                }

                linkInfo.Delete();
            }

            // check link is not symlink
            bool symlinkExists = File.Exists(symlinkPath) || Directory.Exists(symlinkPath);

            if (symlinkExists)
            {
                FileAttributes attributes = File.GetAttributes(symlinkPath);

                bool isNotSymlink = (attributes & FileAttributes.ReparsePoint) == 0;

                if (isNotSymlink)
                {
                    _logFile.WriteLine(Messages.Get("symlink_not_required"));
                    debugs[2] += 1;
                    return;
                }
            }

            // make symbolic link
            try
            {
                _logFile.WriteLine(Messages.Get("create_symlink").KeyFormat(("location", Path.Combine(symlinkPath, name))));
                bool isDirectory = Directory.Exists(target);
                if (isDirectory)
                {
                    Directory.CreateSymbolicLink(symlinkPath, name);
                    debugs[0] += 1;
                }
                else
                {
                    File.CreateSymbolicLink(symlinkPath, name);
                    debugs[1] += 1;
                }
            } catch(Exception e)
            {
                _logFile.WriteLine(Messages.Get("create_symlink_failed").KeyFormat(("location", Path.Combine(symlinkPath, name))));
                debugs[3] += 1;
            }
        }
    }
}
