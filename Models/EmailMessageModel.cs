using System;
using System.ComponentModel;

namespace SparkleMail.Models;

public class EmailMessageModel : INotifyPropertyChanged
{
    private bool _isSeen;

    public uint UniqueId { get; set; }
    public string Sender { get; set; } = string.Empty;       // имя (или адрес, если имени нет)
    public string SenderEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Preview { get; set; } = string.Empty;

    public string Initial => string.IsNullOrEmpty(Sender) ? "?" : Sender[..1].ToUpper();

    public bool IsSeen
    {
        get => _isSeen;
        set
        {
            _isSeen = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSeen)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}