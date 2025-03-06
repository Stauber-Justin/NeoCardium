using System;
using System.Runtime.Versioning;
using Windows.System;
using Windows.UI.Core;
using NeoCardium.Helpers;

namespace NeoCardium.Helpers
{
    public static class KeyboardHelper
    {
        /// <summary>
        /// Prüft, ob eine Modifier-Taste (STRG, SHIFT oder ALT) gedrückt ist.
        /// </summary>
        [SupportedOSPlatform("windows10.0.10240.0")]
        public static bool IsModifierKeyPressed()
        {
            try
            {
                var coreWindow = CoreWindow.GetForCurrentThread();
                if (coreWindow == null)
                {
                    ExceptionHelper.ShowErrorInfoBar(new Microsoft.UI.Xaml.Controls.InfoBar(), "Fehler: CoreWindow ist null!");
                    return false;
                }

                return coreWindow.GetKeyState(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down) ||
                       coreWindow.GetKeyState(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down) ||
                       coreWindow.GetKeyState(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down);
            }
            catch (Exception ex)
            {
                ExceptionHelper.ShowErrorInfoBar(new Microsoft.UI.Xaml.Controls.InfoBar(), "Fehler beim Prüfen der Tastatureingabe.", ex);
                return false;
            }
        }
    }
}
