using System.Text;

namespace PrestaFacil.Api.Security;

/// <summary>Sección "Jwt" de appsettings. En producción, Key se pasa por variable de entorno (Jwt__Key).</summary>
public class JwtOptions
{
    public const string Seccion = "Jwt";

    public string Issuer { get; set; } = "PrestaFacil";
    public string Audience { get; set; } = "PrestaFacil";
    public string Key { get; set; } = string.Empty;
    public int ExpiracionMinutos { get; set; } = 60;

    public byte[] KeyBytes => Encoding.UTF8.GetBytes(Key);

    /// <summary>HMAC-SHA256 requiere una clave de al menos 256 bits.</summary>
    public void Validar()
    {
        if (KeyBytes.Length < 32)
            throw new InvalidOperationException(
                "Falta Jwt:Key o tiene menos de 32 caracteres. Configúrela con la variable de entorno Jwt__Key.");
    }
}
