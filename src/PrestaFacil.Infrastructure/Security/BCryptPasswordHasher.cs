using PrestaFacil.Application.Usuarios;

namespace PrestaFacil.Infrastructure.Security;

/// <summary>
/// BCrypt incluye una sal aleatoria en cada hash y es lento a propósito,
/// lo que encarece los ataques de fuerza bruta (RNF-01).
/// </summary>
public class BCryptPasswordHasher : IPasswordHasher
{
    private const int FactorDeTrabajo = 11;

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, FactorDeTrabajo);

    public bool Verificar(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Hash con formato inválido (por ejemplo, el valor temporal de los datos de prueba)
            return false;
        }
    }
}
