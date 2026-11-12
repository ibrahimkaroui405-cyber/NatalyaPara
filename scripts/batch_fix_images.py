import os
import io
import time
import urllib.request
import sqlite3
from concurrent.futures import ThreadPoolExecutor
from PIL import Image
import numpy as np

DB_PATH = os.path.join(os.path.dirname(__file__), '..', 'App_Data', 'natalyapara.db')
PRODUCTS_DIR = os.path.join(os.path.dirname(__file__), '..', 'wwwroot', 'images', 'products')
os.makedirs(PRODUCTS_DIR, exist_ok=True)

def process_single(row):
    pid, url = row
    if not url or not url.startswith('http'):
        return None

    try:
        req = urllib.request.Request(url, headers={
            'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36'
        })
        with urllib.request.urlopen(req, timeout=5) as resp:
            data = resp.read()

        img = Image.open(io.BytesIO(data))
        img_rgba = img.convert('RGBA')
        arr = np.array(img_rgba)

        alpha = arr[:, :, 3]
        not_white = (arr[:, :, 0] < 242) | (arr[:, :, 1] < 242) | (arr[:, :, 2] < 242)
        item_mask = (alpha > 20) & not_white

        coords = np.argwhere(item_mask)
        if len(coords) < 30:
            return None

        y_min, x_min = coords.min(axis=0)
        y_max, x_max = coords.max(axis=0)

        w = x_max - x_min
        h = y_max - y_min
        img_w, img_h = img.size

        ratio_w = w / img_w
        ratio_h = h / img_h
        max_ratio = max(ratio_w, ratio_h)

        # If the item fills less than 72% of canvas, or height is less than 50%
        # it definitely has excessive empty space!
        if max_ratio < 0.72 or ratio_h < 0.50:
            max_dim = max(w, h)
            pad = int(max_dim * 0.08)

            center_x = (x_min + x_max) // 2
            center_y = (y_min + y_max) // 2
            half_side = (max_dim + pad * 2) // 2

            c_x1 = max(0, center_x - half_side)
            c_y1 = max(0, center_y - half_side)
            c_x2 = min(img_w, center_x + half_side)
            c_y2 = min(img_h, center_y + half_side)

            cropped = img.crop((c_x1, c_y1, c_x2, c_y2))

            final_img = Image.new('RGB', (600, 600), (255, 255, 255))
            c_w, c_h = cropped.size
            if c_w > c_h:
                new_w = 540
                new_h = int(540 * (c_h / c_w))
            else:
                new_h = 540
                new_w = int(540 * (c_w / c_h))

            resized_item = cropped.resize((new_w, new_h), Image.Resampling.LANCZOS)
            paste_x = (600 - new_w) // 2
            paste_y = (600 - new_h) // 2

            if resized_item.mode == 'RGBA':
                final_img.paste(resized_item, (paste_x, paste_y), resized_item)
            else:
                final_img.paste(resized_item, (paste_x, paste_y))

            out_file = f"{pid}.webp"
            out_path = os.path.join(PRODUCTS_DIR, out_file)
            final_img.save(out_path, 'WEBP', quality=90)

            local_url = f"/images/products/{out_file}"
            return (pid, local_url)
        return None

    except Exception:
        return None

def run_fixer(batch_size=100, max_batches=None):
    conn = sqlite3.connect(DB_PATH)
    c = conn.cursor()
    # Select products that still point to remote http URLs
    c.execute("SELECT Id, ImageUrl FROM Products WHERE ImageUrl LIKE 'http%'")
    rows = c.fetchall()
    conn.close()

    total = len(rows)
    print(f"Total remote products to inspect: {total}")
    if total == 0:
        print("All products are already locally optimized!")
        return

    num_batches = (total + batch_size - 1) // batch_size
    if max_batches:
        num_batches = min(num_batches, max_batches)

    total_fixed = 0
    t_start = time.time()

    for b in range(num_batches):
        batch = rows[b * batch_size : (b + 1) * batch_size]
        t_batch = time.time()

        with ThreadPoolExecutor(max_workers=16) as ex:
            results = list(ex.map(process_single, batch))

        updates = [(r[1], r[0]) for r in results if r]
        if updates:
            conn = sqlite3.connect(DB_PATH)
            c = conn.cursor()
            c.executemany("UPDATE Products SET ImageUrl = ? WHERE Id = ?", updates)
            conn.commit()
            conn.close()
            total_fixed += len(updates)

        elapsed = time.time() - t_batch
        print(f"[{b+1}/{num_batches}] Batch of {len(batch)} done in {elapsed:.1f}s | Fixed this batch: {len(updates)} | Total fixed so far: {total_fixed}")

    total_elapsed = time.time() - t_start
    print(f"DONE! Processed {total} products in {total_elapsed/60:.1f} minutes. Total small images auto-cropped: {total_fixed}")

if __name__ == '__main__':
    run_fixer()
