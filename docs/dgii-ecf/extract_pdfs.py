from pathlib import Path
from pypdf import PdfReader

pdf_dir = Path(r"C:\Users\USUARIO\Documents\GitHub\AlahiaPosApi\docs\dgii-ecf\pdfs")
txt_dir = Path(r"C:\Users\USUARIO\Documents\GitHub\AlahiaPosApi\docs\dgii-ecf\txt")
txt_dir.mkdir(parents=True, exist_ok=True)

priority = [
    "Informe",
    "Descripcion Tecnica Emisores",
    "Descripcion Tecnica Servicios",
    "Formato Comprobante",
    "Formato Acuse",
    "Formato Aprob",
    "Formato Anul",
    "Formato Resumen",
    "Firmado",
    "Proceso de Certificacion para ser Emisor",
    "Proceso-Certificacion-EmisorElectronico-Proveedor",
    "Instructivo-Contingencia",
    "Instructivo Delegaciones",
    "Solicitud Usuario",
    "Instructivo App",
    "Instructivo-Facturador",
    "Representaci",
]

pdfs = list(pdf_dir.glob("*.pdf"))


def rank(p: Path) -> int:
    name = p.name.lower()
    for i, key in enumerate(priority):
        if key.lower() in name:
            return i
    return 100


pdfs.sort(key=rank)

for pdf in pdfs:
    out = txt_dir / (pdf.stem + ".txt")
    try:
        reader = PdfReader(str(pdf))
        parts = []
        for i, page in enumerate(reader.pages):
            t = page.extract_text() or ""
            parts.append(f"\n\n===== PAGE {i+1}/{len(reader.pages)} =====\n\n{t}")
        text = "".join(parts)
        out.write_text(text, encoding="utf-8", errors="replace")
        print(f"OK pages={len(reader.pages)} chars={len(text)} {pdf.name}")
    except Exception as e:
        print(f"FAIL {pdf.name}: {e}")
