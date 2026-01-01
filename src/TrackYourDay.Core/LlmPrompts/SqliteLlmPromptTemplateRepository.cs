// src/TrackYourDay.Core/LlmPrompts/SqliteLlmPromptTemplateRepository.cs
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace TrackYourDay.Core.LlmPrompts;

/// <summary>
/// SQLite implementation of template repository.
/// </summary>
public class SqliteLlmPromptTemplateRepository : ILlmPromptTemplateRepository
{
    private readonly string _databaseFileName = InitializeDatabase();
    private readonly ILogger<SqliteLlmPromptTemplateRepository> _logger;

    private static string InitializeDatabase()
    {
        var appDataPath = Environment.ExpandEnvironmentVariables("%AppData%\\TrackYourDay");
        if (!Directory.Exists(appDataPath))
        {
            Directory.CreateDirectory(appDataPath);
        }
        return $"{appDataPath}\\TrackYourDayGeneric.db";
    }

    public SqliteLlmPromptTemplateRepository(ILogger<SqliteLlmPromptTemplateRepository> logger)
    {
        _logger = logger;
        
        try
        {
            InitializeStructure();
            SeedDefaultTemplatesIfEmpty();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize LLM prompt templates database");
        }
    }

    public IReadOnlyList<LlmPromptTemplate> GetActiveTemplates()
    {
        using var connection = new SqliteConnection($"Data Source={_databaseFileName}");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, TemplateKey, Name, SystemPrompt, IsActive, DisplayOrder, CreatedAt, UpdatedAt
            FROM llm_prompt_templates
            WHERE IsActive = 1
            ORDER BY DisplayOrder";

        return ReadTemplates(command);
    }

    public IReadOnlyList<LlmPromptTemplate> GetAllTemplates()
    {
        using var connection = new SqliteConnection($"Data Source={_databaseFileName}");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, TemplateKey, Name, SystemPrompt, IsActive, DisplayOrder, CreatedAt, UpdatedAt
            FROM llm_prompt_templates
            ORDER BY DisplayOrder";

        return ReadTemplates(command);
    }

    public LlmPromptTemplate? GetByKey(string templateKey)
    {
        using var connection = new SqliteConnection($"Data Source={_databaseFileName}");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT Id, TemplateKey, Name, SystemPrompt, IsActive, DisplayOrder, CreatedAt, UpdatedAt
            FROM llm_prompt_templates
            WHERE TemplateKey = @key";
        command.Parameters.AddWithValue("@key", templateKey);

        var templates = ReadTemplates(command);
        return templates.Count > 0 ? templates[0] : null;
    }

