// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace Firefly.Client.Service
{
    public class AlarmConfirmerDataObject
    {
        #region constructor

        public AlarmConfirmerDataObject()
        {
        }

        public AlarmConfirmerDataObject(int id, bool isConfirmed, string note)
        {
            ID = id;
            IsConfirmed = isConfirmed;
            Note = note;
        }

        #endregion

        #region property

        [JsonPropertyName("id")]
        public int ID { get; set; }

        [JsonPropertyName("isConfirmed")]
        public bool IsConfirmed { get; set; }

        [JsonPropertyName("confirmedUser")]
        public string ConfirmedUser { get; set; }

        [JsonPropertyName("note")]
        public string Note { get; set; }

        #endregion
    }
}