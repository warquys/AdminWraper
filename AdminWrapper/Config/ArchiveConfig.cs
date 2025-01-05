using System.ComponentModel;
using AdminWrapper.Logging;
using Syml;

namespace AdminWrapper.Config;

[DocumentSection("Logs Archive")]
public class ArchiveConfig : IDocumentSection
{

    [Description($"Format for the old log, \"{LogArchiver.TIME_BALISE}\" mean time, \"{LogArchiver.PORT_BALISE}\" mean port")]
    public string FileFormat { get; set; } = $"{LogArchiver.TIME_BALISE}.log";
    public string CrashFileFormat { get; set; } = $"{LogArchiver.TIME_BALISE}.crash.log";
    public string DirectoryFormat { get; set; } = $"{LogArchiver.TIME_BALISE}-{LogArchiver.PORT_BALISE}";

    public int TimeOffset { get; set; } = 0;
    public string FileTimeFormat { get; set; } = "hh-mm-ss";
    public string DirectoryTimeFormat { get; set; } = "yyyy-MM-dd";

    public int DayBeforeCompression { get; set; } = 5;
    public int DayBeforeDeletion { get; set; } = 30;

    public DateTime GetDateTimeOffseted()
        => DateTime.Now.AddHours(TimeOffset);
}
