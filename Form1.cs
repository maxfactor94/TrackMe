using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TrackMe
{
    public partial class Form1 : Form
    {
        private Timer timer;
        private string apiKey;
        private string redmineApiUrl = "https://redmine.cbt.by/";
        private string startFileName = "startTime.dat";
        private string tokenFileName = "token.dat";
        private string trackFileName = "track.dat";
        private string logFileName = "TrackMe.log";
        private string[] shapeTrack = { "issueId:", "hours:", "comment:" };

        public Form1()
        {
            InitializeComponent();
            if (DateTime.Now > DateTime.Parse("01.01.2026"))
            {
                MessageBox.Show("Лицензия закончилась. \nПожалуйста, обратитесь к разработчику по адресу m.ivanchik@cbt.by");
                Environment.Exit(0);
            }
            if (File.Exists(trackFileName))
            {
                Console.WriteLine("Файл "+ trackFileName + " существует");
            }
            else
            {
                File.WriteAllLines(trackFileName, shapeTrack);
                reportLog("Пожалуйста, заполните данные задачи и времени.");
            }
            if (File.Exists(tokenFileName))
            {
                Console.WriteLine("Файл " + tokenFileName + " существует");
            }
            else
            {
                File.WriteAllText(tokenFileName, "");
                reportLog("Токен доступа Redmine не установлен.");
            }
            if (File.Exists(startFileName))
            {
                Console.WriteLine("Файл " + startFileName + " существует");
            }
            else
            {
                File.WriteAllText(startFileName, "17:30:00");
                reportLog("Установлено время трэка по умолчанию 17:30.");
            }
            // Создаем таймер
            timer = new Timer();
            timer.Interval = 1000; // Интервал в миллисекундах (1000 мс = 1 секунда)
            timer.Tick += Timer_Tick; // Подписываемся на событие Tick
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateLabel3();
            // Проверяем, является ли сегодняшний день выходным (субботой или воскресеньем)
            if (DateTime.Now.DayOfWeek == DayOfWeek.Saturday || DateTime.Now.DayOfWeek == DayOfWeek.Sunday)
            {
                this.Text = "Выходной день - Track Me";
                return; // Прерываем выполнение метода, если сегодня выходной
            }
            else
            {
                // Устанавливаем заголовок формы с номером задачи
                this.Text = label7.Text+" - Track Me";
            }
            this.Text = label7.Text + " - Track Me";
            if (label3.Text == label4.Text)
            {
                int issueId;
                int.TryParse(label7.Text, out issueId);

                int hours;
                int.TryParse(label9.Text, out hours);

                string comment = label11.Text;

                _ = TrackTime(issueId, hours, comment);
            }
        }

        private void reportLog(string message)
        {
            listBox1.Items.Insert(0,DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + " - " + message);
            File.AppendAllText(logFileName, DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + " - " + message + Environment.NewLine);
        }

        private void UpdateLabel3()
        {
            label3.Text = DateTime.Now.ToString("HH:mm:ss");
            try
            {
                label4.Text = File.ReadAllText(startFileName);
            }
            catch
            {
                File.WriteAllText(startFileName, "17:30:00");
            }
            try
            {
                label6.Text = File.ReadAllText(tokenFileName);
                if (File.ReadAllText(tokenFileName) != "")
                {
                    apiKey = File.ReadAllText(tokenFileName);
                }
                else
                {
                    timer.Stop();
                    MessageBox.Show("Заполните файл token.dat. \nТокен доступа можно получить в Redmine в разделе:\nМоя учётная запись -> Ключ доступа к API -> Показать");
                    Application.Exit();
                }
            }
            catch
            {
                File.WriteAllText(tokenFileName, "");
            }
            try
            {
                label7.Text = File.ReadAllLines(trackFileName)[0].Split(":".ToCharArray())[1];
                label9.Text = File.ReadAllLines(trackFileName)[1].Split(":".ToCharArray())[1];
                label11.Text = File.ReadAllLines(trackFileName)[2].Split(":".ToCharArray())[1];
                if (File.ReadAllLines(trackFileName)[0].Split(":".ToCharArray())[1] != "" ||
                    File.ReadAllLines(trackFileName)[1].Split(":".ToCharArray())[1] != "")
                {
                    //apiKey = File.ReadAllText(trackFileName);
                }
                else
                {
                    timer.Stop();
                    MessageBox.Show("Заполните данные Заполните файл track.dat. \n" +
                        "Поле issueId - это номер задачи в Redmine\n" +
                        "Поле hours - это количество часов, которое будет отправлено в текущую задачу\n" +
                        "Поле comment - это комментарий к трэку (необязательное поле)");
                    Application.Exit();
                }
            }
            catch
            {
                File.WriteAllLines(trackFileName, shapeTrack);
            }
        }

        private void StartEndBtn_Click(object sender, EventArgs e)
        {
            if (StartEndBtn.Text == "Запустить")
            {
                StartEndBtn.Text = "Остановить";
                timer.Start(); // Запускаем таймер
                reportLog($"Запущен");
            }
            else
            {
                StartEndBtn.Text = "Запустить";
                timer.Stop();
                reportLog($"Остановлен");
            }
        }

        public async Task TrackTime(int issueId, int hours, string comments)
        {
            using (var client = new HttpClient())
            {
                reportLog("Authorization through X-Redmine-API-Key: " + apiKey);
                client.DefaultRequestHeaders.Add("X-Redmine-API-Key", apiKey);
                client.DefaultRequestHeaders.Accept.Clear();

                var content = new StringContent(
                    $"{{ \"time_entry\": {{ \"issue_id\": {issueId}, \"hours\": {hours}, \"comments\": \"{comments}\" }} }}",
                    Encoding.UTF8,
                    "application/json"
                );
                //reportLog($"{{ \"time_entry\": {{ \"issue_id\": {issueId}, \"hours\": {hours}, \"comments\": \"{comments}\" }} }}");
                reportLog("Waiting for a response from " + redmineApiUrl);
                try
                {
                    var response = await client.PostAsync($"{redmineApiUrl}/time_entries.json", content);

                    if (response.IsSuccessStatusCode)
                    {
                        reportLog("Time entry tracked successfully.");
                        reportLog("In task " + issueId + " sent " + hours + " hour(s).");
                    }
                    else
                    {
                        reportLog($"Failed to track time. Status code: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    reportLog($"Error occurred: {ex.Message}");
                }
            }
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            Settings settings = new Settings();
            settings.Show();
        }

        private void ВыходToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void ОНасToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About about = new About();
            about.Show();
        }
    }
}