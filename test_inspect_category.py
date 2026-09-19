import requests
from bs4 import BeautifulSoup
import json

headers = {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
}

# Inspect 11-visage
url = "https://parafendri.tn/11-visage"
resp = requests.get(url, headers=headers, timeout=15)
soup = BeautifulSoup(resp.content.decode("utf-8", "ignore"), "html.parser")

print("Page Title:", soup.title.string if soup.title else "")

# Subcategories
subcats = []
for sub in soup.select(".subcategories-list a, .category-sub-menu a, .subcategories a"):
    title = sub.get_text(strip=True)
    href = sub.get("href", "")
    if title and href:
        subcats.append({"title": title, "url": href})

print(f"\nSubcategories found ({len(subcats)}):")
for s in subcats:
    print(f"  * {s['title']} -> {s['url']}")

# Product cards
products = []
for prod in soup.select(".product-miniature, .js-product-miniature, article.product-miniature"):
    title_el = prod.select_one(".product-title a, h2.product-title a, h3.product-title a")
    price_el = prod.select_one(".product-price-and-shipping .price, .price, .current-price")
    img_el = prod.select_one(".thumbnail-container img, img.cover-image, img")
    brand_el = prod.select_one(".product-brand, .product-manufacturer, .manufacturer")
    
    title = title_el.get_text(strip=True) if title_el else ""
    link = title_el.get("href", "") if title_el else ""
    price = price_el.get_text(strip=True) if price_el else ""
    img = img_el.get("src", "") or img_el.get("data-src", "") if img_el else ""
    brand = brand_el.get_text(strip=True) if brand_el else ""
    
    if title:
        products.append({
            "title": title,
            "link": link,
            "price": price,
            "image": img,
            "brand": brand
        })

print(f"\nProducts on page 1 ({len(products)}):")
for p in products[:5]:
    print(f"  - {p['title']} | {p['price']} | {p['image']}")

# Pagination
pagination = soup.select_one(".pagination, .page-list")
if pagination:
    print("\nPagination found:", pagination.get_text(strip=True))
