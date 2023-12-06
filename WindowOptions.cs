using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NagaeSimpleWebBrowser
{
    public class WindowOptions
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public int WindowWidth { get; set; }
        public int WindowHeight { get; set; }
        
        public bool DisableMaximizeButton { get; set; }
        public bool DisableMinimizeButton { get; set; }
        public bool DisableCloseButton { get; set; }

        public bool MaximizeOnShow { get; set; }
        public bool HideFrame { get; set; }
        public bool NoResizable { get; set; }

        public WindowOptions() {
            Title = "Nagae Simple Web Browser Window";
            Url = "about:blank";
            WindowWidth = 800;
            WindowHeight = 450;
        }

        public WindowOptions(GoURLOptions CliOptions) {
            if (CliOptions.URL == null)
            {
                Url = "about:blank";
            }
            else
            {
                if(CliOptions.URL == "")
                {
                    Url = "about:blank";
                }
                else
                {
                    Url = CliOptions.URL;
                }
            }
            if(CliOptions.Title == null)
            {
                Title = "Nagae Simple Web Browser Window";
            }
            else
            {
                Title = CliOptions.Title;
            }
            WindowHeight = CliOptions.WindowHeight;
            WindowWidth = CliOptions.WindowWidth;
            if(WindowWidth <= 200)
            {
                WindowWidth = 200;
            }
            if(WindowHeight <= 200)
            {
                WindowHeight = 200;
            }
            DisableMaximizeButton = CliOptions.DisableMaximizeButton;
            DisableMinimizeButton = CliOptions.DisableMinimizeButton;
            MaximizeOnShow = CliOptions.MaximizeOnShow;
            HideFrame = false;
            DisableCloseButton = false;
            NoResizable = CliOptions.NoResizable;
        }

        public WindowOptions(Dictionary<string, object> param): this() { 
            if(param == null)
            {
                return;
            }
            if (param.ContainsKey("title") && param["title"]is string)
            {
                this.Title = (string)param["title"];
            }
            if (param.ContainsKey("url") && param["url"] is string)
            {
                this.Url = (string)param["url"];
            }
            if (param.ContainsKey("width") && param["width"] is int)
            {
                this.WindowWidth = (int)param["width"];
            }
            if (param.ContainsKey("height") && param["height"] is int)
            {
                this.WindowHeight = (int)param["height"];
            }
            if(WindowWidth <= 200)
            {
                WindowWidth = 200;
            }
            if(WindowHeight <= 200)
            {
                WindowHeight = 200;
            }
            if (param.ContainsKey("hide_frame") && param["hide_frame"] is bool)
            {
                this.HideFrame = (bool)param["hide_frame"];
            }
            if (param.ContainsKey("disable_maximize_btn") && param["disable_maximize_btn"] is bool)
            {
                this.DisableMaximizeButton = (bool)param["disable_maximize_btn"];
            }
            if (param.ContainsKey("disable_minimize_btn") && param["disable_minimize_btn"] is bool)
            {
                this.DisableMinimizeButton = (bool)param["disable_minimize_btn"];
            }
            if (param.ContainsKey("disable_close_btn") && param["disable_close_btn"] is bool)
            {
                this.DisableCloseButton = (bool)param["disable_close_btn"];
            }
            if (param.ContainsKey("maximize_on_show") && param["maximize_on_show"] is bool)
            {
                this.MaximizeOnShow = (bool)param["maximize_on_show"];
            }
            if (param.ContainsKey("no_resizable") && param["no_resizable"] is bool)
            {
                this.NoResizable = (bool)param["no_resizable"];
            }
        }
    }
}
