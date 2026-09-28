# -*- coding: utf-8 -*-
"""Backup PDF of Terraza 55 invoices that block catalog alignment to ListadoProductos.pdf."""
from __future__ import annotations

import argparse
import csv
import shutil
import re
import subprocess
import sys
from collections import defaultdict
from datetime import datetime
from pathlib import Path

from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER, TA_LEFT, TA_RIGHT
from reportlab.lib.pagesizes import letter
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import inch
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (
    PageBreak,
    Paragraph,
    SimpleDocTemplate,
    Spacer,
    Table,
    TableStyle,
)

CATALOG_SQL = Path(__file__).with_name("Prod_Catalogo_Terraza55_Listado.sql")
SQLCMD = shutil.which("sqlcmd") or r"C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\SQLCMD.EXE"
SEP = "|"


def parse_catalog(path: Path) -> list[tuple[str, str]]:
    text = path.read_text(encoding="utf-8")
    start = text.index("INSERT INTO #Cat (Nombre, BarCode")
    chunk = text[start:]
    chunk = chunk.split(";\n", 1)[0]
    rows = re.findall(r"\(N'(.*?)',\s*N'(.*?)',", chunk, flags=re.S)
    if len(rows) < 300:
        raise SystemExit(f"Catalog parse too small: {len(rows)} rows")
    out = []
    for nombre, barcode in rows:
        out.append((nombre.replace("''", "'").strip(), barcode.replace("''", "'").strip()))
    return out


def sql_literal(value: str) -> str:
    return "N'" + value.replace("'", "''") + "'"


def run_sqlcmd(server: str, user: str, password: str, database: str, query: str) -> str:
    sql_path = Path(r"C:\Users\USUARIO\AppData\Local\Temp\terraza55_backup_query.sql")
    sql_path.write_text(query, encoding="utf-8-sig")
    out_path = Path(r"C:\Users\USUARIO\AppData\Local\Temp\terraza55_backup_out.txt")
    if out_path.exists():
        out_path.unlink()
    cmd = [
        SQLCMD,
        "-S", server,
        "-U", user,
        "-P", password,
        "-C",
        "-d", database,
        "-h", "-1",
        "-W",
        "-s", "|",
        "-f", "65001",
        "-i", str(sql_path),
        "-o", str(out_path),
    ]
    proc = subprocess.run(cmd, capture_output=True, text=True)
    raw = out_path.read_text(encoding="utf-8-sig", errors="replace") if out_path.exists() else ""
    if proc.returncode != 0:
        raise SystemExit(f"sqlcmd failed ({proc.returncode}): {proc.stderr}\n{raw[-2000:]}")
    if "Msg " in raw and "Level 16" in raw:
        raise SystemExit(f"SQL error:\n{raw[-3000:]}")
    return raw


def parse_sections(raw: str) -> dict[str, list[list[str]]]:
    sections: dict[str, list[list[str]]] = defaultdict(list)
    current = None
    for line in raw.splitlines():
        line = line.strip("\ufeff").rstrip()
        if not line or line.startswith("Changed database") or line.startswith("("):
            continue
        if line.startswith("Msg ") or line.startswith("Warning:") or set(line) <= {"-", " ", "|"}:
            continue
        parts = [p.strip() for p in line.split(SEP)]
        if not parts:
            continue
        tag = parts[0]
        if tag in ("META", "EXTRA", "HDR", "DET", "PAGO", "ECF", "SUM"):
            current = tag
            sections[tag].append(parts[1:])
        elif current:
            # wrapped continuation — ignore, we used -w 65535
            pass
    return sections


def money(n: float) -> str:
    return f"RD$ {n:,.2f}"


def register_fonts() -> tuple[str, str]:
    arial = Path(r"C:\Windows\Fonts\arial.ttf")
    arial_bd = Path(r"C:\Windows\Fonts\arialbd.ttf")
    if arial.exists() and arial_bd.exists():
        pdfmetrics.registerFont(TTFont("ArialT", str(arial)))
        pdfmetrics.registerFont(TTFont("ArialT-Bold", str(arial_bd)))
        return "ArialT", "ArialT-Bold"
    return "Helvetica", "Helvetica-Bold"


