using System.Text.Json;
using BuildingMaterialsAuditAgent.Models;
using Microsoft.Data.Sqlite;

namespace BuildingMaterialsAuditAgent.Data;

public sealed class AuditHistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly string _connectionString;

    public AuditHistoryStore(IWebHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);

        var databasePath = Path.Combine(dataDirectory, "material-audit.db");
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath
        }.ToString();

        EnsureCreated();
    }

    public async Task SaveAsync(MaterialAuditReport report, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT OR REPLACE INTO audit_reports
            (
                report_id,
                created_at,
                project_name,
                material_name,
                category,
                supplier,
                conclusion,
                risk_level,
                risk_score,
                total_amount,
                report_json
            )
            VALUES
            (
                $reportId,
                $createdAt,
                $projectName,
                $materialName,
                $category,
                $supplier,
                $conclusion,
                $riskLevel,
                $riskScore,
                $totalAmount,
                $reportJson
            );
            """;

        command.Parameters.AddWithValue("$reportId", report.ReportId);
        command.Parameters.AddWithValue("$createdAt", report.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$projectName", report.Profile.ProjectName);
        command.Parameters.AddWithValue("$materialName", report.Profile.MaterialName);
        command.Parameters.AddWithValue("$category", report.Profile.Category);
        command.Parameters.AddWithValue("$supplier", report.Profile.Supplier);
        command.Parameters.AddWithValue("$conclusion", report.Conclusion);
        command.Parameters.AddWithValue("$riskLevel", report.RiskLevel);
        command.Parameters.AddWithValue("$riskScore", report.RiskScore);
        command.Parameters.AddWithValue("$totalAmount", report.Cost.TotalAmount);
        command.Parameters.AddWithValue("$reportJson", JsonSerializer.Serialize(report, JsonOptions));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditHistoryItem>> ListAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 200);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                report_id,
                created_at,
                project_name,
                material_name,
                category,
                supplier,
                conclusion,
                risk_level,
                risk_score,
                total_amount
            FROM audit_reports
            ORDER BY created_at DESC
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$limit", limit);

        var items = new List<AuditHistoryItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new AuditHistoryItem
            {
                ReportId = reader.GetString(0),
                CreatedAt = DateTimeOffset.Parse(reader.GetString(1)),
                ProjectName = reader.GetString(2),
                MaterialName = reader.GetString(3),
                Category = reader.GetString(4),
                Supplier = reader.GetString(5),
                Conclusion = reader.GetString(6),
                RiskLevel = reader.GetString(7),
                RiskScore = reader.GetInt32(8),
                TotalAmount = reader.GetDecimal(9)
            });
        }

        return items;
    }

    public async Task<MaterialAuditReport?> GetAsync(string reportId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT report_json
            FROM audit_reports
            WHERE report_id = $reportId
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$reportId", reportId);

        var json = await command.ExecuteScalarAsync(cancellationToken) as string;
        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<MaterialAuditReport>(json, JsonOptions);
    }

    private void EnsureCreated()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS audit_reports
            (
                report_id TEXT PRIMARY KEY,
                created_at TEXT NOT NULL,
                project_name TEXT NOT NULL,
                material_name TEXT NOT NULL,
                category TEXT NOT NULL,
                supplier TEXT NOT NULL,
                conclusion TEXT NOT NULL,
                risk_level TEXT NOT NULL,
                risk_score INTEGER NOT NULL,
                total_amount REAL NOT NULL,
                report_json TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_audit_reports_created_at
                ON audit_reports(created_at DESC);

            CREATE INDEX IF NOT EXISTS ix_audit_reports_project_name
                ON audit_reports(project_name);
            """;

        command.ExecuteNonQuery();
    }
}
