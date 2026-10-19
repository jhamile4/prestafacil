namespace PrestaFacil.Domain.Common;

/// <summary>
/// Se lanza cuando una operación viola una regla de negocio (RN-xx).
/// La API la traduce a una respuesta 422.
/// </summary>
public class DomainException(string message) : Exception(message);
