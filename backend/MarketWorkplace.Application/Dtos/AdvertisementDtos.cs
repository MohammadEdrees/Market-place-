using System.ComponentModel.DataAnnotations;

namespace MarketWorkplace.Application.Dtos;

/// <summary>Request body for creating or updating an advertisement (the image is uploaded separately).</summary>
public class AdvertisementInput
{
    /// <summary>Headline shown on the slide.</summary>
    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional supporting line rendered under the headline.</summary>
    [StringLength(240)]
    public string? Subtitle { get; set; }

    /// <summary>Optional destination: <c>product/{id}</c>, <c>service/{id}</c> or an absolute http(s) URL.</summary>
    [StringLength(300)]
    public string? TargetUrl { get; set; }

    /// <summary>Whether the slide may be shown (the schedule window still applies).</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Moment the slide starts showing; <c>null</c> means immediately.</summary>
    public DateTime? StartsAt { get; set; }

    /// <summary>Moment the slide stops showing; <c>null</c> means it never expires.</summary>
    public DateTime? EndsAt { get; set; }

    /// <summary>Display order inside the slider (lower first).</summary>
    [Range(-1000, 1000)]
    public int SortOrder { get; set; }
}
