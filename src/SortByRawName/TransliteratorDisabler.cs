using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using MediaBrowser.Controller.Entities;

namespace SortByRawName;

/// <summary>
/// Disables Jellyfin's ICU transliterator.
/// An empty id is not the same as an unset variable: unset keeps the Latin default.
/// The server also caches the first transliterator it creates, so the cache has to be replaced.
/// </summary>
public static class TransliteratorDisabler
{
    public const string VariableName = "JELLYFIN_TRANSLITERATOR_ID";

    [ModuleInitializer]
    internal static void Initialize()
    {
        try
        {
            Disable();
        }
        catch (Exception)
        {
            // A throw here stops Jellyfin from loading the plugin at all.
        }
    }

    public static void Disable()
    {
        Environment.SetEnvironmentVariable(VariableName, string.Empty);
        ReplaceCachedTransliterator();
    }

    private static void ReplaceCachedTransliterator()
    {
        var assemblies = new List<Assembly>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (string.Equals(assembly.GetName().Name, "Jellyfin.Extensions", StringComparison.Ordinal))
            {
                assemblies.Add(assembly);
            }
        }

        try
        {
            var reference = typeof(BaseItem).Assembly.GetReferencedAssemblies()
                .FirstOrDefault(name => string.Equals(name.Name, "Jellyfin.Extensions", StringComparison.Ordinal));
            if (reference is not null)
            {
                var serverAssembly = AssemblyLoadContext.Default.LoadFromAssemblyName(reference);
                if (!assemblies.Contains(serverAssembly))
                {
                    assemblies.Add(serverAssembly);
                }
            }
        }
        catch (Exception)
        {
            // The server copy is not available yet. The empty variable covers its first use.
        }

        foreach (var assembly in assemblies)
        {
            var type = assembly.GetType("Jellyfin.Extensions.StringExtensions");
            var field = type?.GetField("_transliterator", BindingFlags.Static | BindingFlags.NonPublic);
            if (field is null)
            {
                continue;
            }

            SetStaticField(field, CreateNullLazy(field.FieldType));
        }
    }

    private static void SetStaticField(FieldInfo field, object value)
    {
        var method = new DynamicMethod(
            "SetSortByRawNameTransliterator",
            typeof(void),
            [field.FieldType],
            field.DeclaringType!,
            skipVisibility: true);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Stsfld, field);
        il.Emit(OpCodes.Ret);
        method.Invoke(null, [value]);
    }

    private static object CreateNullLazy(Type lazyType)
    {
        var valueType = lazyType.GetGenericArguments()[0];
        var factoryType = typeof(Func<>).MakeGenericType(valueType);
        var factory = Expression.Lambda(factoryType, Expression.Constant(null, valueType)).Compile();
        return Activator.CreateInstance(lazyType, factory)!;
    }
}
