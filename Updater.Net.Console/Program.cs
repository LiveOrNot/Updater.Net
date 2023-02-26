using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Lifetime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Updater.Net.Console
{
    internal class Program
    {
        static void Main(string[] args)
        {
            AutoUpdater.Initialize(args);
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(3000);
                AutoUpdater.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Release.json"));
            });
            System.Console.WriteLine(Assembly.GetEntryAssembly().GetName().Version.ToString());
            System.Console.ReadLine();
        }
    }
}
