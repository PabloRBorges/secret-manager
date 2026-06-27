namespace SecretManager.Models;

/// <summary>
/// Uma credencial armazenada no cofre. Todos os campos sao criptografados
/// em repouso (o objeto so existe em memoria enquanto o cofre esta destravado).
/// </summary>
public sealed class VaultEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool Matches(string term)
    {
        if (string.IsNullOrWhiteSpace(term)) return true;
        return Title.Contains(term, StringComparison.OrdinalIgnoreCase)
            || Username.Contains(term, StringComparison.OrdinalIgnoreCase)
            || Url.Contains(term, StringComparison.OrdinalIgnoreCase)
            || Notes.Contains(term, StringComparison.OrdinalIgnoreCase);
    }
}
