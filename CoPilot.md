# CoPilot

CoPilot is a DOT driver tracking application.

## Brash Commands

```bash
dotnet run project-init -n CoPilot -d /shop/randomsilo/CoPilot
dotnet run data-init -n CoPilot -d /shop/randomsilo/CoPilot
dotnet run sqlite-gen --file /shop/randomsilo/CoPilot/structure.json

## - run combine.sh in the sql directory to create a single file for execution
cd ./sql/sqlite
. ./combine.sh
cd ..

dotnet run cs-domain --file /shop/randomsilo/CoPilot/structure.json
dotnet run cs-repo-sqlite --file /shop/randomsilo/CoPilot/structure.json
dotnet run cs-xtest-sqlite --file /shop/randomsilo/CoPilot/structure.json

dotnet run cs-api-sqlite --file /shop/randomsilo/CoPilot/structure.json --user APICOPILOT --pass BADTRUCKER --port 6100 --dev-site https://localhost:5001 --web-site https://copilot.ctrlshiftesc.com

```