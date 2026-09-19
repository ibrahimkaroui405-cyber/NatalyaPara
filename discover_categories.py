import requests
from bs4 import BeautifulSoup
import re

headers = {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
}

resp = requests.get("https://parafendri.tn/", headers=headers, timeout=15)
soup = BeautifulSoup(resp.text, "html.parser")

cat_links = set()
for a in soup.find_all("a", href=True):
    href = a["href"]
    # Prestashop category URLs typically have e.g. /12-visage, /3-corps, etc.
    if re.search(r"parafendri\.tn/\d+-[a-z0-9\-]+", href):
        name = a.get_text(strip=True)
        if name and len(name) > 2:
            cat_links.add((name, href.split("?")[0]))

for name, url in sorted(cat_links, key=lambda x: x[1]):
    print(f"{name:35} | {url}")

print(f"\nTotal categories found: {len(cat_links)}")
