using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Orchard.ContentManagement;
using Orchard.Logging;
using Orchard.Workflows.Activities;
using Orchard.Workflows.Services;
using Orchard.Mvc.Extensions;

namespace Orchard.Workflows.Controllers
{
    public class SignalController : Controller
    {
        private readonly IWorkflowManager _workflowManager;
        private readonly ISignalService _genericEventService;
        private readonly IContentManager _contentManager;

        public SignalController(
            IWorkflowManager workflowManager,
            ISignalService genericEventService,
            IContentManager contentManager)
        {
            _workflowManager = workflowManager;
            _genericEventService = genericEventService;
            _contentManager = contentManager;
        }

        public ILogger Logger { get; set; }

        // This could be invoked by external applications and services to trigger an event within the workflows.
        public ActionResult Trigger(string nonce)
        {

            int contentItemId;
            string signal;

            if (!_genericEventService.DecryptNonce(nonce, out contentItemId, out signal))
            {
                Logger.Debug("Invalid nonce provided: " + nonce);
                return NotFound();
            }

            var contentItem = _contentManager.Get(contentItemId, VersionOptions.Latest);

            if (contentItem == null)
            {
                Logger.Debug("Could not find specified content item in none: " + contentItemId);
                return NotFound();
            }

            // Right now, all workflow instances that are at the WebRequest activity node would continue as soon as a request
            // to this action comes in, but that should not happen; it should be controlled by activity configuration and evaluations.
            _workflowManager.TriggerEvent(SignalActivity.SignalEventName, contentItem, () =>
            {
                var dictionary = new Dictionary<string, object> { { "Content", contentItem }, { SignalActivity.SignalEventName, signal } };

                // Let's include query string stuff, so that the WebRequest activity can
                // potentially match against certain parameters to decide whether or not it should execute,
                // based on its configuration (yet to be defined).
                Request.Query.CopyTo(dictionary);
                if (Request.HasFormContentType)
                {
                    Request.Form.CopyTo(dictionary);
                }

                return dictionary;
            });

            // 200
            return new EmptyResult();
        }
    }
}