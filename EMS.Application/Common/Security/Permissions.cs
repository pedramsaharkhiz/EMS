using System;
using System.Collections.Generic;
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

        public static IReadOnlyList<string> All => new List<string> 
        { 
            Equipments.View,
            Equipments.Create,
            Equipments.Update,
            Equipments.Delete,
            Equipments.Assign,
            FailureReports.View,
            FailureReports.Create,
            FailureReports.Update,
            FailureReports.Delete,
            Users.View,
            Users.Create,
            Users.Update,
            Users.Delete,
            Users.ManageRoles,
            Roles.View,
            Roles.Create,
            Roles.Update,
            Roles.Delete,
            Roles.ManagePermissions,
            Roles.ManageUsers
        };
    }
}
