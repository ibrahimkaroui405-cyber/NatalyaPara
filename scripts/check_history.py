import sqlite3
conn = sqlite3.connect('App_Data/natalyapara.db')
c = conn.cursor()
c.execute("SELECT name FROM sqlite_master WHERE type='table' AND name='__EFMigrationsHistory'")
print("Migration history table:", c.fetchall())
conn.close()

