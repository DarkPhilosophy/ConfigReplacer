# ConfigReplacer

A utility application for replacing configuration values in multiple files.

## Overview

ConfigReplacer is a WPF application that allows you to replace configuration values in multiple files based on predefined rules. It's designed to streamline the process of updating configuration files across different environments.

## Features

- Replace configuration values in multiple files
- Support for different file formats
- Multi-language support (English/Romanian)
- Detailed logging
- Advertisement system

## Getting Started

### Prerequisites

- Windows 7 or later
- .NET Framework 4.8 or later
- .NET 6.0 or later (for newer framework targets)
- Visual Studio 2019 or later

### Installation

1. Download the latest release from the [Releases](https://github.com/DarkPhilosophy/ConfigReplacer/releases) page
2. Extract the ZIP file to your preferred location
3. Run `ConfigReplacer.exe`

### Building from Source

The application can be built for multiple target frameworks:

```powershell
# For .NET Framework 4.8 (framework-dependent)
dotnet publish ConfigReplacer\ConfigReplacer.csproj -r win-x64 -f net48 -c Release -o Release\ConfigReplacer-net48-fd

# For .NET 6.0 (framework-dependent)
dotnet publish ConfigReplacer\ConfigReplacer.csproj -r win-x64 -f net6.0-windows -c Release -o Release\ConfigReplacer-net6-fd

# For .NET 9.0 (framework-dependent)
dotnet publish ConfigReplacer\ConfigReplacer.csproj -r win-x64 -f net9.0-windows -c Release -o Release\ConfigReplacer-net9-fd
```

## Usage

1. Launch the application
2. Configure the replacement rules in settings.json
3. Click "Replace" to process the files
4. View the log for details on the replacements made

### Configuration

The application uses a settings.json file to define replacement rules. Example configuration:

```json
{
  "Language": "English",
  "FilePaths": [
    "C:\\Path\\To\\Config1.xml",
    "C:\\Path\\To\\Config2.ini"
  ],
  "Replacements": [
    {
      "OldValue": "localhost",
      "NewValue": "production.server.com"
    },
    {
      "OldValue": "DEBUG",
      "NewValue": "RELEASE"
    }
  ]
}
```

## Project Structure

- `/src`: Source code files
- `/assets`: Resources (icons, sounds, language files)
- `/docs`: Documentation

## Dependencies

- [Common Library](https://github.com/DarkPhilosophy/Common) - Shared components for WPF applications
- [Newtonsoft.Json](https://www.newtonsoft.com/json) - JSON framework for .NET

## License

This project is licensed under the MIT License - see the [LICENSE](../LICENSE) file for details.

## Author

Adalbert Alexandru Ungureanu - [adalbertalexandru.ungureanu@flex.com](mailto:adalbertalexandru.ungureanu@flex.com)
