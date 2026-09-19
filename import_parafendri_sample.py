import requests
from bs4 import BeautifulSoup
import sqlite3
import re
import os
import time

DB_PATH = os.path.join(os.path.dirname(__file__), "App_Data", "natalyapara.db")

headers = {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
}

# Category mappings from Para Fendri URL categories to Natalya Para category slugs & names
CATEGORIES_TO_SCRAPE = [
    {
        "main_name": "Soins du Visage",
        "main_slug": "visage",
        "subcats": [
            {"name": "Nettoyants & Démaquillants", "url": "https://parafendri.tn/27-nettoyants-et-demaquillants"},
            {"name": "Anti-Âge & Rides", "url": "https://parafendri.tn/22-anti-age-et-anti-ride"},
            {"name": "Anti-Imperfections & Acné", "url": "https://parafendri.tn/20-peaux-acneiques"},
            {"name": "Anti-Taches & Éclat", "url": "https://parafendri.tn/24-anti-taches-et-depigmentants"},
            {"name": "Contour des Yeux", "url": "https://parafendri.tn/74-contour-des-yeux"},
        ]
    },
    {
        "main_name": "Protection Solaire",
        "main_slug": "solaire",
        "subcats": [
            {"name": "Crèmes Solaires Visage", "url": "https://parafendri.tn/31-creme-solaire-"},
            {"name": "Packs Solaires", "url": "https://parafendri.tn/32-pack-solaire"},
            {"name": "Après-Solaire & Réparateur", "url": "https://parafendri.tn/34-apres-solaire"},
            {"name": "Solaires Bébé & Enfant", "url": "https://parafendri.tn/30-solaire-bebes-enfants"}
        ]
    },
    {
        "main_name": "Soins Capillaires",
        "main_slug": "cheveux",
        "subcats": [
            {"name": "Shampoings", "url": "https://parafendri.tn/79-shampoing-"},
            {"name": "Après-Shampoings", "url": "https://parafendri.tn/80-apres-shampoing"},
            {"name": "Masques & Sérums", "url": "https://parafendri.tn/81-masques"},
            {"name": "Anti-Chute & Repousse", "url": "https://parafendri.tn/82-soins-capillaires-"}
        ]
    },
    {
        "main_name": "Maman & Bébé",
        "main_slug": "maman-bebe",
        "subcats": [
            {"name": "Soins & Toilette Bébé", "url": "https://parafendri.tn/84-soins-et-toilette-bebe"},
            {"name": "Change & Érythème", "url": "https://parafendri.tn/85-change-et-soins-de-siege"},
            {"name": "Biberons & Accessoires", "url": "https://parafendri.tn/86-accesssoires"}
        ]
    },
    {
        "main_name": "Soins du Corps",
        "main_slug": "corps",
        "subcats": [
            {"name": "Hydratation & Nutrition", "url": "https://parafendri.tn/59-hydratation-et-nutrition"},
            {"name": "Hygiène Corporelle", "url": "https://parafendri.tn/65-hygiene-corporelle"},
            {"name": "Gommages & Exfoliants", "url": "https://parafendri.tn/58-gommages-et-exfoliants"}
        ]
    },
    {
        "main_name": "Hygiène & Beauté",
        "main_slug": "hygiene",
        "subcats": [
            {"name": "Douche & Bain", "url": "https://parafendri.tn/49-douche-et-bain"},
            {"name": "Hygiène Bucco-Dentaire", "url": "https://parafendri.tn/54-hygiene-bucco-dentaire"},
            {"name": "Hygiène Intime", "url": "https://parafendri.tn/51-hygiene-intime"}
        ]
    },
    {
        "main_name": "Vitamines & Compléments",
        "main_slug": "vitamines",
        "subcats": [
            {"name": "Vitamines & Énergie", "url": "https://parafendri.tn/6-complements-alimentaires"},
            {"name": "Immunité & Défenses", "url": "https://parafendri.tn/8-stimulants-immunitaires"}
        ]
    }
]

def clean_price(price_str):
    # e.g. "30,000 TND" or "10,000 DT"
    if not price_str:
        return 25.00
    cleaned = re.sub(r"[^\d,\.]", "", price_str).replace(",", ".")
    try:
        val = float(cleaned)
        return round(val, 2)
    except:
        return 25.00

