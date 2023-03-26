using Autodesk.Navisworks.Api.Plugins;
using NavisCustomRibbon;
using NavisCustomRibbon.Control;
using System;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Rhino.Geometry;
using System.Net.NetworkInformation;
using System.Reflection;

namespace NavisCustomRibbon
{
    [Plugin("CustomRibbonAddin","LOR", DisplayName ="LOR")]
    [RibbonLayout("AddinRibbon.xaml")]
    [RibbonTab("ID_CustomTab_1", DisplayName ="LOR TAB")]
    [Command("ID_Button_1", Icon ="1_16.png", LargeIcon ="1_32.png", ToolTip = "Show a message")]
    public class CustomRibbon : CommandHandlerPlugin
    {
        public override int ExecuteCommand(string name, params string[] parameters)
        {
            //AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

            switch (name)
            {
                case "ID_Button_1":
                    if (!Autodesk.Navisworks.Api.Application.IsAutomated)
                    {
                        var pluginRecord = Autodesk.Navisworks.Api.Application.Plugins.FindPlugin("SignalSightDockPanel.LOR");
                        if (pluginRecord is DockPanePluginRecord && pluginRecord.IsEnabled)
                        {
                            var docPanel = (DockPanePlugin)(pluginRecord.LoadedPlugin ?? pluginRecord.LoadPlugin());
                            docPanel.ActivatePane();
                        }
                    }

                    break;
            }
            return 0;
        }

        //static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        //{
        //    return Assembly.LoadFile(@".\Rhino3dm.dll");
        //}
    }



    /*

    [Plugin("NavisPlugin", "LOR", DisplayName = "Signal Sighting")]
    public class NavisAddin : AddInPlugin
    {
        public override int Execute(params string[] parameters)
        {
            MessageBox.Show("Hello");
            return 0;
        }
    }

    */
}


namespace NavisDockPanel
{
    [Plugin("SignalSightDockPanel", "LOR", DisplayName = "SignalSightDockPanel")]
    [DockPanePlugin(160,350,AutoScroll = true, MinimumWidth = 160, FixedSize = false)]
    public class SignalSightDockPanel : DockPanePlugin
    {
        public override Control CreateControlPane()
        {
            //return new SignalSight() { Dock = DockStyle.Fill };

            //create an ElementHost
            ElementHost eh = new ElementHost();        
            eh.Dock = DockStyle.Fill;
            eh.AutoSize = true;

            //assign the control
            eh.Child = new UserControl1();

            eh.CreateControl();

            //return the ElementHost
            return eh;

        }

        public override void DestroyControlPane(Control pane)
        {
            try
            {
            //var ctrlSignalSight = (SignalSight)pane;
            //ctrlSignalSight?.Dispose();
            pane.Dispose();
            }
            catch(Exception) { }
        }
    }
}