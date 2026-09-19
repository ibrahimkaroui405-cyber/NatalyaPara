import sqlite3
import os

DB_PATH = os.path.join(os.path.dirname(__file__), "App_Data", "natalyapara.db")
conn = sqlite3.connect(DB_PATH)
c = conn.cursor()

c.execute("UPDATE Categories SET Slug='visage' WHERE Slug='soins-visage'")
c.execute("UPDATE Categories SET Slug='maman-bebe' WHERE Slug='soins-bebe'")
c.execute("UPDATE Products SET CategorySlug='visage' WHERE CategorySlug='soins-visage'")
c.execute("UPDATE Products SET CategorySlug='maman-bebe' WHERE CategorySlug='soins-bebe'")
c.execute("""
    UPDATE Categories 
    SET ItemCount = (
        SELECT COUNT(Id) FROM Products WHERE Products.CategorySlug = Categories.Slug
    )
""")
conn.commit()

print("Categories and counts after sync:")
for row in c.execute("SELECT Slug, Name, ItemCount FROM Categories").fetchall():
    print(f"  {row[0]}: {row[1]} -> {row[2]} products")

print(f"\nTotal Products: {c.execute('SELECT COUNT(Id) FROM Products').fetchone()[0]}")
print(f"Total Brands: {c.execute('SELECT COUNT(Slug) FROM Brands').fetchone()[0]}")

conn.close()