def build_pdf(
    pdf_path: Path,
    meta: dict[str, str],
    extras: list[list[str]],
    headers: list[dict],
    details: dict[int, list[list[str]]],
    pagos: dict[int, list[list[str]]],
    ecfs: dict[int, list[list[str]]],
) -> None:
    font, font_b = register_fonts()
    styles = getSampleStyleSheet()
    styles.add(ParagraphStyle(name="CoverTitle", fontName=font_b, fontSize=16, leading=20, alignment=TA_CENTER, textColor=colors.HexColor("#1B3A2F")))
    styles.add(ParagraphStyle(name="CoverSub", fontName=font, fontSize=10, leading=14, alignment=TA_CENTER, textColor=colors.HexColor("#333333")))
    styles.add(ParagraphStyle(name="H1", fontName=font_b, fontSize=12, leading=15, textColor=colors.HexColor("#1B3A2F")))
    styles.add(ParagraphStyle(name="Body", fontName=font, fontSize=9, leading=12))
    styles.add(ParagraphStyle(name="Small", fontName=font, fontSize=8, leading=11, textColor=colors.HexColor("#444444")))
    styles.add(ParagraphStyle(name="InvTitle", fontName=font_b, fontSize=13, leading=16, textColor=colors.HexColor("#1B3A2F")))
    styles.add(ParagraphStyle(name="InvMeta", fontName=font, fontSize=8.5, leading=11))
    styles.add(ParagraphStyle(name="Cell", fontName=font, fontSize=8, leading=10))
    styles.add(ParagraphStyle(name="CellB", fontName=font_b, fontSize=8, leading=10))
    styles.add(ParagraphStyle(name="Warn", fontName=font, fontSize=8.5, leading=11, textColor=colors.HexColor("#7A1F1F")))
    styles.add(ParagraphStyle(name="Right", fontName=font, fontSize=8.5, leading=11, alignment=TA_RIGHT))
    styles.add(ParagraphStyle(name="RightB", fontName=font_b, fontSize=9, leading=12, alignment=TA_RIGHT))

    doc = SimpleDocTemplate(
        str(pdf_path),
        pagesize=letter,
        leftMargin=0.6 * inch,
        rightMargin=0.6 * inch,
        topMargin=0.55 * inch,
        bottomMargin=0.55 * inch,
        title="Backup facturas Terraza 27 — alineación catálogo",
        author="Alahia ERP",
    )
    story = []

    story.append(Paragraph("BACKUP DE FACTURAS", styles["CoverTitle"]))
    story.append(Spacer(1, 6))
    story.append(Paragraph(meta.get("empresa", "Terraza 27 (MATBERT SRL)"), styles["CoverSub"]))
    story.append(Paragraph(f"RNC {meta.get('rnc', '')} · IdEmpresa {meta.get('idEmpresa', '55')}", styles["CoverSub"]))
    story.append(Spacer(1, 10))
    story.append(Paragraph(
        "Respaldo generado antes de eliminar productos que no están en el listado "
        "<b>ListadoProductos.pdf</b> y las facturas que los contienen, para dejar el catálogo "
        "exactamente igual al PDF.",
        styles["Body"],
    ))
    story.append(Spacer(1, 8))
    story.append(Paragraph(
        f"Generado: {datetime.now().strftime('%d/%m/%Y %H:%M')} · "
        f"Facturas: {len(headers)} · Productos fuera del listado: {len(extras)}",
        styles["Small"],
    ))

    ncf_e = sum(1 for h in headers if (h.get("ncf") or "").upper().startswith("E"))
    ncf_b = sum(1 for h in headers if (h.get("ncf") or "").upper().startswith("B"))
    if ncf_e or any(ecfs.values()):
        story.append(Spacer(1, 8))
        story.append(Paragraph(
            "Aviso: borrar estas facturas en el ERP no anula el comprobante en DGII. "
            "Si alguna ya fue enviada/aceptada, queda el registro fiscal en DGII aunque desaparezca aquí.",
            styles["Warn"],
        ))

    story.append(Spacer(1, 14))
    story.append(Paragraph("Resumen", styles["H1"]))
    total = sum(h["total"] for h in headers)
    itbis = sum(h["itbis"] for h in headers)
    sum_data = [
        [Paragraph("<b>Concepto</b>", styles["CellB"]), Paragraph("<b>Valor</b>", styles["CellB"])],
        [Paragraph("Facturas respaldadas", styles["Cell"]), Paragraph(str(len(headers)), styles["Cell"])],
        [Paragraph("Total facturado (suma)", styles["Cell"]), Paragraph(money(total), styles["Cell"])],
        [Paragraph("ITBIS (suma)", styles["Cell"]), Paragraph(money(itbis), styles["Cell"])],
        [Paragraph("Con NCF electrónico (E…)", styles["Cell"]), Paragraph(str(ncf_e), styles["Cell"])],
        [Paragraph("Con NCF tradicional (B…)", styles["Cell"]), Paragraph(str(ncf_b), styles["Cell"])],
        [Paragraph("Sin NCF", styles["Cell"]), Paragraph(str(sum(1 for h in headers if not (h.get("ncf") or "").strip())), styles["Cell"])],
    ]
    t = Table(sum_data, colWidths=[4.4 * inch, 2.4 * inch])
    t.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#1B3A2F")),
        ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
        ("BACKGROUND", (0, 1), (-1, -1), colors.HexColor("#F4F7F5")),
        ("GRID", (0, 0), (-1, -1), 0.3, colors.HexColor("#C5D1CB")),
        ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
        ("LEFTPADDING", (0, 0), (-1, -1), 6),
        ("RIGHTPADDING", (0, 0), (-1, -1), 6),
        ("TOPPADDING", (0, 0), (-1, -1), 4),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
    ]))
    story.append(t)

    story.append(Spacer(1, 14))
    story.append(Paragraph("Productos que no están en el listado (motivo del backup)", styles["H1"]))
    extra_rows = [[
        Paragraph("<b>Id</b>", styles["CellB"]),
        Paragraph("<b>Producto</b>", styles["CellB"]),
        Paragraph("<b>Barcode</b>", styles["CellB"]),
        Paragraph("<b>Facturas</b>", styles["CellB"]),
        Paragraph("<b>Líneas</b>", styles["CellB"]),
    ]]
    for row in extras:
        extra_rows.append([
            Paragraph(row[0], styles["Cell"]),
            Paragraph(row[1], styles["Cell"]),
            Paragraph(row[2], styles["Cell"]),
            Paragraph(row[3], styles["Cell"]),
            Paragraph(row[4], styles["Cell"]),
        ])
    et = Table(extra_rows, colWidths=[0.6 * inch, 3.3 * inch, 1.6 * inch, 0.7 * inch, 0.6 * inch])
    et.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#1B3A2F")),
        ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
        ("GRID", (0, 0), (-1, -1), 0.3, colors.HexColor("#C5D1CB")),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("LEFTPADDING", (0, 0), (-1, -1), 4),
        ("RIGHTPADDING", (0, 0), (-1, -1), 4),
        ("TOPPADDING", (0, 0), (-1, -1), 3),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 3),
        ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, colors.HexColor("#F4F7F5")]),
    ]))
    story.append(et)

    story.append(Spacer(1, 14))
    story.append(Paragraph("Índice de facturas", styles["H1"]))
    idx = [[
        Paragraph("<b>Documento</b>", styles["CellB"]),
        Paragraph("<b>Fecha</b>", styles["CellB"]),
        Paragraph("<b>Cliente</b>", styles["CellB"]),
        Paragraph("<b>NCF / e-CF</b>", styles["CellB"]),
        Paragraph("<b>Estado</b>", styles["CellB"]),
        Paragraph("<b>Total</b>", styles["CellB"]),
    ]]
    for h in headers:
        ncf = h.get("ncf") or ""
        encf = h.get("encf") or ""
        fiscal = encf or ncf or "(sin NCF)"
        idx.append([
            Paragraph(h["doc"] or f"#{h['id']}", styles["Cell"]),
            Paragraph(h["fecha"], styles["Cell"]),
            Paragraph(h["cliente"] or "Consumidor final", styles["Cell"]),
            Paragraph(fiscal, styles["Cell"]),
            Paragraph(h["estado"], styles["Cell"]),
            Paragraph(money(h["total"]), styles["Cell"]),
        ])
    it = Table(idx, colWidths=[0.95 * inch, 1.15 * inch, 1.7 * inch, 1.5 * inch, 0.7 * inch, 0.8 * inch])
    it.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#1B3A2F")),
        ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
        ("GRID", (0, 0), (-1, -1), 0.3, colors.HexColor("#C5D1CB")),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("LEFTPADDING", (0, 0), (-1, -1), 3),
        ("RIGHTPADDING", (0, 0), (-1, -1), 3),
        ("TOPPADDING", (0, 0), (-1, -1), 3),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 3),
        ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, colors.HexColor("#F4F7F5")]),
        ("ALIGN", (-1, 1), (-1, -1), "RIGHT"),
    ]))
    story.append(it)

    for h in headers:
        story.append(PageBreak())
        story.append(Paragraph(h["doc"] or f"Factura #{h['id']}", styles["InvTitle"]))
        story.append(Paragraph(meta.get("empresa", ""), styles["Small"]))
        story.append(Spacer(1, 6))
        meta_txt = (
            f"<b>Id interno:</b> {h['id']} &nbsp;&nbsp; "
            f"<b>Fecha:</b> {h['fecha']} {h['hora']} &nbsp;&nbsp; "
            f"<b>Estado:</b> {h['estado']}<br/>"
            f"<b>Cliente:</b> {h['cliente'] or 'Consumidor final'} &nbsp;&nbsp; "
            f"<b>RNC/Cédula:</b> {h['clienteRnc'] or '—'}<br/>"
            f"<b>NCF:</b> {h['ncf'] or '—'} &nbsp;&nbsp; "
            f"<b>e-CF:</b> {h['encf'] or '—'} &nbsp;&nbsp; "
            f"<b>TrackId:</b> {h['trackId'] or '—'}<br/>"
            f"<b>Estado DGII:</b> {h['estadoDgii'] or '—'} &nbsp;&nbsp; "
            f"<b>Forma de pago:</b> {h['formaPago'] or '—'} &nbsp;&nbsp; "
            f"<b>Cajero:</b> {h['usuario'] or '—'}"
        )
        if h.get("nota"):
            meta_txt += f"<br/><b>Nota:</b> {h['nota']}"
        story.append(Paragraph(meta_txt, styles["InvMeta"]))
        story.append(Spacer(1, 8))

        lines = details.get(h["id"], [])
        det_rows = [[
            Paragraph("<b>#</b>", styles["CellB"]),
            Paragraph("<b>Producto</b>", styles["CellB"]),
            Paragraph("<b>Cant.</b>", styles["CellB"]),
            Paragraph("<b>Precio</b>", styles["CellB"]),
            Paragraph("<b>ITBIS</b>", styles["CellB"]),
            Paragraph("<b>Subtotal</b>", styles["CellB"]),
            Paragraph("<b>Fuera PDF</b>", styles["CellB"]),
        ]]
        for i, d in enumerate(lines, 1):
            fuera = "SÍ" if d[7] == "1" else ""
            det_rows.append([
                Paragraph(str(i), styles["Cell"]),
                Paragraph(f"{d[1]}<br/><font size='7' color='#666666'>{d[2]}</font>" if d[2] else d[1], styles["Cell"]),
                Paragraph(d[3], styles["Cell"]),
                Paragraph(money(float(d[4])), styles["Cell"]),
                Paragraph(money(float(d[5])), styles["Cell"]),
                Paragraph(money(float(d[6])), styles["Cell"]),
                Paragraph(fuera, styles["CellB"] if fuera else styles["Cell"]),
            ])
        dt = Table(det_rows, colWidths=[0.35 * inch, 2.7 * inch, 0.6 * inch, 0.9 * inch, 0.85 * inch, 0.9 * inch, 0.7 * inch])
        dt.setStyle(TableStyle([
            ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#1B3A2F")),
            ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
            ("GRID", (0, 0), (-1, -1), 0.3, colors.HexColor("#C5D1CB")),
            ("VALIGN", (0, 0), (-1, -1), "TOP"),
            ("LEFTPADDING", (0, 0), (-1, -1), 3),
            ("RIGHTPADDING", (0, 0), (-1, -1), 3),
            ("TOPPADDING", (0, 0), (-1, -1), 3),
            ("BOTTOMPADDING", (0, 0), (-1, -1), 3),
            ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, colors.HexColor("#F7F3EA")]),
        ]))
        story.append(dt)
        story.append(Spacer(1, 8))

        tot_tbl = Table([
            [Paragraph("Subtotal", styles["Right"]), Paragraph(money(h["subtotal"]), styles["Right"])],
            [Paragraph("ITBIS", styles["Right"]), Paragraph(money(h["itbis"]), styles["Right"])],
            [Paragraph("Descuento", styles["Right"]), Paragraph(money(h["descuento"]), styles["Right"])],
            [Paragraph("<b>TOTAL</b>", styles["RightB"]), Paragraph(f"<b>{money(h['total'])}</b>", styles["RightB"])],
            [Paragraph("Pagado", styles["Right"]), Paragraph(money(h["pagado"]), styles["Right"])],
        ], colWidths=[5.5 * inch, 1.5 * inch])
        tot_tbl.setStyle(TableStyle([
            ("ALIGN", (0, 0), (-1, -1), "RIGHT"),
            ("TOPPADDING", (0, 0), (-1, -1), 2),
            ("BOTTOMPADDING", (0, 0), (-1, -1), 2),
            ("LINEABOVE", (0, 3), (-1, 3), 0.6, colors.HexColor("#1B3A2F")),
        ]))
        story.append(tot_tbl)

        pay = pagos.get(h["id"], [])
        if pay:
            story.append(Spacer(1, 8))
            story.append(Paragraph("Pagos registrados", styles["H1"]))
            pr = [[
                Paragraph("<b>Fecha</b>", styles["CellB"]),
                Paragraph("<b>Forma</b>", styles["CellB"]),
                Paragraph("<b>Monto</b>", styles["CellB"]),
                Paragraph("<b>Nota</b>", styles["CellB"]),
            ]]
            for p in pay:
                pr.append([
                    Paragraph(p[0], styles["Cell"]),
                    Paragraph(p[1], styles["Cell"]),
                    Paragraph(money(float(p[2])), styles["Cell"]),
                    Paragraph(p[3], styles["Cell"]),
                ])
            pt = Table(pr, colWidths=[1.4 * inch, 1.4 * inch, 1.2 * inch, 3.0 * inch])
            pt.setStyle(TableStyle([
                ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#5A6B63")),
                ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
                ("GRID", (0, 0), (-1, -1), 0.3, colors.HexColor("#C5D1CB")),
                ("LEFTPADDING", (0, 0), (-1, -1), 3),
                ("RIGHTPADDING", (0, 0), (-1, -1), 3),
                ("TOPPADDING", (0, 0), (-1, -1), 3),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 3),
            ]))
            story.append(pt)

        e_rows = ecfs.get(h["id"], [])
        if e_rows:
            story.append(Spacer(1, 6))
            for e in e_rows:
                story.append(Paragraph(
                    f"e-CF {e[0]} · Estado DGII: {e[1]} · Documento: {e[2]} · TrackId: {e[3] or '—'}",
                    styles["Warn"],
                ))

    def footer(canvas, doc_):
        canvas.saveState()
        canvas.setFont(font, 8)
        canvas.setFillColor(colors.HexColor("#666666"))
        canvas.drawString(0.6 * inch, 0.32 * inch, "Backup Terraza 27 · no sustituye el XML/DGII")
        canvas.drawRightString(letter[0] - 0.6 * inch, 0.32 * inch, f"Página {doc_.page}")
        canvas.restoreState()

    doc.build(story, onFirstPage=footer, onLaterPages=footer)


