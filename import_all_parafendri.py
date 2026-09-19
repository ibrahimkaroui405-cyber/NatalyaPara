import requests
from bs4 import BeautifulSoup
import sqlite3
import re
import os
import time
from concurrent.futures import ThreadPoolExecutor, as_completed
from threading import Lock

DB_PATH = os.path.join(os.path.dirname(__file__), "App_Data", "natalyapara.db")

headers = {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
    "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8",
    "Accept-Language": "fr,fr-FR;q=0.8,en-US;q=0.5,en;q=0.3"
}

# Complete mapping of Para Fendri URLs to our taxonomy
CATEGORY_MAPPINGS = [
    # 1. Soins du Visage
    {"url": "https://parafendri.tn/11-visage", "cat": "Soins du Visage", "cat_slug": "visage", "sub": "Soins Quotidiens Visage"},
    {"url": "https://parafendri.tn/18-peaux-grasses", "cat": "Soins du Visage", "cat_slug": "visage", "sub": "Anti-Imperfections & Acné"},
    {"url": "https://parafendri.tn/19-peaux-mixtes", "cat": "Soins du Visage", "cat_slug": "visage", "sub": "Hydratation & Nutrition"},
    {"url": "https://parafendri.tn/20-peaux-acneiques", "cat": "Soins du Visage", "cat_slug": "visage", "sub": "Anti-Imperfections & Acné"},
    {"url": "https://parafendri.tn/21-anti-imperfections", "cat": "Soins du Visage", "cat_slug": "visage", "sub": "Anti-Imperfections & Acné"},
    {"url": "https://parafendri.tn/22-anti-age-et-anti-ride", "cat": "Soins du Visage", "cat_slug": "visage", "sub": "Anti-Âge & Rides"},
    {"url": "https://parafendri.tn/23-peaux-sensibles-", "cat": "Soins du Visage", "cat_slug": "visage", "sub": "Peaux Sensibles & Rougeurs"},
    {"url": "https://parafendri.tn/24-anti-taches-et-depigmentants", "cat": "Soins du Visage", "cat_slug": "visage", "sub": "Anti-Taches & Éclat"},
    {"url": "https://parafendri.tn/25-apaisants", "cat": "Soins du Visage", "cat_slug": "visage", "sub": "Peaux Sensibles & Rougeurs"},
    {"url": "https://parafendri.tn/27-nettoyants-et-demaquillants", "cat": "Soins du Visage", "cat_slug": "visage", "sub": "Nettoyants & Démaquillants"},
    {"url": "https://parafendri.tn/74-contour-des-yeux", "cat": "Soins du Visage", "cat_slug": "visage", "sub": "Contour des Yeux"},
    {"url": "https://parafendri.tn/75-peaux-seches", "cat": "Soins du Visage", "cat_slug": "visage", "sub": "Hydratation & Nutrition"},
    {"url": "https://parafendri.tn/87-stick-et-baume-a-levres", "cat": "Soins du Visage", "cat_slug": "visage", "sub": "Soins des Lèvres"},
    {"url": "https://parafendri.tn/88-peeling-du-visage", "cat": "Soins du Visage", "cat_slug": "visage", "sub": "Peeling & Masques"},
    {"url": "https://parafendri.tn/91-soins", "cat": "Soins du Visage", "cat_slug": "visage", "sub": "Soins Spécifiques"},
    {"url": "https://parafendri.tn/95-vinopure", "cat": "Soins du Visage", "cat_slug": "visage", "sub": "Anti-Imperfections & Acné"},

    # 2. Protection Solaire
    {"url": "https://parafendri.tn/15-solaire", "cat": "Protection Solaire", "cat_slug": "solaire", "sub": "Crèmes Solaires Visage"},
    {"url": "https://parafendri.tn/30-solaire-bebes-enfants", "cat": "Protection Solaire", "cat_slug": "solaire", "sub": "Solaires Bébé & Enfant"},
    {"url": "https://parafendri.tn/31-creme-solaire-", "cat": "Protection Solaire", "cat_slug": "solaire", "sub": "Crèmes Solaires Visage"},
    {"url": "https://parafendri.tn/32-pack-solaire", "cat": "Protection Solaire", "cat_slug": "solaire", "sub": "Packs Solaires"},
    {"url": "https://parafendri.tn/34-apres-solaire", "cat": "Protection Solaire", "cat_slug": "solaire", "sub": "Après-Solaire & Réparateur"},
    {"url": "https://parafendri.tn/42-solaire", "cat": "Protection Solaire", "cat_slug": "solaire", "sub": "Crèmes Solaires Visage"},

    # 3. Soins Capillaires
    {"url": "https://parafendri.tn/13-cheveux", "cat": "Soins Capillaires", "cat_slug": "cheveux", "sub": "Shampoings Traitants"},
    {"url": "https://parafendri.tn/41-soins-capillaires-", "cat": "Soins Capillaires", "cat_slug": "cheveux", "sub": "Anti-Chute & Repousse"},
    {"url": "https://parafendri.tn/79-shampoing-", "cat": "Soins Capillaires", "cat_slug": "cheveux", "sub": "Shampoings"},
    {"url": "https://parafendri.tn/80-apres-shampoing", "cat": "Soins Capillaires", "cat_slug": "cheveux", "sub": "Après-Shampoings"},
    {"url": "https://parafendri.tn/81-masques", "cat": "Soins Capillaires", "cat_slug": "cheveux", "sub": "Masques & Sérums"},
    {"url": "https://parafendri.tn/82-soins-capillaires-", "cat": "Soins Capillaires", "cat_slug": "cheveux", "sub": "Anti-Chute & Repousse"},

    # 4. Maman & Bébé
    {"url": "https://parafendri.tn/14-maman-et-bebe", "cat": "Maman & Bébé", "cat_slug": "maman-bebe", "sub": "Soins & Toilette Bébé"},
    {"url": "https://parafendri.tn/83-maman", "cat": "Maman & Bébé", "cat_slug": "maman-bebe", "sub": "Soins Grossesse & Maternité"},
    {"url": "https://parafendri.tn/84-soins-et-toilette-bebe", "cat": "Maman & Bébé", "cat_slug": "maman-bebe", "sub": "Soins & Toilette Bébé"},
    {"url": "https://parafendri.tn/85-change-et-soins-de-siege", "cat": "Maman & Bébé", "cat_slug": "maman-bebe", "sub": "Change & Érythème"},
    {"url": "https://parafendri.tn/86-accesssoires", "cat": "Maman & Bébé", "cat_slug": "maman-bebe", "sub": "Biberons & Accessoires"},

    # 5. Soins du Corps
    {"url": "https://parafendri.tn/12-corps", "cat": "Soins du Corps", "cat_slug": "corps", "sub": "Hydratation & Nutrition"},
    {"url": "https://parafendri.tn/57-epilation-", "cat": "Soins du Corps", "cat_slug": "corps", "sub": "Hygiène Corporelle"},
    {"url": "https://parafendri.tn/58-gommages-et-exfoliants", "cat": "Soins du Corps", "cat_slug": "corps", "sub": "Gommages & Exfoliants"},
    {"url": "https://parafendri.tn/59-hydratation-et-nutrition", "cat": "Soins du Corps", "cat_slug": "corps", "sub": "Hydratation & Nutrition"},
    {"url": "https://parafendri.tn/60-jambes-lourdes", "cat": "Soins du Corps", "cat_slug": "corps", "sub": "Jambes Légères"},
    {"url": "https://parafendri.tn/61-massage", "cat": "Soins du Corps", "cat_slug": "corps", "sub": "Huiles & Massages"},
    {"url": "https://parafendri.tn/62-soins-des-mains", "cat": "Soins du Corps", "cat_slug": "corps", "sub": "Soins Mains & Pieds"},
    {"url": "https://parafendri.tn/63-produits-soins-minceur", "cat": "Soins du Corps", "cat_slug": "corps", "sub": "Soins Minceur & Fermeté"},
    {"url": "https://parafendri.tn/64-parfum", "cat": "Soins du Corps", "cat_slug": "corps", "sub": "Parfums & Brumes"},
    {"url": "https://parafendri.tn/65-hygiene-corporelle", "cat": "Soins du Corps", "cat_slug": "corps", "sub": "Hygiène Corporelle"},
    {"url": "https://parafendri.tn/73-soins-des-pieds", "cat": "Soins du Corps", "cat_slug": "corps", "sub": "Soins Mains & Pieds"},
    {"url": "https://parafendri.tn/89-anti-grattage", "cat": "Soins du Corps", "cat_slug": "corps", "sub": "Anti-Grattage & Sécheresse"},

    # 6. Hygiène & Beauté
    {"url": "https://parafendri.tn/3-beaute", "cat": "Hygiène & Beauté", "cat_slug": "hygiene", "sub": "Beauté & Éclat"},
    {"url": "https://parafendri.tn/16-hygiene", "cat": "Hygiène & Beauté", "cat_slug": "hygiene", "sub": "Douche & Bain"},
    {"url": "https://parafendri.tn/43-deodorant-et-anti-transpirants", "cat": "Hygiène & Beauté", "cat_slug": "hygiene", "sub": "Déodorants & Anti-Transpirants"},
    {"url": "https://parafendri.tn/47-anti-acarien-et-anti-moustique", "cat": "Hygiène & Beauté", "cat_slug": "hygiene", "sub": "Anti-Moustiques & Parasites"},
    {"url": "https://parafendri.tn/48-deodorant-et-anti-transpirant", "cat": "Hygiène & Beauté", "cat_slug": "hygiene", "sub": "Déodorants & Anti-Transpirants"},
    {"url": "https://parafendri.tn/49-douche-et-bain", "cat": "Hygiène & Beauté", "cat_slug": "hygiene", "sub": "Douche & Bain"},
    {"url": "https://parafendri.tn/51-hygiene-intime", "cat": "Hygiène & Beauté", "cat_slug": "hygiene", "sub": "Hygiène Intime"},
    {"url": "https://parafendri.tn/54-hygiene-bucco-dentaire", "cat": "Hygiène & Beauté", "cat_slug": "hygiene", "sub": "Hygiène Bucco-Dentaire"},
    {"url": "https://parafendri.tn/71-coffrets-et-cadeaux", "cat": "Hygiène & Beauté", "cat_slug": "hygiene", "sub": "Coffrets & Cadeaux"},

    # 7. Vitamines & Compléments
    {"url": "https://parafendri.tn/6-complements-alimentaires", "cat": "Vitamines & Compléments", "cat_slug": "vitamines", "sub": "Vitamines & Énergie"},
    {"url": "https://parafendri.tn/8-stimulants-immunitaires", "cat": "Vitamines & Compléments", "cat_slug": "vitamines", "sub": "Immunité & Défenses"},

    # 8. Soins Homme
    {"url": "https://parafendri.tn/4-hommes", "cat": "Soins Homme", "cat_slug": "homme", "sub": "Soins Visage Homme"},
    {"url": "https://parafendri.tn/44-hygiene-corporelle", "cat": "Soins Homme", "cat_slug": "homme", "sub": "Hygiène Homme"},
    {"url": "https://parafendri.tn/45-rasage", "cat": "Soins Homme", "cat_slug": "homme", "sub": "Rasage & Après-Rasage"},
    {"url": "https://parafendri.tn/46-soins-de-visage", "cat": "Soins Homme", "cat_slug": "homme", "sub": "Soins Visage Homme"},
    {"url": "https://parafendri.tn/77-homme", "cat": "Soins Homme", "cat_slug": "homme", "sub": "Soins Visage Homme"},

    # 9. Matériel Médical & Orthopédie
    {"url": "https://parafendri.tn/9-produits-paramedicaux", "cat": "Matériel Médical & Orthopédie", "cat_slug": "materiel-medical", "sub": "Pansements & Premiers Secours"},
    {"url": "https://parafendri.tn/72-sabots-et-claquettes-", "cat": "Matériel Médical & Orthopédie", "cat_slug": "materiel-medical", "sub": "Chaussures Médicales"},
    {"url": "https://parafendri.tn/90-orthopedie", "cat": "Matériel Médical & Orthopédie", "cat_slug": "materiel-medical", "sub": "Orthopédie & Maintien"}
]

