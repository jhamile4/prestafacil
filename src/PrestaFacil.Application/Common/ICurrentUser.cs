namespace PrestaFacil.Application.Common;

/// <summary>Usuario que hace la petición. Se usa para la auditoría (CreadoPor, RNF-07).</summary>
public interface ICurrentUser
{
    int UserId { get; }
}
