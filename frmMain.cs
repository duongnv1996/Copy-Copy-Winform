using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;

using RAD.ClipMon.Win32;
using RAD.Windows;
using FireSharp;
using FireSharp.Interfaces;
using FireSharp.Config;
using System.Collections.Generic;


using Microsoft.Win32;
using System.IO;
using copy.copy.Properties;
using FireSharp.Response;
using System.Threading.Tasks;


namespace RAD.ClipMon
{
	
	public class frmMain : System.Windows.Forms.Form
	{
		#region Clipboard Formats

		string[] formatsAll = new string[] 
		{
			DataFormats.Bitmap,
			DataFormats.CommaSeparatedValue,
			DataFormats.Dib,
			DataFormats.Dif,
			DataFormats.EnhancedMetafile,
			DataFormats.FileDrop,
			DataFormats.Html,
			DataFormats.Locale,
			DataFormats.MetafilePict,
			DataFormats.OemText,
			DataFormats.Palette,
			DataFormats.PenData,
			DataFormats.Riff,
			DataFormats.Rtf,
			DataFormats.Serializable,
			DataFormats.StringFormat,
			DataFormats.SymbolicLink,
			DataFormats.Text,
			DataFormats.Tiff,
			DataFormats.UnicodeText,
			DataFormats.WaveAudio
		};

		string[] formatsAllDesc = new String[] 
		{
			"Bitmap",
			"CommaSeparatedValue",
			"Dib",
			"Dif",
			"EnhancedMetafile",
			"FileDrop",
			"Html",
			"Locale",
			"MetafilePict",
			"OemText",
			"Palette",
			"PenData",
			"Riff",
			"Rtf",
			"Serializable",
			"StringFormat",
			"SymbolicLink",
			"Text",
			"Tiff",
			"UnicodeText",
			"WaveAudio"
		};

		#endregion


		#region Constants



		#endregion


		#region Fields

		private System.ComponentModel.IContainer components;

        private System.Windows.Forms.MainMenu menuMain;
        private System.Windows.Forms.RichTextBox ctlClipboardText;
		private RAD.Windows.NotificationAreaIcon notAreaIcon;

		IntPtr _ClipboardViewerNext;
		Queue _hyperlink = new Queue();

        //firebase
        private  string BasePath = "https://clipboard-copy.firebaseio.com/";
        private const string FirebaseSecret = "";
        private static FirebaseClient _client;

        private bool isFirstTime= true;
        private String oldTextCopied = "";
        private String textFromServer = "";
        private String keyMsg;
        private bool isTimeToCopied;
        private bool isFirtInstall;
        private TextBox txtId;
        private Button btnLogin;
        private PictureBox ptrbox;
        protected ContextMenu cmnuTray;
        private MenuItem itmSystray;
        private MenuItem itmHyperlink;
        private MenuItem itmSep1;
        private MenuItem itmHide;
        private MenuItem itmSep2;
        private MenuItem itmExit;
        private MenuItem itemLogout;
        private RegistryKey reg = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run",true);

		#endregion


		#region Constructors

		public frmMain()
		{
            reg.SetValue("Copy Copy", Application.ExecutablePath.ToString());
			InitializeComponent();
           // var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            isFirtInstall = Settings.Default.firstinstall;
            if (!isFirtInstall) isFirtInstall = true;
            keyMsg = Settings.Default.keymsg;
            notAreaIcon.Visible = true;
            String url = copy.copy.Properties.Settings.Default.url;
            if (!url.Equals("")) {
                BasePath = url;
                goListen();
            }
            ContextMenuBuild();
		}
        
		#endregion


		#region Properties - Public



		#endregion


		#region Methods - Private

		/// <summary>
		/// Register this form as a Clipboard Viewer application
		/// </summary>
        /// 
		private void RegisterClipboardViewer()
		{
			_ClipboardViewerNext = Win32.User32.SetClipboardViewer(this.Handle);
		}
        private static Dictionary<string, string> keyHolder = new Dictionary<string, string>();
        private async void ListenToStream() {
            await _client.OnAsync("", (sender, args, context) => {
                string dataFromFB = args.Data;
                string paths = args.Path;

                // run on UI
                
                //Extracts a Unique ID at each iteration
                string[] uniqueKey = paths.Split('/');
                string key = uniqueKey[uniqueKey.Length - 1];             
                textFromServer = dataFromFB;
               
                    if (!dataFromFB.Equals("") && key.Equals("content") && !dataFromFB.Equals(oldTextCopied)) {
                       
                        try {
                            Invoke((Action)(() => {
                                Clipboard.SetDataObject(dataFromFB, true, 2, 1);
                                // Clipboard.SetText(dataFromFB);
                            }));
                        } catch (System.Exception ex) {
                            MessageBox.Show("Please waiting for initializing...");
                        }
                    }
             
               
                ////Checks if Unique ID already exist or not
                //if (keyHolder.ContainsKey(uniqueKey)) {
                //    keyHolder[uniqueKey] = dataFromFB;
                  
                //} else {
                //    keyHolder.Add(uniqueKey, dataFromFB);
                //    Invoke((Action)(() => { Clipboard.SetText(dataFromFB); }));
                //}
               
             
               

            });

        }
        public string RemoveNameSubstring(string name) {
            int index = name.IndexOf("/content");
            string uniqueKey = (index < 0) ? name : name.Remove(index, "/content".Length);
            return uniqueKey;

        }
		/// <summary>
		/// Remove this form from the Clipboard Viewer list
		/// </summary>
		private void UnregisterClipboardViewer()
		{
			Win32.User32.ChangeClipboardChain(this.Handle, _ClipboardViewerNext);
		}

