namespace PrestaFacil.Domain.Entities;

public class ProductoPrestamo
{
    public int Id { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string? Descripcion { get; private set; }
    /// <summary>Tasa efectiva anual como fracción: 0.35 = 35 %.</summary>
    public decimal Tea { get; private set; }
    public decimal MontoMinimo { get; private set; }
    public decimal MontoMaximo { get; private set; }
    public int PlazoMinimoMeses { get; private set; }
    public int PlazoMaximoMeses { get; private set; }
    /// <summary>Tasa de mora por día como fracción: 0.001 = 0.10 % diario.</summary>
    public decimal TasaMoraDiaria { get; private set; }
    public bool Activo { get; private set; }

    private ProductoPrestamo() { }

    public ProductoPrestamo(
        string nombre, decimal tea, decimal montoMinimo, decimal montoMaximo,
        int plazoMinimoMeses, int plazoMaximoMeses, decimal tasaMoraDiaria, string? descripcion = null)
    {
        Nombre = nombre;
        Descripcion = descripcion;
        Tea = tea;
        MontoMinimo = montoMinimo;
        MontoMaximo = montoMaximo;
        PlazoMinimoMeses = plazoMinimoMeses;
        PlazoMaximoMeses = plazoMaximoMeses;
        TasaMoraDiaria = tasaMoraDiaria;
        Activo = true;
    }
}
