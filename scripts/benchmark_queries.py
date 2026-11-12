import sqlite3

conn = sqlite3.connect('App_Data/natalyapara.db')
c = conn.cursor()

queries = [
    ("Filter by Category (visage)", "SELECT Id, Name, Price FROM Products WHERE CategorySlug = 'visage'"),
    ("Filter by Brand (Eucerin)", "SELECT Id, Name, Price FROM Products WHERE Brand = 'Eucerin'"),
    ("Filter by Category + Brand (visage + Eucerin)", "SELECT Id, Name, Price FROM Products WHERE CategorySlug = 'visage' AND Brand = 'Eucerin'"),
    ("Sort by Price (low to high)", "SELECT Id, Name, Price FROM Products ORDER BY Price ASC LIMIT 20"),
    ("Sort Orders by Date", "SELECT OrderNumber, CustomerName, TotalAmount FROM Orders ORDER BY OrderDate DESC LIMIT 20"),
    ("Filter Orders by Status", "SELECT OrderNumber, CustomerName FROM Orders WHERE Status = 'En attente'"),
    ("Find Order items by ProductId", "SELECT * FROM OrderItems WHERE ProductId = 2974"),
    ("Find Stock Movements by ProductId", "SELECT * FROM StockMovements WHERE ProductId = 2974 ORDER BY Timestamp DESC")
]

print("=== VERIFYING SQLITE QUERY PLANS & INDEX USE ===")
for label, sql in queries:
    print(f"\n[QUERY] {label}:")
    c.execute(f"EXPLAIN QUERY PLAN {sql}")
    for row in c.fetchall():
        print(f"  --> {row[3]}")

conn.close()
