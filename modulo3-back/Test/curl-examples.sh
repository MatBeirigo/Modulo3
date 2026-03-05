#!/bin/bash

API_URL="http://localhost:5000/api"

echo "=== Testes da Control API ==="
echo ""

echo "1. Obter dados em tempo real"
curl -X GET "$API_URL/monitoring/realtime" | jq
echo ""

echo "2. Obter histórico do dispositivo IED-001"
curl -X GET "$API_URL/monitoring/historical/IED-001?startTime=2026-02-24T00:00:00Z&endTime=2026-02-24T23:59:59Z" | jq
echo ""

echo "3. Obter status de todos os dispositivos"
curl -X GET "$API_URL/monitoring/devices" | jq
echo ""

echo "4. Obter eventos ativos"
curl -X GET "$API_URL/monitoring/events/active" | jq
echo ""

echo "5. Obter relatórios de eventos"
curl -X GET "$API_URL/monitoring/events/reports" | jq
echo ""

echo "6. Obter alarmes agregados"
curl -X GET "$API_URL/monitoring/alarms" | jq
echo ""

echo "7. Enviar comando para abrir chave"
curl -X POST "$API_URL/command/send" \
  -H "Content-Type: application/json" \
  -d '{
    "deviceId": "SWITCH-001",
    "commandType": "OPEN",
    "targetState": "OPEN"
  }' | jq
echo ""

echo "8. Verificar status do comando"
curl -X GET "$API_URL/command/status/SWITCH-001" | jq
echo ""