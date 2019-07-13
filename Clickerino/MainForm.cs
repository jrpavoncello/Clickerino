using Gma.UserActivityMonitor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Clickerino
{
    public partial class MainForm : Form
    {
        //Delay between mouse down -> mouse up
        int mouseUpMean = 120;
        int mouseUpStdDev = 9;
        int mouseUpMin = 80;
        int mouseUpMax = 180;

        // Delay between mouse up -> mouse down
        int mouseDownMean = 160;
        int mouseDownStdDev = 12;
        int mouseDownMin = 100;
        int mouseDownMax = 220;
        
        Thread thread = null;

        public delegate void writeLineDelegate(string message);
        public writeLineDelegate writeDelegate;

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
        //Mouse actions
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;

        private const Keys STARTKEY = Keys.F2;

        static bool allowStart = false;
        static bool manualClick = true;

        static Semaphore manualClickSem = new Semaphore(0, 1);

        public MainForm()
        {
            writeDelegate = new writeLineDelegate(writeLine);
            HookManager.MouseUp += HookManager_MouseUp;
            HookManager.KeyDown += HookManager_KeyDown;

            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            writeLine($"Press {STARTKEY.ToString()} to prime Clickerino.");
        }

        private void HookManager_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                bool isManualClick = manualClick;

                if (!isManualClick)
                {
                    manualClick = true;
                    manualClickSem.Release();
                }

                if (isManualClick)
                {
                    if (allowStart && thread == null)
                    {
                        writeLine("Starting the clicker.");
                        thread = new Thread(new ThreadStart(startClicking));
                        thread.Start();
                    }
                    else
                    {
                        if (thread != null)
                        {
                            writeLine("Stopping the clicker.");
                            thread.Abort();
                            thread = null;
                            writeLine($"Press {STARTKEY.ToString()} to prime Clickerino.");
                            allowStart = false;
                        }
                    }
                }
            }
        }

        private void HookManager_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == STARTKEY)
            {
                writeLine("Clickerino is primed and ready. Left Click to begin.");
                allowStart = true;
            }
        }

        void startClicking()
        {
            Random randomGen = new Random();

            int nextBreak = 0;
            DateTime workStartTime = DateTime.Now;

            while (true)
            {
                if(nextBreak == 0)
                {
                    //nextBreak = randomGen.Next(120, 420);
                    nextBreak = randomGen.Next(15, 90);
                }

                int secondsWorking = (DateTime.Now - workStartTime).Seconds;

                if (secondsWorking >= nextBreak)
                {
                    int breakSeconds = randomGen.Next(500, 2000);

                    // Take a break
                    writeLine($"{secondsWorking} seconds have passed since last break!");
                    writeLine($"Taking a {breakSeconds} millisecond break!");

                    Thread.Sleep(breakSeconds);

                    nextBreak = 0;
                    workStartTime = DateTime.Now;
                }

                int rand = Clamp((int)generateRandom(randomGen, mouseDownMean, mouseDownStdDev), mouseDownMin, mouseDownMax);

                writeLine(rand.ToString());

                // Just in case
                if (rand < 0 || rand > 500)
                {
                    Environment.FailFast("Bad random value");
                }

                Thread.Sleep(rand);

                //Call the imported function with the cursor's current position
                uint X = (uint)Cursor.Position.X;
                uint Y = (uint)Cursor.Position.Y;

                mouse_event(MOUSEEVENTF_LEFTDOWN, X, Y, 0, 0);

                rand = Clamp((int)generateRandom(randomGen, mouseUpMean, mouseUpStdDev), mouseUpMin, mouseUpMax);

                writeLine(rand.ToString());

                // Just in case
                if (rand < 0 || rand > 500)
                {
                    Environment.FailFast("Bad random value");
                }

                Thread.Sleep(rand);
                
                manualClick = false;

                mouse_event(MOUSEEVENTF_LEFTUP, X, Y, 0, 0);

                manualClickSem.WaitOne();
            }
        }

        public int Clamp(int value, int min, int max)
        {
            if(value < min)
            {
                return min;
            }
            else if(value > max)
            {
                return max;
            }

            return value;
        }

        public void writeLine(string message)
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(writeDelegate, message);
                }
                else
                {
                    this.textBox1.AppendText(message + System.Environment.NewLine);
                }
            }
            catch
            {
                // Swallow it
            }
        }

        public double generateRandom(Random rand, double mean, double stdDev)
        {
            double u1 = 1.0 - rand.NextDouble(); //uniform(0,1] random doubles
            double u2 = 1.0 - rand.NextDouble();

            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                         Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
            
            return mean + stdDev * randStdNormal; //random normal(mean,stdDev^2)
        }
    }
}
