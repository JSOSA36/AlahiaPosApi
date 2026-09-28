"""Regenera las 11 RI CerteCF con el layout de los modelos ilustrativos DGII."""
from __future__ import annotations

import os
import re
from urllib.parse import parse_qs, urlparse, unquote

import pymupdf

SRC = r"C:\Users\USUARIO\Documents\GitHub\AlahiaPosApi\artifacts\ri-origen"
OUT_DESK = r"C:\Users\USUARIO\Documents\GitHub\AlahiaPosApi\artifacts\CerteCF-SUBIR-RI"
OUT_DL = OUT_DESK

RAZON = "DOCUMENTOS ELECTRONICOS DE 02"
RNC = "133659115"
DIRECCION = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA"
AZUL = (11 / 255, 110 / 255, 153 / 255)
HEAD = (217 / 255, 232 / 255, 240 / 255)

TITULOS = {
    "E31": "Factura de Crédito Fiscal Electrónica",
    "E32": "Factura de Consumo Electrónica",
    "E33": "Nota de Débito Electrónica",
    "E34": "Nota de Crédito Electrónica",
    "E41": "Compras Electrónica",
    "E43": "Gastos Menores Electrónica",
    "E44": "Regímenes Especiales Electrónica",
    "E45": "Gubernamental Electrónica",
    "E46": "Exportaciones Electrónica",
    "E47": "Pagos al Exterior Electrónica",
}


def tipo_de(encf: str) -> str:
    return encf[:3]


def mostrar_cliente(encf: str) -> bool:
    return tipo_de(encf) not in ("E32", "E43")


def parse_pdf(path: str) -> dict:
    doc = pymupdf.open(path)
    t = "".join(p.get_text() for p in doc)
    name = os.path.basename(path)
    encf_m = re.search(r"(E\d{12})", name)
    encf = encf_m.group(1) if encf_m else re.search(r"(E\d{12})", t).group(1)
    fecha_m = re.search(r"(\d{2}-\d{2}-\d{4})", t)
    itbis_m = re.search(r"ITBIS:\s*([0-9.,]+)", t)
    total_m = re.search(r"Monto total:\s*([0-9.,]+)", t)
    url = re.search(r"https://[^\s]+", t)
    qr_url = url.group(0).strip() if url else ""
    rs_c_m = re.search(r"comprador\n([^\n]+)", t)
    rnc_c_m = re.search(r"RNC comprador\n([^\n]*)", t)
    cliente_rs = rs_c_m.group(1).strip() if rs_c_m else ""
    cliente_rnc = rnc_c_m.group(1).strip() if rnc_c_m else ""
    if cliente_rnc.lower() in ("item", "cant.", "cant", ""):
        cliente_rnc = ""
    items = []
    lines = [x.strip() for x in t.splitlines() if x.strip()]
    start = None
    for i, line in enumerate(lines):
        if line == "Monto" and i >= 3 and "Precio" in lines[i - 1]:
            start = i + 1
            break
    if start is not None:
        block = []
        for line in lines[start:]:
            if line.startswith("ITBIS"):
                break
            block.append(line)
        i = 0
        while i + 3 < len(block):
            nombre, cant, precio, monto = block[i : i + 4]
            if re.match(r"^[\d.,]+$", cant.replace(",", "")) and re.match(
                r"^[\d.,]+$", precio.replace(",", "")
            ):
                items.append((nombre, cant, precio, monto))
                i += 4
            else:
                i += 1
    imgs = []
    for p in doc:
        for img in p.get_images(full=True):
            imgs.append(doc.extract_image(img[0])["image"])
    return {
        "encf": encf,
        "fecha": fecha_m.group(1) if fecha_m else "",
        "itbis": itbis_m.group(1) if itbis_m else "0.00",
        "total": total_m.group(1) if total_m else "0.00",
        "qr_url": qr_url,
        "cliente_rs": cliente_rs,
        "cliente_rnc": cliente_rnc,
        "items": items,
        "qr_png": imgs[0] if imgs else None,
        "titulo": TITULOS.get(tipo_de(encf), "Comprobante Fiscal Electrónico"),
    }


def timbre(qr_url: str) -> tuple[str, str]:
    if not qr_url:
        return "", ""
    q = parse_qs(urlparse(qr_url).query)
    codigo = (q.get("CodigoSeguridad") or [""])[0]
    fecha = unquote((q.get("FechaFirma") or [""])[0])
    return codigo, fecha


def num_to_float(s: str) -> float:
    return float(s.replace(",", ""))


