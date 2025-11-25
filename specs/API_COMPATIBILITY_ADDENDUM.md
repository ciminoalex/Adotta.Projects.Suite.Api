# ADOTTA Projects Suite – API Addendum (Frontend Compatibility)

Documento di riferimento per allineare il backend .NET/Service Layer agli schemi e alle rotte realmente utilizzate dalla WebApp Angular. Tutte le risposte devono essere JSON (`application/json`) e includere l'header `X-SAP-Session-Id` per la propagazione della sessione SAP, salvo gli endpoint di login.

## Convenzioni

- **Date**: stringhe ISO 8601 (`YYYY-MM-DDTHH:mm:ssZ`).
- **Numeri decimali**: punto come separatore.
- **Campi opzionali**: omettibili o `null`.
- **200 OK** è la risposta di default salvo diversa indicazione.

---

## 1. Authentication & Session

### `POST /api/Auth/login`
- **Request** (`LoginRequestDto` – già in swagger):
  ```json
  {
    "companyDB": "SBODEMOUS",
    "userName": "manager",
    "password": "Password1"
  }
  ```
- **Response** (`LoginResponseDto`):
  ```json
  {
    "sessionId": "CA4B81FF-5DD9-4916-9FBF-DD0F4CFC9A72",
    "version": "10.00.140",
    "sessionTimeout": 30
  }
  ```

### `POST /api/Auth/logout`
- **Request**: header `X-SAP-Session-Id`.
- **Response**: `204 No Content` (nessun body).

### `GET /api/Auth/users/by-email/{email}`
- **Uso**: verifica utenti O365 (`auth.service.ts`).
- **Response** (`UserDto`):
  ```json
  {
    "id": 12,
    "username": "mrossi",
    "email": "mario.rossi@adotta.it",
    "userName": "Mario Rossi",
    "ruolo": "PM",
    "teamTecnico": "Team HVAC Roma",
    "isActive": true
  }
  ```

---

## 2. User Management (Area Admin)

Gli endpoint devono risiedere sotto `/api/users` e restituire/arcare la struttura `UserDto` (come sopra, con campo `password` accettato solo in creazione/aggiornamento).

| Metodo & Endpoint | Note request | Response |
| --- | --- | --- |
| `GET /api/users` | Filtri opzionali `?q=` (testo) | `UserDto[]` |
| `POST /api/users` | Body `UserDto` con `password` iniziale | `UserDto` (senza password) |
| `PUT /api/users/{id}` | Body `UserDto` (facoltativo aggiornare password) | `UserDto` |
| `DELETE /api/users/{id}` | — | `204 No Content` |

Struttura `UserDto` restituita dagli endpoint:
```json
{
  "id": 42,
  "username": "acimino",
  "email": "alessandro.cimino@mtf-srl.com",
  "userName": "Alessandro Cimino",
  "ruolo": "Admin",
  "teamTecnico": "Team Elettrico Milano",
  "isActive": true
}
```

---

## 3. Lookup / Master Data

### Clienti
- `POST /api/lookup/clienti` → `Cliente`
- `PUT /api/lookup/clienti/{id}` → `Cliente`
- `DELETE /api/lookup/clienti/{id}` → `204`
- `GET /api/lookup/clienti/search?q=` → `Cliente[]`

Struttura `Cliente`:
```json
{
  "id": "C001",
  "cardCode": "C001",
  "nome": "TechCorp Italia",
  "email": "info@techcorp.it",
  "telefono": "+39 02 1234567",
  "partitaIVA": "IT12345678901",
  "contatto": "Mario Rossi",
  "indirizzoCompleto": "Via Roma 123, Milano",
  "note": "Cliente principale",
  "validFor": "Y",
  "addresses": [
    {
      "addressName": "Sede",
      "street": "Via Roma 123",
      "city": "Milano",
      "country": "IT",
      "zipCode": "20100"
    }
  ]
}
```

### Stati & Città
- `GET /api/lookup/stati/{id}` → `Stato`
- `POST/PUT/DELETE /api/lookup/citta` (creazione/aggiornamento/eliminazione) → `Citta` / `204`
- `GET /api/lookup/citta/{id}` → `Citta`
- `GET /api/lookup/citta?statoId=IT` → `Citta[]`

