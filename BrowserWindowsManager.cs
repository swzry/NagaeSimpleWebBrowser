using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using nbcpClientLibv1;

namespace NagaeSimpleWebBrowser
{
    public class BrowserWindowEntry
    {
        private readonly string name;
        private readonly BrowserWindowsManager browserWindowsManager;
        private readonly BrowserWindow window;
        public WindowOptions Options { get; set; }
        
        public string Name { get { return name; } }
        public BrowserWindowEntry(BrowserWindowsManager bwm, string name, WindowOptions options)
        {
            this.name = name;
            this.browserWindowsManager = bwm;
            this.window = new BrowserWindow(this.browserWindowsManager, this.browserWindowsManager, this.name);
            Options = options;
        }

        public void OpenWindow()
        {
            this.window.OpenBrowser(this.Options);
        }

        public BrowserWindow GetWindow()
        {
            return this.window;
        }
    }

    public class BrowserWindowsManager : IBrowserEventHandler
    {
        private readonly bool isNBCPMode;
        private Dictionary<string, BrowserWindowEntry> windows = new Dictionary<string, BrowserWindowEntry>();

        public NBCPC? nbcpClient { get; set; }

        public delegate void ErrorHandlerDelegate(string errLocation, Exception exception);
        public delegate void InfoLogHandlerDelegate(string location, string msg);

        public event ErrorHandlerDelegate? ErrorHandler;
        public event InfoLogHandlerDelegate? InfoLogHandler;

        public BrowserWindowsManager(bool nbcpMode, NBCPC? nbcpClient = null)
        {
            this.isNBCPMode = nbcpMode;
            this.nbcpClient = nbcpClient;
        }

        public void LogError(string errLocation, Exception exception)
        {
            this.ErrorHandler?.Invoke(errLocation, exception);
        }

        public void LogInfo(string errLocation, string msg)
        {
            this.InfoLogHandler?.Invoke(errLocation, msg);
        }

        public void OpenURLWithoutNBCP(GoURLOptions? cliOpt)
        {
            if (this.isNBCPMode)
            {
                return;
            }
            WindowOptions winOpt;
            if (cliOpt == null)
            {
                winOpt = new WindowOptions();
            }
            else
            {
                winOpt= new WindowOptions(cliOpt);
            }
            NewBrowserWindow("main", winOpt);
        }

        private void NewBrowserWindow(string name, WindowOptions options)
        {
            if(windows.ContainsKey(name))
            {
                throw new WindowNameExistsException(name);
            }
            BrowserWindowEntry entry = new BrowserWindowEntry(this, name, options);
            windows[name] = entry;
            entry.OpenWindow();
        }

        public void OnWebView2CoreLoaded(string name) {
            InfoLogHandler?.Invoke("bwm/webview2-core-loaded", string.Format("webview2 core loaded on window '{0}'.", name));
            if (isNBCPMode && this.nbcpClient != null)
            {
                var cpm_ext = new Dictionary<string, object>();
                cpm_ext["name"] = name;
                this.nbcpClient.SendPostMsg(new ClientPostMessage("webview2_core_loaded", cpm_ext));
            }
        }

        public void OnWindowClose(string name)
        {
            InfoLogHandler?.Invoke("bwm/window-closed", string.Format("window '{0}' closed.", name));
            windows.Remove(name);
            if(isNBCPMode && this.nbcpClient != null)
            {
                var cpm_ext = new Dictionary<string, object>();
                cpm_ext["name"] = name;
                this.nbcpClient.SendPostMsg(new ClientPostMessage("window_closed", cpm_ext));
            }
            if(windows.Count <= 0)
            {
                onLastWindowClosed();
            }
        }

        private void onLastWindowClosed() {
            InfoLogHandler?.Invoke("bwm/all-window-closed", "all windows closed.");
            if (isNBCPMode) {
                if (this.nbcpClient != null)
                {
                    this.nbcpClient.SendPostMsg(new ClientPostMessage("all_window_closed"));
                }
            }
            else {
                App.INSTANCE?.Shutdown();
            }
        }

