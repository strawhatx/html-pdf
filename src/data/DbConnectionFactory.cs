using System.Data;
using Npgsql;

namespace data;

public interface IDbConnectionFactory
{
    IDbConnection CreateReadConnection();
    IDbConnection CreateWriteConnection();
}

public sealed class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _readerConnectionString;
    private readonly string _writerConnectionString;

    public DbConnectionFactory() : this(null)
    {
    }

    public DbConnectionFactory(string? reader = null, string? writer = null, string? port = null, string? database = null, string? username = null, string? password = null)
    {
        _readerConnectionString = BuildConnectionString(reader, port, database, username, password);
        _writerConnectionString = BuildConnectionString(writer ?? reader, port, database, username, password);
    }

    public static string BuildConnectionString(string? host = null, string? port = null, string? database = null, string? username = null, string? password = null)
    {
        var resolvedHost = FirstNonEmpty(host,
            Environment.GetEnvironmentVariable("HTMLPDF_DB_HOST"),
            Environment.GetEnvironmentVariable("DB_HOST"),
            "localhost");

        if (string.IsNullOrWhiteSpace(resolvedHost))
        {
            resolvedHost = "localhost";
        }

        var resolvedPort = FirstNonEmpty(port,
            Environment.GetEnvironmentVariable("HTMLPDF_DB_PORT"),
            Environment.GetEnvironmentVariable("DB_PORT"),
            "5432");

        if (string.IsNullOrWhiteSpace(resolvedPort))
        {
            resolvedPort = "5432";
        }

        var resolvedDatabase = FirstNonEmpty(database,
            Environment.GetEnvironmentVariable("HTMLPDF_DB_NAME"),
            Environment.GetEnvironmentVariable("DB_NAME"),
            "postgres");

        if (string.IsNullOrWhiteSpace(resolvedDatabase))
        {
            resolvedDatabase = "postgres";
        }

        var resolvedUsername = FirstNonEmpty(username,
            Environment.GetEnvironmentVariable("HTMLPDF_DB_USERNAME"),
            Environment.GetEnvironmentVariable("DB_USERNAME"),
            "postgres");

        if (string.IsNullOrWhiteSpace(resolvedUsername))
        {
            resolvedUsername = "postgres";
        }

        var resolvedPassword = FirstNonEmpty(password,
            Environment.GetEnvironmentVariable("HTMLPDF_DB_IAM_TOKEN"),
            Environment.GetEnvironmentVariable("DB_IAM_TOKEN"),
            Environment.GetEnvironmentVariable("HTMLPDF_DB_PASSWORD"),
            Environment.GetEnvironmentVariable("DB_PASSWORD"),
            "postgres");

        if (string.IsNullOrWhiteSpace(resolvedPassword))
        {
            resolvedPassword = "postgres";
        }

        var resolvedPortNumber = int.TryParse(resolvedPort, out var parsedPort) ? parsedPort : 5432;

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = resolvedHost,
            Port = resolvedPortNumber,
            Database = resolvedDatabase,
            Username = resolvedUsername,
            Password = resolvedPassword,
            SslMode = resolvedHost.Contains("rds.amazonaws.com", StringComparison.OrdinalIgnoreCase) || resolvedHost.Contains(".cluster-", StringComparison.OrdinalIgnoreCase)
                ? SslMode.Require
                : SslMode.Disable,
            Timeout = 15,
            CommandTimeout = 30
        };

        return builder.ConnectionString;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    public IDbConnection CreateReadConnection()
    {
        return new NpgsqlConnection(_readerConnectionString);
    }

    public IDbConnection CreateWriteConnection()
    {
        return new NpgsqlConnection(_writerConnectionString);
    }
}

public abstract class RepositoryBase
{
    private readonly IDbConnectionFactory _connectionFactory;

    protected RepositoryBase(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    protected IDbConnection OpenReadConnection()
    {
        var connection = _connectionFactory.CreateReadConnection();
        connection.Open();
        return connection;
    }

    protected IDbConnection OpenWriteConnection()
    {
        var connection = _connectionFactory.CreateWriteConnection();
        connection.Open();
        return connection;
    }
}
