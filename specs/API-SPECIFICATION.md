# API Specification - ADOTTA Projects Management System

## Descrizione Generale

Questo documento descrive le API necessarie per collegare la WebApp Angular al backend C# .NET. Le API sono suddivise in due categorie principali:
- **Project APIs**: Gestione dei progetti
- **Lookup APIs**: Gestione delle tabelle di supporto (master data)

---

## Base URL

```
http://localhost:5000/api
```

**SAP Business One Service Layer Base URL:**
```
https://{server}:50000/b1s/v1
```

---

## Autenticazione

### Autenticazione SAP Business One Service Layer

L'applicazione utilizza l'autenticazione nativa del Service Layer di SAP Business One. Il token di sessione viene gestito dal Service Layer e restituito all'API.

#### Login Endpoint

**Endpoint:** `POST /Login`

**Request Body:**
```json
{
  "CompanyDB": "SBODEMOUS",
  "UserName": "manager",
  "Password": "Password1",
  "Language": "en"
}
```

**Response:** `200 OK`

```json
{
  "odata.metadata": "https://{server}:50000/b1s/v1/$metadata#B1Sessions/@Element",
  "SessionId": "CA4B81FF-5DD9-4916-9FBF-DD0F4CFC9A72",
  "Version": "10.00.140",
  "SessionTimeout": 30
}
```

**Headers per le richieste successive:**
```
Cookie: B1SESSION={SessionId}
```

#### Logout Endpoint

**Endpoint:** `POST /Logout`

**Headers:**
```
Cookie: B1SESSION={SessionId}
```

**Response:** `204 No Content`

#### Implementazione nell'API .NET

L'API deve propagare il token di sessione di SAP alle chiamate successive:

```csharp
// Ottieni il token dal request header
var sessionId = Request.Headers["X-SAP-Session-Id"];

// Utilizza il token per chiamate al Service Layer
httpClient.DefaultRequestHeaders.Add("Cookie", $"B1SESSION={sessionId}");
```

> **Aggiornamento 2025-11**  
> Il token di sessione SAP viene ora gestito dall'API e serializzato nei JWT rilasciati al client.  
> Il frontend non deve più passare `X-SAP-Session-Id`; tutte le chiamate protette devono inviare `Authorization: Bearer {token}`.

**Flusso Autenticazione (revisione 2025-11):**
1. Frontend invia email/password all'endpoint `POST /api/auth/login`
2. L'API effettua il login tecnico sul Service Layer e verifica l'utente nella tabella `AX_ADT_USERS`
3. Viene generato un JWT che include le informazioni utente e la sessione SAP
4. Il frontend salva il JWT e lo invia negli header `Authorization` per ogni richiesta protetta
5. L'API estrae il `SessionId` dal token e lo utilizza verso il Service Layer
6. `POST /api/auth/logout` invalida la sessione SAP e il JWT lato client

---

## 0. INTEGRAZIONE SAP BUSINESS ONE

### 0.1 Architettura Integrazione

L'API .NET funge da **adapter/intermediario** tra la WebApp Angular e il **SAP Business One Service Layer**:

```
Angular WebApp → API .NET → SAP Service Layer → SAP Database
```

### 0.2 Architettura Dati in SAP Business One

#### Progetti - UDO (User Defined Object)

L'anagrafica dei progetti è implementata come **UDO** in SAP Business One.

**Nome UDO:** `ADOTTA_PROJECTS` (configurabile)

**Tabella SQL sottostante:** `@AOPROJECT` (User Defined Table)

**Campi UDO:**
```sql
-- Tabella principale: @AOPROJECT
Code           NVARCHAR(30)    -- NumeroProgetto (Primary Key)
Name           NVARCHAR(100)   -- NomeProgetto
U_Cliente      NVARCHAR(50)    -- Codice Cliente (collegamento a BP)
U_Citta        NVARCHAR(50)    -- Città
U_Stato        NVARCHAR(50)    -- Stato
U_TeamTecnico  NVARCHAR(50)    -- Team Tecnico
U_TeamAPL      NVARCHAR(50)    -- Team APL
U_Sales        NVARCHAR(50)    -- Sales
U_ProjectManager NVARCHAR(50)  -- Project Manager
U_TeamInstallazione NVARCHAR(50) -- Team Installazione
U_DataCreazione      DATETIME   -- Data Creazione
U_DataInizioInstall  DATETIME   -- Data Inizio Installazione
U_DataFineInstall    DATETIME   -- Data Fine Installazione
U_VersioneWIC         NVARCHAR(20)  -- Versione WIC
U_UltimaModifica      DATETIME
U_StatoProgetto       NVARCHAR(50)  -- Stato (ON_GOING, CRITICAL, ecc.)
U_ValoreProgetto      DECIMAL(19,6)  -- Valore Progetto
U_MarginePrevisto      DECIMAL(19,6) -- Margine Previsto (%)
U_CostiSostenuti       DECIMAL(19,6) -- Costi Sostenuti
U_Note                 NVARCHAR(254) -- Note
U_CodiceSAP            NVARCHAR(50)  -- Codice SAP
U_IsInRitardo          VARCHAR(1)    -- Flag Ritardo
```

**Caratteristiche UDO:**
- Form personalizzabile in SAP
- Possibilità di configurare validation rules
- Integrazione con workflow SAP
- Tracciabilità modifiche nativa
- Collegamento con Business Partner

#### Livelli Progetto - User Defined Table

I livelli del progetto sono memorizzati in una **User Defined Table**.

**Nome Tabella:** `@AOPROJLVL` (User Defined Table)

**Campi:**
```sql
Code     NVARCHAR(30)   -- ID Livello (Primary Key)
Name     NVARCHAR(100)  -- Nome Livello
U_Parent NVARCHAR(30)   -- Numero Progetto (FK a @AOPROJECT)
U_Ordine INT            -- Ordine
U_Descrizione NVARCHAR(254) -- Descrizione
U_DataInizio  DATETIME  -- Data Inizio Installazione
U_DataFine    DATETIME  -- Data Fine Installazione
U_DataCaricamento DATETIME -- Data Caricamento
```

#### Prodotti Progetto - User Defined Table

I prodotti del progetto sono memorizzati in una **User Defined Table**.

**Nome Tabella:** `@AOPROPRD` (User Defined Table)

**Campi:**
```sql
Code           NVARCHAR(30)   -- ID Prodotto (Primary Key)
Name           NVARCHAR(200)  -- Descrizione Prodotto
U_Parent       NVARCHAR(30)   -- Numero Progetto (FK a @AOPROJECT)
U_TipoProdotto NVARCHAR(50)   -- Tipo Prodotto (Metafora/Wallen/Armonica)
U_Variante     NVARCHAR(100)  -- Variante
U_QMq          DECIMAL(19,6)  -- Quantità mq
U_QFt          DECIMAL(19,6)  -- Quantità ft
U_LivelloId    NVARCHAR(30)   -- FK a Livello (opzionale)
```

#### Storico Modifiche - User Defined Table

Lo storico delle modifiche è memorizzato in una **User Defined Table**.

**Nome Tabella:** `@AOPROHIST` (User Defined Table)

**Campi:**
```sql
Code            NVARCHAR(30)   -- ID Modifica (Primary Key)
Name            NVARCHAR(100)  -- Campo Modificato
U_Parent        NVARCHAR(30)   -- Numero Progetto (FK a @AOPROJECT)
U_DataModifica  DATETIME       -- Data Modifica
U_UtenteModifica NVARCHAR(50)  -- Utente Modifica
U_CampoModificato NVARCHAR(100) -- Campo Modificato
U_ValorePrecedente NVARCHAR(500) -- Valore Precedente
U_NuovoValore     NVARCHAR(500)  -- Nuovo Valore
U_VersioneWIC     NVARCHAR(20)   -- Versione WIC
```

### 0.3 Anagrafica Clienti - Business Partner (BP)

Gli oggetti **Cliente** sono mappati ai **Business Partner** standard di SAP Business One.

**Tabella SAP:** `OCRD` (Business Partner Master Data)

