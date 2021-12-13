// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace Firefly.Client.Control.Fire
{
    public class TabStatusModel
    {
        public TabStatusModel()
        {
            IsChanged = false;
            TabName = string.Empty;
            ChangedList = new List<TabChangedItemModel>();
        }

        public bool IsChanged { get; set; }

        public string TabName { get; set; }

        public List<TabChangedItemModel> ChangedList { get; set; }

        public override string ToString()
        {
            string result = TabName + "\r\n";
            foreach (TabChangedItemModel item in ChangedList)
            {
                result += "\t" + item.ToString() + "\r\n";
            }

            return result;
        }
    }

    public class TabChangedItemModel
    {
        public string Name { get; set; }

        public string OldValue { get; set; }

        public string NewValue { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
