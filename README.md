# Modulo 3 - Sistema de Monitoramento de Subestacoes

Monorepo com backend `.NET 10` e frontend `Next.js 16` para monitoramento eletrico em tempo real, com suporte a:

- ingestao UDP de telemetria e eventos (modulos 1, 2, 4 e 5)
- gerenciamento de rele do modulo 6 (descoberta UDP + comando TCP)
- API REST para dashboards e integracoes
- dashboard web com visao operacional, analitica e historica

## Objetivo do sistema

Consolidar em uma unica aplicacao:

- estado em tempo real dos IEDs
- eventos de protecao ativos e historico
- alarmes agregados com distribuicao geografica
- controle operacional de rele do modulo 6

O foco e operacao + observabilidade: dados vivos, status de conectividade, tendencia e historico para analise.

## Arquitetura de alto nivel

```text
Dispositivos/Simuladores
   | (UDP 4210: MODULE1/2/4/5 e tambem pacotes M6)
   | (UDP 4211: canal dedicado M6)
   v
BroadcastReceiverService (BackgroundService)
   -> validacao/normalizacao -> filas concorrentes -> DataAggregationService (estado em memoria)
                                              |-> MonitoringController (REST)
                                              |-> CommandController (REST)
                                              |-> Module6Controller (REST)
                                              |-> Module6Notifier -> SignalR Hub (/hubs/module6)

Frontend Next.js (polling HTTP) -> ControlApi REST
```

## Estrutura do repositorio

```text
modulo3/
├─ modulo3-back/
│  ├─ ControlApi/          # API ASP.NET Core + Swagger + SignalR
│  ├─ Services/            # servicos de ingestao, agregacao e comandos
│  ├─ Core/                # modelos, DTOs, contratos e utilitarios
│  ├─ Infrastructure/      # projeto de infraestrutura (EF pacotes, sem uso ativo)
│  ├─ Test/                # simuladores de modulos
│  └─ Modulo3.slnx
└─ modulo3-front/
   ├─ app/                 # App Router (Next.js)
   ├─ components/          # UI por dominio
   ├─ hooks/               # hooks de polling e comandos
   ├─ services/            # cliente HTTP da API
   └─ types/               # contratos TS alinhados aos DTOs
```

## Backend: arquitetura tecnica

### Composicao por projeto

- `ControlApi`: composicao da aplicacao, DI, controllers, Swagger, CORS e SignalR.
- `Services`: logica de ingestao e agregacao (`BroadcastReceiverService`, `DataAggregationService`, `CommandBroadcastService`, `Module6CommandService`).
- `Core`: modelos de dominio (`BroadcastPacket`, `MeasurementData`, `ProtectionEvent`, `AggregatedAlarm`, `Module6Packet`, etc.) e DTOs de resposta.
- `Infrastructure`: referenciado, com pacotes EF Core, mas sem repositorios/contextos ativos no fluxo atual.
- `Test`: simuladores de carga/eventos para validacao local.

### Fluxo de ingestao e processamento

1. `BroadcastReceiverService` escuta UDP em:
   - `4210` para trafego geral (MODULE1..MODULE5 e pacotes M6 mistos)
   - `4211` dedicado ao modulo 6
2. Pacotes sao validados e normalizados:
   - aceita pacote canonico `ORIGIN;SEQUENCE;MODULE;OPERATION;JSON;TIMESTAMP`
   - aceita JSON bruto de alarmes M5 (`critical_event_id`, `cluster_size`, etc.) e converte para formato canonico
   - aceita JSON de dispositivo real (`Ia`, `Ib`, `Ic`, `numPacote`, `idDispositivo`) e converte para `MODULE1`
3. Pacotes entram em filas concorrentes.
4. `DataAggregationService` atualiza estado em memoria.
5. Controllers expoem os dados por HTTP.
6. Para modulo 6, notificacoes tambem sao emitidas por SignalR.

### Estado em memoria (DataAggregationService)

