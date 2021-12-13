// Copyright (c) IRtek.Co.Ltd.
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Windows.Data;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Firefly.Client.Control
{
    public class ValidationConverter : IValueConverter
    {
        #region Methods

        #region 方案A，强制限制输入的方案，强制约定最大值和最小值

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null)
            {
                return value;
            }

            if (parameter is ValidationRang rang)
            {
                if (IsMatch(value, targetType, rang) && (!rang.ConvertBackValue?.ToString().EndsWith(".") ?? true))
                {
                    if (rang.TargetType != typeof(Single) || rang.ConvertBackValue == null)
                    {
                        rang.ConvertBackValue = value;
                    }
                    else if (Math.Abs(System.Convert.ToSingle(rang.ConvertBackValue) - System.Convert.ToSingle(value)) > 0)
                    {
                        rang.ConvertBackValue = value;
                    }
                    else if (rang.ConvertBackValue.ToString().StartsWith("0") && System.Convert.ToSingle(rang.ConvertBackValue) > 1)
                    {
                        rang.ConvertBackValue = value;
                    }
                }

                return rang.ConvertBackValue;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null)
            {
                return value;
            }

            if (value == null)
            {
                return null;
            }

            if (parameter is ValidationRang rang)
            {
                if (string.IsNullOrWhiteSpace(value?.ToString()))
                {
                    rang.ConvertBackValue = rang.DefaultValue ?? rang.Minimum;
                    return rang.ConvertBackValue;
                }

                if (!IsMatch(value, targetType, rang))
                {
                    return rang.ConvertBackValue;
                }

                if (value.ToString().StartsWith("."))
                {
                    rang.ConvertBackValue = $"0{value}";
                    return rang.ConvertBackValue;
                }

                if (value.ToString().EndsWith("."))
                {
                    rang.ConvertBackValue = value;
                    return rang.ConvertBackValue;
                }

                rang.ConvertBackValue = GetMatchRangValue(value, rang);

                return rang.ConvertBackValue;
            }

            return value;
        }

        public bool IsMatch(object value, Type targetType, ValidationRang rang)
        {
            if (targetType == typeof(float) || targetType == typeof(Single) || targetType == typeof(double))
            {
                Regex regex = new Regex(GetPositiveNumberPatternWithDecimal(rang.MaxDecimalLength));
                if (System.Convert.ToDouble(rang.Minimum.ToString()) < 0)
                {
                    regex = new Regex(GetNumberPatternWithDecimal(rang.MaxDecimalLength));
                }

                if (value.ToString().StartsWith("."))
                {
                    value = $"0{value.ToString()}";
                }

                return regex.IsMatch(value.ToString());
            }

            if (targetType == typeof(int))
            {
                Regex regex = new Regex(GetPositiveNumberPattern());
                if (System.Convert.ToDouble(rang.Minimum.ToString()) < 0)
                {
                    regex = new Regex(GetNumberPattern());
                }

                return regex.IsMatch(value.ToString());
            }

            return true;
        }

        public object GetMatchRangValue(object value, ValidationRang rang)
        {
            if (rang.TargetType == typeof(int))
            {
                if (System.Convert.ToInt32(value) < System.Convert.ToInt32(rang.Minimum))
                {
                    return rang.DefaultValue ?? rang.Minimum; ;
                }

                if (System.Convert.ToInt32(value) > System.Convert.ToInt32(rang.Maximum))
                {
                    return rang.Maximum;
                }
            }

            if (rang.TargetType == typeof(Single))
            {
                if (System.Convert.ToSingle(value) < System.Convert.ToSingle(rang.Minimum))
                {
                    return rang.DefaultValue ?? rang.Minimum; ;
                }

                if (System.Convert.ToSingle(value) > System.Convert.ToSingle(rang.Maximum))
                {
                    return rang.Maximum;
                }
            }

            if (rang.TargetType == typeof(double))
            {
                if (System.Convert.ToDouble(value) < System.Convert.ToDouble(rang.Minimum))
                {
                    return rang.DefaultValue ?? rang.Minimum; ;
                }

                if (System.Convert.ToDouble(value) > System.Convert.ToDouble(rang.Maximum))
                {
                    return rang.Maximum;
                }
            }

            return value;
        }

        #endregion

        #region 方案B，非强制限制输入的方案，会在输入位置右侧，有个取值范围的错误提示

        //
        // public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        // {
        //     if (IsMatch(value, targetType, parameter) && (!convertBackValue?.ToString().EndsWith(".") ?? true))
        //     {
        //         convertBackValue = value;
        //     }
        //
        //     return convertBackValue;
        // }
        //
        // public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        // {
        //     if (string.IsNullOrWhiteSpace(value?.ToString()))
        //     {
        //         convertBackValue = 0;
        //         return convertBackValue;
        //     }
        //
        //     if (!IsMatch(value, targetType, parameter))
        //     {
        //         return convertBackValue;
        //     }
        //
        //     convertBackValue = value;
        //
        //     return value;
        // }
        //
        //
        // private bool IsMatch(object value, Type targetType, object parameter)
        // {
        //     if (targetType == typeof(float) || targetType == typeof(double) || targetType == typeof(Single))
        //     {
        //         Regex regex = new Regex(GetPositiveNumberPatternWithDecimal(3));
        //         if (parameter != null && parameter is ValidationRang rang)
        //         {
        //             string pattern = "";
        //             if (rang.IsPositiveNumber && !rang.IsNegativeNumber)
        //             {
        //                 pattern = GetPositiveNumberPatternWithDecimal(rang.MaxDecimalLength);
        //             }
        //             else if (rang.IsPositiveNumber && rang.IsNegativeNumber)
        //             {
        //                 pattern = GetNumberPatternWithDecimal(rang.MaxDecimalLength);
        //             }
        //             else if (!rang.IsPositiveNumber && rang.IsNegativeNumber)
        //             {
        //                 pattern = GetNegativeNumberPatternWithDecimal(rang.MaxDecimalLength);
        //             }
        //
        //             if (!string.IsNullOrWhiteSpace(pattern))
        //             {
        //                 regex = new Regex(pattern);
        //             }
        //         }
        //
        //         return regex.IsMatch(value.ToString());
        //     }
        //
        //     if (targetType == typeof(int))
        //     {
        //         Regex regex = new Regex(GetPositiveNumberPattern());
        //         if (parameter != null && parameter is ValidationRang rang)
        //         {
        //             string pattern = "";
        //             if (rang.IsPositiveNumber && !rang.IsNegativeNumber)
        //             {
        //                 pattern = GetPositiveNumberPattern();
        //             }
        //             else if (rang.IsPositiveNumber && rang.IsNegativeNumber)
        //             {
        //                 pattern = GetNumberPattern();
        //             }
        //             else if (!rang.IsPositiveNumber && rang.IsNegativeNumber)
        //             {
        //                 pattern = GetNegativeNumberPattern();
        //             }
        //
        //             if (!string.IsNullOrWhiteSpace(pattern))
        //             {
        //                 regex = new Regex(pattern);
        //             }
        //         }
        //
        //         return regex.IsMatch(value.ToString());
        //     }
        //
        //     return true;
        // }

        #endregion


        /// <summary>
        /// 获取有符号实数，带有小数点
        /// </summary>
        /// <param name="maximumDecimalLength"></param>
        /// <returns></returns>
        private string GetNumberPatternWithDecimal(int maximumDecimalLength)
        {
            return @"^(\-|\+)?\d+(\.[0-9]{0," + maximumDecimalLength.ToString() + "})?$";
        }

        /// <summary>
        /// 获取正实数，带有小数点
        /// </summary>
        /// <param name="maximumDecimalLength"></param>
        /// <returns></returns>
        private string GetPositiveNumberPatternWithDecimal(int maximumDecimalLength)
        {
            return @"^[0-9]+(\.[0-9]{0," + maximumDecimalLength.ToString() + "})?$";
        }

        /// <summary>
        /// 获取负实数，带有小数点
        /// </summary>
        /// <param name="maximumDecimalLength"></param>
        /// <returns></returns>
        private string GetNegativeNumberPatternWithDecimal(int maximumDecimalLength)
        {
            return @"^\-\d+(\.[0-9]{0," + maximumDecimalLength.ToString() + "})?$";
        }

        /// <summary>
        /// 获取整数
        /// </summary>
        /// <returns></returns>
        private string GetNumberPattern()
        {
            return @"^(\-|\+)?\d*$";
        }

        /// <summary>
        /// 获取负整数或0
        /// </summary>
        /// <returns></returns>
        private string GetNegativeNumberPattern()
        {
            return @"^-[1-9]\d*|0";
        }

        /// <summary>
        /// 获取正整数或0
        /// </summary>
        /// <returns></returns>
        private string GetPositiveNumberPattern()
        {
            return @"^\d+$";
        }

        #endregion
    }
}