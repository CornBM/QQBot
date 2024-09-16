using Lagrange.Core.Message;
using System.Windows.Forms;
using IniParser;
using IniParser.Model;


namespace QQBot
{
    public partial class Form1 : Form
    {
        private QQBot bot;
        private IniData config;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var parser = new FileIniDataParser();
            try
            {
                config = parser.ReadFile("config.ini");
                this.autologin.Checked = bool.Parse(config["User"]["autologin"]);
                uint groupId = uint.Parse(config["Bot"]["groupId"]);
                //�ǻ��ģ�����ò�����д
                string appId = config["Spark"]["appId"];
                string appSecret = config["Spark"]["appSecret"];
                string apiKey = config["Spark"]["apiKey"];

                bot = new QQBot(groupId, appId, appSecret, apiKey, this.autologin.Checked);
            }
            // �����ļ�����
            catch (System.Exception)
            {
                // ��ʾ
                MessageBox.Show("�����ļ���������config.ini�Ƿ���ڻ��ʽ�Ƿ���ȷ��", "����", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private async void login_Click(object sender, EventArgs e)
        {
            PictureBox p = new PictureBox();
            this.panel1.Controls.Add(p);
            p.Dock = DockStyle.Fill;

            await bot.Login(new Action<byte[]>((qrCode) =>
            {
                // ��bytesתΪpng���浽����
                using (MemoryStream ms = new MemoryStream(qrCode))
                {
                    Image img = Image.FromStream(ms);
                    p.SizeMode = PictureBoxSizeMode.Zoom;
                    p.Size = new Size(400, 400);
                    p.Image = img;
                }
            }));
            MessageBox.Show("��¼�ɹ���", "��ʾ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.panel1.Controls.Remove(p);
        }

        private void send_Click(object sender, EventArgs e)
        {
            bot.SendMessage("������ǻ�����");
        }

        private void autologin_CheckedChanged(object sender, EventArgs e)
        {
            config["User"]["autologin"] = this.autologin.Checked.ToString();
            if (bot!= null)
                bot.AutoLogin = this.autologin.Checked;
            var parser = new FileIniDataParser();
            parser.WriteFile("config.ini", config);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

    }
}
