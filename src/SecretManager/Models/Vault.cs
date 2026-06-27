namespace SecretManager.Models;

/// <summary>
/// Conteudo logico do cofre (a lista de credenciais), serializado para JSON
/// antes de ser criptografado. Nunca toca o disco em texto claro.
/// </summary>
public sealed class Vault
{
    public int SchemaVersion { get; set; } = 1;
    public List<VaultEntry> Entries { get; set; } = new();

    /// <summary>Rotulo usado para credenciais sem grupo definido.</summary>
    public const string NoGroupLabel = "Sem grupo";

    public IEnumerable<VaultEntry> Search(string term) =>
        Entries.Where(e => e.Matches(term)).OrderBy(e => e.Title, StringComparer.OrdinalIgnoreCase);

    /// <summary>Grupos distintos existentes (ignora vazios), em ordem alfabetica.</summary>
    public IEnumerable<string> Groups() =>
        Entries.Select(e => e.Group)
               .Where(g => !string.IsNullOrWhiteSpace(g))
               .Select(g => g.Trim())
               .Distinct(StringComparer.OrdinalIgnoreCase)
               .OrderBy(g => g, StringComparer.OrdinalIgnoreCase);

    /// <summary>Nome do grupo para exibicao (vazio vira "Sem grupo").</summary>
    public static string DisplayGroup(string group) =>
        string.IsNullOrWhiteSpace(group) ? NoGroupLabel : group.Trim();
}
