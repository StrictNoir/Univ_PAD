## Web Proxy

# Tema: Implementarea unui serviciu proxy HTTP pentru medierea accesului la noduri distribuite de tip Data Warehouse.

# 🎯 Scopul proiectului

Realizarea unui proxy transparent care intermediază conexiunea dintre Client și Data-Warehouse (DW), simulând distribuirea datelor semi-structurate prin protocoale HTTP (GET, PUT, POST, opțional PUSH).

🔍 Idee-cheie: Clientul NU trebuie să știe sursa reală a datelor → Proxy-ul devine punct unic de acces.

# 🎯 Obiective principale

✅ Implementarea unui server proxy HTTP în C#
✅ Gestionarea cererilor prin ASP.NET Core Middleware
✅ Acceptarea și rutarea cererilor: GET, PUT, POST, PUSH (opțional)
✅ Simularea unor noduri externe de date (/employees-json, /employees-xml, etc.)
✅ Posibilitate extindere cu cache / load balancing