**Collegamento tra Progetto e BP:**
```sql
-- Nel campo U_Cliente dell'UDO @AOPROJECT
-- si memorizza il CardCode del Business Partner

@AOPROJECT.U_Cliente → OCRD.CardCode
```

**Endpoint SAP per Business Partner:**

```
GET /BusinessPartners                    -- Get all BP
GET /BusinessPartners('{CardCode}')      -- Get BP by code
POST /BusinessPartners                   -- Create BP
PATCH /BusinessPartners('{CardCode}')    -- Update BP
DELETE /BusinessPartners('{CardCode}')   -- Delete BP
```

**Campi Business Partner rilevanti:**
```json
{
  "CardCode": "C001",
  "CardName": "TechCorp Italia",
  "CardType": "C",  // C=Customer, S=Supplier
  "Phone1": "+39 02 1234567",
  "EmailAddress": "info@techcorp.it",
  "FederalTaxID": "IT12345678901",
  "ValidFor": "Y",
  "BPAddresses": [
    {
      "AddressName": "Sede Principale",
      "Street": "Via Roma 123",
      "City": "Milano",
      "Country": "IT",
      "ZipCode": "20100"
    }
  ]
}
```

### 0.4 Tabelle di Supporto - User Defined Tables

Tutte le tabelle di supporto (Lookup) sono implementate come **User Defined Tables** semplici in SAP:

1. **Stati** - Tabella: `@AOSTATI`
2. **Città** - Tabella: `@AOCITTA`
3. **Team Tecnici** - Tabella: `@AOTEAMTECH`
4. **Team APL** - Tabella: `@AOTEAMAPL`
5. **Sales** - Tabella: `@AOSALES`
6. **Project Managers** - Tabella: `@AOPMGR`
7. **Squadre Installazione** - Tabella: `@AOSQUADRA`
8. **Prodotti Master** - Tabella: `@AOPRODMAST`

**Struttura tipica User Defined Table:**
```sql
-- Esempio: @AOTEAMTECH (Team Tecnici)
Code      NVARCHAR(30)    -- ID (Primary Key)
Name      NVARCHAR(100)   -- Nome Team
U_Email   NVARCHAR(100)   -- Email
U_Telefono NVARCHAR(50)   -- Telefono
U_Specializzazione NVARCHAR(100) -- Specializzazione
U_Disponibilita  VARCHAR(1)      -- Flag Disponibilità
```

### 0.5 Endpoint Service Layer per Dati

#### Read UDO (Progetti)

```
GET /UserTablesMD('{TableName}')           -- Get table metadata
GET /{TableName}                           -- Get all records
GET /{TableName}('{Code}')                 -- Get single record
```

Esempio:
```
GET /@AOPROJECT                            -- Get all projects
GET /@AOPROJECT('PRJ-2024-001')            -- Get specific project
```

#### Create/Update/Delete UDO

```
POST /{TableName}                          -- Create record
PATCH /{TableName}('{Code}')              -- Update record
DELETE /{TableName}('{Code}')              -- Delete record
```

**Formato Request per UDO:**
```json
{
  "Code": "PRJ-2024-001",
  "Name": "Installazione HVAC Uffici Milano",
  "U_Cliente": "C001",
  "U_Citta": "Milano",
  "U_Stato": "IT",
  "U_TeamTecnico": "TT001",
  "U_DataCreazione": "2024-01-15T00:00:00",
  "U_StatoProgetto": "ON_GOING",
  "U_ValoreProgetto": 500000,
  "U_MarginePrevisto": 25.0,
  "U_CostiSostenuti": 375000
}
```

#### Child Tables (Livelli, Prodotti, Storico)

```
GET /@AOPROJECT('PRJ-2024-001')/AOPROJLVL$NavigationProperty
GET /@AOPROJECT('PRJ-2024-001')/AOPROPRD$NavigationProperty
```

**Oppure direttamente:**
```
GET /@AOPROJLVL?$filter=U_Parent eq 'PRJ-2024-001'
GET /@AOPROPRD?$filter=U_Parent eq 'PRJ-2024-001'
```

### 0.6 Mapping Dati API .NET ↔ SAP

Il backend .NET deve implementare un **Service Layer** che:

1. **Riceve richieste REST** dalla WebApp Angular
2. **Converta** i modelli Angular/DTO in formati SAP
3. **Chiami** il Service Layer di SAP tramite HTTP Client
4. **Riceva** le risposte SAP in formato OData
5. **Converta** i dati SAP in DTO/Model per Angular
6. **Gestisca** errori e validazioni
7. **Propaghi** il token di sessione SAP

**Esempio Mapping Progetto:**

```csharp
// DTO Angular → SAP UDO
public class ProjectToSapMapper
{
    public object MapProjectToSapUDO(ProjectDto dto)
    {
        return new
        {
            Code = dto.NumeroProgetto,
            Name = dto.NomeProgetto,
            U_Cliente = dto.CodiceClienteSAP,  // Dal BP
            U_Citta = dto.Citta,
            U_Stato = dto.Stato,
            U_TeamTecnico = dto.TeamTecnico,
            U_TeamAPL = dto.TeamAPL,
            U_Sales = dto.Sales,
            U_ProjectManager = dto.ProjectManager,
            U_TeamInstallazione = dto.TeamInstallazione,
            U_DataCreazione = dto.DataCreazione.ToString("yyyy-MM-ddTHH:mm:ss"),
            U_DataInizioInstall = dto.DataInizioInstallazione?.ToString("yyyy-MM-ddTHH:mm:ss"),
            U_DataFineInstall = dto.DataFineInstallazione?.ToString("yyyy-MM-ddTHH:mm:ss"),
            U_VersioneWIC = dto.VersioneWIC,
            U_UltimaModifica = dto.UltimaModifica?.ToString("yyyy-MM-ddTHH:mm:ss"),
            U_StatoProgetto = dto.StatoProgetto.ToString(),
            U_ValoreProgetto = dto.ValoreProgetto,
            U_MarginePrevisto = dto.MarginePrevisto,
            U_CostiSostenuti = dto.CostiSostenuti,
            U_Note = dto.Note,
            U_CodiceSAP = dto.CodiceSAP,
            U_IsInRitardo = dto.IsInRitardo ? "Y" : "N"
        };
    }
}
```

### 0.7 Configurazione Backend .NET

#### Configurazione SAP Service Layer

```csharp
// appsettings.json
{
  "SAPSettings": {
    "ServiceLayerUrl": "https://sap-server:50000/b1s/v1",
    "CompanyDB": "SBODEMOUS",
    "UserName": "manager",
    "DefaultLanguage": "en",
    "SessionTimeout": 30
  }
}

// Dependency Injection
services.Configure<SAPSettings>(configuration.GetSection("SAPSettings"));
services.AddScoped<ISAPServiceLayerClient, SAPServiceLayerClient>();
services.AddHttpClient<ISAPServiceLayerClient>();
```

#### Implementazione Service Client

```csharp
public interface ISAPServiceLayerClient
{
    Task<string> LoginAsync(LoginRequest request);
    Task LogoutAsync(string sessionId);
    Task<List<T>> GetRecordsAsync<T>(string tableName, string sessionId);
    Task<T> GetRecordAsync<T>(string tableName, string code, string sessionId);
    Task<T> CreateRecordAsync<T>(string tableName, T record, string sessionId);
    Task UpdateRecordAsync<T>(string tableName, string code, T record, string sessionId);
    Task DeleteRecordAsync(string tableName, string code, string sessionId);
}
```

### 0.8 Gestione Errori SAP

Il Service Layer di SAP restituisce errori in formato OData:

```json
{
  "error": {
    "code": "-5002",
    "message": {
      "lang": "en-us",
      "value": "Record not found"
    }
  }
}
```

**Codici errore comuni:**
- `-5002`: Record not found
- `-5007`: Duplicate key
- `-5008`: Validation error
- `-5009`: Record in use
- `-5010`: Permission denied

### 0.9 Note Implementative Importanti

