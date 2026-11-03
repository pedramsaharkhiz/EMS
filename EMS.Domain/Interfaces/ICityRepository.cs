using EMS.Domain.Entities;

namespace EMS.Domain.Interfaces
{
    /// <summary>A city together with its province, as shown next to a record that belongs to the city.</summary>
    public sealed record CityLocation(long CityId, string CityTitle, long ProvinceId, string ProvinceTitle);

    public interface ICityRepository : ICatalogRepository<City>
    {
        /// <summary>Title of the city and of its province, active or not (null when the city does not exist).</summary>
        Task<CityLocation?> GetLocationAsync(long cityId, CancellationToken cancellationToken = default);
    }
}
