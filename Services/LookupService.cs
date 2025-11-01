using System.Text.Json;
using ADOTTA.Projects.Suite.Api.Models.Lookup;

namespace ADOTTA.Projects.Suite.Api.Services;

public class LookupService : ILookupService
{
    private readonly ISAPServiceLayerClient _sapClient;
    private readonly ILogger<LookupService> _logger;

    public LookupService(ISAPServiceLayerClient sapClient, ILogger<LookupService> logger)
    {
        _sapClient = sapClient;
        _logger = logger;
    }

    public async Task<List<Cliente>> GetAllClientiAsync(string sessionId)
    {
        var sapData = await _sapClient.GetRecordsAsync<JsonElement>("BusinessPartners", "CardType eq 'C'", sessionId);
        return sapData.Select(MapToCliente).ToList();
    }

    public async Task<Cliente?> GetClienteByIdAsync(string id, string sessionId)
    {
        var sapData = await _sapClient.GetRecordAsync<JsonElement>("BusinessPartners", id, sessionId);
        if (sapData.ValueKind == JsonValueKind.Undefined || sapData.ValueKind == JsonValueKind.Null) return null;
        return MapToCliente(sapData);
    }

    public async Task<List<Stato>> GetAllStatiAsync(string sessionId)
    {
        var sapData = await _sapClient.GetRecordsAsync<JsonElement>("AX_ADT_STATI", null, sessionId);
        return sapData.Select(MapToStato).ToList();
    }

    public async Task<List<Citta>> GetAllCittaAsync(string sessionId, string? statoId = null)
    {
        string? filter = null;
        if (!string.IsNullOrEmpty(statoId))
        {
            filter = $"U_StatoId eq '{statoId}'";
        }
        var sapData = await _sapClient.GetRecordsAsync<JsonElement>("AX_ADT_CITTA", filter, sessionId);
        return sapData.Select(MapToCitta).ToList();
    }

    public async Task<List<TeamTecnico>> GetAllTeamTecniciAsync(string sessionId)
    {
        var sapData = await _sapClient.GetRecordsAsync<JsonElement>("AX_ADT_TEAMTECH", null, sessionId);
        return sapData.Select(MapToTeamTecnico).ToList();
    }

    public async Task<List<TeamAPL>> GetAllTeamAPLAsync(string sessionId)
    {
        var sapData = await _sapClient.GetRecordsAsync<JsonElement>("AX_ADT_TEAMAPL", null, sessionId);
        return sapData.Select(MapToTeamAPL).ToList();
    }

    public async Task<List<Sales>> GetAllSalesAsync(string sessionId)
    {
        var sapData = await _sapClient.GetRecordsAsync<JsonElement>("AX_ADT_SALES", null, sessionId);
        return sapData.Select(MapToSales).ToList();
    }

    public async Task<List<ProjectManager>> GetAllProjectManagersAsync(string sessionId)
    {
        var sapData = await _sapClient.GetRecordsAsync<JsonElement>("AX_ADT_PMGR", null, sessionId);
        return sapData.Select(MapToProjectManager).ToList();
    }

    public async Task<List<SquadraInstallazione>> GetAllSquadreInstallazioneAsync(string sessionId)
    {
        var sapData = await _sapClient.GetRecordsAsync<JsonElement>("AX_ADT_SQUADRA", null, sessionId);
        return sapData.Select(MapToSquadraInstallazione).ToList();
    }

    public async Task<List<ProdottoMaster>> GetAllProdottiMasterAsync(string sessionId, string? categoria = null)
    {
        string? filter = null;
        if (!string.IsNullOrEmpty(categoria))
        {
            filter = $"U_Categoria eq '{categoria}'";
        }
        var sapData = await _sapClient.GetRecordsAsync<JsonElement>("AX_ADT_PRODMAST", filter, sessionId);
        return sapData.Select(MapToProdottoMaster).ToList();
    }

    private Cliente MapToCliente(JsonElement sapData)
    {
        var addresses = new List<BPAddress>();
        if (sapData.TryGetProperty("BPAddresses", out var addrArray) && addrArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var addr in addrArray.EnumerateArray())
            {
                addresses.Add(new BPAddress
                {
                    AddressName = addr.TryGetProperty("AddressName", out var name) ? name.GetString() ?? "" : "",
                    Street = addr.TryGetProperty("Street", out var street) ? street.GetString() : null,
                    City = addr.TryGetProperty("City", out var city) ? city.GetString() : null,
                    Country = addr.TryGetProperty("Country", out var country) ? country.GetString() : null,
                    ZipCode = addr.TryGetProperty("ZipCode", out var zip) ? zip.GetString() : null
                });
            }
        }

