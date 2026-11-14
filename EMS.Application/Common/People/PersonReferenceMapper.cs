using EMS.Application.Dtos.Common;
using EMS.Domain.ReadModels;
using System.Collections.Concurrent;
using System.Reflection;

namespace EMS.Application.Common.People
{
    /// <summary>
    /// Writes a <see cref="PersonReference"/> into the flat, prefixed properties of a DTO (<c>TechnicianName</c>, <c>TechnicianPersonnelCode</c>,
    /// <c>TechnicianIsActive</c>, <c>TechnicianPostId</c>, <c>TechnicianPostTitle</c>, <c>TechnicianSupportingDepartments</c>, ...), so that every DTO
    /// that shows a person shows the same set under the same names: <c>{prefix}Name</c>, <c>{prefix}PersonnelCode</c>, <c>{prefix}IsActive</c>,
    /// <c>{prefix}PostId/PostTitle</c> and <c>{prefix}SupportingDepartments</c> (the list of his departments, each with its city and province)
    /// (the id of the person himself is the DTO's own <c>{prefix}UserId</c>). A test requires every DTO that uses a prefix to have all of them.
    /// </summary>
    public static class PersonReferenceMapper
    {
        public static readonly string[] Suffixes =
        {
            "Name", "PersonnelCode", "IsActive", "PostId", "PostTitle", "SupportingDepartments",
        };

        private static readonly ConcurrentDictionary<(Type, string), PropertyInfo[]> Cache = new();

        /// <summary>Fills the person's fields of <paramref name="dto"/> under <paramref name="prefix"/>; a null person leaves them empty.</summary>
        public static void Fill(object dto, string prefix, PersonReference? person)
        {
            if (person is null) return;
            var properties = Cache.GetOrAdd((dto.GetType(), prefix), key => Suffixes.Select(s =>
                key.Item1.GetProperty(key.Item2 + s) ?? throw new InvalidOperationException($"{key.Item1.Name} has no property {key.Item2 + s} (the person set of '{key.Item2}').")).ToArray());

            properties[0].SetValue(dto, person.FullName);
            properties[1].SetValue(dto, person.PersonnelCode);
            properties[2].SetValue(dto, person.IsActive);
            properties[3].SetValue(dto, person.PostId);
            properties[4].SetValue(dto, person.PostTitle);
            properties[5].SetValue(dto, ToDtos(person.SupportingDepartments));
        }

        /// <summary>Writes only the department list (rows that already carry the rest of the person fill the list from the person lookup).</summary>
        public static void FillDepartments(object dto, string prefix, PersonReference? person)
        {
            var property = dto.GetType().GetProperty(prefix + "SupportingDepartments")
                ?? throw new InvalidOperationException($"{dto.GetType().Name} has no property {prefix}SupportingDepartments.");
            property.SetValue(dto, ToDtos(person?.SupportingDepartments));
        }

        public static List<PersonDepartmentDto> ToDtos(IEnumerable<PersonDepartment>? departments) =>
            (departments ?? Enumerable.Empty<PersonDepartment>())
                .OrderBy(d => d.Title, StringComparer.Ordinal).ThenBy(d => d.Id)
                .Select(d => new PersonDepartmentDto { Id = d.Id, Title = d.Title, CityId = d.CityId, CityTitle = d.CityTitle, ProvinceId = d.ProvinceId, ProvinceTitle = d.ProvinceTitle })
                .ToList();

        /// <summary>The property names a DTO must have for <paramref name="prefix"/> (used by the consistency test).</summary>
        public static IEnumerable<string> PropertyNames(string prefix) => Suffixes.Select(s => prefix + s);
    }
}
