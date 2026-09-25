using System.ComponentModel.DataAnnotations;

namespace MarketWorkplace.Domain.Entities;

/// <summary>A promotional slide for the advertisement slider on the mobile home page.</summary>
public class Advertisement
{
    public int Id { get; set; }

    /// <summary>Headline shown on the slide, e.g. <c>Weekend Audio Sale</c>.</summary>
    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional supporting line rendered under the headline.</summary>
    [StringLength(240)]
    public string? Subtitle { get; set; }

    /// <summary>Slide image stored under <c>wwwroot/images/ads</c> (absolute URL); null shows a gradient card.</summary>
    [StringLength(300)]
    public string? ImagePath { get; set; }

    /// <summary>Optional destination: <c>product/{id}</c>, <c>service/{id}</c> or an absolute http(s) URL.</summary>
    [StringLength(300)]
    public string? TargetUrl { get; set; }

    /// <summary>Master switch maintained from the dashboard's Advertisements page.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Moment the slide starts showing (<c>null</c> = no lower bound).</summary>
    public DateTime? StartsAt { get; set; }

    /// <summary>Moment the slide stops showing (<c>null</c> = no upper bound).</summary>
    public DateTime? EndsAt { get; set; }

    /// <summary>Display order inside the slider (lower first).</summary>
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Derived: enabled, inside its schedule window and therefore shown in the slider.</summary>
    public bool ActiveNow =>
        IsActive &&
        (StartsAt is null || StartsAt <= DateTime.UtcNow) &&
        (EndsAt is null || EndsAt > DateTime.UtcNow);
}
