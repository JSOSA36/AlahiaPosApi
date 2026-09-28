# -*- coding: utf-8 -*-
"""Generate and apply Terraza 55 exact-PDF catalog alignment (Prod)."""
from __future__ import annotations

import argparse
import re
import shutil
import subprocess
from pathlib import Path

CATALOG_SQL = Path(__file__).with_name("Prod_Catalogo_Terraza55_Listado.sql")
DELETE_SQL = Path(__file__).with_name("Prod_Borrar_Facturas_Sobrante_Terraza55.sql")
SQLCMD = shutil.which("sqlcmd") or r"C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\SQLCMD.EXE"


def parse_catalog(path: Path) -> list[tuple[str, str]]:
    text = path.read_text(encoding="utf-8")
    start = text.index("INSERT INTO #Cat (Nombre, BarCode")
    chunk = text[start:]
    chunk = chunk.split(";\n", 1)[0]
    rows = re.findall(r"\(N'(.*?)',\s*N'(.*?)',", chunk, flags=re.S)
    if len(rows) < 300:
        raise SystemExit(f"Catalog parse too small: {len(rows)}")
    out = []
    for nombre, barcode in rows:
        out.append((nombre.replace("''", "'").strip(), barcode.replace("''", "'").strip()))
    return out


def sql_n(value: str) -> str:
    return "N'" + value.replace("'", "''") + "'"


def write_delete_sql(catalog: list[tuple[str, str]]) -> None:
    values = ",\n".join(f"({sql_n(n)}, {sql_n(b)})" for n, b in catalog)
    DELETE_SQL.write_text(
        f"""/*
  Terraza 55: borrar facturas con productos fuera de ListadoProductos.pdf
  y esos productos. Backup: Music\\\\Terraza-Facturas-Backup-2026-09-03.pdf
*/
USE AlahiaPos_Prod;
GO
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
GO

IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Prod.', 16, 1);
    RETURN;
END;

IF NOT EXISTS (
    SELECT 1 FROM dbo.Empresas
    WHERE IdEmpresa = 55 AND NombreComercial LIKE N'Terraza%'
)
BEGIN
    RAISERROR(N'IdEmpresa 55 no es Terraza 27.', 16, 1);
    RETURN;
END;

IF OBJECT_ID('tempdb..#Cat') IS NOT NULL DROP TABLE #Cat;
CREATE TABLE #Cat (Nombre NVARCHAR(200) NOT NULL, BarCode VARCHAR(30) NOT NULL);
INSERT INTO #Cat (Nombre, BarCode) VALUES
{values};

IF OBJECT_ID('tempdb..#Extra') IS NOT NULL DROP TABLE #Extra;
SELECT p.IdProducto, p.Nombre, ISNULL(p.CodigoBarra,'') CodigoBarra
INTO #Extra
FROM dbo.Productos p
WHERE p.IdEmpresa = 55
  AND NOT EXISTS (
      SELECT 1 FROM #Cat c
      WHERE (c.BarCode <> '' AND p.CodigoBarra = c.BarCode)
         OR LOWER(LTRIM(RTRIM(p.Nombre))) = LOWER(LTRIM(RTRIM(c.Nombre)))
  );

IF OBJECT_ID('tempdb..#Fac') IS NOT NULL DROP TABLE #Fac;
SELECT DISTINCT h.IdFacturaHeader
INTO #Fac
FROM dbo.FacturaHeaders h
INNER JOIN dbo.FacturaDetalles d ON d.IdFacturaHeader = h.IdFacturaHeader
INNER JOIN #Extra x ON x.IdProducto = d.IdProducto
WHERE h.IdEmpresa = 55;

SELECT 'AntesFacturas' t, COUNT(*) c FROM #Fac
UNION ALL SELECT 'AntesExtras', COUNT(*) FROM #Extra
UNION ALL SELECT 'AntesDetalles', COUNT(*) FROM dbo.FacturaDetalles d INNER JOIN #Fac f ON f.IdFacturaHeader = d.IdFacturaHeader;

BEGIN TRAN;

UPDATE p SET p.IdMovimientoFinanciero = NULL
FROM dbo.PagosFacturasClientes p
INNER JOIN #Fac f ON f.IdFacturaHeader = p.IdFacturaHeader;

UPDATE g SET g.IdMovimientoFinanciero = NULL
FROM dbo.Ingresos g
INNER JOIN #Fac f ON f.IdFacturaHeader = g.IdFacturaHeader;

DELETE m
FROM dbo.MovimientoFinanciero m
INNER JOIN #Fac f ON f.IdFacturaHeader = m.ReferenciaId
WHERE m.IdEmpresa = 55
  AND m.ReferenciaTipo IN (N'FACTURA', N'Factura', N'VENTA', N'Venta');

DELETE p
FROM dbo.PagosFacturasClientes p
INNER JOIN #Fac f ON f.IdFacturaHeader = p.IdFacturaHeader;

DELETE g
FROM dbo.Ingresos g
INNER JOIN #Fac f ON f.IdFacturaHeader = g.IdFacturaHeader;

DELETE d
FROM dbo.MovimientosInventarioDetalle d
INNER JOIN dbo.MovimientosInventario m ON m.Id = d.IdMovimientoInventario
INNER JOIN #Fac f ON m.Referencia = N'Factura #' + CONVERT(varchar(12), f.IdFacturaHeader)
WHERE m.IdEmpresa = 55;

DELETE m
FROM dbo.MovimientosInventario m
INNER JOIN #Fac f ON m.Referencia = N'Factura #' + CONVERT(varchar(12), f.IdFacturaHeader)
WHERE m.IdEmpresa = 55;

DELETE d
FROM dbo.FacturaDetalles d
INNER JOIN #Fac f ON f.IdFacturaHeader = d.IdFacturaHeader;

DELETE h
FROM dbo.FacturaHeaders h
INNER JOIN #Fac f ON f.IdFacturaHeader = h.IdFacturaHeader
WHERE h.IdEmpresa = 55;

DELETE d
FROM dbo.MovimientosInventarioDetalle d
INNER JOIN #Extra x ON x.IdProducto = d.IdProducto;

DELETE m
FROM dbo.MovimientosInventario m
WHERE m.IdEmpresa = 55
  AND NOT EXISTS (
      SELECT 1 FROM dbo.MovimientosInventarioDetalle d
      WHERE d.IdMovimientoInventario = m.Id
  );

IF OBJECT_ID(N'dbo.PaqueteDetalles', N'U') IS NOT NULL
    DELETE d FROM dbo.PaqueteDetalles d
    WHERE d.IdProducto IN (SELECT IdProducto FROM #Extra)
       OR d.IdProductoPadre IN (SELECT IdProducto FROM #Extra);

IF OBJECT_ID(N'dbo.RecetasDetalles', N'U') IS NOT NULL
    DELETE d FROM dbo.RecetasDetalles d INNER JOIN #Extra x ON x.IdProducto = d.IdProducto;

IF OBJECT_ID(N'dbo.DescuentoDetalle', N'U') IS NOT NULL
    DELETE d FROM dbo.DescuentoDetalle d INNER JOIN #Extra x ON x.IdProducto = d.IdProducto;

IF OBJECT_ID(N'dbo.Citas', N'U') IS NOT NULL
    UPDATE c SET c.IdProducto = NULL
    FROM dbo.Citas c INNER JOIN #Extra x ON x.IdProducto = c.IdProducto;

DELETE e FROM dbo.AlmacenExistencias e INNER JOIN #Extra x ON x.IdProducto = e.IdProducto;
DELETE v FROM dbo.Variaciones v INNER JOIN #Extra x ON x.IdProducto = v.IdProducto;
DELETE c FROM dbo.EmpleadoServicioComisions c INNER JOIN #Extra x ON x.IdProducto = c.IdProducto;

DELETE p
FROM dbo.Productos p
INNER JOIN #Extra x ON x.IdProducto = p.IdProducto
WHERE p.IdEmpresa = 55;

COMMIT TRAN;

SELECT 'FacturasRestantesSobrante' t, COUNT(*) c
FROM dbo.FacturaHeaders h
INNER JOIN dbo.FacturaDetalles d ON d.IdFacturaHeader = h.IdFacturaHeader
INNER JOIN dbo.Productos p ON p.IdProducto = d.IdProducto
WHERE h.IdEmpresa = 55
  AND NOT EXISTS (
      SELECT 1 FROM #Cat c
      WHERE (c.BarCode <> '' AND p.CodigoBarra = c.BarCode)
         OR LOWER(LTRIM(RTRIM(p.Nombre))) = LOWER(LTRIM(RTRIM(c.Nombre)))
  )
UNION ALL SELECT 'ExtrasRestantes', COUNT(*) FROM dbo.Productos p
WHERE p.IdEmpresa = 55
  AND NOT EXISTS (
      SELECT 1 FROM #Cat c
      WHERE (c.BarCode <> '' AND p.CodigoBarra = c.BarCode)
         OR LOWER(LTRIM(RTRIM(p.Nombre))) = LOWER(LTRIM(RTRIM(c.Nombre)))
  )
UNION ALL SELECT 'ProductosEmpresa', COUNT(*) FROM dbo.Productos WHERE IdEmpresa = 55;
GO
""",
        encoding="utf-8",
    )


