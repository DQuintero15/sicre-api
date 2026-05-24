namespace Sicre.Api.Domain.Entities;

public class SICRESettings
{
    public Guid Id { get; set; }

    // Fecha desde la cual SICRE empieza a controlar y generar instancias de reportes.
    // No se deben generar vencimientos anteriores a esta fecha.
    // Se empiezan a generar las obligaciones por primera vez al mes siguiente a esta fecha.
    public DateOnly? GoLiveDate { get; set; }
    public bool AutoNotify { get; set; } = true;
    public Guid? UpdatedByUserId { get; set; }
    public User? UpdatedByUser { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
