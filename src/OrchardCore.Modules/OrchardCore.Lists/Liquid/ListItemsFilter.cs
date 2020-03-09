using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Fluid;
using Fluid.Values;
using OrchardCore.ContentManagement;
using OrchardCore.Liquid;
using YesSql;
using OrchardCore.Lists.Helpers;

namespace OrchardCore.Lists.Liquid
{
    public class ListItemsFilter : ILiquidFilter
    {
        public async ValueTask<FluidValue> ProcessAsync(FluidValue input, FilterArguments arguments, TemplateContext ctx)
        {
            if (!ctx.AmbientValues.TryGetValue("Services", out var services))
            {
                throw new ArgumentException("Services missing while invoking 'list_items'");
            }

            string listContentItemId = null;

            if (input.Type == FluidValues.Object && input.ToObjectValue() is ContentItem contentItem)
            {
                listContentItemId = contentItem.ContentItemId;
            }
            else
            {
                listContentItemId = input.ToStringValue();
            }

            var session = ((IServiceProvider)services).GetRequiredService<ISession>();

            var listItems = await ListQueryHelpers.QueryListItemsAsync(session, listContentItemId);

            return FluidValue.Create(listItems);
        }
    }
}
