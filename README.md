# ADOTTA Projects Suite API

API .NET per la gestione di progetti integrata con SAP Business One Service Layer.

## Descrizione

Questa applicazione fornisce un'interfaccia REST API tra la WebApp Angular e il Service Layer di SAP Business One, gestendo la gestione dei progetti, anagrafiche clienti, lookup e altre entità correlate.

## Caratteristiche

- Integrazione completa con SAP Business One Service Layer
- Gestione progetti e relativi livelli/prodotti
- Anagrafica clienti tramite Business Partners SAP
- Tabelle di lookup per team, sales, project managers, ecc.
- Validazione dati con FluentValidation
- Logging strutturato con Serilog
- Gestione errori centralizzata
- CORS configurato per WebApp Angular

## Prerequisiti

- .NET 8.0 SDK o superiore
- SAP Business One con Service Layer abilitato e configurato
- Accesso al database SAP con le tabelle User Defined Objects configurate

## Struttura Database SAP

Tutte le tabelle sono User Defined Objects/Tables in SAP Business One:

- `@AOPROJECT` - Progetti (UDO)
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

## Configurazione

### appsettings.json

```json
{
  "SAPSettings": {
    "ServiceLayerUrl": "https://your-sap-server:50000/b1s/v1",
    "CompanyDB": "YOUR_DB",
    "UserName": "your_username",
    "DefaultLanguage": "en",
    "SessionTimeout": 30
  },
  "AllowedOrigins": [
    "http://localhost:4200"
  ]
}
```

## Avvio

```bash
# Restore packages
dotnet restore

# Run
dotnet run
```

L'API sarà disponibile su:
- HTTP: http://localhost:5000
- HTTPS: https://localhost:5001

Swagger UI disponibile su: `https://localhost:5001/swagger`

## Endpoint Principali

### Autenticazione
- `POST /api/auth/login` - Login a SAP
- `POST /api/auth/logout` - Logout da SAP

### Progetti
- `GET /api/projects` - Lista tutti i progetti
- `GET /api/projects/{numeroProgetto}` - Dettagli progetto
- `POST /api/projects` - Crea nuovo progetto
- `PUT /api/projects/{numeroProgetto}` - Aggiorna progetto
- `DELETE /api/projects/{numeroProgetto}` - Elimina progetto
- `GET /api/projects/search?q={term}` - Ricerca progetti
- `POST /api/projects/filter` - Filtra progetti
- `GET /api/projects/stats` - Statistiche progetti
- `GET /api/projects/stats/by-status` - Statistiche per stato
- `GET /api/projects/stats/by-month` - Statistiche per mese
- `GET /api/projects/{numeroProgetto}/livelli` - Livelli progetto
- `GET /api/projects/{numeroProgetto}/prodotti` - Prodotti progetto
- `GET /api/projects/{numeroProgetto}/storico` - Storico modifiche
- `POST /api/projects/{numeroProgetto}/wic-snapshot` - Crea snapshot WIC

### Timesheet
- `GET /api/timesheet` - Tutte le rendicontazioni
- `GET /api/timesheet/{id}` - Dettagli rendicontazione
- `POST /api/timesheet` - Crea rendicontazione
- `PUT /api/timesheet/{id}` - Aggiorna rendicontazione
- `DELETE /api/timesheet/{id}` - Elimina rendicontazione
- `GET /api/timesheet/project/{numeroProgetto}` - Rendicontazioni per progetto
- `GET /api/timesheet/overview` - Overview con statistiche
- `GET /api/timesheet/summary` - Riepilogo statistiche
- `GET /api/timesheet/user/{utente}` - Rendicontazioni per utente

### Lookup
- `GET /api/lookup/clienti` - Lista clienti (Business Partners)
- `GET /api/lookup/stati` - Lista stati
- `GET /api/lookup/citta` - Lista città
- `GET /api/lookup/team-tecnici` - Team tecnici
- `GET /api/lookup/team-apl` - Team APL
- `GET /api/lookup/sales` - Sales
- `GET /api/lookup/project-managers` - Project Managers
- `GET /api/lookup/squadre-installazione` - Squadre installazione
- `GET /api/lookup/prodotti-master` - Prodotti master

Vedi `specs/API-SPECIFICATION.md` per la documentazione completa.

## Autenticazione

L'API ora utilizza token JWT per gestire l'identità dell'utente e propagare in sicurezza la sessione SAP lato server:

1. Effettua login tramite `POST /api/auth/login` inviando `email` e `password`
2. Ricevi in risposta un oggetto `LoginResponseDto` contenente il token JWT (`token`) e i dati dell'utente
3. Includi il token in tutte le richieste protette tramite header `Authorization: Bearer {token}`
4. La sessione SAP (`X-SAP-Session-Id`) viene estratta automaticamente dal token e non deve più essere inviata dal client
5. Esegui logout tramite `POST /api/auth/logout` (richiede il token attivo)

## Logging

I log sono gestiti con Serilog e scritti su:
- Console
- File nella cartella `logs/` (rolling daily, conservati per 7 giorni)

## Sviluppo

```bash
# Build
dotnet build

# Test
dotnet test

# Publish
dotnet publish -c Release -o ./publish
```

## Licenza

Copyright (c) ADOTTA

