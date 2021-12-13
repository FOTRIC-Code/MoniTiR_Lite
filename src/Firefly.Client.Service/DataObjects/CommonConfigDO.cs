using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Firefly.Client.Service
{
    public class CommonConfigDO
    {
        /// <summary>
        /// 数据库数据保存时间
        /// </summary>
        [JsonPropertyName("dataRetentionTime")]
        public int DataRetentionTime { get; set; } = 900;

        /// <summary>
        /// 数据库报警保存时间
        /// </summary>
        [JsonPropertyName("alarmRetentionTime")]
        public int AlarmRetentionTime { get; set; } = 900;

        /// <summary>
        /// 日志保存时间
        /// </summary>
        [JsonPropertyName("logRetentionTime")]
        public int LogRetentionTime { get; set; } = 30;

        /// <summary>
        /// 语音报警重复次数
        /// </summary>
        [JsonPropertyName("speechCount")]
        public int SpeechCount { get; set; } = 3;

        /// <summary>
        /// 声音报警重复次数
        /// </summary>
        [JsonPropertyName("soundCount")]
        public int SoundCount { get; set; } = 3;

        /// <summary>
        /// 报警平台UI是否显示
        /// </summary>
        [JsonPropertyName("isAlarmOutEnable")]
        public bool IsAlarmOutEnable { get; set; } = true;
    }
}
