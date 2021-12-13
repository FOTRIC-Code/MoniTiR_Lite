// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0
using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;

namespace Firefly.Client.Common
{
    public static class NameHelper
    {
        public static bool IsNameCompliance(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && Regex.IsMatch(name, Constants.NAME_REG) && name.Length <= Constants.MAX_NAME_LENGTH;
        }

        public static bool IsJsonCompliance(string json)
        {
            return Regex.IsMatch(json, Constants.JSON_REG);
        }

        public static bool IsJsonNameCompliance(string jsonName)
        {
            return !string.IsNullOrWhiteSpace(jsonName) && Regex.IsMatch(jsonName, Constants.JSONNAME_REG);
        }

        public static bool ValidateUserName(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
            {
                return false;
            }
            if (userName.Any((char c) => !Enumerable.Contains(Constants.ALLOWED_USERNAME_CHARACTERS, c)))
            {
                return false;
            }
            return true;
        }

        public static bool ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return false;
            }
            if (password.Length < Constants.MIN_PASSWORD_LENGTH)
            {
                return false;
            }
            if (password.Length > Constants.MAX_PASSWORD_LENGTH)
            {
                return false;
            }
            if (password.Any((char c) => !Enumerable.Contains(Constants.ALLOWED_USERNAME_CHARACTERS, c)))
            {
                return false;
            }
            return true;
        }
    }

    public class NameComparerClass : IComparer
    {
        // Calls CaseInsensitiveComparer.Compare with the parameters reversed.
        ///<summary>
        ///比较两个字符串，如果含用数字，则数字按数字的大小来比较。
        ///</summary>
        ///<param name="x"></param>
        ///<param name="y"></param>
        ///<returns></returns>
        int IComparer.Compare(Object x, Object y)
        {
            if (x == null || y == null)
                throw new ArgumentException("Parameters can't be null");
            string fileA = x as string;
            string fileB = y as string;
            char[] arr1 = fileA.ToCharArray();
            char[] arr2 = fileB.ToCharArray();
            int i = 0, j = 0;
            while (i < arr1.Length && j < arr2.Length)
            {
                if (char.IsDigit(arr1[i]) && char.IsDigit(arr2[j]))
                {
                    string s1 = "", s2 = "";
                    while (i < arr1.Length && char.IsDigit(arr1[i]))
                    {
                        s1 += arr1[i];
                        i++;
                    }
                    while (j < arr2.Length && char.IsDigit(arr2[j]))
                    {
                        s2 += arr2[j];
                        j++;
                    }
                    if (int.Parse(s1) > int.Parse(s2))
                    {
                        return 1;
                    }
                    if (int.Parse(s1) < int.Parse(s2))
                    {
                        return -1;
                    }
                }
                else
                {
                    if (arr1[i] > arr2[j])
                    {
                        return 1;
                    }
                    if (arr1[i] < arr2[j])
                    {
                        return -1;
                    }
                    i++;
                    j++;
                }
            }
            if (arr1.Length == arr2.Length)
            {
                return 0;
            }
            else
            {
                return arr1.Length > arr2.Length ? 1 : -1;
            }
        }
    }
}