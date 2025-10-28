namespace ADOTTA.Projects.Suite.Api.Configuration;

public class SAPSettings
{
    public string ServiceLayerUrl { get; set; } = string.Empty;
    public string CompanyDB { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string DefaultLanguage { get; set; } = "en";
    public int SessionTimeout { get; set; } = 30;
    public bool AllowUntrustedServerCertificate { get; set; } = false;
}