- buffer circular por dispositivo para historico de medicoes (`1000` pontos por device)
- evento ativo por chave `device + tipo + fase`
- historico global de eventos (`500` itens)
- status de conectividade por device com stale threshold de `10s`
- alarmes agregados por `critical_event_id`
- comandos pendentes por dispositivo
- mapa de modulo 6 (`UniqueId -> IP`, `ModuleId -> IP`, `ModuleId -> estado do rele`)

Nao ha persistencia em banco no estado atual: reiniciar a API limpa os dados em memoria.

## Frontend: arquitetura tecnica e usabilidade

### Stack

- Next.js `16.1.6` (App Router)
- React `19.2.3` + TypeScript `strict`
- Tailwind CSS v4 + shadcn/ui + Radix UI
- Recharts para graficos
- Leaflet/OpenStreetMap para mapa de alarmes

### Estrategia de atualizacao de dados

Frontend usa polling HTTP desacoplado por dominio:

- `useRealTimeData`: 2s
- `useDevices`: 3s
- `useAlarms`: 5s
- `useEventReports`: 5s
- `useModule6`: 3s (modulos sem config + estados de rele)

Ha suporte global a pausa/retomada de atualizacoes em `app/page.tsx`.

### Operacao na interface

A dashboard esta dividida em 6 abas principais:

- `Visao Geral`: metricas de sistema, status consolidado e lista de dispositivos.
- `Tempo Real (M1)`: graficos live de tensao/corrente/frequencia/fator de potencia + tabela de medicoes + eventos ativos.
- `Eventos (M2)`: relatorios agregados e distribuicao por dispositivo.
- `Alarmes (M5)`: resumo por severidade/tipo e mapa geografico interativo.
- `Reles (M6)`: descoberta de modulos nao configurados, atribuicao de ID e comandos de abrir/fechar/consultar rele.
- `Historico`: consulta temporal de medicoes (M1) e historico de eventos (M2) com filtros.

Padroes de UX implementados:

- estados claros de `loading`, `erro`, vazio e conectado/pausado
- feedback de sucesso/erro em acoes de controle do modulo 6
- indicadores visuais de online/offline e stale
- tabelas e cards com foco operacional

## Portas e protocolos (estado atual)

| Componente | Protocolo | Porta/URL | Observacao |
|---|---|---|---|
| ControlApi (HTTP) | HTTP | `http://localhost:5151` | Swagger UI na raiz `/` |
| ControlApi (HTTPS) | HTTPS | `https://localhost:7012` | perfil `https` |
| Health | HTTP | `/health` | status simples da API |
| Ingestao geral | UDP | `4210` | MODULE1,2,4,5 e pacotes M6 roteados |
| Ingestao dedicada M6 | UDP | `4211` | canal exclusivo modulo 6 |
| Comando M3 broadcast | UDP | `255.255.255.255:5010` | `CommandBroadcastService` |
| Comando modulo 6 | TCP | `<ip_modulo>:5000` | `Module6CommandService` |
| SignalR modulo 6 | WS/HTTP | `/hubs/module6` | eventos em tempo real do M6 |
| Frontend dev | HTTP | `http://localhost:3000` | Next dev server |

## Contratos de payload

### 1) Pacote canonico de broadcast

```text
ORIGIN;SEQUENCE;MODULE;OPERATION_TYPE;DATA_JSON;TIMESTAMP_ISO8601
```

Exemplo `MODULE1`:

```text
IED-001;42;MODULE1;MEASUREMENT;{"Voltage":220.5,"Current":10.2,"Frequency":60.0,"PowerFactor":0.98,"Status":"NORMAL"};2026-03-05T12:00:00.0000000Z
```

### 2) Protocolo modulo 6

- broadcast sem configuracao:
  - `#00;0;00;F499540B65F4`
- configurar ID:
  - `#00;9;05;F499540B65F4`
