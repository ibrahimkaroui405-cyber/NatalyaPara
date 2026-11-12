import sqlite3

migration_id = "20261112180559_InitialCreate"
product_version = "9.0.0"

conn = sqlite3.connect('App_Data/natalyapara.db')
c = conn.cursor()

c.execute('''
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
    "ProductVersion" TEXT NOT NULL
);
''')

c.execute('''
INSERT OR IGNORE INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES (?, ?);
''', (migration_id, product_version))

conn.commit()

c.execute("SELECT * FROM __EFMigrationsHistory")
rows = c.fetchall()
print("Recorded migrations in __EFMigrationsHistory:")
for r in rows:
    print(" ", r)

conn.close()

