using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace SecretManager.Crypto;

/// <summary>
/// Criptografia autenticada do cofre.
///
/// Esquema:
///   - Derivacao de chave: Argon2id (resistente a GPU/ASIC) a partir da senha mestra.
///   - Cifra: AES-256-GCM (confidencialidade + integridade autenticada).
///   - Nonce de 96 bits aleatorio por gravacao; tag de 128 bits.
///
/// A senha mestra nunca e gravada. A chave derivada vive apenas em memoria
/// enquanto o cofre esta destravado e e zerada no lock.
/// </summary>
public static class VaultCrypto
{
    // Identificador de formato no inicio do arquivo.
    private static readonly byte[] Magic = "SMGR"u8.ToArray();
    private const byte FormatVersion = 1;

    // Parametros Argon2id (custo deliberadamente alto para uso desktop).
    public const int Argon2MemoryKb = 64 * 1024;   // 64 MiB
    public const int Argon2Iterations = 4;          // passes
    public const int Argon2Parallelism = 2;
    private const int KeyBytes = 32;                // AES-256
    private const int SaltBytes = 16;
    private const int NonceBytes = 12;              // GCM padrao
    private const int TagBytes = 16;

    /// <summary>Deriva a chave AES de 256 bits a partir da senha mestra e do salt.</summary>
    public static byte[] DeriveKey(string masterPassword, byte[] salt)
    {
        var pwBytes = Encoding.UTF8.GetBytes(masterPassword);
        try
        {
            using var argon = new Argon2id(pwBytes)
            {
                Salt = salt,
                MemorySize = Argon2MemoryKb,
                Iterations = Argon2Iterations,
                DegreeOfParallelism = Argon2Parallelism,
            };
            return argon.GetBytes(KeyBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pwBytes);
        }
    }

    /// <summary>Criptografa o JSON do cofre com a senha mestra, produzindo o blob de arquivo.</summary>
    public static byte[] Encrypt(byte[] plaintext, string masterPassword)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var key = DeriveKey(masterPassword, salt);
        try
        {
            return EncryptWithKey(plaintext, key, salt);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    /// <summary>Variante que reaproveita uma chave ja derivada (evita re-rodar Argon2 a cada save).</summary>
    public static byte[] EncryptWithKey(byte[] plaintext, byte[] key, byte[] salt)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceBytes);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagBytes];

        using (var aes = new AesGcm(key, TagBytes))
        {
            aes.Encrypt(nonce, plaintext, ciphertext, tag);
        }

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write(Magic);
        bw.Write(FormatVersion);
        bw.Write(Argon2MemoryKb);
        bw.Write(Argon2Iterations);
        bw.Write((byte)Argon2Parallelism);
        bw.Write((byte)salt.Length);
        bw.Write(salt);
        bw.Write(nonce);
        bw.Write(tag);
        bw.Write(ciphertext.Length);
        bw.Write(ciphertext);
        bw.Flush();
        return ms.ToArray();
    }

    /// <summary>
    /// Descriptografa o blob de arquivo. Lanca <see cref="CryptographicException"/>
    /// se a senha estiver errada ou os dados forem adulterados (falha na tag GCM).
    /// </summary>
    public static byte[] Decrypt(byte[] fileBytes, string masterPassword, out byte[] derivedKey, out byte[] salt)
    {
        using var ms = new MemoryStream(fileBytes);
        using var br = new BinaryReader(ms);

        var magic = br.ReadBytes(Magic.Length);
        if (!magic.AsSpan().SequenceEqual(Magic))
            throw new InvalidDataException("Arquivo de cofre invalido ou corrompido.");

        var version = br.ReadByte();
        if (version != FormatVersion)
            throw new InvalidDataException($"Versao de formato nao suportada: {version}.");

        _ = br.ReadInt32(); // memoria Argon2 (informativo; usamos parametros do arquivo abaixo)
        _ = br.ReadInt32(); // iteracoes
        _ = br.ReadByte();  // paralelismo

        var saltLen = br.ReadByte();
        salt = br.ReadBytes(saltLen);
        var nonce = br.ReadBytes(NonceBytes);
        var tag = br.ReadBytes(TagBytes);
        var ctLen = br.ReadInt32();
        var ciphertext = br.ReadBytes(ctLen);

        derivedKey = DeriveKey(masterPassword, salt);
        var plaintext = new byte[ciphertext.Length];
        try
        {
            using var aes = new AesGcm(derivedKey, TagBytes);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
        }
        catch (CryptographicException)
        {
            CryptographicOperations.ZeroMemory(derivedKey);
            throw new CryptographicException("Senha mestra incorreta ou cofre adulterado.");
        }
        return plaintext;
    }
}
