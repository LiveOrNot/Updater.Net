using Newtonsoft.Json;

namespace Updater.Net
{
    public class UpdateInfo
    {
        /// <summary>
        /// 版本号
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; }

        /// <summary>
        /// 更新包地址
        /// </summary>
        [JsonProperty("address")]
        public string Address { get; set; }

        /// <summary>
        /// 更新包哈希值
        /// </summary>
        [JsonProperty("hash")]
        public string Hash { get; set; }

        /// <summary>
        /// 强制更新
        /// </summary>
        [JsonProperty("force")]
        public bool Force { get; set; }

        /// <summary>
        /// 更新日志
        /// </summary>
        [JsonProperty("changelog")]
        public string Changelog { get; set; }

        /// <summary>
        /// 灰度配置
        /// </summary>
        [JsonProperty("grayscale")]
        public string Grayscale { get; set; }

        /// <summary>
        /// 拓展信息
        /// </summary>
        [JsonProperty("extension")]
        public string Extension { get; set; }
    }
}