def draw(path_out: str, d: dict) -> None:
    doc = pymupdf.open()
    page = doc.new_page(width=595, height=842)
    y = 40
    page.insert_text((40, y), RAZON, fontsize=13, fontname="helv", color=AZUL)
    page.insert_text((340, y), d["titulo"], fontsize=11, fontname="helv", color=AZUL)
    y += 16
    page.insert_text((40, y), RAZON, fontsize=10, fontname="helv")
    page.insert_text((340, y), f"e-NCF: {d['encf']}", fontsize=10, fontname="helv")
    y += 14
    page.insert_text((40, y), f"RNC {RNC}", fontsize=10, fontname="helv")
    y += 14
    page.insert_text((40, y), f"Dirección: {DIRECCION}", fontsize=9, fontname="helv")
    y += 14
    page.insert_text((40, y), f"Fecha Emisión: {d['fecha']}", fontsize=9, fontname="helv")
    y += 16
    page.draw_rect(pymupdf.Rect(40, y, 555, y + 2.2), color=AZUL, fill=AZUL)
    y += 18
    if mostrar_cliente(d["encf"]) and d["cliente_rs"]:
        page.insert_text((40, y), f"Razón Social Cliente: {d['cliente_rs']}", fontsize=10, fontname="helv")
        y += 14
        page.insert_text((40, y), f"RNC Cliente: {d['cliente_rnc']}", fontsize=10, fontname="helv")
        y += 18

    cols = [40, 95, 290, 360, 430, 500, 555]
    headers = ["Cantidad", "Descripción", "Unidad de Medida", "Precio", "ITBIS", "Valor"]
    page.draw_rect(pymupdf.Rect(40, y, 555, y + 18), color=HEAD, fill=HEAD)
    for i, h in enumerate(headers):
        page.insert_text((cols[i] + 2, y + 12), h, fontsize=7, fontname="helv")
    y += 20
    for nombre, cant, precio, monto in d["items"]:
        try:
            itbis_l = f"{num_to_float(monto) * 0.18:,.2f}"
        except Exception:
            itbis_l = "0.00"
        vals = [cant, nombre[:34], "UND", precio, itbis_l, monto]
        for i, v in enumerate(vals):
            page.insert_text((cols[i] + 2, y + 10), str(v), fontsize=8, fontname="helv")
        page.draw_line(pymupdf.Point(40, y + 14), pymupdf.Point(555, y + 14), color=(0.85, 0.85, 0.85))
        y += 16

    y += 12
    if d["qr_png"]:
        rect = pymupdf.Rect(40, y, 132, y + 92)
        page.insert_image(rect, stream=d["qr_png"])
    codigo, fecha_f = timbre(d["qr_url"])
    ty = y + 98
    if codigo:
        page.insert_text((40, ty), f"Código de Seguridad: {codigo}", fontsize=8, fontname="helv")
        ty += 12
    if fecha_f:
        page.insert_text((40, ty), f"Fecha Firma: {fecha_f}", fontsize=8, fontname="helv")

    gravado = num_to_float(d["total"]) - num_to_float(d["itbis"])
    box_x, box_w = 360, 195
    rows = [
        ("Subtotal Gravado:", f"{gravado:,.2f}"),
        ("Total ITBIS:", d["itbis"]),
        ("Total:", d["total"]),
    ]
    by = y
    for lab, val in rows:
        page.draw_rect(pymupdf.Rect(box_x, by, box_x + box_w, by + 16), color=(0.7, 0.7, 0.7))
        page.insert_text((box_x + 4, by + 11), lab, fontsize=8, fontname="helv")
        page.insert_text((box_x + 118, by + 11), val, fontsize=8, fontname="helv")
        by += 16

    os.makedirs(os.path.dirname(path_out) or ".", exist_ok=True)
    doc.save(path_out)
    doc.close()


def main() -> None:
    os.makedirs(OUT_DESK, exist_ok=True)
    files = sorted(
        f for f in os.listdir(SRC) if f.startswith("RI-E") and f.endswith(".pdf")
    )
    assert len(files) == 11, files
    errors = []
    for name in files:
        src = os.path.join(SRC, name)
        data = parse_pdf(src)
        dest = os.path.join(OUT_DESK, name)
        draw(dest, data)
        chk = pymupdf.open(dest)
        text = "".join(p.get_text() for p in chk)
        chk.close()
        if RAZON not in text:
            errors.append(f"{name}: falta razón social")
        if "PRUEBA" in text or "250MIL" in text or "MacroBits" in text:
            errors.append(f"{name}: texto prohibido")
        if f"RNC {RNC}" not in text and RNC not in text:
            errors.append(f"{name}: falta RNC")
        if tipo_de(data["encf"]) == "E32" and "Razón Social Cliente" in text:
            errors.append(f"{name}: E32 no debe llevar cliente (modelo DGII 2.1/2.2)")
        print("OK", name, data["encf"], "items", len(data["items"]))
        if not data["items"]:
            errors.append(f"{name}: sin items")
        if data["encf"] not in text:
            errors.append(f"{name}: e-NCF incompleto")
    if errors:
        raise SystemExit("\n".join(errors))
    print("TODAS las RI: razón social =", RAZON, "RNC =", RNC)


if __name__ == "__main__":
    main()
