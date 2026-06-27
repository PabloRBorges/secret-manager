using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SecretManager.Crypto;
using SecretManager.Models;

namespace SecretManager.Storage;

/// <summary>
/// Persistencia do cofre em disco e backup criptografado para pendrive.
/// O arquivo principal fica em %APPDATA%\SecretManager\vault.smgr.
///
/// Mantem a chave derivada em memoria apos o destravamento para evitar
/// rodar Argon2 a cada gravacao; chame <see cref="Lock"/> para zera-la.
/// </summary>
public sealed class VaultStore : IDisposable
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
    };

    public const string FileExtension = ".smgr";
    public const string DefaultFileName = "vault" + FileExtension;

    private byte[]? _key;   // chave AES viva enquanto destravado
    private byte[]? _salt;

    public string VaultPath { get; }
    public Vault? Current { get; private set; }
    public bool IsUnlocked => _key is not null && Current is not null;

    public VaultStore(string? customPath = null)
    {
        VaultPath = customPath ?? Path.Combine(AppDirectory, DefaultFileName);
    }

    public static string AppDirectory
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SecretManager");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public bool VaultExists => File.Exists(VaultPath);

    /// <summary>Cria um cofre novo e vazio protegido pela senha mestra.</summary>
    public void Create(string masterPassword)
    {
        Current = new Vault();
        var plaintext = Serialize(Current);
        var blob = VaultCrypto.Encrypt(plaintext, masterPassword);
        // Re-deriva e guarda chave para gravacoes subsequentes.
        VaultCrypto.Decrypt(blob, masterPassword, out _key, out _salt);
        AtomicWrite(VaultPath, blob);
        CryptographicOperations.ZeroMemory(plaintext);
    }

    /// <summary>Destrava o cofre existente. Lanca em senha incorreta.</summary>
    public void Unlock(string masterPassword)
    {
        var blob = File.ReadAllBytes(VaultPath);
        var plaintext = VaultCrypto.Decrypt(blob, masterPassword, out var key, out var salt);
        try
        {
            Current = Deserialize(plaintext);
            _key = key;
            _salt = salt;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    /// <summary>Grava o cofre atual usando a chave em memoria (sem re-rodar Argon2).</summary>
    public void Save()
    {
        if (_key is null || _salt is null || Current is null)
            throw new InvalidOperationException("Cofre travado.");

        var plaintext = Serialize(Current);
        try
        {
            var blob = VaultCrypto.EncryptWithKey(plaintext, _key, _salt);
            AtomicWrite(VaultPath, blob);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    /// <summary>
    /// Grava uma copia criptografada do cofre no destino (ex.: pendrive).
    /// O backup usa exatamente o mesmo formato/cifra do arquivo principal —
    /// nada sai em texto claro.
    /// </summary>
    public string Backup(string destinationDirectory)
    {
        if (_key is null || _salt is null || Current is null)
            throw new InvalidOperationException("Cofre travado.");

        Directory.CreateDirectory(destinationDirectory);
        var stamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");
        var dest = Path.Combine(destinationDirectory, $"vault-backup-{stamp}{FileExtension}");

        var plaintext = Serialize(Current);
        try
        {
            var blob = VaultCrypto.EncryptWithKey(plaintext, _key, _salt);
            AtomicWrite(dest, blob);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
        return dest;
    }

    /// <summary>Troca a senha mestra re-criptografando todo o cofre com novo salt/chave.</summary>
    public void ChangeMasterPassword(string newPassword)
    {
        if (Current is null) throw new InvalidOperationException("Cofre travado.");
        var plaintext = Serialize(Current);
        try
        {
            var blob = VaultCrypto.Encrypt(plaintext, newPassword);
            VaultCrypto.Decrypt(blob, newPassword, out var newKey, out var newSalt);
            if (_key is not null) CryptographicOperations.ZeroMemory(_key);
            _key = newKey;
            _salt = newSalt;
            AtomicWrite(VaultPath, blob);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    /// <summary>Zera a chave em memoria e descarta o conteudo destravado.</summary>
    public void Lock()
    {
        if (_key is not null) CryptographicOperations.ZeroMemory(_key);
        _key = null;
        _salt = null;
        Current = null;
    }

    private static byte[] Serialize(Vault vault) =>
        JsonSerializer.SerializeToUtf8Bytes(vault, JsonOpts);

    private static Vault Deserialize(byte[] json) =>
        JsonSerializer.Deserialize<Vault>(json, JsonOpts) ?? new Vault();

    /// <summary>Grava de forma atomica (temp + replace) para nao corromper em queda de energia.</summary>
    private static void AtomicWrite(string path, byte[] data)
    {
        var tmp = path + ".tmp";
        File.WriteAllBytes(tmp, data);
        if (File.Exists(path))
            File.Replace(tmp, path, null);
        else
            File.Move(tmp, path);
    }

    public void Dispose() => Lock();
}
