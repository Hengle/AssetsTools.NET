﻿using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AssetsView.Winforms
{
    public partial class OpenBundleDialog : Form
    {
        public int selection = -1;
        public BundleFileInstance inst;
        public AssetBundleFile file;
        private string filePath;
        private string fileName;
        public OpenBundleDialog(AssetsManager helper, string filePath)
        {
            InitializeComponent();
            this.filePath = filePath;
            fileName = Path.GetFileName(filePath);
            inst = helper.LoadBundleFile(filePath);
            file = inst.file;
            uint compressionMethod = file.bundleHeader6.flags & 0x3F;
            if (compressionMethod == 0)
            {
                justThisFile.Enabled = true;
                fileAndDependencies.Enabled = true;
                compressBundle.Enabled = true;
                status.Text = $"Opening bundle file {fileName}...";
            }
            else
            {
                decompressBundle.Enabled = true;
                decompressBundleInMemory.Enabled = true;
                if (compressionMethod == 1)
                {
                    status.Text = $"Opening bundle file {fileName} (LZMA)...";
                }
                else if (compressionMethod == 2 || compressionMethod == 3)
                {
                    status.Text = $"Opening bundle file {fileName} (LZ4)...";
                }
                else
                {
                    status.Text = $"Opening bundle file {fileName} (???)...";
                }
            }
        }

        private void justThisFile_Click(object sender, EventArgs e)
        {
            selection = 0;
            Close();
        }

        private void fileAndDependencies_Click(object sender, EventArgs e)
        {
            selection = 1;
            Close();
        }

        private void decompressBundle_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.InitialDirectory = filePath;
            sfd.FileName = Path.GetFileName(fileName) + ".unpacked";
            sfd.Filter = "All types (*.*)|*.*";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                DecompressBundle(File.Open(sfd.FileName, FileMode.Create, FileAccess.ReadWrite));
            }
        }

        private void decompressBundleInMemory_Click(object sender, EventArgs e)
        {
            DecompressBundle(new MemoryStream());
        }

        private void DecompressBundle(Stream stream)
        {
            status.Text = "Decompressing...";
            decompressBundle.Enabled = false;
            decompressBundleInMemory.Enabled = false;
            BackgroundWorker bw = new BackgroundWorker();
            string error = string.Empty;
            bw.DoWork += delegate
            {
                try
                {
                    file.reader.Position = 0;
                    file.Unpack(file.reader, new AssetsFileWriter(stream));
                    stream.Position = 0;
                    file = new AssetBundleFile();
                    file.Read(new AssetsFileReader(stream), false);
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
            };
            bw.RunWorkerCompleted += delegate
            {
                if (error != string.Empty)
                {
                    MessageBox.Show("An error occurred:\n" + error, "AssetsView", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    justThisFile.Enabled = true;
                    fileAndDependencies.Enabled = true;
                    compressBundle.Enabled = true;
                    status.Text = $"Opening bundle file {fileName}...";
                }
            };
            bw.RunWorkerAsync();
        }

        private void compressBundle_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Compression not implemented yet.", "Assets View");
        }
    }
}