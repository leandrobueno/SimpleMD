using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using System.Reflection;

namespace SimpleMD.Helpers
{
    public static class CursorHelper
    {
        public static void SetCursor(this UIElement element, InputSystemCursorShape cursorShape)
        {
            var cursor = InputSystemCursor.Create(cursorShape);
            typeof(UIElement).InvokeMember("ProtectedCursor",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                null, element, new[] { cursor });
        }

        public static void ResetCursor(this UIElement element)
        {
            typeof(UIElement).InvokeMember("ProtectedCursor",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                null, element, new object?[] { null });
        }
    }
}
