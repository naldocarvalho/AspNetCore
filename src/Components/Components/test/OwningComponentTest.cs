// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Dynamic;
using System.Linq;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Components
{
    public class OwningComponentTest
    {
        [Fact]
        public void CreatesScopeAndService()
        {
            var services = new ServiceCollection();
            services.AddTransient<MyService>();

            var renderer = new TestRenderer(services.BuildServiceProvider());

            var i = 0;
            var component = new TestComponent(builder =>
            {
                if (i % 2 == 0)
                {
                    builder.OpenComponent<MyOwningComponent>(1);
                    builder.CloseComponent();
                }

                builder.AddContent(2, "--");

                if (i % 2 == 1)
                {
                    builder.OpenComponent<MyOwningComponent>(3);
                    builder.CloseComponent();
                }

                i++;
            });

            var componentId = renderer.AssignRootComponentId(component);
            renderer.RenderRootComponent(componentId);

            var batch = renderer.Batches[0];
            var frames = batch.ReferenceFrames;
            Assert.Collection(
                frames,
                frame => AssertFrame.Component<MyOwningComponent>(frame, 1, 1),
                frame => AssertFrame.Text(frame, "--", 2),
                frame => AssertFrame.Text(frame, "Created: 1 - Disposed: 0", 1));

            component.TriggerRender();

            batch = renderer.Batches[1];
            frames = batch.ReferenceFrames;
            Assert.Collection(
                frames,
                frame => AssertFrame.Component<MyOwningComponent>(frame, 1, 3),
                frame => AssertFrame.Text(frame, "Created: 2 - Disposed: 1", 1));
        }

        private class MyService : IDisposable
        {
            private static int CreatedCount = 0;

            private static int DisposedCount = 0;

            public MyService() => CreatedCount++;

            void IDisposable.Dispose() => DisposedCount++;

            public string Message => $"Created: {CreatedCount} - Disposed: {DisposedCount}";
        }

        private class MyOwningComponent : OwningComponent<MyService>
        {
            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.AddContent(1, Service.Message);
            }
        }

        class TestComponent : AutoRenderComponent
        {
            private readonly RenderFragment _renderFragment;

            public TestComponent(RenderFragment renderFragment)
            {
                _renderFragment = renderFragment;
            }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
                => _renderFragment(builder);
        }
    }
}
