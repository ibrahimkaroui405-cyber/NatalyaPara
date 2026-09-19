import requests
from bs4 import BeautifulSoup

headers = {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
}

url = "https://parafendri.tn/11-visage"
resp = requests.get(url, headers=headers, timeout=15)
soup = BeautifulSoup(resp.content.decode("utf-8", "ignore"), "html.parser")

for prod in soup.select(".product-miniature")[:3]:
    title = prod.select_one(".product-title a").get_text(strip=True)
    link = prod.select_one(".product-title a")["href"]
    price = prod.select_one(".price").get_text(strip=True)
    
    # Image resolution
    img_el = prod.select_one("img")
    img_url = ""
    if img_el:
        img_url = img_el.get("data-src") or img_el.get("data-original") or img_el.get("data-full-size-image-url") or img_el.get("src")
        
    print(f"Name: {title}")
    print(f"Price: {price}")
    print(f"Image: {img_url}")
    print(f"Link: {link}")
    
    # Check detail page for brand & description
    det_resp = requests.get(link, headers=headers, timeout=15)
    det_soup = BeautifulSoup(det_resp.content.decode("utf-8", "ignore"), "html.parser")
    
    brand_el = det_soup.select_one(".product-manufacturer a, .product-brand a, [itemprop='brand']")
    brand_name = brand_el.get_text(strip=True) if brand_el else "Marque Générale"
    
    # Real large image on product page
    large_img = det_soup.select_one(".js-qv-product-cover, img.js-qv-product-cover, .product-cover img")
    real_img = large_img.get("src") or large_img.get("data-src") if large_img else img_url
    
    # Description
    desc_el = det_soup.select_one("#description .product-description, .product-description")
    desc = desc_el.get_text(strip=True)[:100] if desc_el else "Description standard"
    
    print(f"Brand: {brand_name}")
    print(f"Real High-Res Image: {real_img}")
    print(f"Desc snippet: {desc}...")
    print("-" * 50)
