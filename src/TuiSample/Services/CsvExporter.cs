using System.Globalization;
using System.Text;
using TuiSample.Models;

namespace TuiSample.Services;

public sealed class CsvExporter : ICsvExporter
{
    public void ExportToFile(IEnumerable<User> users, string filePath)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var csvContent = BuildCsvContent(users);
        var utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        File.WriteAllText(filePath, csvContent, utf8WithBom);
    }

    public string BuildCsvContent(IEnumerable<User> users)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ID,氏名,メールアドレス,部署,ロール,ステータス,登録日");

        foreach (var user in users)
        {
            sb.Append(EscapeCsvField(user.Id.ToString())).Append(',')
              .Append(EscapeCsvField(user.Name)).Append(',')
              .Append(EscapeCsvField(user.Email)).Append(',')
              .Append(EscapeCsvField(user.Department)).Append(',')
              .Append(EscapeCsvField(user.Role.ToString())).Append(',')
              .Append(EscapeCsvField(user.Status.ToString())).Append(',')
              .Append(EscapeCsvField(user.CreatedAt.ToString("yyyy-MM-dd")))
              .AppendLine();
        }

        return sb.ToString();
    }

    public IReadOnlyList<User> ImportFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("指定されたCSVファイルが見つかりません。", filePath);
        }

        var content = File.ReadAllText(filePath, Encoding.UTF8);
        return ParseCsvContent(content);
    }

    public IReadOnlyList<User> ParseCsvContent(string csvContent)
    {
        var result = new List<User>();
        if (string.IsNullOrWhiteSpace(csvContent))
        {
            return result;
        }

        using var reader = new StringReader(csvContent);
        string? line;
        var isFirstLine = true;

        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = ParseCsvLine(line);
            if (isFirstLine)
            {
                isFirstLine = false;
                if (fields.Count > 0 && (fields[0].Equals("ID", StringComparison.OrdinalIgnoreCase) || fields[0].Contains("氏名")))
                {
                    continue;
                }
            }

            if (fields.Count < 3)
            {
                continue;
            }

            _ = int.TryParse(fields[0], out var id);
            var name = fields.ElementAtOrDefault(1)?.Trim() ?? string.Empty;
            var email = fields.ElementAtOrDefault(2)?.Trim() ?? string.Empty;
            var dept = fields.ElementAtOrDefault(3)?.Trim() ?? "未所属";
            var roleStr = fields.ElementAtOrDefault(4)?.Trim() ?? nameof(UserRole.Viewer);
            var statusStr = fields.ElementAtOrDefault(5)?.Trim() ?? nameof(UserStatus.Active);
            var dateStr = fields.ElementAtOrDefault(6)?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
            {
                continue;
            }

            if (!Enum.TryParse<UserRole>(roleStr, ignoreCase: true, out var role))
            {
                role = UserRole.Viewer;
            }

            if (!Enum.TryParse<UserStatus>(statusStr, ignoreCase: true, out var status))
            {
                status = UserStatus.Active;
            }

            if (!DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var createdAt))
            {
                createdAt = DateTime.Today;
            }

            result.Add(new User
            {
                Id = id,
                Name = name,
                Email = email,
                Department = string.IsNullOrWhiteSpace(dept) ? "未所属" : dept,
                Role = role,
                Status = status,
                CreatedAt = createdAt
            });
        }

        return result;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
        }

        fields.Add(current.ToString());
        return fields;
    }

    private static string EscapeCsvField(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
