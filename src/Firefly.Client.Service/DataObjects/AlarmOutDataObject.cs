// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace Firefly.Client.Service
{
    public class AlarmOutDataObject
    {
        #region constructor

        public AlarmOutDataObject(int channelID, ChannelCommand command)
        {
            ChannelID = channelID;
            Command = command;
        }

        #endregion

        #region property

        [JsonPropertyName("channelID")]
        public int ChannelID { get; set; }

        [JsonPropertyName("command")]
        public ChannelCommand Command { get; set; }

        #endregion
    }
}