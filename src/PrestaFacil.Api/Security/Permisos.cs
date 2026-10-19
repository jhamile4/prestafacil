using PrestaFacil.Application.Usuarios;

namespace PrestaFacil.Api.Security;

/// <summary>
/// Combinaciones de roles para [Authorize(Roles = ...)]. El ADMIN puede todo;
/// ver la matriz completa en docs/01_requerimientos.md, sección 2.
/// </summary>
public static class Permisos
{
    public const string Admin = Roles.Admin;
    public const string AdminAsesor = Roles.Admin + "," + Roles.Asesor;
    public const string AdminCajero = Roles.Admin + "," + Roles.Cajero;
}
