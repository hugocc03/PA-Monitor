using System;
using System.IO;
using System.Reflection;
using System.ComponentModel.Composition;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Interfaces;

namespace PAMonitor.XrmToolBox
{
    [Export(typeof(IXrmToolBoxPlugin)),
     ExportMetadata("Name", "PA Run Monitor"),
     ExportMetadata("Description", "Power Automate Cloud run monitor with nested child-flow tree"),
     ExportMetadata("SmallImageBase64", null),
     ExportMetadata("BigImageBase64", null),
     ExportMetadata("BackgroundColor", "WhiteSmoke"),
     ExportMetadata("PrimaryFontColor", "Black"),
     ExportMetadata("SecondaryFontColor", "DimGray")]
    public class Plugin : PluginBase
    {
        static Plugin()
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        public override IXrmToolBoxPluginControl GetControl()
        {
            return new PluginControl();
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var name = new AssemblyName(args.Name).Name + ".dll";
            var folder = Path.Combine(
                Path.GetDirectoryName(typeof(Plugin).Assembly.Location) ?? string.Empty,
                "PAMonitor.XrmToolBox");

            var candidate = Path.Combine(folder, name);
            return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
        }
    }
}