- comando rele:
  - `#05;1;00` (fechar)
  - `#05;2;00` (abrir)
  - `#05;3;00` (consultar)
- resposta de estado (esperada pelo backend):
  - `!00;3;05;01` (`05` modulo, `01` rele fechado)

### 3) JSON bruto aceito e normalizado

- alarme agregado M5:
  - `critical_event_id`, `critical_event_type`, `local`, `timestamp`, `cluster_size`
- medicao de dispositivo real:
  - `Ia`, `Ib`, `Ic`, `numPacote`, `idDispositivo`

## API REST

### Monitoramento

- `GET /api/Monitoring/realtime`
- `GET /api/Monitoring/historical/{deviceId}?startTime=...&endTime=...`
- `GET /api/Monitoring/devices`
- `GET /api/Monitoring/events/active`
- `GET /api/Monitoring/events/reports`
- `GET /api/Monitoring/alarms`
- `GET /api/Monitoring/events/history?deviceId=&limit=100&startTime=&endTime=`

### Comandos gerais

- `POST /api/Command/send`
- `GET /api/Command/status/{deviceId}`

### Modulo 6 - reles

- `GET /api/module6/modules`
- `GET /api/module6/unconfigured`
- `POST /api/module6/configure`
- `POST /api/module6/{moduleId}/relay/close`
- `POST /api/module6/{moduleId}/relay/open`
- `GET /api/module6/{moduleId}/relay/state`

### Sistema

- `GET /health`

## Como executar em desenvolvimento

### 1) Backend

```bash
cd modulo3-back
dotnet restore
dotnet run --project ControlApi/ControlApi.csproj
```

### 2) Frontend

```bash
cd modulo3-front
npm install
npm run dev
```

### 3) Variavel de ambiente frontend

Crie `modulo3-front/.env.local`:

```env
NEXT_PUBLIC_API_URL=http://localhost:5151
```

### 4) Simuladores (opcional)

```bash
cd modulo3-back
dotnet run --project Test/Test.csproj
```

Argumentos uteis:

- `dotnet run --project Test/Test.csproj -- module1`
- `dotnet run --project Test/Test.csproj -- module2`
- `dotnet run --project Test/Test.csproj -- module4`
- `dotnet run --project Test/Test.csproj -- module5`
- `dotnet run --project Test/Test.csproj -- module6`
- sem argumento: roda todos

## Validacao rapida ponta a ponta

1. Suba API e frontend.
2. Rode simulador (`module1`, `module2`, `module5` e `module6` sao os mais visiveis na UI).
3. Abra `http://localhost:3000`.
4. Verifique:
   - dispositivos ativos/inativos
   - curvas de medicao variando
   - eventos ativos e historico
   - alarmes no mapa
   - gerenciamento de rele no modulo 6
5. Valide API diretamente em `http://localhost:5151/` (Swagger) e `GET /health`.

## Observabilidade

- logs detalhados de entrada/validacao/processamento UDP em `BroadcastReceiverService`
- logs de estado e confirmacao de comandos em `DataAggregationService`
- logs de conexao, envio e leitura TCP para modulo 6 em `Module6CommandService`

Sugestao: em ambiente local, manter nivel `Information` para rastreabilidade de pacotes.

## Limitacoes e pontos de atencao atuais

- dados operacionais sao somente em memoria (sem persistencia historica em DB).
- `Module6Notifier` publica SignalR, mas frontend atual usa polling HTTP (nao ha cliente SignalR implementado).
- `Module6PollingService` existe no codigo, mas nao esta registrado no `Program.cs`.
- bloco `BroadcastSettings` em `appsettings.json` ainda nao esta ligado via options binding (portas principais estao hardcoded nos servicos).
- projeto `Infrastructure` possui pacotes EF Core, porem sem uso ativo no fluxo atual.
- componentes `CommandPanel` e `DataControls` existem no frontend, mas nao estao montados na pagina principal no estado atual.

## Requisitos

- .NET SDK 10
- Node.js 20+
- npm
