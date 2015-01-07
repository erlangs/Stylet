﻿using StyletIoC.Creation;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace StyletIoC.Internal.Creators
{
    /// <summary>
    /// Creator which knows how to create an instance of a type, by finding a suitable constructor and calling it
    /// </summary>
    // Sealed so Code Analysis doesn't moan about us setting the virtual Type property
    internal sealed class TypeCreator : CreatorBase
    {
        private readonly string _attributeKey;
        public string AttributeKey
        {
            get { return this._attributeKey; }
        }
        private Expression creationExpression;

        public TypeCreator(Type type, IRegistrationContext parentContext)
            : base(parentContext)
        {
            this.Type = type;

            // Use the key from InjectAttribute (if present), and let someone else override it if they want
            var attribute = type.GetCustomAttribute<InjectAttribute>(true);
            if (attribute != null)
                this._attributeKey = attribute.Key;
        }

        private string KeyForParameter(ParameterInfo parameter)
        {
            var attribute = parameter.GetCustomAttribute<InjectAttribute>(true);
            return attribute == null ? null : attribute.Key;
        }

        public override Expression GetInstanceExpression(ParameterExpression registrationContext)
        {
            if (this.creationExpression != null)
                return this.creationExpression;

            // Find the constructor which has the most parameters which we can fulfill, accepting default values which we can't fulfill
            ConstructorInfo ctor;
            var ctorsWithAttribute = this.Type.GetConstructors().Where(x => x.GetCustomAttribute<InjectAttribute>(true) != null).ToList();
            if (ctorsWithAttribute.Count > 1)
            {
                throw new StyletIoCFindConstructorException(String.Format("Found more than one constructor with [Inject] on type {0}.", this.Type.GetDescription()));
            }
            else if (ctorsWithAttribute.Count == 1)
            {
                ctor = ctorsWithAttribute[0];
                var key = ctorsWithAttribute[0].GetCustomAttribute<InjectAttribute>(true).Key;
                var cantResolve = ctor.GetParameters().Where(p => !this.parentContext.CanResolve(p.ParameterType, key) && !p.HasDefaultValue).FirstOrDefault();
                if (cantResolve != null)
                    throw new StyletIoCFindConstructorException(String.Format("Found a constructor with [Inject] on type {0}, but can't resolve parameter '{1}' (of type {2}, and doesn't have a default value).", this.Type.GetDescription(), cantResolve.Name, cantResolve.ParameterType.GetDescription()));
            }
            else
            {
                ctor = this.Type.GetConstructors()
                    .Where(c => c.GetParameters().All(p => this.parentContext.CanResolve(p.ParameterType, this.KeyForParameter(p)) || p.HasDefaultValue))
                    .OrderByDescending(c => c.GetParameters().Count(p => !p.HasDefaultValue))
                    .FirstOrDefault();

                if (ctor == null)
                {
                    // Get us a bit more information....
                    var info = String.Join("\n\n", this.Type.GetConstructors().Select(c => String.Format("Constructor:\n{0}\n\n", String.Join("\n", c.GetParameters().Select(p =>
                    {
                        var key = this.KeyForParameter(p);
                        var canResolve = this.parentContext.CanResolve(p.ParameterType, key) || p.HasDefaultValue;
                        var keyStr = key == null ? "" : String.Format(" [Key = {0}]", key);
                        var usingDefaultStr = (!this.parentContext.CanResolve(p.ParameterType, key) && p.HasDefaultValue) ? " [Using Default]" : "";
                        return String.Format("   {0}{1}: {2}{3}", p.ParameterType.GetDescription(), keyStr, canResolve ? "Success" : "Failure", usingDefaultStr);
                    })))));

                    throw new StyletIoCFindConstructorException(String.Format("Unable to find a constructor for type {0} which we can call:\n{1}", this.Type.GetDescription(), info));
                }
            }

            // If we get circular dependencies, we'll just blow the stack. They're a pain to resolve.

            // If there parameter's got an InjectAttribute with a key, use that key to resolve
            var ctorParams = ctor.GetParameters().Select(x =>
            {
                var key = this.KeyForParameter(x);
                if (this.parentContext.CanResolve(x.ParameterType, key))
                {
                    try
                    {
                        return this.parentContext.GetSingleRegistration(x.ParameterType, key, true).GetInstanceExpression(registrationContext);
                    }
                    catch (StyletIoCRegistrationException e)
                    {
                        throw new StyletIoCRegistrationException(String.Format("{0} Required by parameter '{1}' of type {2} (which is a {3}).", e.Message, x.Name, this.Type.GetDescription(), x.ParameterType.GetDescription()), e);
                    }
                }
                // For some reason we need this cast...
                return Expression.Convert(Expression.Constant(x.DefaultValue), x.ParameterType);
            });

            var creator = Expression.New(ctor, ctorParams);

            var completeExpression = this.CompleteExpressionFromCreator(creator, registrationContext);

            Interlocked.CompareExchange(ref this.creationExpression, completeExpression, null);
            return this.creationExpression;
        }
    }
}