using System;
using System.Windows.Forms;

namespace ToSLoginTool {
	public partial class OneTimePassForm :Form {
		public OneTimePassForm() {
			InitializeComponent();
		}

		public string otp;
		private void txtOtp_KeyPress( object sender, KeyPressEventArgs e ) {
			if( e.KeyChar == (char)Keys.Enter ) {
				Ok();
			}
		}

		private void Ok() {
			this.otp = this.txtOtp.Text;
			Close();
		}

		private void button1_Click( object sender, EventArgs e ) {
			Ok();
		}
	}
}