1. **Session Management**: Gestire il timeout delle sessioni SAP (default 30 minuti)
2. **Connection Pooling**: Riutilizzare connessioni HTTP al Service Layer
3. **Retry Logic**: Implementare retry per chiamate fallite (network issues)
4. **Caching**: Cache dei dati lookup (team, sales, etc.) per ridurre chiamate SAP
5. **Async/Await**: Tutte le chiamate a SAP devono essere asincrone
6. **Logging**: Loggare tutte le chiamate SAP per debugging e audit
7. **Validation**: Validare i dati prima di inviarli a SAP
8. **Transaction Support**: Utilizzare batch requests per operazioni multiple

---

## 1. PROJECT API

### 1.1 Get All Projects

Recupera l'elenco completo dei progetti.

**Endpoint:** `GET /api/projects`

**Response:** `200 OK`

```json
[
  {
    "numeroProgetto": "string",
    "cliente": "string",
    "nomeProgetto": "string",
    "citta": "string",
    "stato": "string",
    "teamTecnico": "string",
    "teamAPL": "string",
    "sales": "string",
    "projectManager": "string",
    "teamInstallazione": "string",
    "dataCreazione": "2024-01-15T00:00:00",
    "dataInizioInstallazione": "2024-02-01T00:00:00",
    "dataFineInstallazione": "2024-03-20T00:00:00",
    "versioneWIC": "string",
    "ultimaModifica": "2024-01-20T00:00:00",
    "statoProgetto": "UPCOMING",
    "isInRitardo": false,
    "livelli": [
      {
        "id": 0,
        "progettoId": 0,
        "nome": "string",
        "ordine": 0,
        "descrizione": "string",
        "dataInizioInstallazione": "2024-01-01T00:00:00",
        "dataFineInstallazione": "2024-01-15T00:00:00",
        "dataCaricamento": "2024-01-01T00:00:00"
      }
    ],
    "prodotti": [
      {
        "id": 0,
        "progettoId": 0,
        "tipoProdotto": "string",
        "variante": "string",
        "qMq": 0.0,
        "qFt": 0.0
      }
    ]
  }
]
```

**Campi di ProjectStatus (enum):**
- `UPCOMING` - Progetto in arrivo
- `ON_GOING` - Progetto in corso
- `CRITICAL` - Progetto critico
- `HOLD_ON` - Progetto in attesa
- `RUSH` - Progetto urgente
- `TO_CHECK` - Da verificare
- `PUSHED_OUT` - Rinviato
- `ON_BID` - In offerta

---

### 1.2 Get Project by ID

Recupera un singolo progetto per numero progetto.

**Endpoint:** `GET /api/projects/{numeroProgetto}`

**Parameters:**
- `numeroProgetto` (string, required): Numero identificativo del progetto

**Response:** `200 OK`

```json
{
  "numeroProgetto": "PRJ-2024-001",
  "cliente": "TechCorp Italia",
  "nomeProgetto": "Installazione HVAC Uffici Milano",
  "citta": "Milano",
  "stato": "IT",
  "teamTecnico": "Team Alpha",
  "teamAPL": "APL Team 1",
  "sales": "Giuseppe Verdi",
  "projectManager": "Mario Rossi",
  "teamInstallazione": "Install Team A",
  "dataCreazione": "2024-01-15T00:00:00",
  "dataInizioInstallazione": "2024-02-01T00:00:00",
  "dataFineInstallazione": "2024-03-20T00:00:00",
  "versioneWIC": "WIC-1.0",
  "ultimaModifica": "2024-01-20T00:00:00",
  "statoProgetto": "ON_GOING",
  "isInRitardo": false,
  "livelli": [],
  "prodotti": []
}
```

**Error Responses:**
- `404 Not Found`: Progetto non trovato

---

### 1.3 Create Project

Crea un nuovo progetto.

**Endpoint:** `POST /api/projects`

**Request Body:**

```json
{
  "numeroProgetto": "PRJ-2024-001",
  "cliente": "TechCorp Italia",
  "nomeProgetto": "Installazione HVAC Uffici Milano",
  "citta": "Milano",
  "stato": "IT",
  "teamTecnico": "Team Alpha",
  "teamAPL": "APL Team 1",
  "sales": "Giuseppe Verdi",
  "projectManager": "Mario Rossi",
  "teamInstallazione": "Install Team A",
  "dataCreazione": "2024-01-15T00:00:00",
  "dataInizioInstallazione": "2024-02-01T00:00:00",
  "dataFineInstallazione": "2024-03-20T00:00:00",
  "versioneWIC": "WIC-1.0",
  "statoProgetto": "ON_GOING",
  "note": "Progetto di installazione sistemi HVAC"
}
```

**Validation Rules:**
- `numeroProgetto`: Required, max 50 chars, unique
- `cliente`: Required, max 100 chars
- `nomeProgetto`: Required, max 200 chars
- `dataCreazione`: Required
- `statoProgetto`: Required, must be valid enum value

**Response:** `201 Created`

```json
{
  "numeroProgetto": "PRJ-2024-001",
  "cliente": "TechCorp Italia",
  "nomeProgetto": "Installazione HVAC Uffici Milano",
  ...
}
```

**Error Responses:**
- `400 Bad Request`: Dati di input non validi
- `409 Conflict`: Numero progetto già esistente

---

### 1.4 Update Project

Aggiorna un progetto esistente.

**Endpoint:** `PUT /api/projects/{numeroProgetto}`

**Parameters:**
- `numeroProgetto` (string, required): Numero identificativo del progetto

**Request Body:** Same as Create Project

**Response:** `200 OK`

**Error Responses:**
- `404 Not Found`: Progetto non trovato
- `400 Bad Request`: Dati di input non validi

---

### 1.5 Delete Project

Elimina un progetto.

**Endpoint:** `DELETE /api/projects/{numeroProgetto}`

**Parameters:**
- `numeroProgetto` (string, required): Numero identificativo del progetto

**Response:** `204 No Content`

**Error Responses:**
- `404 Not Found`: Progetto non trovato
- `409 Conflict`: Progetto non può essere eliminato (es. ha livelli o prodotti associati)

---

## 2. LIVELI PROGETTO API

### 2.1 Get Livelli by Project

Recupera tutti i livelli di un progetto.

**Endpoint:** `GET /api/projects/{numeroProgetto}/livelli`

**Response:** `200 OK`

```json
[
  {
    "id": 1,
    "progettoId": 1,
    "nome": "Livello 1 - Piano Terra",
    "ordine": 1,
    "descrizione": "Installazione impianti HVAC piano terra",
    "dataInizioInstallazione": "2024-02-01T00:00:00",
    "dataFineInstallazione": "2024-02-15T00:00:00",
    "dataCaricamento": "2024-01-15T00:00:00"
  }
]
```

---

### 2.2 Create Livello

Aggiunge un nuovo livello al progetto.

**Endpoint:** `POST /api/projects/{projectId}/livelli`

**Parameters:**
- `projectId` (int, required): ID del progetto

**Request Body:**

```json
{
  "progettoId": 1,
  "nome": "Livello 1 - Piano Terra",
  "ordine": 1,
  "descrizione": "Installazione impianti HVAC piano terra",
  "dataInizioInstallazione": "2024-02-01T00:00:00",
  "dataFineInstallazione": "2024-02-15T00:00:00"
}
```

**Validation Rules:**
- `progettoId`: Required
- `nome`: Required, max 200 chars
- `ordine`: Required, >= 1
- `dataFineInstallazione`: Must be after `dataInizioInstallazione`

**Response:** `201 Created`

---

### 2.3 Update Livello

Aggiorna un livello esistente.

**Endpoint:** `PUT /api/projects/{projectId}/livelli/{livelloId}`

**Parameters:**
- `projectId` (int, required)
- `livelloId` (int, required)

**Request Body:** Same as Create Livello

**Response:** `200 OK`

---

### 2.4 Delete Livello

Elimina un livello dal progetto.

**Endpoint:** `DELETE /api/projects/{projectId}/livelli/{livelloId}`

**Parameters:**
- `projectId` (int, required)
- `livelloId` (int, required)