db_lock = Lock()
known_brands = [
    "La Roche-Posay", "La Roche Posay", "Cerave", "CeraVe", "SVR", "Bioderma", "ACM", 
    "Uriage", "Vichy", "Eucerin", "Avene", "Avène", "Ducray", "Mustela", "Klorane", 
    "Nuxe", "Isdin", "Filorga", "Caudalie", "Pharmaceris", "Dermedic", "Dermacare", 
    "Phyteol", "Phyteale", "Xen", "XEN", "Lirene", "Beesline", "Vital", "A-Derma", 
    "Sesderma", "Topicrem", "Novophane", "Bionike", "Babé", "Babe", "Chicco", "Bébisol",
    "Curaprox", "Elgydium", "Sensodyne", "Oral-B", "Kelo-Cote", "Bi-Oil", "Somatoline",
    "Arkopharma", "Forté Pharma", "Forte Pharma", "Nutrisante", "Pileje", "Therascience",
    "Embryolisse", "Rilastil", "Mixa", "Neutrogena", "Garnier", "L'Oréal", "Lierac",
    "MartiDerm", "SkinCeuticals", "Institut Esthederm", "Sanoflore", "Dermina"
]

def clean_price(price_str):
    if not price_str:
        return "29.90"
    cleaned = re.sub(r"[^\d,\.]", "", price_str).replace(",", ".")
    try:
        val = float(cleaned)
        return f"{val:.2f}"
    except:
        return "29.90"

