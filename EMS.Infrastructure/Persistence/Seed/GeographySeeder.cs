using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EMS.Infrastructure.Persistence.Seed;

public interface IGeographySeeder
{
    /// <summary>Adds the missing provinces and their capital cities; returns how many provinces and cities were added.</summary>
    Task<(int Provinces, int Cities)> SeedAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Loads the 31 provinces of Iran with their capital cities as a starting point for the Province / City catalogs.
/// Idempotent and additive: a province or city that already exists (active or not, any casing) is left exactly as it is,
/// so it is safe to run repeatedly and never overwrites what users edited. It runs at startup only when
/// <c>Seed:Geography:Enabled</c> is true. The other cities are added through the City API.
/// </summary>
public sealed class GeographySeeder : IGeographySeeder
{
    private readonly EMSDbContext _context;
    private readonly ILogger<GeographySeeder> _logger;

    public GeographySeeder(EMSDbContext context, ILogger<GeographySeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(int Provinces, int Cities)> SeedAsync(CancellationToken cancellationToken = default)
    {
        // Deactivated rows count too: a title that exists (even inactive) is not recreated behind the user's back.
        var provinces = await _context.Provinces.IncludingInactive().ToListAsync(cancellationToken);
        var cities = await _context.Cities.IncludingInactive().ToListAsync(cancellationToken);

        int addedProvinces = 0, addedCities = 0;
        foreach (var (provinceTitle, capital) in Data)
        {
            var province = provinces.FirstOrDefault(p => string.Equals(p.Title, provinceTitle, StringComparison.OrdinalIgnoreCase));
            if (province is null)
            {
                province = new Province { Title = provinceTitle };
                _context.Provinces.Add(province);
                provinces.Add(province);
                addedProvinces++;
            }

            // Only provinces that already exist in the database have an id to compare with; a new one cannot have cities yet.
            var hasCapital = province.ProvinceId != 0 &&
                cities.Any(c => c.ProvinceId == province.ProvinceId && string.Equals(c.Title, capital, StringComparison.OrdinalIgnoreCase));
            if (!hasCapital)
            {
                var city = new City { Title = capital, Province = province };
                _context.Cities.Add(city);
                cities.Add(city);
                addedCities++;
            }
        }

        if (addedProvinces + addedCities > 0)
            await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Geography seed: added {Provinces} provinces and {Cities} cities", addedProvinces, addedCities);
        return (addedProvinces, addedCities);
    }

    /// <summary>Province, capital city.</summary>
    internal static readonly (string Province, string Capital)[] Data =
    {
        ("آذربایجان شرقی", "تبریز"),
        ("آذربایجان غربی", "ارومیه"),
        ("اردبیل", "اردبیل"),
        ("اصفهان", "اصفهان"),
        ("البرز", "کرج"),
        ("ایلام", "ایلام"),
        ("بوشهر", "بوشهر"),
        ("تهران", "تهران"),
        ("چهارمحال و بختیاری", "شهرکرد"),
        ("خراسان جنوبی", "بیرجند"),
        ("خراسان رضوی", "مشهد"),
        ("خراسان شمالی", "بجنورد"),
        ("خوزستان", "اهواز"),
        ("زنجان", "زنجان"),
        ("سمنان", "سمنان"),
        ("سیستان و بلوچستان", "زاهدان"),
        ("فارس", "شیراز"),
        ("قزوین", "قزوین"),
        ("قم", "قم"),
        ("کردستان", "سنندج"),
        ("کرمان", "کرمان"),
        ("کرمانشاه", "کرمانشاه"),
        ("کهگیلویه و بویراحمد", "یاسوج"),
        ("گلستان", "گرگان"),
        ("گیلان", "رشت"),
        ("لرستان", "خرم‌آباد"),
        ("مازندران", "ساری"),
        ("مرکزی", "اراک"),
        ("هرمزگان", "بندرعباس"),
        ("همدان", "همدان"),
        ("یزد", "یزد"),
    };
}
