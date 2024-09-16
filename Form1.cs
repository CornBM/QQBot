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
                //星火大模型配置参数填写
                string appId = config["Spark"]["appId"];
                string appSecret = config["Spark"]["appSecret"];
                string apiKey = config["Spark"]["apiKey"];

                bot = new QQBot(groupId, appId, appSecret, apiKey, this.autologin.Checked);
            }
            // 配置文件错误
            catch (System.Exception)
            {
                // 提示
                MessageBox.Show("配置文件错误，请检查config.ini是否存在或格式是否正确！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                // 把bytes转为png保存到本地
                using (MemoryStream ms = new MemoryStream(qrCode))
                {
                    Image img = Image.FromStream(ms);
                    p.SizeMode = PictureBoxSizeMode.Zoom;
                    p.Size = new Size(400, 400);
                    p.Image = img;
                }
            }));
            MessageBox.Show("登录成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.panel1.Controls.Remove(p);
        }

        private void send_Click(object sender, EventArgs e)
        {
            bot.SendMessage("你好我是机器人");
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
