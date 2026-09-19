import requests
from bs4 import BeautifulSoup
import urllib.parse

base = 'http://localhost:5000/Products?category=vitamines'
subs = [
    'Vitamines & Énergie',
    'Immunité & Défenses',
    'Minceur & Détox',
    'Sommeil & Sérénité',
    'Articulations & Os',
    'Beauté Peau & Ongles'
]

print("=== VERIFYING SUB-CATEGORY FILTERS ===")
for sub in subs:
    encoded_sub = urllib.parse.quote(sub)
    url = f"{base}&subCategory={encoded_sub}"
    r = requests.get(url, timeout=10)
    soup = BeautifulSoup(r.text, 'html.parser')
    
    # Check for count in header
    count_el = soup.find(lambda el: el.name in ['div', 'span', 'p'] and 'soins disponibles' in el.get_text().lower())
    count_str = count_el.get_text().strip() if count_el else "N/A"
    
    print(f"[{r.status_code}] {sub} -> {count_str}")

print("\n=== VERIFYING OTHER CATEGORY FILTERS ===")
test_others = [
    ('solaire', 'Crèmes Solaires Visage'),
    ('cheveux', 'Shampoings'),
    ('corps', 'Hydratation & Nutrition'),
    ('visage', 'Anti-Imperfections & Acné')
]
for cat, sub in test_others:
    url = f"http://localhost:5000/Products?category={cat}&subCategory={urllib.parse.quote(sub)}"
    r = requests.get(url, timeout=10)
    soup = BeautifulSoup(r.text, 'html.parser')
    count_el = soup.find(lambda el: el.name in ['div', 'span', 'p'] and 'soins disponibles' in el.get_text().lower())
    count_str = count_el.get_text().strip() if count_el else "N/A"
    print(f"[{r.status_code}] {cat} > {sub} -> {count_str}")
