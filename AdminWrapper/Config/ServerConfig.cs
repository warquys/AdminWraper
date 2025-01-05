using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Syml;

namespace AdminWrapper.Config;

[DocumentSection("Server")]
public class ServerConfig : IDocumentSection
{
    public string ServerPath { get; set; } = "SCPSL.x86_64";

    public List<string> ScpSlArgument { get; set; } = new List<string>();

    [Description("Do not handle silence crash (when server infinite loop).")]
    public bool RestartWhenCrash { get; set; }
}
