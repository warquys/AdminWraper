// See https://aka.ms/new-console-template for more information
using AdminWrapper;
using AdminWrapper.Resource;
using Spectre.Console.Cli;
using static System.Runtime.InteropServices.JavaScript.JSType;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName(AssemblyInfo.NAME);
    config.SetApplicationVersion(AssemblyInfo.VERSION);
    
    config.AddCommand<StartCommand>("start")
        .WithDescription(LocalizedLog.StartCommandDescription)
        .WithExample(new[] { "start", "7777" });

    config.ValidateExamples();
});

#if DEBUG
app.Run(new[] { "start", "7777", "--rd_stderr", "--rd_std" });
#else
app.Run(args);
#endif