    public void Save(LlmPromptTemplate template)
    {
        using var connection = new SqliteConnection($"Data Source={_databaseFileName}");
        connection.Open();

        var command = connection.CreateCommand();
        if (template.Id == 0)
        {
            command.CommandText = @"
                INSERT INTO llm_prompt_templates 
                    (TemplateKey, Name, SystemPrompt, IsActive, DisplayOrder, CreatedAt, UpdatedAt)
                VALUES 
                    (@key, @name, @prompt, @active, @order, @created, @updated)";
        }
        else
        {
            command.CommandText = @"
                UPDATE llm_prompt_templates
                SET Name = @name,
                    SystemPrompt = @prompt,
                    IsActive = @active,
                    DisplayOrder = @order,
                    UpdatedAt = @updated
                WHERE Id = @id";
            command.Parameters.AddWithValue("@id", template.Id);
        }

        command.Parameters.AddWithValue("@key", template.TemplateKey);
        command.Parameters.AddWithValue("@name", template.Name);
        command.Parameters.AddWithValue("@prompt", template.SystemPrompt);
        command.Parameters.AddWithValue("@active", template.IsActive ? 1 : 0);
        command.Parameters.AddWithValue("@order", template.DisplayOrder);
        command.Parameters.AddWithValue("@created", template.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("@updated", template.UpdatedAt.ToString("O"));

        command.ExecuteNonQuery();
    }

    public void Delete(string templateKey)
    {
        using var connection = new SqliteConnection($"Data Source={_databaseFileName}");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE llm_prompt_templates
            SET IsActive = 0,
                UpdatedAt = @updated
            WHERE TemplateKey = @key";
        command.Parameters.AddWithValue("@key", templateKey);
        command.Parameters.AddWithValue("@updated", DateTime.UtcNow.ToString("O"));

        command.ExecuteNonQuery();
    }

    public void Restore(string templateKey)
    {
        using var connection = new SqliteConnection($"Data Source={_databaseFileName}");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE llm_prompt_templates
            SET IsActive = 1,
                UpdatedAt = @updated
            WHERE TemplateKey = @key";
        command.Parameters.AddWithValue("@key", templateKey);
        command.Parameters.AddWithValue("@updated", DateTime.UtcNow.ToString("O"));

        command.ExecuteNonQuery();
    }

    public int GetActiveTemplateCount()
    {
        using var connection = new SqliteConnection($"Data Source={_databaseFileName}");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM llm_prompt_templates WHERE IsActive = 1";
        
        var result = command.ExecuteScalar();
        return result != null ? Convert.ToInt32(result) : 0;
    }

    public bool TemplateKeyExists(string templateKey)
    {
        using var connection = new SqliteConnection($"Data Source={_databaseFileName}");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM llm_prompt_templates WHERE TemplateKey = @key";
        command.Parameters.AddWithValue("@key", templateKey);
        
        var result = command.ExecuteScalar();
        return result != null && Convert.ToInt32(result) > 0;
    }

    public int GetMaxDisplayOrder()
    {
        using var connection = new SqliteConnection($"Data Source={_databaseFileName}");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(MAX(DisplayOrder), 0) FROM llm_prompt_templates";
        
        var result = command.ExecuteScalar();
        return result != null ? Convert.ToInt32(result) : 0;
    }

    public void BulkUpdateDisplayOrder(Dictionary<string, int> keyToOrder)
    {
        using var connection = new SqliteConnection($"Data Source={_databaseFileName}");
        connection.Open();

        using var transaction = connection.BeginTransaction();
        try
        {
            foreach (var kvp in keyToOrder)
            {
                var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
                    UPDATE llm_prompt_templates
                    SET DisplayOrder = @order,
                        UpdatedAt = @updated
                    WHERE TemplateKey = @key";
                command.Parameters.AddWithValue("@key", kvp.Key);
                command.Parameters.AddWithValue("@order", kvp.Value);
                command.Parameters.AddWithValue("@updated", DateTime.UtcNow.ToString("O"));
                command.ExecuteNonQuery();
            }
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private void InitializeStructure()
    {
        using var connection = new SqliteConnection($"Data Source={_databaseFileName}");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS llm_prompt_templates (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TemplateKey TEXT NOT NULL UNIQUE,
                Name TEXT NOT NULL,
                SystemPrompt TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                DisplayOrder INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_llm_templates_active 
                ON llm_prompt_templates(IsActive, DisplayOrder);
            
            CREATE UNIQUE INDEX IF NOT EXISTS idx_llm_templates_key 
                ON llm_prompt_templates(TemplateKey);";
        
        command.ExecuteNonQuery();
    }

    private void SeedDefaultTemplatesIfEmpty()
    {
        var count = GetActiveTemplateCount();
        if (count > 0) return;

        var now = DateTime.UtcNow;
        var templates = new[]
        {
            new LlmPromptTemplate
            {
                Id = 0,
                TemplateKey = "detailed",
                Name = "Detailed Time Breakdown",
                SystemPrompt = @"You are a time tracking assistant. Analyze the following activity log and generate a detailed time log report.

REQUIREMENTS:
- Generate between 3 and 9 time log entries
- Group similar activities together
- Identify Jira ticket keys using pattern: [A-Z][A-Z0-9]+-\d+
- If no Jira key found, use ""N/A"" for Jira Key field
- Sum durations for grouped activities
- Each entry must include: Description, Duration (decimal hours), Jira Key
- Note: Durations already exclude break periods

ACTIVITY DATA:
{ACTIVITY_DATA_PLACEHOLDER}

OUTPUT FORMAT:
| Description | Duration (hours) | Jira Key |
|-------------|------------------|----------|
| ... | ... | ... |

Generate the report now.",
                IsActive = true,
                DisplayOrder = 1,
                CreatedAt = now,
                UpdatedAt = now
            },
            new LlmPromptTemplate
            {
                Id = 0,
                TemplateKey = "concise",
                Name = "Concise Summary",
                SystemPrompt = @"Summarize the following workday into 3-9 time log entries suitable for Jira worklog submission.

Rules:
1. Merge similar activities
2. Extract Jira keys (format: ABC-123) from descriptions
3. If no key found, write ""No ticket"" in Jira Key column
4. Convert durations to decimal hours (e.g., 1h 30m = 1.5)
5. Break time already excluded from durations

Data:
{ACTIVITY_DATA_PLACEHOLDER}

Output as table with columns: Task, Hours, Jira Ticket",
                IsActive = true,
                DisplayOrder = 2,
                CreatedAt = now,
                UpdatedAt = now
            },
            new LlmPromptTemplate
            {
                Id = 0,
                TemplateKey = "task-oriented",
                Name = "Task-Oriented Log",
                SystemPrompt = @"Act as a project manager reviewing an engineer's workday. Group the activities below into distinct tasks (minimum 3, maximum 9).

For each task:
- Write a clear description
- Sum total time spent (already excludes breaks)
- Identify associated Jira ticket (if mentioned in activity names)
- If no ticket, indicate ""Administrative"" or ""Untracked""

Activities:
{ACTIVITY_DATA_PLACEHOLDER}

Format output as:
1. [Jira Key or ""N/A""] - Description (X.X hours)
2. ...",
                IsActive = true,
                DisplayOrder = 3,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        foreach (var template in templates)
        {
            Save(template);
        }

        _logger.LogInformation("Seeded {Count} default LLM prompt templates", templates.Length);
    }

    private static IReadOnlyList<LlmPromptTemplate> ReadTemplates(SqliteCommand command)
    {
        var templates = new List<LlmPromptTemplate>();
        
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            templates.Add(new LlmPromptTemplate
            {
                Id = reader.GetInt32(0),
                TemplateKey = reader.GetString(1),
                Name = reader.GetString(2),
                SystemPrompt = reader.GetString(3),
                IsActive = reader.GetInt32(4) == 1,
                DisplayOrder = reader.GetInt32(5),
                CreatedAt = DateTime.Parse(reader.GetString(6)),
                UpdatedAt = DateTime.Parse(reader.GetString(7))
            });
        }

        return templates;
    }
}
