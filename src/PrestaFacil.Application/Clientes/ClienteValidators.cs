using FluentValidation;

namespace PrestaFacil.Application.Clientes;

/// <summary>
/// Validación de formato de la petición (respuesta 400 con el detalle por campo).
/// Las reglas de negocio se vuelven a comprobar en la entidad Cliente.
/// </summary>
public class CrearClienteValidator : AbstractValidator<CrearClienteRequest>
{
    public CrearClienteValidator()
    {
        RuleFor(x => x.Dni)
            .NotEmpty().WithMessage("El DNI es obligatorio.")
            .Matches("^[0-9]{8}$").WithMessage("El DNI debe tener exactamente 8 dígitos.");

        Include(new ClienteDatosValidator());
    }
}

public class ActualizarClienteValidator : AbstractValidator<ActualizarClienteRequest>
{
    public ActualizarClienteValidator()
    {
        Include(new ClienteDatosValidator());
    }
}

/// <summary>Reglas compartidas entre crear y actualizar.</summary>
public class ClienteDatosValidator : AbstractValidator<IClienteDatos>
{
    public ClienteDatosValidator()
    {
        RuleFor(x => x.Nombres)
            .NotEmpty().WithMessage("Los nombres son obligatorios.")
            .MaximumLength(100);

        RuleFor(x => x.Apellidos)
            .NotEmpty().WithMessage("Los apellidos son obligatorios.")
            .MaximumLength(100);

        RuleFor(x => x.Telefono)
            .Matches("^9[0-9]{8}$").WithMessage("El teléfono debe ser un celular de 9 dígitos que empiece con 9.")
            .When(x => !string.IsNullOrWhiteSpace(x.Telefono));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("El correo no es válido.")
            .MaximumLength(150)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Direccion).MaximumLength(200);

        RuleFor(x => x.IngresoMensual)
            .GreaterThan(0).WithMessage("El ingreso mensual debe ser mayor a 0.")
            .PrecisionScale(12, 2, true).WithMessage("El ingreso admite como máximo 2 decimales.");

        RuleFor(x => x.GastosMensuales)
            .GreaterThanOrEqualTo(0).WithMessage("Los gastos no pueden ser negativos.")
            .PrecisionScale(12, 2, true).WithMessage("Los gastos admiten como máximo 2 decimales.");
    }
}