**Response:** `204 No Content`

---

## 3. PRODOTTI PROGETTO API

### 3.1 Get Prodotti by Project

Recupera tutti i prodotti di un progetto.

**Endpoint:** `GET /api/projects/{numeroProgetto}/prodotti`

**Response:** `200 OK`

```json
[
  {
    "id": 1,
    "progettoId": 1,
    "tipoProdotto": "Metafora",
    "variante": "Standard",
    "qMq": 150.5,
    "qFt": 1620.0
  }
]
```

---

### 3.2 Create Prodotto

Aggiunge un nuovo prodotto al progetto.

**Endpoint:** `POST /api/projects/{projectId}/prodotti`

**Parameters:**
- `projectId` (int, required): ID del progetto

**Request Body:**

```json
{
  "progettoId": 1,
  "tipoProdotto": "Metafora",
  "variante": "Standard",
  "qMq": 150.5,
  "qFt": 1620.0
}
```

**Validation Rules:**
- `progettoId`: Required
- `tipoProdotto`: Required, max 100 chars (Metafora/Wallen/Armonica)
- `variante`: Required, max 100 chars
- `qMq`: Required, >= 0
- `qFt`: Required, >= 0

**Response:** `201 Created`

---

### 3.3 Update Prodotto

Aggiorna un prodotto esistente.

**Endpoint:** `PUT /api/projects/{projectId}/prodotti/{prodottoId}`

**Parameters:**
- `projectId` (int, required)
- `prodottoId` (int, required)

**Request Body:** Same as Create Prodotto

**Response:** `200 OK`

---

### 3.4 Delete Prodotto

Elimina un prodotto dal progetto.

**Endpoint:** `DELETE /api/projects/{projectId}/prodotti/{prodottoId}`

**Parameters:**
- `projectId` (int, required)
- `prodottoId` (int, required)

**Response:** `204 No Content`

---

## 4. STORICO MODIFICHE API

### 4.1 Get Storico Modifiche

Recupera lo storico delle modifiche di un progetto.

**Endpoint:** `GET /api/projects/{numeroProgetto}/storico`

**Response:** `200 OK`

```json
[
  {
    "id": 1,
    "progettoId": 1,
    "dataModifica": "2024-01-20T14:30:00",
    "utenteModifica": "Mario Rossi",
    "campoModificato": "Stato Progetto",
    "valorePrecedente": "In Corso",
    "nuovoValore": "Completato"
  }
]
```

---

### 4.2 Create WIC Snapshot

Crea uno snapshot WIC del progetto (registra tutte le modifiche correnti).

**Endpoint:** `POST /api/projects/{projectId}/wic-snapshot`

**Parameters:**
- `projectId` (int, required)

**Request Body:** `{}`

**Response:** `200 OK`

```json
[
  {
    "id": 1,
    "progettoId": 1,
    "dataModifica": "2024-01-20T14:30:00",
    "utenteModifica": "Mario Rossi",
    "campoModificato": "Stato Progetto",
    "valorePrecedente": "In Corso",
    "nuovoValore": "Completato"
  }
]
```

---

## 5. MESSAGGI E CHANGE LOG API

### 5.1 Messaggi Progetto

#### 5.1.1 Get Messaggi Progetto

Recupera tutti i messaggi associati a un progetto.

**Endpoint:** `GET /api/projects/{numeroProgetto}/messaggi`

**Response:** `200 OK`

```json
[
  {
    "id": 1,
    "progettoId": 1,
    "data": "2024-01-20T14:30:00",
    "utente": "Mario Rossi",
    "messaggio": "Installazione completata con successo",
    "tipo": "success",
    "allegato": null
  }
]
```

**Tipi messaggio:**
- `info`: Informazione generica
- `success`: Operazione completata con successo
- `warning`: Avviso
- `error`: Errore

#### 5.1.2 Create Messaggio Progetto

Aggiunge un nuovo messaggio al progetto.

**Endpoint:** `POST /api/projects/{projectId}/messaggi`

**Parameters:**
- `projectId` (int, required)

**Request Body:**
```json
{
  "progettoId": 1,
  "data": "2024-01-20T14:30:00",
  "utente": "Mario Rossi",
  "messaggio": "Installazione completata con successo",
  "tipo": "success",
  "allegato": null
}
```

**Response:** `201 Created`

#### 5.1.3 Update Messaggio Progetto

Aggiorna un messaggio esistente.

**Endpoint:** `PUT /api/projects/{projectId}/messaggi/{messaggioId}`

**Parameters:**
- `projectId` (int, required)
- `messaggioId` (int, required)

**Response:** `200 OK`

#### 5.1.4 Delete Messaggio Progetto

Elimina un messaggio dal progetto.

**Endpoint:** `DELETE /api/projects/{projectId}/messaggi/{messaggioId}`

**Parameters:**
- `projectId` (int, required)
- `messaggioId` (int, required)

**Response:** `204 No Content`

---

### 5.2 Change Log Progetto

#### 5.2.1 Get Change Log Progetto

Recupera il log completo delle modifiche di un progetto.

**Endpoint:** `GET /api/projects/{numeroProgetto}/changelog`

**Response:** `200 OK`

```json
[
  {
    "id": 1,
    "progettoId": 1,
    "data": "2024-01-20T14:30:00",
    "utente": "Mario Rossi",
    "azione": "updated",
    "descrizione": "Stato Progetto: \"ON_GOING\" → \"CRITICAL\"",
    "dettagli": {
      "campo": "Stato Progetto",
      "vecchioValore": "ON_GOING",
      "nuovoValore": "CRITICAL"
    }
  }
]
```

**Azioni possibili:**
- `created`: Progetto creato
- `updated`: Progetto aggiornato
- `deleted`: Progetto eliminato
- `status_changed`: Stato progetto modificato
- `message_added`: Messaggio aggiunto
- `level_added`: Livello aggiunto
- `product_added`: Prodotto aggiunto

#### 5.2.2 Create Change Log Entry

Crea una nuova voce nel change log (normalmente gestito automaticamente).

**Endpoint:** `POST /api/projects/{projectId}/changelog`

**Request Body:**
```json
{
  "progettoId": 1,
  "data": "2024-01-20T14:30:00",
  "utente": "Mario Rossi",
  "azione": "updated",
  "descrizione": "Modifica campo specifico",
  "dettagli": {
    "campo": "Nome Progetto",
    "vecchioValore": "Vecchio Nome",
    "nuovoValore": "Nuovo Nome"
  }
}
```

**Response:** `201 Created`

**Note:** Il change log viene generalmente popolato automaticamente dal sistema quando si verificano modifiche ai progetti.

---

## 6. TIMESHEET API

Il sistema Timesheet permette di registrare le ore lavorate sui progetti per la rendicontazione.

### 6.1 Get All Timesheet Entries

Recupera tutte le rendicontazioni di ore lavorate.

**Endpoint:** `GET /api/timesheet`

**Response:** `200 OK`

```json
[
  {
    "id": 1,
    "progettoId": "PRJ-2024-001",
    "numeroProgetto": "PRJ-2024-001",
    "nomeProgetto": "Installazione HVAC Uffici Milano",
    "cliente": "TechCorp Italia",
    "dataRendicontazione": "2024-01-20T00:00:00",
    "oreLavorate": 8.0,
    "note": "Installazione impianti primo piano",
    "utente": "Mario Rossi",
    "dataCreazione": "2024-01-20T08:00:00",
    "ultimaModifica": "2024-01-20T08:00:00"
  }
]
```

---

### 6.2 Get Timesheet Entry by ID

Recupera una singola rendicontazione.

**Endpoint:** `GET /api/timesheet/{id}`

**Parameters:**
- `id` (int, required): ID della rendicontazione

**Response:** `200 OK`

---

### 6.3 Create Timesheet Entry

Crea una nuova rendicontazione di ore lavorate.

**Endpoint:** `POST /api/timesheet`

**Request Body:**
```json
{
  "progettoId": "PRJ-2024-001",
  "dataRendicontazione": "2024-01-20T00:00:00",
  "oreLavorate": 8.0,
  "note": "Installazione impianti primo piano",
  "utente": "Mario Rossi"
}
```

