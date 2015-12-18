using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace daash
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Mutex SingleInstanceMutex;
            try
            {
                SingleInstanceMutex = Mutex.OpenExisting("daash");
            }
            catch (Exception)
            {
                SingleInstanceMutex = new Mutex(false, "daash");
            }

            try
            {
                if (SingleInstanceMutex.WaitOne(0, false))
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new MainForm());
                }
                else
                {
                    MessageBox.Show("It is only possible to run one instance of daash", "daash is already running", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                if (SingleInstanceMutex != null)
                {
                    SingleInstanceMutex.Close();
                    SingleInstanceMutex = null;
                }
            }
        }
    }
}
