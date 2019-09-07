using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace DialogGenerator.ZIPFIleUtility
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        #region Event Handlers
        private void BtnConvertFromZip_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            string fileFilter = "";
            
            if(btnZipFrom.IsChecked == true)
            {
                fileFilter = "ZIP files (*.zip) | *.zip";
            } else
            {
                fileFilter = "TOYS2LIFE files (*.t2lf) | *.t2lf";
            }

            ofd.Filter = fileFilter;
            if(ofd.ShowDialog() != true)
            {
                return;
            }

            txtFromZip.Text = ofd.FileName;
        }

        private void BtnStartConversion_Click(object sender, RoutedEventArgs e)
        {
            bool result = false;
            if (btnZipFrom.IsChecked == true)
            {
                result = fromZip(txtFromZip.Text);
            }
            else
            {
                result = toZip(txtFromZip.Text);
            }

            if (result == true)
            {                
                MessageBox.Show(Application.Current.MainWindow, "The conversion was done successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(Application.Current.MainWindow, "Something went wrong!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);                
            }
        }

        #endregion

        #region Private utilities
        private bool fromZip(string fileName)
        {
            // Header content.
            string headerContent = "TOYS2LIFE_THE_DIALOG_GENERATOR";

            // Create addition to the ZIP file header.
            byte[] headerInBytes = new byte[headerContent.Length];
            char[] headerInChars = headerContent.ToArray();
            for (int i = 0; i < headerInChars.Length; i++)
            {
                headerInBytes[i] = Convert.ToByte(headerInChars[i]);
            }

            // Get contents of temporary ZIP file.
            byte[] zipFileBytes = File.ReadAllBytes(fileName);

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Toys2Life files (*.t2lf) | *.t2lf";
            if(sfd.ShowDialog() != true)
            {
                return false;
            }

            // Create new file with the customized header.
            FileStream sb = new FileStream(sfd.FileName, FileMode.OpenOrCreate);
            sb.Write(headerInBytes, 0, headerInBytes.Length);
            sb.Write(zipFileBytes, 0, zipFileBytes.Length);
            sb.Close();

            return true;
        }

        private bool toZip(string fileName)
        {
            // Header content.
            string headerContent = "TOYS2LIFE_THE_DIALOG_GENERATOR";
            int headerLength = headerContent.Length;

            // Open file.
            byte[] initialFileBytes = File.ReadAllBytes(fileName);

            // Read header.
            byte[] headerBytes = new byte[headerLength];
            Array.Copy(initialFileBytes, 0, headerBytes, 0, headerLength);
            char[] headerChars = new char[headerLength];
            for (int i = 0; i < headerLength; i++)
            {
                headerChars[i] = Convert.ToChar(headerBytes[i]);
            }

            string header = new string(headerChars);
            if (!header.Equals(headerContent))
            {
                return false;
            }

            // Save the rest as temporary file (zip file).
            byte[] tempFileBytes = new byte[initialFileBytes.Length - headerLength];
            Array.Copy(initialFileBytes, headerLength, tempFileBytes, 0, tempFileBytes.Length);

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "ZIP files (*.zip) | *.zip";
            if (sfd.ShowDialog() != true)
            {
                return false;
            }

            FileStream sb = new FileStream(sfd.FileName, FileMode.OpenOrCreate);
            sb.Write(tempFileBytes, 0, tempFileBytes.Length);
            sb.Close();

            return true;
        }
        #endregion

        private void BtnExitApp_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
