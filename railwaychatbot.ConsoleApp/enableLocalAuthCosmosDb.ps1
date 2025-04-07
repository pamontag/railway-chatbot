$cosmosdbname = "openaicosmosdbconv"
$resourcegroup = "RG_OPENAI"
$cosmosdb = az cosmosdb show --name $cosmosdbname --resource-group $resourcegroup | ConvertFrom-Json
az resource update --ids $cosmosdb.id --set properties.disableLocalAuth=false --latest-include-preview