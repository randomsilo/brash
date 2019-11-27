# Strawman

Strawman is a project with everything.

```bash
dotnet run project-init -n Strawman -d /shop/randomsilo/Strawman
# make c# projects
# cd /shop/randomsilo/Strawman
# . ./init.sh

dotnet run data-init -n Strawman -d /shop/randomsilo/Strawman
# modify generated /shop/randomsilo/Strawman/structure.json

dotnet run sqlite-gen --file /shop/randomsilo/Strawman/id_patterns.json
dotnet run cs-domain --file /shop/randomsilo/Strawman/id_patterns.json
dotnet run cs-repo-sqlite --file /shop/randomsilo/Strawman/id_patterns.json
dotnet run cs-xtest-sqlite  --file /shop/randomsilo/Strawman/id_patterns.json
dotnet run cs-api-sqlite  --file /shop/randomsilo/Strawman/id_patterns.json

dotnet run sqlite-gen --file /shop/randomsilo/Strawman/tracking_patterns.json
dotnet run cs-domain --file /shop/randomsilo/Strawman/tracking_patterns.json
dotnet run cs-repo-sqlite --file /shop/randomsilo/Strawman/tracking_patterns.json
dotnet run cs-xtest-sqlite  --file /shop/randomsilo/Strawman/tracking_patterns.json
dotnet run cs-api-sqlite  --file /shop/randomsilo/Strawman/tracking_patterns.json

