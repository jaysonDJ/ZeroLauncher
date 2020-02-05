﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using WPFFolderBrowser;

namespace ZeroLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static bool Terminate = false;
        public static bool consoleEnabled = false;
        public static IDZConfig gameConfig = new IDZConfig();
        List<NetworkInterface> networkAdapters = new List<NetworkInterface>();
        public MainWindow()
        {
            InitializeComponent();
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
               networkAdapters.Add(nic);
               comboBoxNetAdapter.Items.Add(nic.Name + " " + nic.GetIPProperties().UnicastAddresses[0].Address.ToString());
            }

            if (File.Exists("idzconfig.bin"))
            {
                gameConfig = ReadFromBinaryFile<IDZConfig>("idzconfig.bin");
                int selAdapter = networkAdapters.FindIndex(x => x.Name == gameConfig.selectedNic);
                comboBoxNetAdapter.SelectedIndex = selAdapter;
                if (gameConfig.JapOrExp)
                {
                    buttonJap.IsChecked = false;
                    buttonExp.IsChecked = true;
                }
                else if (gameConfig.JapOrExp == false)
                {
                    buttonExp.IsChecked = false;
                    buttonJap.IsChecked = true;
                }

                textBoxGameAMFS.Text = gameConfig.AMFSDir;
                if (gameConfig.XOrDInput)
                {
                    buttonDinput.IsChecked = false;
                    buttonXinput.IsChecked = true;
                }
                else if (gameConfig.XOrDInput == false)
                {
                    buttonDinput.IsChecked = true;
                    buttonXinput.IsChecked = false;
                }

                checkBoxIdeal.IsChecked = gameConfig.IdealLan;
                checkBoxDistServ.IsChecked = gameConfig.DistServer;
                // When I implement a online AIME server this will be togglable
                //checkBoxAime.IsChecked = gameConfig.ImitateMe;
            }
            else
            {
                comboBoxNetAdapter.SelectedIndex = 0;
            }

        }

        /// <summary>
        /// Writes the given object instance to a binary file.
        /// <para>Object type (and all child types) must be decorated with the [Serializable] attribute.</para>
        /// <para>To prevent a variable from being serialized, decorate it with the [NonSerialized] attribute; cannot be applied to properties.</para>
        /// </summary>
        /// <typeparam name="T">The type of object being written to the binary file.</typeparam>
        /// <param name="filePath">The file path to write the object instance to.</param>
        /// <param name="objectToWrite">The object instance to write to the binary file.</param>
        /// <param name="append">If false the file will be overwritten if it already exists. If true the contents will be appended to the file.</param>
        public static void WriteToBinaryFile<T>(string filePath, T objectToWrite, bool append = false)
        {
            using (Stream stream = File.Open(filePath, append ? FileMode.Append : FileMode.Create))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                binaryFormatter.Serialize(stream, objectToWrite);
            }
        }

        /// <summary>
        /// Reads an object instance from a binary file.
        /// </summary>
        /// <typeparam name="T">The type of object to read from the binary file.</typeparam>
        /// <param name="filePath">The file path to read the object instance from.</param>
        /// <returns>Returns a new instance of the object read from the binary file.</returns>
        public static T ReadFromBinaryFile<T>(string filePath)
        {
            using (Stream stream = File.Open(filePath, FileMode.Open))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                return (T)binaryFormatter.Deserialize(stream);
            }
        }
        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void ComboBoxNetAdapter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            gameConfig.selectedNic = networkAdapters[comboBoxNetAdapter.SelectedIndex].Name;
        }

        private void configUpdate()
        {
            gameConfig.selectedNic = networkAdapters[comboBoxNetAdapter.SelectedIndex].Name;
            gameConfig.selectedIP = networkAdapters[comboBoxNetAdapter.SelectedIndex].GetIPProperties()
                .UnicastAddresses[0].Address.ToString();
            if ((bool)buttonExp.IsChecked)
            {
                gameConfig.JapOrExp = true;
            }
            else if ((bool)buttonJap.IsChecked)
            {
                gameConfig.JapOrExp = false;
            }

            gameConfig.AMFSDir = textBoxGameAMFS.Text;
            if ((bool)buttonXinput.IsChecked)
            {
                gameConfig.XOrDInput = true;
            }
            else if ((bool)buttonDinput.IsChecked)
            {
                gameConfig.XOrDInput = false;
            }

            gameConfig.IdealLan = (bool)checkBoxIdeal.IsChecked;
            gameConfig.DistServer = (bool)checkBoxDistServ.IsChecked;
            // When I implement a online AIME server this will be togglable
            //checkBoxAime.IsChecked = gameConfig.ImitateMe;
            WriteToBinaryFile("idzconfig.bin", gameConfig, false);
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            configUpdate();
        }

        private void TextBoxGameAMFS_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void TextBoxGameAMFS_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            WPFFolderBrowserDialog folderBrowser = new WPFFolderBrowserDialog();
            if (folderBrowser.ShowDialog() == true)
                textBoxGameAMFS.Text = folderBrowser.FileName;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            configUpdate();
            gameConfig.ExportConfig();
            //MessageBox.Show("bruh im not that far yet lol");
            if (!gameConfig.XOrDInput)
            {
                MessageBox.Show("It appears you're using DirectInput. There is a bug at the moment with ZeroLauncher that means that the controls get overwritten every time you launch the game.\nThis messagebox is here so you can go and edit that textbox BEFORE the game starts, to get your controls working. Refer to README BEFORE USE for more information.");
            }
            gameBoot();
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FreeConsole();

        private void bootAime()
        {
            var psiNpmRunDist = new ProcessStartInfo
            {
                FileName = "cmd",
                RedirectStandardInput = true,
                WorkingDirectory = ".\\deps\\minime"

            };
            //psiNpmRunDist.CreateNoWindow = true;
            psiNpmRunDist.UseShellExecute = false;
            var pNpmRunDist = Process.Start(psiNpmRunDist);
            pNpmRunDist.StandardInput.WriteLine("start.bat");
            pNpmRunDist.WaitForExit();
        }
        private void bootAmdaemon()
        {
            var psiNpmRunDist = new ProcessStartInfo
            {
                FileName = gameConfig.AMFSDir + "\\..\\app\\package\\inject.exe",
                WorkingDirectory = gameConfig.AMFSDir + "\\..\\app\\package\\",
                Arguments = "-d -k .\\idzhook.dll .\\amdaemon.exe -c configDHCP_Final_Common.json configDHCP_Final_JP.json configDHCP_Final_JP_ST1.json configDHCP_Final_JP_ST2.json configDHCP_Final_EX.json configDHCP_Final_EX_ST1.json configDHCP_Final_EX_ST2.json"
            };
            psiNpmRunDist.UseShellExecute = false;
            var pNpmRunDist = Process.Start(psiNpmRunDist);
            pNpmRunDist.WaitForExit();
        }

        private void bootIDZ()
        {
            var psiNpmRunDist = new ProcessStartInfo
            {
                FileName = gameConfig.AMFSDir + "\\..\\app\\package\\inject.exe",
                WorkingDirectory = gameConfig.AMFSDir + "\\..\\app\\package\\",
                Arguments = "-k .\\idzhook.dll .\\InitialD0_DX11_Nu.exe"
            };
            psiNpmRunDist.UseShellExecute = false;
            var pNpmRunDist = Process.Start(psiNpmRunDist);
            pNpmRunDist.WaitForExit();
        }

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleTitle(string lpConsoleTitle);

        private void gameBoot()
        {
            AllocConsole();
            SetConsoleTitle("ZeroLauncher Console Output (contains MiniMe and Amdaemon in one window)");
            /*
             *  IDZ needs a very specific chain of things booted because SEGA
             *  Step 1: MiniMe
             *  Step 2: Locale Emu
             *  Step 3: Amdaemon
             *  Step 4: Game
             *  this gon' be a bitch
             */

            //  Step 1: MiniMe.
            //checking for NodeJS
            ThreadStart ths = null;
            Thread th = null;
            if (isNodeInstalled())
            {
                //passed node check, now to install dependencies
                if (File.Exists("deps\\minime\\start.bat"))
                {
                    ths = new ThreadStart(() => bootAime());
                    th = new Thread(ths);
                    th.Start();
                }
            }
            else
            {
                MessageBox.Show("You don't have Node.js installed! MiniMe requires this! Quitting game.",
                    "Missing dependency", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            //locale emu stuff can go here once i figure out how tf to get it to work
            //  Step 3: Amdaemon
            ThreadStart ths2 = new ThreadStart(() => bootAmdaemon());
            Thread th2 = new Thread(ths2);
            th2.Start();

            //  Step 4: IDZ
            ThreadStart ths3 = new ThreadStart(() => bootIDZ());
            Thread th3 = new Thread(ths3);
            th3.Start();

            ThreadStart ths4 = new ThreadStart(() => monitorGame(th,th2,th3));
            Thread th4 = new Thread(ths4);
            th4.Start();

            buttonLaunch.Visibility = Visibility.Hidden;
            buttonLaunch.IsEnabled = false;
            buttonClose.Visibility = Visibility.Visible;
            buttonClose.IsEnabled = true;
        }

        private void flipTerm()
        {
            Terminate = !Terminate;
            buttonLaunch.Visibility = Visibility.Visible;
            buttonLaunch.IsEnabled = true;
            buttonClose.Visibility = Visibility.Hidden;
            buttonClose.IsEnabled = false;
        }
        public void monitorGame(Thread th, Thread th2, Thread th3)
        {
            while (th2.IsAlive || th3.IsAlive)
            {
                if (Terminate)
                {
                    var currentId = Process.GetCurrentProcess().Id;
                    Regex regex = new Regex(@"amdaemon.*");
                    foreach (Process p in Process.GetProcesses("."))
                    {
                        if (regex.Match(p.ProcessName).Success)
                        {
                            p.Kill();
                            Console.WriteLine("killed amdaemon!");
                        }
                    }
                    regex = new Regex(@"InitialD0.*");
                    foreach (Process p in Process.GetProcesses("."))
                    {
                        if (regex.Match(p.ProcessName).Success)
                        {
                            p.Kill();
                            Console.WriteLine("killed game process!");
                        }
                    }
                    regex = new Regex(@"ServerBoxD8.*");
                    foreach (Process p in Process.GetProcesses("."))
                    {
                        if (regex.Match(p.ProcessName).Success)
                        {
                            p.Kill();
                            Console.WriteLine("killed serverbox!");
                        }
                    }
                    regex = new Regex(@"inject.*");
                    foreach (Process p in Process.GetProcesses("."))
                    {
                        if (regex.Match(p.ProcessName).Success)
                        {
                            p.Kill();
                            Console.WriteLine("killed inject.exe!");
                        }
                    }
                    regex = new Regex(@"node.*");
                    foreach (Process p in Process.GetProcesses("."))
                    {
                        if (regex.Match(p.ProcessName).Success)
                        {
                            p.Kill();
                            Console.WriteLine("killed nodeJS! (if you were running node, you may want to restart it)");
                        }
                    }
                }
            }
            //time to kill node
            Terminate = !Terminate;
            FreeConsole();
            return;
        }
        public bool isNodeInstalled()
        {
            // FUCK NODE
            if (Directory.Exists("C:\\Program Files\\nodejs"))
            {
                if (File.Exists("C:\\Program Files\\nodejs\\npm.cmd"))
                {
                    // fuck you
                    return true;
                }
                else
                {
                    MessageBox.Show("You're missing NPM somehow, google how to install it.");
                    return false;
                }
            }

            return false;
        }

        private void ButtonControls_Click(object sender, RoutedEventArgs e)
        {
            ControllerSettings cs = new ControllerSettings(gameConfig, gameConfig.XOrDInput);
            cs.ShowDialog();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Terminate = true;
        }

        private void buttonClose_Click(object sender, RoutedEventArgs e)
        {
            flipTerm();
        }
    }
}