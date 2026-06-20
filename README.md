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
* **Historial Centralizado:** Acceso y control total de todos los documentos generados con funciones de:
  * 🔍 Búsqueda inteligente por cliente, tipo o número de documento.
  * 🖨️ Reimpresión y apertura instantánea de PDFs físicos directamente desde la interfaz.
  * 🗑️ Eliminación segura con integridad referencial en cascada a nivel Base de Datos.

---

## 🛠️ Arquitectura y Tecnologías

El proyecto fue migrado de una estructura monolítica a un modelo distribuido y robusto:

### 🖥️ Front-End (Interfaz de Usuario)
* **Framework:** [Avalonia UI](https://avaloniaui.net/) (.NET 8) - Permite una interfaz moderna, fluida y **100% multiplataforma**.
* **Patrón de Diseño:** MVVM (Model-View-ViewModel) utilizando el **CommunityToolkit.Mvvm** para un manejo de estado limpio y reactivo.
* **Comunicación:** Consumo asíncrono de servicios mediante `HttpClient`.

### ⚙️ Back-End & Datos (API)
* **Framework:** ASP.NET Core Web API (.NET 8).
* **Base de Datos:** SQLite gestionado a través de **Entity Framework Core** para un acceso eficiente y seguro a los datos.
* **Motor de PDF:** QuestPDF (Motor de maquetación profesional basado en código).
* **Calidad de Código:** Compilación estricta con manejo de tipos de referencia nulos (*Nullables*) logrando un entorno **libre de warnings**.

---

## 📦 Compilación e Implementación Independiente

El proyecto está preparado para compilarse en archivos únicos e independientes (*Self-Contained Single File*). Esto significa que podés generar un ejecutable final para Windows (o Linux) que **no requiere tener .NET instalado en la máquina del cliente** para funcionar, empaquetando todo el entorno en un solo archivo `.exe`.

---

## 📸 Capturas (Próximamente)

Desarrollado con 💙 por [AibnKrysiuk](https://github.com/AibnKrysiuk) para agilizar el trabajo diario.
