using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swpublished;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

namespace SpacenavdSw
{
    [ComVisible(true)]
    [Guid("31B803E0-7A01-4841-A0DE-895B726625C9")]
    [DisplayName("Spacenavd SW")]
    [Description("Spacenavd solidworks plugin")]
    public class SpacenavdSw : ISwAddin
    {
        #region Registration

        private const string ADDIN_KEY_TEMPLATE = @"SOFTWARE\SolidWorks\Addins\{{{0}}}";
        private const string ADDIN_STARTUP_KEY_TEMPLATE = @"Software\SolidWorks\AddInsStartup\{{{0}}}";
        private const string ADD_IN_TITLE_REG_KEY_NAME = "Title";
        private const string ADD_IN_DESCRIPTION_REG_KEY_NAME = "Description";

        [ComRegisterFunction]
        public static void RegisterFunction(Type t)
        {
            try
            {
                var addInTitle = "";
                var loadAtStartup = true;
                var addInDesc = "";

                var dispNameAtt = t.GetCustomAttributes(false).OfType<DisplayNameAttribute>().FirstOrDefault();

                if (dispNameAtt != null)
                {
                    addInTitle = dispNameAtt.DisplayName;
                }
                else
                {
                    addInTitle = t.ToString();
                }

                var descAtt = t.GetCustomAttributes(false).OfType<DescriptionAttribute>().FirstOrDefault();

                if (descAtt != null)
                {
                    addInDesc = descAtt.Description;
                }
                else
                {
                    addInDesc = t.ToString();
                }

                var addInkey = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(
                    string.Format(ADDIN_KEY_TEMPLATE, t.GUID));

                addInkey.SetValue(null, 0);

                addInkey.SetValue(ADD_IN_TITLE_REG_KEY_NAME, addInTitle);
                addInkey.SetValue(ADD_IN_DESCRIPTION_REG_KEY_NAME, addInDesc);

                var addInStartupkey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(
                    string.Format(ADDIN_STARTUP_KEY_TEMPLATE, t.GUID));

                addInStartupkey.SetValue(null, Convert.ToInt32(loadAtStartup), Microsoft.Win32.RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {

                Console.WriteLine("Error while registering the addin: " + ex.Message);
            }
        }

        [ComUnregisterFunction]
        public static void UnregisterFunction(Type t)
        {
            try
            {
                Microsoft.Win32.Registry.LocalMachine.DeleteSubKey(
                    string.Format(ADDIN_KEY_TEMPLATE, t.GUID));

                Microsoft.Win32.Registry.CurrentUser.DeleteSubKey(
                    string.Format(ADDIN_STARTUP_KEY_TEMPLATE, t.GUID));
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while unregistering the addin: " + e.Message);
            }
        }

        #endregion

        private ISldWorks m_App;
        private Thread t1;

        void test()
        {
            ModelDoc2 swModel = default(ModelDoc2);
            ModelViewManager swModelViewManager = default(ModelViewManager);
            ModelView swModelView = default(ModelView);
            swModel = (ModelDoc2)m_App.ActiveDoc;

            swModelView = (ModelView)swModel.ActiveView;
            swModelView.TranslateBy(-0.0005535897435897, 0);
        }

        void poll_thread()
        {
            var server = "192.168.1.52";
            Int32 port = 11111;
            using (TcpClient client = new TcpClient(server, port))
            {

                NetworkStream stream = client.GetStream();
                while (true)
                {
                    var data = new Byte[256];
                    var res = stream.Read(data, 0, 32);
                    var res_array = new Int32[32 / 4];
                    for (int i = 0; i < 32 / 4; i++)
                    {
                        int num = BitConverter.ToInt32(data, i * 4);
                        res_array[i] = num;
                    }

                    for (int i = 0; i < 32 / 4; i++)
                    {
                        Debug.WriteLine(res_array[i]);
                    }
                    Debug.WriteLine("*****");
                    ModelDoc2 swModel = default(ModelDoc2);
                    ModelViewManager swModelViewManager = default(ModelViewManager);
                    ModelView swModelView = default(ModelView);
                    swModel = (ModelDoc2)m_App.ActiveDoc;

                    swModelView = (ModelView)swModel.ActiveView;
                    swModelView.TranslateBy(20e-6 * res_array[1], 20e-6 * res_array[2]);
                }
            }
        }

        public bool ConnectToSW(object ThisSW, int Cookie)
        {
            m_App = ThisSW as ISldWorks;

            Debug.WriteLine("Hello from the spacenavd_sw plugin");
            t1 = new Thread(poll_thread);
            t1.Start();

            return true;
        }

        public bool DisconnectFromSW()
        {
            return true;
        }
    }
}