### Team Tecnici / Team APL / Sales / Project Managers / Squadre Installazione / Prodotti Master
Per ciascuna risorsa sono richiesti:
- `GET /api/lookup/{resource}` → lista DTO specifico
- `GET /api/lookup/{resource}/{id}` → singolo DTO
- `POST /api/lookup/{resource}` → DTO creato
- `PUT /api/lookup/{resource}/{id}` → DTO aggiornato
- `DELETE /api/lookup/{resource}/{id}` → `204`

Gli schemi sono quelli definiti in `lookup.model.ts` (campi `nome`, `email`, `specializzazione`, ecc.) e già richiamati nei mock.

---

## 4. Projects Core

### `PATCH /api/projects/{numeroProgetto}`
- **Uso**: salvataggi parziali.
- **Request**: `Partial<Project>` (qualsiasi subset dei campi sotto).
- **Response**: `Project`.

### `GET /api/projects/{numeroProgetto}/livelli`
Deve restituire i livelli già popolati con i prodotti:
```json
[
  {
    "id": 1,
    "progettoId": 1,
    "nome": "Piano Terra",
    "ordine": 1,
    "descrizione": "Descrizione",
    "dataInizioInstallazione": "2024-02-01T00:00:00Z",
    "dataFineInstallazione": "2024-02-20T00:00:00Z",
    "dataCaricamento": "2024-01-15T00:00:00Z",
    "prodotti": [
      {
        "id": 10,
        "progettoId": 1,
        "livelloId": 1,
        "tipoProdotto": "Metafora",
        "variante": "Bianco",
        "qMq": 250.5,
        "qFt": 2696.38
      }
    ]
  }
]
```

### `PUT /api/projects/{numero}/livelli/{livelloId}`
- **Request**: `LivelloProgetto` (con o senza `prodotti`).
- **Response**: livello aggiornato (stesso schema).

### Prodotti per Livello
| Endpoint | Request | Response |
| --- | --- | --- |
| `GET /api/projects/{numero}/livelli/{livelloId}/prodotti` | — | `ProdottoProgetto[]` |
| `POST /api/projects/{numero}/livelli/{livelloId}/prodotti` | `ProdottoProgetto` | `ProdottoProgetto` |
| `PUT /api/projects/{numero}/prodotti/{prodottoId}` | `ProdottoProgetto` | `ProdottoProgetto` |

`ProdottoProgetto`:
```json
{
  "id": 10,
  "progettoId": 1,
  "livelloId": 1,
  "tipoProdotto": "Metafora",
  "variante": "Bianco",
  "qMq": 250.5,
  "qFt": 2696.38
}
```

### Storico & Snapshot
- `GET /api/projects/{numero}/storico` → `StoricoModifica[]`
- `POST /api/projects/{numero}/wic-snapshot` → `StoricoModifica[]` (lista con la nuova snapshot in testa)

`StoricoModifica`:
```json
{
  "id": 123,
  "progettoId": 1,
  "dataModifica": "2024-02-04T12:30:00Z",
  "utenteModifica": "Mario Rossi",
  "campoModificato": "dataInizioInstallazione",
  "valorePrecedente": "2024-02-10",
  "nuovoValore": "2024-02-15",
  "versioneWIC": "WIC-5"
}
```

### Chat Progetto
| Endpoint | Response |
| --- | --- |
| `GET /api/projects/{numero}/messaggi` | `MessaggioProgetto[]` |
| `POST /api/projects/{numero}/messaggi` | `MessaggioProgetto` |
| `PUT /api/projects/{numero}/messaggi/{id}` | `MessaggioProgetto` |
| `DELETE /api/projects/{numero}/messaggi/{id}` | `204` |

`MessaggioProgetto`:
```json
{
  "id": 45,
  "progettoId": 1,
  "data": "2024-02-05T09:15:00Z",
  "utente": "Giulia Bianchi",
  "messaggio": "Team installazione confermato",
  "tipo": "info",
  "allegato": null
}
```

### Change Log
- `GET /api/projects/{numero}/changelog` → `ChangeLog[]`
- `POST /api/projects/{numero}/changelog` → `ChangeLog`

