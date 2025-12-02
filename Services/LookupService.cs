using System.Linq;
using System.Text.Json;
using ADOTTA.Projects.Suite.Api.DTOs;
using ADOTTA.Projects.Suite.Api.Models.Lookup;

namespace ADOTTA.Projects.Suite.Api.Services;

public class LookupService : ILookupService
{
    private const string BusinessPartnersTable = "BusinessPartners";
    private const string StatiTable = "AX_ADT_STATI";
    private const string CittaTable = "AX_ADT_CITTA";
    private const string TeamTecniciTable = "AX_ADT_TEAMTECH";
    private const string TeamAPLTable = "AX_ADT_TEAMAPL";
    private const string SalesTable = "AX_ADT_SALES";
    private const string ProjectManagerTable = "AX_ADT_PMGR";
    private const string SquadraInstallazioneTable = "AX_ADT_SQUADRA";
    private const string ProdottiMasterTable = "AX_ADT_PRODMAST";

    private readonly ISAPServiceLayerClient _sapClient;
    private readonly ILogger<LookupService> _logger;

    public LookupService(ISAPServiceLayerClient sapClient, ILogger<LookupService> logger)
    {
        _sapClient = sapClient;
        _logger = logger;
    }

    public async Task<List<Cliente>> GetAllClientiAsync(string sessionId)
    {
        var sapData = await _sapClient.GetRecordsAsync<JsonElement>(BusinessPartnersTable, "CardType eq 'C'", sessionId);
        return sapData.Select(MapToCliente).ToList();
    }

    public async Task<PagedResultDto<Cliente>> GetClientiPagedAsync(string sessionId, int page = 1, int pageSize = 20, string? search = null, string? sortBy = null, string? sortDirection = null)
    {
        var skip = (page - 1) * pageSize;
        var filter = "CardType eq 'C'";
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            var safeSearch = search.Replace("'", "''");
            filter += $" and (contains(CardCode, '{safeSearch}') or contains(CardName, '{safeSearch}') or contains(EmailAddress, '{safeSearch}'))";
        }

        var orderBy = BuildClientiOrderBy(sortBy, sortDirection);
        var (sapData, totalCount) = await _sapClient.GetRecordsPagedAsync<JsonElement>(BusinessPartnersTable, skip, pageSize, filter, sessionId, orderBy);
        var clienti = sapData.Select(MapToCliente).ToList();

        return new PagedResultDto<Cliente>
        {
            Items = clienti,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private static string BuildClientiOrderBy(string? sortBy, string? sortDirection)
    {
        var direction = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase) ? "desc" : "asc";

        var field = sortBy?.ToLowerInvariant() switch
        {
            "id" or "code" or "cardcode" => "CardCode",
            "email" or "emailaddress" => "EmailAddress",
            "nome" or "name" or "cardname" => "CardName",
            _ => "CardName"
        };

        return $"{field} {direction}";
    }

    public async Task<Cliente?> GetClienteByIdAsync(string id, string sessionId)
    {
        var sapData = await _sapClient.GetRecordAsync<JsonElement>(BusinessPartnersTable, id, sessionId);
        if (sapData.ValueKind == JsonValueKind.Undefined || sapData.ValueKind == JsonValueKind.Null) return null;
        return MapToCliente(sapData);
    }

    public async Task<List<Cliente>> SearchClientiAsync(string query, string sessionId)
    {
        var safeQuery = (query ?? string.Empty).Replace("'", "''");
        var filter = $"CardType eq 'C' and (contains(CardCode, '{safeQuery}') or contains(CardName, '{safeQuery}') or contains(EmailAddress, '{safeQuery}'))";
        var sapData = await _sapClient.GetRecordsAsync<JsonElement>(BusinessPartnersTable, filter, sessionId);
        return sapData.Select(MapToCliente).ToList();
    }

    public async Task<Cliente> CreateClienteAsync(Cliente cliente, string sessionId)
    {
        var payload = MapClienteToSap(cliente, isUpdate: false);
        var created = await _sapClient.CreateRecordAsync<JsonElement>(BusinessPartnersTable, payload, sessionId);
        return MapToCliente(created);
    }

    public async Task<Cliente> UpdateClienteAsync(string id, Cliente cliente, string sessionId)
    {
        cliente.CardCode = id;
        var payload = MapClienteToSap(cliente, isUpdate: true);
        await _sapClient.UpdateRecordAsync<JsonElement>(BusinessPartnersTable, id, payload, sessionId);
        var updatedSap = await _sapClient.GetRecordAsync<JsonElement>(BusinessPartnersTable, id, sessionId);
        return MapToCliente(updatedSap);
    }