        var firstAddress = addresses.FirstOrDefault();
        
        return new Cliente
        {
            Id = sapData.TryGetProperty("CardCode", out var code) ? code.GetString() ?? "" : "",
            CardCode = sapData.TryGetProperty("CardCode", out var cardCode) ? cardCode.GetString() ?? "" : "",
            Nome = sapData.TryGetProperty("CardName", out var cardName) ? cardName.GetString() ?? "" : "",
            Email = sapData.TryGetProperty("EmailAddress", out var email) ? email.GetString() : null,
            Telefono = sapData.TryGetProperty("Phone1", out var phone) ? phone.GetString() : null,
            PartitaIVA = sapData.TryGetProperty("FederalTaxID", out var tax) ? tax.GetString() : null,
            Note = sapData.TryGetProperty("Notes", out var notes) ? notes.GetString() : null,
            IndirizzoCompleto = firstAddress != null ? $"{firstAddress.Street}, {firstAddress.City} {firstAddress.ZipCode}" : null,
            Citta = firstAddress?.City,
            Provincia = null,
            Cap = firstAddress?.ZipCode,
            Stato = firstAddress?.Country,
            ValidFor = sapData.TryGetProperty("ValidFor", out var valid) ? valid.GetString() ?? "Y" : "Y",
            Addresses = addresses
        };
    }

    private Stato MapToStato(JsonElement sapData)
    {
        return new Stato
        {
            Id = sapData.TryGetProperty("Code", out var code) ? code.GetString() ?? "" : "",
            Nome = sapData.TryGetProperty("Name", out var name) ? name.GetString() ?? "" : "",
            CodiceISO = sapData.TryGetProperty("U_CodiceISO", out var iso) ? iso.GetString() ?? "" : "",
            Continente = sapData.TryGetProperty("U_Continente", out var cont) ? cont.GetString() ?? "" : ""
        };
    }

    private Citta MapToCitta(JsonElement sapData)
    {
        return new Citta
        {
            Id = sapData.TryGetProperty("Code", out var code) ? code.GetString() ?? "" : "",
            Nome = sapData.TryGetProperty("Name", out var name) ? name.GetString() ?? "" : "",
            StatoId = sapData.TryGetProperty("U_StatoId", out var stateId) ? stateId.GetString() ?? "" : "",
            Cap = sapData.TryGetProperty("U_Cap", out var cap) ? cap.GetString() : null,
            Provincia = sapData.TryGetProperty("U_Provincia", out var prov) ? prov.GetString() : null,
            Regione = sapData.TryGetProperty("U_Regione", out var reg) ? reg.GetString() : null
        };
    }

    private TeamTecnico MapToTeamTecnico(JsonElement sapData)
    {
        var membri = new List<string>();
        if (sapData.TryGetProperty("U_Membri", out var memb))
        {
            if (memb.ValueKind == JsonValueKind.String)
            {
                membri.AddRange(memb.GetString()?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Enumerable.Empty<string>());
            }
        }

        return new TeamTecnico
        {
            Id = sapData.TryGetProperty("Code", out var code) ? code.GetString() ?? "" : "",
            Nome = sapData.TryGetProperty("Name", out var name) ? name.GetString() ?? "" : "",
            Specializzazione = sapData.TryGetProperty("U_Specializzazione", out var spec) ? spec.GetString() : null,
            Email = sapData.TryGetProperty("U_Email", out var email) ? email.GetString() : null,
            Telefono = sapData.TryGetProperty("U_Telefono", out var phone) ? phone.GetString() : null,
            Disponibilita = sapData.TryGetProperty("U_Disponibilita", out var disp) ? disp.GetString() == "Y" : true,
            Membri = membri.Any() ? membri : null
        };
    }

    private TeamAPL MapToTeamAPL(JsonElement sapData)
    {
        var competenze = new List<string>();
        if (sapData.TryGetProperty("U_Competenze", out var comp))
        {
            if (comp.ValueKind == JsonValueKind.String)
            {
                competenze.AddRange(comp.GetString()?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Enumerable.Empty<string>());
            }
        }

        return new TeamAPL
        {
            Id = sapData.TryGetProperty("Code", out var code) ? code.GetString() ?? "" : "",
            Nome = sapData.TryGetProperty("Name", out var name) ? name.GetString() ?? "" : "",
            Email = sapData.TryGetProperty("U_Email", out var email) ? email.GetString() : null,
            Telefono = sapData.TryGetProperty("U_Telefono", out var phone) ? phone.GetString() : null,
            Area = sapData.TryGetProperty("U_Area", out var area) ? area.GetString() : null,
            Competenze = competenze.Any() ? competenze : null
        };
    }

    private Sales MapToSales(JsonElement sapData)
    {
        return new Sales
        {
            Id = sapData.TryGetProperty("Code", out var code) ? code.GetString() ?? "" : "",
            Nome = sapData.TryGetProperty("Name", out var name) ? name.GetString() ?? "" : "",
            Email = sapData.TryGetProperty("U_Email", out var email) ? email.GetString() : null,
            Telefono = sapData.TryGetProperty("U_Telefono", out var phone) ? phone.GetString() : null,
            Zona = sapData.TryGetProperty("U_Zona", out var zone) ? zone.GetString() : null,
            RegioneDiCompetenza = sapData.TryGetProperty("U_RegioneCompetenza", out var reg) ? reg.GetString() : null
        };
    }

    private ProjectManager MapToProjectManager(JsonElement sapData)
    {
        var certificazioni = new List<string>();
        if (sapData.TryGetProperty("U_Certificazioni", out var cert))
        {
            if (cert.ValueKind == JsonValueKind.String)
            {
                certificazioni.AddRange(cert.GetString()?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Enumerable.Empty<string>());
            }
        }

        return new ProjectManager
        {
            Id = sapData.TryGetProperty("Code", out var code) ? code.GetString() ?? "" : "",
            Nome = sapData.TryGetProperty("Name", out var name) ? name.GetString() ?? "" : "",
            Email = sapData.TryGetProperty("U_Email", out var email) ? email.GetString() : null,
            Telefono = sapData.TryGetProperty("U_Telefono", out var phone) ? phone.GetString() : null,
            Esperienza = sapData.TryGetProperty("U_Esperienza", out var exp) ? exp.GetString() : null,
            ProgettiAttivi = sapData.TryGetProperty("U_ProgettiAttivi", out var proj) ? proj.GetInt32() : null,
            Certificazioni = certificazioni.Any() ? certificazioni : null
        };
    }

    private SquadraInstallazione MapToSquadraInstallazione(JsonElement sapData)
    {
        var competenze = new List<string>();
        if (sapData.TryGetProperty("U_Competenze", out var comp))
        {
            if (comp.ValueKind == JsonValueKind.String)
            {
                competenze.AddRange(comp.GetString()?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Enumerable.Empty<string>());
            }
        }

        return new SquadraInstallazione
        {
            Id = sapData.TryGetProperty("Code", out var code) ? code.GetString() ?? "" : "",
            Nome = sapData.TryGetProperty("Name", out var name) ? name.GetString() ?? "" : "",
            Tipo = sapData.TryGetProperty("U_Tipo", out var tipo) ? tipo.GetString() : null,
            Contatto = sapData.TryGetProperty("U_Contatto", out var cont) ? cont.GetString() : null,
            Disponibilita = sapData.TryGetProperty("U_Disponibilita", out var disp) ? disp.GetString() == "Y" : true,
            NumeroMembri = sapData.TryGetProperty("U_NumeroMembri", out var membri) ? membri.GetInt32() : null,
            Competenze = competenze.Any() ? competenze : null
        };
    }

    private ProdottoMaster MapToProdottoMaster(JsonElement sapData)
    {
        var varianti = new List<string>();
        if (sapData.TryGetProperty("U_Varianti", out var vari))
        {
            if (vari.ValueKind == JsonValueKind.String)
            {
                varianti.AddRange(vari.GetString()?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Enumerable.Empty<string>());
            }
        }

        return new ProdottoMaster
        {
            Id = sapData.TryGetProperty("Code", out var code) ? code.GetString() ?? "" : "",
            Nome = sapData.TryGetProperty("Name", out var name) ? name.GetString() ?? "" : "",
            Categoria = sapData.TryGetProperty("U_Categoria", out var cat) ? cat.GetString() ?? "" : "",
            UnitaMisura = sapData.TryGetProperty("U_UnitaMisura", out var unit) ? unit.GetString() ?? "" : "",
            CodiceSAP = sapData.TryGetProperty("U_CodiceSAP", out var sapCode) ? sapCode.GetString() : null,
            Descrizione = sapData.TryGetProperty("U_Descrizione", out var desc) ? desc.GetString() : null,
            VariantiDisponibili = varianti.Any() ? varianti : null
        };
    }
}

