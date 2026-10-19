using FluentValidation;

namespace PrestaFacil.Application.Prestamos;

// Solo formato. Los límites de cada producto (RN-02) los valida la entidad Prestamo.

public class SimularPrestamoValidator : AbstractValidator<SimularPrestamoRequest>
{
    public SimularPrestamoValidator()
    {
        RuleFor(x => x.ProductoId).GreaterThan(0).WithMessage("Seleccione un producto.");
        RuleFor(x => x.Monto).GreaterThan(0).WithMessage("El monto debe ser mayor a 0.");
        RuleFor(x => x.PlazoMeses).GreaterThan(0).WithMessage("El plazo debe ser mayor a 0.");
        RuleFor(x => x.ClienteId).GreaterThan(0).When(x => x.ClienteId.HasValue);
    }
}

public class SolicitarPrestamoValidator : AbstractValidator<SolicitarPrestamoRequest>
{
    public SolicitarPrestamoValidator()
    {
        RuleFor(x => x.ClienteId).GreaterThan(0).WithMessage("Seleccione un cliente.");
        RuleFor(x => x.ProductoId).GreaterThan(0).WithMessage("Seleccione un producto.");
        RuleFor(x => x.Monto).GreaterThan(0).WithMessage("El monto debe ser mayor a 0.");
        RuleFor(x => x.PlazoMeses).GreaterThan(0).WithMessage("El plazo debe ser mayor a 0.");
    }
}

public class AprobarPrestamoValidator : AbstractValidator<AprobarPrestamoRequest>
{
    public AprobarPrestamoValidator()
    {
        RuleFor(x => x.Comentario).MaximumLength(300);
    }
}

public class RechazarPrestamoValidator : AbstractValidator<RechazarPrestamoRequest>
{
    public RechazarPrestamoValidator()
    {
        RuleFor(x => x.Motivo)
            .NotEmpty().WithMessage("Debe indicar el motivo del rechazo.")
            .MaximumLength(300);
    }
}
