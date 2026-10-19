#!/bin/bash
# Crea la base PrestaFacilDB con los scripts de /database la primera vez.
# Si la base ya existe, no hace nada (así no se pierden datos al reiniciar).
set -euo pipefail

SQLCMD=(/opt/mssql-tools18/bin/sqlcmd -S db -U sa -P "$MSSQL_SA_PASSWORD" -C -b -f 65001)

echo "Esperando a SQL Server..."
for i in $(seq 1 60); do
  if "${SQLCMD[@]}" -Q "SELECT 1" > /dev/null 2>&1; then break; fi
  sleep 2
done

EXISTE=$("${SQLCMD[@]}" -h -1 -W -Q "SET NOCOUNT ON; SELECT CASE WHEN DB_ID('PrestaFacilDB') IS NULL THEN 0 ELSE 1 END")
if [ "$EXISTE" = "1" ]; then
  echo "PrestaFacilDB ya existe; no se ejecutan los scripts."
  exit 0
fi

for script in 01_schema.sql 02_procedures.sql 03_seed.sql; do
  echo "Ejecutando $script"
  "${SQLCMD[@]}" -i "/database/$script"
done
echo "Base de datos lista."
