using System;
using System.Runtime.InteropServices;
using NeoCardium.Helpers;
using Microsoft.UI.Xaml.Controls;

namespace NeoCardium.Helpers
{
    public static class KeyboardHelper
    {
        // P/Invoke: GetKeyState aus user32.dll
        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        private const int VK_SHIFT = 0x10;
        private const int VK_CONTROL = 0x11;
        private const int VK_MENU = 0x12; // ALT

        /// <summary>
        /// Prüft, ob mindestens eine Modifier-Taste (Shift, Control oder Alt) gedrückt ist.
        /// </summary>
        public static bool IsModifierKeyPressed()
        {
            try
            {
                bool shiftPressed = (GetKeyState(VK_SHIFT) & 0x8000) != 0;
                bool ctrlPressed = (GetKeyState(VK_CONTROL) & 0x8000) != 0;
                bool altPressed = (GetKeyState(VK_MENU) & 0x8000) != 0;
                return shiftPressed || ctrlPressed || altPressed;
            }
            catch (Exception ex)
            {
                // Hinweis: In einer produktiven App sollten Sie ggf. einen geeigneteren Fallback nutzen.
                ExceptionHelper.ShowErrorInfoBar(new InfoBar(), "Fehler beim Prüfen der Tastatureingabe.", ex);
                return false;
            }
        }
    }
}