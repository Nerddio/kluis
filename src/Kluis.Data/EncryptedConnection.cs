using Microsoft.Data.Sqlite;

namespace Kluis.Data;

/// <summary>
/// Opens SQLite connections encrypted by SQLite3 Multiple Ciphers.
/// </summary>
/// <remarks>
/// Cipher scheme: "sqlcipher" with legacy = 4, the SQLCipher 4 format. Each page is encrypted with
/// AES-256-CBC and carries an HMAC-SHA512, so tampering is detected (an authenticated scheme). The key
/// is derived from the passphrase with PBKDF2-HMAC-SHA512 at 256,000 iterations and a random per-file
/// salt. The scheme is set explicitly on every connection rather than relying on the library default.
/// </remarks>
public static class EncryptedConnection
{
    /// <summary>SQLite's result code for "file is not a database", which is what a wrong key produces.</summary>
    private const int SqliteNotADatabase = 26;

    public static SqliteConnection Create(string path, string passphrase)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        return Open(path, passphrase, SqliteOpenMode.ReadWriteCreate)
            ?? throw new InvalidOperationException("A newly created database rejected its own key.");
    }

    /// <summary>Opens an existing database, or returns null when the passphrase is wrong.</summary>
    public static SqliteConnection? OpenExisting(string path, string passphrase) =>
        Open(path, passphrase, SqliteOpenMode.ReadWrite);

    private static SqliteConnection? Open(string path, string passphrase, SqliteOpenMode mode)
    {
        // Validate the key before touching the file, so a rejected passphrase never leaves an empty file behind.
        var keyLiteral = ToSqlStringLiteral(passphrase);

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = mode,
            // No pooling: closing a connection must really release the file and the key held in memory.
            Pooling = false,
        }.ToString();

        var connection = new SqliteConnection(connectionString);
        try
        {
            connection.Open();
            Execute(connection, "PRAGMA cipher = 'sqlcipher';");
            Execute(connection, "PRAGMA legacy = 4;");
            Execute(connection, $"PRAGMA key = {keyLiteral};");

            // The key is only checked when the first page is read.
            Execute(connection, "SELECT count(*) FROM sqlite_master;");
            return connection;
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == SqliteNotADatabase)
        {
            connection.Dispose();
            return null;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    /// <summary>
    /// PRAGMA does not accept parameters, so the passphrase has to become a SQL string literal.
    /// Concatenating it raw would let a passphrase containing a quote character break out of the
    /// literal and run its own SQL: SQL injection. Doubling every single quote is the SQL standard
    /// escape and the only one SQLite needs. It cannot be delegated to SELECT quote(), because no
    /// statement may run before the key on an existing file: preparing one reads the encrypted schema.
    /// </summary>
    internal static string ToSqlStringLiteral(string value)
    {
        if (value.Contains('\0'))
        {
            // SQLite would silently truncate the key at a NUL character.
            throw new ArgumentException("A passphrase cannot contain a NUL character.", nameof(value));
        }

        return "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
    }

    private static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
