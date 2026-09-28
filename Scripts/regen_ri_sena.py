"""Regenera las 11 RI CerteCF de Dra Sena con razón social / dirección reales."""
from __future__ import annotations

import html
import os
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path
from urllib.parse import quote

GOLD = Path(r"C:\Users\USUARIO\Documents\GitHub\AlahiaPosApi\artifacts\gold-testecf")
OUT = Path(os.path.expandvars(r"%USERPROFILE%\Desktop\CerteCF-SUBIR-RI"))

RAZON = "CENTRO ODONTOLOGICO DRA SENA 1723 SRL"
COMERCIAL = "CENTRO ODONTOLOGICO DRA SENA 1723"
DIRECCION = "Santo Domingo"
TELEFONO = "8492556007"

TITULO = {
    31: "Factura de Crédito Fiscal Electrónica",
    32: "Factura de Consumo Electrónica",
    33: "Nota de Débito Electrónica",
    34: "Nota de Crédito Electrónica",
    41: "Comprobante Electrónico de Compras",
    43: "Comprobante Electrónico para Gastos Menores",
    44: "Comprobante Electrónico para Regímenes Especiales",
    45: "Comprobante Electrónico Gubernamental",
    46: "Comprobante Electrónico para Exportaciones",
    47: "Comprobante Electrónico para Pagos al Exterior",
}

SLOTS = [
    ("01-RI-tipo-31-E310000000041.pdf", "E310000000041"),
]


def local(el: ET.Element | None, name: str) -> str:
    if el is None:
        return ""
    for n in el.iter():
        if n.tag.rsplit("}", 1)[-1] == name and n.text:
            return n.text.strip()
    return ""


def locals_all(el: ET.Element | None, name: str) -> list[str]:
    if el is None:
        return []
    return [n.text.strip() for n in el.iter() if n.tag.rsplit("}", 1)[-1] == name and n.text]


def find_chrome() -> str:
    candidates = [
        Path(os.environ.get("PROGRAMFILES", r"C:\Program Files")) / "Google/Chrome/Application/chrome.exe",
        Path(os.environ.get("PROGRAMFILES(X86)", r"C:\Program Files (x86)")) / "Google/Chrome/Application/chrome.exe",
        Path(os.environ.get("LOCALAPPDATA", "")) / "Google/Chrome/Application/chrome.exe",
        Path(os.environ.get("PROGRAMFILES", r"C:\Program Files")) / "Microsoft/Edge/Application/msedge.exe",
    ]
    for p in candidates:
        if p.exists():
            return str(p)
    raise FileNotFoundError("No se encontró Chrome ni Edge")


def qr_url(tipo: int, rnc_e: str, rnc_c: str, encf: str, fecha_em: str, monto: str, fecha_firma: str, codigo: str) -> str:
    monto_n = f"{float(monto):.2f}"
    if tipo == 32 and float(monto) < 250000:
        qs = "&".join([
            f"RncEmisor={quote(rnc_e)}",
            f"ENCF={quote(encf)}",
            f"MontoTotal={quote(monto_n)}",
            f"CodigoSeguridad={quote(codigo)}",
        ])
        return f"https://fc.dgii.gov.do/certecf/ConsultaTimbreFC?{qs}"
    firma = fecha_firma.replace(" ", "%20")
    qs = "&".join([
        f"RncEmisor={quote(rnc_e)}",
        f"RncComprador={quote(rnc_c)}",
        f"ENCF={quote(encf)}",
        f"FechaEmision={quote(fecha_em)}",
        f"MontoTotal={quote(monto_n)}",
        f"FechaFirma={firma}",
        f"CodigoSeguridad={quote(codigo)}",
    ])
    return f"https://ecf.dgii.gov.do/certecf/ConsultaTimbre?{qs}"


