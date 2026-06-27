namespace SecretManager.Models;

/// <summary>
/// Conteudo logico do cofre (a lista de credenciais), serializado para JSON
/// antes de ser criptografado. Nunca toca o disco em texto claro.
/// </summary>
public sealed class Vault
{
    public int SchemaVersion { get; set; } = 1;
    public List<VaultEntry> Entries { get; set; } = new();

    public IEnumerable<VaultEntry> Search(string term) =>
        Entries.Where(e => e.Matches(term)).OrderBy(e => e.Title, StringComparer.OrdinalIgnoreCase);
}
