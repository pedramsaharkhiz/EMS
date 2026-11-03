using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace EMS.Application.Common.Security
{
    public static class Permissions
    {
        public static class Equipments
        {
            public const string View = "Equipments.View";
            public const string Create = "Equipments.Create";
            public const string Update = "Equipments.Update";
            public const string Delete = "Equipments.Delete";
            public const string Assign = "Equipments.Assign";
        }

        public static class FailureReports
        {
            public const string View = "FailureReports.View";
            public const string Create = "FailureReports.Create";
            public const string Update = "FailureReports.Update";
            public const string Delete = "FailureReports.Delete";
        }

        public static class Users
        {
            public const string View = "Users.View";
            public const string Create = "Users.Create";
            public const string Update = "Users.Update";
            public const string Delete = "Users.Delete";

            public const string ManageRoles = "Users.ManageRoles";
        }

        public static class Roles
        {
            public const string View = "Roles.View";
            public const string Create = "Roles.Create";
            public const string Update = "Roles.Update";
            public const string Delete = "Roles.Delete";

            public const string ManagePermissions = "Roles.ManagePermissions";

            public const string ManageUsers = "Roles.ManageUsers";
        }

        public static class Brands
        {
            public const string View = "Brands.View";
            public const string Create = "Brands.Create";
            public const string Update = "Brands.Update";
            public const string Delete = "Brands.Delete";
        }

        public static class EquipmentCategories
        {
            public const string View = "EquipmentCategories.View";
            public const string Create = "EquipmentCategories.Create";
            public const string Update = "EquipmentCategories.Update";
            public const string Delete = "EquipmentCategories.Delete";
        }

        public static class EquipmentLocationTypes
        {
            public const string View = "EquipmentLocationTypes.View";
            public const string Create = "EquipmentLocationTypes.Create";
            public const string Update = "EquipmentLocationTypes.Update";
            public const string Delete = "EquipmentLocationTypes.Delete";
        }

        public static class EquipmentModels
        {
            public const string View = "EquipmentModels.View";
            public const string Create = "EquipmentModels.Create";
            public const string Update = "EquipmentModels.Update";
            public const string Delete = "EquipmentModels.Delete";
        }

        public static class EquipmentStatuses
        {
            public const string View = "EquipmentStatuses.View";
            public const string Create = "EquipmentStatuses.Create";
            public const string Update = "EquipmentStatuses.Update";
            public const string Delete = "EquipmentStatuses.Delete";
        }

        public static class EquipmentTypes
        {
            public const string View = "EquipmentTypes.View";
            public const string Create = "EquipmentTypes.Create";
            public const string Update = "EquipmentTypes.Update";
            public const string Delete = "EquipmentTypes.Delete";
        }

        public static class FailureCategories
        {
            public const string View = "FailureCategories.View";
            public const string Create = "FailureCategories.Create";
            public const string Update = "FailureCategories.Update";
            public const string Delete = "FailureCategories.Delete";
        }

        public static class SupportingDepartments
        {
            public const string View = "SupportingDepartments.View";
            public const string Create = "SupportingDepartments.Create";
            public const string Update = "SupportingDepartments.Update";
            public const string Delete = "SupportingDepartments.Delete";
        }
        public static class Provinces
        {
            public const string View = "Provinces.View";
            public const string Create = "Provinces.Create";
            public const string Update = "Provinces.Update";
            public const string Delete = "Provinces.Delete";
        }

        public static class Cities
        {
            public const string View = "Cities.View";
            public const string Create = "Cities.Create";
            public const string Update = "Cities.Update";
            public const string Delete = "Cities.Delete";
        }

        public static class Skills
        {
            public const string View = "Skills.View";

            public const string Create = "Skills.Create";

            public const string Update = "Skills.Update";

            public const string Delete = "Skills.Delete";
        }
        public static class OrganizationHierarchy
        {
            public const string View = "OrganizationHierarchy.View";
        }


        /// <summary>
        /// Every permission, discovered from the constants of the nested groups above, so a new group or a new constant
        /// can never be forgotten in a hand-written list. Sorted for a stable order.
        /// </summary>
        public static IReadOnlyList<string> All { get; } = Discover();

        private static List<string> Discover() =>
            typeof(Permissions).GetNestedTypes(BindingFlags.Public)
                .SelectMany(group => group.GetFields(BindingFlags.Public | BindingFlags.Static))
                .Where(field => field.IsLiteral && field.FieldType == typeof(string))
                .Select(field => (string)field.GetRawConstantValue()!)
                .OrderBy(permission => permission, StringComparer.Ordinal)
                .ToList();
    }
}
