using CardServicesProcessor.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CardServicesProcessor.Utilities.Constants
{
    public static partial class StringExtensions
    {
        [GeneratedRegex(@"\d")]
        private static partial Regex StripNumbersRegex();

        public static bool ContainsAny(this string source, params string[] values)
        {
            return values.Any(value => source.Contains(value, StringComparison.OrdinalIgnoreCase));
        }

        private static readonly char[] separators = [' ', '.', ',', ';', ':', '!', '?'];

        public static bool ContainsAnyFullWord(this string source, params string[] values)
        {
            // Split the source string into words
            string[] sourceWords = source.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            // Check if any of the words in source match any of the values
            return sourceWords.Any(sourceWord =>
                values.Any(value =>
                    string.Equals(sourceWord, value, StringComparison.OrdinalIgnoreCase)));
        }

        public static DateTime? ParseAndConvertDateTime(this string? utcDateTimeString, string columnName)
        {
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

        public static decimal? ParseAmount(this string amount, string columnName)
        {
            return string.IsNullOrEmpty(columnName)
                ? null
                : !amount.IsTruthy() ? null : decimal.TryParse(amount, out decimal result) ? result : null;
        }

        public static bool IsTruthy(this string? value)
        {
            return !string.IsNullOrWhiteSpace(value)
                && !value.Equals("NULL", StringComparison.OrdinalIgnoreCase);
        }

        public static string? GetDenialReason(this string? value)
        {
            return value.IsTruthy() ? value : null;
        }

        public static bool IsNA(this decimal? value)
        {
            // Check if the value is null
            if (!value.HasValue || value == 0 || value is null)
            {
                return true;
            }

            // Check if the value represents an invalid decimal value (e.g., NaN, Infinity)
            return false;
        }

        public static void FormatForExcel(this DataRow dataRow, string columnName, string? value)
        {
            dataRow[columnName] = columnName switch
            {
                ColumnNames.CreateDate or ColumnNames.TransactionDate or ColumnNames.ProcessedDate
                    => !value.IsTruthy() ? "NULL"
                        : DateTime.TryParse(value, out DateTime date)
                    ? date.ToShortDateString()
                        : value,
                ColumnNames.RequestedTotalReimbursementAmount or ColumnNames.ApprovedTotalReimbursementAmount
                    => !value.IsTruthy() || dataRow[ColumnNames.CaseTopic].ToString() != "Reimbursement" ? "NULL"
                        : decimal.TryParse(value, out decimal amount)
                    ? amount.ToString("C2")
                        : value,
                _ => !value.IsTruthy() ? "NULL" : value,
            };
        }

        public static string ToPascalCase(this string input)
        {
            if (!input.IsTruthy())
            {
                return input;
            }

            string[] words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string pascalCase = string.Concat(words.Select(word => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(word.ToLower())));
            return new string(pascalCase.Where(char.IsLetterOrDigit).ToArray());
        }

        public static string ToEasternStandardDateString(this string datetimeString)
        {
            if (!datetimeString.IsTruthy())
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

        public static bool IsValidJson(this string? jsonString)
        {
            if (jsonString is null)
            {
                return false;
            }

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

        public static bool PathExists(this string jsonString, string propertyPath)
        {
            try
            {
                JObject jsonObject = JObject.Parse(jsonString);
                string[] properties = propertyPath.Split('.'); // Split the property path

                JToken token = jsonObject;
                foreach (string property in properties)
                {
                    token = token[property]; // Navigate through the nested structure
                    if (token == null)
                    {
                        return false; // Return null if any property in the path is missing
                    }
                }
                return true;
            }
            catch (Exception)
            {
                // Handle JSON parsing errors
                return false;
            }
        }

        public static string? GetJsonValue(this string? jsonString, string propertyPath)
        {
            if (jsonString is null)
            {
                return null;
            }

            try
            {
                JObject jsonObject = JObject.Parse(jsonString);
                string[] properties = propertyPath.Split('.'); // Split the property path

                JToken token = jsonObject;
                foreach (string property in properties)
                {
                    token = token[property]; // Navigate through the nested structure
                    if (token == null)
                    {
                        return null; // Return null if any property in the path is missing
                    }
                }

                return token.ToString();
            }
            catch (JsonReaderException)
            {
                return null;
            }
        }


        public static string StripNumbers(this string? input)
        {
            return input.IsTruthy() ? StripNumbersRegex().Replace(input, "") : "";
        }

        public static bool ContainsNumbersOnly(this string? input)
        {
            return input.IsTruthy() && input.All(char.IsDigit);
        }
    }
}
