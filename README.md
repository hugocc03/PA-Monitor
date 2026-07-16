# PA Run Monitor

Plugin de [XrmToolBox](https://www.xrmtoolbox.com/) para monitorizar ejecuciones de **Power Automate Cloud** desde **Dataverse**, con árbol de flujos hijo, filtros y detalle de errores a nivel de acción.

Pensado para cuando el monitor nativo de Power Automate llega tarde, no muestra bien runs incompletos o no deja ver el error real de la acción.

## Características

- Selección multi-solución y multi-flujo (Select All + búsqueda)
- Carga dinámica: solución → flujos → runs
- Filtros por estado, fecha desde/hasta, máximo de resultados y Run Id / Workflow Id
- Lista de runs con colores por estado
- Árbol de runs anidados (child flows) con carga lazy
- **Expand failures** para expandir nodos fallidos
- Abrir el run en el portal de Power Automate (doble clic, menú contextual o toolbar)
- Resumen Dataverse + detalle de acciones vía Flow API (opcional, requiere app Entra)

## Requisitos

- [XrmToolBox](https://www.xrmtoolbox.com/) actualizado
- Conexión a un entorno Dataverse / Power Platform
- .NET Framework 4.8 (para compilar)
- SDK: `dotnet` CLI

Para el **detalle de errores de acción** (Flow API):

- App registration en Entra ID (public client)
- Permiso delegado `Flows.Read.All` (Microsoft Flow Service) + consent admin
- Redirect URI `http://localhost` y *Allow public client flows*

## Instalación (desarrollo)

1. Clona el repositorio.
2. Ajusta en `deploy.ps1` la ruta a tu `XrmToolBox.exe` si no coincide.
3. Ejecuta:

```powershell
.\deploy.ps1
```

El script cierra XrmToolBox si está abierto, compila en Debug, copia la DLL a:

`%APPDATA%\MscrmTools\XrmToolBox\Plugins`

y las dependencias MSAL a:

`%APPDATA%\MscrmTools\XrmToolBox\Plugins\PAMonitor.XrmToolBox\`

luego abre XrmToolBox.

También puedes compilar solo:

```powershell
dotnet build .\src\PAMonitor.XrmToolBox\PAMonitor.XrmToolBox.csproj -c Debug
```

## Uso rápido

1. Conéctate a tu organización en XrmToolBox.
2. Abre **PA Run Monitor**.
3. Selecciona una o más **solutions** → se cargan los flows.
4. Selecciona uno o más **flows** → se cargan los runs.
5. Filtra por estado/fechas/Run Id según necesites.
6. Selecciona un run para ver el árbol y el detalle.
7. Usa **Open run** para abrirlo en make.powerautomate.com.

### Flow API settings

Sin configurar Flow API solo verás el resumen genérico de Dataverse (`ActionFailed`, etc.).

La primera vez que selecciones un run sin Client Id configurado, el tool te preguntará si quieres configurar Flow API. Si dices que no, puedes hacerlo más tarde con el botón **Flow API settings** de la barra superior.

Pasos resumidos (o pídeselos a un admin de Entra):

1. Registrar una app (single tenant).
2. Copiar el **Application (client) ID**.
3. Authentication → Mobile and desktop → Redirect URI `http://localhost` → Allow public client flows = Yes.
4. API permissions → Microsoft Flow Service → `Flows.Read.All` → Grant admin consent.
5. Pegar el Client Id en **Flow API settings** y volver a seleccionar un run fallido.

## Estructura del proyecto

```
PA-Monitor/
├── deploy.ps1
├── LICENSE
├── README.md
└── src/PAMonitor.XrmToolBox/
    ├── Plugin.cs                 # Entrada XrmToolBox + AssemblyResolve
    ├── PluginControl.cs          # UI principal
    ├── FlowApiSettingsForm.cs
    ├── Settings.cs
    ├── ToolbarIcons.cs
    ├── Controls/
    ├── Models/
    └── Services/                 # Dataverse queries, Flow API, MSAL, URLs
```

## Licencia

MIT — ver [LICENSE](LICENSE).
