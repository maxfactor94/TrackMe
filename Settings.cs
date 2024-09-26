using System;
using System.IO;
using System.Windows.Forms;

namespace TrackMe
{
    public partial class Settings : Form
    {
        private string startFileName = "startTime.dat";
        private string tokenFileName = "token.dat";
        private string trackFileName = "track.dat";

        public Settings()
        {
            InitializeComponent();
            textBox1.Text = File.ReadAllLines(trackFileName)[0].Split(":".ToCharArray())[1];
            textBox2.Text = File.ReadAllLines(trackFileName)[1].Split(":".ToCharArray())[1];
            textBox3.Text = File.ReadAllLines(trackFileName)[2].Split(":".ToCharArray())[1];
            textBox4.Text = File.ReadAllText(startFileName);
            textBox5.Text = File.ReadAllText(tokenFileName);
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            string[] shapeTrack = { "issueId:"+textBox1.Text, "hours:" + textBox2.Text, "comment:"+textBox3.Text };
            File.WriteAllLines(trackFileName, shapeTrack);
            File.WriteAllText(startFileName, textBox4.Text);
            File.WriteAllText(tokenFileName, textBox5.Text);
            MessageBox.Show("Сохранено!");
            this.Close();
        }
    }
}
