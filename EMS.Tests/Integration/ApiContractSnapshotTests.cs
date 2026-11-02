using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using System.Text;

namespace EMS.Tests.Integration
{
    /// <summary>
    /// Describes every endpoint (route, parameters, request/response shapes, required permission) and compares it with a
    /// committed baseline. Refactoring must not change the public API: if this test fails, the change is either a bug
    /// or an intentional contract change - in which case regenerate the baseline consciously with UPDATE_API_CONTRACT=1.
    /// </summary>
    public class ApiContractSnapshotTests : ApiTestBase
    {
        public ApiContractSnapshotTests(EmsApiFactory factory) : base(factory) { }

        private static string BaselinePath([CallerFilePath] string thisFile = "") =>
            Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "Contracts", "api-contract.baseline.txt"));

        [Fact]
        public void The_public_api_contract_matches_the_committed_baseline()
        {
            var actual = Describe();
            var path = BaselinePath();

            if (Environment.GetEnvironmentVariable("UPDATE_API_CONTRACT") == "1")
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, actual);
                return;
            }

            Assert.True(File.Exists(path), $"No baseline at {path}. Create it with UPDATE_API_CONTRACT=1.");
            var expected = File.ReadAllText(path).Replace("\r\n", "\n");
            if (expected != actual)
            {
                var a = expected.Split('\n');
                var b = actual.Split('\n');
                var firstDiff = Enumerable.Range(0, Math.Max(a.Length, b.Length))
                    .First(i => (i < a.Length ? a[i] : null) != (i < b.Length ? b[i] : null));
                Assert.Fail($"API contract changed (first difference at line {firstDiff + 1}):\n  baseline: {(firstDiff < a.Length ? a[firstDiff] : "<end>")}\n  actual:   {(firstDiff < b.Length ? b[firstDiff] : "<end>")}");
            }
        }

        private string Describe()
        {
            var provider = _factory.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>();
            var sb = new StringBuilder();

            foreach (var api in provider.ApiDescriptionGroups.Items.SelectMany(g => g.Items)
                         .OrderBy(a => a.RelativePath, StringComparer.Ordinal).ThenBy(a => a.HttpMethod, StringComparer.Ordinal))
            {
                sb.Append(api.HttpMethod).Append(' ').AppendLine(api.RelativePath);

                var metadata = api.ActionDescriptor.EndpointMetadata;
                var policies = metadata.OfType<IAuthorizeData>().Select(a => a.Policy ?? (a.Roles is null ? "(authenticated)" : "roles:" + a.Roles)).Distinct().OrderBy(x => x);
                sb.AppendLine("  authorization: " + (metadata.OfType<IAllowAnonymous>().Any() ? "anonymous" : string.Join(", ", policies)));

                foreach (var p in api.ParameterDescriptions.OrderBy(p => p.Source.Id, StringComparer.Ordinal).ThenBy(p => p.Name, StringComparer.Ordinal))
                {
                    if (p.Source == BindingSource.Body)
                        sb.AppendLine($"  body: {TypeText(p.Type)}").Append(Members(p.Type, "    "));
                    else
                        sb.AppendLine($"  {p.Source.Id}: {p.Name} : {TypeText(p.Type)}{(p.IsRequired ? " (required)" : "")}");
                }

                foreach (var r in api.SupportedResponseTypes.OrderBy(r => r.StatusCode))
                {
                    sb.AppendLine($"  response {r.StatusCode}: {(r.Type is null ? "(none)" : TypeText(r.Type))}");
                    if (r.Type is not null) sb.Append(Members(r.Type, "    "));
                }
            }

            return sb.ToString().Replace("\r\n", "\n");
        }

        private static string TypeText(Type? type)
        {
            if (type is null) return "(none)";
            var underlying = Nullable.GetUnderlyingType(type);
            if (underlying is not null) return TypeText(underlying) + "?";
            if (!type.IsGenericType) return type.Name;
            var name = type.Name[..type.Name.IndexOf('`')];
            return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(TypeText))}>";
        }

        private static string Members(Type type, string indent)
        {
            var element = ElementOf(type);
            if (element is not null) type = element;
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type.Namespace?.StartsWith("System") == true && !type.IsGenericType)
                return "";

            var sb = new StringBuilder();
            foreach (var p in type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                sb.AppendLine($"{indent}{p.Name} : {TypeText(p.PropertyType)}");
            return sb.ToString();
        }

        private static Type? ElementOf(Type type) =>
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>) ? type.GetGenericArguments()[0] : null;
    }
}
