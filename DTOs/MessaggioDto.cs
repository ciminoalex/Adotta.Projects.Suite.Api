namespace ADOTTA.Projects.Suite.Api.DTOs;

public class MessaggioProgettoDto
{
    public int Id { get; set; }
    public string NumeroProgetto { get; set; } = string.Empty;
    public DateTime Data { get; set; }
    public string Utente { get; set; } = string.Empty;
    public string Messaggio { get; set; } = string.Empty;
    public string Tipo { get; set; } = "info"; // 'info', 'success', 'warning', 'error'
    public string? Allegato { get; set; }
}

