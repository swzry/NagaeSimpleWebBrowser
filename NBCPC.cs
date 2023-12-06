using nbcpClientLibv1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NagaeSimpleWebBrowser
{
    public class NBCPC : NBCPLogicHandler
    {
        private NBCPClient client;
        private readonly string nbcpServerUrl;

        public delegate void ErrorHandlerDelegate(string errLocation, Exception exception);
        public delegate void InfoLogHandlerDelegate(string location, string msg);
        public delegate void ClientExitDelegate();
        public event ErrorHandlerDelegate? ErrorHandler;
        public event InfoLogHandlerDelegate? InfoLogHandler;
        public event ClientExitDelegate? OnClientExit;
        
        private readonly BrowserWindowsManager browserWindowsManager;

        public NBCPC(BrowserWindowsManager bwm, string serverURL) {
            this.nbcpServerUrl = serverURL;
            client = new NBCPClient(serverURL, 1, 1, this);
            this.browserWindowsManager = bwm;
            client.ErrorHandler += this.handleNBCPError;
            client.InfoLogHandler += this.handleNBCPInfoLog;
            client.OnClientExit += this.handleNBCPEnd;
        }

        public string GetNBCPServerURL()
        {
            return nbcpServerUrl;
        }

        public void Start()
        {
            client.Start();
        }

        public void Stop()
        {
            client.Stop();
        }

        private void handleNBCPError(string errLocation, Exception exception)
        {
            ErrorHandler?.Invoke(errLocation, exception);
        }

        private void handleNBCPInfoLog(string location, string msg)
        {
            InfoLogHandler?.Invoke(location, msg);
        }

        private void handleNBCPEnd()
        {
            OnClientExit?.Invoke();
        }

        public RPCResult RPCHandler(string action, Dictionary<string, object> param)
        {
            InfoLogHandler?.Invoke("debug/rpc-handler/action", string.Format("action '{0}': ", action));
            foreach(KeyValuePair<string, object> kvp in param)
            {
                InfoLogHandler?.Invoke("debug/rpc-handler/action", string.Format("\t'{0}': {1}", kvp.Key, kvp.Value));
            }
            if(App.INSTANCE == null)
            {
                ErrorHandler?.Invoke("debug/rpc-handler/what-the-xxxx", new NullReferenceException("App.INSTANCE is null"));
            }
            RPCResult? rs = App.INSTANCE?.Dispatcher.Invoke(new Func<RPCResult>(() => {
                return this.browserWindowsManager.DoRPC(action, param);
            }));
            if(rs == null)
            {
                rs = RPCResult.Success();
            }
            if (rs.isSuccess)
            {
                InfoLogHandler?.Invoke("debug/rpc-handler/result", string.Format("\taction '{0}': OK.", action));
            }
            else
            {
                if(rs.ErrMsg!= null)
                {
                    InfoLogHandler?.Invoke("debug/rpc-handler/result", string.Format("\taction '{0}': failed: {1}", action, rs.ErrMsg));
                }
                else
                {
                    InfoLogHandler?.Invoke("debug/rpc-handler/result", string.Format("\taction '{0}': failed.", action));
                }
            }
            if (rs.ExtraInfo != null)
            {
                foreach (KeyValuePair<string, object> kvp in rs.ExtraInfo)
                {
                    InfoLogHandler?.Invoke("debug/rpc-handler/result/extra-data", string.Format("\t'{0}': {1}", kvp.Key, kvp.Value));
                }
            }
            return rs;
        }

        public bool WillReAttach()
        {
            bool ret = App.Current.Dispatcher.Invoke(new Func<bool>(() => {
                ReattachDialog rd = new ReattachDialog();
                var res = rd.ShowDialog();
                var ret = res.GetValueOrDefault(false);
                return ret;
            }));
            return ret;
        }

        public void SendPostMsg(ClientPostMessage cpm)
        {
            this.client.SendClientPostMsg(cpm);
        }

        public string GetSessionId()
        {
            return this.client.GetSessionId();
        }

        public string GetSessionInfo()
        {
            return this.client.GetSessionInfo();
        }

        public string GetServerId()
        {
            return this.client.GetServerId();
        }
    }
}
