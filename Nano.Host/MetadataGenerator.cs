using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using Nano.Core;

namespace Nano.Host
{
    /// <summary>
    /// Generates metadata via reflection.
    /// </summary>
    public static class MetadataGenerator
    {
        /// <summary>
        /// Generates metadata for the given type via reflection.
        /// </summary>
        /// <param name="type">Type to reflect and generate metadata for.</param>
        /// <returns>Metadata</returns>
        public static ApiMetadata GenerateApiMetadata( Type type )
        {
            var apiMetadata = new ApiMetadata();

            apiMetadata.ApiName = type.Name;

            apiMetadata.ApiDescription = GetDescription( type );

            var methods = type.GetMethods( BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance );

            foreach( var method in methods )
            {
                string[] excludedMethods = { "Equals", "ToString", "ReferenceEquals", "GetHashCode", "GetType" };

                if( excludedMethods.Contains( method.Name ) )
                    continue;

                var methodMetadata = new MethodMetadata();

                methodMetadata.MethodName = method.Name;

                methodMetadata.MethodDescription = GetDescription( method );

                var xmlMethodDocumentation = XmlDocumentationHelper.GetXmlDocumentation( method );

                var inputParameters = method.GetParameters();

                foreach( var inputParameter in inputParameters.OrderBy( x => x.Position ) )
                {
                    var methodParameter = new DynamicDictionary();

                    methodParameter.Add( "ParameterName", inputParameter.Name );
                    methodParameter.Add( "ParameterType", GetTypeName( inputParameter.ParameterType ) );
                    methodParameter.Add( "IsNullable", IsNullable( inputParameter.ParameterType ) );
                    methodParameter.Add( "ParameterDescription", XmlDocumentationHelper.GetParameterDescription( xmlMethodDocumentation, inputParameter.Name ) );
                    methodParameter.Add( "OrdinalPosition", inputParameter.Position );
                    methodParameter.Add( "IsOptional", inputParameter.IsOptional );

                    methodMetadata.InputParameters.Add( methodParameter );

                    // Adding all user types as "Models
                    apiMetadata.AddModels( inputParameter.ParameterType );
                }

                methodMetadata.ReturnParameterType = method.ReturnType.Name;

                // Adding all user types as "Models
                apiMetadata.AddModels( method.ReturnType );
                
                apiMetadata.Methods.Add( methodMetadata );
            }

            return apiMetadata;
        }

        /// <summary>
        /// Recursive method that crawls each of the types fields and properties creating
        /// Models for each user type.
        /// </summary>
        /// <param name="apiMetadata">ApiMetadata to add each model to.</param>
        /// <param name="type">Type to crawl.</param>
        public static void AddModels( this ApiMetadata apiMetadata, Type type )
        {
            var nestedUserTypes = new List<Type>();

            // Adding all user types as "Models"
            if( IsUserType( type ) && apiMetadata.Models.Any( x => x.ModelType == type.Name ) == false )
            {
                var modelMetadata = new ModelMetadata();

                modelMetadata.ModelType = type.Name;

                modelMetadata.ModelDescription = GetDescription( type );

                var modelFields = type.GetFields();

                foreach( var modelField in modelFields )
                {
                    // Add nested user types
                    if( IsUserType( type ) && apiMetadata.Models.Any( x => x.ModelType == type.Name ) == false )
                        nestedUserTypes.Add( modelField.FieldType );

                    var fieldMetadata = new DynamicDictionary();

                    fieldMetadata.Add( "PropertyName", modelField.Name );
                    fieldMetadata.Add( "PropertyType", GetTypeName( modelField.FieldType ) );
                    fieldMetadata.Add( "IsNullable", IsNullable( modelField.FieldType ) );
                    fieldMetadata.Add( "PropertyDescription", GetDescription( modelField ) );

                    modelMetadata.Properties.Add( fieldMetadata );
                }

                var modelProperties = type.GetProperties();

                foreach( var modelProperty in modelProperties )
                {
                    // Add nested user types
                    if( IsUserType( type ) && apiMetadata.Models.Any( x => x.ModelType == type.Name ) == false )
                        nestedUserTypes.Add( modelProperty.PropertyType );

                    var propertyMetadata = new DynamicDictionary();

                    propertyMetadata.Add( "PropertyName", modelProperty.Name );
                    propertyMetadata.Add( "PropertyType", GetTypeName( modelProperty.PropertyType ) );
                    propertyMetadata.Add( "IsNullable", IsNullable( modelProperty.PropertyType ) );
                    propertyMetadata.Add( "PropertyDescription", GetDescription( modelProperty ) );

                    modelMetadata.Properties.Add( propertyMetadata );
                }

                apiMetadata.Models.Add( modelMetadata );

                // For each nested user type recursively call this same method to add them
                // Note it is important to do this after the current modelMetadata has been added
                // to avoid a stack overflow due to possible circular dependencies.
                foreach( var nestedUserType in nestedUserTypes )
                {
                    apiMetadata.AddModels( nestedUserType );
                }
            }
        }

