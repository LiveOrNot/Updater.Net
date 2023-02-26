using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Resources;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommandLine;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using Serilog;

namespace Updater.Net
{
    /// <summary>
    /// 自动更新器
    /// </summary>
    public static class AutoUpdater
    {
        #region 更新信息获取相关事件

        /// <summary>
        /// 更新信息获取事件（默认支持从文件、HTTP/HTTPS 服务器获取更新信息）
        /// <list type="table">
        /// <item>输入参数：更新地址</item>
        /// <item>输出参数：更新信息列表</item>
        /// </list>
        /// </summary>
        public static Func<string, IEnumerable<UpdateInfo>> UpdateFetchEvent { get; set; } = address =>
        {
            List<UpdateInfo> updateInfos = new List<UpdateInfo>();

            #region 从文件获取更新信息

            if (File.Exists(address))
            {
                string json = File.ReadAllText(address);
                updateInfos = JsonConvert.DeserializeObject<List<UpdateInfo>>(json);
            }

            #endregion

            #region 从 HTTP(S) 服务器获取更新信息

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

        /// <summary>
        /// 更新信息获取成功事件
        /// <list type="table">
        /// <item>输入参数 [0]：更新地址</item>
        /// <item>输入参数 [1]：更新信息</item>
        /// <item>输出参数：无</item>
        /// </list>
        /// </summary>
        public static Action<string, IEnumerable<UpdateInfo>> UpdateFetchSucceedEvent { get; set; } = (address, updateInfos) =>
        {

        };

        /// <summary>
        /// 更新信息获取失败事件
        /// <list type="table">
        /// <item>输入参数 [0]：更新地址</item>
        /// <item>输入参数 [1]：更新信息</item>
        /// <item>输入参数 [2]：异常信息</item>
        /// <item>输出参数：无</item>
        /// </list>
        /// </summary>
        public static Action<string, IEnumerable<UpdateInfo>, Exception> UpdateFetchFailedEvent { get; set; } = (address, updateInfos, exception) =>
        {
            throw exception;
        };

        #endregion

        #region 更新信息决策相关事件

        /// <summary>
        /// 更新信息决策事件（默认更新到首个高于当前入口程序集版本号的版本）
        /// <list type="table">
        /// <item>输入参数 [0]：更新信息</item>
        /// <item>输入参数 [1]：灰度标识</item>
        /// <item>输出参数：是否更新</item>
        /// </list>
        /// </summary>
        public static Func<UpdateInfo, string, bool> UpdateDecideEvent { get; set; } = (updateInfo, grayscale) =>
        {
            if (new Version(updateInfo?.Version) > Assembly.GetEntryAssembly().GetName().Version)
            {
                if (!string.IsNullOrWhiteSpace(updateInfo.Grayscale) && !string.IsNullOrWhiteSpace(grayscale) && updateInfo.Grayscale.Contains(grayscale))
                {
                    if (!updateInfo.Force)
                    {
                        MessageBoxResult result = MessageBox.Show(
                            $"当前版本：{Assembly.GetEntryAssembly().GetName().Version}{Environment.NewLine}" +
                            $"目标版本：{updateInfo.Version}{Environment.NewLine}" +
                            $"更新包地址：{updateInfo.Address}{Environment.NewLine}" +
                            $"更新包哈希值：{updateInfo.Hash}{Environment.NewLine}" +
                            $"更新日志：{updateInfo.Changelog}{Environment.NewLine}" +
                            $"拓展信息：{updateInfo.Extension}",
                            "检查到新版本，是否立即更新？", MessageBoxButton.YesNo);
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
                            $"当前版本：{Assembly.GetEntryAssembly().GetName().Version}{Environment.NewLine}" +
                            $"目标版本：{updateInfo.Version}{Environment.NewLine}" +
                            $"更新包地址：{updateInfo.Address}{Environment.NewLine}" +
                            $"更新包哈希值：{updateInfo.Hash}{Environment.NewLine}" +
                            $"更新日志：{updateInfo.Changelog}{Environment.NewLine}" +
                            $"拓展信息：{updateInfo.Extension}",
                            "检查到新版本，是否立即更新？", MessageBoxButton.YesNo);
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

        /// <summary>
        /// 更新信息决策成功事件
        /// <list type="table">
        /// <item>输入参数 [0]：更新信息</item>
        /// <item>输入参数 [1]：灰度标识</item>
        /// <item>输入参数 [2]：决策结果</item>
        /// <item>输出参数：无</item>
        /// </list>
        /// </summary>
        public static Action<UpdateInfo, string, bool> UpdateDecideSucceedEvent = (updateInfo, grayscale, result) =>
        {

        };

        /// <summary>
        /// 更新信息决策失败事件
        /// <list type="table">
        /// <item>输入参数 [0]：更新信息</item>
        /// <item>输入参数 [1]：灰度标识</item>
        /// <item>输入参数 [2]：决策结果</item>
        /// <item>输入参数 [3]：异常信息</item>
        /// <item>输出参数：无</item>
        /// </list>
        /// </summary>
        public static Action<UpdateInfo, string, bool, Exception> UpdateDecideFailedEvent = (updateInfo, grayscale, result, exception) =>
        {
            throw exception;
        };

        #endregion

        #region 更新包下载相关事件

        /// <summary>
        /// 更新包下载事件（默认支持从文件、HTTP/HTTPS 服务器获取更新包）
        /// <list type="table">
        /// <item>输入参数：更新信息</item>
        /// <item>输出参数：更新包下载完成后的文件路径</item>
        /// </list>
        /// </summary>
        public static Func<UpdateInfo, string> UpdateDownloadEvent { get; set; } = updateInfo =>
        {
            string updateFilePath = Path.Combine(UpdateDownloadDirectory, $"{Guid.NewGuid().ToString()}.zip");
            if (!Directory.Exists(UpdateDownloadDirectory)) Directory.CreateDirectory(UpdateDownloadDirectory);

            #region 从文件获取更新包

            if (File.Exists(updateInfo?.Address))
            {
                File.Copy(updateInfo.Address, updateFilePath, true);
            }

            #endregion

            #region 从 HTTP(S) 服务器获取更新包

            if ((updateInfo?.Address?.StartsWith("http", StringComparison.CurrentCultureIgnoreCase) ?? false) || (updateInfo?.Address?.StartsWith("https", StringComparison.CurrentCultureIgnoreCase) ?? false))
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(updateInfo.Address);
                request.Method = "GET";
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                using (Stream stream = response.GetResponseStream())
                {
                    using (FileStream fileStream = File.Create(updateFilePath))
                    {
                        int count = 0;
                        byte[] buffer = new byte[2048];
                        while ((count = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            fileStream.Write(buffer, 0, count);
                            buffer = new byte[2048];
                        }
                    }
                }
            }

            #endregion

            return updateFilePath;
        };

        /// <summary>
        /// 更新包下载成功事件
        /// <list type="table">
        /// <item>输入参数 [0]：更新信息</item>
        /// <item>输入参数 [1]：更新包下载文件路径</item>
        /// <item>输出参数：无</item>
        /// </list>
        /// </summary>
        public static Action<UpdateInfo, string> UpdateDownloadSucceedEvent = (updateInfo, downloadFilePath) =>
        {
            MessageBox.Show("更新包下载成功");
        };

        /// <summary>
        /// 更新包下载失败事件
        /// <list type="table">
        /// <item>输入参数 [0]：更新信息</item>
        /// <item>输入参数 [1]：更新包下载文件路径</item>
        /// <item>输入参数 [2]：异常信息</item>
        /// <item>输出参数：无</item>
        /// </list>
        /// </summary>
        public static Action<UpdateInfo, string, Exception> UpdateDownloadFailedEvent = (updateInfo, downloadFilePath, exception) =>
        {
            throw exception;
        };

        #endregion

        #region 更新操作相关事件

        /// <summary>
        /// 更新操作事件
        /// <list type="table">
        /// <item>输入参数 [0]：更新包文件路径</item>
        /// <item>输入参数 [1]：应用根目录</item>
        /// <item>输入参数 [2]：更新信息</item>
        /// <item>输出参数：更新操作完成后的入口文件路径</item>
        /// </list>
        /// </summary>
        public static Func<string, string, UpdateInfo, string> UpdateResolveEvent { get; set; } = (updateFilePath, rootDirectory, updateInfo) =>
        {
            string entryAssemblyFileName = Path.Combine(rootDirectory, Path.GetFileName(Assembly.GetEntryAssembly().Location));

            if (File.Exists(updateFilePath))
            {
                ZipUtil.UnZip(updateFilePath, rootDirectory, "", true);
            }

            return entryAssemblyFileName;
        };

        /// <summary>
        /// 更新操作成功事件
        /// <list type="table">
        /// <item>输入参数 [0]：更新包文件路径</item>
        /// <item>输入参数 [1]：应用根目录</item>
        /// <item>输入参数 [2]：更新信息</item>
        /// <item>输入参数 [3]：更新操作完成后的入口文件路径</item>
        /// <item>输出参数：无</item>
        /// </list>
        /// </summary>
        public static Action<string, string, UpdateInfo, string> UpdateResolveSucceedEvent { get; set; } = (updateFilePath, rootDirectory, updateInfo, entryAssemblyFileName) =>
        {
            MessageBox.Show("更新操作成功");
        };

        /// <summary>
        /// 更新操作失败事件
        /// <list type="table">
        /// <item>输入参数 [0]：更新包文件路径</item>
        /// <item>输入参数 [1]：应用根目录</item>
        /// <item>输入参数 [2]：更新信息</item>
        /// <item>输入参数 [3]：更新操作完成后的入口文件路径</item>
        /// <item>输入参数 [4]：异常信息</item>
        /// <item>输出参数：无</item>
        /// </list>
        /// </summary>
        public static Action<string, string, UpdateInfo, string, Exception> UpdateResolveFailedEvent { get; set; } = (updateFilePath, rootDirectory, updateInfo, entryAssemblyFileName, exception) =>
        {

        };

        #endregion

        #region 更新备份相关事件

        /// <summary>
        /// 更新备份操作成功事件
        /// <list type="table">
        /// <item>输入参数：更新信息</item>
        /// <item>输出参数：无</item>
        /// </list>
        /// </summary>
        public static Action<UpdateInfo> UpdateBackupSucceedEvent { get; set; } = updateInfo =>
        {

        };

        /// <summary>
        /// 更新备份操作失败事件
        /// <list type="table">
        /// <item>输入参数 [0]：更新信息</item>
        /// <item>输入参数 [1]：异常信息</item>
        /// <item>输出参数：无</item>
        /// </list>
        /// </summary>
        public static Action<UpdateInfo, Exception> UpdateBackupFailedEvent { get; set; } = (updateInfo, exception) =>
        {
            throw exception;
        };

        #endregion

        #region 更新回滚相关事件

        /// <summary>
        /// 更新失败后回滚操作成功事件
        /// <list type="table">
        /// <item>输入参数：更新信息</item>
        /// <item>输出参数：无</item>
        /// </list>
        /// </summary>
        public static Action<UpdateInfo> UpdateRollbackSucceedEvent { get; set; } = updateInfo =>
        {

        };

        /// <summary>
        /// 更新失败后回滚操作失败事件
        /// <list type="table">
        /// <item>输入参数 [0]：更新信息</item>
        /// <item>输入参数 [1]：异常信息</item>
        /// <item>输出参数：无</item>
        /// </list>
        /// </summary>
        public static Action<UpdateInfo, Exception> UpdateRollbackFailedEvent { get; set; } = (updateInfo, exception) =>
        {
            throw exception;
        };

        #endregion

        #region 自动更新器基础设置

        /// <summary>
        /// 是否启用更新日志记录器
        /// </summary>
        public static bool UpdateLoggerEnabled { get; set; } = true;

        /// <summary>
        /// 自动更新器进程互斥锁名称
        /// </summary>
        internal static string UpdateMutexName { get; set; } = $"{Assembly.GetEntryAssembly().GetName().Name}.AutoUpdater";

        /// <summary>
        /// 更新工作目录（默认为应用根目录下的 AutoUpdater 文件夹）
        /// </summary>
        internal static string UpdateWorkDirectory { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AutoUpdater");

        /// <summary>
        /// 更新日志目录（默认为应用根目录下的 AutoUpdater\Logs 文件夹）
        /// </summary>
        internal static string UpdateLogDirectory { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AutoUpdater", "Logs");

        /// <summary>
        /// 更新备份目录（默认为应用根目录下的 AutoUpdater\Backups 文件夹）
        /// </summary>
        internal static string UpdateBackupDirectory { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AutoUpdater", "Backups");

        /// <summary>
        /// 更新下载目录（默认为应用根目录下的 AutoUpdater\Downloads 文件夹）
        /// </summary>
        internal static string UpdateDownloadDirectory { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AutoUpdater", "Downloads");

        #endregion

        /// <summary>
        /// 初始化自动更新器（必须在应用入口所有其他操作前调用）
        /// </summary>
        /// <param name="args">命令行参数</param>
        public static void Initialize(string[] args)
        {
            if (Directory.Exists(UpdateWorkDirectory)) Directory.Delete(UpdateWorkDirectory, true);

            Parser.Default.ParseArguments<AutoUpdaterOptions>(args)
                .MapResult((AutoUpdaterOptions options) => OnAutoUpdaterOptionsExecute(options),
                errors => 0);
        }

        /// <summary>
        /// 启动自动更新器（必须在 <see cref="Initialize(string[])"/> 操作完成后调用）
        /// </summary>
        /// <param name="address">更新地址</param>
        /// <param name="grayscale">灰度标识</param>
        public static void Start(string address, string grayscale = null)
        {
            #region 初始化日志记录器

            ILogger logger = GetLogger();

            #endregion

            #region 检查是否有新版本

            List<UpdateInfo> updateInfos = null;
            try
            {
                logger?.Information($"开始检查是否有新版本");
                updateInfos = UpdateFetchEvent?.Invoke(address)?.ToList();
                logger?.Information($"检查是否有新版本成功,{nameof(address)}={address},{nameof(updateInfos)}={JsonConvert.SerializeObject(updateInfos)}");
                UpdateFetchSucceedEvent?.Invoke(address, updateInfos);
            }
            catch (Exception e)
            {
                logger?.Error($"检查是否有新版本失败,{nameof(e.Message)}={e.Message},{nameof(e.StackTrace)}={e.StackTrace}");
                UpdateFetchFailedEvent?.Invoke(address, updateInfos, e);
            }

            #endregion

            #region 决策是否应该更新

            UpdateInfo updateInfo = null;
            try
            {
                if (updateInfos?.Any() ?? false)
                {
                    logger?.Information($"开始决策是否应该更新");
                    for (int i = 0; i < updateInfos.Count; i++)
                    {
                        updateInfo = updateInfos.ElementAtOrDefault(i);
                        if (updateInfo != null)
                        {
                            if (UpdateDecideEvent?.Invoke(updateInfo, grayscale) ?? false)
                            {
                                logger?.Information($"决策是否应该更新成功,{nameof(updateInfo)}={JsonConvert.SerializeObject(updateInfo)},{nameof(grayscale)}={grayscale},true");
                                UpdateDecideSucceedEvent?.Invoke(updateInfo, grayscale, true);
                                break;
                            }
                            else
                            {
                                logger?.Information($"决策是否应该更新成功,{nameof(updateInfo)}={JsonConvert.SerializeObject(updateInfo)},{nameof(grayscale)}={grayscale},false");
                                updateInfo = null;
                                UpdateDecideSucceedEvent?.Invoke(updateInfo, grayscale, false);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger?.Error($"决策是否应该更新失败,{nameof(e.Message)}={e.Message},{nameof(e.StackTrace)}={e.StackTrace}");
                UpdateDecideFailedEvent?.Invoke(updateInfo, grayscale, updateInfo != null, e);
            }

            #endregion

            #region 下载新版本更新包

            string updateDownloadFilePath = null;
            try
            {
                if (updateInfo != null)
                {
                    logger?.Information($"开始下载新版本更新包");
                    updateDownloadFilePath = UpdateDownloadEvent?.Invoke(updateInfo);
                    logger?.Information($"下载新版本更新包成功,{nameof(updateInfo)}={JsonConvert.SerializeObject(updateInfo)},{updateDownloadFilePath}");
                    UpdateDownloadSucceedEvent?.Invoke(updateInfo, updateDownloadFilePath);
                }
            }
            catch (Exception e)
            {
                logger?.Error($"下载新版本更新包失败,{nameof(e.Message)}={e.Message},{nameof(e.StackTrace)}={e.StackTrace}");
                UpdateDownloadFailedEvent?.Invoke(updateInfo, updateDownloadFilePath, e);
            }

            #endregion

            #region 备份原有目录并在备份目录中启动自动更新器

            try
            {
                string entryAssemblyFileName = Path.GetFileName(Assembly.GetEntryAssembly().Location);
                string updateBackupZipFilePath = Path.Combine(UpdateBackupDirectory, $"{Guid.NewGuid().ToString()}.zip");
                if (File.Exists(updateDownloadFilePath))
                {
                    logger?.Information($"开始备份原有目录并在备份目录中启动自动更新器");
                    ZipUtil.ZipDirectory(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), updateBackupZipFilePath, new string[]
                    {
                        updateBackupZipFilePath, UpdateWorkDirectory
                    });
                    ZipUtil.UnZip(updateBackupZipFilePath, UpdateBackupDirectory, "", true);
                    File.Delete(updateBackupZipFilePath);
                    UpdateBackupSucceedEvent?.Invoke(updateInfo);
                    Process process = Process.Start(new ProcessStartInfo()
                    {
                        Verb = "runas",
                        FileName = Path.Combine(UpdateBackupDirectory, entryAssemblyFileName),
                        Arguments = $"AutoUpdater -m {AutoUpdaterModes.Update} -p {updateDownloadFilePath} -r {Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)} -v {updateInfo.Version ?? string.Empty} -a {updateInfo.Address ?? string.Empty} -h {updateInfo.Hash ?? string.Empty} -f {updateInfo.Force} -c {updateInfo.Changelog ?? string.Empty} -g {updateInfo.Grayscale ?? string.Empty} -e {updateInfo.Extension ?? string.Empty}"
                    });
                    logger?.Information($"备份原有目录并在备份目录中启动自动更新器成功");
                    Environment.Exit(0);
                }
            }
            catch (Exception e)
            {
                logger?.Error($"备份原有目录并在备份目录中启动自动更新器失败,{nameof(e.Message)}={e.Message},{nameof(e.StackTrace)}={e.StackTrace}");
                UpdateBackupFailedEvent?.Invoke(updateInfo, e);
                Environment.Exit(-1);
            }

            #endregion
        }

        /// <summary>
        /// 启动自动更新器（必须在 <see cref="Initialize(string[])"/> 操作完成后调用）
        /// </summary>
        /// <param name="updateInfos">更新信息列表</param>
        /// <param name="grayscale">灰度标识</param>
        public static void Start(List<UpdateInfo> updateInfos, string grayscale = null)
        {
            #region 初始化日志记录器

            ILogger logger = GetLogger();

            #endregion

            #region 决策是否应该更新

            UpdateInfo updateInfo = null;
            try
            {
                if (updateInfos?.Any() ?? false)
                {
                    logger?.Information($"开始决策是否应该更新");
                    for (int i = 0; i < updateInfos.Count; i++)
                    {
                        updateInfo = updateInfos.ElementAtOrDefault(i);
                        if (updateInfo != null)
                        {
                            if (UpdateDecideEvent?.Invoke(updateInfo, grayscale) ?? false)
                            {
                                logger?.Information($"决策是否应该更新成功,{nameof(updateInfo)}={JsonConvert.SerializeObject(updateInfo)},{nameof(grayscale)}={grayscale},true");
                                UpdateDecideSucceedEvent?.Invoke(updateInfo, grayscale, true);
                                break;
                            }
                            else
                            {
                                logger?.Information($"决策是否应该更新成功,{nameof(updateInfo)}={JsonConvert.SerializeObject(updateInfo)},{nameof(grayscale)}={grayscale},false");
                                updateInfo = null;
                                UpdateDecideSucceedEvent?.Invoke(updateInfo, grayscale, false);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger?.Error($"决策是否应该更新失败,{nameof(e.Message)}={e.Message},{nameof(e.StackTrace)}={e.StackTrace}");
                UpdateDecideFailedEvent?.Invoke(updateInfo, grayscale, updateInfo != null, e);
            }

            #endregion

            #region 下载新版本更新包

            string updateDownloadFilePath = null;
            try
            {
                if (updateInfo != null)
                {
                    logger?.Information($"开始下载新版本更新包");
                    updateDownloadFilePath = UpdateDownloadEvent?.Invoke(updateInfo);
                    logger?.Information($"下载新版本更新包成功,{nameof(updateInfo)}={JsonConvert.SerializeObject(updateInfo)},{updateDownloadFilePath}");
                    UpdateDownloadSucceedEvent?.Invoke(updateInfo, updateDownloadFilePath);
                }
            }
            catch (Exception e)
            {
                logger?.Error($"下载新版本更新包失败,{nameof(e.Message)}={e.Message},{nameof(e.StackTrace)}={e.StackTrace}");
                UpdateDownloadFailedEvent?.Invoke(updateInfo, updateDownloadFilePath, e);
            }

            #endregion

            #region 备份原有目录并在备份目录中启动自动更新器

            try
            {
                string entryAssemblyFileName = Path.GetFileName(Assembly.GetEntryAssembly().Location);
                string updateBackupZipFilePath = Path.Combine(UpdateBackupDirectory, $"{Guid.NewGuid().ToString()}.zip");
                if (File.Exists(updateDownloadFilePath))
                {
                    logger?.Information($"开始备份原有目录并在备份目录中启动自动更新器");
                    ZipUtil.ZipDirectory(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), updateBackupZipFilePath, new string[]
                    {
                        updateBackupZipFilePath, UpdateWorkDirectory
                    });
                    ZipUtil.UnZip(updateBackupZipFilePath, UpdateBackupDirectory, "", true);
                    File.Delete(updateBackupZipFilePath);
                    UpdateBackupSucceedEvent?.Invoke(updateInfo);
                    Process process = Process.Start(new ProcessStartInfo()
                    {
                        Verb = "runas",
                        FileName = Path.Combine(UpdateBackupDirectory, entryAssemblyFileName),
                        Arguments = $"AutoUpdater -m {AutoUpdaterModes.Update} -p {updateDownloadFilePath} -r {Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)} -v {updateInfo.Version ?? string.Empty} -a {updateInfo.Address ?? string.Empty} -h {updateInfo.Hash ?? string.Empty} -f {updateInfo.Force} -c {updateInfo.Changelog ?? string.Empty} -g {updateInfo.Grayscale ?? string.Empty} -e {updateInfo.Extension ?? string.Empty}"
                    });
                    logger?.Information($"备份原有目录并在备份目录中启动自动更新器成功");
                    Environment.Exit(0);
                }
            }
            catch (Exception e)
            {
                logger?.Error($"备份原有目录并在备份目录中启动自动更新器失败,{nameof(e.Message)}={e.Message},{nameof(e.StackTrace)}={e.StackTrace}");
                UpdateBackupFailedEvent?.Invoke(updateInfo, e);
                Environment.Exit(-1);
            }

            #endregion
        }

        /// <summary>
        /// 获取日志记录器
        /// </summary>
        /// <returns></returns>
        private static ILogger GetLogger(string logDirectory = null)
        {
            if (UpdateLoggerEnabled)
            {
                logDirectory = logDirectory ?? UpdateLogDirectory;
                if (!Directory.Exists(logDirectory)) Directory.CreateDirectory(logDirectory);
                Log.Logger = new LoggerConfiguration()
                            .WriteTo.RollingFile(Path.Combine(logDirectory, "{Date}.log"))
                            .CreateLogger();
                return Log.Logger;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 执行自动更新器相关操作
        /// </summary>
        /// <param name="options">自动更新器相关操作</param>
        /// <returns></returns>
        private static object OnAutoUpdaterOptionsExecute(AutoUpdaterOptions options)
        {
            Mutex updateMutex = null;
            UpdateInfo updateInfo = null;

            if (options?.Mode == AutoUpdaterModes.Update)
            {
                #region 初始化日志记录器

                string updateWorkDirectory = Path.Combine(options.Root, UpdateWorkDirectory.Replace(AppDomain.CurrentDomain.BaseDirectory, ""));
                string updateLogDirectory = Path.Combine(options.Root, UpdateLogDirectory.Replace(AppDomain.CurrentDomain.BaseDirectory, ""));
                string updateBackupDirectory = Path.Combine(options.Root, UpdateBackupDirectory.Replace(AppDomain.CurrentDomain.BaseDirectory, ""));
                string updateDownloadDirectory = Path.Combine(options.Root, UpdateDownloadDirectory.Replace(AppDomain.CurrentDomain.BaseDirectory, ""));

                ILogger logger = GetLogger(updateLogDirectory);

                #endregion

                #region 创建自动更新器进程互斥锁

                logger?.Information($"开始创建自动更新器进程互斥锁");
                bool createdNew = false;
                try
                {
                    updateMutex = new Mutex(true, UpdateMutexName, out createdNew);
                    if (createdNew)
                    {
                        logger?.Information($"创建自动更新器进程互斥锁成功,{nameof(updateMutex)}={UpdateMutexName}");
                        AppDomain.CurrentDomain.ProcessExit += (o, e) =>
                        {
                            #region 释放自动更新器进程互斥锁并退出自动更新器

                            try
                            {
                                logger?.Information($"开始释放自动更新器进程互斥锁并退出自动更新器");
                                updateMutex?.Dispose();
                                updateMutex = null;
                                logger?.Information($"释放自动更新器进程互斥锁并退出自动更新器成功");
                            }
                            catch (Exception ex)
                            {
                                logger?.Error($"释放自动更新器进程互斥锁并退出自动更新器失败,{nameof(ex.Message)}={ex.Message},{nameof(ex.StackTrace)}={ex.StackTrace}");
                            }

                            #endregion
                        };
                    }
                    else
                    {
                        logger?.Warning($"自动更新器进程互斥锁已存在,{nameof(updateMutex)}={UpdateMutexName}");
                        Environment.Exit(0);
                    }
                }
                catch (Exception e)
                {
                    logger?.Error($"创建自动更新器进程互斥锁失败,{nameof(e.Message)}={e.Message},{nameof(e.StackTrace)}={e.StackTrace}");
                    throw e;
                }

                #endregion

                #region 解压覆盖更新文件到原目录并启动应用

                Exception exception = null;
                string entryAssemblyFileName = null;
                logger?.Information($"开始解压覆盖更新文件到原目录并启动应用");
                try
                {
                    updateInfo = new UpdateInfo()
                    {
                        Version = options.Version,
                        Address = options.Address,
                        Hash = options.Hash,
                        Force = options.Force,
                        Changelog = options.Changelog,
                        Grayscale = options.Grayscale,
                        Extension = options.Extension
                    };
                    entryAssemblyFileName = UpdateResolveEvent?.Invoke(options.Path, options.Root, updateInfo);
                    UpdateResolveSucceedEvent?.Invoke(options.Path, options.Root, updateInfo, entryAssemblyFileName);
                    // 启动原目录中的应用
                    Process process = Process.Start(new ProcessStartInfo()
                    {
                        Verb = "runas",
                        FileName = entryAssemblyFileName
                    });
                    logger?.Information($"解压覆盖更新文件到原目录并启动应用成功,{nameof(options.Path)}={options.Path},{nameof(options.Root)}={options.Root},{nameof(updateInfo)}={JsonConvert.SerializeObject(updateInfo)}");
                    Environment.Exit(0);
                }
                catch (Exception e)
                {
                    logger?.Error($"解压覆盖更新文件到原目录并启动应用失败,{nameof(e.Message)}={e.Message},{nameof(e.StackTrace)}={e.StackTrace}");
                    UpdateResolveFailedEvent?.Invoke(options.Path, options.Root, updateInfo, entryAssemblyFileName, e);
                }

                #endregion

                #region 更新失败后回滚并启动原应用

                if (exception != null)
                {
                    logger?.Information($"开始更新失败后回滚并启动原应用");

                    try
                    {
                        entryAssemblyFileName = Path.GetFileName(Assembly.GetEntryAssembly().Location);
                        string updateRollbackZipFilePath = Path.Combine(updateBackupDirectory, $"{Guid.NewGuid().ToString()}.zip");
                        ZipUtil.ZipDirectory(updateBackupDirectory, updateRollbackZipFilePath, new string[]
                        {
                            updateRollbackZipFilePath
                        });
                        ZipUtil.UnZip(updateRollbackZipFilePath, options.Root, "", true);
                        UpdateRollbackSucceedEvent?.Invoke(updateInfo);
                        Process process = Process.Start(new ProcessStartInfo()
                        {
                            Verb = "runas",
                            FileName = Path.Combine(options.Root, entryAssemblyFileName)
                        });
                        logger?.Information($"更新失败后回滚并启动原应用成功,{nameof(updateInfo)}={JsonConvert.SerializeObject(updateInfo)}");
                        Environment.Exit(0);
                    }
                    catch (Exception ex)
                    {
                        logger?.Error($"更新失败后回滚并启动原应用失败,{nameof(ex.Message)}={ex.Message},{nameof(ex.StackTrace)}={ex.StackTrace}");
                        UpdateRollbackFailedEvent?.Invoke(updateInfo, ex);
                        Environment.Exit(-1);
                    }
                }

                #endregion
            }

            return null;
        }
    }
}
