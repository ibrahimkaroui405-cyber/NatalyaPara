import requests
from bs4 import BeautifulSoup
import json

headers = {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
}

try:
    resp = requests.get("https://parafendri.tn/", headers=headers, timeout=15)
    soup = BeautifulSoup(resp.text, "html.parser")
    
    # Let's inspect the main menu / vertical menu / megamenu links
    menu_items = []
    
    # Try finding categories in nav or megamenu
    for a in soup.find_all("a", href=True):
        href = a["href"]
        text = a.get_text(strip=True)
        if text and ("/c/" in href or "/category" in href or any(kw in href for kw in ["visage", "cheveux", "corps", "bebe", "solaires", "solaire", "bio", "homme", "hygiene", "complements"])):
            menu_items.append({"title": text, "url": href})
            
    print(f"Found {len(menu_items)} relevant category/menu links.")
    
    # Remove duplicates preserving order
    seen = set()
    unique_items = []
    for item in menu_items:
        key = (item["title"], item["url"])
        if key not in seen and len(item["title"]) > 1:
            seen.add(key)
            unique_items.append(item)
            
    for item in unique_items[:40]:
        print(f" - {item['title']} => {item['url']}")
        
    with open("categories_discovered.json", "w", encoding="utf-8") as f:
        json.dump(unique_items, f, ensure_ascii=False, indent=2)
        
except Exception as e:
    print("Error:", e)
