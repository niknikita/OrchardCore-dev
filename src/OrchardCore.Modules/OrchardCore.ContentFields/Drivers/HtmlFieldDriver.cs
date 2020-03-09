using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.ContentFields.ViewModels;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Liquid;

namespace OrchardCore.ContentFields.Fields
{
    public class HtmlFieldDisplayDriver : ContentFieldDisplayDriver<HtmlField>
    {
        private readonly ILiquidTemplateManager _liquidTemplateManager;
        private readonly HtmlEncoder _htmlEncoder;
        private readonly IStringLocalizer<HtmlFieldDisplayDriver> S;

        public HtmlFieldDisplayDriver(ILiquidTemplateManager liquidTemplateManager, IStringLocalizer<HtmlFieldDisplayDriver> localizer, HtmlEncoder htmlEncoder)
        {
            _liquidTemplateManager = liquidTemplateManager;
            S = localizer;
            _htmlEncoder = htmlEncoder;
        }

        public override IDisplayResult Display(HtmlField field, BuildFieldDisplayContext context)
        {
            return Initialize<DisplayHtmlFieldViewModel>(GetDisplayShapeType(context), async model =>
            {
                model.Html = field.Html;
                model.Field = field;
                model.Part = context.ContentPart;
                model.PartFieldDefinition = context.PartFieldDefinition;

                model.Html = await _liquidTemplateManager.RenderAsync(field.Html, _htmlEncoder, model,
                    scope => scope.SetValue("ContentItem", field.ContentItem));
            })
            .Location("Detail", "Content")
            .Location("Summary", "Content");
        }

        public override IDisplayResult Edit(HtmlField field, BuildFieldEditorContext context)
        {
            return Initialize<EditHtmlFieldViewModel>(GetEditorShapeType(context), model =>
            {
                model.Html = field.Html;
                model.Field = field;
                model.Part = context.ContentPart;
                model.PartFieldDefinition = context.PartFieldDefinition;
            });
        }

        public override async Task<IDisplayResult> UpdateAsync(HtmlField field, IUpdateModel updater, UpdateFieldEditorContext context)
        {
            var viewModel = new EditHtmlFieldViewModel();

            if (await updater.TryUpdateModelAsync(viewModel, Prefix, f => f.Html))
            {
                if (!string.IsNullOrEmpty(viewModel.Html) && !_liquidTemplateManager.Validate(viewModel.Html, out var errors))
                {
                    var fieldName = context.PartFieldDefinition.DisplayName();
                    context.Updater.ModelState.AddModelError(nameof(field.Html), S["{0} doesn't contain a valid Liquid expression. Details: {1}", fieldName, string.Join(" ", errors)]);
                }
                else
                {
                    field.Html = viewModel.Html;
                }
            }

            return Edit(field, context);
        }
    }
}