		private void ContextMenuBuild()
		{
			//
			// Only show the last 10 items
			//
			while (_hyperlink.Count > 10)
			{
				_hyperlink.Dequeue();
			}

			cmnuTray.MenuItems.Clear();

            //foreach (string objLink in _hyperlink)
            //{
            //    cmnuTray.MenuItems.Add(objLink.ToString(), new EventHandler(itmHyperlink_Click));
            //}
			
			cmnuTray.MenuItems.Add(itmHide.Text, new EventHandler(itmHide_Click));
			
			cmnuTray.MenuItems.Add("E&xit", new EventHandler(itmExit_Click));
            cmnuTray.MenuItems.Add("Log Out", new EventHandler(itemLogout_Click));
		}


		/// <summary>
		/// Called when an item is chosen from the menu
		/// </summary>
		/// <param name="pstrLink">The link that was clicked</param>
        //private void OpenLink(string pstrLink)
        //{
        //    try
        //    {
        //        //
        //        // Run the link
        //        //

        //        // TODO needs more work to check for missing files etc
        //        System.Diagnostics.Process.Start(pstrLink);
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(this, ex.ToString(), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        //    }

        //}


        private void pushMsg(String content) {
            Msg msg = new Msg();
            msg.content =content;
            DateTime today = DateTime.Today;
            String timeNow = DateTime.Now.ToString("HH:mm");
            msg.date = today.ToString("dd/MM ") + timeNow;
            _client.PushAsync("", msg);
            
        }
		/// <summary>
		/// Show the clipboard contents in the window 
		/// and show the notification balloon if a link is found
		/// </summary>
		private void GetClipboardData()
		{
			//
			// Data on the clipboard uses the 
			// IDataObject interface
			//
			IDataObject iData = new DataObject();  
			string strText = "clipmon";
            String txtClipboard="";
			try
			{
				iData = Clipboard.GetDataObject();
               txtClipboard= Clipboard.GetText();
               oldTextCopied = txtClipboard;
			}
			catch (System.Runtime.InteropServices.ExternalException externEx)
			{
				// Copying a field definition in Access 2002 causes this sometimes?
				Debug.WriteLine("InteropServices.ExternalException: {0}", externEx.Message);
				return;
			}
			catch(Exception ex)
			{
				MessageBox.Show(ex.ToString());
				return;
			}

            
			// 
			// Get RTF if it is present
			//
			if (iData.GetDataPresent(DataFormats.Rtf))
			{
				//ctlClipboardText.Rtf = (string)iData.GetData(DataFormats.Rtf);
                if (!txtClipboard.Equals(textFromServer)) 
                pushMsg(txtClipboard);
				if(iData.GetDataPresent(DataFormats.Text))
				{
					strText = "RTF";
				}
			}
			else
			{
				// 
				// Get Text if it is present
				//
				if(iData.GetDataPresent(DataFormats.Text))
				{
					//ctlClipboardText.Text = (string)iData.GetData(DataFormats.Text);
                    if (!txtClipboard.Equals(textFromServer)) 
                    pushMsg(txtClipboard);	
					strText = "Text"; 

					Debug.WriteLine((string)iData.GetData(DataFormats.Text));
				}
				else
				{
					//
					// Only show RTF or TEXT
					//
					//ctlClipboardText.Text = "(cannot display this format)";
				}
			}

			notAreaIcon.Tooltip = strText;


		}

		#endregion


		#region Methods - Public

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() 
		{
			Application.Run(new frmMain());
		}


		protected override void WndProc(ref Message m)
		{
			switch ((Win32.Msgs)m.Msg)
			{
			
				case Win32.Msgs.WM_DRAWCLIPBOARD:
					
					Debug.WriteLine("WindowProc DRAWCLIPBOARD: " + m.Msg, "WndProc");
                    if (!isFirstTime) {
                        GetClipboardData();
                    } else {
                        isFirstTime = false;
                    }
			
					Win32.User32.SendMessage(_ClipboardViewerNext, m.Msg, m.WParam, m.LParam);
					break;


				case Win32.Msgs.WM_CHANGECBCHAIN:
					Debug.WriteLine("WM_CHANGECBCHAIN: lParam: " + m.LParam, "WndProc");

				
					if (m.WParam == _ClipboardViewerNext)
					{
						
						_ClipboardViewerNext = m.LParam;
					}
					else
					{
						Win32.User32.SendMessage(_ClipboardViewerNext, m.Msg, m.WParam, m.LParam);
					}
					break;

				default:
					
					base.WndProc(ref m);
					break;

			}

		}

		#endregion


		#region Event Handlers - Menu

		private void itmExit_Click(object sender, EventArgs e)
		{
			this.Close();
		}
        private void itemLogout_Click(object sender, EventArgs e) {
            Settings.Default.url = "";
            //_client.Dispose();
            itmHide_Click(sender, e);
            BasePath = "https://clipboard-copy.firebaseio.com/"; 
            showLogin();
        }
		private void itmHide_Click(object sender, System.EventArgs e)
		{
			this.Visible = (! this.Visible);
			itmHide.Text = this.Visible ? "Hide" : "Show";

			if (this.Visible == true)
			{
				if (this.WindowState == FormWindowState.Minimized)
				{
					this.WindowState = FormWindowState.Normal;
				}
			}
		}

		
		
		private void frmMain_Resize(object sender, System.EventArgs e)
		{
			if ((this.WindowState == FormWindowState.Minimized) && (this.Visible == true))
			{
				// hide when minimised
				this.Visible = false;
				itmHide.Text = "Show";
			}
		}

		#endregion


		#region Event Handlers - Internal

		private void frmMain_Load(object sender, System.EventArgs e)
		{
			RegisterClipboardViewer();
          
            
		}

		private void frmMain_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
            Settings.Default.keymsg = keyMsg;
            Settings.Default.Save();
			UnregisterClipboardViewer();
            
		}

