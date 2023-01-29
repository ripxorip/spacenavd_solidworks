using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
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
    [DisplayName("Spacenavd SW Dotnet")]
    [Description("Spacenavd dotnet solidworks plugin")]
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

        [Flags]
        public enum MouseEventFlags
        {
            LeftDown = 0x00000002,
            LeftUp = 0x00000004,
            MiddleDown = 0x00000020,
            MiddleUp = 0x00000040,
            Move = 0x00000001,
            Absolute = 0x00008000,
            RightDown = 0x00000008,
            RightUp = 0x00000010
        }

        [DllImport("user32.dll", EntryPoint = "SetCursorPos")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out MousePoint lpMousePoint);

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        public static void SetCursorPosition(int x, int y)
        {
            SetCursorPos(x, y);
        }

        public static void SetCursorPosition(MousePoint point)
        {
            SetCursorPos(point.X, point.Y);
        }

        public static MousePoint GetCursorPosition()
        {
            MousePoint currentMousePoint;
            var gotPoint = GetCursorPos(out currentMousePoint);
            if (!gotPoint) { currentMousePoint = new MousePoint(0, 0); }
            return currentMousePoint;
        }

        public static void MouseEvent(MouseEventFlags value)
        {
            MousePoint position = GetCursorPosition();

            mouse_event
                ((int)value,
                 position.X,
                 position.Y,
                 0,
                 0)
                ;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MousePoint
        {
            public int X;
            public int Y;

            public MousePoint(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        private SldWorks m_App;
        private ModelView mw;
        private Thread t1;

        private Mouse deadmouse;

        private double x;
        private double y;
        private double z;
        private double rx;
        private double ry;
        private double rz;

        private double move_coef = 0.00004;
        private double rot_coef = 0.00030;
        private double tilt_coef = 0.0014;
        private double zoom_coef = 0.000000005;

        void poll_thread()
        {
            var server = "10.0.0.230";
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

                    x = res_array[1];
                    y = res_array[2];
                    z = res_array[3];
                    rx = res_array[4];
                    ry = res_array[5];
                    rz = res_array[6];

                    MouseEvent(MouseEventFlags.LeftUp);
                }
            }
        }

        public bool ConnectToSW(object ThisSW, int Cookie)
        {
            m_App = ThisSW as SldWorks;
            m_App.ActiveDocChangeNotify += M_App_ActiveDocChangeNotify;

            t1 = new Thread(poll_thread);
            t1.Start();

            return true;
        }

        private int M_App_ActiveDocChangeNotify()
        {
            ModelDoc2 doc = m_App.ActiveDoc as ModelDoc2;
            mw = doc.ActiveView;
            mw.ViewChangeNotify += Mw_ViewChangeNotify;
            deadmouse = mw.GetMouse();
            deadmouse.MouseNotify += Deadmouse_MouseNotify;
            return 0;
        }

        private int Deadmouse_MouseNotify(int Message, int WParam, int LParam)
        {
            if (Message == 4)
            {
                /*
                MathTransform trans = mw.Orientation3;
                MathUtility mu = m_App.GetMathUtility();

                double[] data = { 0.00, 0.00, 0.00 };

                MathVector x = mu.ICreateVector(ref data[0]);
                MathVector y = mu.ICreateVector(ref data[0]);
                MathVector z = mu.ICreateVector(ref data[0]);
                MathVector transl = mu.ICreateVector(ref data[0]);
                double scale = 0.00;

                trans.IGetData2(ref x, ref y, ref z, ref transl, ref scale);
                scale += 0.3;
                trans.ISetData(x, y, z, transl, scale);

                mw.Orientation3 = trans;
                mw.GraphicsRedraw(null);
                */
                /* The below seems to work fine :) */
                mw.TranslateBy(x * move_coef, y * move_coef);
                mw.RotateAboutCenter(rx * rot_coef, ry * rot_coef);
                mw.RollBy(-rz * rot_coef);
            }
            return 0;
        }

        private int Mw_ViewChangeNotify(object View)
        {
            return 0;
        }

        public bool DisconnectFromSW()
        {
            return true;
        }
    }
}
