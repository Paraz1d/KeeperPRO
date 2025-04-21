using System;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace KeeperPRO
{
    public partial class MainWindow : Window
    {
        private string connectionString = "Server=DESKTOP-M0DT52I;Database=ХранительПРО;Trusted_Connection=True;";
        private int currentUserId = -1;

        public MainWindow()
        {
            InitializeComponent();
            MainTabControl.SelectedIndex = 0; // Ensure login tab is shown first
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите логин и пароль");
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT user_id, password_hash FROM users WHERE login = @Login";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Login", login);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string storedHash = reader["password_hash"].ToString();
                            string inputHash = HashPassword(password);

                            if (storedHash == inputHash)
                            {
                                currentUserId = Convert.ToInt32(reader["user_id"]);

                                // Show requests tab
                                RequestsTab.Visibility = Visibility.Visible;
                                LoginTab.Visibility = Visibility.Collapsed;
                                MainTabControl.SelectedItem = RequestsTab;

                                // Load user requests
                                LoadUserRequests();
                            }
                            else
                            {
                                MessageBox.Show("Неверный пароль");
                            }
                        }
                        else
                        {
                            MessageBox.Show("Пользователь не найден");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка входа: {ex.Message}");
            }
        }

        private void ShowRegisterButton_Click(object sender, RoutedEventArgs e)
        {
            RegisterTab.Visibility = Visibility.Visible;
            LoginTab.Visibility = Visibility.Collapsed;
            MainTabControl.SelectedItem = RegisterTab;
        }

        private void BackToLoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoginTab.Visibility = Visibility.Visible;
            RegisterTab.Visibility = Visibility.Collapsed;
            MainTabControl.SelectedItem = LoginTab;
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate all required fields
            if (string.IsNullOrEmpty(FullNameTextBox.Text) ||
                string.IsNullOrEmpty(PhoneTextBox.Text) ||
                string.IsNullOrEmpty(EmailTextBox.Text) ||
                BirthDatePicker.SelectedDate == null ||
                string.IsNullOrEmpty(PassportSeriesTextBox.Text) ||
                string.IsNullOrEmpty(PassportNumberTextBox.Text) ||
                string.IsNullOrEmpty(UsernameTextBox.Text) ||
                RegisterPasswordBox.Password.Length == 0 ||
                ConfirmPasswordBox.Password.Length == 0)
            {
                MessageBox.Show("Заполните все обязательные поля");
                return;
            }

            if (RegisterPasswordBox.Password != ConfirmPasswordBox.Password)
            {
                MessageBox.Show("Пароли не совпадают");
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Check if username already exists
                    string checkQuery = "SELECT COUNT(*) FROM users WHERE login = @Login";
                    SqlCommand checkCommand = new SqlCommand(checkQuery, connection);
                    checkCommand.Parameters.AddWithValue("@Login", UsernameTextBox.Text);
                    int userCount = (int)checkCommand.ExecuteScalar();

                    if (userCount > 0)
                    {
                        MessageBox.Show("Пользователь с таким логином уже существует");
                        return;
                    }

                    // Insert new user
                    string insertQuery = @"INSERT INTO users 
                                (full_name, phone, email, birth_date, passport_series, passport_number, login, password_hash)
                                VALUES 
                                (@FullName, @Phone, @Email, @BirthDate, @PassportSeries, @PassportNumber, @Login, @PasswordHash)";

                    SqlCommand insertCommand = new SqlCommand(insertQuery, connection);

                    insertCommand.Parameters.AddWithValue("@FullName", FullNameTextBox.Text);
                    insertCommand.Parameters.AddWithValue("@Phone", PhoneTextBox.Text);
                    insertCommand.Parameters.AddWithValue("@Email", EmailTextBox.Text);
                    insertCommand.Parameters.AddWithValue("@BirthDate", BirthDatePicker.SelectedDate);
                    insertCommand.Parameters.AddWithValue("@PassportSeries", PassportSeriesTextBox.Text);
                    insertCommand.Parameters.AddWithValue("@PassportNumber", PassportNumberTextBox.Text);
                    insertCommand.Parameters.AddWithValue("@Login", UsernameTextBox.Text);
                    insertCommand.Parameters.AddWithValue("@PasswordHash", HashPassword(RegisterPasswordBox.Password));

                    int rowsAffected = insertCommand.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Регистрация успешна!");
                        BackToLoginButton_Click(null, null);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка регистрации: {ex.Message}");
            }
        }

        private void NewRequestButton_Click(object sender, RoutedEventArgs e)
        {
            NewRequestTab.Visibility = Visibility.Visible;
            RequestsTab.Visibility = Visibility.Collapsed;
            MainTabControl.SelectedItem = NewRequestTab;

            // Load employees for combobox
            LoadEmployees();
        }

        private void SubmitRequestButton_Click(object sender, RoutedEventArgs e)
        {
            if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Укажите даты начала и окончания");
                return;
            }

            if (string.IsNullOrEmpty(PurposeTextBox.Text))
            {
                MessageBox.Show("Укажите цель посещения");
                return;
            }

            if (IsGroupCheckBox.IsChecked == true && EmployeeComboBox.SelectedIndex == -1)
            {
                MessageBox.Show("Для групповой заявки выберите сопровождающего сотрудника");
                return;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"INSERT INTO requests 
                            (user_id, employee_id, start_date, end_date, purpose, status_id, is_group, notes)
                            VALUES 
                            (@UserId, @EmployeeId, @StartDate, @EndDate, @Purpose, 1, @IsGroup, @Notes)";

                    SqlCommand command = new SqlCommand(query, connection);

                    command.Parameters.AddWithValue("@UserId", currentUserId);
                    command.Parameters.AddWithValue("@EmployeeId",
                        IsGroupCheckBox.IsChecked == true && EmployeeComboBox.SelectedValue != null ?
                        EmployeeComboBox.SelectedValue : (object)DBNull.Value);
                    command.Parameters.AddWithValue("@StartDate", StartDatePicker.SelectedDate);
                    command.Parameters.AddWithValue("@EndDate", EndDatePicker.SelectedDate);
                    command.Parameters.AddWithValue("@Purpose", PurposeTextBox.Text);
                    command.Parameters.AddWithValue("@IsGroup", IsGroupCheckBox.IsChecked == true);
                    command.Parameters.AddWithValue("@Notes",
                        string.IsNullOrEmpty(NotesTextBox.Text) ? (object)DBNull.Value : NotesTextBox.Text);

                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Заявка успешно создана!");
                        CancelRequestButton_Click(null, null);
                        LoadUserRequests();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания заявки: {ex.Message}");
            }
        }

        private void CancelRequestButton_Click(object sender, RoutedEventArgs e)
        {
            NewRequestTab.Visibility = Visibility.Collapsed;
            RequestsTab.Visibility = Visibility.Visible;
            MainTabControl.SelectedItem = RequestsTab;

            // Clear form
            StartDatePicker.SelectedDate = DateTime.Today;
            EndDatePicker.SelectedDate = DateTime.Today;
            PurposeTextBox.Text = "";
            NotesTextBox.Text = "";
            IsGroupCheckBox.IsChecked = false;
            EmployeeComboBox.SelectedIndex = -1;
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            currentUserId = -1;
            RequestsTab.Visibility = Visibility.Collapsed;
            NewRequestTab.Visibility = Visibility.Collapsed;
            LoginTab.Visibility = Visibility.Visible;
            MainTabControl.SelectedItem = LoginTab;

            // Clear login fields
            LoginTextBox.Text = "";
            PasswordBox.Password = "";
        }

        private void RefreshRequestsButton_Click(object sender, RoutedEventArgs e)
        {
            LoadUserRequests();
        }

        private void LoadUserRequests()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"SELECT r.request_id, r.start_date, r.end_date, r.purpose, 
                                   s.status_name, r.is_group
                                   FROM requests r
                                   JOIN statuses s ON r.status_id = s.status_id
                                   WHERE r.user_id = @UserId
                                   ORDER BY r.start_date DESC";

                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@UserId", currentUserId);

                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    RequestsDataGrid.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заявок: {ex.Message}");
            }
        }

        private void LoadEmployees()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT employee_id, full_name FROM employees";
                    SqlCommand command = new SqlCommand(query, connection);
                    SqlDataAdapter adapter = new SqlDataAdapter(command);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    EmployeeComboBox.ItemsSource = dt.DefaultView;
                    EmployeeComboBox.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки сотрудников: {ex.Message}");
            }
        }

        private void IsGroupCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            EmployeeLabel.Visibility = Visibility.Visible;
            EmployeeComboBox.Visibility = Visibility.Visible;
        }

        private void IsGroupCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            EmployeeLabel.Visibility = Visibility.Collapsed;
            EmployeeComboBox.Visibility = Visibility.Collapsed;
            EmployeeComboBox.SelectedIndex = -1;
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }
    }
}