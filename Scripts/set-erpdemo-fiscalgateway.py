import json
import os
import shutil
import time
from datetime import datetime

erp_path = r"\\144.126.143.154\C$\inetpub\wwwroot\AlahiaPosApi_ErpDemo\appsettings.json"
ecf_path = r"\\144.126.143.154\C$\inetpub\wwwroot\AlahiaEcfApi\appsettings.json"
site_dir = r"\\144.126.143.154\C$\inetpub\wwwroot\AlahiaPosApi_ErpDemo"

with open(erp_path, encoding="utf-8-sig") as f:
    erp = json.load(f)
with open(ecf_path, encoding="utf-8-sig") as f:
    ecf = json.load(f)

fg = erp.setdefault("FiscalGateway", {})
old_base = (fg.get("BaseUrl") or "").strip()
fg["BaseUrl"] = "https://ecf.alahiapos.com"
fg["PublicBaseUrl"] = "https://ecf.alahiapos.com"
ecf_key = ((ecf.get("AlahiaEcfApi") or {}).get("ApiKey") or "").strip()
erp_key = (fg.get("ApiKey") or "").strip()
copied_key = False
if ecf_key and erp_key != ecf_key:
    fg["ApiKey"] = ecf_key
    copied_key = True

stamp = datetime.now().strftime("%Y%m%d-%H%M%S")
backup = erp_path + f".bak-fe-{stamp}"
shutil.copy2(erp_path, backup)

with open(erp_path, "w", encoding="utf-8", newline="\n") as f:
    json.dump(erp, f, indent=2, ensure_ascii=False)
    f.write("\n")

print(f"oldBase={old_base}")
print("newBase=https://ecf.alahiapos.com")
print(f"apiKeyCopied={copied_key}")
print(f"backup={os.path.basename(backup)}")

offline = os.path.join(site_dir, "app_offline.htm")
with open(offline, "w", encoding="utf-8") as f:
    f.write("offline")
time.sleep(4)
os.remove(offline)
print("recycled=True")
