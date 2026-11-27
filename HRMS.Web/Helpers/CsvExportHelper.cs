using System.Text;

namespace HRMS.Web.Helpers;

public static class CsvExportHelper
{
    public static byte[] BuildCsv<T>(IEnumerable<T> rows, (string Header, Func<T, object?> Selector)[] columns)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", columns.Select(c => EscapeCsv(c.Header))));

        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",", columns.Select(c => EscapeCsv(c.Selector(row)?.ToString() ?? string.Empty))));
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        // Prevent spreadsheet formula injection
        if (value[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
        {
            value = "'" + value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
