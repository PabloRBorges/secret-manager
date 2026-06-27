using System.Security.Cryptography;

namespace SecretManager.Crypto;

/// <summary>Gera senhas fortes usando RNG criptografico (sem vies de modulo).</summary>
public static class PasswordGenerator
{
    private const string Lower = "abcdefghijkmnopqrstuvwxyz";      // sem l
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";       // sem I, O
    private const string Digits = "23456789";                     // sem 0, 1
    private const string Symbols = "!@#$%&*-_=+?";

    public static string Generate(
        int length = 20,
        bool useUpper = true,
        bool useLower = true,
        bool useDigits = true,
        bool useSymbols = true)
    {
        var pool = string.Concat(
            useLower ? Lower : "",
            useUpper ? Upper : "",
            useDigits ? Digits : "",
            useSymbols ? Symbols : "");

        if (pool.Length == 0) pool = Lower + Upper + Digits;
        if (length < 4) length = 4;

        var chars = new char[length];
        for (int i = 0; i < length; i++)
            chars[i] = pool[RandomNumberGenerator.GetInt32(pool.Length)];

        return new string(chars);
    }
}
