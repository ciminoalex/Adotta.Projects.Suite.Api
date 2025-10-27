namespace ADOTTA.Projects.Suite.Api.DTOs;

public class ChangeLogDto
{
    public int Id { get; set; }
    public string NumeroProgetto { get; set; } = string.Empty;
    public DateTime Data { get; set; }
    public string Utente { get; set; } = string.Empty;
    public string Azione { get; set; } = string.Empty; // 'created', 'updated', 'deleted', etc.
    public string Descrizione { get; set; } = string.Empty;
    public Dictionary<string, string>? Dettagli { get; set; }
}

