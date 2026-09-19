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

def classify_product(cat_slug, title, short_desc="", full_desc=""):
    full_text = f"{title} {short_desc} {full_desc}".lower()
    t = title.lower()

    # 1. --- MAMAN & BÉBÉ ---
    if cat_slug == "maman-bebe":
        # Biberons & Accessoires
        if any(k in full_text for k in [
            "biberon", "tetine", "tétine", "sucette", "tasse d'apprentissage", "tasse a bec", "tasse", 
            "goupillon", "bavoir", "tire-lait", "tire lait", "attache sucette", "attache-sucette",
            "anneau de dentition", "anneaux dentition", "cuillere", "cuillère", "assiette", "bol bebe",
            "sterilisateur", "stérilisateur", "chauffe biberon", "chauffe-biberon", "dodie biberon", "avent biberon",
            "wee baby", "babytime", "bebisol biberon", "boite a sucette", "boîte à sucette", "doseur lait", "brosse a biberon"
        ]):
            return "Biberons & Accessoires"
            
        # Laits & Alimentation
        if any(k in full_text for k in [
            "lait 1er age", "lait 2eme age", "lait 3eme age", "lait de croissance", "infantile", "guigoz", 
            "novalac", "aptamil", "bledina", "blédina", "cereale", "céréale", "bouillie", "picot", "modilac", 
            "nutriben", "nutribén", "bionutron", "bledine", "blédine", "petit pot", "puree bebe", "purée bébé"
        ]):
            return "Laits & Alimentation"
            
        # Change & Érythème
        if any(k in full_text for k in [
            "change", "siege", "siège", "erytheme", "érythème", "couche", "couches", "liniment", 
            "fesses", "mitosyl", "bepanthen", "bepanthol", "sudocrem", "pate a l eau", "pâte à l'eau",
            "creme change", "crème change", "spray change", "diaper"
        ]):
            return "Change & Érythème"
            
        # Soins Grossesse & Maternité
        if any(k in full_text for k in [
            "grossesse", "vergeture", "vergetures", "mamelon", "mamelons", "buste", "post-partum", 
            "maternite", "maternité", "coussinet d'allaitement", "coussinet", "creme allaitement", "baume allaitement",
            "bout de sein", "coquille allaitement", "creme vergeture", "huile vergeture"
        ]):
            return "Soins Grossesse & Maternité"
            
        # Default for maman-bebe:
        return "Soins & Toilette Bébé"

    # 2. --- SOINS CAPILLAIRES ---
    if cat_slug == "cheveux":
        # Anti-Chute & Repousse
        if any(k in full_text for k in [
            "anti-chute", "antichute", "chute", "repousse", "novophane lotion", "anaphase", "triphasic", 
            "forcapil lotion", "serum chute", "lotion chute", "perte de cheveux", "densifiant capillaire", "creastim", "neoptide"
        ]):
            return "Anti-Chute & Repousse"
            
        # Anti-Pelliculaire
        if any(k in full_text for k in [
            "anti-pelliculaire", "antipelliculaire", "pellicule", "pellicules", "squames", "ketoderm", 
            "selsun", "kelual", "dermatite seborrheique", "pityriasis"
        ]):
            return "Anti-Pelliculaire"
            
        # Après-Shampoings
        if any(k in full_text for k in [
            "apres-shampoing", "après-shampoing", "apres shampoing", "après shampoing", "conditioner", 
            "baume demelant", "baume démêlant", "soin demelant", "soin démêlant", "creme de rincage", "crème de rinçage"
        ]):
            return "Après-Shampoings"
            
        # Masques & Sérums
        if any(k in full_text for k in [
            "masque", "serum", "sérum", "bain d'huile", "elixir", "huile capillaire", "ampoule capillaire",
            "beurre capillaire", "botox capillaire", "soin sans rincage", "soin sans rinçage"
        ]):
            return "Masques & Sérums"
            
        # Cheveux Secs & Abîmés
        if any(k in full_text for k in [
            "cheveux secs", "cheveux abimes", "cheveux abîmés", "reparateur cheveux", "nourrissant cheveux", 
            "keratine cheveux", "kératine cheveux", "nutrition intense cheveux", "pointes fourchues"
        ]):
            return "Cheveux Secs & Abîmés"
            
        # Default for cheveux:
        return "Shampoings"

    # 3. --- PROTECTION SOLAIRE ---
    if cat_slug == "solaire":
        # Packs Solaires
        if any(k in full_text for k in ["pack", "trousse", "duo solaire", "offre solaire", "coffret solaire"]):
            return "Packs Solaires"
            
        # Après-Solaire & Réparateur
        if any(k in full_text for k in [
            "apres-solaire", "après-solaire", "apres solaire", "après solaire", "post-soleil", "post soleil", 
            "lait apres soleil", "reparateur solaire", "prolongateur de bronzage", "brûlure soleil", "coup de soleil"
        ]):
            return "Après-Solaire & Réparateur"
            
        # Solaires Bébé & Enfant
        if any(k in full_text for k in ["enfant", "enfants", "bebe", "bébé", "kids", "pediatrique", "pédiatrique", "junior", "bebes"]):
            return "Solaires Bébé & Enfant"
            
        # Fluides Solaires Teintés
        if any(k in full_text for k in ["teinte", "teinté", "teintee", "teintée", "bb creme solaire", "cc solaire", "claire", "doree", "dorée", "medium teinte"]):
            return "Fluides Solaires Teintés"
            
        # Huiles de Bronzage
        if any(k in full_text for k in ["huile bronzante", "huile de bronzage", "monoi", "monoï", "graisse a traire", "graisse à traire", "autobronzant", "accelerateur bronzage"]):
            return "Huiles de Bronzage"
            
        # Default for solaire:
        return "Crèmes Solaires Visage"

    # 4. --- HYGIÈNE & BEAUTÉ ---
    if cat_slug == "hygiene":
        # Hygiène Bucco-Dentaire
        if any(k in full_text for k in [
            "dentifrice", "brosse a dents", "brosse à dents", "fil dentaire", "bain de bouche", "blancheur dents", 
            "gencives", "parodont", "sensodyne", "elgydium", "oral-b", "oral b", "curaprox", "cure-dents", "orthodontique"
        ]):
            return "Hygiène Bucco-Dentaire"
            
        # Hygiène Intime
        if any(k in full_text for k in [
            "intime", "gel intime", "soin intime", "saforelle", "hydralin", "flore intime", "muqueuses", "secheresse intime"
        ]):
            return "Hygiène Intime"
            
        # Déodorants & Anti-Transpirants
        if any(k in full_text for k in [
            "deodorant", "déodorant", "anti-transpirant", "antitranspirant", "roll-on", "roll on", "stick deo", "spray deo", "pierre d'alun"
        ]):
            return "Déodorants & Anti-Transpirants"
            
        # Anti-Moustiques & Parasites
        if any(k in full_text for k in [
            "moustique", "moustiques", "repulsif", "répulsif", "piqure", "piqûre", "poux", "pou", "lentes", "anti-poux", "acarien", "acariens"
        ]):
            return "Anti-Moustiques & Parasites"
            
        # Default for hygiene:
        return "Douche & Bain"

    # 5. --- SOINS DU CORPS ---
    if cat_slug == "corps":
        # Gommages & Exfoliants
        if any(k in full_text for k in ["gommage", "exfoliant", "scrub", "peeling corps"]):
            return "Gommages & Exfoliants"
            
        # Soins Mains & Pieds
        if any(k in full_text for k in ["main", "mains", "pied", "pieds", "ongle", "ongles", "callosite", "callosités", "creme mains", "baume pieds", "talons fendilles"]):
            return "Soins Mains & Pieds"
            
        # Anti-Grattage & Sécheresse
        if any(k in full_text for k in ["anti-grattage", "antikrattage", "eczema", "eczéma", "atopie", "atoderm", "lipikar", "demangeaison", "démangeaison", "xerose", "xérose", "relipidant", "baume ap+"]):
            return "Anti-Grattage & Sécheresse"
            
        # Jambes Légères
        if any(k in full_text for k in ["jambes lourdes", "jambes legeres", "jambes légères", "circulation jambes", "veinotonique", "veines", "fraicheur jambes"]):
            return "Jambes Légères"
            
        # Soins Minceur & Fermeté
        if any(k in full_text for k in ["minceur corps", "fermete corps", "fermeté corps", "cellulite", "somatoline", "sculpteur", "amincissant corps", "vergeture corps"]):
            return "Soins Minceur & Fermeté"
            
        # Parfums & Brumes
        if any(k in full_text for k in ["parfum", "brume", "eau de cologne", "eau fraiche", "eau fraîche", "fragrance", "body mist"]):
            return "Parfums & Brumes"
            
        # Hygiène Corporelle
        if any(k in full_text for k in ["epilation", "épilation", "cire", "bande depilatoire", "rasoir corps"]):
            return "Hygiène Corporelle"
            
        # Default for corps:
        return "Hydratation & Nutrition"

    # 6. --- SOINS DU VISAGE ---
    if cat_slug == "visage":
        # Nettoyants & Démaquillants
        if any(k in full_text for k in [
            "nettoyant", "demaquillant", "démaquillant", "eau micellaire", "gel nettoyant", "mousse nettoyante", 
            "lait demaquillant", "lait démaquillant", "huile demaquillante", "huile démaquillante", "pain dermatologique"
        ]):
            return "Nettoyants & Démaquillants"
            
        # Anti-Imperfections & Acné
        if any(k in full_text for k in [
            "acne", "acné", "imperfection", "imperfections", "bouton", "boutons", "points noirs", "sebum", "sébum", 
            "matifiant", "effaclar", "sebionex", "hyséac", "hyseac", "dermopure", "keracnyl", "purifiant", "peaux grasses", "peaux mixtes"
        ]):
            return "Anti-Imperfections & Acné"
            
        # Anti-Âge & Rides
        if any(k in full_text for k in [
            "anti-age", "anti-âge", "anti age", "ride", "rides", "ridules", "fermete visage", "fermeté visage", 
            "lifting", "retinol", "rétinol", "acide hyaluronique", "collagene visage", "collagène visage", "hyaluron-filler", "resveratrol", "liftactiv"
        ]):
            return "Anti-Âge & Rides"
            
        # Anti-Taches & Éclat
        if any(k in full_text for k in [
            "anti-tache", "anti-taches", "depigmentant", "dépigmentant", "tache", "taches", "eclat", "éclat", 
            "vitamine c visage", "thiamidol", "melascreen", "pigmentclar", "clairial", "unifiant teint"
        ]):
            return "Anti-Taches & Éclat"
            
        # Contour des Yeux
        if any(k in full_text for k in ["contour des yeux", "yeux", "paupieres", "paupières", "cernes", "poches yeux", "anti-cernes"]):
            return "Contour des Yeux"
            
        # Soins des Lèvres
        if any(k in full_text for k in ["levres", "lèvres", "stick levres", "stick lèvres", "baume levres", "baume lèvres", "lip balm", "crevasse levre"]):
            return "Soins des Lèvres"
            
        # Peaux Sensibles & Rougeurs
        if any(k in full_text for k in [
            "rougeurs", "rosacee", "rosacée", "couperose", "peaux sensibles", "sensidiane", "roseliane", "roséliane", 
            "toleriane", "tolériane", "apaisant visage", "anti-rougeurs"
        ]):
            return "Peaux Sensibles & Rougeurs"
            
        # Peeling & Masques
        if any(k in full_text for k in ["masque visage", "peeling visage", "exfoliant visage", "gommage visage"]):
            return "Peeling & Masques"
            
        # Default for visage:
        return "Hydratation & Nutrition"

    # 7. --- SOINS HOMME ---
    if cat_slug == "homme":
        # Rasage & Après-Rasage
        if any(k in full_text for k in ["rasage", "apres-rasage", "après-rasage", "mousse a raser", "mousse à raser", "gel de rasage", "baume apres rasage"]):
            return "Rasage & Après-Rasage"
            
        # Capillaire & Barbe
        if any(k in full_text for k in ["barbe", "huile a barbe", "huile à barbe", "baume barbe", "shampoing barbe", "cire barbe"]):
            return "Capillaire & Barbe"
            
        # Hygiène & Déodorants
        if any(k in full_text for k in ["deodorant homme", "déodorant homme", "douche homme", "gel douche homme"]):
            return "Hygiène & Déodorants"
            
        # Default for homme:
        return "Soins Visage Homme"

    # 8. --- MATÉRIEL MÉDICAL & ORTHOPÉDIE ---
    if cat_slug == "materiel-medical":
        # Tensiomètres & Diagnostic
        if any(k in full_text for k in ["tensiometre", "tensiomètre", "glucometre", "glucomètre", "oxymetre", "oxymètre", "bandelette", "glycemie", "glycémie"]):
            return "Tensiomètres & Diagnostic"
            
        # Thermomètres
        if any(k in full_text for k in ["thermometre", "thermomètre", "temperature", "température"]):
            return "Thermomètres"
            
        # Chaussures Médicales
        if any(k in full_text for k in ["sabot", "sabots", "claquette", "claquettes", "chaussure", "chaussures", "semelle", "semelles", "talonnette"]):
            return "Chaussures Médicales"
            
        # Orthopédie & Maintien
        if any(k in full_text for k in [
            "orthopedie", "orthopédie", "genouillere", "genouillère", "chevillere", "chevillère", "ceinture lombaire", 
            "collier cervical", "attelle", "echarpe", "bandage", "bas de contention", "chaussette de contention", "maintien"
        ]):
            return "Orthopédie & Maintien"
            
        # Default for materiel-medical:
        return "Pansements & Premiers Secours"

    # 9. --- VITAMINES & COMPLÉMENTS ---
    if cat_slug == "vitamines":
        if any(k in full_text for k in ["sommeil", "melatonine", "mélatonine", "valeriane", "valériane", "passiflore", "stress", "serenite", "sérénité", "anxiete", "anxiété", "relax", "calme", "dormir", "nuit"]):
            return "Sommeil & Sérénité"
        if any(k in full_text for k in ["minceur", "detox", "détox", "bruleur", "brûleur", "draineur", "drainage", "coupe-faim", "silhouette", "ventre plat", "cellulite", "perte de poids", "amincissant"]):
            return "Minceur & Détox"
        if any(k in full_text for k in ["collagene", "collagène", "biotine", "cheveux", "ongles", "levure de biere", "levure de bière", "acide hyaluronique", "peau", "eclat", "éclat", "anti-chute", "forcapil", "anacaps", "novophane", "keratine", "kératine"]):
            return "Beauté Peau & Ongles"
        if any(k in full_text for k in ["articulation", "articulaire", "cartilage", "glucosamine", "chondroitine", "chondroïtine", "curcuma", "harpagophytum", "os", "calcium", "souplesse", "douleur articulaire", "arthrose"]):
            return "Articulations & Os"
        if any(k in full_text for k in ["immunite", "immunité", "defense", "défense", "propolis", "echinacee", "échinacée", "stimulant", "orl", "gorge", "respiratoire", "hiver", "vitamine c", "zinc", "gelee royale", "gelée royale", "ginseng"]):
            return "Immunité & Défenses"
        return "Vitamines & Énergie"

    return "Général"

