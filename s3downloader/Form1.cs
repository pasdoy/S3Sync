using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace s3downloader
{
    public partial class Form1 : Form
    {
        AmazonS3Config config = new AmazonS3Config();
        AmazonS3Client s3Client;
        public Form1()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.FixedSingle; //fixed window
            //Log("Switch destination folder: " + folderBrowserDialog1.SelectedPath);

            config.RegionEndpoint = RegionEndpoint.USEast1;
            s3Client = new AmazonS3Client(null, null, config);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.ShowDialog();
            //Log("Switch destination folder: " + folderBrowserDialog1.SelectedPath);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;
            button2.Text = "Started";

            if (textBox1.Text == "")
            {
                LogError("Missing S3 folder");
                return;
            }

            if (folderBrowserDialog1.SelectedPath == "")
            {
                LogError("Missing destination folder");
                return;
            }

            ListObjectsRequest request = new ListObjectsRequest();
            request.BucketName = textBox1.Text;
            ListObjectsResponse response = new ListObjectsResponse();
            try
            {
                response = s3Client.ListObjects(request);
            } catch(Exception ee)
            {
                LogError("ERROR: " + ee.Message);
                return;
            }

            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                LogError("S3 foler cannot be listed, http code: " + response.HttpStatusCode);
                return;
            }

            Log("Found object count: " + response.S3Objects.Count);

            foreach (S3Object o in response.S3Objects)
            {
                Application.DoEvents();
                var keySplit = o.Key.Split('/');
                if (keySplit[keySplit.Length - 1] == "")
                {
                    //just folder name
                    continue;
                }

                try
                {
                    GetObjectRequest objReq = new GetObjectRequest
                    {
                        BucketName = o.BucketName,
                        Key = o.Key
                    };

                    using (GetObjectResponse objRes = s3Client.GetObject(objReq))
                    {
                        string dest = Path.Combine(folderBrowserDialog1.SelectedPath, o.Key);
                        if (!File.Exists(dest))
                        {
                            objRes.WriteResponseStreamToFile(dest);
                        }
                    }
                    

                    Log("Downloaded " + o.Key);
                }
                catch (Exception ee)
                {
                    Log("Failed to download " + o.Key + " " + ee.Message);
                    continue;
                }

            }

            LogError("Finished!");
        }

        private void LogError(String s)
        {
            richTextBox1.AppendText(s + "\n");
            button2.Enabled = true;
            button2.Text = "Start";
        }

        private void Log(String s)
        {
            richTextBox1.AppendText(s + "\n");
        }
    }
}
