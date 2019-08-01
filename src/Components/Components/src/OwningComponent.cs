// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// A base class that creates a service provider scope, and resolves a service of type <typeparamref name="TService"/>.
    /// </summary>
    /// <typeparam name="TService">The service type.</typeparam>
    /// <remarks>
    /// Use the <see cref="OwningComponent{TService}"/> class as a base class to author components that control
    /// the lifetime of a service or multiple services. This is useful when using a transient or scoped service that
    /// requires disposal such as a repository or database abstraction. Using <see cref="OwningComponent{TService}"/>
    /// as a base class ensures that the service and relates services that share its scope are disposed with the component.
    /// </remarks>
    public abstract class OwningComponent<TService> : ComponentBase, IDisposable
    {
        private IServiceScope _scope;
        private TService _item;

        [Inject] IServiceScopeFactory ScopeFactory { get; set; }

        /// <summary>
        /// Gets the scoped <see cref="IServiceProvider"/> that is associated with this component.
        /// </summary>
        protected IServiceProvider ScopedServices
        {
            get
            {
                if (ScopeFactory == null)
                {
                    throw new InvalidOperationException("Services cannot be accessed before the component is initialized.");
                }

                _scope ??= ScopeFactory.CreateScope();
                return _scope.ServiceProvider;
            }
        }

        /// <summary>
        /// Gets the <typeparamref name="TService"/> that is associated with this component.
        /// </summary>
        protected TService Service
        {
            get
            {
                // We cache this because we don't know the lifetime. We have to assume that it could be transient.
                _item ??= ScopedServices.GetRequiredService<TService>();
                return _item;
            }
        }

        void IDisposable.Dispose()
        {
            _scope?.Dispose();
            _scope = null;
            Dispose(disposing: true);
        }

        /// <inheritdoc />
        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
