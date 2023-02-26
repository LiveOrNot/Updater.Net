# Updater.Net

### Dependencies
> .Net Framework v4.0

> CommandLineParser v2.9.1

>  Costura.Fody v4.1.0

> Fody v6.0.0

> ICSharpCode.SharpZipLib.dll v0.85.4.369

> Newtonsoft.Json v6.0.0

> Serilog v1.5.14

### Tutorials
``` C#
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
``` 
### Extensions
#### Customize Update Fetch Action
``` C#
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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

            AutoUpdater.UpdateFetchEvent = AutoUpdater.UpdateFetchEvent = address =>
            {
                List<UpdateInfo> updateInfos = new List<UpdateInfo>();

                #region ���ļ���ȡ������Ϣ

                if (File.Exists(address))
                {
                    string json = File.ReadAllText(address);
                    updateInfos = JsonConvert.DeserializeObject<List<UpdateInfo>>(json);
                }

                #endregion

                #region �� HTTP(S) ��������ȡ������Ϣ

                if ((address?.StartsWith("http", StringComparison.CurrentCultureIgnoreCase) ?? false) || (address?.StartsWith("https", StringComparison.CurrentCultureIgnoreCase) ?? false))
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);
                    request.Method = "GET";
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    using (Stream stream = response.GetResponseStream())
                    {
                        StreamReader reader = new StreamReader(stream);
                        string json = reader.ReadToEnd();
                        updateInfos = JsonConvert.DeserializeObject<List<UpdateInfo>>(json);
                    }
                }

                #endregion

                return updateInfos;
            };

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
```
#### Customize Update Decide Action
``` C#
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

            AutoUpdater.UpdateDecideEvent = = (updateInfo, grayscale) =>
            {
                if (new Version(updateInfo?.Version) > Assembly.GetEntryAssembly().GetName().Version)
                {
                    if (!string.IsNullOrWhiteSpace(updateInfo.Grayscale) && !string.IsNullOrWhiteSpace(grayscale) && updateInfo.Grayscale.Contains(grayscale))
                    {
                        if (!updateInfo.Force)
                        {
                            MessageBoxResult result = MessageBox.Show(
                                $"��ǰ�汾��{Assembly.GetEntryAssembly().GetName().Version}{Environment.NewLine}" +
                                $"Ŀ��汾��{updateInfo.Version}{Environment.NewLine}" +
                                $"���°���ַ��{updateInfo.Address}{Environment.NewLine}" +
                                $"���°���ϣֵ��{updateInfo.Hash}{Environment.NewLine}" +
                                $"������־��{updateInfo.Changelog}{Environment.NewLine}" +
                                $"��չ��Ϣ��{updateInfo.Extension}",
                                "��鵽�°汾���Ƿ��������£�", MessageBoxButton.YesNo);
                            if (result == MessageBoxResult.Yes)
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (!updateInfo.Force)
                        {
                            MessageBoxResult result = MessageBox.Show(
                                $"��ǰ�汾��{Assembly.GetEntryAssembly().GetName().Version}{Environment.NewLine}" +
                                $"Ŀ��汾��{updateInfo.Version}{Environment.NewLine}" +
                                $"���°���ַ��{updateInfo.Address}{Environment.NewLine}" +
                                $"���°���ϣֵ��{updateInfo.Hash}{Environment.NewLine}" +
                                $"������־��{updateInfo.Changelog}{Environment.NewLine}" +
                                $"��չ��Ϣ��{updateInfo.Extension}",
                                "��鵽�°汾���Ƿ��������£�", MessageBoxButton.YesNo);
                            if (result == MessageBoxResult.Yes)
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    return false;
                }
            };

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
```
#### Customize More Actions
> git clone https://github.com/LiveOrNot/Updater.Net.git