        /// <summary>
        /// Determines if a type is a User Type meaning it is not a standard .NET type.
        /// </summary>
        /// <param name="type">Type.</param>
        /// <returns>Boolean value.</returns>
        public static bool IsUserType( Type type )
        {
            return type.Namespace != null &&
                   type.Namespace.StartsWith( "System" ) == false &&
                   type.Namespace.StartsWith( "Microsoft" ) == false;
        }

        /// <summary>
        /// Gets the type name.
        /// </summary>
        /// <param name="type">Type.</param>
        /// <returns>Type name.</returns>
        public static string GetTypeName( Type type )
        {
            var underlyingType = Nullable.GetUnderlyingType( type );

            if( underlyingType != null )
            {
                return underlyingType.Name;
            }

            return type.Name;
        }

        /// <summary>
        /// Determines if the given type is nullable.
        /// </summary>
        /// <param name="type">Type.</param>
        /// <returns>True if the type is nullable.</returns>
        public static bool IsNullable( Type type )
        {
            var underlyingType = Nullable.GetUnderlyingType( type );

            if( underlyingType != null )
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets a description for the type by first attempting to get it from a [Description] attribute
        /// and falling back to the summary node of any XML documentation.
        /// </summary>
        /// <param name="type">Type to get a description for.</param>
        /// <returns>Description text.</returns>
        public static string GetDescription( Type type )
        {
            var descriptionAttribute = type.GetCustomAttributes( typeof( DescriptionAttribute ), true ).FirstOrDefault();

            if( descriptionAttribute != null )
            {
                return ( (DescriptionAttribute)descriptionAttribute ).Description;
            }

            return XmlDocumentationHelper.GetSummary( XmlDocumentationHelper.GetXmlDocumentation( type ) );
        }

        /// <summary>
        /// Gets a description for the member by first attempting to get it from a [Description] attribute
        /// and falling back to the summary node of any XML documentation.
        /// </summary>
        /// <param name="methodInfo">Member to get a description for.</param>
        /// <returns>Description text.</returns>
        public static string GetDescription( MethodInfo methodInfo )
        {
            var descriptionAttribute = methodInfo.GetCustomAttributes( typeof( DescriptionAttribute ), true ).FirstOrDefault();

            if( descriptionAttribute != null )
            {
                return ( (DescriptionAttribute)descriptionAttribute ).Description;
            }

            return XmlDocumentationHelper.GetSummary( XmlDocumentationHelper.GetXmlDocumentation( methodInfo ) );
        }

        /// <summary>
        /// Gets a description for the member by first attempting to get it from a [Description] attribute
        /// and falling back to the summary node of any XML documentation.
        /// </summary>
        /// <param name="fieldInfo">Member to get a description for.</param>
        /// <returns>Description text.</returns>
        public static string GetDescription( FieldInfo fieldInfo )
        {
            var descriptionAttribute = fieldInfo.GetCustomAttributes( typeof( DescriptionAttribute ), true ).FirstOrDefault();

            if( descriptionAttribute != null )
            {
                return ( (DescriptionAttribute)descriptionAttribute ).Description;
            }

            return XmlDocumentationHelper.GetSummary( XmlDocumentationHelper.GetXmlDocumentation( fieldInfo ) );
        }

        /// <summary>
        /// Gets a description for the member by first attempting to get it from a [Description] attribute
        /// and falling back to the summary node of any XML documentation.
        /// </summary>
        /// <param name="propertyInfo">Member to get a description for.</param>
        /// <returns>Description text.</returns>
        public static string GetDescription( PropertyInfo propertyInfo )
        {
            var descriptionAttribute = propertyInfo.GetCustomAttributes( typeof( DescriptionAttribute ), true ).FirstOrDefault();

            if( descriptionAttribute != null )
            {
                return ( (DescriptionAttribute)descriptionAttribute ).Description;
            }

            return XmlDocumentationHelper.GetSummary( XmlDocumentationHelper.GetXmlDocumentation( propertyInfo ) );
        }
    }

    public class ApiMetadata
    {
        public string ApiName;

        public string ApiDescription;

        public List<MethodMetadata> Methods = new List<MethodMetadata>();

        public List<ModelMetadata> Models = new List<ModelMetadata>();
    }

    public class MethodMetadata
    {
        public string MethodName;

        public string MethodDescription;

        public List<dynamic> InputParameters = new List<dynamic>();

        public string ReturnParameterType;
    }

    public class ModelMetadata
    {
        public string ModelType;

        public string ModelDescription;

        public List<dynamic> Properties = new List<dynamic>();
    }
}