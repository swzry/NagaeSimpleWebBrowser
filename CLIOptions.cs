using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommandLine;

namespace NagaeSimpleWebBrowser
{
    [Verb("go-url", HelpText = "Directly Open an URL.")]
    public class GoURLOptions
    { 
        [Option("url")]
        [JsonPropertyName("url")]
        public string? URL { get; set; }

        [Option("width", Default = 800)]
        [JsonPropertyName("width")]
        public int WindowWidth { get; set; } = 800;

        [Option("height", Default = 450)]
        [JsonPropertyName("height")]
        public int WindowHeight { get; set; } = 450;

        [Option("title", Default = "Nagae Simple Web Browser Window")]
        [JsonPropertyName("title")]
        public string? Title { get; set; } = "Nagae Simple Web Browser Window";

        [Option("disable-maximize-btn", Default = false)]
        [JsonPropertyName("disable-maximize-btn")]
        public bool DisableMaximizeButton { get; set; } = false;

        [Option("disable-minimize-btn", Default = false)]
        [JsonPropertyName("disable-minimize-btn")]
        public bool DisableMinimizeButton { get; set; } = false;

        [Option("maximize-on-show", Default = false)]
        [JsonPropertyName("maximize-on-show")]
        public bool MaximizeOnShow { get; set; } = false;

        [Option("no-resizable", Default = false)]
        [JsonPropertyName("no-resizable")]
        public bool NoResizable { get; set; } = false;
    }

    [Verb("with-nbcp", HelpText = "Start with nbcp mode.")]
    public class WithNBCPOptions
    { 
        [Option("url")]
        public string? URL { get; set; }
    }

    [Verb("cfg", isDefault: true, HelpText = "find config.json and start.")]
    public class WithCfgCLIOption
    {
        [Option('f', HelpText = "specify json file")]
        public string? file { get; set; }

        [Option('p', HelpText = "specify preset name" )]
        public string? preset { get; set; }
    }

    public class ConfigJsonDef
    {
        [JsonPropertyName("with-nbcp")]
        public WithNBCPOptions? NBCP { get; set; }

        [JsonPropertyName("go-url")]
        public GoURLOptions? GoURL { get; set; }
    }
}
