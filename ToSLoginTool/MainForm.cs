using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using System.Xml.Linq;

namespace ToSLoginTool {
	public partial class MainForm : Form {

		private Hc _hc;

		public MainForm() {
			InitializeComponent();
			AccountManager.Load();
			UpdateListBox();
		}

		/// <summary>
		/// ToSログイン
		/// </summary>
		/// <param name="account"></param>
		/// <returns></returns>
		private async Task Login( Account account) {
			//セッションタイマーストップ
			this.timerUpdateSession.Stop();

			this._hc = new Hc();
			this._hc.RequestHeader.Referrer = new Uri( "http://tos.nexon.co.jp/" );
			var html = await this._hc.Navigate( "https://www.nexon.co.jp/login/" );
			this.tsslStatus.Text = account.Nickname;

			if( !Regex.IsMatch( html, "^.*\\$\\(\"#(i\\d+)\"\\).focus\\(\\);.*$", RegexOptions.Singleline ) || !Regex.IsMatch( html, "^.*name=(\"|')entm(\"|') value=(\"|')(.*?)(\"|').*$", RegexOptions.Singleline ) ) {
				throw new Exception("ログイン失敗");
			}

			var uniqueId = Regex.Replace( html, "^.*\\$\\(\"#(i\\d+)\"\\).focus\\(\\);.*$", "$1", RegexOptions.Singleline );
			var uniquePassword = Regex.Replace( uniqueId, "^i", "p" );
			var entm = Regex.Replace( html, "^.*name=(\"|')entm(\"|') value=(\"|')(.*?)(\"|').*$", "$4", RegexOptions.Singleline );

			var data = new Dictionary<string, string>();

			data.Add( "entm", entm );
			data.Add( uniqueId, account.Id );
			data.Add( uniquePassword, account.Pw );
			data.Add( "onetimepass", "" );
			data.Add( "HiddenUrl", "http://tos.nexon.co.jp/" );
			data.Add( "otp", "" );
			this._hc.RequestHeader.Referrer = new Uri( "https://login.nexon.co.jp/login/?gm=ToS" );
			html = await this._hc.Navigate( "https://login.nexon.co.jp/login/login_process1.aspx", data );

			//ワンタイムパスワードが要求された場合
			if( html.Contains( "window.parent.location.replace(\"https://login.nexon.co.jp/login/otp/\");" ) ) {
				await this._hc.Navigate( "https://login.nexon.co.jp/login/otp/" );
				var otpf = new OneTimePassForm();
				otpf.ShowDialog();
				data = new Dictionary<string, string> {
					{
						"otp", otpf.otp
					}
				};
				await this._hc.Navigate( "https://login.nexon.co.jp/login/login_process2.aspx", data );
			}

			this.tsslStatus.Text = account.Nickname + "ログイン完了";
		}

		/// <summary>
		/// ToS起動
		/// ToSの起動JavaScriptから
		/// http://platform.nexon.co.jp/Auth/NGM/JS/npf_ngm.js
		/// http://platform.nexon.co.jp/Auth/NGM/JS/NGMModuleInfo.js
		/// </summary>
		/// <returns></returns>
		private async Task GameStart( ) {
			await this._hc.Navigate( "http://tos.nexon.co.jp/launcher/game/GameStart.aspx" );
			var cookie = this._hc.Cookies.GetCookies( new Uri( "http://tos.nexon.co.jp" ) ).Cast<Cookie>().FirstOrDefault( x => x.Name == "NPP" );
			var xmldoc = Regex.Replace(await this._hc.Navigate( "http://platform.nexon.co.jp/Auth/NGM/JS/NGMModuleInfo.js" ), "^var NGMModuleInfo = '(.*)';","$1" );
			var doc = XElement.Parse( xmldoc );
			var serverHost = doc.Descendants(  "NGMDll" ).Select( x => x.Attribute( "host" )?.Value ?? "" ).First();
			var ngmDllCrc = doc.Descendants( "NGMDll" ).Select( x => x.Attribute( "crc" )?.Value ?? "" ).First();
			var game = "16818186";
			var passport = cookie?.Value ?? "";
			var arg = "";
			var param = "-dll:" + serverHost + ":" + ngmDllCrc + " -locale:JP -mode:launch" + " -game:" + game + ":0 -token:'" + HttpUtility.UrlDecode(passport) + "' -passarg:'" + arg + "'";
			Process.Start( "ngmj://launch/" + param );
		}


		/// <summary>
		/// アカウントリストボックス更新
		/// </summary>
		private void UpdateListBox() {
			this.lbId.Items.Clear();
			this.lbId.Items.AddRange( AccountManager.Value.Select( account => account.Nickname ).ToArray() );
		}

		#region イベント

		/// <summary>
		/// スタートボタンクリックイベント
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private async void btnStart_Click( object sender, EventArgs e ) {
			if( this.lbId.SelectedIndex < 0 ) {
				return;
			}
			var account = AccountManager.Value.ToArray()[this.lbId.SelectedIndex];
			try {
				await Login( account );
			} catch( Exception ex ) {
				this.tsslStatus.Text = account.Nickname + "ログイン失敗";
				LogOutput( ex.StackTrace + ex.Message );
			}
			try {
				await GameStart();
			} catch( Exception ex ) {
				this.tsslStatus.Text = account.Nickname + "ゲーム起動失敗";
				LogOutput( ex.StackTrace + ex.Message );
			}
			this.btnRestart.Visible = true;
			this.btnRestart.Text = account.Nickname + " Game Start";
			this.timerUpdateSession.Start();
		}

		/// <summary>
		/// リスタートボタンクリックイベント
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private async void btnRestart_Click( object sender, EventArgs e ) {
			await GameStart();
		}


		/// <summary>
		/// アカウント追加ボタンクリックイベント
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void btnAdd_Click( object sender, EventArgs e ) {
			var rf = new RegistAccount();
			rf.ShowDialog();
			UpdateListBox();
		}

		/// <summary>
		/// アカウント削除ボタンクリックイベント
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void btnDelete_Click( object sender, EventArgs e ) {
			if( this.lbId.SelectedIndex < 0 ) {
				return;
			}
			AccountManager.Remove( this.lbId.SelectedIndex);
			UpdateListBox();
		}

		/// <summary>
		/// セッション維持タイマー
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void timerUpdateSession_Tick( object sender, EventArgs e ) {
			var unixEpoch = new DateTime( 1970, 1, 1, 9, 0, 0 );
			var cookie = this._hc.Cookies.GetCookies( new Uri( "http://tos.nexon.co.jp" ) ).Cast<Cookie>().FirstOrDefault( x => x.Name == "NPP" );
			var split = HttpUtility.UrlDecode(cookie?.Value)?.Split(':');
			if( split?.Length > 1 ) {
					var rnd = new Random();
				var callBackSerial = ( ( (DateTime.Now.Ticks - unixEpoch.Ticks ) / 10000 )% 1000000 ) * 100 + rnd.Next(0,100);
				var url = "http://" + split[1] + ".nexon.co.jp/Ajax/Default.aspx?_vb=UpdateSession&_cs="+callBackSerial;
				var t = this._hc.Navigate( url );
			}
		}

		#endregion

		#region ログ出力

		private static void LogOutput( string log ) {
			//そのうち
		}
		private static void LogOutput( Exception e ) {
			//そのうち
		}

		#endregion
	}
}