def extract_brand(title, detail_soup):
    # Try finding brand on page
    brand_el = detail_soup.select_one(".product-manufacturer a, .product-brand a, [itemprop='brand']")
    if brand_el and brand_el.get_text(strip=True):
        b = brand_el.get_text(strip=True)
        if len(b) > 1 and not b.lower().startswith("accueil"):
            return b.strip()
    
    # Try getting first 1-2 words if capitalized
    words = title.split()
    if words:
        first = words[0].strip().replace(":", "").replace("-", "")
        if first.isupper() and len(first) > 1:
            if len(words) > 1 and words[1].isupper() and len(words[1]) > 2:
                return f"{first} {words[1]}"
            return first
            
        known_brands = ["Eucerin", "Ducray", "Avène", "Avene", "SVR", "Pharmaceris", "Dermedic", "Dermacare", "Caudalie", "La Roche-Posay", "Bioderma", "Vichy", "Phyteol", "Phyteale", "Xen", "Lirene", "Beesline", "Vital", "Uriage", "A-Derma", "Mustela", "Klorane", "Nuxe", "Isdin"]
        for kb in known_brands:
            if kb.lower() in title.lower():
                return kb
                
        return words[0].capitalize()
    return "Laboratoire Officinal"

def slugify(text):
    text = text.lower().strip()
    text = re.sub(r"[àáâãäå]", "a", text)
    text = re.sub(r"[èéêë]", "e", text)
    text = re.sub(r"[ìíîï]", "i", text)
    text = re.sub(r"[òóôõö]", "o", text)
    text = re.sub(r"[ùúûü]", "u", text)
    text = re.sub(r"[ç]", "c", text)
    text = re.sub(r"[^a-z0-9]+", "-", text)
    return text.strip("-")

