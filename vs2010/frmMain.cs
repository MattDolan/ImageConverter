using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Microsoft.Win32;  //This is for the registry

/* This is designed to allow a use to select a group of files (or just one) and
 * have the software batch process the files converting them from one format to another.
 * 
 * Matt Dolan - 09/2015
 * 
 * This requires the use of ImageMagick  
 * http://www.imagemagick.org/script/index.php
 * http://www.imagemagick.org/script/license.php
 *
 * ImageMagick relies on Ghostscript
 * http://www.ghostscript.com/
 * http://www.ghostscript.com/Licensing.html
 * 
 * 'Here are all the options for converting files
 * http://www.imagemagick.org/script/convert.php
 * 
 * The standard format of the command is
 *   C:\CurrentFolder>convert -verbose -density 150 -trim fileToConvert.jpg -quality 100 -sharpen 0x1.0 outputFile.pdf
 *   
 */
namespace ImageConverter
{
    public partial class frmMain : Form
    {
        string strLastPath = "";
        RegistryKey keySelectMode;
        RegistryKey keyLastPath;
        RegistryKey keyOptions;

        public frmMain()
        {
            InitializeComponent();

            // Get the last used value for the "multi select mode" checkbox
            try
            {
                string value = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Software\QCData\ImageConvert", "SelectMode", "checked");
                if (value == "Checked")
                {
                    chkSimple.CheckState = CheckState.Checked;
                    listBox1.SelectionMode = SelectionMode.MultiSimple;
                }else{
                    chkSimple.CheckState = CheckState.Unchecked;
                    listBox1.SelectionMode = SelectionMode.MultiExtended;
                }
            }catch{
                //This is just here for the first time the tool is run on a worstation.
            }
        }

        /***************************************************************************************************/

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        /***************************************************************************************************/

        private void btnSelectFolder_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();

            /* Have the user select the directory for the files. */

            FolderBrowserDialog fbd = new FolderBrowserDialog();
            fbd.SelectedPath = strLastPath;
            DialogResult result = fbd.ShowDialog();

            /* Check for a cancel */

            if (result == DialogResult.Cancel)
            {
                return;
            }

            string[] files = Directory.GetFiles(fbd.SelectedPath);
            
            /* Loop through the files adding the to the file list. */

            foreach (string s in files)
            {
                listBox1.Items.Add(Path.GetFileName(s));
            }

            /* Save the last directory selected */

            keyLastPath = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\QCData\ImageConvert");
            keyLastPath.SetValue("LastPath", fbd.SelectedPath);
            keyLastPath.Close();
            strLastPath = fbd.SelectedPath;

        }

        /***************************************************************************************************/

        private void frmMain_Load(object sender, EventArgs e)
        {
            string value = "";
            
            /* Load the settings from the last time the user used the tool */

            strLastPath = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Software\QCData\ImageConvert", "LastPath", null);
            
            value = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Software\QCData\ImageConvert", "OutPutDir", null);
            if (value != null)
            {
                txtSubFolder.Text = value;
            }
            value = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Software\QCData\ImageConvert", "Density", null);
            if (value != null)
            {
                txtDensity.Text = value;
            }
            value = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Software\QCData\ImageConvert", "Quality", null);
            if (value != null)
            {
                txtQuality.Text = value;
            }

            chkSharpen.Checked = Convert.ToBoolean(Registry.GetValue(@"HKEY_CURRENT_USER\Software\QCData\ImageConvert", "Sharpen", null));
            chkTrim.Checked = Convert.ToBoolean(Registry.GetValue(@"HKEY_CURRENT_USER\Software\QCData\ImageConvert", "Trim", null));
            
            /* Format extension */

            value = (string)Registry.GetValue(@"HKEY_CURRENT_USER\Software\QCData\ImageConvert", "OutputFormat", null);
            if (value == "BMP")
            {
                cmbOutputFormat.Text = "BMP";
            }
            else if (value == "GIF")
            {
                cmbOutputFormat.Text = "GIF";
            }
            else if (value == "JPEG")
            {
                cmbOutputFormat.Text = "JPEG";
            }
            else if (value == "PDF")
            {
                cmbOutputFormat.Text = "PDF";
            }
            else if (value == "PNG")
            {
                cmbOutputFormat.Text = "PNG";
            }
            else if (value == "TIFF")
            {
                cmbOutputFormat.Text = "TIFF";
            }


        }

        /***************************************************************************************************/

