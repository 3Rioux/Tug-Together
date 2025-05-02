// C#
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

public static class WidgetDependenciesWrapper
{
    private static readonly Type widgetDependenciesType;
    private static readonly object instance;

    static WidgetDependenciesWrapper()
    {
        widgetDependenciesType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("Unity.Multiplayer.Widgets.WidgetDependencies"))
            .FirstOrDefault(t => t != null);

        if (widgetDependenciesType != null)
        {
            PropertyInfo instanceProp = widgetDependenciesType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            if (instanceProp != null)
            {
                instance = instanceProp.GetValue(null);
            }
            else
            {
                Debug.LogError("Instance property not found on WidgetDependencies.");
            }
        }
        else
        {
            Debug.LogError("WidgetDependencies type not found.");
        }
    }

    public static async Task<string> GetPlayerNameAsync()
    {
        if (instance == null || widgetDependenciesType == null)
            return "";

        PropertyInfo authProp = widgetDependenciesType.GetProperty("AuthenticationService", BindingFlags.Public | BindingFlags.Instance);
        if (authProp == null)
            return "";

        object authService = authProp.GetValue(instance);
        if (authService == null)
            return "";

        MethodInfo getPlayerNameMethod = authService.GetType().GetMethod("GetPlayerNameAsync", BindingFlags.Public | BindingFlags.Instance);
        if (getPlayerNameMethod == null)
            return "";

        // Get the method parameters and build an array with default values (null) for each expected parameter.
        ParameterInfo[] methodParams = getPlayerNameMethod.GetParameters();
        object[] args = new object[methodParams.Length];
        for (int i = 0; i < methodParams.Length; i++)
        {
            args[i] = Type.Missing;
        }

        var task = (Task<string>)getPlayerNameMethod.Invoke(authService, args);
        string playerName = await task;
        Debug.Log("Retrieved player name: " + playerName);
        return playerName;
    }
}