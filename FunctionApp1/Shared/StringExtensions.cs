using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReimbursementReporting.Shared;
using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReimbursementReporting.Utilities.Constants
{
    public static partial class StringExtensions
    {
        public static bool ContainsAny(this string source, params string[] values)
        {
            return values.Any(value => source.Contains(value, StringComparison.OrdinalIgnoreCase));
        }

        public static string FormatForCode(this DataRow dataRow, string colName)
        {
            ArgumentNullException.ThrowIfNull(dataRow);

            string value = dataRow[colName]?.ToString()?.Trim();
            return value.IsNA() ? null : value;
        }

        public static DateTime? ParseAndConvertDate(this DataRow dataRow, string columnName)
        {
            string utcDateTimeString = dataRow[columnName].ToString();

            if (DateTime.TryParse(utcDateTimeString, out DateTime utcDateTime))
            {
                // Get the Eastern Standard Time (EST) time zone
                TimeZoneInfo estTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

                // Convert the UTC DateTime to EST
                DateTime estDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, estTimeZone);

                return estDateTime;
            }
            else
            {
                // Parsing failed, return DateTime.MinValue to indicate failure
                return null;
            }
        }

        public static decimal? ParseAmount(this DataRow dataRow, string columnName)
        {
            if (dataRow == null || string.IsNullOrEmpty(columnName) || !dataRow.Table.Columns.Contains(columnName))
            {
                return null;
            }

            string valueAsString = dataRow[columnName]?.ToString();
            return string.IsNullOrWhiteSpace(valueAsString) ? null : decimal.TryParse(valueAsString, out decimal result) ? result : null;
        }

        public static bool IsNA(this string value)
        {
            return string.IsNullOrWhiteSpace(value) || value.Equals("NULL", StringComparison.OrdinalIgnoreCase);
        }

        public static void FormatToExcel(this DataRow dataRow, string columnName, string value)
        {
            dataRow[columnName] = columnName switch
            {
                ColumnNames.CreateDate or ColumnNames.TransactionDate or ColumnNames.ProcessedDate
                    => string.IsNullOrWhiteSpace(value) ? "NULL"
                        : DateTime.TryParse(value, out DateTime date)
                    ? date.ToShortDateString()
                        : value,
                ColumnNames.RequestedTotalReimbursementAmount or ColumnNames.ApprovedTotalReimbursementAmount
                    => string.IsNullOrWhiteSpace(value) ? "NULL"
                        : decimal.TryParse(value, out decimal amount)
                    ? amount.ToString("C2")
                        : value,
                _ => string.IsNullOrWhiteSpace(value) ? "NULL" : value,
            };
        }

        public static string ToPascalCase(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            string[] words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string pascalCase = string.Concat(words.Select(word => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(word.ToLower())));
            return new string(pascalCase.Where(char.IsLetterOrDigit).ToArray());
        }

        public static string ToEasternStandardDateString(this string datetimeString)
        {
            if (string.IsNullOrWhiteSpace(datetimeString))
            {
                return "NULL";
            }

            if (!DateTime.TryParse(datetimeString, out DateTime date))
            {
                return "Invalid Date";
            }

            DateTime estDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(date, TimeZoneInfo.Utc.Id, "Eastern Standard Time");
            return estDate.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture);
        }

        public static string GetJsonValue(this string jsonString, string propertyName)
        {
            try
            {
                JObject jsonObject = JObject.Parse(jsonString);
                return jsonObject[propertyName]?.ToString();
            }
            catch (JsonReaderException)
            {
                return null;
            }
        }

        public static bool IsValidJson(this string jsonString)
        {
            try
            {
                _ = JToken.Parse(jsonString);
                return true;
            }
            catch (JsonReaderException)
            {
                return false;
            }
        }

        public static string StripNumbers(this string input)
        {
            return StripNumbersRegex().Replace(input, "");
        }

        public static bool ContainsNumbers(this string input)
        {
            return !string.IsNullOrEmpty(input) && input.All(char.IsDigit);
        }

        [GeneratedRegex(@"\d")]
        private static partial Regex StripNumbersRegex();
    }
}
