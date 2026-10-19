#!/bin/bash
# Prueba de humo contra la API levantada con docker compose.
# Uso: DEMO_PASSWORD=... ./docker/smoke-test.sh [http://localhost:8080]
set -euo pipefail

BASE="${1:-http://localhost:8080}"
API="$BASE/api"
FALLOS=0

esperar_api() {
  echo "Esperando a que la API responda en $BASE/health..."
  for _ in $(seq 1 60); do
    if [ "$(curl -s -o /dev/null -w '%{http_code}' "$BASE/health")" = "200" ]; then
      echo "API lista."
      return 0
    fi
    sleep 3
  done
  echo "La API no respondió a tiempo."
  return 1
}

login() {
  curl -s -X POST "$API/auth/login" -H "Content-Type: application/json" \
    -d "{\"nombreUsuario\":\"$1\",\"password\":\"$DEMO_PASSWORD\"}" \
    | grep -o '"token":"[^"]*"' | cut -d'"' -f4
}

# verificar "descripción" código_esperado método url [token]
verificar() {
  local descripcion=$1 esperado=$2 metodo=$3 url=$4 token=${5:-}
  local args=(-s -o /dev/null -w '%{http_code}' -X "$metodo" "$API$url" -H "Content-Type: application/json")
  [ -n "$token" ] && args+=(-H "Authorization: Bearer $token")
  [ "$metodo" = "POST" ] && args+=(-d '{}')

  local obtenido
  obtenido=$(curl "${args[@]}")
  if [ "$obtenido" = "$esperado" ]; then
    echo "  OK    $descripcion ($obtenido)"
  else
    echo "  FALLO $descripcion: se esperaba $esperado y llegó $obtenido"
    FALLOS=$((FALLOS + 1))
  fi
}

esperar_api

ADMIN=$(login admin)
CAJERO=$(login cajero1)
[ -n "$ADMIN" ] && [ -n "$CAJERO" ] || { echo "FALLO: no se pudo iniciar sesión"; exit 1; }
echo "Login de admin y cajero: OK"

verificar "Sin token no hay acceso"              401 GET  /clientes
verificar "Admin lista clientes"                 200 GET  /clientes "$ADMIN"
verificar "Cajero lista clientes"                200 GET  /clientes "$CAJERO"
verificar "Admin ve la cartera (SP)"             200 GET  /reportes/cartera "$ADMIN"
verificar "Cajero no ve la cartera"              403 GET  /reportes/cartera "$CAJERO"
verificar "Estado de cuenta del cliente 1 (SP)"  200 GET  /clientes/1/estado-cuenta "$ADMIN"
verificar "Deuda de PR-000002 con mora"          200 GET  /prestamos/2/deuda "$CAJERO"
verificar "Cajero no puede aprobar"              403 POST /prestamos/5/aprobar "$CAJERO"
verificar "No se aprueba dos veces (PR-000006)"  422 POST /prestamos/6/aprobar "$ADMIN"

if [ "$FALLOS" -gt 0 ]; then
  echo "$FALLOS verificación(es) fallaron."
  exit 1
fi
echo "Prueba de humo superada."
