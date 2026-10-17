using TuiSample.Models;

namespace TuiSample.Services;

public interface ICsvExporter
{
    void ExportToFile(IEnumerable<User> users, string filePath);
    string BuildCsvContent(IEnumerable<User> users);
    IReadOnlyList<User> ImportFromFile(string filePath);
    IReadOnlyList<User> ParseCsvContent(string csvContent);
}
