using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Runtime.InteropServices;
using CommandLine;
using System.Text;
using System.Transactions;
using System.Security.Policy;
using System.IO;
using System.Text.Json;

namespace NagaeSimpleWebBrowser
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;
        private static bool consoleAttached;
        private static App? instance;
        public static App? INSTANCE { get { return instance; } }

        //private MainWindow? mainWindow;
        private bool enableConsole;
        private string mainURL;
        private bool isRunningNBCPClientMode;
        private NBCPC? nbcpClient;
        private BrowserWindowsManager browserWindowsManager;
        private WithNBCPOptions? nbcpCliOptions;
        private GoURLOptions? goURLCliOptions;

        [STAThread()]
        public static void Main(string[] args)
        {
            consoleAttached = AttachConsole(ATTACH_PARENT_PROCESS);
            if (consoleAttached)
            {
                Console.WriteLine("<I> [console] Attached.");
            }
            Parser.Default.ParseArguments(args, typeof(GoURLOptions), typeof(WithNBCPOptions), typeof(WithCfgCLIOption))
                .WithParsed<GoURLOptions>(CmdGoURL)
                .WithParsed<WithNBCPOptions>(CmdWithNBCP)
                .WithParsed<WithCfgCLIOption>(CmdWithCfg)
                .WithNotParsed(BadCLI);
        }

        private static void CmdWithCfg(WithCfgCLIOption cli)
        {
            string executablePath = AppDomain.CurrentDomain.BaseDirectory;
            string presetCfgPath = Path.Combine(executablePath, "cfg-presets");
            string configFilePath;
            if (cli.file != null && cli.file != "")
            {
                configFilePath = cli.file;
            }
            else if (cli.preset != null && cli.preset != "")
            {
                configFilePath = Path.Combine(presetCfgPath, string.Format("{0}.json", cli.preset));
            }
            else
            {
                configFilePath = Path.Combine(executablePath, "config.json");
            }
            if (consoleAttached) { 
                Console.WriteLine("<I> [cli] default mode."); 
                Console.WriteLine(string.Format("<I> [cli] base dir: {0}", executablePath)); 
            }
            if (File.Exists(configFilePath))
            {
                if (consoleAttached) {
                    Console.WriteLine(string.Format("<I> [cli] found '{0}'.", configFilePath));
                }
                string jsonContent;
                try
                {
                    jsonContent = File.ReadAllText(configFilePath);
                }catch(Exception e)
                {
                    if (consoleAttached)
                    {
                        Console.WriteLine(string.Format("<E> [config-parser] Exception '{0}' in reading config: {1}", e.GetType().Name, e));
                    } 
                    return;
                }
                ConfigJsonDef? jconfig = null;
                try
                {
                   jconfig = JsonSerializer.Deserialize<ConfigJsonDef>(jsonContent);
                }
                catch (Exception e)
                {
                    if (consoleAttached)
                    {
                        Console.WriteLine(string.Format("<E> [config-parser] Exception '{0}' in parsing config: {1}", e.GetType().Name, e));
                    } 
                }
                if (jconfig == null)
                {
                    if (consoleAttached)
                    {
                        Console.WriteLine("<E> [cli] config.json parsed but get null result.");
                    }
                    return;
                }
                else
                {
                    if(jconfig.GoURL != null)
                    {
                        if (consoleAttached)
                        {
                            Console.WriteLine("<I> [cli] enter 'go-url' mode with config.json.");
                        }
                        CmdGoURL(jconfig.GoURL);
                        return;
                    }
                    if(jconfig.NBCP != null)
                    {
                        if (consoleAttached)
                        {
                            Console.WriteLine("<I> [cli] enter 'with-nbcp' mode with config.json.");
                        }
                        CmdWithNBCP(jconfig.NBCP);
                        return;
                    }
                    if (consoleAttached)
                    {
                        Console.WriteLine("<E> [cli] config.json parsed but neither 'go-url' nor 'with-nbcp' field found.");
                    }
                    return;
                }
            }
            else
            {
                if (consoleAttached) {
                    Console.WriteLine("<E> [cli] config.json not found. You can:");
                    Console.WriteLine("\t1. provide 'config.json' in base dir.");
                    Console.WriteLine("\t2. provide json file by '-f' flag.");
                    Console.WriteLine("\t3. provide json file in 'cfg-presets' dir,");
                    Console.WriteLine("\t\tthen specify name by '-p' flag (without extension).");
                    Console.WriteLine("\t4. use cli verbs: 'go-url' or 'with-nbcp'.");
                }
                return;
            }
        }

        private static void CmdGoURL(GoURLOptions cli)
        {
            if (consoleAttached) { Console.WriteLine("<I> [cli] goto-url mode."); }
            Dictionary<string, object> opt = new Dictionary<string, object>();
            var uri = cli.URL;
            if (uri == null)
            {
                uri = "";
            }
            instance = new App(consoleAttached, uri, cli);
            instance.Run();
        }
        private static void CmdWithNBCP(WithNBCPOptions cli) {
            if (consoleAttached) { Console.WriteLine("<I> [cli] nbcp mode."); }
            Dictionary<string, object> opt = new Dictionary<string, object>();
            var uri = cli.URL;
            if(uri == null)
            {
                uri = "";
            }
            instance = new App(consoleAttached, uri, cli);
            instance.Run();
        }

        private static void BadCLI(IEnumerable<Error> err) {
            if (consoleAttached) {
                Console.WriteLine("<E> [cli] Bad Arguments:");
                foreach(var i in err){
                    Console.WriteLine(i);
                }
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Bad Arguments:");
                foreach(var i in err){
                    sb.AppendLine(i.ToString());
                }
                MessageBox.Show(sb.ToString(), "Nagae Simple Web Browser", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            System.Environment.Exit(0);
        }

        public App(bool enableConsole, string uri, WithNBCPOptions cliOpt)
        {
            this.enableConsole = enableConsole;
            this.isRunningNBCPClientMode = true;
            this.mainURL = uri;
            this.nbcpCliOptions = cliOpt;
            this.browserWindowsManager = new BrowserWindowsManager(true);
            this.InitializeComponent();
        }

        public App(bool enableConsole, string uri, GoURLOptions cliOpt)
        {
            this.enableConsole = enableConsole;
            this.isRunningNBCPClientMode = false;
            this.mainURL = uri;
            this.goURLCliOptions = cliOpt;
            this.browserWindowsManager = new BrowserWindowsManager(false);
            this.InitializeComponent();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            if (this.enableConsole) { Console.WriteLine("<I> [app] startup."); }
            browserWindowsManager.ErrorHandler += handleErrorLog;
            browserWindowsManager.InfoLogHandler += handleInfoLog;
            if (this.isRunningNBCPClientMode)
            {
                nbcpClient = new NBCPC(this.browserWindowsManager, this.mainURL);
                this.browserWindowsManager.nbcpClient = this.nbcpClient;
                nbcpClient.ErrorHandler += handleErrorLog;
                nbcpClient.InfoLogHandler += handleInfoLog;
                nbcpClient.OnClientExit += handleNBCPClientExit;
                nbcpClient.Start();
            }
            else
            {
                this.browserWindowsManager.OpenURLWithoutNBCP(this.goURLCliOptions);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (this.enableConsole) { Console.WriteLine("<I> [app] exiting..."); }
            if (this.nbcpClient != null)
            {
                this.nbcpClient.Stop();
            }
            base.OnExit(e);
        }

        private void handleErrorLog(string errLocation, Exception exception)
        {
            if (this.enableConsole)
            {
                Console.WriteLine(string.Format("<E> [{0}] Exception: {1}", errLocation, exception.GetType().FullName));
                Console.WriteLine("Detail:");
                Console.WriteLine(exception.Message);
                Console.WriteLine("Stacktrace:");
                Console.WriteLine(exception.StackTrace);
            }
        }

        private void handleInfoLog(string location, string msg)
        {
            if (this.enableConsole)
            {
                Console.WriteLine(string.Format("<I> [{0}] {1}", location, msg));
            }
        }

        private void handleNBCPClientExit()
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                this.Shutdown();
            }));
        }
    }
}
