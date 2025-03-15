using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using NeoCardium.ViewModels;

namespace NeoCardium.Views
{
    /// <summary>
    /// PracticePage – displays the practice session UI.
    /// </summary>
    public sealed partial class PracticePage : Page
    {
        // Use a single instance of PracticePageViewModel.
        public PracticePageViewModel ViewModel { get; }

        public PracticePage()
        {
            this.InitializeComponent();
            // Instantiate the ViewModel once and assign it as DataContext.
            ViewModel = new PracticePageViewModel();
            this.DataContext = ViewModel;
        }
    }
}
