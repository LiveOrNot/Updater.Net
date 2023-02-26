using CommandLine;

namespace Updater.Net
{
    /// <summary>
    /// 自动更新器相关操作
    /// </summary>
    [Verb("AutoUpdater", HelpText = "自动更新器相关操作")]
    internal class AutoUpdaterOptions
    {
        /// <summary>
        /// 工作模式
        /// </summary>
        [Option('m', "Mode", HelpText = "工作模式")]
        public string Mode { get; set; }

        /// <summary>
        /// 更新包文件路径
        /// </summary>
        [Option('p', "Path", HelpText = "更新包文件路径")]
        public string Path { get; set; }

        /// <summary>
        /// 应用根目录
        /// </summary>
        [Option('r', "Root", HelpText = "应用根目录")]
        public string Root { get; set; }

        /// <summary>
        /// 版本号
        /// </summary>
        [Option('v', "Version", HelpText = "版本号")]
        public string Version { get; set; }

        /// <summary>
        /// 更新包地址
        /// </summary>
        [Option('a', "Address", HelpText = "更新包地址")]
        public string Address { get; set; }

        /// <summary>
        /// 更新包哈希值
        /// </summary>
        [Option('h', "Hash", HelpText = "更新包哈希值")]
        public string Hash { get; set; }

        /// <summary>
        /// 强制更新
        /// </summary>
        [Option('f', "Force", HelpText = "强制更新")]
        public bool Force { get; set; }

        /// <summary>
        /// 更新日志
        /// </summary>
        [Option('c', "Changelog", HelpText = "更新日志")]
        public string Changelog { get; set; }

        /// <summary>
        /// 灰度配置
        /// </summary>
        [Option('g', "Grayscale", HelpText = "灰度配置")]
        public string Grayscale { get; set; }

        /// <summary>
        /// 拓展信息
        /// </summary>
        [Option('e', "Extension", HelpText = "拓展信息")]
        public string Extension { get; set; }
    }
}
