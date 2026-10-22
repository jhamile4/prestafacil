using FluentValidation;
using PrestaFacil.Application.Common;
using PrestaFacil.Application.Usuarios;
using PrestaFacil.Domain.Entities;
using PrestaFacil.UnitTests.Clientes;

namespace PrestaFacil.UnitTests.Usuarios;

public class AuthServiceTests
{
    private const string PasswordValida = "Segura2026!";

    private readonly FakeUsuarioRepository _repositorio = new();
    private readonly FakePasswordHasher _hasher = new();
    private readonly Usuario _cajero;

    public AuthServiceTests()
    {
        _cajero = _repositorio.Agregar("cajero1", RolUsuario.Cajero, PasswordValida);
    }

    private AuthService Servicio(int usuarioActualId = 0) => new(
        _repositorio, _hasher, new FakeTokenService(),
        new LoginValidator(), new CambiarPasswordValidator(),
        new FakeCurrentUser(usuarioActualId));

    [Fact]
    public async Task Login_ConCredencialesCorrectas_DevuelveToken()
    {
        var respuesta = await Servicio().LoginAsync(new LoginRequest("cajero1", PasswordValida));

        Assert.Equal("token-de-cajero1", respuesta.Token);
        Assert.Equal("Bearer", respuesta.TipoToken);
        Assert.Equal(RolUsuario.Cajero, respuesta.Usuario.Rol);
    }

    [Fact]
    public async Task Login_NoDistingueMayusculasEnElUsuario()
    {
        var respuesta = await Servicio().LoginAsync(new LoginRequest("  CAJERO1 ", PasswordValida));

        Assert.Equal("cajero1", respuesta.Usuario.NombreUsuario);
    }

    [Fact]
    public async Task Login_ConPasswordIncorrecta_Falla()
    {
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            Servicio().LoginAsync(new LoginRequest("cajero1", "otra")));

        Assert.Equal("Usuario o contraseña incorrectos.", ex.Message);
    }

    [Fact]
    public async Task Login_UsuarioInexistente_DaElMismoMensajeYVerificaIgual()
    {
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            Servicio().LoginAsync(new LoginRequest("noexiste", PasswordValida)));

        // Mismo mensaje y se ejecutó la verificación: no revela si el usuario existe
        Assert.Equal("Usuario o contraseña incorrectos.", ex.Message);
        Assert.Equal(1, _hasher.Verificaciones);
    }

    [Fact]
    public async Task Login_UsuarioDesactivado_Falla()
    {
        _cajero.Desactivar();

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            Servicio().LoginAsync(new LoginRequest("cajero1", PasswordValida)));
    }

    [Fact]
    public async Task CambiarPassword_ConPasswordActualIncorrecta_Falla()
    {
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            Servicio(_cajero.Id).CambiarPasswordAsync(new CambiarPasswordRequest("equivocada", "Nueva2026!")));

        Assert.Equal("PasswordActual", Assert.Single(ex.Errors).PropertyName);
    }

    [Fact]
    public async Task CambiarPassword_Correcta_GuardaElNuevoHash()
    {
        await Servicio(_cajero.Id).CambiarPasswordAsync(new CambiarPasswordRequest(PasswordValida, "Nueva2026!"));

        Assert.Equal(FakePasswordHasher.HashDe("Nueva2026!"), _cajero.PasswordHash);
    }

    [Fact]
    public async Task Directorio_DevuelveNombreYRolSinDatosSensibles()
    {
        var directorio = await Servicio().DirectorioAsync();

        var cajero = Assert.Single(directorio);
        Assert.Equal((_cajero.Id, _cajero.NombreCompleto, RolUsuario.Cajero), (cajero.Id, cajero.NombreCompleto, cajero.Rol));
    }

    [Theory]
    [InlineData("corta1A")]       // menos de 8
    [InlineData("sinnumeroA")]    // sin número
    [InlineData("SINMINUSCULA1")] // sin minúscula
    [InlineData("sinmayuscula1")] // sin mayúscula
    public async Task CambiarPassword_QueNoCumpleLaPolitica_Falla(string nueva)
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            Servicio(_cajero.Id).CambiarPasswordAsync(new CambiarPasswordRequest(PasswordValida, nueva)));
        Assert.Equal(FakePasswordHasher.HashDe(PasswordValida), _cajero.PasswordHash);
    }
}
