using System;
using System.Windows.Forms;

namespace ToSLoginTool {
	public partial class RegistAccount :Form {
		public RegistAccount() {
			InitializeComponent();
		}

		private void btnOk_Click( object sender, EventArgs e ) {
			if( this.txtId.Text == "" || this.txtPw.Text == "" ) {
				MessageBox.Show("IDまたはパスワードが入力されていません。");
				return;
			}
			AccountManager.Add( new Account( this.txtNickname.Text, this.txtId.Text, this.txtPw.Text ) );
			Close();
		}

		private void btnCancel_Click( object sender, EventArgs e ) {
			Close();
		}
	}
}