def extract_brand(title):
    for kb in known_brands:
        if re.search(r"\b" + re.escape(kb) + r"\b", title, re.IGNORECASE):
            return kb

    words = title.split()
    if not words:
        return "Laboratoire Officinal"

    first = words[0].strip().replace(":", "").replace("-", "")
    if len(first) > 1 and first.isupper():
        if len(words) > 1 and words[1].isupper() and len(words[1]) > 2:
            return f"{first} {words[1]}"
        return first
        
    return words[0].capitalize()

def slugify(text):
    text = text.lower()
    text = re.sub(r"[àáâãäå]", "a", text)
    text = re.sub(r"[èéêë]", "e", text)
    text = re.sub(r"[ìíîï]", "i", text)
    text = re.sub(r"[òóôõö]", "o", text)
    text = re.sub(r"[ùúûü]", "u", text)
    text = re.sub(r"[ç]", "c", text)
    text = re.sub(r"[^a-z0-9]+", "-", text)
    return text.strip("-")

def get_page_count(url, session):
    try:
        r = session.get(url, headers=headers, timeout=15)
        if r.status_code != 200:
            return 1
        soup = BeautifulSoup(r.text, "html.parser")
        page_nums = []
        for a in soup.select(".pagination a, ul.page-list a, .page-list a"):
            href = a.get("href", "")
            m = re.search(r"page=(\d+)", href)
            if m:
                page_nums.append(int(m.group(1)))
            elif a.get_text(strip=True).isdigit():
                page_nums.append(int(a.get_text(strip=True)))
                
        if page_nums:
            return min(max(page_nums), 90)
        return 1
    except:
        return 1

