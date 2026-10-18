# SparkleMail

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)
![License: MIT](https://img.shields.io/badge/License-MIT-green)

A cross-platform desktop email client built with C# and Avalonia UI. Receives mail over IMAP and sends it over SMTP using [MailKit](https://github.com/jstedfast/MailKit).

<!-- TODO: add a screenshot -->
<!-- ![SparkleMail](docs/screenshot.png) -->

## Features

- Connect to a mailbox over IMAP
- Send emails over SMTP
- Runs on Linux, Windows, and macOS
- TODO: list what is actually implemented (folder list, reading messages, attachments, etc.)

## Tech Stack

- C# / .NET 8
- [Avalonia UI](https://avaloniaui.net/)
- [MailKit](https://github.com/jstedfast/MailKit) (IMAP/SMTP)

## Getting Started

Requires the [.NET SDK 8.0+](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/kuroshi-dev/SparkleMail.git
cd SparkleMail
dotnet run
```

## Mail Account Setup

| Setting | Example |
|---------|---------|
| IMAP    | `imap.gmail.com:993` |
| SMTP    | `smtp.gmail.com:587` |

> Gmail and some other providers require an app password instead of your main account password.

## Project Structure


```text
SparkleMail/
├── Models/              # Data structures and models
│   ├── EmailMessageModel.cs
│   └── FolderModel.cs
├── Services/            # Business logic and external integrations
│   ├── AccountSettings.cs
│   ├── MailKitEmailService.cs
│   └── SettingsStorage.cs
├── Views/               # Avalonia UI views and windows
│   └── MainWindow.axaml
├── App.axaml            # Application styles and global resources
└── Program.cs           # Entry point
```
## License

Released under the [MIT License](LICENSE).