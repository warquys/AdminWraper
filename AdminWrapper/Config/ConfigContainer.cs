using System.Diagnostics;
using System.Reflection;
using System.Text;
using Spectre.Console;
using Syml;

// Taken and modify from
// https://github.com/AnomalousCoders/Neuron/blob/489e2422e00befedf5f7ec2ecb69a1985279794d/Neuron.Modules.Configs/ConfigContainer.cs

namespace AdminWrapper.Config;

public class ConfigContainer
{
    public string File { get; set; }
    public SymlDocument Document { get; set; } = new();

    public ConfigContainer(string path)
    {
        File = Path.ChangeExtension(path, ".syml");
        Load();
    }

    public T Get<T>() where T : IDocumentSection, new()
    {
        CheckTypeValidity(typeof(T), out var documentSectionAttribute);

        var exist = Document.Has<T>();
        if (exist)
        {
            var section = Document.Get<T>();
            // Document.Set(section);
            // Store();
            return section;
        }

        AnsiConsole.MarkupLine($"[yellow]Configuration section [bold]{documentSectionAttribute.SectionName}[/] not found.[/]");
        var newSection = new T();
        Document.Set(newSection);
        Store();
        return newSection;
    }

    public object Get(Type type)
    {
        CheckTypeValidity(type, out var documentSectionAttribute);

        var exist = Document.Sections.ContainsKey(documentSectionAttribute.SectionName);
        if (exist)
        {
            var section = Document.Get(type);
            // Document.Set(section);
            // Store();
            return section;
        }

        AnsiConsole.MarkupLine($"[yellow]Configuration section [bold]{documentSectionAttribute.SectionName}[/] not found.[/]");
        var newSection = Activator.CreateInstance(type)!;
        Document.Set(newSection);
        Store();
        return newSection;
    }


    public void Load()
    {
        if (!System.IO.File.Exists(File)) Store();
        AnsiConsole.MarkupLine($"[yellow]Loading configuration file [bold]{File}[/].[/]");
        Document.Load(System.IO.File.ReadAllText(File, Encoding.UTF8));
    }

    public void Store()
    {
        AnsiConsole.MarkupLine($"[yellow]Storing configuration file [bold]{File}[/].[/]");
        System.IO.File.WriteAllText(File, Document.Dump(), Encoding.UTF8);
    }

    public void LoadString(string content) => Document.Load(content);
    public string StoreString() => Document.Dump();

    [StackTraceHidden]
    private void CheckTypeValidity(Type type, out DocumentSectionAttribute documentSection)
    {
        if (!typeof(IDocumentSection).IsAssignableFrom(type))
            throw new ArgumentException($"{type} must implement {nameof(IDocumentSection)}", nameof(type));

        documentSection = type.GetCustomAttribute<DocumentSectionAttribute>()!;
        if (documentSection == null)
            throw new ArgumentException($"{type} must have {nameof(DocumentSectionAttribute)}", nameof(type));
    }
}