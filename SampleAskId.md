# SampleAskId

SampleAskId is a basic project to test the AskId database pattern.

```bash
dotnet run project-init -n SampleAskId -d /shop/randomsilo/SampleAskId
# make c# projects
# cd /shop/randomsilo/SampleAskId
# . ./init.sh

dotnet run data-init -n SampleAskId -d /shop/randomsilo/SampleAskId
# modify generated /shop/randomsilo/SampleAskId/structure.json

dotnet run sqlite-gen --file /shop/randomsilo/SampleAskId/structure.json
## - run combine.sh in the sql directory to create a single file for execution
dotnet run cs-domain --file /shop/randomsilo/SampleAskId/structure.json
dotnet run cs-repo-sqlite --file /shop/randomsilo/SampleAskId/structure.json
dotnet run cs-xtest-sqlite --file /shop/randomsilo/SampleAskId/structure.json
dotnet run cs-api-sqlite --file /shop/randomsilo/SampleAskId/structure.json
```