// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace titlebar_customization
{
    public sealed partial class MinMaxCloseControl : UserControl
    {
        public MinMaxCloseControl()
        {
            this.InitializeComponent();
        }
        private void Min_Button_Click(object sender, RoutedEventArgs e)
        {
            OnMin?.Invoke(this, new EventArgs());
        }

        private void Max_Button_Click(object sender, RoutedEventArgs e)
        {
            OnMax?.Invoke(this, new EventArgs());
        }
        private void Close_Button_Click(object sender, RoutedEventArgs e)
        {
            OnClose?.Invoke(this, new EventArgs());
        }

        public event EventHandler OnMin;
        public event EventHandler OnMax;
        public event EventHandler OnClose;


    }
}
