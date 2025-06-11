using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SimpleMD.Controls
{
    public class ResizableGrid : Grid
    {
        public void SetCursor(InputSystemCursorShape cursorShape)
        {
            this.ProtectedCursor = InputSystemCursor.Create(cursorShape);
        }

        public void ResetCursor()
        {
            this.ProtectedCursor = null;
        }
    }
}
