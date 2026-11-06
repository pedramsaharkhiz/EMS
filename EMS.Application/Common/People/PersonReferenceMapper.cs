using EMS.Domain.ReadModels;
using System.Collections.Concurrent;
using System.Reflection;

namespace EMS.Application.Common.People
{
    /// <summary>
    /// Writes a <see cref="PersonReference"/> into the flat, prefixed properties of a DTO (<c>TechnicianName</c>, <c>TechnicianPersonnelCode</c>,
    /// <c>TechnicianIsActive</c>, <c>TechnicianPostId</c>, <c>TechnicianPostTitle</c>, <c>TechnicianSupportingDepartmentId</c>, ...), so that every DTO
    /// that shows a person shows the same set under the same names: <c>{prefix}Name</c>, <c>{prefix}PersonnelCode</c>, <c>{prefix}IsActive</c>,
    /// <c>{prefix}PostId/PostTitle</c>, <c>{prefix}SupportingDepartmentId/Title</c>, <c>{prefix}CityId/Title</c>, <c>{prefix}ProvinceId/Title</c>
    /// (the id of the person himself is the DTO's own <c>{prefix}UserId</c>). A test requires every DTO that uses a prefix to have all of them.
    /// </summary>
    public static class PersonReferenceMapper
    {
        public static readonly string[] Suffixes =
        {
            "Name", "PersonnelCode", "IsActive", "PostId", "PostTitle", "SupportingDepartmentId", "SupportingDepartmentTitle",
            "CityId", "CityTitle", "ProvinceId", "ProvinceTitle",
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
            properties[5].SetValue(dto, person.SupportingDepartmentId);
            properties[6].SetValue(dto, person.SupportingDepartmentTitle);
            properties[7].SetValue(dto, person.CityId);
            properties[8].SetValue(dto, person.CityTitle);
            properties[9].SetValue(dto, person.ProvinceId);
            properties[10].SetValue(dto, person.ProvinceTitle);
        }

        /// <summary>The property names a DTO must have for <paramref name="prefix"/> (used by the consistency test).</summary>
        public static IEnumerable<string> PropertyNames(string prefix) => Suffixes.Select(s => prefix + s);
    }
}
