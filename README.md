<img width="404" height="225" alt="Logo" src="https://github.com/user-attachments/assets/8fba6515-512d-4d86-a519-7dded216a256" />


# 🚀 QuickDocs v2.0

QuickDocs es una aplicación de escritorio liviana, eficiente y multiplataforma diseñada para la gestión rápida de documentos comerciales. Ideal para pequeños talleres y negocios que necesitan profesionalizar su facturación administrativa sin complicaciones.

Esta versión 2.0 redefine el proyecto mediante una arquitectura desacoplada (Front-End / Back-End), mejorando el rendimiento, la mantenibilidad y permitiendo su ejecución nativa tanto en **Linux** como en **Windows**.

---

## ✨ Funcionalidades Principales

* **Configuración de Perfil:** Personaliza tus documentos con el nombre de tu negocio, CUIT/CUIL, dirección, teléfono y logo empresarial.
* **Gestión de Catálogos:** Módulos integrados y optimizados para administrar clientes, productos y servicios con sugerencias de búsqueda dinámica en tiempo real.
* **Generación de Documentos:** Creación de PDFs con diseño profesional y descarga directa para:
  * 📋 Presupuestos
  * 📦 Remitos 
  * 🧾 Recibos de cobro
  * 💳 Notas de Crédito

<img width="1538" height="827" alt="Screenshot_2026-06-20_17-34-16" src="https://github.com/user-attachments/assets/5d0379b8-fc42-4c21-a024-2771b32e5739" />

* **Historial Centralizado:** Acceso y control total de todos los documentos generados con funciones de:
  * 🔍 Búsqueda inteligente por cliente, tipo o número de documento.
  * 🖨️ Reimpresión y apertura instantánea de PDFs físicos directamente desde la interfaz.
  * 🗑️ Eliminación segura con integridad referencial en cascada a nivel Base de Datos.

---

## 🛠️ Arquitectura y Tecnologías

El proyecto fue migrado de una estructura monolítica a un modelo distribuido y robusto:

### 🖥️ Front-End (Interfaz de Usuario)
* **Framework:** [Avalonia UI](https://avaloniaui.net/) (.NET 8) - Permite una interfaz moderna, fluida y **100% multiplataforma**, con proyectos head listos para Desktop, Android, iOS y Browser sobre una base de código compartida.
* **Patrón de Diseño:** MVVM (Model-View-ViewModel) utilizando el **CommunityToolkit.Mvvm** para un manejo de estado limpio y reactivo.
* **Comunicación:** Consumo asíncrono de servicios mediante `HttpClient`.

### ⚙️ Back-End & Datos (API)
* **Framework:** ASP.NET Core Web API (.NET 8).
* **Base de Datos:** SQLite gestionado a través de **Entity Framework Core** para un acceso eficiente y seguro a los datos.
* **Motor de PDF:** QuestPDF (Motor de maquetación profesional basado en código).
* **Calidad de Código:** Compilación estricta con manejo de tipos de referencia nulos (*Nullables*) logrando un entorno **libre de warnings**.

---

## 🚀 Cómo ejecutar el proyecto

### Requisitos
* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### 1. Levantar el Backend (API)
```bash
cd src/QuickDocs.Backend
dotnet run
```

### 2. Levantar el cliente de escritorio
En otra terminal:
```bash
cd src/QuickDocs.UI/QuickDocs.UI.Desktop
dotnet run
```

> La aplicación de escritorio se conecta al Backend local (`http://localhost:5018/`), así que asegurate de que el paso 1 esté corriendo antes de abrir el cliente.

---

## 📦 Compilación e Implementación Independiente

El proyecto está pensado para compilarse en archivos únicos e independientes (*Self-Contained Single File*). Esto significa que se puede generar un ejecutable final para Windows o Linux que **no requiere tener .NET instalado en la máquina del cliente** para funcionar, empaquetando todo el entorno en un solo archivo.

---

## 🗺️ Roadmap

- [ ] Despliegue de la base de datos en un VPS
- [ ] Cliente móvil (Android / iOS)
- [ ] Cliente web (Browser)
- [ ] Sincronización multi-dispositivo contra la base de datos remota

---

## 📸 Capturas
<img width="1538" height="827" alt="Screenshot_2026-06-20_17-33-52" src="https://github.com/user-attachments/assets/f68c18c3-7ce3-41ef-9058-e95057973342" />
<img width="1538" height="827" alt="Screenshot_2026-06-20_17-39-39" src="https://github.com/user-attachments/assets/eb5abe4f-2d65-4071-b6b9-38b62a84e0bd" />
<img width="1538" height="827" alt="Screenshot_2026-06-20_17-37-27" src="https://github.com/user-attachments/assets/6f0a50ce-c018-4cfe-a194-1c4bae07a422" />
<img width="1481" height="699" alt="Screenshot_2026-06-20_17-40-17" src="https://github.com/user-attachments/assets/82a894cb-dd1b-4a42-8452-f5af3a70673a" />



Desarrollado con 💙 por [AibnKrysiuk](https://github.com/AibnKrysiuk) para agilizar el trabajo diario.
