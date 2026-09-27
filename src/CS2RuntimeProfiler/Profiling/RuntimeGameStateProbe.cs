using System;
using System.Reflection;
using Game.SceneFlow;

namespace CS2RuntimeProfiler.Profiling
{
    internal static class RuntimeGameStateProbe
    {
        private static readonly BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static bool IsAutomaticCaptureAllowed()
        {
            var gameManager = GameManager.instance;
            if (gameManager == null)
                return false;

            try
            {
                var type = gameManager.GetType();
                var property = type.GetProperty("isGameLoading", InstanceFlags)
                    ?? type.GetProperty("IsGameLoading", InstanceFlags);
                if (property != null && property.PropertyType == typeof(bool) && property.GetIndexParameters().Length == 0)
                    return !(bool)property.GetValue(gameManager, null);

                var getter = type.GetMethod("get_isGameLoading", InstanceFlags, null, Type.EmptyTypes, null);
                if (getter != null && getter.ReturnType == typeof(bool))
                    return !(bool)getter.Invoke(gameManager, null);

                var field = type.GetField("m_IsGameLoading", InstanceFlags)
                    ?? type.GetField("isGameLoading", InstanceFlags);
                if (field != null && field.FieldType == typeof(bool))
                    return !(bool)field.GetValue(gameManager);
            }
            catch
            {
                // Loading-state discovery is a compatibility gate only. If a future game build changes
                // the member, fail open rather than disabling automatic capture for the whole session.
                return true;
            }

            return true;
        }
    }
}
