// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Linq;
using System.Collections.Generic;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Header;

namespace Firefly.Client
{
    public static class TreeViewHelper
    {
        public static void AddRange(this TreeItem rootItem, List<TreeItem> allItems)
        {
            Queue<TreeItem> queue = new Queue<TreeItem>();
            queue.Enqueue(rootItem);
            while (queue.Count > 0)
            {
                TreeItem curItem = queue.Dequeue();
                List<TreeItem> childItem = allItems.Where(x =>
                {
                    if (x.ParentID == curItem.ID)
                    {
                        if (x.ItemType == ItemType.Device && curItem.ItemType == ItemType.Channel)
                        {
                            return false;
                        }
                        return true;
                    }
                    return false;
                }).ToList();
                foreach (TreeItem item in childItem)
                {
                    item.Parent = curItem;
                    curItem.Children.Add(item);
                    queue.Enqueue(item);
                }
            }
        }

        public static void Append(this TreeItem rootTree, TreeItem childTree, List<TreeItem> allItems)
        {
            if (childTree == null)
            {
                return;
            }
            rootTree.Children.Add(childTree);
            Queue<TreeItem> queue = new Queue<TreeItem>();
            queue.Enqueue(childTree);
            while (queue.Count > 0)
            {
                TreeItem curItem = queue.Dequeue();
                List<TreeItem> childItem = allItems.Where(x =>
                {
                    if (x.ParentID == curItem.ID)
                    {
                        if (x.ItemType == ItemType.Device && curItem.ItemType == ItemType.Channel)
                        {
                            return false;
                        }
                        return true;
                    }
                    return false;
                }).ToList();
                foreach (TreeItem item in childItem)
                {
                    item.Parent = curItem;
                    curItem.Children.Add(item);
                    queue.Enqueue(item);
                }
            }
        }

        public static List<TreeItem> GetSelectedItems(this TreeItem rootItem)
        {
            List<TreeItem> itemList = new List<TreeItem>();
            Queue<TreeItem> queue = new Queue<TreeItem>();
            queue.Enqueue(rootItem);
            while (queue.Count > 0)
            {
                TreeItem curItem = queue.Dequeue();
                if (curItem.IsChecked == true)
                {
                    itemList.Add(curItem);
                }

                foreach (TreeItem child in curItem.Children)
                {
                    queue.Enqueue(child);
                }
            }
            itemList.Remove(rootItem);

            return itemList;
        }

        public static List<T> GetSelectedItems<T>(this TreeItem treeItem)
        {
            List<TreeItem> itemList = new List<TreeItem>();
            Queue<TreeItem> queue = new Queue<TreeItem>();
            queue.Enqueue(treeItem);
            while (queue.Count > 0)
            {
                TreeItem curItem = queue.Dequeue();
                if (curItem.IsChecked == true)
                {
                    itemList.Add(curItem);
                }

                foreach (TreeItem child in curItem.Children)
                {
                    queue.Enqueue(child);
                }
            }

            return itemList.Where(x => x.DataObject.GetType() == typeof(T)).Select(x => (T)x.DataObject).ToList();
        }

        public static List<TreeItem> GetItems(this TreeItem treeItem, Func<TreeItem, bool> func)
        {
            List<TreeItem> treeItems = new List<TreeItem>();
            Queue<TreeItem> queue = new Queue<TreeItem>();
            queue.Enqueue(treeItem);
            while (queue.Count > 0)
            {
                TreeItem curItem = queue.Dequeue();
                if (func(curItem))
                {
                    treeItems.Add(curItem);
                }

                foreach (TreeItem child in curItem.Children)
                {
                    queue.Enqueue(child);
                }
            }

            return treeItems;
        }

        public static void Expland(this TreeItem treeItem)
        {
            Queue<TreeItem> queue = new Queue<TreeItem>();
            queue.Enqueue(treeItem);
            while (queue.Count > 0)
            {
                TreeItem curItem = queue.Dequeue();
                if(treeItem != curItem)
                {
                    curItem.IsExpanded = false;
                }
                foreach (TreeItem child in curItem.Children)
                {
                    queue.Enqueue(child);
                }
            }
        }
    }
}