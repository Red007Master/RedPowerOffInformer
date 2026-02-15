# RedPowerOffInformer

This program displays power-off schedules for groups from the LOE API.

## TLDR

Fetches power-off information from an API and displays it in a formatted table showing today's and tomorrow's schedules.

## Building

```bash
dotnet build
```

## Running

```bash
dotnet run
```

## Configuration

First run will ask for your power-off group name, which is saved to the config file. You can also specify a group via command line:

```bash
dotnet run -- -g "YourGroupName"
```