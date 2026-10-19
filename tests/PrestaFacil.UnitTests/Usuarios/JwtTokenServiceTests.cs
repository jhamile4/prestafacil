using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PrestaFacil.Api.Security;
using PrestaFacil.Domain.Entities;
using PrestaFacil.UnitTests.Clientes;

namespace PrestaFacil.UnitTests.Usuarios;

public class JwtTokenServiceTests
{
    private static readonly JwtOptions Opciones = new()
    {
        Issuer = "PrestaFacil",
        Audience = "PrestaFacil",
        Key = "clave-de-pruebas-con-mas-de-32-caracteres!!",
        ExpiracionMinutos = 60
    };

    private static readonly DateTimeOffset Ahora = new(2026, 10, 19, 15, 0, 0, TimeSpan.Zero);

    private static Usuario Cajero()
    {
        var usuario = new Usuario("cajero1", "Miguel Ángel Salazar", "msalazar@prestafacil.pe", RolUsuario.Cajero, "hash");
        typeof(Usuario).GetProperty(nameof(Usuario.Id))!.SetValue(usuario, 4);
        return usuario;
    }

    private static TokenValidationParameters Parametros(string key) => new()
    {
        ValidIssuer = Opciones.Issuer,
        ValidAudience = Opciones.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key)),
        // El token se emitió con un reloj fijo; aquí solo se valida firma y emisor
        ValidateLifetime = false
    };

    [Fact]
    public async Task Generar_IncluyeIdUsuarioYRolYExpiraSegunConfiguracion()
    {
        var servicio = new JwtTokenService(Options.Create(Opciones), new FixedTimeProvider(Ahora));

        var generado = servicio.Generar(Cajero());

        var resultado = await new JsonWebTokenHandler().ValidateTokenAsync(generado.Token, Parametros(Opciones.Key));
        Assert.True(resultado.IsValid);
        Assert.Equal("4", resultado.Claims[JwtRegisteredClaimNames.Sub]);
        Assert.Equal("cajero1", resultado.Claims[JwtRegisteredClaimNames.UniqueName]);
        Assert.Equal("CAJERO", resultado.Claims[JwtTokenService.ClaimRol]);
        Assert.Equal(Ahora.UtcDateTime.AddMinutes(60), generado.ExpiraEn);
    }

    [Fact]
    public async Task TokenFirmadoConOtraClave_EsRechazado()
    {
        var servicio = new JwtTokenService(Options.Create(Opciones), new FixedTimeProvider(Ahora));
        var generado = servicio.Generar(Cajero());

        var resultado = await new JsonWebTokenHandler()
            .ValidateTokenAsync(generado.Token, Parametros("otra-clave-distinta-de-mas-de-32-caracteres"));

        Assert.False(resultado.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("corta")]
    public void Validar_ConClaveCortaOVacia_Falla(string key)
    {
        Assert.Throws<InvalidOperationException>(() => new JwtOptions { Key = key }.Validar());
    }
}
