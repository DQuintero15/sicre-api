using System.Text.Json.Serialization;

namespace Sicre.Api.Features.Users.Dtos;

public class UserProfileResponseDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("nombre")]
    public required string Nombre { get; set; }

    [JsonPropertyName("iniciales")]
    public required string Iniciales { get; set; }

    [JsonPropertyName("miembroDesde")]
    public required string MiembroDesde { get; set; }

    [JsonPropertyName("correoElectronico")]
    public required string CorreoElectronico { get; set; }

    [JsonPropertyName("telefono")]
    public string? Telefono { get; set; }

    [JsonPropertyName("cargo")]
    public string? Cargo { get; set; }

    [JsonPropertyName("idCargo")]
    public Guid? IdCargo { get; set; }

    [JsonPropertyName("fechaCreacion")]
    public DateTime FechaCreacion { get; set; }

    [JsonPropertyName("ultimaActualizacion")]
    public DateTime? UltimaActualizacion { get; set; }
}
