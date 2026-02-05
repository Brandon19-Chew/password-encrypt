using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace FileEncryptionSystem
{
    public partial class Form1 : Form
    {
        private string selectedFilePath;
        private string selectedFolderPath;
        private string currentPassword;
        private const string PasswordHashFile = "password.hash";
        private const string SaltFile = "password.salt";
        private const string FileExtension = ".encrypted";

        public Form1()
        {
            InitializeComponent();
            CheckPasswordStatus();
        }

        private void CheckPasswordStatus()
        {
            bool hasPassword = File.Exists(PasswordHashFile) && File.Exists(SaltFile);

            if (hasPassword)
            {
                lblStatus.Text = "Password is set. Please enter your password for security.";
                lblStatus.ForeColor = Color.Green;
                EnableButtons(false);
            }
            else
            {
                lblStatus.Text = "No password set. Please create a password first.";
                lblStatus.ForeColor = Color.OrangeRed;
                EnableButtons(false);
            }
        }

        private void TxtPassword_TextChanged(object sender, EventArgs e)
        {
            currentPassword = txtPassword.Text;

            bool hasPassword = File.Exists(PasswordHashFile) && File.Exists(SaltFile);

            if (hasPassword && !string.IsNullOrEmpty(currentPassword))
            {
                // Verify password
                if (EncryptionHelper.VerifyPassword(currentPassword))
                {
                    lblStatus.Text = "✓ Password verified. Ready for encryption/decryption.";
                    lblStatus.ForeColor = Color.Green;
                    UpdateEncryptionButtons();
                }
                else
                {
                    lblStatus.Text = "✗ Incorrect password. Try again.";
                    lblStatus.ForeColor = Color.Red;
                    EnableButtons(false);
                }
            }
            else if (!hasPassword && !string.IsNullOrEmpty(currentPassword))
            {
                lblStatus.Text = "Ready to set new password.";
                lblStatus.ForeColor = Color.Blue;
                EnableButtons(false);
            }
            else
            {
                EnableButtons(false);
            }
        }

        private void UpdateEncryptionButtons()
        {
            bool canEncrypt = false;
            bool canDecrypt = false;

            if (!string.IsNullOrEmpty(currentPassword) && EncryptionHelper.VerifyPassword(currentPassword))
            {
                if (!string.IsNullOrEmpty(selectedFilePath))
                {
                    // Check if file is already encrypted
                    bool isEncrypted = selectedFilePath.EndsWith(FileExtension);

                    if (isEncrypted)
                    {
                        // For encrypted files: can only decrypt
                        canDecrypt = true;
                        canEncrypt = false;

                        // Disable ONLY the Set Password button for encrypted files
                        btnSetPassword.Enabled = false;
                        btnSetPassword.Text = "Password Locked";
                        btnSetPassword.BackColor = Color.LightGray;

                        // Keep password textbox enabled for security verification
                        txtPassword.Enabled = true;

                        lblStatus.Text = "File is encrypted. Enter password to decrypt.";
                        lblStatus.ForeColor = Color.Purple;
                    }
                    else
                    {
                        // For normal files: can only encrypt
                        canEncrypt = true;
                        canDecrypt = false;

                        // Enable Set Password button for normal files
                        btnSetPassword.Enabled = true;
                        btnSetPassword.Text = "🔑 Set Password";
                        btnSetPassword.BackColor = Color.Gold;

                        lblStatus.Text = "File is not encrypted. Ready to encrypt.";
                        lblStatus.ForeColor = Color.Green;
                    }
                }
                else if (!string.IsNullOrEmpty(selectedFolderPath))
                {
                    // For folders: can both encrypt and decrypt
                    canEncrypt = true;
                    canDecrypt = true;

                    // Enable Set Password button for folders
                    btnSetPassword.Enabled = true;
                    btnSetPassword.Text = "🔑 Set Password";
                    btnSetPassword.BackColor = Color.Gold;

                    lblStatus.Text = "Folder selected. Ready to encrypt/decrypt.";
                    lblStatus.ForeColor = Color.Green;
                }
            }
            else
            {
                // Reset Set Password button state
                bool hasPassword = File.Exists(PasswordHashFile) && File.Exists(SaltFile);
                if (!hasPassword)
                {
                    btnSetPassword.Enabled = true;
                    btnSetPassword.Text = "🔑 Set Password";
                    btnSetPassword.BackColor = Color.Gold;
                }
            }

            btnEncrypt.Enabled = canEncrypt;
            btnDecrypt.Enabled = canDecrypt;

            // Update button colors based on state
            if (btnEncrypt.Enabled)
                btnEncrypt.BackColor = Color.LightGreen;
            else
                btnEncrypt.BackColor = Color.LightGray;

            if (btnDecrypt.Enabled)
                btnDecrypt.BackColor = Color.LightCoral;
            else
                btnDecrypt.BackColor = Color.LightGray;
        }

        private void BtnSetPassword_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtPassword.Text))
            {
                MessageBox.Show("Please enter a password.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (txtPassword.Text.Length < 6)
            {
                MessageBox.Show("Password must be at least 6 characters long.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                EncryptionHelper.SetPassword(txtPassword.Text);
                lblStatus.Text = "✓ Password set successfully!";
                lblStatus.ForeColor = Color.Green;
                txtPassword.Clear();
                UpdateEncryptionButtons();
                MessageBox.Show("Password has been set successfully!\n\nRemember this password - it cannot be recovered!",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error setting password: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSelectFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "All files (*.*)|*.*";
                openFileDialog.Title = "Select a file to encrypt/decrypt";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    selectedFilePath = openFileDialog.FileName;
                    selectedFolderPath = null;

                    // Check if file is encrypted
                    bool isEncrypted = selectedFilePath.EndsWith(FileExtension);

                    if (isEncrypted)
                    {
                        lblFileInfo.Text = $"🔒 ENCRYPTED: {Path.GetFileName(selectedFilePath)}";
                        lblFileInfo.ForeColor = Color.Red;
                        lblFileInfo.Font = new Font(lblFileInfo.Font, FontStyle.Bold);

                        // Clear password field for security
                        txtPassword.Clear();
                        lblStatus.Text = "Enter password to verify identity and decrypt";
                        lblStatus.ForeColor = Color.DarkRed;

                        // Auto-disable encrypt button for encrypted files
                        btnEncrypt.Enabled = false;
                        btnEncrypt.BackColor = Color.LightGray;
                    }
                    else
                    {
                        lblFileInfo.Text = $"📄 File: {Path.GetFileName(selectedFilePath)}";
                        lblFileInfo.ForeColor = Color.Blue;
                        lblFileInfo.Font = new Font(lblFileInfo.Font, FontStyle.Regular);
                    }

                    UpdateEncryptionButtons();
                }
            }
        }

        private void BtnSelectFolder_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select a folder to encrypt/decrypt";

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    selectedFolderPath = folderDialog.SelectedPath;
                    selectedFilePath = null;
                    lblFileInfo.Text = $"📁 Folder: {Path.GetFileName(selectedFolderPath)}";
                    lblFileInfo.ForeColor = Color.DarkGreen;

                    // Check if folder contains any encrypted files
                    bool hasEncryptedFiles = Directory.GetFiles(selectedFolderPath, "*" + FileExtension, SearchOption.AllDirectories).Length > 0;

                    if (hasEncryptedFiles)
                    {
                        // Auto-disable encrypt button if folder contains encrypted files
                        btnEncrypt.Enabled = false;
                        btnEncrypt.BackColor = Color.LightGray;
                        lblStatus.Text = "Folder contains encrypted files. Decrypt them first before encrypting.";
                        lblStatus.ForeColor = Color.Red;
                    }
                    else
                    {
                        lblStatus.Text = "Folder selected. Ready to encrypt/decrypt.";
                        lblStatus.ForeColor = Color.Green;
                    }

                    UpdateEncryptionButtons();
                }
            }
        }

        private void BtnEncrypt_Click(object sender, EventArgs e)
        {
            if (!EncryptionHelper.VerifyPassword(currentPassword))
            {
                MessageBox.Show("Invalid password! Please enter the correct password.", "Security Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                if (!string.IsNullOrEmpty(selectedFilePath))
                {
                    // Double-check file is not already encrypted
                    if (selectedFilePath.EndsWith(FileExtension))
                    {
                        MessageBox.Show("This file is already encrypted!", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Security confirmation
                    DialogResult confirm = MessageBox.Show(
                        $"Are you sure you want to encrypt:\n{Path.GetFileName(selectedFilePath)}\n\n" +
                        $"Password will be required to decrypt this file.",
                        "Confirm Encryption",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (confirm == DialogResult.Yes)
                    {
                        EncryptionHelper.EncryptFile(selectedFilePath, currentPassword);

                        // Update file info after encryption
                        selectedFilePath = selectedFilePath + FileExtension;
                        lblFileInfo.Text = $"🔒 ENCRYPTED: {Path.GetFileName(selectedFilePath)}";
                        lblFileInfo.ForeColor = Color.Red;

                        // Disable Set Password button after encryption
                        btnSetPassword.Enabled = false;
                        btnSetPassword.Text = "Password Locked";
                        btnSetPassword.BackColor = Color.LightGray;

                        UpdateEncryptionButtons();
                    }
                }
                else if (!string.IsNullOrEmpty(selectedFolderPath))
                {
                    DialogResult confirm = MessageBox.Show(
                        $"Are you sure you want to encrypt ALL files in:\n{selectedFolderPath}\n\n" +
                        $"Password will be required to decrypt these files.",
                        "Confirm Folder Encryption",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (confirm == DialogResult.Yes)
                    {
                        EncryptionHelper.EncryptFolder(selectedFolderPath, currentPassword);
                    }
                }
                else
                {
                    MessageBox.Show("Please select a file or folder first.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Encryption failed: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDecrypt_Click(object sender, EventArgs e)
        {
            if (!EncryptionHelper.VerifyPassword(currentPassword))
            {
                MessageBox.Show("Invalid password! Access denied.", "Security Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                if (!string.IsNullOrEmpty(selectedFilePath))
                {
                    // Double-check file is encrypted
                    if (!selectedFilePath.EndsWith(FileExtension))
                    {
                        MessageBox.Show("This file is not encrypted!", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Security confirmation
                    DialogResult confirm = MessageBox.Show(
                        $"Are you sure you want to decrypt:\n{Path.GetFileName(selectedFilePath)}",
                        "Confirm Decryption",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (confirm == DialogResult.Yes)
                    {
                        EncryptionHelper.DecryptFile(selectedFilePath, currentPassword);

                        // Update file info after decryption
                        string originalName = selectedFilePath.Substring(0, selectedFilePath.Length - FileExtension.Length);
                        selectedFilePath = originalName;
                        lblFileInfo.Text = $"📄 File: {Path.GetFileName(originalName)}";
                        lblFileInfo.ForeColor = Color.Blue;

                        // Re-enable Set Password button after decryption
                        bool hasPassword = File.Exists(PasswordHashFile) && File.Exists(SaltFile);
                        if (hasPassword)
                        {
                            btnSetPassword.Enabled = true;
                            btnSetPassword.Text = "🔑 Set Password";
                            btnSetPassword.BackColor = Color.Gold;
                        }

                        UpdateEncryptionButtons();
                    }
                }
                else if (!string.IsNullOrEmpty(selectedFolderPath))
                {
                    DialogResult confirm = MessageBox.Show(
                        $"Are you sure you want to decrypt ALL encrypted files in:\n{selectedFolderPath}",
                        "Confirm Folder Decryption",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (confirm == DialogResult.Yes)
                    {
                        EncryptionHelper.DecryptFolder(selectedFolderPath, currentPassword);
                    }
                }
                else
                {
                    MessageBox.Show("Please select a file or folder first.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Decryption failed: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EnableButtons(bool enable)
        {
            btnEncrypt.Enabled = enable;
            btnDecrypt.Enabled = enable;

            if (enable)
            {
                btnEncrypt.BackColor = Color.LightGreen;
                btnDecrypt.BackColor = Color.LightCoral;
            }
            else
            {
                btnEncrypt.BackColor = Color.LightGray;
                btnDecrypt.BackColor = Color.LightGray;
            }
        }
    }
}