`ChangeLog`:
```json
{
  "id": 77,
  "progettoId": 1,
  "data": "2024-02-06T10:00:00Z",
  "utente": "System",
  "azione": "status_changed",
  "descrizione": "Stato da ON_GOING a RUSH",
  "dettagli": {
    "old": "ON_GOING",
    "new": "RUSH"
  }
}
```

### Export
- `POST /api/projects/export/{format}` con body `{ "filters": "{...}" }` → `application/octet-stream` (file Excel/PDF/CSV).

---

## 5. Timesheet & KPI

### Nuovi Endpoint
| Endpoint | Descrizione | Response |
| --- | --- | --- |
| `GET /api/timesheet/by-date-range?startDate=&endDate=` | Filtro custom | `TimesheetEntry[]` |
| `GET /api/timesheet/stats/by-project` | KPI pro progetto | `Array<{ numeroProgetto: string; totaleOre: number; numeroRendicontazioni: number; ultimaRendicontazione?: string; }>` |
| `GET /api/timesheet/stats/by-user` | KPI per utente | `Array<{ utente: string; totaleOre: number; numeroRendicontazioni: number; progettiCoinvolti: number; }>` |
| `GET /api/timesheet/stats/daily?date=` | Trend giornaliero | `Array<{ ora: string; totaleOre: number; entries: number; }>` |

### Adeguamento Schemi
- `TimesheetEntryDto` deve includere il campo opzionale `livelloId`:
  ```json
  {
    "id": 501,
    "progettoId": "24001",
    "numeroProgetto": "24001",
    "nomeProgetto": "Installazione HVAC Uffici Milano",
    "cliente": "TechCorp Italia",
    "livelloId": 1,
    "dataRendicontazione": "2024-02-07T00:00:00Z",
    "oreLavorate": 6.5,
    "note": "Cablaggio piano terra",
    "utente": "antonio.verdi",
    "dataCreazione": "2024-02-07T08:00:00Z",
    "ultimaModifica": "2024-02-07T10:00:00Z"
  }
  ```
- `TimesheetOverviewResponse` (già usato dall’app) deve mantenere:
  ```json
  {
    "timesheets": [
      {
        "numeroProgetto": "24001",
        "nomeProgetto": "Installazione HVAC",
        "cliente": "TechCorp Italia",
        "totaleOre": 120,
        "numeroRendicontazioni": 18,
        "ultimaRendicontazione": "2024-02-07T00:00:00Z",
        "rendicontazioni": [ /* TimesheetEntry[] opzionale */ ]
      }
    ],
    "summary": {
      "totaleOre": 450,
      "totaleRendicontazioni": 60,
      "progettiRendicontati": 8,
      "mediaOrePerProgetto": 56.25
    }
  }
  ```

---

## 6. Schema Project completo (per riferimento)

```json
{
  "numeroProgetto": "24001",
  "cliente": "TechCorp Italia",
  "nomeProgetto": "Installazione HVAC Uffici Milano",
  "citta": "Milano",
  "stato": "IT",
  "teamTecnico": "Team Elettrico Milano",
  "teamAPL": "Team APL Nord",
  "sales": "Marco Vendite",
  "projectManager": "Mario Rossi",
  "teamInstallazione": "Squadra Installazione Milano",
  "dataCreazione": "2024-01-15T00:00:00Z",
  "dataInizioInstallazione": "2024-02-01T00:00:00Z",
  "dataFineInstallazione": "2024-03-20T00:00:00Z",
  "versioneWIC": "WIC-1.0",
  "ultimaModifica": "2024-02-05T11:00:00Z",
  "statoProgetto": "ON_GOING",
  "isInRitardo": false,
  "note": "Note aggiuntive",
  "valoreProgetto": 500000,
  "marginePrevisto": 0.25,
  "costiSostenuti": 375000,
  "livelli": [ /* come definito sopra */ ],
  "prodotti": [ /* fallback legacy */ ],
  "storico": [ /* StoricoModifica[] */ ],
  "messaggi": [ /* MessaggioProgetto[] */ ],
  "changeLog": [ /* ChangeLog[] */ ],
  "quantitaTotaleMq": 1200,
  "quantitaTotaleFt": 12916
}
```

---

Con l’implementazione di tutti gli endpoint e delle strutture dati qui descritte, l’API risulta pienamente compatibile con i servizi Angular (sia reali sia mock) dell’app ADOTTA Projects Suite.