    public async Task DeleteClienteAsync(string id, string sessionId)
    {
        await _sapClient.DeleteRecordAsync(BusinessPartnersTable, id, sessionId);
    }

    public async Task<List<Stato>> GetAllStatiAsync(string sessionId)
    {
        var sapData = await _sapClient.GetRecordsAsync<JsonElement>(StatiTable, null, sessionId);
        return sapData.Select(MapToStato).ToList();
    }

    public async Task<PagedResultDto<Stato>> GetStatiPagedAsync(string sessionId, int page = 1, int pageSize = 20)
    {
        var skip = (page - 1) * pageSize;
        var (sapData, totalCount) = await _sapClient.GetRecordsPagedAsync<JsonElement>(StatiTable, skip, pageSize, null, sessionId);
        var stati = sapData.Select(MapToStato).ToList();

        return new PagedResultDto<Stato>
        {
            Items = stati,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Stato?> GetStatoByIdAsync(string id, string sessionId)
    {
        var sapData = await _sapClient.GetRecordAsync<JsonElement>(StatiTable, id, sessionId);
        if (sapData.ValueKind == JsonValueKind.Undefined || sapData.ValueKind == JsonValueKind.Null) return null;
        return MapToStato(sapData);
    }

    public async Task<List<Citta>> GetAllCittaAsync(string sessionId, string? statoId = null)
    {
        string? filter = null;
        if (!string.IsNullOrEmpty(statoId))
        {
            filter = $"U_StatoId eq '{statoId}'";
        }
        var sapData = await _sapClient.GetRecordsAsync<JsonElement>(CittaTable, filter, sessionId);
        return sapData.Select(MapToCitta).ToList();
    }

    public async Task<Citta?> GetCittaByIdAsync(string id, string sessionId)
    {
        var sapData = await _sapClient.GetRecordAsync<JsonElement>(CittaTable, id, sessionId);
        if (sapData.ValueKind == JsonValueKind.Undefined || sapData.ValueKind == JsonValueKind.Null) return null;
        return MapToCitta(sapData);
    }

    public async Task<Citta> CreateCittaAsync(Citta citta, string sessionId)
    {
        var payload = MapCittaToSap(citta, isUpdate: false);
        var created = await _sapClient.CreateRecordAsync<JsonElement>(CittaTable, payload, sessionId);
        return MapToCitta(created);
    }

    public async Task<Citta> UpdateCittaAsync(string id, Citta citta, string sessionId)
    {
        citta.Id = id;
        var payload = MapCittaToSap(citta, isUpdate: true);
        await _sapClient.UpdateRecordAsync<JsonElement>(CittaTable, id, payload, sessionId);
        var updatedSap = await _sapClient.GetRecordAsync<JsonElement>(CittaTable, id, sessionId);
        return MapToCitta(updatedSap);
    }

    public async Task DeleteCittaAsync(string id, string sessionId)
    {
        await _sapClient.DeleteRecordAsync(CittaTable, id, sessionId);
    }

    public async Task<List<TeamTecnico>> GetAllTeamTecniciAsync(string sessionId)
    {
        var sapData = await _sapClient.GetRecordsAsync<JsonElement>(TeamTecniciTable, null, sessionId);
        return sapData.Select(MapToTeamTecnico).ToList();
    }

    public async Task<TeamTecnico?> GetTeamTecnicoByIdAsync(string id, string sessionId)
    {
        var sapData = await _sapClient.GetRecordAsync<JsonElement>(TeamTecniciTable, id, sessionId);
        if (sapData.ValueKind == JsonValueKind.Undefined || sapData.ValueKind == JsonValueKind.Null) return null;
        return MapToTeamTecnico(sapData);
    }

    public async Task<TeamTecnico> CreateTeamTecnicoAsync(TeamTecnico team, string sessionId)
    {
        var payload = MapTeamTecnicoToSap(team);
        var created = await _sapClient.CreateRecordAsync<JsonElement>(TeamTecniciTable, payload, sessionId);
        return MapToTeamTecnico(created);
    }

    public async Task<TeamTecnico> UpdateTeamTecnicoAsync(string id, TeamTecnico team, string sessionId)
    {
        team.Id = id;
        var payload = MapTeamTecnicoToSap(team);
        await _sapClient.UpdateRecordAsync<JsonElement>(TeamTecniciTable, id, payload, sessionId);
        var updatedSap = await _sapClient.GetRecordAsync<JsonElement>(TeamTecniciTable, id, sessionId);
        return MapToTeamTecnico(updatedSap);
    }

    public async Task DeleteTeamTecnicoAsync(string id, string sessionId)
    {
        await _sapClient.DeleteRecordAsync(TeamTecniciTable, id, sessionId);
    }

    public async Task<List<TeamAPL>> GetAllTeamAPLAsync(string sessionId)
    {
        var sapData = await _sapClient.GetRecordsAsync<JsonElement>(TeamAPLTable, null, sessionId);
        return sapData.Select(MapToTeamAPL).ToList();
    }

    public async Task<TeamAPL?> GetTeamAPLByIdAsync(string id, string sessionId)
    {
        var sapData = await _sapClient.GetRecordAsync<JsonElement>(TeamAPLTable, id, sessionId);
        if (sapData.ValueKind == JsonValueKind.Undefined || sapData.ValueKind == JsonValueKind.Null) return null;
        return MapToTeamAPL(sapData);
    }

    public async Task<TeamAPL> CreateTeamAPLAsync(TeamAPL team, string sessionId)
    {
        var payload = MapTeamAPLToSap(team);
        var created = await _sapClient.CreateRecordAsync<JsonElement>(TeamAPLTable, payload, sessionId);
        return MapToTeamAPL(created);
    }

    public async Task<TeamAPL> UpdateTeamAPLAsync(string id, TeamAPL team, string sessionId)
    {
        team.Id = id;
        var payload = MapTeamAPLToSap(team);
        await _sapClient.UpdateRecordAsync<JsonElement>(TeamAPLTable, id, payload, sessionId);
        var updatedSap = await _sapClient.GetRecordAsync<JsonElement>(TeamAPLTable, id, sessionId);
        return MapToTeamAPL(updatedSap);
    }

    public async Task DeleteTeamAPLAsync(string id, string sessionId)
    {
        await _sapClient.DeleteRecordAsync(TeamAPLTable, id, sessionId);
    }

    public async Task<List<Sales>> GetAllSalesAsync(string sessionId)
    {
        var sapData = await _sapClient.GetRecordsAsync<JsonElement>(SalesTable, null, sessionId);
        return sapData.Select(MapToSales).ToList();
    }

    public async Task<Sales?> GetSalesByIdAsync(string id, string sessionId)
    {
        var sapData = await _sapClient.GetRecordAsync<JsonElement>(SalesTable, id, sessionId);
        if (sapData.ValueKind == JsonValueKind.Undefined || sapData.ValueKind == JsonValueKind.Null) return null;
        return MapToSales(sapData);
    }

    public async Task<Sales> CreateSalesAsync(Sales sales, string sessionId)
    {
        var payload = MapSalesToSap(sales);
        var created = await _sapClient.CreateRecordAsync<JsonElement>(SalesTable, payload, sessionId);
        return MapToSales(created);
    }

    public async Task<Sales> UpdateSalesAsync(string id, Sales sales, string sessionId)
    {
        sales.Id = id;
        var payload = MapSalesToSap(sales);
        await _sapClient.UpdateRecordAsync<JsonElement>(SalesTable, id, payload, sessionId);
        var updatedSap = await _sapClient.GetRecordAsync<JsonElement>(SalesTable, id, sessionId);
        return MapToSales(updatedSap);
    }

    public async Task DeleteSalesAsync(string id, string sessionId)
    {
        await _sapClient.DeleteRecordAsync(SalesTable, id, sessionId);
    }

    public async Task<List<ProjectManager>> GetAllProjectManagersAsync(string sessionId)
    {
        var sapData = await _sapClient.GetRecordsAsync<JsonElement>(ProjectManagerTable, null, sessionId);
        return sapData.Select(MapToProjectManager).ToList();
    }

    public async Task<ProjectManager?> GetProjectManagerByIdAsync(string id, string sessionId)
    {
        var sapData = await _sapClient.GetRecordAsync<JsonElement>(ProjectManagerTable, id, sessionId);
        if (sapData.ValueKind == JsonValueKind.Undefined || sapData.ValueKind == JsonValueKind.Null) return null;
        return MapToProjectManager(sapData);
    }

    public async Task<ProjectManager> CreateProjectManagerAsync(ProjectManager manager, string sessionId)
    {
        var payload = MapProjectManagerToSap(manager);
        var created = await _sapClient.CreateRecordAsync<JsonElement>(ProjectManagerTable, payload, sessionId);
        return MapToProjectManager(created);
    }

    public async Task<ProjectManager> UpdateProjectManagerAsync(string id, ProjectManager manager, string sessionId)
    {
        manager.Id = id;
        var payload = MapProjectManagerToSap(manager);
        await _sapClient.UpdateRecordAsync<JsonElement>(ProjectManagerTable, id, payload, sessionId);
        var updatedSap = await _sapClient.GetRecordAsync<JsonElement>(ProjectManagerTable, id, sessionId);
        return MapToProjectManager(updatedSap);
    }

    public async Task DeleteProjectManagerAsync(string id, string sessionId)
    {
        await _sapClient.DeleteRecordAsync(ProjectManagerTable, id, sessionId);
    }

    public async Task<List<SquadraInstallazione>> GetAllSquadreInstallazioneAsync(string sessionId)
    {
        var sapData = await _sapClient.GetRecordsAsync<JsonElement>(SquadraInstallazioneTable, null, sessionId);
        return sapData.Select(MapToSquadraInstallazione).ToList();
    }

    public async Task<SquadraInstallazione?> GetSquadraInstallazioneByIdAsync(string id, string sessionId)
    {
        var sapData = await _sapClient.GetRecordAsync<JsonElement>(SquadraInstallazioneTable, id, sessionId);
        if (sapData.ValueKind == JsonValueKind.Undefined || sapData.ValueKind == JsonValueKind.Null) return null;
        return MapToSquadraInstallazione(sapData);
    }

    public async Task<SquadraInstallazione> CreateSquadraInstallazioneAsync(SquadraInstallazione squadra, string sessionId)
    {
        var payload = MapSquadraInstallazioneToSap(squadra);
        var created = await _sapClient.CreateRecordAsync<JsonElement>(SquadraInstallazioneTable, payload, sessionId);
        return MapToSquadraInstallazione(created);
    }

    public async Task<SquadraInstallazione> UpdateSquadraInstallazioneAsync(string id, SquadraInstallazione squadra, string sessionId)
    {
        squadra.Id = id;
        var payload = MapSquadraInstallazioneToSap(squadra);
        await _sapClient.UpdateRecordAsync<JsonElement>(SquadraInstallazioneTable, id, payload, sessionId);
        var updatedSap = await _sapClient.GetRecordAsync<JsonElement>(SquadraInstallazioneTable, id, sessionId);
        return MapToSquadraInstallazione(updatedSap);
    }

    public async Task DeleteSquadraInstallazioneAsync(string id, string sessionId)
    {
        await _sapClient.DeleteRecordAsync(SquadraInstallazioneTable, id, sessionId);
    }

    public async Task<List<ProdottoMaster>> GetAllProdottiMasterAsync(string sessionId, string? categoria = null)
    {
        string? filter = null;
        if (!string.IsNullOrEmpty(categoria))
        {
            filter = $"U_Categoria eq '{categoria}'";
        }
        var sapData = await _sapClient.GetRecordsAsync<JsonElement>(ProdottiMasterTable, filter, sessionId);
        return sapData.Select(MapToProdottoMaster).ToList();
    }

    public async Task<ProdottoMaster?> GetProdottoMasterByIdAsync(string id, string sessionId)
    {
        var sapData = await _sapClient.GetRecordAsync<JsonElement>(ProdottiMasterTable, id, sessionId);
        if (sapData.ValueKind == JsonValueKind.Undefined || sapData.ValueKind == JsonValueKind.Null) return null;
        return MapToProdottoMaster(sapData);
    }

    public async Task<ProdottoMaster> CreateProdottoMasterAsync(ProdottoMaster prodotto, string sessionId)
    {
        var payload = MapProdottoMasterToSap(prodotto);
        var created = await _sapClient.CreateRecordAsync<JsonElement>(ProdottiMasterTable, payload, sessionId);
        return MapToProdottoMaster(created);
    }

    public async Task<ProdottoMaster> UpdateProdottoMasterAsync(string id, ProdottoMaster prodotto, string sessionId)
    {
        prodotto.Id = id;
        var payload = MapProdottoMasterToSap(prodotto);
        await _sapClient.UpdateRecordAsync<JsonElement>(ProdottiMasterTable, id, payload, sessionId);
        var updatedSap = await _sapClient.GetRecordAsync<JsonElement>(ProdottiMasterTable, id, sessionId);
        return MapToProdottoMaster(updatedSap);
    }

    public async Task DeleteProdottoMasterAsync(string id, string sessionId)
    {
        await _sapClient.DeleteRecordAsync(ProdottiMasterTable, id, sessionId);
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

    private object MapClienteToSap(Cliente cliente, bool isUpdate)
    {
        var cardCode = string.IsNullOrWhiteSpace(cliente.CardCode) ? cliente.Id : cliente.CardCode;
        if (string.IsNullOrWhiteSpace(cardCode))
        {
            cardCode = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        }

        return new
        {
            CardCode = cardCode,
            CardName = cliente.Nome,
            CardType = "C",
            EmailAddress = cliente.Email,
            Phone1 = cliente.Telefono,
            FederalTaxID = cliente.PartitaIVA,
            ValidFor = string.IsNullOrWhiteSpace(cliente.ValidFor) ? "Y" : cliente.ValidFor,
            Notes = cliente.Note,
            BPAddresses = cliente.Addresses?.Select(a => new
            {
                AddressName = a.AddressName,
                Street = a.Street,
                City = a.City,
                Country = a.Country,
                ZipCode = a.ZipCode
            }).ToArray()
        };
    }

    private object MapCittaToSap(Citta citta, bool isUpdate)
    {
        var code = string.IsNullOrWhiteSpace(citta.Id) ? Guid.NewGuid().ToString("N") : citta.Id;
        return new
        {
            Code = code,
            Name = citta.Nome,
            U_StatoId = citta.StatoId,
            U_Cap = citta.Cap,
            U_Provincia = citta.Provincia,
            U_Regione = citta.Regione
        };
    }

    private object MapTeamTecnicoToSap(TeamTecnico team)
    {
        var code = string.IsNullOrWhiteSpace(team.Id) ? Guid.NewGuid().ToString("N") : team.Id;
        return new
        {
            Code = code,
            Name = team.Nome,
            U_Specializzazione = team.Specializzazione,
            U_Email = team.Email,
            U_Telefono = team.Telefono,
            U_Disponibilita = team.Disponibilita ? "Y" : "N",
            U_Membri = SerializeList(team.Membri)
        };
    }

    private object MapTeamAPLToSap(TeamAPL team)
    {
        var code = string.IsNullOrWhiteSpace(team.Id) ? Guid.NewGuid().ToString("N") : team.Id;
        return new
        {
            Code = code,
            Name = team.Nome,
            U_Email = team.Email,
            U_Telefono = team.Telefono,
            U_Area = team.Area,
            U_Competenze = SerializeList(team.Competenze)
        };
    }

    private object MapSalesToSap(Sales sales)
    {
        var code = string.IsNullOrWhiteSpace(sales.Id) ? Guid.NewGuid().ToString("N") : sales.Id;
        return new
        {
            Code = code,
            Name = sales.Nome,
            U_Email = sales.Email,
            U_Telefono = sales.Telefono,
            U_Zona = sales.Zona,
            U_RegioneCompetenza = sales.RegioneDiCompetenza,
            U_ProgettiGestiti = sales.ProgettiGestiti ?? 0
        };
    }

    private object MapProjectManagerToSap(ProjectManager manager)
    {
        var code = string.IsNullOrWhiteSpace(manager.Id) ? Guid.NewGuid().ToString("N") : manager.Id;
        return new
        {
            Code = code,
            Name = manager.Nome,
            U_Email = manager.Email,
            U_Telefono = manager.Telefono,
            U_Esperienza = manager.Esperienza,
            U_ProgettiAttivi = manager.ProgettiAttivi ?? 0,
            U_Certificazioni = SerializeList(manager.Certificazioni)
        };
    }

    private object MapSquadraInstallazioneToSap(SquadraInstallazione squadra)
    {
        var code = string.IsNullOrWhiteSpace(squadra.Id) ? Guid.NewGuid().ToString("N") : squadra.Id;
        return new
        {
            Code = code,
            Name = squadra.Nome,
            U_Tipo = squadra.Tipo,
            U_Contatto = squadra.Contatto,
            U_Disponibilita = squadra.Disponibilita ? "Y" : "N",
            U_NumeroMembri = squadra.NumeroMembri ?? 0,
            U_Competenze = SerializeList(squadra.Competenze)
        };
    }

    private object MapProdottoMasterToSap(ProdottoMaster prodotto)
    {
        var code = string.IsNullOrWhiteSpace(prodotto.Id) ? Guid.NewGuid().ToString("N") : prodotto.Id;
        return new
        {
            Code = code,
            Name = prodotto.Nome,
            U_Categoria = prodotto.Categoria,
            U_UnitaMisura = prodotto.UnitaMisura,
            U_CodiceSAP = prodotto.CodiceSAP,
            U_Descrizione = prodotto.Descrizione,
            U_Varianti = SerializeList(prodotto.VariantiDisponibili)
        };
    }

    private static string? SerializeList(IEnumerable<string>? items)
    {
        if (items == null) return null;
        var cleaned = items
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim());
        var result = string.Join(',', cleaned);
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }
}

