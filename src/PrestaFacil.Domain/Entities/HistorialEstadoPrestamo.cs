namespace PrestaFacil.Domain.Entities;

/// <summary>RN-12: quién cambió el estado de un préstamo, cuándo y por qué.</summary>
public class HistorialEstadoPrestamo
{
    public int Id { get; private set; }
    public int PrestamoId { get; private set; }
    public EstadoPrestamo? EstadoAnterior { get; private set; }
    public EstadoPrestamo EstadoNuevo { get; private set; }
    public int UsuarioId { get; private set; }
    public DateTime Fecha { get; private set; }
    public string? Comentario { get; private set; }

    private HistorialEstadoPrestamo() { }

    internal HistorialEstadoPrestamo(
        EstadoPrestamo? estadoAnterior, EstadoPrestamo estadoNuevo, int usuarioId, DateTime fecha, string? comentario)
    {
        EstadoAnterior = estadoAnterior;
        EstadoNuevo = estadoNuevo;
        UsuarioId = usuarioId;
        Fecha = fecha;
        Comentario = comentario;
    }
}
