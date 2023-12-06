using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;

namespace NagaeSimpleWebBrowser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class BrowserWindow : MetroWindow
    {
        private readonly string name;
        private readonly BrowserWindowsManager browserWindowsManager;
        private readonly IBrowserEventHandler browserEventHandler;
        private bool disableClose;
        private bool isWebView2CoreLoaded = false;
        public BrowserWindow(BrowserWindowsManager bwm, IBrowserEventHandler browserEventHandler, string name)
        {
            this.browserWindowsManager = bwm;
            this.name = name;
            this.browserEventHandler = browserEventHandler;
            this.Closed += BrowserWindow_Closed;
            this.Closing += BrowserWindow_Closing;
            InitializeComponent();
            this.webView.CoreWebView2InitializationCompleted += WebView_CoreWebView2InitializationCompleted;
        }

        private void BrowserWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if(disableClose)
            {
                e.Cancel = true;
            }
        }

        private void BrowserWindow_Closed(object? sender, EventArgs e)
        {
            browserEventHandler?.OnWindowClose(this.name);
        }
        private void WebView_CoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                LogInfo("webview2/core-load", "webview2 core initialzed.");
                isWebView2CoreLoaded = true;
                browserEventHandler?.OnWebView2CoreLoaded(this.name);
                AddConstantToScript();
            }
            else
            {
                LogInfo("webview2/core-load", "webview2 core initialzing failed.");
            }
        }

        private void AddConstantToScript()
        {
            try
            {
                webView.CoreWebView2.AddHostObjectToScript("nbcp", new ExposedConstant(
                    this.name,
                    this.browserWindowsManager.GetServerId(),
                    this.browserWindowsManager.GetSessionId(),
                    this.browserWindowsManager.GetSessionInfo(),
                    this.browserWindowsManager.GetNBCPServerURL()
                ));
            }
            catch (Exception ex)
            {
                LogError("webview2/expose-obj", ex);
            }
        }

        public void OpenBrowser(WindowOptions options) {
            this.Title = options.Title;
            this.webView.Source = new Uri(options.Url);
            if(options.HideFrame)
            {
                this.ShowTitleBar = false;
            }
            this.disableClose = options.DisableCloseButton;
            this.ShowCloseButton = !options.DisableCloseButton;
            this.Width = options.WindowWidth;
            this.Height = options.WindowHeight;
            if (options.MaximizeOnShow)
            {
                this.WindowState = WindowState.Maximized;
            }
            if (options.DisableMaximizeButton)
            {
                this.ShowMaxRestoreButton = false;
            }
            if (options.DisableMinimizeButton)
            {
                this.ShowMinButton = false;
            }
            if (options.NoResizable)
            {
                if (options.DisableMinimizeButton) {
                    this.ResizeMode = ResizeMode.NoResize;
                }
                else
                {
                    this.ResizeMode = ResizeMode.CanMinimize;
                }
            }
            this.Show();
        }

        public bool OpenDevTool()
        {
            if(!this.isWebView2CoreLoaded)
            {
                return false;
            }
            this.webView.CoreWebView2.OpenDevToolsWindow();
            return true;
        }

        public void ToggleMaximize()
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        protected void LogError(string field, Exception ex)
        {
            string f = string.Format("browser-window/{0}/{1}", this.name, field);
            this.browserWindowsManager.LogError(f, ex); 
        }

        protected void LogInfo(string field, string msg)
        {
            string f = string.Format("browser-window/{0}/{1}", this.name, field);
            this.browserWindowsManager.LogInfo(f, msg); 
        }
    }

    [ComVisible(true)]
    public class ExposedConstant
    {
        private readonly string windowName;
        private readonly string nbcpServerUrl;
        private readonly string serverId;
        private readonly string sessionId;
        private readonly string sessionInfo;

        public ExposedConstant (string windowName, string serverId, string sessionId, string sessionInfo, string nbcpServerUrl)
        {
            this.windowName = windowName;
            this.serverId = serverId;
            this.sessionId = sessionId;
            this.sessionInfo = sessionInfo;
            this.nbcpServerUrl = nbcpServerUrl;
        }

        public string getWindowName() { return windowName; }
        public string getServerId() { return serverId; }
        public string getSessionId() { return sessionId; }
        public string getSessionInfo() { return sessionInfo; }

        public string getNBCPServerUrl() { return nbcpServerUrl; }
    }
}