def run_sqlcmd(server: str, user: str, password: str, sql_path: Path) -> str:
    out_path = Path(r"C:\Users\USUARIO\AppData\Local\Temp") / (sql_path.stem + "_out.txt")
    cmd = [
        SQLCMD,
        "-S", server,
        "-U", user,
        "-P", password,
        "-C",
        "-d", "AlahiaPos_Prod",
        "-b",
        "-i", str(sql_path),
        "-o", str(out_path),
        "-f", "65001",
        "-W",
    ]
    proc = subprocess.run(cmd, capture_output=True, text=True)
    raw = out_path.read_text(encoding="utf-8-sig", errors="replace") if out_path.exists() else ""
    if proc.returncode != 0:
        raise SystemExit(f"sqlcmd {sql_path.name} failed ({proc.returncode}): {proc.stderr}\n{raw[-4000:]}")
    if "Msg " in raw and ("Level 16" in raw or "Level 15" in raw):
        raise SystemExit(f"SQL error in {sql_path.name}:\n{raw[-4000:]}")
    return raw


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--server", default=r"144.126.143.154\SQLEXPRESS,1433")
    ap.add_argument("--user", default="sa")
    ap.add_argument("--password", default="")
    ap.add_argument("--apply", action="store_true")
    args = ap.parse_args()

    catalog = parse_catalog(CATALOG_SQL)
    write_delete_sql(catalog)
    print(f"Wrote {DELETE_SQL} catalog_rows={len(catalog)}")
    if not args.apply:
        return
    if not args.password:
        raise SystemExit("--password required with --apply")

    print("=== DELETE ===")
    print(run_sqlcmd(args.server, args.user, args.password, DELETE_SQL))
    print("=== CATALOG ===")
    print(run_sqlcmd(args.server, args.user, args.password, CATALOG_SQL))


if __name__ == "__main__":
    main()
