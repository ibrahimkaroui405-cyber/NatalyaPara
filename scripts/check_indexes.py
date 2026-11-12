import sqlite3

conn = sqlite3.connect('App_Data/natalyapara.db')
c = conn.cursor()
c.execute("SELECT tbl_name, name, sql FROM sqlite_master WHERE type='index'")
for r in c.fetchall():
    print(f"Table: {r[0]:<15} | Index: {r[1]:<30} | SQL: {r[2]}")
conn.close()

