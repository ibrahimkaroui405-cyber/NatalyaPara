# -*- coding: utf-8 -*-
import sqlite3
import os
import re

DB_PATH = os.path.join(os.path.dirname(__file__), "App_Data", "natalyapara.db")

def slugify(text):
    if not text:
        return ""
    text = text.lower()
    text = re.sub(r"[àáâãäå]", "a", text)
    text = re.sub(r"[èéêë]", "e", text)
    text = re.sub(r"[ìíîï]", "i", text)
    text = re.sub(r"[òóôõö]", "o", text)
    text = re.sub(r"[ùúûü]", "u", text)
    text = re.sub(r"[ç]", "c", text)
    text = re.sub(r"[^a-z0-9]+", "-", text)
    return text.strip("-")

def main():
    conn = sqlite3.connect(DB_PATH)
    conn.text_factory = lambda b: b.decode('utf-8', errors='ignore')
    c = conn.cursor()

    # 1. First, fix all Category Names & Slugs in Categories table
    clean_categories = [
        ("visage", "Soins du Visage", "face_retouching_natural", '["Nettoyants & Démaquillants","Anti-Âge & Rides","Anti-Imperfections & Acné","Anti-Taches & Éclat","Contour des Yeux","Hydratation & Nutrition","Peaux Sensibles & Rougeurs","Peeling & Masques","Soins des Lèvres"]'),
        ("cheveux", "Soins Capillaires", "brush", '["Shampoings","Après-Shampoings","Masques & Sérums","Anti-Chute & Repousse","Cheveux Secs & Abîmés","Anti-Pelliculaire"]'),
        ("corps", "Soins du Corps", "spa", '["Hydratation & Nutrition","Hygiène Corporelle","Gommages & Exfoliants","Soins Mains & Pieds","Anti-Grattage & Sécheresse","Jambes Légères","Soins Minceur & Fermeté","Parfums & Brumes"]'),
        ("solaire", "Protection Solaire", "sunny", '["Crèmes Solaires Visage","Fluides Solaires Teintés","Packs Solaires","Après-Solaire & Réparateur","Solaires Bébé & Enfant","Huiles de Bronzage"]'),
        ("maman-bebe", "Maman & Bébé", "child_care", '["Soins & Toilette Bébé","Change & Érythème","Laits & Alimentation","Biberons & Accessoires","Soins Grossesse & Maternité"]'),
        ("hygiene", "Hygiène & Beauté", "sanitizer", '["Douche & Bain","Hygiène Bucco-Dentaire","Hygiène Intime","Déodorants & Anti-Transpirants","Anti-Moustiques & Parasites"]'),
        ("vitamines", "Vitamines & Compléments", "medication", '["Vitamines & Énergie","Immunité & Défenses","Minceur & Détox","Sommeil & Sérénité","Articulations & Os","Beauté Peau & Ongles"]'),
        ("homme", "Soins Homme", "face_6", '["Rasage & Après-Rasage","Soins Visage Homme","Capillaire & Barbe","Hygiène & Déodorants"]'),
        ("materiel-medical", "Matériel Médical & Orthopédie", "medical_services", '["Tensiomètres & Diagnostic","Thermomètres","Pansements & Premiers Secours","Orthopédie & Maintien","Chaussures Médicales"]')
    ]

    for slug, name, icon, subcats in clean_categories:
        c.execute("""
            UPDATE Categories 
            SET Name = ?, Icon = ?, SubCategories = ? 
            WHERE Slug = ?
        """, (name, icon, subcats, slug))
        c.execute("UPDATE Products SET CategoryName = ? WHERE CategorySlug = ?", (name, slug))

    # 2. Intelligent classifier for Vitamines & Compléments
    c.execute('SELECT Id, Name, ShortDescription, FullDescription FROM Products WHERE CategorySlug = "vitamines"')
    vitamine_prods = c.fetchall()

    def classify_vitamine(title, short_d, full_d):
        text = f"{title} {short_d} {full_d}".lower()
        
        # Sommeil & Sérénité
        if any(k in text for k in ["sommeil", "melatonine", "mélatonine", "valeriane", "valériane", "passiflore", "stress", "serenite", "sérénité", "anxiete", "anxiété", "relax", "calme", "dormir", "nuit", "magnesium", "magnésium"]):
            if any(k in text for k in ["sommeil", "melatonine", "mélatonine", "valeriane", "passiflore", "stress", "relax", "dormir", "nuit"]):
                return "Sommeil & Sérénité"

        # Minceur & Détox
        if any(k in text for k in ["minceur", "detox", "détox", "bruleur", "brûleur", "draineur", "drainage", "coupe-faim", "silhouette", "ventre plat", "cellulite", "perte de poids", "amincissant", "the vert", "thé vert", "artichaut"]):
            return "Minceur & Détox"

        # Beauté Peau & Ongles
        if any(k in text for k in ["collagene", "collagène", "biotine", "cheveux", "ongles", "levure de biere", "levure de bière", "acide hyaluronique", "peau", "eclat", "éclat", "anti-chute", "forcapil", "anacaps", "novophane", "keratine", "kératine"]):
            return "Beauté Peau & Ongles"

        # Articulations & Os
        if any(k in text for k in ["articulation", "articulaire", "cartilage", "glucosamine", "chondroitine", "chondroïtine", "curcuma", "harpagophytum", "os", "calcium", "souplesse", "douleur articulaire", "arthrose"]):
            return "Articulations & Os"

        # Immunité & Défenses
        if any(k in text for k in ["immunite", "immunité", "defense", "défense", "propolis", "echinacee", "échinacée", "stimulant", "orl", "gorge", "respiratoire", "hiver", "vitamine c", "zinc", "gelee royale", "gelée royale", "ginseng"]):
            return "Immunité & Défenses"

        # Vitamines & Énergie (Default)
        return "Vitamines & Énergie"

    vit_updated = 0
    for p_id, title, s_desc, f_desc in vitamine_prods:
        subcat = classify_vitamine(title, s_desc or "", f_desc or "")
        subcat_slug = slugify(subcat)
        c.execute("""
            UPDATE Products 
            SET SubCategory = ?, SubCategorySlug = ? 
            WHERE Id = ?
        """, (subcat, subcat_slug, p_id))
        vit_updated += 1

    # 3. Clean and fix all other subcategories across the entire database
    subcat_replacements = {
        # Capillaires
        "shampoings": ("Shampoings", "shampoings"),
        "shampoings traitants": ("Shampoings", "shampoings"),
        "apres-shampoing": ("Après-Shampoings", "apres-shampoings"),
        "apres-shampoings": ("Après-Shampoings", "apres-shampoings"),
        "masques": ("Masques & Sérums", "masques-serums"),
        "masques & serums": ("Masques & Sérums", "masques-serums"),
        "anti-chute & repousse": ("Anti-Chute & Repousse", "anti-chute-repousse"),
        
        # Solaire
        "creme solaire": ("Crèmes Solaires Visage", "cremes-solaires-visage"),
        "cremes solaires visage": ("Crèmes Solaires Visage", "cremes-solaires-visage"),
        "pack solaire": ("Packs Solaires", "packs-solaires"),
        "packs solaires": ("Packs Solaires", "packs-solaires"),
        "apres-solaire": ("Après-Solaire & Réparateur", "apres-solaire-reparateur"),
        "apres-solaire & reparateur": ("Après-Solaire & Réparateur", "apres-solaire-reparateur"),
        "solaire bebes enfants": ("Solaires Bébé & Enfant", "solaires-bebe-enfant"),
        "solaires bebe & enfant": ("Solaires Bébé & Enfant", "solaires-bebe-enfant"),
        
        # Corps
        "hydratation & nutrition": ("Hydratation & Nutrition", "hydratation-nutrition"),
        "hygiene corporelle": ("Hygiène Corporelle", "hygiene-corporelle"),
        "gommages & exfoliants": ("Gommages & Exfoliants", "gommages-exfoliants"),
        "soins mains & pieds": ("Soins Mains & Pieds", "soins-mains-pieds"),
        "anti-grattage & secheresse": ("Anti-Grattage & Sécheresse", "anti-grattage-secheresse"),
        "jambes legeres": ("Jambes Légères", "jambes-legeres"),
        "soins minceur & fermete": ("Soins Minceur & Fermeté", "soins-minceur-fermete"),
        "parfums & brumes": ("Parfums & Brumes", "parfums-brumes"),
        "huiles & massages": ("Hydratation & Nutrition", "hydratation-nutrition"),

        # Visage
        "soins quotidiens visage": ("Hydratation & Nutrition", "hydratation-nutrition"),
        "soins specifiques": ("Hydratation & Nutrition", "hydratation-nutrition"),
        "nettoyants & demaquillants": ("Nettoyants & Démaquillants", "nettoyants-demaquillants"),
        "anti-age & rides": ("Anti-Âge & Rides", "anti-age-rides"),
        "anti-imperfections & acne": ("Anti-Imperfections & Acné", "anti-imperfections-acne"),
        "anti-taches & eclat": ("Anti-Taches & Éclat", "anti-taches-eclat"),
        "contour des yeux": ("Contour des Yeux", "contour-des-yeux"),
        "peaux sensibles & rougeurs": ("Peaux Sensibles & Rougeurs", "peaux-sensibles-rougeurs"),
        "peeling & masques": ("Peeling & Masques", "peeling-masques"),
        "soins des levres": ("Soins des Lèvres", "soins-des-levres"),

        # Maman & Bebe
        "soins & toilette bebe": ("Soins & Toilette Bébé", "soins-toilette-bebe"),
        "change & erytheme": ("Change & Érythème", "change-erytheme"),
        "biberons & accessoires": ("Biberons & Accessoires", "biberons-accessoires"),
        "soins grossesse & maternite": ("Soins Grossesse & Maternité", "soins-grossesse-maternite"),

        # Hygiene
        "douche & bain": ("Douche & Bain", "douche-bain"),
        "hygiene bucco-dentaire": ("Hygiène Bucco-Dentaire", "hygiene-bucco-dentaire"),
        "hygiene intime": ("Hygiène Intime", "hygiene-intime"),
        "deodorants & anti-transpirants": ("Déodorants & Anti-Transpirants", "deodorants-anti-transpirants"),
        "anti-moustiques & parasites": ("Anti-Moustiques & Parasites", "anti-moustiques-parasites"),
        "beaute & eclat": ("Douche & Bain", "douche-bain"),
        "coffrets & cadeaux": ("Douche & Bain", "douche-bain"),

        # Homme
        "rasage & apres-rasage": ("Rasage & Après-Rasage", "rasage-apres-rasage"),
        "soins visage homme": ("Soins Visage Homme", "soins-visage-homme"),
        "hygiene homme": ("Hygiène & Déodorants", "hygiene-deodorants"),

        # Materiel Medical
        "pansements & premiers secours": ("Pansements & Premiers Secours", "pansements-premiers-secours"),
        "chaussures medicales": ("Chaussures Médicales", "chaussures-medicales"),
        "orthopedie & maintien": ("Orthopédie & Maintien", "orthopedie-maintien")
    }

    c.execute('SELECT Id, SubCategory, CategorySlug FROM Products WHERE CategorySlug != "vitamines"')
    other_prods = c.fetchall()
    for p_id, sub, cat_slug in other_prods:
        if not sub:
            continue
        cleaned_sub = slugify(sub).replace("-", " ")
        if cleaned_sub in subcat_replacements:
            new_sub, new_slug = subcat_replacements[cleaned_sub]
            c.execute("UPDATE Products SET SubCategory = ?, SubCategorySlug = ? WHERE Id = ?", (new_sub, new_slug, p_id))
        else:
            c.execute("UPDATE Products SET SubCategorySlug = ? WHERE Id = ?", (slugify(sub), p_id))

    # 4. Update category ItemCount
    c.execute("""
        UPDATE Categories 
        SET ItemCount = (
            SELECT COUNT(Id) FROM Products WHERE Products.CategorySlug = Categories.Slug
        )
    """)
    conn.commit()

    print("\n--- VITAMINES & COMPLÉMENTS BREAKDOWN ---")
    c.execute('SELECT SubCategory, COUNT(*) FROM Products WHERE CategorySlug="vitamines" GROUP BY SubCategory')
    for row in c.fetchall():
        print(f"  {row[0]}: {row[1]} produits")

    print("\n--- ALL CATEGORIES & SUB-CATEGORIES ---")
    c.execute('SELECT CategorySlug, SubCategory, COUNT(*) FROM Products GROUP BY CategorySlug, SubCategory ORDER BY CategorySlug, COUNT(*) DESC')
    for row in c.fetchall():
        print(f"  [{row[0]}] {row[1]}: {row[2]} produits")

    conn.close()

if __name__ == "__main__":
    main()