        public RPCResult DoRPC(string action, Dictionary<string, object> param)
        {
            switch(action)
            {
                case "new_window": return RPCNewWindow(param);
                case "open_dev_tool": return RPCOpenDevTool(param);
                case "toggle_maximize": return RPCToggleMaximize(param);
                case "minimize": return RPCMinimize(param);
                case "close_window": return RPCCloseWindow(param);
                case "set_titlebar_display": return RPCSetTitleBarDisplay(param);
                default:
                    return RPCResult.Fail(string.Format("invalid rpc action '{0}'", action));
            }
        }

        private RPCResult RPCNewWindow(Dictionary<string, object> param)
        {
            if(param == null)
            {
                return RPCResult.Fail("invalid rpc arguments: null arguments");
            }
            if((!param.ContainsKey("name")) || (!(param["name"] is string)))
            {
                return RPCResult.Fail("invalid rpc arguments: field 'name' corrupted.");
            }
            string name = (string)param["name"];
            if(name == null || name == "")
            {
                return RPCResult.Fail("invalid rpc arguments: field 'name' should have non-empty value.");
            }
            if(windows.ContainsKey(name))
            {
                return RPCResult.Fail(string.Format("rpc 'new_window': can not create window named '{0}': window with this name is already exists.", name));
            }
            WindowOptions options = new WindowOptions(param);
            BrowserWindowEntry entry = new BrowserWindowEntry(this, name, options);
            windows[name] = entry;
            entry.OpenWindow();
            return RPCResult.Success();
        }

        private RPCResult RPCOpenDevTool(Dictionary<string, object> param)
        {
            if(param == null)
            {
                return RPCResult.Fail("invalid rpc arguments: null arguments");
            }
            if((!param.ContainsKey("name")) || (!(param["name"] is string)))
            {
                return RPCResult.Fail("invalid rpc arguments: field 'name' corrupted.");
            }
            string name = (string)param["name"];
            if(name == null || name == "")
            {
                return RPCResult.Fail("invalid rpc arguments: field 'name' should have non-empty value.");
            }
            if(!windows.ContainsKey(name))
            {
                return RPCResult.Fail(string.Format("rpc 'open_dev_tool': can not open dev tool for window named '{0}': no such window (0x114).", name));
            }
            BrowserWindowEntry bwe = windows[name];
            if(bwe == null) { 
                return RPCResult.Fail(string.Format("rpc 'open_dev_tool': can not open dev tool for window named '{0}': no such window (0x514).", name));
            }
            bool ok = bwe.GetWindow().OpenDevTool();
            if (!ok)
            {
                return RPCResult.Fail(string.Format("rpc 'open_dev_tool': can not open dev tool for window named '{0}': webview2 core not loaded.", name));
            }
            return RPCResult.Success();
        }

        private RPCResult RPCToggleMaximize(Dictionary<string, object> param)
        {
            if(param == null)
            {
                return RPCResult.Fail("invalid rpc arguments: null arguments");
            }
            if((!param.ContainsKey("name")) || (!(param["name"] is string)))
            {
                return RPCResult.Fail("invalid rpc arguments: field 'name' corrupted.");
            }
            string name = (string)param["name"];
            if(name == null || name == "")
            {
                return RPCResult.Fail("invalid rpc arguments: field 'name' should have non-empty value.");
            }
            if(!windows.ContainsKey(name))
            {
                return RPCResult.Fail(string.Format("rpc 'toggle_maximize': can not open dev tool for window named '{0}': no such window (0x114).", name));
            }
            BrowserWindowEntry bwe = windows[name];
            if(bwe == null) { 
                return RPCResult.Fail(string.Format("rpc 'toggle_maximize': can not open dev tool for window named '{0}': no such window (0x514).", name));
            }
            bwe.GetWindow().ToggleMaximize();
            return RPCResult.Success();
        }

