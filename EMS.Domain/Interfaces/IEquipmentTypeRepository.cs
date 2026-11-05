using EMS.Domain.Entities;
using EMS.Domain.ReadModels;

namespace EMS.Domain.Interfaces
{
    public interface IEquipmentTypeRepository : ICatalogRepository<EquipmentType>
    {
        /// <summary>The ACTIVE types that have no active model (equipment of such a type can only be registered with the type itself), with their category.</summary>
        IQueryable<EquipmentTypeWithCategoryRow> QueryWithoutModels();

        /// <summary>The type (and its category) of a model, active or not; empty when the model does not exist.</summary>
        IQueryable<EquipmentTypeWithCategoryRow> QueryTypeOfModel(long modelId);
    }
}
