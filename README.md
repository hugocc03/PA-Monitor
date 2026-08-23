# PA Run Monitor

Plugin de [XrmToolBox](https://www.xrmtoolbox.com/) para monitorizar ejecuciones de **Power Automate Cloud** desde **Dataverse**, con árbol de flujos hijo, filtros y resumen de errores.

Pensado para cuando el monitor nativo de Power Automate llega tarde o no muestra bien runs incompletos.

## Características

- Selección multi-solución y multi-flujo (Select All + búsqueda)
- Carga dinámica: solución → flujos → runs
- Filtros por estado, fecha desde/hasta, máximo de resultados y Run Id / Workflow Id
- Lista de runs con colores por estado
- Árbol de runs anidados (child flows) con carga lazy
- **Expand failures** para expandir nodos fallidos
- Abrir el run en el portal de Power Automate (doble clic, menú contextual o toolbar)
- Detalle de error desde Dataverse (`flowrun.errormessage`)

## Limitaciones conocidas

- Solo muestra ejecuciones de producción sincronizadas en la tabla **flowrun** de Dataverse (flujos en solución).
- Las ejecuciones **Test** desde el editor del flujo no se guardan en `flowrun` y no aparecen en el listado.
- Puede haber varios minutos de retraso entre que termina un run y aparece en Dataverse.

## Requisitos

- [XrmToolBox](https://www.xrmtoolbox.com/) actualizado
- Conexión a un entorno Dataverse / Power Platform con historial de runs en Dataverse habilitado
- .NET Framework 4.8 (para compilar)
- SDK: `dotnet` CLI

## Instalación (desarrollo)

1. Clona el repositorio.
2. Ajusta en `deploy.ps1` la ruta a tu `XrmToolBox.exe` si no coincide.
3. Ejecuta:

```powershell
.\deploy.ps1
```

El script cierra XrmToolBox si está abierto, compila en Debug, copia la DLL a:

`%APPDATA%\MscrmTools\XrmToolBox\Plugins`

y abre XrmToolBox.

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

## Estructura del proyecto

```
PA-Monitor/
├── deploy.ps1
├── LICENSE
├── README.md
└── src/PAMonitor.XrmToolBox/
    ├── Plugin.cs                 # Entrada XrmToolBox
    ├── PluginControl.cs          # UI principal
    ├── ToolbarIcons.cs
    ├── Controls/
    ├── Models/
    └── Services/                 # Dataverse queries, URLs
```

## Licencia

MIT — ver [LICENSE](LICENSE).

## Publicación (Tool Library)

Para publicar en la [XrmToolBox Tool Library](https://www.xrmtoolbox.com/documentation/for-developers/deploy-your-plugin-in-plugins-store/):

1. Compilar y empaquetar:

```powershell
.\pack-release.ps1
```

2. Verificar el `.nupkg` en `dist/` (DLL en `lib/net48/Plugins/`, dependencia `XrmToolBox`, icono embebido).

3. Publicar en [nuget.org](https://www.nuget.org/) con tu API key.

4. Registrar el Package Id `PAMonitor.XrmToolBox` en el portal XrmToolBox y esperar validación.

Instalación para usuarios: XrmToolBox → Tool Library → buscar **PA Run Monitor**.

