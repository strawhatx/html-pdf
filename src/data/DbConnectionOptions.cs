using Microsoft.Extensions.Configuration;

namespace data;

public sealed class DbConnectionOptions
{
    public required string Reader { get; init; }
    public required string Writer { get; init; }
    public required string Port { get; init; }
    public required string Database { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }

    public static DbConnectionOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection("DbConnection");

        return new DbConnectionOptions
        {
            Reader = FirstNonEmpty(
                Environment.GetEnvironmentVariable("HTMLPDF_DB_READER_HOST"),
                Environment.GetEnvironmentVariable("DB_READER_HOST"),
                section["Reader"],
                Environment.GetEnvironmentVariable("HTMLPDF_DB_HOST"),
                Environment.GetEnvironmentVariable("DB_HOST"),
                "localhost"),
            Writer = FirstNonEmpty(
                Environment.GetEnvironmentVariable("HTMLPDF_DB_WRITER_HOST"),
                Environment.GetEnvironmentVariable("DB_WRITER_HOST"),
                Environment.GetEnvironmentVariable("HTMLPDF_DB_HOST"),
                Environment.GetEnvironmentVariable("DB_HOST"),
                section["Writer"],
                "localhost"),
            Port = FirstNonEmpty(
                Environment.GetEnvironmentVariable("HTMLPDF_DB_PORT"),
                Environment.GetEnvironmentVariable("DB_PORT"),
                section["Port"],
                "5432"),
            Database = FirstNonEmpty(
                Environment.GetEnvironmentVariable("HTMLPDF_DB_NAME"),
                Environment.GetEnvironmentVariable("DB_NAME"),
                section["Database"],
                "postgres"),
            Username = FirstNonEmpty(
                Environment.GetEnvironmentVariable("HTMLPDF_DB_USERNAME"),
                Environment.GetEnvironmentVariable("DB_USERNAME"),
                section["Username"],
                "postgres"),
            Password = FirstNonEmpty(
                Environment.GetEnvironmentVariable("HTMLPDF_DB_IAM_TOKEN"),
                Environment.GetEnvironmentVariable("DB_IAM_TOKEN"),
                Environment.GetEnvironmentVariable("HTMLPDF_DB_PASSWORD"),
                Environment.GetEnvironmentVariable("DB_PASSWORD"),
                section["IamToken"],
                section["Password"],
                "postgres")
        };
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        throw new InvalidOperationException("A database connection value was empty.");
    }
}