**Validation Rules:**
- `progettoId`: Required
- `dataRendicontazione`: Required, date format
- `oreLavorate`: Required, >= 0
- `utente`: Required

**Response:** `201 Created`

---

### 6.4 Update Timesheet Entry

Aggiorna una rendicontazione esistente.

**Endpoint:** `PUT /api/timesheet/{id}`

**Parameters:**
- `id` (int, required)

**Request Body:** Same as Create

**Response:** `200 OK`

---

### 6.5 Delete Timesheet Entry

Elimina una rendicontazione.

**Endpoint:** `DELETE /api/timesheet/{id}`

**Parameters:**
- `id` (int, required)

**Response:** `204 No Content`

---

### 6.6 Get Timesheet by Project

Recupera tutte le rendicontazioni per un progetto specifico.

**Endpoint:** `GET /api/timesheet/project/{numeroProgetto}`

**Parameters:**
- `numeroProgetto` (string, required)

**Response:** `200 OK`

Array di rendicontazioni filtrate per progetto.

---

### 6.7 Get Timesheet Overview

Recupera una panoramica delle rendicontazioni con statistiche.

**Endpoint:** `GET /api/timesheet/overview`

**Query Parameters:**
- `fromDate` (date, optional): Data inizio filtro
- `toDate` (date, optional): Data fine filtro
- `utente` (string, optional): Filtro per utente

**Response:** `200 OK`

```json
{
  "timesheets": [
    {
      "numeroProgetto": "PRJ-2024-001",
      "nomeProgetto": "Installazione HVAC Uffici Milano",
      "cliente": "TechCorp Italia",
      "totaleOre": 40.0,
      "numeroRendicontazioni": 5,
      "ultimaRendicontazione": "2024-01-24T00:00:00",
      "rendicontazioni": [...]
    }
  ],
  "summary": {
    "totaleOre": 120.0,
    "totaleRendicontazioni": 15,
    "progettiRendicontati": 8,
    "mediaOrePerProgetto": 15.0
  }
}
```

---

### 6.8 Get Timesheet Summary

Recupera statistiche riassuntive delle rendicontazioni.

**Endpoint:** `GET /api/timesheet/summary`

**Query Parameters:**
- `fromDate` (date, optional)
- `toDate` (date, optional)
- `utente` (string, optional)

**Response:** `200 OK`

```json
{
  "totaleOre": 120.0,
  "totaleRendicontazioni": 15,
  "progettiRendicontati": 8,
  "mediaOrePerProgetto": 15.0
}
```

---

### 6.9 Get Timesheet by User

Recupera tutte le rendicontazioni di un utente specifico.

**Endpoint:** `GET /api/timesheet/user/{utente}`

**Parameters:**
- `utente` (string, required): Nome utente

**Response:** `200 OK`

Array di rendicontazioni filtrate per utente.

---

## 7. RICERCA E FILTRI API

### 7.1 Search Projects

Cerca progetti per termine di ricerca.

**Endpoint:** `GET /api/projects/search?q={searchTerm}`

**Parameters:**
- `q` (string, required): Termine di ricerca

**Response:** `200 OK`

Restituisce l'elenco dei progetti che matchano il termine di ricerca (nome progetto, cliente, numero progetto).

---

### 7.2 Filter Projects

Filtra progetti per parametri multipli.

**Endpoint:** `POST /api/projects/filter`

**Request Body:**

```json
{
  "stato": "ON_GOING",
  "cliente": "TechCorp",
  "projectManager": "Mario Rossi"
}
```

**Response:** `200 OK`

Array di progetti che soddisfano i criteri di filtro.

---

## 8. STATISTICHE E KPI API

### 8.1 Get Project Stats

Recupera statistiche generali sui progetti.

**Endpoint:** `GET /api/projects/stats`

**Response:** `200 OK`

```json
{
  "progettiAttivi": 15,
  "valorePortfolio": 5000000.00,
  "installazioniMese": 5,
  "progettiRitardo": 3
}
```

---

### 8.2 Get Projects by Status

Recupera la distribuzione dei progetti per stato.

**Endpoint:** `GET /api/projects/stats/by-status`

**Response:** `200 OK`

```json
[
  {
    "stato": "ON_GOING",
    "count": 10
  },
  {
    "stato": "CRITICAL",
    "count": 3
  },
  {
    "stato": "HOLD_ON",
    "count": 5
  }
]
```

---

### 8.3 Get Projects by Month

Recupera la distribuzione dei progetti per mese.

**Endpoint:** `GET /api/projects/stats/by-month`

**Response:** `200 OK`

```json
[
  {
    "label": "Gen",
    "value": 5
  },
  {
    "label": "Feb",
    "value": 8
  },
  {
    "label": "Mar",
    "value": 12
  }
]
```

---

## 9. EXPORT API

### 9.1 Export Projects

Esporta i progetti in vari formati.

**Endpoint:** `POST /api/projects/export/{format}`

**Parameters:**
- `format` (string, required): Formato di esportazione (`excel`, `pdf`, `csv`)

**Request Body (optional):**

```json
{
  "filters": {
    "stato": "ON_GOING"
  }
}
```

**Response:** `200 OK`

**Content-Type:** 
- Excel: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- PDF: `application/pdf`
- CSV: `text/csv`

**Content-Disposition:** `attachment; filename="progetti_2024-01-15.xlsx"`

---

## 10. LOOKUP API

### 10.1 Clienti (Clients)

> **⚠️ IMPORTANTE**: I clienti sono mappati direttamente ai **Business Partner** standard di SAP Business One. 
> Il backend .NET deve recuperare i dati dalla tabella `OCRD` (Business Partners) di SAP, filtrare per tipo "C" (Customer),
> e restituire i dati in formato compatibile con l'interfaccia Angular.

#### 10.1.1 Get All Clienti

**Endpoint:** `GET /api/lookup/clienti`

**Backend Implementation:**
- Chiama SAP Service Layer: `GET /BusinessPartners?$filter=CardType eq 'C'`
- Mappa i dati SAP BP in formato Cliente per Angular

**Response:** `200 OK`

```json
[
  {
    "id": "C001",
    "cardCode": "C001",
    "nome": "TechCorp Italia",
    "email": "info@techcorp.it",
    "telefono": "+39 02 1234567",
    "partitaIVA": "IT12345678901",
    "contatto": "Mario Rossi",
    "indirizzoCompleto": "Via Roma 123, 20100 Milano",
    "citta": "Milano",
    "provincia": "MI",
    "cap": "20100",
    "stato": "IT",
    "note": "Cliente principale",
    "validFor": "Y",
    "addresses": [
      {
        "addressName": "Sede Principale",
        "street": "Via Roma 123",
        "city": "Milano",
        "country": "IT",
        "zipCode": "20100"
      }
    ]
  }
]
```

**Mapping SAP BP → Cliente:**
- `CardCode` → `cardCode`
- `CardName` → `nome`
- `EmailAddress` → `email`
- `Phone1` → `telefono`
- `FederalTaxID` → `partitaIVA`
- `Notes` → `note`
- `BPAddresses[0]` → `addresses[0]`, `indirizzoCompleto`

#### 10.1.2 Get Cliente by ID

**Endpoint:** `GET /api/lookup/clienti/{id}`

**Response:** `200 OK`

#### 10.1.3 Create Cliente

**Endpoint:** `POST /api/lookup/clienti`

**Request Body:**

```json
{
  "nome": "Nuovo Cliente",
  "email": "info@cliente.it",
  "telefono": "+39 02 1234567",
  "partitaIVA": "IT12345678901",
  "contatto": "Nome Contatto",
  "indirizzoCompleto": "Indirizzo completo",
  "note": "Note cliente"
}
```

**Validation Rules:**
- `nome`: Required, max 200 chars

#### 10.1.4 Update Cliente

**Endpoint:** `PUT /api/lookup/clienti/{id}`

#### 10.1.5 Delete Cliente

**Endpoint:** `DELETE /api/lookup/clienti/{id}`

