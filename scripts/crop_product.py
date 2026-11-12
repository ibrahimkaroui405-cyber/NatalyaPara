"""
Utility script to auto-crop whitespace from any product image in the database.
Usage:
    python scripts/crop_product.py <product_id>
Example:
    python scripts/crop_product.py 2974
"""
import sys
import os
import io
import urllib.request
import sqlite3
from PIL import Image
import numpy as np

def crop_product(product_id: int):
    db_path = os.path.join(os.path.dirname(__file__), '..', 'App_Data', 'natalyapara.db')
    conn = sqlite3.connect(db_path)
    c = conn.cursor()
    c.execute("SELECT Name, ImageUrl FROM Products WHERE Id = ?", (product_id,))
    row = c.fetchone()
    if not row:
        print(f"Product {product_id} not found.")
        conn.close()
        return

    name, url = row
    print(f"Product {product_id}: {name}")
    print(f"Current Image: {url}")

    if not url.startswith("http"):
        print("Image is already local or not a remote URL.")
        conn.close()
        return

    req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
    try:
        with urllib.request.urlopen(req) as resp:
            data = resp.read()
    except Exception as e:
        print(f"Failed to download image: {e}")
        conn.close()
        return

    img = Image.open(io.BytesIO(data))
    img_rgba = img.convert('RGBA')
    arr = np.array(img_rgba)

    alpha = arr[:, :, 3]
    is_visible = alpha > 20
    not_white = (arr[:, :, 0] < 240) | (arr[:, :, 1] < 240) | (arr[:, :, 2] < 240)
    item_mask = is_visible & not_white

    coords = np.argwhere(item_mask)
    if len(coords) == 0:
        print("Could not isolate product bounding box.")
        conn.close()
        return

    y_min, x_min = coords.min(axis=0)
    y_max, x_max = coords.max(axis=0)
    item_w = x_max - x_min
    item_h = y_max - y_min
    max_dim = max(item_w, item_h)
    pad = int(max_dim * 0.1)

    center_x = (x_min + x_max) // 2
    center_y = (y_min + y_max) // 2
    half_side = (max_dim + pad * 2) // 2

    c_x1 = max(0, center_x - half_side)
    c_y1 = max(0, center_y - half_side)
    c_x2 = min(img.size[0], center_x + half_side)
    c_y2 = min(img.size[1], center_y + half_side)

    cropped = img.crop((c_x1, c_y1, c_x2, c_y2))

    final_img = Image.new('RGB', (600, 600), (255, 255, 255))
    ratio = cropped.size[0] / cropped.size[1]
    if ratio > 1:
        new_w = 520
        new_h = int(520 / ratio)
    else:
        new_h = 520
        new_w = int(520 * ratio)

    resized_item = cropped.resize((new_w, new_h), Image.Resampling.LANCZOS)
    paste_x = (600 - new_w) // 2
    paste_y = (600 - new_h) // 2

    if resized_item.mode == 'RGBA':
        final_img.paste(resized_item, (paste_x, paste_y), resized_item)
    else:
        final_img.paste(resized_item, (paste_x, paste_y))

    products_dir = os.path.join(os.path.dirname(__file__), '..', 'wwwroot', 'images', 'products')
    os.makedirs(products_dir, exist_ok=True)
    out_file = f"{product_id}.webp"
    out_path = os.path.join(products_dir, out_file)
    final_img.save(out_path, 'WEBP', quality=92)

    local_url = f"/images/products/{out_file}"
    c.execute("UPDATE Products SET ImageUrl = ? WHERE Id = ?", (local_url, product_id))
    conn.commit()
    conn.close()
    print(f"Successfully cropped & saved to {local_url}!")

if __name__ == '__main__':
    if len(sys.argv) < 2:
        print("Usage: python scripts/crop_product.py <product_id>")
    else:
        crop_product(int(sys.argv[1]))

