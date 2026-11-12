import sqlite3

conn = sqlite3.connect('App_Data/natalyapara.db')
c = conn.cursor()
c.execute("SELECT name, sql FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'")
for r in c.fetchall():
    print(f"\n=== TABLE: {r[0]} ===")
    print(r[1])
conn.close()

