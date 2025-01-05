using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using AdminWrapper.Config;
using Spectre.Console;

namespace AdminWrapper.Log;

public class LogArchiver
{
    #region Properties & Variables
    private const string CRASH_DIRECTORY = "Crash";
    private const string DATE_CAPTUR_GROUP = "date";
    private const string ZIP_EXTENSION = ".zip";

    public const string PORT_BALISE = "pppp";
    public const string TIME_BALISE = "tt";

    public readonly ArchiveConfig config;
    public readonly ushort port;
    #endregion

    #region Constructor & Destructor
    public LogArchiver(ArchiveConfig config, ushort port)
    {
        this.config = config;
        this.port = port;
    }
    #endregion

    #region Methods
    public void Archive(string logs)
    {
        var dayDir = CreateDayDirectory();
        DateTime now = config.GetDateTimeOffseted();
        var file = config.FileFormat
            .Replace(PORT_BALISE, port.ToString())
            .Replace(TIME_BALISE, now.ToString(config.FileTimeFormat));
 
        File.WriteAllText(Path.Combine(dayDir.FullName, file), logs);
    }

    public void ArchiveCrash(string logs)
    {
        var crashDir = CreateCrashDirectory();
        DateTime now = config.GetDateTimeOffseted();
        var file = config.CrashFileFormat
            .Replace(PORT_BALISE, port.ToString())
            .Replace(TIME_BALISE, now.ToString(config.FileTimeFormat));

        File.WriteAllText(Path.Combine(crashDir.FullName, file), logs);
    }

    public void RemoveOldArchives()
    {
        var regex = GetDateDirRegex();

        foreach (var dir in ApplicationPaths.Logs.GetDirectories())
        {
            try
            {
                if (TryGetTime(regex, dir.Name, out var date) && IsOld(date))
                    dir.Delete(true);

            }
            catch (Exception e)
            {
                AnsiConsole.WriteLine($"Error! RemovingOldArchives, {dir.Name}.");
                AnsiConsole.WriteException(e, Spectre.Console.ExceptionFormats.ShortenEverything);
            }
        }

        bool IsOld(DateTime time) => config.GetDateTimeOffseted().Subtract(time).Days > config.DayBeforeDeletion;
    }

    public void CompressOldArchives()
    {
        var regex = GetDateDirRegex();
        foreach (var dir in ApplicationPaths.Logs.GetDirectories())
        {
            try
            {
                if (TryGetTime(regex, dir.Name, out var date) && IsOld(date))
                {
                    using var zipFile = File.Open(dir.FullName + ZIP_EXTENSION, FileMode.CreateNew);
                    using var archive = new ZipArchive(zipFile, ZipArchiveMode.Create);

                    foreach (var file in dir.GetFiles())
                    {
                        archive.CreateEntryFromFile(file.FullName, file.Name);
                    }

                    dir.Delete(true);
                }    
            }
            catch (Exception e)
            {
                AnsiConsole.WriteLine($"Error! CompressOldArchives, {dir.Name}.");
                AnsiConsole.WriteException(e, Spectre.Console.ExceptionFormats.ShortenEverything);
            }
        }
        bool IsOld(DateTime time) => config.GetDateTimeOffseted().Subtract(time).Days > config.DayBeforeCompression;
    }

    private DirectoryInfo CreateCrashDirectory()
        => Directory.CreateDirectory(Path.Combine(ApplicationPaths.Logs.FullName, CRASH_DIRECTORY));

    private DirectoryInfo CreateDayDirectory()
    {
        var day = config.GetDateTimeOffseted().Date;
        var dir = config.DirectoryFormat
            .Replace(PORT_BALISE, port.ToString())
            .Replace(TIME_BALISE, day.ToString(config.DirectoryTimeFormat));
        return Directory.CreateDirectory(Path.Combine(ApplicationPaths.Logs.FullName, dir));
    }

    private Regex GetDateDirRegex()
    {
        var pattern = Regex.Escape(config.DirectoryFormat)
            .Replace(PORT_BALISE, port.ToString())
            .Replace(TIME_BALISE, $"(?<{DATE_CAPTUR_GROUP}>.+)");
        return new Regex(pattern);
    }

    private bool TryGetTime(Regex regex, string name, out DateTime time)
    {
        time = default;

        var match = regex.Match(name);
        if (!match.Success) return false;

        var group = match.Groups[DATE_CAPTUR_GROUP];
        if (!group.Success) return false;

        return DateTime.TryParseExact(group.ValueSpan,
                                    config.DirectoryFormat,
                                    CultureInfo.InvariantCulture,
                                    DateTimeStyles.None,
                                    out time);
    }
    #endregion

    //private string GetNamingFormat(Format format) => format switch
    //{
    //    Format.Log => config.FileFormat,
    //    Format.Crash => config.CrashFileFormat,
    //    Format.Directory => config.DirectoryFormat,
    //    _ => throw new ArgumentOutOfRangeException(nameof(format))
    //};

    //private string GetTimeFormat(Format format) => format switch
    //{
    //    Format.Log => config.FileTimeFormat,
    //    Format.Crash => config.FileTimeFormat,
    //    Format.Directory => config.DirectoryTimeFormat,
    //    _ => throw new ArgumentOutOfRangeException(nameof(format))
    //};

    //enum Format
    //{
    //    Log,
    //    Crash,
    //    Directory
    //}
}