using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.ContentManagement;
using OrchardCore.Contents;
using OrchardCore.Mvc.Utilities;

namespace OrchardCore.Content.Controllers
{
    [Route("api/content")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Api"), IgnoreAntiforgeryToken, AllowAnonymous]
    public class ApiController : Controller
    {
        private readonly IContentManager _contentManager;
        private readonly IAuthorizationService _authorizationService;

        public ApiController(
            IContentManager contentManager,
            IAuthorizationService authorizationService)
        {
            _authorizationService = authorizationService;
            _contentManager = contentManager;
        }

        [Route("{contentItemId}"), HttpGet]
        public async Task<IActionResult> Get(string contentItemId)
        {
            var contentItem = await _contentManager.GetAsync(contentItemId);

            if (contentItem == null)
            {
                return NotFound();
            }

            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ViewContent, contentItem))
            {
                return this.ChallengeOrForbid();
            }

            return Ok(contentItem);
        }

        [HttpDelete]
        [Route("{contentItemId}")]
        public async Task<IActionResult> Delete(string contentItemId)
        {
            var contentItem = await _contentManager.GetAsync(contentItemId);

            if (contentItem == null)
            {
                return StatusCode(204);
            }

            if (!await _authorizationService.AuthorizeAsync(User, Permissions.DeleteContent, contentItem))
            {
                return this.ChallengeOrForbid();
            }

            await _contentManager.RemoveAsync(contentItem);

            return Ok(contentItem);
        }

        [HttpPost]
        public async Task<IActionResult> Post(ContentItem model, bool draft = false)
        {
            // It is really important to keep the proper method calls order with the ContentManager
            // so that all event handlers gets triggered in the right sequence.
            
            var contentItem = await _contentManager.GetAsync(model.ContentItemId, VersionOptions.DraftRequired);

            if (contentItem == null)
            {
                if (!await _authorizationService.AuthorizeAsync(User, Permissions.PublishContent))
                {
                    return this.ChallengeOrForbid();
                }

                var newContentItem = await _contentManager.NewAsync(model.ContentType);
                newContentItem.Merge(model);

                await _contentManager.UpdateAndCreateAsync(newContentItem, draft ? VersionOptions.DraftRequired : VersionOptions.Published);

                contentItem = newContentItem;
            }
            else
            {
                if (!await _authorizationService.AuthorizeAsync(User, Permissions.EditContent, contentItem))
                {
                    return this.ChallengeOrForbid();
                }

                contentItem.Merge(model);
                await _contentManager.UpdateAsync(contentItem);

                if (!draft)
                {
                    await _contentManager.PublishAsync(contentItem); 
                }
            }

            return Ok(contentItem);
        }
    }
}