def fetch_page_products(cat_info, page_num, existing_titles, session):
    url = cat_info["url"]
    page_url = f"{url}?page={page_num}" if page_num > 1 else url
    try:
        r = session.get(page_url, headers=headers, timeout=15)
        if r.status_code != 200:
            return []
            
        soup = BeautifulSoup(r.text, "html.parser")
        products_el = soup.select("article.product-miniature, .js-product-miniature")
        if not products_el:
            return []

        new_items = []
        for p in products_el:
            title_el = p.select_one(".product-title a, h3 a, h2 a, .product-title")
            if not title_el:
                continue
            title = title_el.get_text(strip=True)
            if not title or len(title) < 3:
                continue

            normalized_title = title.lower().strip()
            with db_lock:
                if normalized_title in existing_titles:
                    continue
                existing_titles.add(normalized_title)

            # Price
            price_el = p.select_one(".price, span.price")
            price_str = price_el.get_text(strip=True) if price_el else ""
            price = clean_price(price_str)

            # Regular price
            reg_price_el = p.select_one(".regular-price, span.regular-price")
            original_price = clean_price(reg_price_el.get_text(strip=True)) if reg_price_el else None

            # Image URL
            img_el = p.select_one("img")
            img_url = ""
            if img_el:
                img_url = (img_el.get("data-full-size-image-url") or 
                           img_el.get("data-src") or 
                           img_el.get("src") or "")
                           
            if not img_url or "data:image" in img_url:
                img_url = "https://parafendri.tn/img/p-default.jpg"

            # Brand
            brand = extract_brand(title)
            
            # Taxonomy
            cat_name = cat_info["cat"]
            cat_slug = cat_info["cat_slug"]
            sub_name = cat_info["sub"]
            sub_slug = slugify(sub_name)

            short_desc = f"{title}. Soin dermatologique certifié et formulé par {brand}."
            full_desc = f"{title} de {brand}. Produit authentique disponible en parapharmacie officinale Natalya. Conseils d'utilisation d'experts et livraison rapide sur toute la Tunisie."

            # Tuple matching:
            # ("Name", "Brand", "ShortDescription", "FullDescription", "Price", "OriginalPrice", 
            #  "Rating", "ReviewCount", "ImageUrl", "CategorySlug", "CategoryName", "StockQuantity", 
            #  "InStock", "RxRequired", "BadgeText", "Ingredients", "DosageInstructions", "SubCategory", "SubCategorySlug")
            new_items.append((
                title, brand, short_desc, full_desc, price, original_price,
                4.8, 18, img_url, cat_slug, cat_name, 25,
                1, 0, "En Stock", "[]", "[]", sub_name, sub_slug
            ))

        return new_items
    except Exception as e:
        return []