        private void chkSimple_Click(object sender, EventArgs e)
        {
            if (chkSimple.Checked == false)
            {
                listBox1.SelectionMode = SelectionMode.MultiExtended;
                keySelectMode = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\QCData\ImageConvert");
                keySelectMode.SetValue("SelectMode", chkSimple.CheckState);
                keySelectMode.Close();
            }
            else
            {
                listBox1.SelectionMode = SelectionMode.MultiSimple;
                keySelectMode = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\QCData\ImageConvert");
                keySelectMode.SetValue("SelectMode", chkSimple.CheckState);
                keySelectMode.Close();
            }
        }

        /***************************************************************************************************/

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("http://www.imagemagick.org/script/convert.php");
            }
            catch
            {
                MessageBox.Show("Error opening webpage http://www.imagemagick.org/script/convert.php", "Error opening folder.");
            }
            
        }

        /***************************************************************************************************/

        private void btnProcess_Click(object sender, EventArgs e)
        {
            string strPrefix = "convert";
            string strOutputPrefix = "";
            TextWriter tw;

            /* Record the condition of the form for the next time it is opened. */

            keyOptions = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\QCData\ImageConvert");
            keyOptions.SetValue("OutPutDir", txtSubFolder.Text);
            keyOptions.Close();

            keyOptions = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\QCData\ImageConvert");
            keyOptions.SetValue("Density", txtDensity.Text);
            keyOptions.Close();

            keyOptions = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\QCData\ImageConvert");
            keyOptions.SetValue("Quality", txtQuality.Text);
            keyOptions.Close();
            
            keyOptions = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\QCData\ImageConvert");
            keyOptions.SetValue("Sharpen", chkSharpen.Checked);
            keyOptions.Close();

            keyOptions = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\QCData\ImageConvert");
            keyOptions.SetValue("Trim", chkTrim.Checked);
            keyOptions.Close();

            keyOptions = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\QCData\ImageConvert");
            keyOptions.SetValue("OutputFormat", cmbOutputFormat.Text);
            keyOptions.Close();

            /* Build the strings to create the script */

            if (txtDensity.Text != "")
            {
                strPrefix = strPrefix + " -density " + txtDensity.Text;
            }
            if (chkTrim.Checked == true)
            {
                strPrefix = strPrefix + " -trim";
            }

            if (txtQuality.Text != "")
            {
                strOutputPrefix = strOutputPrefix + " -quality " + txtQuality.Text;
            }
            if (chkSharpen.Checked == true)
            {
                strOutputPrefix = strOutputPrefix + " -sharpen 0x1.0";
            }

            /* Create the batch file */

            string path = @strLastPath + "\\Process.bat";
            if (File.Exists(path))
            {
                File.Create(path).Close();
            }else{
                File.Delete(path);
                File.Create(path).Close();
            }

            strLastPath = strLastPath + "\\";

            if (txtSubFolder.Text != "")
            {
                DirectoryInfo di = Directory.CreateDirectory(strLastPath + txtSubFolder.Text);
            }

            /* Write the commands to the file */

            tw = new StreamWriter(path);
            foreach (var item in listBox1.SelectedItems)
            {
                if (txtSubFolder.Text == "")
                {
                    tw.WriteLine(strPrefix + " \"" + strLastPath + item.ToString() + "\" " + strOutputPrefix + " \"" + strLastPath + Path.GetFileNameWithoutExtension(item.ToString()) + "." + cmbOutputFormat.Text + "\"");
                }
                else
                {
                    tw.WriteLine(strPrefix + " \"" + strLastPath + item.ToString() + "\" " + strOutputPrefix + " \"" + strLastPath + txtSubFolder.Text + "\\" + Path.GetFileNameWithoutExtension(item.ToString()) + "." + cmbOutputFormat.Text + "\"");
                }
            }
            
            tw.WriteLine("Pause");
            tw.Close();

            /* Shell out the file for execution */

            System.Diagnostics.Process.Start(path);

        }

        /***************************************************************************************************/

        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < listBox1.Items.Count; i++)
            {
                listBox1.SetSelected(i, true);
            }
        }

        /***************************************************************************************************/

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("http://www.imagemagick.org/script/formats.php");
            }
            catch
            {
                MessageBox.Show("Error opening webpage http://www.imagemagick.org/script/formats.php", "Error opening folder.");
            }
        }

        private void txtQuality_TextChanged(object sender, EventArgs e)
        {

        }

        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("http://www.imagemagick.org/Usage/formats/#summary");
            }
            catch
            {
                MessageBox.Show("Error opening webpage http://www.imagemagick.org/Usage/formats/#summary", "Error opening folder.");
            }
        }

    }
}