def html_ri(xml_path: Path) -> str:
    tree = ET.parse(xml_path)
    root = tree.getroot()
    tipo = int(local(root, "TipoeCF") or "0")
    encf = local(root, "eNCF")
    fecha_em = local(root, "FechaEmision")
    fecha_firma = local(root, "FechaHoraFirma")
    venc = local(root, "FechaVencimientoSecuencia")
    rnc_e = "".join(ch for ch in local(root, "RNCEmisor") if ch.isdigit())
    rnc_c = "".join(ch for ch in local(root, "RNCComprador") if ch.isdigit())
    razon_c = local(root, "RazonSocialComprador")
    itbis = local(root, "TotalITBIS") or "0.00"
    total = local(root, "MontoTotal") or "0.00"
    sig = local(root, "SignatureValue").replace("\n", "").replace(" ", "")
    codigo = sig[:6]
    qr = qr_url(tipo, rnc_e, rnc_c, encf, fecha_em, total, fecha_firma, codigo)
    qr_img = f"https://api.qrserver.com/v1/create-qr-code/?size=150x150&data={quote(qr, safe='')}"

    items = []
    for item in root.iter():
        if item.tag.rsplit("}", 1)[-1] != "Item":
            continue
        items.append((
            local(item, "NombreItem"),
            local(item, "CantidadItem"),
            local(item, "PrecioUnitarioItem"),
            local(item, "MontoItem"),
        ))

    rows = "".join(
        f"<tr><td>{html.escape(n)}</td><td>{html.escape(c.split('.')[0] if c.endswith('.00') else c)}</td>"
        f"<td>{html.escape(p)}</td><td>{html.escape(m)}</td></tr>"
        for n, c, p, m in items
    )
    titulo = TITULO.get(tipo, f"Comprobante Fiscal Electrónico E{tipo}")
    venc_html = ""
    if tipo not in (32, 34) and venc:
        venc_html = f'<p class="idoc">Fecha Vencimiento: {html.escape(venc)}</p>'
    return f"""<!DOCTYPE html><html lang="es"><head><meta charset="utf-8"><title>{html.escape(titulo)}</title>
<style>
@page{{size:A4;margin:12mm}}
body{{font-family:Arial,sans-serif;max-width:720px;margin:0 auto;color:#111;background:#fff}}
h1{{font-size:16px;text-align:center;margin:0 0 4px}}
.idoc{{text-align:center;font-size:13px;margin:0 0 2px}}
table{{width:100%;border-collapse:collapse;font-size:12px;margin-top:12px}}
td,th{{border-bottom:1px solid #ddd;padding:5px;text-align:left}}
.lab{{color:#444;width:160px}}
.tot{{text-align:right;font-weight:700}}
.qr{{margin-top:16px;text-align:center;font-size:11px}}
.qr img{{display:block;margin:8px auto}}
.qr p{{margin:4px 0 0;white-space:nowrap;word-break:normal}}
</style></head><body>
<h1>{html.escape(titulo)}</h1>
<p class="idoc">e-NCF: <strong>{html.escape(encf)}</strong></p>
{venc_html}
<table>
<tr><td class="lab">Razón Social</td><td><strong>{html.escape(RAZON)}</strong></td></tr>
<tr><td class="lab">Nombre Comercial</td><td>{html.escape(COMERCIAL)}</td></tr>
<tr><td class="lab">RNC</td><td>{html.escape(rnc_e)}</td></tr>
<tr><td class="lab">Dirección</td><td>{html.escape(DIRECCION)}</td></tr>
<tr><td class="lab">Teléfono</td><td>{html.escape(TELEFONO)}</td></tr>
<tr><td class="lab">Fecha de emisión</td><td>{html.escape(fecha_em)}</td></tr>
<tr><td class="lab">Razón Social comprador</td><td>{html.escape(razon_c)}</td></tr>
<tr><td class="lab">RNC comprador</td><td>{html.escape(rnc_c)}</td></tr>
</table>
<table style="margin-top:12px"><thead><tr><th>Item</th><th>Cant.</th><th>Precio</th><th>Monto</th></tr></thead><tbody>
{rows}
</tbody></table>
<p class="tot">ITBIS: {html.escape(itbis)}<br>Monto total: {html.escape(total)}</p>
<div class="qr"><strong>ConsultaTimbre</strong>
<img alt="QR ConsultaTimbre" width="150" height="150" src="{qr_img}" />
<p>Código de Seguridad: {html.escape(codigo)}</p>
<p>Fecha Firma: {html.escape(fecha_firma)}</p>
</div>
</body></html>"""


def main() -> int:
    chrome = find_chrome()
    OUT.mkdir(parents=True, exist_ok=True)
    keep = {name for name, _ in SLOTS}
    for p in OUT.glob("*.pdf"):
        if p.name not in keep:
            p.unlink()
            print("borrado", p.name)
    tmp = Path(tempfile.mkdtemp(prefix="ri-sena-"))
    for name, encf in SLOTS:
        xml = GOLD / f"certecf_ECF_{encf}_firmado.xml"
        if not xml.exists():
            print("FALTA", xml)
            return 1
        html_path = tmp / f"{encf}.html"
        html_path.write_text(html_ri(xml), encoding="utf-8")
        pdf_path = OUT / name
        uri = html_path.resolve().as_uri()
        cmd = [
            chrome,
            "--headless=new",
            "--disable-gpu",
            "--no-pdf-header-footer",
            f"--print-to-pdf={pdf_path}",
            uri,
        ]
        subprocess.run(cmd, check=True, capture_output=True)
        text = html_path.read_text(encoding="utf-8")
        if RAZON not in text or DIRECCION not in text:
            print("HTML incompleto", encf)
            return 1
        tipo = int(encf[1:3])
        if tipo in (32, 34) and "Fecha Vencimiento" in text:
            print("E32/E34 no debe llevar vencimiento", encf)
            return 1
        if tipo not in (32, 34) and "Fecha Vencimiento:" not in text:
            print("Falta fecha vencimiento", encf)
            return 1
        if "Código de Seguridad:" not in text or "Fecha Firma:" not in text:
            print("QR footer incompleto", encf)
            return 1
        print("OK", name, pdf_path.stat().st_size)
    extras = [p for p in OUT.iterdir() if p.suffix.lower() != ".pdf" or not p.name[:2].isdigit()]
    for p in extras:
        if p.name[:3] in {f"{i:02d}-" for i in range(1, 12)} and p.suffix.lower() == ".pdf":
            continue
        p.unlink()
        print("borrado", p.name)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