		private void notAreaIcon_BalloonClick(object sender, System.EventArgs e)
		{
			if(_hyperlink.Count == 1)
			{
				string strItem = (string)_hyperlink.ToArray()[0];

				// Only one link so open it
                //OpenLink(strItem);
			}
			else
			{
				notAreaIcon.ContextMenuDisplay();
			}
		}

		#endregion


		#region IDisposable Implementation

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#endregion


		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            this.menuMain = new System.Windows.Forms.MainMenu(this.components);
            this.notAreaIcon = new RAD.Windows.NotificationAreaIcon(this.components);
            this.cmnuTray = new System.Windows.Forms.ContextMenu();
            this.itmHide = new System.Windows.Forms.MenuItem();
            this.itmExit = new System.Windows.Forms.MenuItem();
            this.itemLogout = new System.Windows.Forms.MenuItem();
            this.ctlClipboardText = new System.Windows.Forms.RichTextBox();
            this.txtId = new System.Windows.Forms.TextBox();
            this.btnLogin = new System.Windows.Forms.Button();
            this.ptrbox = new System.Windows.Forms.PictureBox();
            this.itmSystray = new System.Windows.Forms.MenuItem();
            this.itmHyperlink = new System.Windows.Forms.MenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.ptrbox)).BeginInit();
            this.SuspendLayout();
            // 
            // notAreaIcon
            // 
            this.notAreaIcon.ContextMenu = this.cmnuTray;
            this.notAreaIcon.DisplayMenuOnLeftClick = true;
            this.notAreaIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("notAreaIcon.Icon")));
            this.notAreaIcon.Tooltip = "Clip Monitor";
            this.notAreaIcon.Visible = false;
            this.notAreaIcon.BalloonClick += new System.EventHandler(this.notAreaIcon_BalloonClick);
            // 
            // cmnuTray
            // 
            this.cmnuTray.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.itmHide,
            this.itmExit,
            this.itemLogout});
            // 
            // itmHide
            // 
            this.itmHide.Index = 0;
            this.itmHide.Text = "Hide";
            this.itmHide.Click += new System.EventHandler(this.itmHide_Click);
            // 
            // itmExit
            // 
            this.itmExit.Index = 1;
            this.itmExit.MergeOrder = 1000;
            this.itmExit.Text = "E&xit";
            // 
            // itemLogout
            // 
            this.itemLogout.Enabled = false;
            this.itemLogout.Index = 2;
            this.itemLogout.MergeOrder = 1000;
            this.itemLogout.Text = "Log Out";
            this.itemLogout.Visible = false;
            // 
            // ctlClipboardText
            // 
            this.ctlClipboardText.BackColor = System.Drawing.SystemColors.Control;
            this.ctlClipboardText.DetectUrls = false;
            this.ctlClipboardText.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ctlClipboardText.Location = new System.Drawing.Point(0, 0);
            this.ctlClipboardText.Name = "ctlClipboardText";
            this.ctlClipboardText.ReadOnly = true;
            this.ctlClipboardText.Size = new System.Drawing.Size(445, 334);
            this.ctlClipboardText.TabIndex = 0;
            this.ctlClipboardText.Text = "Connected";
            this.ctlClipboardText.Visible = false;
            this.ctlClipboardText.WordWrap = false;
            this.ctlClipboardText.TextChanged += new System.EventHandler(this.ctlClipboardText_TextChanged);
            // 
            // txtId
            // 
            this.txtId.Location = new System.Drawing.Point(85, 175);
            this.txtId.Name = "txtId";
            this.txtId.Size = new System.Drawing.Size(272, 26);
            this.txtId.TabIndex = 1;
            this.txtId.Text = "Enter your code";
            // 
            // btnLogin
            // 
            this.btnLogin.Location = new System.Drawing.Point(148, 228);
            this.btnLogin.Name = "btnLogin";
            this.btnLogin.Size = new System.Drawing.Size(156, 52);
            this.btnLogin.TabIndex = 2;
            this.btnLogin.Text = "Connect";
            this.btnLogin.UseVisualStyleBackColor = true;
            this.btnLogin.Click += new System.EventHandler(this.btnLogin_Click);
            // 
            // ptrbox
            // 
            this.ptrbox.Image = global::copy.copy.Properties.Resources.ic_connect;
            this.ptrbox.Location = new System.Drawing.Point(157, 23);
            this.ptrbox.Name = "ptrbox";
            this.ptrbox.Size = new System.Drawing.Size(134, 126);
            this.ptrbox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.ptrbox.TabIndex = 3;
            this.ptrbox.TabStop = false;
            // 
            // itmSystray
            // 
            this.itmSystray.Index = -1;
            this.itmSystray.Text = "";
            // 
            // itmHyperlink
            // 
            this.itmHyperlink.Index = -1;
            this.itmHyperlink.Text = "";
            // 
            // frmMain
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(7, 19);
            this.ClientSize = new System.Drawing.Size(445, 334);
            this.Controls.Add(this.ptrbox);
            this.Controls.Add(this.btnLogin);
            this.Controls.Add(this.txtId);
            this.Controls.Add(this.ctlClipboardText);
            this.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Location = new System.Drawing.Point(100, 100);
            this.Menu = this.menuMain;
            this.Name = "frmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Copy Copy";
            this.Closing += new System.ComponentModel.CancelEventHandler(this.frmMain_Closing);
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.Resize += new System.EventHandler(this.frmMain_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.ptrbox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

        private void ctlClipboardText_TextChanged(object sender, EventArgs e) {

        }
        private void showLogin() {
            ctlClipboardText.Visible = false;
            txtId.Visible = true;
            btnLogin.Visible = true;
            ptrbox.Visible = true;

        }
        private void hideLogin() {
            ctlClipboardText.Visible = true;
            txtId.Visible = false;
            btnLogin.Visible = false;
            ptrbox.Visible = false;
        }
        private  async Task    goListen() {
            hideLogin();
            IFirebaseConfig config = new FirebaseConfig {
                AuthSecret = FirebaseSecret,
                BasePath = BasePath
            };
            _client = new FirebaseClient(config);
            this.itemLogout.Visible = true;
             ListenToStream();
           
        }

        private  void btnLogin_Click(object sender, EventArgs e) {
            String idUrl = txtId.Text;
            
            if (!idUrl.Equals("") ) {
                verify(idUrl);
            }
        }
        private async Task verify(String email) {

            IFirebaseConfig configReg = new FirebaseConfig {
                AuthSecret = FirebaseSecret,
                BasePath = "https://clipboard-copy.firebaseio.com/" + "users/" + email
            };
            FirebaseClient _clientReg = new FirebaseClient(configReg);
            FirebaseResponse response = await _clientReg.GetAsync("todo");
            if (response.Body.ToString().Equals("\"1\"")) {
                BasePath = BasePath + "users/" + email;
                copy.copy.Properties.Settings.Default.url = BasePath;
                copy.copy.Properties.Settings.Default.Save();
                Settings.Default.Save();
                goListen();
            } else {
                MessageBox.Show("Your code was not exist!");
            }
        }

      


		

	}
}
