# Modulo 3 - Sistema de Monitoramento de Subestacoes

Repositorio com dois projetos integrados:

- `modulo3-back`: API .NET para ingestao de pacotes UDP/TCP, processamento e exposicao de dados.
- `modulo3-front`: Dashboard Next.js para visualizacao e operacao do sistema.

## Estrutura do repositorio

```text
modulo3/
├─ modulo3-back/
│  ├─ ControlApi/
│  ├─ Services/
│  ├─ Core/
│  └─ Test/
└─ modulo3-front/
   ├─ app/
   ├─ components/
   ├─ hooks/
   ├─ services/
   └─ types/
```

## Visao geral da arquitetura

```text
Integracao externa (UDP/TCP) -> modulo3-back -> API HTTP -> modulo3-front
```

- A integracao externa envia pacotes no formato padrao para as portas TCP/UDP do backend.
- O backend processa os modulos e agrega o estado atual/historico.
- O frontend consulta a API por polling e atualiza a dashboard.

## Requisitos

Backend:

- .NET SDK 10

Frontend:

- Node.js 20+
- npm

## Portas e protocolos

- API HTTP: `http://localhost:5151`
- API HTTPS: `https://localhost:7012`
- Ingestao TCP: `5555`
- Ingestao UDP: `5002`
- Envio de comando (UDP broadcast): `255.255.255.255:5010`
- Simulador modulo 6 (teste): TCP `5006`

## Como executar (desenvolvimento)

1. Subir backend:

```bash
cd modulo3-back
dotnet restore
dotnet run --project ControlApi/ControlApi.csproj
```

2. Subir frontend:

```bash
cd modulo3-front
npm install
npm run dev
```

3. (Opcional) Subir simuladores de teste:

```bash
cd modulo3-back
dotnet run --project Test/Test.csproj
```

## URLs uteis

- Frontend: `http://localhost:3000`
- Swagger/API: `http://localhost:5151/`
- Health check: `http://localhost:5151/health`

## Configuracao do frontend

Arquivo `modulo3-front/.env.local`:

```env
NEXT_PUBLIC_API_URL=http://localhost:5151
```

## Endpoints da API

Monitoramento:

- `GET /api/Monitoring/realtime`
- `GET /api/Monitoring/historical/{deviceId}?startTime=...&endTime=...`
- `GET /api/Monitoring/devices`
- `GET /api/Monitoring/events/active`
- `GET /api/Monitoring/events/reports`
- `GET /api/Monitoring/alarms`

Comandos:

- `POST /api/Command/send`
- `GET /api/Command/status/{deviceId}`

Sistema:

- `GET /health`

## Formato de pacote para ingestao externa (TCP e UDP)

Formato base:

```text
ORIGIN;SEQUENCE;MODULE;OPERATION_TYPE;DATA_JSON;TIMESTAMP_ISO8601
```

Exemplo:

```text
IED-001;42;MODULE1;MEASUREMENT;{"Voltage":220.5,"Current":10.2,"Frequency":60.0,"PowerFactor":0.98,"Status":"NORMAL"};2026-03-05T12:00:00.0000000Z
```

Regras:

- `SEQUENCE`: numero inteiro (`long`).
- `MODULE`: `MODULE1`, `MODULE2`, `MODULE3`, `MODULE4`, `MODULE5` ou `MODULE6`.
- `TIMESTAMP`: data parseavel (recomendado ISO 8601 UTC).

## Modulos e operacoes processadas

- `MODULE1`: medicoes eletricas.
- `MODULE2`: eventos de protecao (`EVENT_START` / `EVENT_END`).
- `MODULE4`: relatorios de eventos.
- `MODULE5`: alarmes agregados.
- `MODULE6`: atualizacao de estado de chave/disjuntor.

## Integracao externa

Existem duas formas praticas.

### 1) Reaproveitar os metodos ja implementados

A classe `modulo3-back/Test/ModuleSimulator.cs` ja possui metodos prontos:

- `SendMeasurement(...)`
- `SendProtectionEvent(...)`
- `SendEventReport(...)`
- `SendAggregatedAlarm(...)`
- `SendStateUpdate(...)`

E metodos de transporte:

- `SendViaUdp(...)`
- `SendViaTcp(...)`

Exemplo (C#):

```csharp
var simulatorUdp = new ModuleSimulator("EXT-MODULE", "127.0.0.1", 5002, useTcp: false);
var simulatorTcp = new ModuleSimulator("EXT-MODULE", "127.0.0.1", 5555, useTcp: true);

await simulatorUdp.SendMeasurement("IED-EXT-01", 221.3, 14.2, 60.0, 0.97);
await simulatorTcp.SendProtectionEvent("IED-EXT-01", "OVERCURRENT", "HIGH", isStart: true);
await simulatorUdp.SendAggregatedAlarm("VOLTAGE_DROP", 35, -18.9123, -48.2755);
```

### 2) Implementar um gerador proprio

Passos minimos:

1. Gerar pacote no formato `ORIGIN;SEQUENCE;MODULE;OPERATION;JSON;TIMESTAMP`.
2. Enviar para `UDP 5002` ou `TCP 5555`.
3. Variar dados/modulos em loop para testes de carga e comportamento.

Exemplo (Python):

```python
import json, random, socket, time
from datetime import datetime, timezone

USE_TCP = False
seq = 0

def build_packet():
    global seq
    seq += 1
    data = {
        "Voltage": round(random.uniform(215, 230), 2),
        "Current": round(random.uniform(10, 20), 2),
        "Frequency": round(random.uniform(59.8, 60.2), 3),
        "PowerFactor": round(random.uniform(0.9, 1.0), 3),
        "Status": "NORMAL"
    }
    ts = datetime.now(timezone.utc).isoformat()
    return f"IED-TEST;{seq};MODULE1;MEASUREMENT;{json.dumps(data)};{ts}"

while True:
    payload = build_packet().encode()
    if USE_TCP:
        with socket.create_connection(("127.0.0.1", 5555), timeout=3) as s:
            s.sendall(payload)
    else:
        with socket.socket(socket.AF_INET, socket.SOCK_DGRAM) as s:
            s.sendto(payload, ("127.0.0.1", 5002))
    time.sleep(1)
```

## Fluxo de validacao rapida

1. Suba backend e frontend.
2. Envie pacotes com os simuladores ou integracao externa.
3. Verifique na dashboard:

- dispositivos ativos
- medicoes variando
- eventos/alarmes preenchidos

4. Valide tambem via API:

- `GET /api/Monitoring/realtime`
- `GET /api/Monitoring/devices`
- `GET /api/Monitoring/alarms`

## Observacoes tecnicas

- O receiver tambem aceita JSON bruto de `MODULE5` com os campos `critical_event_id`, `critical_event_type`, `local`, `timestamp`, `cluster_size`, normalizando para o formato interno.
- Hoje as portas de ingestao TCP/UDP estao definidas diretamente no servico (`5555` e `5002`).