def main():
    print(f"=========================================================")
    print(f"  NATALYA PARA - Full Catalog Scraper & Importer")
    print(f"  Target DB: {DB_PATH}")
    print(f"=========================================================")

    conn = sqlite3.connect(DB_PATH, check_same_thread=False)
    cursor = conn.cursor()

    cursor.execute('SELECT LOWER("Name") FROM "Products"')
    existing_titles = set(r[0] for r in cursor.fetchall())
    print(f"Current products in database: {len(existing_titles)}")

    session = requests.Session()
    session.headers.update(headers)

    # 1. Discover total pages for each category
    tasks = []
    print("\n[1/3] Calculating pages per category...")
    for idx, cat_info in enumerate(CATEGORY_MAPPINGS):
        pages = get_page_count(cat_info["url"], session)
        print(f" -> {cat_info['cat']} > {cat_info['sub']}: {pages} pages")
        for p in range(1, pages + 1):
            tasks.append((cat_info, p))

    print(f"\n[2/3] Fetching {len(tasks)} total pages using concurrent threads...")
    total_imported = 0
    start_time = time.time()

    def process_task(task):
        cat_info, page_num = task
        thread_session = requests.Session()
        thread_session.headers.update(headers)
        return fetch_page_products(cat_info, page_num, existing_titles, thread_session)

    batch = []
    with ThreadPoolExecutor(max_workers=8) as executor:
        futures = {executor.submit(process_task, t): t for t in tasks}
        completed_count = 0
        for future in as_completed(futures):
            completed_count += 1
            products = future.result()
            if products:
                batch.extend(products)

            # Insert batch every 50 products or at the end
            if len(batch) >= 50:
                with db_lock:
                    cursor.executemany("""
                        INSERT INTO "Products" 
                        ("Name", "Brand", "ShortDescription", "FullDescription", "Price", "OriginalPrice", 
                         "Rating", "ReviewCount", "ImageUrl", "CategorySlug", "CategoryName", "StockQuantity", 
                         "InStock", "RxRequired", "BadgeText", "Ingredients", "DosageInstructions", "SubCategory", "SubCategorySlug")
                        VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                    """, batch)
                    conn.commit()
                    total_imported += len(batch)
                    print(f"  Progress: {completed_count}/{len(tasks)} pages done. Imported so far: {total_imported} products.")
                    batch = []

    # Insert remaining batch
    if batch:
        cursor.executemany("""
            INSERT INTO "Products" 
            ("Name", "Brand", "ShortDescription", "FullDescription", "Price", "OriginalPrice", 
             "Rating", "ReviewCount", "ImageUrl", "CategorySlug", "CategoryName", "StockQuantity", 
             "InStock", "RxRequired", "BadgeText", "Ingredients", "DosageInstructions", "SubCategory", "SubCategorySlug")
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        """, batch)
        conn.commit()
        total_imported += len(batch)

    # 3. Update Category counts & sync Brands
    print("\n[3/3] Updating Category counts and Brands catalog...")
    cursor.execute("""
        UPDATE "Categories" 
        SET "ItemCount" = (
            SELECT COUNT(*) FROM "Products" 
            WHERE "Products"."CategorySlug" = "Categories"."Slug"
        )
    """)
    conn.commit()

    # Discover and populate new brands into Brands table
    cursor.execute('SELECT DISTINCT "Brand" FROM "Products" WHERE "Brand" IS NOT NULL AND "Brand" != ""')
    all_product_brands = [r[0] for r in cursor.fetchall()]

    cursor.execute('SELECT LOWER("Name") FROM "Brands"')
    existing_brand_names = set(r[0] for r in cursor.fetchall())

    new_brands_added = 0
    for b_name in all_product_brands:
        if b_name.lower() not in existing_brand_names:
            b_slug = slugify(b_name)
            initials = "".join([w[0].upper() for w in b_name.split()[:2]]) if b_name else "NP"
            try:
                cursor.execute("""
                    INSERT OR IGNORE INTO "Brands"
                    ("Slug", "Name", "Tagline", "Description", "Origin", "Category", "CategorySlug",
                     "LogoInitials", "Icon", "ProductCount", "SignatureProduct", "KeyFeatures",
                     "IsFeatured", "Rating", "BadgeText", "ImageUrl", "LogoUrl", "TutorialTag",
                     "VideoDuration", "RitualCategory", "ReviewCount", "Price", "OriginalPrice",
                     "BenefitDescription", "AssociatedProductId")
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
                """, (
                    b_slug, b_name, "Excellence Dermatologique & Soins Spécialisés",
                    f"Découvrez la gamme complète de soins {b_name} sélectionnés pour leur haute efficacité et tolérance optimale.",
                    "France & Tunisie", "Dermatologie", "visage", initials, "verified", 10,
                    f"Soin Phare {b_name}", "[]", 0, 4.8, "Marque Partenaire", "", "", "Expertise", "3 min", "Soin Quotidien", 45, "35.00", "42.00", "Haute tolérance", None
                ))
                new_brands_added += 1
            except Exception:
                pass
    conn.commit()

    # Final counts
    cursor.execute('SELECT COUNT(*) FROM "Products"')
    final_count = cursor.fetchone()[0]

    cursor.execute('SELECT COUNT(*) FROM "Brands"')
    brand_count = cursor.fetchone()[0]

    conn.close()
    elapsed = time.time() - start_time

    print(f"\n=========================================================")
    print(f"  SUCCESS! IMPORT FINISHED IN {elapsed:.1f}s")
    print(f"  Newly Imported: {total_imported} products")
    print(f"  Total Products in DB: {final_count}")
    print(f"  Total Brands in DB: {brand_count} (+{new_brands_added} added)")
    print(f"=========================================================")

if __name__ == "__main__":
    main()
