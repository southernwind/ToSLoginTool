using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace ToSLoginTool {
	static class AccountManager {

		private const string FILE_NAME = "./op.cnf";
		private static List<Account> _value;
		/// <summary>
		/// アカウント配列
		/// </summary>
		internal static IEnumerable<Account> Value {
			get {
				return _value;
			}
		}

		/// <summary>
		/// アカウント追加
		/// </summary>
		/// <param name="account">アカウント情報</param>
		internal static void Add( Account account ) {
			_value.Add(account);
			Save();
		}

		/// <summary>
		/// アカウント削除
		/// </summary>
		/// <param name="index">アカウント配列インデックス</param>
		internal static void Remove( int index ) {
			_value.RemoveAt(index);
			Save();
		}

		/// <summary>
		/// アカウントファイル読み込み
		/// </summary>
		internal static void Load() {
			if( _value == null ) {
				_value = new List<Account>();
			}
			if( System.IO.File.Exists( FILE_NAME ) ) {
				var serializer2 = new System.Xml.Serialization.XmlSerializer( typeof( List<Account> ) );
				var sr = new System.IO.StreamReader( FILE_NAME, new UTF8Encoding( false ) );
				try {
					_value = (List<Account>)serializer2.Deserialize( sr );
				} catch( InvalidOperationException ) {
					MessageBox.Show( "op.cnfが壊れていて設定を読み込めませんでした。" );
				} finally {
					sr.Close();
				}
			}
		}

		/// <summary>
		/// アカウントファイル保存
		/// </summary>
		private static void Save() {
			var serializer1 = new System.Xml.Serialization.XmlSerializer( typeof( List<Account> ) );
			var sw = new System.IO.StreamWriter( FILE_NAME, false, new UTF8Encoding( false ) );
			serializer1.Serialize( sw, _value );
			sw.Close();
		}
	}
	public class Account {

		//privateだとserializeされないのでとりあえずpublic
		public string nickname;
		public string id;
		public string pw;

		/// <summary>
		/// 管理名
		/// </summary>
		public string Nickname {
			get {
				if( this.nickname == "" ) {
					return this.id;
				}
				return this.nickname;
			}
		}

		/// <summary>
		/// ID
		/// </summary>
		public string Id {
			get {
				return this.id;
			}
		}

		/// <summary>
		/// パスワード
		/// </summary>
		public string Pw {
			get {
				return Cypher.DecryptString(this.pw);
			}
		}
		public Account() : this( "", "", "" ) {
		}

		public Account( string nickname, string id, string pw) {
			this.nickname = nickname;
			this.id = id;
			this.pw = Cypher.EncryptString( pw );
		}
	}
}