def run_import():
    print(f"Connecting to database at {DB_PATH}...")
    conn = sqlite3.connect(DB_PATH)
    cursor = conn.cursor()

    # Ensure columns exist
    try: cursor.execute('ALTER TABLE "Products" ADD COLUMN "SubCategory" TEXT NULL;')
    except: pass
    try: cursor.execute('ALTER TABLE "Products" ADD COLUMN "SubCategorySlug" TEXT NULL;')
    except: pass
    try: cursor.execute('ALTER TABLE "Categories" ADD COLUMN "ParentSlug" TEXT NULL;')
    except: pass
    try: cursor.execute('ALTER TABLE "Categories" ADD COLUMN "SubCategories" TEXT NOT NULL DEFAULT \'[]\';')
    except: pass
    conn.commit()

    total_imported = 0
    brands_added = set()

    for cat_info in CATEGORIES_TO_SCRAPE:
        main_name = cat_info["main_name"]
        main_slug = cat_info["main_slug"]
        
        for sub_info in cat_info["subcats"]:
            sub_name = sub_info["name"]
            sub_url = sub_info["url"]
            sub_slug = slugify(sub_name)

            print(f"\nScraping Sub-category: [{main_name}] -> [{sub_name}] from {sub_url}...")
            
            try:
                resp = requests.get(sub_url, headers=headers, timeout=15)
                if resp.status_code != 200:
                    print(f"  Skipping {sub_url} (status {resp.status_code})")
                    continue
                    
                soup = BeautifulSoup(resp.content.decode("utf-8", "ignore"), "html.parser")
                miniatures = soup.select(".product-miniature, .js-product-miniature")
                print(f"  Found {len(miniatures)} products on page. Extracting top 3-4...")

                for mini in miniatures[:3]:  # Top 3 per subcategory for a rich, diverse sample
                    try:
                        title_el = mini.select_one(".product-title a, h2.product-title a, h3.product-title a")
                        price_el = mini.select_one(".product-price-and-shipping .price, .price, .current-price")
                        img_el = mini.select_one("img")
                        
                        if not title_el:
                            continue
                            
                        title = title_el.get_text(strip=True)
                        product_url = title_el.get("href", "")
                        price_text = price_el.get_text(strip=True) if price_el else ""
                        price = clean_price(price_text)
                        
                        img_url = ""
                        if img_el:
                            img_url = img_el.get("data-src") or img_el.get("data-original") or img_el.get("data-full-size-image-url") or img_el.get("src") or ""
                            
                        # Fetch detail page for description & brand
                        desc = f"Soin dermo-cosmétique d'officine certifié {sub_name}. Formule authentique et testée sous contrôle dermatologique."
                        brand = "Laboratoire Officinal"
                        
                        if product_url:
                            try:
                                d_resp = requests.get(product_url, headers=headers, timeout=10)
                                if d_resp.status_code == 200:
                                    d_soup = BeautifulSoup(d_resp.content.decode("utf-8", "ignore"), "html.parser")
                                    brand = extract_brand(title, d_soup)
                                    
                                    desc_el = d_soup.select_one("#description .product-description, .product-description, [itemprop='description']")
                                    if desc_el and len(desc_el.get_text(strip=True)) > 20:
                                        desc = desc_el.get_text(strip=True)[:450]
                                        
                                    large_img_el = d_soup.select_one(".js-qv-product-cover, img.js-qv-product-cover, .product-cover img")
                                    if large_img_el and large_img_el.get("src") and not "loader.svg" in large_img_el.get("src"):
                                        img_url = large_img_el.get("src")
                            except Exception as ex_det:
                                print(f"    Notice on detail page: {ex_det}")
                                
                        if not img_url or "loader.svg" in img_url:
                            img_url = "https://images.unsplash.com/photo-1556228720-195a672e8a03?auto=format&fit=crop&w=600&q=80"

                        # Check if product already exists
                        cursor.execute('SELECT "Id" FROM "Products" WHERE "Name" = ?;', (title,))
                        existing = cursor.fetchone()
                        
                        if not existing:
                            cursor.execute('''
                                INSERT INTO "Products" 
                                ("Name", "Brand", "ShortDescription", "FullDescription", "Price", "OriginalPrice", 
                                 "Rating", "ReviewCount", "ImageUrl", "CategorySlug", "CategoryName", 
                                 "SubCategory", "SubCategorySlug", "StockQuantity", "InStock", "RxRequired", "BadgeText", 
                                 "Ingredients", "DosageInstructions")
                                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);
                            ''', (
                                title,
                                brand,
                                desc[:120] + "...",
                                desc,
                                price,
                                round(price * 1.15, 2),
                                4.8,
                                32,
                                img_url,
                                main_slug,
                                main_name,
                                sub_name,
                                sub_slug,
                                20,
                                1,
                                0,
                                "Officine Agréée",
                                '["Actifs dermo-officinaux purs", "Tolérance testée"]',
                                '["Appliquer matin et/ou soir sur peau propre et sèche."]'
                            ))
                            total_imported += 1
                            print(f"    + Imported: [{brand}] {title[:40]}... ({price} TND) -> [{sub_name}]")
                        else:
                            cursor.execute('''
                                UPDATE "Products" 
                                SET "SubCategory" = ?, "SubCategorySlug" = ?, "CategorySlug" = ?, "CategoryName" = ?
                                WHERE "Id" = ?;
                            ''', (sub_name, sub_slug, main_slug, main_name, existing[0]))
                            print(f"    ~ Updated: {title[:40]}... -> [{sub_name}]")

                        # Ensure brand exists in Brands table
                        brand_slug = slugify(brand)
                        if brand_slug and brand_slug not in brands_added:
                            cursor.execute('SELECT "Slug" FROM "Brands" WHERE "Slug" = ? OR "Name" = ?;', (brand_slug, brand))
                            if not cursor.fetchone():
                                cursor.execute('''
                                    INSERT INTO "Brands" 
                                    ("Slug", "Name", "Tagline", "Description", "Origin", "Category", "CategorySlug", 
                                     "LogoInitials", "Icon", "ProductCount", "SignatureProduct", "KeyFeatures", 
                                     "IsFeatured", "Rating", "BadgeText", "ImageUrl", "LogoUrl", "TutorialTag", 
                                     "VideoDuration", "RitualCategory", "ReviewCount", "Price", "OriginalPrice", 
                                     "BenefitDescription")
                                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);
                                ''', (
                                    brand_slug,
                                    brand,
                                    f"Laboratoire de référence dermo-pharmaceutique {brand}",
                                    f"Découvrez tous les soins et formulations du laboratoire {brand} disponibles dans notre officine.",
                                    "France / Europe",
                                    "Dermo-Cosmétique",
                                    "dermo",
                                    brand[:2].upper(),
                                    "verified",
                                    1,
                                    title[:50],
                                    '["Formules testées", "Haute tolérance"]',
                                    0,
                                    4.9,
                                    "Officine Agréée",
                                    img_url,
                                    "",
                                    "TUTO SOIN",
                                    "0:45 min",
                                    "RITUEL DE SOIN",
                                    45,
                                    price,
                                    round(price * 1.15, 2),
                                    "Soin authentique certifié."
                                ))
                                brands_added.add(brand_slug)
                                print(f"      * Created Brand: {brand}")
                                
                    except Exception as ex_prod:
                        print(f"    Error on product: {ex_prod}")

            except Exception as ex_cat:
                print(f"  Error on subcategory {sub_url}: {ex_cat}")

    conn.commit()
    conn.close()
    print(f"\n==========================================")
    print(f"IMPORT COMPLETE! {total_imported} new products imported.")
    print(f"==========================================")

if __name__ == "__main__":
    run_import()
