using System;
using System.Reflection;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace McpUnity.Resources
{
    /// <summary>
    /// Resource for listing available Component-derived types in the project, for use with
    /// update_component/destroy_component
    /// </summary>
    public class GetComponentTypesResource : McpResourceBase
    {
        public GetComponentTypesResource()
        {
            Name = "get_component_types";
            Description = "Retrieves the names of all non-abstract Component-derived types available in the " +
                "project, optionally filtered by 'searchPattern'";
            Uri = "unity://component-types";
        }

        /// <summary>
        /// Fetch the list of available component types
        /// </summary>
        /// <param name="parameters">Optional 'searchPattern' to filter type names (case-insensitive substring match)</param>
        /// <returns>A JObject containing the matching component types</returns>
        public override JObject Fetch(JObject parameters)
        {
            string searchPattern = parameters?["searchPattern"]?.ToObject<string>();

            JArray typesArray = new JArray();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = Array.FindAll(ex.Types, t => t != null);
                }
                catch (Exception)
                {
                    continue;
                }

                foreach (Type type in types)
                {
                    if (type.IsAbstract || !typeof(Component).IsAssignableFrom(type))
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(searchPattern) &&
                        type.Name.IndexOf(searchPattern, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    typesArray.Add(new JObject
                    {
                        ["name"] = type.Name,
                        ["fullName"] = type.FullName,
                        ["assembly"] = assembly.GetName().Name
                    });
                }
            }

            return new JObject
            {
                ["success"] = true,
                ["message"] = $"Retrieved {typesArray.Count} component type(s)",
                ["componentTypes"] = typesArray
            };
        }
    }
}
