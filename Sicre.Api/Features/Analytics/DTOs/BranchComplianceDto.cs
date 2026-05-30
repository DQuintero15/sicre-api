namespace Sicre.Api.Features.Analytics.DTOs;

public sealed record BranchComplianceDto(
    string BranchName,
    int Total,
    int OnTime,
    int Late,
    int Overdue,
    int Pending,
    double OnTimeRate,
    double DeliveryRate
);
