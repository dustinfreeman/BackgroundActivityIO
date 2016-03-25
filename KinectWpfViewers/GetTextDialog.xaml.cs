using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Threading;

namespace Microsoft.Samples.Kinect.WpfViewers
{
    /// <summary>
    /// Interaction logic for GetTextDialog.xaml
    /// </summary>
    public partial class GetTextDialog : Window
    {
        public event Action OK;

        public GetTextDialog()
        {
            InitializeComponent();

            this.KeyUp += new KeyEventHandler(GetTextDialog_KeyUp);
        }

        void GetTextDialog_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OK();
                this.Hide();
            }
            if (e.Key == Key.Escape)
            {
                this.Hide();
            }
        }

        public string Description
        {
            set { DescriptionLbl.Content = value; }
        }

        public string Text
        {
            get { return DialogText.Text; }
        }

        public long Long
        {
            get { 
                long result;
                bool isNum = Int64.TryParse(DialogText.Text, out result);
                if (isNum)
                {
                    return result;
                }
                else
                {
                    return 0;
                }
            }
        }

        public new void ShowDialog()
        {
            //returns true if dialog is to be accepted, false if cancelled
            this.Show();

            DialogText.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            OK();
            this.Hide();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }
    }
}
