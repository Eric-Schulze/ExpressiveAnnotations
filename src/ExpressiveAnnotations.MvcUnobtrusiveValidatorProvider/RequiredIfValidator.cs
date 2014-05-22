﻿using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;
using ExpressiveAnnotations.ConditionalAttributes;
using System.Collections.Generic;
using ExpressiveAnnotations.Misc;
using Newtonsoft.Json;

namespace ExpressiveAnnotations.MvcUnobtrusiveValidatorProvider
{
    public class RequiredIfValidator : DataAnnotationsModelValidator<RequiredIfAttribute>
    {
        private readonly string _errorMessage;
        private readonly string _dependentProperty;
        private readonly string _relationalOperator;
        private readonly object _targetValue;
        private readonly string _type; 

        public RequiredIfValidator(ModelMetadata metadata, ControllerContext context, RequiredIfAttribute attribute)
            : base(metadata, context, attribute)
        {
            var dependentProperty = PropHelper.ExtractProperty(metadata.ContainerType, attribute.DependentProperty);
            var relationalOperator = attribute.RelationalOperator ?? "==";

            string targetPropertyName;
            var attributeName = GetType().BaseType.GetGenericArguments().Single().Name;
            if (PropHelper.TryExtractName(attribute.TargetValue, out targetPropertyName))
            {
                var targetProperty = PropHelper.ExtractProperty(metadata.ContainerType, targetPropertyName);
                Assert.ConsistentTypes(dependentProperty, targetProperty, metadata.PropertyName, attributeName, relationalOperator);
            }
            else
                Assert.ConsistentTypes(dependentProperty, attribute.TargetValue, metadata.PropertyName, attributeName, relationalOperator);

            var displayAttribute = dependentProperty.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            var dependentPropertyName = displayAttribute != null ? displayAttribute.GetName() : attribute.DependentProperty;

            _errorMessage = attribute.FormatErrorMessage(metadata.GetDisplayName(), dependentPropertyName);
            _dependentProperty = attribute.DependentProperty;
            _relationalOperator = relationalOperator;
            _targetValue = attribute.TargetValue;

            _type = TypeHelper.GetCoarseType(dependentProperty.PropertyType);
        }

        public override IEnumerable<ModelClientValidationRule> GetClientValidationRules()
        {
            var rule = new ModelClientValidationRule
            {
                ErrorMessage = _errorMessage,
                ValidationType = "requiredif",
            };
            rule.ValidationParameters.Add("dependentproperty", JsonConvert.SerializeObject(_dependentProperty));
            rule.ValidationParameters.Add("relationaloperator", JsonConvert.SerializeObject(_relationalOperator));
            rule.ValidationParameters.Add("targetvalue", JsonConvert.SerializeObject(_targetValue));
            rule.ValidationParameters.Add("type", JsonConvert.SerializeObject(_type));
            yield return rule;
        }
    }
}
