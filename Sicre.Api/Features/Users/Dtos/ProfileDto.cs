namespace Sicre.Api.Features.Users.Dtos;

public sealed record ProfileDto(string FullName, IList<string> Roles);
