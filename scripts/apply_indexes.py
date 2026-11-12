import sqlite3
import time

conn = sqlite3.connect('App_Data/natalyapara.db')
c = conn.cursor()

# Clean up any misplaced index attempts
c.execute('DROP INDEX IF EXISTS "IX_Orders_CreatedAt"')
c.execute('DROP INDEX IF EXISTS "IX_Orders_CustomerPhone"')
c.execute('DROP INDEX IF EXISTS "IX_Orders_CustomerEmail"')
c.execute('DROP INDEX IF EXISTS "IX_StockMovements_CreatedAt"')

indexes = [
    # Product Indexes (Catalog, Brand, Category, Price, Stock)
    ("IX_Products_CategorySlug", 'CREATE INDEX IF NOT EXISTS "IX_Products_CategorySlug" ON "Products" ("CategorySlug")'),
    ("IX_Products_SubCategorySlug", 'CREATE INDEX IF NOT EXISTS "IX_Products_SubCategorySlug" ON "Products" ("SubCategorySlug")'),
    ("IX_Products_Brand", 'CREATE INDEX IF NOT EXISTS "IX_Products_Brand" ON "Products" ("Brand")'),
    ("IX_Products_InStock", 'CREATE INDEX IF NOT EXISTS "IX_Products_InStock" ON "Products" ("InStock")'),
    ("IX_Products_Price", 'CREATE INDEX IF NOT EXISTS "IX_Products_Price" ON "Products" ("Price")'),
    ("IX_Products_IsFeatured", 'CREATE INDEX IF NOT EXISTS "IX_Products_IsFeatured" ON "Products" ("IsFeatured")'),
    ("IX_Products_Category_Brand", 'CREATE INDEX IF NOT EXISTS "IX_Products_Category_Brand" ON "Products" ("CategorySlug", "Brand")'),
    
    # Order & Tracking Indexes
    ("IX_Orders_OrderDate", 'CREATE INDEX IF NOT EXISTS "IX_Orders_OrderDate" ON "Orders" ("OrderDate")'),
    ("IX_Orders_Status", 'CREATE INDEX IF NOT EXISTS "IX_Orders_Status" ON "Orders" ("Status")'),
    ("IX_Orders_Phone", 'CREATE INDEX IF NOT EXISTS "IX_Orders_Phone" ON "Orders" ("Phone")'),
    ("IX_Orders_Email", 'CREATE INDEX IF NOT EXISTS "IX_Orders_Email" ON "Orders" ("Email")'),
    
    # OrderItem & Stock Movement Indexes
    ("IX_OrderItems_ProductId", 'CREATE INDEX IF NOT EXISTS "IX_OrderItems_ProductId" ON "OrderItems" ("ProductId")'),
    ("IX_StockMovements_ProductId", 'CREATE INDEX IF NOT EXISTS "IX_StockMovements_ProductId" ON "StockMovements" ("ProductId")'),
    ("IX_StockMovements_Timestamp", 'CREATE INDEX IF NOT EXISTS "IX_StockMovements_Timestamp" ON "StockMovements" ("Timestamp")'),
    
    # Offers & Reels Ordering
    ("IX_SpecialOffers_IsActive_DisplayOrder", 'CREATE INDEX IF NOT EXISTS "IX_SpecialOffers_IsActive_DisplayOrder" ON "SpecialOffers" ("IsActive", "DisplayOrder")'),
    ("IX_StoryReels_IsActive_DisplayOrder", 'CREATE INDEX IF NOT EXISTS "IX_StoryReels_IsActive_DisplayOrder" ON "StoryReels" ("IsActive", "DisplayOrder")')
]

print("Applying verified performance indexes...")
t0 = time.time()
for name, sql in indexes:
    c.execute(sql)
    print(f"  + Verified index: {name}")

conn.commit()

# Run ANALYZE to update query statistics for SQLite query planner
c.execute("ANALYZE")
conn.commit()

elapsed = time.time() - t0
print(f"\nAll {len(indexes)} indexes successfully verified and analyzed in {elapsed:.3f}s!")

conn.close()
