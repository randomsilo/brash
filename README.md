# hasty

Hasty is a tool for quickly building sqlite backed APIs


## How to Use

```bash

cd ./brashcli
dotnet run project-init -n Rampart -d /shop/randomsilo/Rampart
dotnet run data-init -n Rampart -d /shop/randomsilo/Rampart

```


## Deploy NuGet Package

```bash
cd /shop/randomsilo/brash/Brash/bin/Debug/
dotnet nuget push Brash.1.0.0.nupkg -k oy2f6zfjelxyfzypku7qjwze4d3ev2quhm6zvresyvywka -s https://api.nuget.org/v3/index.json

```