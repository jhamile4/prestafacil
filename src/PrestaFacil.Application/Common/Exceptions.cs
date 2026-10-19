namespace PrestaFacil.Application.Common;

/// <summary>El recurso pedido no existe. La API responde 404.</summary>
public class NotFoundException(string message) : Exception(message);

/// <summary>La operación choca con datos existentes (ej. DNI duplicado). La API responde 409.</summary>
public class ConflictException(string message) : Exception(message);

/// <summary>Credenciales incorrectas o usuario desactivado. La API responde 401.</summary>
public class UnauthorizedException(string message) : Exception(message);
