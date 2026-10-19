using PrestaFacil.Domain.Common;
using PrestaFacil.Domain.Entities;

namespace PrestaFacil.UnitTests.Domain;

public class ClienteTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 19);

    private static Cliente CrearCliente(
        string dni = "45879632",
        DateOnly? fechaNacimiento = null,
        decimal ingreso = 4500m,
        decimal gastos = 1800m) =>
        new(dni, "Ana María", "Flores Gutiérrez", fechaNacimiento ?? new DateOnly(1990, 3, 14),
            "987654321", "Ana.Flores@Gmail.com", null, ingreso, gastos, creadoPor: 2, hoy: Hoy);

    [Fact]
    public void Crear_ConDatosValidos_QuedaActivoYNormalizaDatos()
    {
        var cliente = CrearCliente();

        Assert.True(cliente.Activo);
        Assert.Equal("45879632", cliente.Dni);
        Assert.Equal("ana.flores@gmail.com", cliente.Email);
        Assert.Equal(2, cliente.CreadoPor);
    }

    [Theory]
    [InlineData("1234567")]    // 7 dígitos
    [InlineData("123456789")]  // 9 dígitos
    [InlineData("1234567A")]   // letra
    [InlineData("")]
    public void Crear_ConDniInvalido_LanzaDomainException(string dni)
    {
        var ex = Assert.Throws<DomainException>(() => CrearCliente(dni: dni));
        Assert.Contains("RN-01", ex.Message);
    }

    [Fact]
    public void Crear_MenorDeEdad_LanzaDomainException()
    {
        // Cumple 18 mañana
        var nacimiento = Hoy.AddYears(-18).AddDays(1);

        Assert.Throws<DomainException>(() => CrearCliente(fechaNacimiento: nacimiento));
    }

    [Fact]
    public void Crear_QueCumple18Hoy_EsValido()
    {
        var cliente = CrearCliente(fechaNacimiento: Hoy.AddYears(-18));

        Assert.Equal(18, Cliente.CalcularEdad(cliente.FechaNacimiento, Hoy));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-100, 0)]
    public void Crear_ConIngresoNoPositivo_LanzaDomainException(decimal ingreso, decimal gastos)
    {
        Assert.Throws<DomainException>(() => CrearCliente(ingreso: ingreso, gastos: gastos));
    }

    [Fact]
    public void IngresoNeto_EsIngresoMenosGastos()
    {
        var cliente = CrearCliente(ingreso: 4500m, gastos: 1800m);

        Assert.Equal(2700m, cliente.IngresoNeto);
    }

    [Fact]
    public void Desactivar_y_Activar_CambianElEstado()
    {
        var cliente = CrearCliente();

        cliente.Desactivar();
        Assert.False(cliente.Activo);

        cliente.Activar();
        Assert.True(cliente.Activo);
    }

    [Fact]
    public void Actualizar_CambiaDatosPeroNoElDni()
    {
        var cliente = CrearCliente();

        cliente.Actualizar("Ana", "Flores", new DateOnly(1990, 3, 14), null, null,
            "Av. Siempre Viva 123", 5000m, 2000m, Hoy);

        Assert.Equal("Ana", cliente.Nombres);
        Assert.Equal(3000m, cliente.IngresoNeto);
        Assert.Null(cliente.Telefono);
        Assert.Equal("45879632", cliente.Dni);
    }

    [Theory]
    [InlineData(2000, 10, 19, 26)] // cumple hoy
    [InlineData(2000, 10, 20, 25)] // cumple mañana
    [InlineData(2000, 2, 29, 26)]  // nació en año bisiesto
    public void CalcularEdad_ConsideraSiYaCumplioEsteAnio(int anio, int mes, int dia, int esperado)
    {
        Assert.Equal(esperado, Cliente.CalcularEdad(new DateOnly(anio, mes, dia), Hoy));
    }
}
