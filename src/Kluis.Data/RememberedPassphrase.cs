using System.Security.Cryptography;
using System.Text;

namespace Kluis.Data;

/// <summary>
/// The optional "remember on this PC" secret: the passphrase, encrypted with Windows DPAPI
/// (Data Protection API) for the current Windows user and stored next to the database.
/// </summary>
/// <remarks>
/// DPAPI with CurrentUser scope ties the encryption to this Windows account, so the file is useless
/// copied to another machine or opened by another user. It does not protect against anything already
/// running as this user, malware included: that program can ask DPAPI to decrypt it exactly as Kluis
/// does. That is the accepted trade-off of the setting, which is why it is off by default.
/// </remarks>
public sealed class RememberedPassphrase(string path)
{
    /// <summary>
    /// Extra input mixed into the DPAPI encryption. Another program running as the same user must also
    /// know this value to decrypt the file, so a generic "unprotect every blob I find" tool fails.
    /// It is not a secret (the source is public); it only narrows who decrypts by accident.
    /// </summary>
    private static readonly byte[] Entropy = "Kluis.RememberedPassphrase.v1"u8.ToArray();

    public string Path { get; } = path;

    public bool Exists => File.Exists(Path);

    /// <summary>Stored beside the database, so a database in another folder never picks up this secret.</summary>
    public static RememberedPassphrase For(string databasePath) => new(System.IO.Path.Combine(
        System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(databasePath))!,
        System.IO.Path.GetFileNameWithoutExtension(databasePath) + ".remembered"));

    public void Save(string passphrase)
    {
        var plain = Encoding.UTF8.GetBytes(passphrase);
        try
        {
            var protectedBytes = ProtectedData.Protect(plain, Entropy, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(Path, protectedBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plain);
        }
    }

    /// <summary>The stored passphrase, or null when there is none or it can no longer be decrypted.</summary>
    public string? TryLoad()
    {
        if (!Exists)
        {
            return null;
        }

        try
        {
            var plain = ProtectedData.Unprotect(File.ReadAllBytes(Path), Entropy, DataProtectionScope.CurrentUser);
            try
            {
                return Encoding.UTF8.GetString(plain);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plain);
            }
        }
        catch (Exception ex) when (ex is CryptographicException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public void Delete()
    {
        if (Exists)
        {
            File.Delete(Path);
        }
    }
}