#### 10.1.6 Search Cliente

**Endpoint:** `GET /api/lookup/clienti/search?q={searchTerm}`

---

### 10.2 Stati (States)

#### 10.2.1 Get All Stati

**Endpoint:** `GET /api/lookup/stati`

**Response:** `200 OK`

```json
[
  {
    "id": 1,
    "nome": "Lombardia",
    "codiceISO": "IT-LO",
    "continente": "Europa"
  }
]
```

#### 10.2.2 Get Stato by ID

**Endpoint:** `GET /api/lookup/stati/{id}`

---

### 10.3 Città (Cities)

#### 10.3.1 Get All Città

**Endpoint:** `GET /api/lookup/citta`

**Response:** `200 OK`

```json
[
  {
    "id": 1,
    "nome": "Milano",
    "statoId": 1,
    "cap": "20100",
    "provincia": "MI",
    "regione": "Lombardia"
  }
]
```

#### 10.3.2 Get Città by Stato

**Endpoint:** `GET /api/lookup/citta?statoId={statoId}`

#### 10.3.3 Get Città by ID

**Endpoint:** `GET /api/lookup/citta/{id}`

---

### 10.4 Team Tecnici

#### 10.4.1 Get All Team Tecnici

**Endpoint:** `GET /api/lookup/team-tecnici`

**Response:** `200 OK`

```json
[
  {
    "id": 1,
    "nome": "Team Elettrico Milano",
    "specializzazione": "Impianti Elettrici",
    "email": "elettrico.milano@adotta.it",
    "telefono": "+39 02 1111111",
    "disponibilita": true,
    "membri": ["Marco Rossi", "Paolo Bianchi", "Luca Verdi"]
  }
]
```

#### 10.4.2 Get Team Tecnico by ID

**Endpoint:** `GET /api/lookup/team-tecnici/{id}`

#### 10.4.3 Create Team Tecnico

**Endpoint:** `POST /api/lookup/team-tecnici`

#### 10.4.4 Update Team Tecnico

**Endpoint:** `PUT /api/lookup/team-tecnici/{id}`

#### 10.4.5 Delete Team Tecnico

**Endpoint:** `DELETE /api/lookup/team-tecnici/{id}`

---

### 10.5 Team APL

#### 10.5.1 Get All Team APL

**Endpoint:** `GET /api/lookup/team-apl`

**Response:** `200 OK`

```json
[
  {
    "id": 1,
    "nome": "Team APL Nord",
    "email": "apl.nord@adotta.it",
    "telefono": "+39 02 4444444",
    "area": "Nord Italia",
    "competenze": ["Progettazione HVAC", "Calcoli Termici", "Dimensionamento Impianti"]
  }
]
```

#### 10.5.2 Get Team APL by ID

**Endpoint:** `GET /api/lookup/team-apl/{id}`

#### 10.5.3 Create Team APL

**Endpoint:** `POST /api/lookup/team-apl`

#### 10.5.4 Update Team APL

**Endpoint:** `PUT /api/lookup/team-apl/{id}`

#### 10.5.5 Delete Team APL

**Endpoint:** `DELETE /api/lookup/team-apl/{id}`

---

### 10.6 Sales

#### 10.6.1 Get All Sales

**Endpoint:** `GET /api/lookup/sales`

**Response:** `200 OK`

```json
[
  {
    "id": 1,
    "nome": "Marco Vendite",
    "email": "marco.vendite@adotta.it",
    "telefono": "+39 02 7777777",
    "zona": "Nord Italia",
    "regioneDiCompetenza": "Lombardia, Piemonte, Veneto",
    "progettiGestiti": 15
  }
]
```

#### 10.6.2 Get Sales by ID

**Endpoint:** `GET /api/lookup/sales/{id}`

#### 10.6.3 Create Sales

**Endpoint:** `POST /api/lookup/sales`

#### 10.6.4 Update Sales

**Endpoint:** `PUT /api/lookup/sales/{id}`

#### 10.6.5 Delete Sales

**Endpoint:** `DELETE /api/lookup/sales/{id}`

---

### 10.7 Project Managers

#### 10.7.1 Get All Project Managers

**Endpoint:** `GET /api/lookup/project-managers`

**Response:** `200 OK`

```json
[
  {
    "id": 1,
    "nome": "Mario Rossi",
    "email": "mario.rossi@adotta.it",
    "telefono": "+39 02 1010101",
    "progettiAttivi": 8,
    "esperienza": "Senior",
    "certificazioni": ["PMP", "PRINCE2", "Agile"]
  }
]
```

#### 10.7.2 Get Project Manager by ID

**Endpoint:** `GET /api/lookup/project-managers/{id}`

#### 10.7.3 Create Project Manager

**Endpoint:** `POST /api/lookup/project-managers`

#### 10.7.4 Update Project Manager

**Endpoint:** `PUT /api/lookup/project-managers/{id}`

#### 10.7.5 Delete Project Manager

**Endpoint:** `DELETE /api/lookup/project-managers/{id}`

---

### 10.8 Squadre Installazione

#### 10.8.1 Get All Squadre Installazione

**Endpoint:** `GET /api/lookup/squadre-installazione`

**Response:** `200 OK`

```json
[
  {
    "id": 1,
    "nome": "Squadra Installazione Milano",
    "tipo": "HVAC",
    "contatto": "Marco Installatore",
    "disponibilita": true,
    "competenze": ["Installazione HVAC", "Manutenzione"],
    "numeroMembri": 4
  }
]
```

#### 10.8.2 Get Squadra Installazione by ID

**Endpoint:** `GET /api/lookup/squadre-installazione/{id}`

#### 10.8.3 Create Squadra Installazione

**Endpoint:** `POST /api/lookup/squadre-installazione`

#### 10.8.4 Update Squadra Installazione

**Endpoint:** `PUT /api/lookup/squadre-installazione/{id}`

#### 10.8.5 Delete Squadra Installazione

**Endpoint:** `DELETE /api/lookup/squadre-installazione/{id}`

---

### 10.9 Prodotti Master

#### 10.9.1 Get All Prodotti Master

**Endpoint:** `GET /api/lookup/prodotti-master`

**Response:** `200 OK`

```json
[
  {
    "id": 1,
    "nome": "Metafora Standard",
    "categoria": "Metafora",
    "unitaMisura": "pz",
    "codiceSAP": "META001",
    "descrizione": "Sistema Metafora standard per uffici",
    "variantiDisponibili": ["Bianco", "Grigio", "Nero"]
  }
]
```

#### 10.9.2 Get Prodotti Master by Categoria

**Endpoint:** `GET /api/lookup/prodotti-master?categoria={categoria}`

**Parameters:**
- `categoria`: Metafora / Wallen / Armonica

#### 10.9.3 Get Prodotto Master by ID

**Endpoint:** `GET /api/lookup/prodotti-master/{id}`

#### 10.9.4 Create Prodotto Master

**Endpoint:** `POST /api/lookup/prodotti-master`

#### 10.9.5 Update Prodotto Master

**Endpoint:** `PUT /api/lookup/prodotti-master/{id}`

#### 10.9.6 Delete Prodotto Master

**Endpoint:** `DELETE /api/lookup/prodotti-master/{id}`

---

## 11. Modelli Dati C# .NET

### 11.1 Project Model

```csharp
public class Project
{
    public string NumeroProgetto { get; set; }
    public string Cliente { get; set; }
    public string NomeProgetto { get; set; }
    public string Citta { get; set; }
    public string Stato { get; set; }
    public string? TeamTecnico { get; set; }
    public string? TeamAPL { get; set; }
    public string? Sales { get; set; }
    public string? ProjectManager { get; set; }
    public string? TeamInstallazione { get; set; }
    public DateTime DataCreazione { get; set; }
    public DateTime? DataInizioInstallazione { get; set; }
    public DateTime? DataFineInstallazione { get; set; }
    public string? VersioneWIC { get; set; }
    public DateTime? UltimaModifica { get; set; }
    public ProjectStatus StatoProgetto { get; set; }
    public bool IsInRitardo { get; set; }
    public string? Note { get; set; }
    public decimal? ValoreProgetto { get; set; }
    public decimal? MarginePrevisto { get; set; }
    public decimal CostiSostenuti { get; set; }
    
    // Navigation properties
    public List<LivelloProgetto> Livelli { get; set; }
    public List<ProdottoProgetto> Prodotti { get; set; }
    public List<StoricoModifica> Storico { get; set; }
}

public enum ProjectStatus
{
    ON_GOING,
    CRITICAL,
    HOLD_ON,
    RUSH,
    TO_CHECK,
    UPCOMING,
    PUSHED_OUT,
    ON_BID
}
```

