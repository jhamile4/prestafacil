using FluentValidation;
using PrestaFacil.Domain.Entities;

namespace PrestaFacil.Application.Usuarios;

// ---------- Peticiones ----------

public record LoginRequest(string NombreUsuario, string Password);

public record CambiarPasswordRequest(string PasswordActual, string PasswordNueva);

public record CrearUsuarioRequest(
    string NombreUsuario,
    string NombreCompleto,
    string Email,
    RolUsuario Rol,
    string Password);

public record ActualizarUsuarioRequest(string NombreCompleto, string Email, RolUsuario Rol);

public record RestablecerPasswordRequest(string PasswordNueva);

// ---------- Respuestas ----------

public record UsuarioResponse(
    int Id,
    string NombreUsuario,
    string NombreCompleto,
    string Email,
    RolUsuario Rol,
    bool Activo,
    DateTime FechaCreacion)
{
    public static UsuarioResponse Desde(Usuario u) =>
        new(u.Id, u.NombreUsuario, u.NombreCompleto, u.Email, u.Rol, u.Activo, u.FechaCreacion);
}

public record LoginResponse(string Token, string TipoToken, DateTime ExpiraEn, UsuarioResponse Usuario);

// ---------- Validaciones ----------

/// <summary>Política de contraseñas: mínimo 8 caracteres, con mayúscula, minúscula y número.</summary>
public static class PoliticaPassword
{
    public static IRuleBuilderOptions<T, string> PasswordSegura<T>(this IRuleBuilder<T, string> regla) =>
        regla
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .MaximumLength(72).WithMessage("La contraseña no puede superar 72 caracteres.") // límite de BCrypt
            .Matches("[A-Z]").WithMessage("La contraseña debe tener al menos una mayúscula.")
            .Matches("[a-z]").WithMessage("La contraseña debe tener al menos una minúscula.")
            .Matches("[0-9]").WithMessage("La contraseña debe tener al menos un número.");
}

public class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.NombreUsuario).NotEmpty().WithMessage("Ingrese su usuario.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Ingrese su contraseña.");
    }
}

public class CambiarPasswordValidator : AbstractValidator<CambiarPasswordRequest>
{
    public CambiarPasswordValidator()
    {
        RuleFor(x => x.PasswordActual).NotEmpty().WithMessage("Ingrese su contraseña actual.");
        RuleFor(x => x.PasswordNueva).PasswordSegura()
            .NotEqual(x => x.PasswordActual).WithMessage("La nueva contraseña debe ser distinta de la actual.");
    }
}

public class CrearUsuarioValidator : AbstractValidator<CrearUsuarioRequest>
{
    public CrearUsuarioValidator()
    {
        RuleFor(x => x.NombreUsuario)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio.")
            .Matches("^[a-zA-Z0-9._]{3,50}$").WithMessage("El usuario debe tener de 3 a 50 letras, números, puntos o guiones bajos.");
        RuleFor(x => x.NombreCompleto).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("El correo no es válido.").MaximumLength(150);
        RuleFor(x => x.Rol).IsInEnum();
        RuleFor(x => x.Password).PasswordSegura();
    }
}

public class ActualizarUsuarioValidator : AbstractValidator<ActualizarUsuarioRequest>
{
    public ActualizarUsuarioValidator()
    {
        RuleFor(x => x.NombreCompleto).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("El correo no es válido.").MaximumLength(150);
        RuleFor(x => x.Rol).IsInEnum();
    }
}

public class RestablecerPasswordValidator : AbstractValidator<RestablecerPasswordRequest>
{
    public RestablecerPasswordValidator()
    {
        RuleFor(x => x.PasswordNueva).PasswordSegura();
    }
}