def fnum(s: str) -> float:
    try:
        return float(s.replace(",", ""))
    except Exception:
        return 0.0


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("--server", default=r"144.126.143.154\SQLEXPRESS,1433")
    ap.add_argument("--user", default="sa")
    ap.add_argument("--password", required=True)
    ap.add_argument("--database", default="AlahiaPos_Prod")
    args = ap.parse_args()

    catalog = parse_catalog(CATALOG_SQL)
    values = ",\n".join(f"({sql_literal(n)}, {sql_literal(b)})" for n, b in catalog)

    query = f"""
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
IF DB_NAME() <> N'AlahiaPos_Prod'
BEGIN
    RAISERROR(N'Abortado: solo AlahiaPos_Prod.', 16, 1);
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

SELECT 'META', 'empresa', REPLACE(ISNULL(NombreComercial,''),'|','/'), 'rnc', REPLACE(ISNULL(RNC,''),'|','/'), 'idEmpresa', '55'
FROM dbo.Empresas WHERE IdEmpresa = 55;

SELECT 'EXTRA',
    CONVERT(varchar(12), x.IdProducto),
    REPLACE(REPLACE(REPLACE(ISNULL(x.Nombre,''), CHAR(13), ' '), CHAR(10), ' '), '|', '/'),
    ISNULL(x.CodigoBarra,''),
    CONVERT(varchar(12), (
        SELECT COUNT(DISTINCT d.IdFacturaHeader)
        FROM dbo.FacturaDetalles d
        INNER JOIN #Fac f ON f.IdFacturaHeader = d.IdFacturaHeader
        WHERE d.IdProducto = x.IdProducto
      )),
    CONVERT(varchar(12), (
        SELECT COUNT(*) FROM dbo.FacturaDetalles d
        INNER JOIN #Fac f ON f.IdFacturaHeader = d.IdFacturaHeader
        WHERE d.IdProducto = x.IdProducto
      ))
FROM #Extra x
ORDER BY x.Nombre;

SELECT 'HDR',
    CONVERT(varchar(12), h.IdFacturaHeader),
    REPLACE(ISNULL(h.NumeroDocumento,''),'|','/'),
    REPLACE(ISNULL(h.NCF,''),'|','/'),
    CONVERT(varchar(19), h.FechaInseccion, 120),
    REPLACE(ISNULL(h.Hora,''),'|','/'),
    REPLACE(ISNULL(h.Estado,''),'|','/'),
    REPLACE(ISNULL(h.FormaPago,''),'|','/'),
    REPLACE(REPLACE(REPLACE(ISNULL(c.NombreComercial,''), CHAR(13),' '), CHAR(10),' '), '|','/'),
    REPLACE(ISNULL(c.CedulaRNC,''),'|','/'),
    CONVERT(varchar(20), h.SubTotal),
    CONVERT(varchar(20), h.TotalItbis),
    CONVERT(varchar(20), h.TotalDescuento),
    CONVERT(varchar(20), h.Total),
    CONVERT(varchar(20), h.Pagado),
    REPLACE(REPLACE(REPLACE(ISNULL(h.Nota,''), CHAR(13),' '), CHAR(10),' '), '|','/'),
    REPLACE(ISNULL(u.UserName,''),'|','/'),
    REPLACE(ISNULL(e.ENCF,''),'|','/'),
    REPLACE(ISNULL(e.TrackId,''),'|','/'),
    REPLACE(ISNULL(e.EstadoDGII,''),'|','/')
FROM dbo.FacturaHeaders h
INNER JOIN #Fac f ON f.IdFacturaHeader = h.IdFacturaHeader
LEFT JOIN dbo.Clientes c ON c.IDCliente = h.IDCliente
LEFT JOIN dbo.Usuarios u ON u.IdUsuario = h.IdUsuario
OUTER APPLY (
    SELECT TOP 1 ee.ENCF, ee.TrackId, ee.EstadoDGII
    FROM dbo.ECFEncabezado ee
    WHERE ee.IdEmpresa = 55
      AND (ee.IdOrigen = h.IdFacturaHeader OR ee.IdFacturaInterna = h.IdFacturaHeader)
    ORDER BY ee.IdECF DESC
) e
ORDER BY h.FechaInseccion, h.IdFacturaHeader;

SELECT 'DET',
    CONVERT(varchar(12), d.IdFacturaHeader),
    REPLACE(REPLACE(REPLACE(ISNULL(p.Nombre,''), CHAR(13),' '), CHAR(10),' '), '|','/'),
    ISNULL(p.CodigoBarra,''),
    CONVERT(varchar(20), d.Cantidad),
    CONVERT(varchar(20), d.PrecioOferta),
    CONVERT(varchar(20), d.Itbis),
    CONVERT(varchar(20), d.SubTotal),
    CASE WHEN x.IdProducto IS NULL THEN '0' ELSE '1' END
FROM dbo.FacturaDetalles d
INNER JOIN #Fac f ON f.IdFacturaHeader = d.IdFacturaHeader
LEFT JOIN dbo.Productos p ON p.IdProducto = d.IdProducto
LEFT JOIN #Extra x ON x.IdProducto = d.IdProducto
ORDER BY d.IdFacturaHeader, d.IdFacturaDetalle;

SELECT 'PAGO',
    CONVERT(varchar(12), p.IdFacturaHeader),
    CONVERT(varchar(19), p.FechaInseccion, 120),
    REPLACE(ISNULL(p.FormaPago,''),'|','/'),
    CONVERT(varchar(20), p.Monto),
    REPLACE(REPLACE(REPLACE(ISNULL(p.Nota,''), CHAR(13),' '), CHAR(10),' '), '|','/')
FROM dbo.PagosFacturasClientes p
INNER JOIN #Fac f ON f.IdFacturaHeader = p.IdFacturaHeader
ORDER BY p.IdFacturaHeader, p.Id;

SELECT 'SUM',
    CONVERT(varchar(12), (SELECT COUNT(*) FROM #Fac)),
    CONVERT(varchar(12), (SELECT COUNT(*) FROM #Extra));
"""

    raw = run_sqlcmd(args.server, args.user, args.password, args.database, query)
    sections = parse_sections(raw)
    if not sections.get("HDR"):
        raise SystemExit("No se encontraron facturas a respaldar.\n" + raw[-1500:])

    meta = {"empresa": "Terraza 27", "rnc": "", "idEmpresa": "55"}
    if sections.get("META"):
        m = sections["META"][0]
        # empresa, rnc, idEmpresa pairs after tag stripped: empresa | value | rnc | value | idEmpresa | 55
        kv = {}
        it = iter(m)
        for k in it:
            kv[k] = next(it, "")
        meta.update(kv)

    extras = sections.get("EXTRA", [])
    headers = []
    for row in sections["HDR"]:
        headers.append({
            "id": int(row[0]),
            "doc": row[1],
            "ncf": row[2],
            "fecha": row[3],
            "hora": row[4],
            "estado": row[5],
            "formaPago": row[6],
            "cliente": row[7],
            "clienteRnc": row[8],
            "subtotal": fnum(row[9]),
            "itbis": fnum(row[10]),
            "descuento": fnum(row[11]),
            "total": fnum(row[12]),
            "pagado": fnum(row[13]),
            "nota": row[14] if len(row) > 14 else "",
            "usuario": row[15] if len(row) > 15 else "",
            "encf": row[16] if len(row) > 16 else "",
            "trackId": row[17] if len(row) > 17 else "",
            "estadoDgii": row[18] if len(row) > 18 else "",
        })

    details: dict[int, list[list[str]]] = defaultdict(list)
    for row in sections.get("DET", []):
        details[int(row[0])].append(row)

    pagos: dict[int, list[list[str]]] = defaultdict(list)
    for row in sections.get("PAGO", []):
        pagos[int(row[0])].append(row[1:])

    ecfs: dict[int, list[list[str]]] = defaultdict(list)
    for h in headers:
        if h["encf"] or h["trackId"] or h["estadoDgii"]:
            ecfs[h["id"]].append([h["encf"], h["estadoDgii"], h["ncf"], h["trackId"]])

    stamp = datetime.now().strftime("%Y-%m-%d")
    music = Path(r"C:\Users\USUARIO\Music") / f"Terraza-Facturas-Backup-{stamp}.pdf"
    desktop = Path(r"C:\Users\USUARIO\Desktop") / f"Terraza-Facturas-Backup-{stamp}.pdf"
    artifacts = Path(__file__).resolve().parents[1] / "artifacts" / "terraza-backup"
    artifacts.mkdir(parents=True, exist_ok=True)
    art_pdf = artifacts / f"Terraza-Facturas-Backup-{stamp}.pdf"

    build_pdf(art_pdf, meta, extras, headers, details, pagos, ecfs)
    art_pdf.replace(art_pdf)  # no-op keep
    for dest in (music, desktop):
        dest.write_bytes(art_pdf.read_bytes())

    csv_path = artifacts / f"Terraza-Facturas-Backup-{stamp}.csv"
    with csv_path.open("w", encoding="utf-8-sig", newline="") as f:
        w = csv.writer(f)
        w.writerow(["IdFactura", "Documento", "Fecha", "Cliente", "NCF", "ENCF", "EstadoDGII", "Estado", "Total", "Producto", "Barcode", "Cantidad", "Precio", "ITBIS", "Subtotal", "FueraPDF"])
        for h in headers:
            for d in details.get(h["id"], []):
                w.writerow([
                    h["id"], h["doc"], h["fecha"], h["cliente"], h["ncf"], h["encf"], h["estadoDgii"],
                    h["estado"], f"{h['total']:.2f}", d[1], d[2], d[3], d[4], d[5], d[6], d[7],
                ])

    print(f"PDF {art_pdf}")
    print(f"PDF {music}")
    print(f"PDF {desktop}")
    print(f"CSV {csv_path}")
    print(f"FACTURAS {len(headers)}")
    print(f"EXTRAS {len(extras)}")
    print(f"TOTAL {sum(h['total'] for h in headers):.2f}")


if __name__ == "__main__":
    main()
