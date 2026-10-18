namespace SparkleMail.Models;

public class AccountSettings
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    
    public string ImapServer { get; set; } = "imap.ukr.net";
    public int ImapPort { get; set; } = 993;
    
    public string SmtpServer { get; set; } = "smtp.ukr.net";
    public int SmtpPort { get; set; } = 465;
}