def main():
    print("=== STARTING FULL CATALOG INTELLIGENT SIFTING ===")
    conn = sqlite3.connect(DB_PATH)
    conn.text_factory = lambda b: b.decode('utf-8', errors='ignore')
    c = conn.cursor()

    c.execute('SELECT Id, CategorySlug, Name, ShortDescription, FullDescription FROM Products')
    all_products = c.fetchall()
    print(f"Total products to classify: {len(all_products)}")

    updates = []
    category_summary = {}

    for p_id, cat_slug, name, s_desc, f_desc in all_products:
        sub_name = classify_product(cat_slug, name or "", s_desc or "", f_desc or "")
        sub_slug = slugify(sub_name)
        updates.append((sub_name, sub_slug, p_id))

        if cat_slug not in category_summary:
            category_summary[cat_slug] = {}
        category_summary[cat_slug][sub_name] = category_summary[cat_slug].get(sub_name, 0) + 1

    # Apply batch update
    c.executemany("""
        UPDATE Products 
        SET SubCategory = ?, SubCategorySlug = ? 
        WHERE Id = ?
    """, updates)

    # Sync Categories table ItemCount
    c.execute("""
        UPDATE Categories 
        SET ItemCount = (
            SELECT COUNT(Id) FROM Products WHERE Products.CategorySlug = Categories.Slug
        )
    """)
    conn.commit()

    print("\n========================================================")
    print("  FINAL CATALOG BREAKDOWN (ALL 9 RAYONS)")
    print("========================================================")
    for cat_slug, subs in sorted(category_summary.items()):
        total = sum(subs.values())
        print(f"\n[RAYON: {cat_slug.upper()}] - {total} produits au total:")
        for sub_name, count in sorted(subs.items(), key=lambda x: x[1], reverse=True):
            print(f"   -> {sub_name}: {count} produits")

    conn.close()
    print("\n========================================================")
    print(" SUCCESS! FULL CATALOG INTELLIGENTLY CLASSIFIED.")
    print("========================================================")

if __name__ == "__main__":
    main()
