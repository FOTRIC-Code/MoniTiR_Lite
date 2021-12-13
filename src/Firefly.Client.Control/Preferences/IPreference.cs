// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using Newtonsoft.Json;

namespace Firefly.Client.Control
{
    public interface IPreference
    {
        [JsonIgnore]
        string FileName { get; }

        void LoadJson(string json);

        string GetJson();
    }
}