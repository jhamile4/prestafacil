using PrestaFacil.Domain.Common;

namespace PrestaFacil.Domain.Entities;

public enum RolUsuario
{
    Admin,
    Asesor,
    Cajero
}

/// <summary>Usuario del sistema (RF-19 a RF-21).</summary>
public class Usuario
{
    public int Id { get; private set; }
    public string NombreUsuario { get; private set; } = null!;
    public string NombreCompleto { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    /// <summary>Hash BCrypt. La contraseña en texto plano nunca se guarda.</summary>
    public string PasswordHash { get; private set; } = null!;
    public RolUsuario Rol { get; private set; }
    public bool Activo { get; private set; }
    public DateTime FechaCreacion { get; private set; }

    private Usuario() { }

    public Usuario(string nombreUsuario, string nombreCompleto, string email, RolUsuario rol, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario))
            throw new DomainException("El nombre de usuario es obligatorio.");

        NombreUsuario = nombreUsuario.Trim().ToLowerInvariant();
        Activo = true;
        Actualizar(nombreCompleto, email, rol);
        CambiarPasswordHash(passwordHash);
    }

    public void Actualizar(string nombreCompleto, string email, RolUsuario rol)
    {
        if (string.IsNullOrWhiteSpace(nombreCompleto))
            throw new DomainException("El nombre completo es obligatorio.");
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("El correo es obligatorio.");

        NombreCompleto = nombreCompleto.Trim();
        Email = email.Trim().ToLowerInvariant();
        Rol = rol;
    }

    public void CambiarPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("El hash de la contraseña no puede estar vacío.");
        PasswordHash = passwordHash;
    }

    public void Desactivar() => Activo = false;

    public void Activar() => Activo = true;
}
