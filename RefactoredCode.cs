using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Net;

public class RefactoredEntityProcessor
{
    private readonly ILogger logger;
    private readonly Config config;
    private readonly HttpRequest req;

    public RefactoredEntityProcessor(ILogger logger, Config config, HttpRequest req)
    {
        this.logger = logger;
        this.config = config;
        this.req = req;
    }

    public async Task<IActionResult> ProcessEntityAsync(RemoteExecContext remoteExecContext, dynamic postimage)
    {
        if (postimage == default)
            return await CreateErrorResponse("Post image is null or default");

        return remoteExecContext.PrimaryEntityName switch
        {
            "contact" => await ProcessContactEntityAsync(postimage),
            "team" => await ProcessTeamEntityAsync(postimage),
            _ => await CreateErrorResponse($"Unsupported entity type: {remoteExecContext.PrimaryEntityName}")
        };
    }

    private async Task<IActionResult> ProcessContactEntityAsync(dynamic postimage)
    {
        if (!IsValidContactEntity(postimage))
            return await CreateErrorResponse("Invalid contact entity data");

        if (!IsSyncedWithMarketo(postimage))
            throw new Exception($"The contact is not synchronized with Marketo: {postimage.Id}");

        return await ProcessPathwayInformationAsync(postimage);
    }

    private async Task<IActionResult> ProcessTeamEntityAsync(dynamic postimage)
    {
        if (!IsValidTeamEntity(postimage))
            return await CreateErrorResponse("Invalid team entity data");

        return await ProcessPathwayInformationAsync(postimage);
    }

    private bool IsValidContactEntity(dynamic postimage)
    {
        return postimage.Contains("contactid") && 
               postimage.Contains("ownerid") &&
               postimage.GetAttributeValue<EntityReference>("ownerid").LogicalName == "team";
    }

    private bool IsValidTeamEntity(dynamic postimage)
    {
        return postimage.Contains("teamid") && 
               postimage.Contains("ie_signingpersonid");
    }

    private bool IsSyncedWithMarketo(dynamic postimage)
    {
        return !postimage.Contains("new_synctomkto") || 
               postimage.GetAttributeValue<bool>("new_synctomkto");
    }

    private async Task<IActionResult> ProcessPathwayInformationAsync(dynamic postimage)
    {
        try
        {
            logger.LogInformation(config.CrmConnString);

            var pathwayBusinessLogic = new PathwayInformationBusinessGM(config, postimage, logger);
            var pathwayInformation = new UpsertCustomObjectsGM(pathwayBusinessLogic, logger);
            var response = pathwayInformation.postData();

            return await HandlePathwayResponse(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing pathway information");
            return await CreateErrorResponse($"Error processing pathway information: {ex.Message}");
        }
    }

    private async Task<IActionResult> HandlePathwayResponse(string response)
    {
        logger.LogInformation($"Response: {response}");

        var responseJson = JsonSerializer.Deserialize<OpportunityProductToMktoResponse>(response);

        if (responseJson?.success != true)
        {
            logger.LogError($"Error synchronizing with Marketo: {response}");
            return await CreateErrorResponse($"Error synchronizing with pathway information: {response}");
        }

        logger.LogInformation($"Successfully completed: {response}");
        return await CreateSuccessResponse($"Successfully completed \n{response}");
    }

    private async Task<IActionResult> CreateErrorResponse(string message)
    {
        return await AzfsHelper.CreateResponse_Data(req, message, HttpStatusCode.BadRequest);
    }

    private async Task<IActionResult> CreateSuccessResponse(string message)
    {
        return await AzfsHelper.CreateResponse_Data(req, message, HttpStatusCode.OK);
    }
}

// Usage example:
public class YourController
{
    public async Task<IActionResult> YourMethod(RemoteExecContext remoteExecContext, dynamic postimage)
    {
        var processor = new RefactoredEntityProcessor(logger, config, req);
        return await processor.ProcessEntityAsync(remoteExecContext, postimage);
    }
}