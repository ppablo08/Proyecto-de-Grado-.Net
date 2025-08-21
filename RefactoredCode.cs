// Refactored version of the original code with extracted methods to eliminate duplication

// Original duplicated code refactored into reusable methods
public async Task<IActionResult> HandleEntitySynchronization()
{
    if (RemoteExecContext.PrimaryEntityName == "contact" && postimage != default)
    {
        if (IsValidContactForSync(postimage))
        {
            return await ProcessEntitySynchronization(postimage, "contact");
        }
    }
    else if (RemoteExecContext.PrimaryEntityName == "team" && postimage != default)
    {
        if (IsValidTeamForSync(postimage))
        {
            return await ProcessEntitySynchronization(postimage, "team");
        }
    }

    // Return appropriate response if no valid entity found
    return (IActionResult)await AzfsHelper.CreateResponse_Data(req, 
        "No valid entity found for synchronization", 
        System.Net.HttpStatusCode.BadRequest);
}

private bool IsValidContactForSync(Entity postimage)
{
    if (!postimage.Contains("contactid") || !postimage.Contains("ownerid"))
        return false;

    var ownerRef = postimage.GetAttributeValue<EntityReference>("ownerid");
    if (ownerRef?.LogicalName != "team")
        return false;

    if (postimage.Contains("new_synctomkto") && !postimage.GetAttributeValue<bool>("new_synctomkto"))
    {
        throw new Exception($"The contact is not synchronized with Marketo: {postimage.Id}");
    }

    return true;
}

private bool IsValidTeamForSync(Entity postimage)
{
    return postimage.Contains("teamid") && postimage.Contains("ie_signingpersonid");
}

private async Task<IActionResult> ProcessEntitySynchronization(Entity postimage, string entityType)
{
    try
    {
        logger.LogInformation(Config.CrmConnString);
        
        var pathwayGMToMktoBL = new PathwayInformationBusinessGM(Config, postimage, logger);
        var pathwayInformationGM = new UpsertCustomObjectsGM(pathwayGMToMktoBL, logger);
        
        string response = pathwayInformationGM.postData();
        var responseJson = System.Text.Json.JsonSerializer.Deserialize<OpportunityProductToMktoResponse>(response);

        logger.LogInformation($"response : {response}");

        if (responseJson == null || responseJson.success == false)
        {
            string errorMessage = $"Error al sincronizar con marketo : {response}";
            logger.LogInformation(errorMessage);
            return (IActionResult)await AzfsHelper.CreateResponse_Data(req, 
                $"Error al sincronizar con pathwayinformation : {response}", 
                System.Net.HttpStatusCode.BadRequest);
        }

        string successMessage = $"Finalizada correctamente {response}";
        logger.LogInformation(successMessage);
        return (IActionResult)await AzfsHelper.CreateResponse_Data(req, 
            $"Finalizada correctamente \n{response}", 
            System.Net.HttpStatusCode.OK);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, $"Error processing {entityType} synchronization");
        return (IActionResult)await AzfsHelper.CreateResponse_Data(req, 
            $"Error processing {entityType} synchronization: {ex.Message}", 
            System.Net.HttpStatusCode.InternalServerError);
    }
}