### 11.2 LivelloProgetto Model

```csharp
public class LivelloProgetto
{
    public int Id { get; set; }
    public int ProgettoId { get; set; }
    public string Nome { get; set; }
    public int Ordine { get; set; }
    public string? Descrizione { get; set; }
    public DateTime? DataInizioInstallazione { get; set; }
    public DateTime? DataFineInstallazione { get; set; }
    public DateTime? DataCaricamento { get; set; }
    
    // Navigation property
    public Project? Progetto { get; set; }
}
```

### 11.3 ProdottoProgetto Model

```csharp
public class ProdottoProgetto
{
    public int Id { get; set; }
    public int ProgettoId { get; set; }
    public string TipoProdotto { get; set; }  // Metafora / Wallen / Armonica
    public string Variante { get; set; }
    public decimal QMq { get; set; }  // Quantità in metri quadri
    public decimal QFt { get; set; }  // Quantità in piedi quadri
    
    // Navigation property
    public Project? Progetto { get; set; }
}
```

### 11.4 StoricoModifica Model

```csharp
public class StoricoModifica
{
    public int Id { get; set; }
    public int ProgettoId { get; set; }
    public DateTime DataModifica { get; set; }
    public string UtenteModifica { get; set; }
    public string CampoModificato { get; set; }
    public string? ValorePrecedente { get; set; }
    public string? NuovoValore { get; set; }
    
    // Navigation property
    public Project? Progetto { get; set; }
}
```

### 11.5 MessaggioProgetto Model

```csharp
public class MessaggioProgetto
{
    public int Id { get; set; }
    public int ProgettoId { get; set; }
    public DateTime Data { get; set; }
    public string Utente { get; set; }
    public string Messaggio { get; set; }
    public string? Tipo { get; set; } // 'info', 'success', 'warning', 'error'
    public string? Allegato { get; set; }
    
    // Navigation property
    public Project? Progetto { get; set; }
}
```

### 11.6 ChangeLog Model

```csharp
public class ChangeLog
{
    public int Id { get; set; }
    public int ProgettoId { get; set; }
    public DateTime Data { get; set; }
    public string Utente { get; set; }
    public string Azione { get; set; } // 'created', 'updated', 'deleted', etc.
    public string Descrizione { get; set; }
    public Dictionary<string, object>? Dettagli { get; set; }
    
    // Navigation property
    public Project? Progetto { get; set; }
}
```

### 11.7 TimesheetEntry Model

```csharp
public class TimesheetEntry
{
    public int Id { get; set; }
    public string ProgettoId { get; set; }
    public string NumeroProgetto { get; set; }
    public string NomeProgetto { get; set; }
    public string Cliente { get; set; }
    public DateTime DataRendicontazione { get; set; }
    public double OreLavorate { get; set; }
    public string Note { get; set; }
    public string Utente { get; set; }
    public DateTime? DataCreazione { get; set; }
    public DateTime? UltimaModifica { get; set; }
}
```

### 11.8 Lookup Models

```csharp
public class Cliente
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? PartitaIVA { get; set; }
    public string? Contatto { get; set; }
    public string? IndirizzoCompleto { get; set; }
    public string? Note { get; set; }
}

public class Stato
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string CodiceISO { get; set; }
    public string Continente { get; set; }
}

public class Citta
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public int StatoId { get; set; }
    public string? Cap { get; set; }
    public string? Provincia { get; set; }
    public string? Regione { get; set; }
    
    // Navigation property
    public Stato? Stato { get; set; }
}

public class TeamTecnico
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string? Specializzazione { get; set; }
    public List<string>? Membri { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public bool Disponibilita { get; set; }
}

public class TeamAPL
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? Area { get; set; }
    public List<string>? Competenze { get; set; }
}

public class Sales
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? Zona { get; set; }
    public string? RegioneDiCompetenza { get; set; }
    public int? ProgettiGestiti { get; set; }
}

public class ProjectManager
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public int? ProgettiAttivi { get; set; }
    public string? Esperienza { get; set; }
    public List<string>? Certificazioni { get; set; }
}

public class SquadraInstallazione
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string? Tipo { get; set; }
    public string? Contatto { get; set; }
    public bool Disponibilita { get; set; }
    public List<string>? Competenze { get; set; }
    public int? NumeroMembri { get; set; }
}

public class ProdottoMaster
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string Categoria { get; set; }  // Metafora / Wallen / Armonica
    public string UnitaMisura { get; set; }
    public string? CodiceSAP { get; set; }
    public string? Descrizione { get; set; }
    public List<string>? VariantiDisponibili { get; set; }
}
```

---

## 12. Note Implementative

### 12.1 Paginazione

Per endpoint che restituiscono liste (es. GET /api/projects), considerare l'aggiunta di paginazione:

```
GET /api/projects?page=1&pageSize=50
```

### 12.2 Filtri e Sorting

Estendere l'endpoint GET /api/projects con filtri e ordinamento:

```
GET /api/projects?status=ON_GOING&sortBy=dataCreazione&sortDirection=desc
```

### 12.3 CORS Configuration

Assicurarsi che il backend configuri CORS per permettere richieste dalla WebApp Angular:

```csharp
services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});
```

#### 12.3.1 Middleware per Propagazione SessionId SAP

```csharp
public class SAPSessionMiddleware
{
    private readonly RequestDelegate _next;

    public SAPSessionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Estrai SessionId dal cookie o header
        var sessionId = context.Request.Headers["X-SAP-Session-Id"].ToString();
        
        // Aggiungi SessionId al context per uso downstream
        if (!string.IsNullOrEmpty(sessionId))
        {
            context.Items["SAPSessionId"] = sessionId;
        }

        await _next(context);
    }
}

// Registrazione in Startup.cs
app.UseMiddleware<SAPSessionMiddleware>();
```

### 12.4 Validation

Implementare validation usando Data Annotations o FluentValidation:

```csharp
public class ProjectValidator : AbstractValidator<Project>
{
    public ProjectValidator()
    {
        RuleFor(x => x.NumeroProgetto).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Cliente).NotEmpty().MaximumLength(100);
        RuleFor(x => x.NomeProgetto).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DataCreazione).NotEmpty();
    }
}
```

### 12.5 Database Schema

> **⚠️ IMPORTANTE**: Il database utilizzato è **SAP Business One**. Non viene creato un database separato.
> Tutte le tabelle sono User Defined Tables/Objects nel database SAP.

**Architettura Dati SAP:**

**UDO (User Defined Object):**
- `@AOPROJECT` - Progetti (UDO con form personalizzabile)

**User Defined Tables:**
- `@AOPROJLVL` - Livelli Progetto
- `@AOPROPRD` - Prodotti Progetto
- `@AOPROHIST` - Storico Modifiche
- `@AOSTATI` - Stati
- `@AOCITTA` - Città
- `@AOTEAMTECH` - Team Tecnici
- `@AOTEAMAPL` - Team APL
- `@AOSALES` - Sales
- `@AOPMGR` - Project Managers
- `@AOSQUADRA` - Squadre Installazione
- `@AOPRODMAST` - Prodotti Master

**Tabelle SAP Standard:**
- `OCRD` - Business Partners (Clienti)

**Relazioni:**
- Project → Livelli (1:N) via U_Parent
- Project → Prodotti (1:N) via U_Parent
- Project → StoricoModifiche (1:N) via U_Parent
- Project → Cliente (N:1) via U_Cliente → OCRD.CardCode
- Citta → Stato (N:1) via StatoId

