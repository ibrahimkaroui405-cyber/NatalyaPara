import urllib.request
import time
from concurrent.futures import ThreadPoolExecutor
import sqlite3

conn = sqlite3.connect('App_Data/natalyapara.db')
c = conn.cursor()
c.execute("SELECT Id, ImageUrl FROM Products WHERE ImageUrl LIKE 'https://parafendri.tn%' LIMIT 20")
rows = c.fetchall()
conn.close()

def fetch(row):
    pid, url = row
    t0 = time.time()
    try:
        req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
        with urllib.request.urlopen(req, timeout=5) as r:
            data = r.read()
        return pid, len(data), time.time() - t0
    except Exception as e:
        return pid, str(e), time.time() - t0

t_start = time.time()
with ThreadPoolExecutor(max_workers=10) as ex:
    results = list(ex.map(fetch, rows))

print(f"Fetched {len(results)} images in {time.time() - t_start:.2f}s")
for r in results[:5]:
    print(r)

