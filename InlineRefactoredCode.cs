// Inline refactored version - direct replacement for your original code
if (postimage == default)
{
    return (IActionResult)await AzfsHelper.CreateResponse_Data(req, "Post image is null or default", System.Net.HttpStatusCode.BadRequest);
}

// Extract entity processing logic
async Task<IActionResult> ProcessPathwayInformationAsync(dynamic entity)
{
    logger.LogInformation(Config.CrmConnString);
    var pathwayGMToMktoBL = new PathwayInformationBusinessGM(Config, entity, logger);
    var pathwayInformationGM = new UpsertCustomObjectsGM(pathwayGMToMktoBL, logger);
    var response = pathwayInformationGM.postData();
    var responseJson = System.Text.Json.JsonSerializer.Deserialize<OpportunityProductToMktoResponse>(response);

    logger.LogInformation($"response : {response}");
    
    if (responseJson == null || responseJson.success == false)
    {
        logger.LogInformation($"Error al sincronizar con marketo : {response}");
        return (IActionResult)await AzfsHelper.CreateResponse_Data(req, $"Error al sincronizar con pathwayinformation : {response}", System.Net.HttpStatusCode.BadRequest);
    }

    logger.LogInformation($"Finalizada correctamente {response}");
    return (IActionResult)await AzfsHelper.CreateResponse_Data(req, $"Finalizada correctamente \n{response}", System.Net.HttpStatusCode.OK);
}

// Simplified entity type handling with switch expression
var result = RemoteExecContext.PrimaryEntityName switch
{
    "contact" when IsValidContactEntity(postimage) => await ProcessContactSpecificLogicAsync(postimage),
    "team" when IsValidTeamEntity(postimage) => await ProcessPathwayInformationAsync(postimage),
    _ => (IActionResult)await AzfsHelper.CreateResponse_Data(req, $"Invalid or unsupported entity configuration", System.Net.HttpStatusCode.BadRequest)
};

return result;

// Helper methods for validation
bool IsValidContactEntity(dynamic entity)
{
    return entity.Contains("contactid") && 
           entity.Contains("ownerid") &&
           entity.GetAttributeValue<EntityReference>("ownerid").LogicalName == "team";
}

bool IsValidTeamEntity(dynamic entity)
{
    return entity.Contains("teamid") && entity.Contains("ie_signingpersonid");
}

// Contact-specific processing with Marketo sync validation
async Task<IActionResult> ProcessContactSpecificLogicAsync(dynamic contact)
{
    if (contact.Contains("new_synctomkto") && !contact.GetAttributeValue<bool>("new_synctomkto"))
        throw new Exception($"The contact is not synchronized with Marketo: {contact.Id}");
    
    return await ProcessPathwayInformationAsync(contact);
}