        private RPCResult RPCMinimize(Dictionary<string, object> param)
        {
            if(param == null)
            {
                return RPCResult.Fail("invalid rpc arguments: null arguments");
            }
            if((!param.ContainsKey("name")) || (!(param["name"] is string)))
            {
                return RPCResult.Fail("invalid rpc arguments: field 'name' corrupted.");
            }
            string name = (string)param["name"];
            if(name == null || name == "")
            {
                return RPCResult.Fail("invalid rpc arguments: field 'name' should have non-empty value.");
            }
            if(!windows.ContainsKey(name))
            {
                return RPCResult.Fail(string.Format("rpc 'minimize': can not open dev tool for window named '{0}': no such window (0x114).", name));
            }
            BrowserWindowEntry bwe = windows[name];
            if(bwe == null) { 
                return RPCResult.Fail(string.Format("rpc 'minimize': can not open dev tool for window named '{0}': no such window (0x514).", name));
            }
            bwe.GetWindow().WindowState = System.Windows.WindowState.Minimized;
            return RPCResult.Success();
        }

        private RPCResult RPCCloseWindow(Dictionary<string, object> param)
        {
            if(param == null)
            {
                return RPCResult.Fail("invalid rpc arguments: null arguments");
            }
            if((!param.ContainsKey("name")) || (!(param["name"] is string)))
            {
                return RPCResult.Fail("invalid rpc arguments: field 'name' corrupted.");
            }
            string name = (string)param["name"];
            if(name == null || name == "")
            {
                return RPCResult.Fail("invalid rpc arguments: field 'name' should have non-empty value.");
            }
            if(!windows.ContainsKey(name))
            {
                return RPCResult.Fail(string.Format("rpc 'close_window': can not open dev tool for window named '{0}': no such window (0x114).", name));
            }
            BrowserWindowEntry bwe = windows[name];
            if(bwe == null) { 
                return RPCResult.Fail(string.Format("rpc 'close_window': can not open dev tool for window named '{0}': no such window (0x514).", name));
            }
            bwe.GetWindow().Close();
            return RPCResult.Success();
        }

        private RPCResult RPCSetTitleBarDisplay(Dictionary<string, object> param)
        {
            if(param == null)
            {
                return RPCResult.Fail("invalid rpc arguments: null arguments");
            }
            if((!param.ContainsKey("name")) || (!(param["name"] is string)))
            {
                return RPCResult.Fail("invalid rpc arguments: field 'name' corrupted.");
            }
            if((!param.ContainsKey("value")) || (!(param["value"] is bool)))
            {
                return RPCResult.Fail("invalid rpc arguments for 'set_titlebar_display': field 'value' corrupted.");
            }
            string name = (string)param["name"];
            bool value = (bool)param["value"];
            if(name == null || name == "")
            {
                return RPCResult.Fail("invalid rpc arguments: field 'name' should have non-empty value.");
            }
            if(!windows.ContainsKey(name))
            {
                return RPCResult.Fail(string.Format("rpc 'set_titlebar_display': can not open dev tool for window named '{0}': no such window (0x114).", name));
            }
            BrowserWindowEntry bwe = windows[name];
            if(bwe == null) { 
                return RPCResult.Fail(string.Format("rpc 'set_titlebar_display': can not open dev tool for window named '{0}': no such window (0x514).", name));
            }
            bwe.GetWindow().ShowTitleBar = value;
            return RPCResult.Success();
        }

        public string GetSessionId()
        {
            if(this.nbcpClient != null)
            {
                return this.nbcpClient.GetSessionId();
            }
            else
            {
                return "";
            }
        }

        public string GetSessionInfo()
        {
            if(this.nbcpClient != null)
            {
                return this.nbcpClient.GetSessionInfo();
            }
            else
            {
                return "";
            }
        }

        public string GetServerId()
        {
            if (this.nbcpClient == null)
            {
                return "";
            }
            else
            {
                return this.nbcpClient.GetServerId();
            }
        }

        public string GetNBCPServerURL()
        {
            if (this.nbcpClient == null)
            {
                return "";
            }
            else
            {
                return this.nbcpClient.GetNBCPServerURL();
            }
        }
    }

    public class WindowNameExistsException : Exception
    {
        public WindowNameExistsException(string name):
            base(string.Format("can not create window named '{0}': window with this name is already exists.", name))
        { }
    }

    public interface IBrowserEventHandler
    {
        void OnWebView2CoreLoaded(string name);
        void OnWindowClose(string name);
    }
}
