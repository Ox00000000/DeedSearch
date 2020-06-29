using MahApps.Metro.Controls;
using Serilog;
using System.Windows;

namespace DeedSearch
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : MetroWindow
    {
        private bool lastLoginFailed;

        public Login()
        {
            InitializeComponent();

            lastLoginFailed = false;
            this.Username.Focus();

            if (!string.IsNullOrWhiteSpace(DataStore.Instance.Username))
            {
                this.Username.Text = DataStore.Instance.Username;
            }
            if (!string.IsNullOrWhiteSpace(DataStore.Instance.Password))
            {
                this.Password.Password = DataStore.Instance.Password;
            }
            // Check for automatic login
            if (!string.IsNullOrWhiteSpace(this.Username.Text) ||
                !string.IsNullOrWhiteSpace(this.Password.Password))
            {
                //this.Login_Click(this.LoginButton, null);
            }
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            this.Error.Text = "";
            this.toggleUi(false);

            if (string.IsNullOrWhiteSpace(this.Username.Text) ||
                string.IsNullOrWhiteSpace(this.Password.Password))
            {
                this.Error.Text = "Username and password required";
                this.toggleUi(true);
            }
            else
            {
                Log.Information($"Login as {this.Username.Text}");
                string response = await Search.LoginAsync(this.Username.Text, this.Password.Password, this.lastLoginFailed);
                GSCCCAPage page = new GSCCCAPage(response);

                if (page.IsLoginRequired())
                {
                    Log.Information("Login failed");
                    // Login failed
                    this.Error.Text = "Login failed, please try again";

                    this.lastLoginFailed = true;
                    this.toggleUi(true);
                }
                else
                {
                    Log.Information("Login succeeded");

                    if (this.SaveUserPass.IsChecked.HasValue && this.SaveUserPass.IsChecked.Value)
                    {
                        DataStore.Instance.Username = this.Username.Text;
                        DataStore.Instance.Password = this.Password.Password;
                    }

                    this.Close();
                }
            }
        }

        private void toggleUi(bool enabled)
        {
            this.Username.IsEnabled = enabled;
            this.Password.IsEnabled = enabled;
            this.LoginButton.IsEnabled = enabled;
        }
    }
}