**Nota**: Non utilizzare database esterni. Tutti i dati risiedono nel database SAP Business One.

### 12.6 Error Handling

Implementare un gestore centralizzato degli errori:

```csharp
public class GlobalExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }
}
```

---

## 13. Test Cases

### 13.1 Unit Tests

Testare:
- Logica di business
- Validation rules
- Calculation logic (es. isInRitardo)

### 13.2 Integration Tests

Testare:
- CRUD operations
- Relationship management
- Search and filter functionality

### 13.3 End-to-End Tests

Testare:
- Complete workflow from create to delete
- Export functionality
- Statistics calculation

---

## 14. Deployment

### 14.1 Environment Variables

```
ConnectionString=Server=localhost;Database=AdottaProjects;...
EnableSwagger=true
LogLevel=Information
```

### 14.2 Build and Publish

```bash
dotnet build
dotnet publish -c Release -o ./publish
```

---

## 15. Security Considerations

1. **Autenticazione SAP Business One**: Utilizzare SessionId del Service Layer
   - Gestire timeout di sessione (default 30 minuti)
   - Implementare refresh session automatico
   - Logout esplicito al termine della sessione utente

2. **Validazione Input**: Validare tutti gli input prima di inviarli a SAP
   - Regole di validazione SAP UDO
   - Lunghezza campi conforme a SAP
   - Tipi di dato corretti

3. **Protezione Endpoint**: Endpoint protetti richiedono SessionId SAP valido
   - Middleware per verifica SessionId
   - Headers personalizzati per propagazione SessionId

4. **Rate Limiting**: Implementare rate limiting per:
   - Evitare sovraccarico Service Layer SAP
   - Proteggere da chiamate abusive
   - Gestire timeout/retry automatici

5. **Logging**: Loggare tutte le operazioni critiche
   - Audit trail per modifiche progetti
   - Log chiamate SAP Service Layer
   - Log errori e eccezioni
   - Log accessi utente

6. **HTTPS**: Obbligatorio per tutte le comunicazioni
   - API .NET → SAP Service Layer (HTTPS)
   - WebApp Angular → API .NET (HTTPS)

7. **Credenziali**: Gestire credenziali SAP in modo sicuro
   - Usare appsettings.json (development) o Azure Key Vault (production)
   - Never commit credentials nel codice sorgente
   - Ruoli e permessi SAP configurati correttamente

---

## 16. Deployment e Configurazione

### 16.1 SAP Business One Setup

Prima di utilizzare l'API, configurare in SAP Business One:

1. **Creare User Defined Tables**:
   ```
   - @AOPROJECT (Progetti - come UDO)
   - @AOPROJLVL (Livelli Progetto)
   - @AOPROPRD (Prodotti Progetto)
   - @AOPROHIST (Storico Modifiche)
   - @AOSTATI (Stati)
   - @AOCITTA (Città)
   - @AOTEAMTECH (Team Tecnici)
   - @AOTEAMAPL (Team APL)
   - @AOSALES (Sales)
   - @AOPMGR (Project Managers)
   - @AOSQUADRA (Squadre Installazione)
   - @AOPRODMAST (Prodotti Master)
   ```

2. **Creare UDO**:
   ```
   - Nome: ADOTTA_PROJECTS
   - Tabella: @AOPROJECT
   - Configurare form con tutti i campi necessari
   - Impostare validation rules
   ```

3. **Configurare User Defined Fields** per tutte le tabelle

4. **Configurare Service Layer**:
   ```
   - Abilitare Service Layer in SAP B1
   - Configurare porta (default 50000)
   - Configurare HTTPS certificate
   - Configurare utenze e permessi
   ```

### 16.2 Configurazione API .NET

#### 16.2.1 appsettings.json Production

```json
{
  "SAPSettings": {
    "ServiceLayerUrl": "https://sap-production-server:50000/b1s/v1",
    "CompanyDB": "PRODUCTION_DB",
    "UserName": "ADOTTA_API_USER",
    "DefaultLanguage": "it",
    "SessionTimeout": 30
  },
  "AllowedOrigins": [
    "https://app.adotta.it",
    "https://www.adotta.it"
  ],
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "SAP": "Debug"
    }
  },
  "Azure": {
    "KeyVaultUrl": "https://adotta-kv.vault.azure.net/"
  }
}
```

### 16.3 Environment Variables

```bash
# Development
SAP_SERVICE_LAYER_URL=https://localhost:50000/b1s/v1
SAP_COMPANY_DB=SBODEMOUS
SAP_USERNAME=manager
SAP_PASSWORD=Password1

# Production
SAP_SERVICE_LAYER_URL=https://sap-prod:50000/b1s/v1
SAP_COMPANY_DB=PROD_DB
SAP_USERNAME=<Azure Key Vault Secret>
SAP_PASSWORD=<Azure Key Vault Secret>
```

### 16.4 Build and Publish

```bash
# Build
dotnet build -c Release

# Publish
dotnet publish -c Release -o ./publish

# Docker (opzionale)
docker build -t adotta-api .
docker run -p 5000:5000 adotta-api
```

---

## 17. Implementazione Attuale

### 17.1 Stato Implementazione

Il sistema è attualmente in fase di sviluppo con le seguenti funzionalità implementate:

#### Frontend (Angular)
- ✅ **Gestione Progetti**: CRUD completo per progetti
- ✅ **Messaggi Progetto**: Sistema di messaggistica per progetti
- ✅ **Change Log**: Tracciamento automatico modifiche
- ✅ **Timesheet**: Sistema di rendicontazione ore lavorate
- ✅ **Lookup Tables**: Gestione tabelle di supporto
- ✅ **Autenticazione**: Sistema session-based con localStorage
- ✅ **Dashboard**: Dashboard principale con statistiche progetti

#### Servizi Mock
- ✅ Mock Project Service
- ✅ Mock Lookup Service
- ✅ Mock Timesheet Service
- ✅ Mock Auth Service
- ✅ Mock Data Service (gestione dati in-memory)

#### API Endpoints (Preparati per Backend .NET)
- ✅ Interfacce complete per tutte le API
- ✅ Modelli TypeScript/Interfaces definiti
- ✅ Servizi Angular con HttpClient configurati

### 17.2 Architettura Autenticazione Attuale

Il sistema utilizza attualmente un'autenticazione basata su **session token** memorizzato nel localStorage:

```typescript
// Session Structure
interface Session {
  token: string;
  user: User;
  expiresAt: Date;
}
```

**Flusso Autenticazione:**
1. Frontend riceve credenziali utente
2. AuthService valida credenziali
3. Crea session token
4. Salva session nel localStorage
5. Include token in tutte le richieste successive (via HTTP interceptor)

**Prossimi Sviluppi:**
- Integrazione con SAP Business One Service Layer
- Sostituzione session token con SAP SessionId
- Propagazione SessionId in tutte le chiamate API

### 17.3 Modelli Dati Implementati

Tutti i modelli sono stati implementati in TypeScript:
- `Project` con relazioni a Livelli, Prodotti, Storico, Messaggi, ChangeLog
- `LivelloProgetto`, `ProdottoProgetto`, `StoricoModifica`
- `MessaggioProgetto`, `ChangeLog`
- `TimesheetEntry` con statistiche
- Tutte le lookup models (Cliente, Stato, Città, Team, etc.)

---

## 18. Changelog

- **v1.2** (2024-12): Implementate funzionalità Messaggi, Change Log e Timesheet
  - Aggiunto sistema messaggistica progetti
  - Implementato change log automatico
  - Creato sistema timesheet per rendicontazione ore
  - Aggiornati modelli dati e API specification

- **v1.1** (2024-01-20): Aggiunta integrazione SAP Business One Service Layer
  - Implementata autenticazione via SessionId SAP
  - Anagrafica progetti come UDO
  - Tabelle utente per dati di supporto
  - Integrazione Business Partners per clienti
  - Mapping DTO ↔ SAP UDO

- **v1.0** (2024-01-15): Prima versione della specifica API

