using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace Nano.Host
{
    /// <summary>
    /// Provides methods for extracting documentation for types, methods, properties, and fields from .NET XML comments.
    /// </summary>
    public static class XmlDocumentationHelper
    {
        /// <summary>
        /// Cache for storing XML documentation.
        /// </summary>
        public static readonly Dictionary<Assembly, XmlDocument> XmlDocumentCache = new Dictionary<Assembly, XmlDocument>();

        /// <summary>
        /// Gets the XML comments for the given assembly.
        /// </summary>
        /// <param name="assembly">Assembly to get XML documention for.</param>
        /// <returns>
        /// Returns null if the XML file for the assembly does not exist else returns 
        /// the documentation in the form of an XmlDocument.
        /// </returns>
        public static XmlDocument GetXmlDocumentation( this Assembly assembly )
        {
            if( assembly == null )
                throw new ArgumentNullException( "assembly", "The parameter 'assembly' must not be null." );

            if( XmlDocumentCache.ContainsKey( assembly ) )
                return XmlDocumentCache[assembly];

            string assemblyFilename = assembly.CodeBase;

            const string prefix = "file:///";

            if( string.IsNullOrWhiteSpace( assemblyFilename ) == false && assemblyFilename.StartsWith( prefix ) )
            {
                try
                {
                    var xmlDocumentationPath = Path.ChangeExtension( assemblyFilename.Substring( prefix.Length ), ".xml" );

                    if( File.Exists( xmlDocumentationPath ) )
                    {
                        var xmlDocument = new XmlDocument();

                        xmlDocument.Load( xmlDocumentationPath );

                        XmlDocumentCache.Add( assembly, xmlDocument );

                        return xmlDocument;
                    }
                }
                catch( Exception ex )
                {
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the XML comments for the given type in the form of an XmlElement.
        /// </summary>
        /// <param name="type">Type to get XML comments for.</param>
        /// <returns>
        /// Returns null if the XML file for the assembly does not exist or if comments
        /// for the given type do not exist else returns an XmlElement for the given type.
        /// </returns>
        public static XmlElement GetXmlDocumentation( this Type type )
        {
            if( type == null )
                throw new ArgumentNullException( "type", "The parameter 'type' must not be null." );

            var xmlDocument = type.Assembly.GetXmlDocumentation();

            if( xmlDocument == null )
                return null;

            var xmlMemberName = string.Format( "T:{0}", GetFullTypeName( type ) );

            var memberElement = xmlDocument.GetMemberByName( xmlMemberName );

            return memberElement;
        }

        /// <summary>
        /// Gets the XML comments for the given method in the form of an XmlElement.
        /// </summary>
        /// <param name="methodInfo">Method to get XML comments for.</param>
        /// <returns>
        /// Returns null if the XML file for the assembly does not exist or if comments 
        /// for the given method do not exist else returns an XmlElement representing the 
        /// comments for the given method.
        /// </returns>
        public static XmlElement GetXmlDocumentation( this MethodInfo methodInfo )
        {
            if( methodInfo == null )
                throw new ArgumentNullException( "methodInfo", "The parameter 'methodInfo' must not be null." );

            var declaryingType = methodInfo.DeclaringType;

            if( declaryingType == null )
                return null;

            var xmlDocument = declaryingType.Assembly.GetXmlDocumentation();

            if( xmlDocument == null )
                return null;

            string parameterList = "";

            foreach( ParameterInfo parameterInfo in methodInfo.GetParameters().OrderBy( x => x.Position ) )
            {
                if( parameterList.Length > 0 )
                {
                    parameterList += ",";
                }

                parameterList += GetParameterTypeName( methodInfo, parameterInfo );
            }

            var genericArguments = methodInfo.GetGenericArguments();

            string xmlMethodName = string.Format( "M:{0}.{1}{2}{3}", GetFullTypeName( methodInfo.DeclaringType ), methodInfo.Name, genericArguments.Length > 0 ? string.Format( "``{0}", genericArguments.Length ) : "", parameterList.Length > 0 ? string.Format( "({0})", parameterList ) : "" );

            var memberElement = xmlDocument.GetMemberByName( xmlMethodName );

            return memberElement;
        }

        /// <summary>
        /// Gets the XML comments for the given field in the form of an XmlElement.
        /// </summary>
        /// <param name="fieldInfo">Field to get XML comments for.</param>
        /// <returns>
        /// Returns null if the XML file for the assembly does not exist or if comments for
        /// the given field do not exist else returns an XmlElement representing the comments 
        /// for the given field.
        /// </returns>
        public static XmlElement GetXmlDocumentation( this FieldInfo fieldInfo )
        {
            if( fieldInfo == null )
                throw new ArgumentNullException( "fieldInfo", "The parameter 'fieldInfo' must not be null." );

            var declaryingType = fieldInfo.DeclaringType;

            if( declaryingType == null )
                return null;

            var xmlDocument = declaryingType.Assembly.GetXmlDocumentation();

            if( xmlDocument == null )
                return null;

            var xmlPropertyName = string.Format( "F:{0}.{1}", GetFullTypeName( declaryingType ), fieldInfo.Name );

            var memberElement = xmlDocument.GetMemberByName( xmlPropertyName );

            return memberElement;
        }

        /// <summary>
        /// Gets the XML comments for the given property in the form of an XmlElement.
        /// </summary>
        /// <param name="propertyInfo">Property to get XML comments for.</param>
        /// <returns>
        /// Returns null if the XML file for the assembly does not exist or if comments for
        /// the given property do not exist else returns an XmlElement representing the comments 
        /// for the given property.
        /// </returns>
        public static XmlElement GetXmlDocumentation( this PropertyInfo propertyInfo )
        {
            if( propertyInfo == null )
                throw new ArgumentNullException( "propertyInfo", "The parameter 'propertyInfo' must not be null." );

            var declaryingType = propertyInfo.DeclaringType;

            if( declaryingType == null )
                return null;

            var xmlDocument = declaryingType.Assembly.GetXmlDocumentation();

            if( xmlDocument == null )
                return null;

            var xmlPropertyName = string.Format( "P:{0}.{1}", GetFullTypeName( declaryingType ), propertyInfo.Name );

            var memberElement = xmlDocument.GetMemberByName( xmlPropertyName );

            return memberElement;
        }

        /// <summary>
        /// Gets the XML comments for the given member in the form of an XmlElement.
        /// </summary>
        /// <remarks>
        /// This method attempts to generically handle any reflected members that do not have an
        /// explicit GetXmlDocumentation method for the member type. This has been tested to work 
        /// with PropertyInfo, FieldInfo, and EventInfo but may not work for all member types.
        /// </remarks>
        /// <param name="memberInfo">Member to get XML comments for.</param>
        /// <returns>
        /// Returns null if the XML file for the assembly does not exist or if comments for
        /// the given member do not exist else returns an XmlElement representing the comments 
        /// for the given member.
        /// </returns>
        public static XmlElement GetXmlDocumentation( this MemberInfo memberInfo )
        {
            if( memberInfo == null )
                throw new ArgumentNullException( "memberInfo", "The parameter 'memberInfo' must not be null." );

            var declaryingType = memberInfo.DeclaringType;

            if( declaryingType == null )
                return null;

            var xmlDocument = declaryingType.Assembly.GetXmlDocumentation();

            if( xmlDocument == null )
                return null;

            var xmlPropertyName = string.Format( "{0}:{1}.{2}", memberInfo.MemberType.ToString()[0], GetFullTypeName( declaryingType ), memberInfo.Name );

            var memberElement = xmlDocument.GetMemberByName( xmlPropertyName );

            return memberElement;
        }

        /// <summary>
        /// Gets the parameter type name used in XML documentation.
        /// </summary>
        /// <remarks>
        /// This method is needed due to how nullable type names and generic parameters
        /// are formatted in XML documentation.
        /// </remarks>
        /// <param name="methodInfo">MethodInfo where the parameter is implemented.</param>
        /// <param name="parameterInfo">ParameterInfo to get the parameter type name from.</param>
        /// <returns>Parameter type name.</returns>
        public static string GetParameterTypeName( this MethodInfo methodInfo, ParameterInfo parameterInfo )
        {
            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType( parameterInfo.ParameterType );

            if( underlyingType != null )
            {
                return string.Format( "System.Nullable{{{0}}}", GetFullTypeName( underlyingType ) );
            }

            var parameterTypeFullName = GetFullTypeName( parameterInfo.ParameterType );

            // Handle generic types
            if( string.IsNullOrWhiteSpace( parameterTypeFullName ) )
            {
                var typeName = parameterInfo.ParameterType.Name;

                var genericParameter = methodInfo.GetGenericArguments().FirstOrDefault( x => x.Name == typeName );

                if( genericParameter != null )
                {
                    var genericParameterPosition = genericParameter.GenericParameterPosition;

                    return "``" + genericParameterPosition;
                }
            }

            return parameterTypeFullName;
        }

        /// <summary>
        /// Gets the full type name.
        /// </summary>
        /// <remarks>
        /// This method is needed in order to replace a nested types '+' plus sign with a '.' dot.
        /// </remarks>
        /// <param name="type">Type.</param>
        /// <returns>Full type name.</returns>
        public static string GetFullTypeName( Type type )
        {
            var parameterTypeFullName = type.FullName;

            if( string.IsNullOrWhiteSpace( parameterTypeFullName ) == false )
            {
                return parameterTypeFullName.Replace( "+", "." );
            }

            return null;
        }

        /// <summary>
        /// Gets a member by name.
        /// </summary>
        /// <param name="xmlDocument">XmlDocument to search for the member in.</param>
        /// <param name="memberName">Member name to search for.</param>
        /// <returns>
        /// Returns null if the member is not found or returns an XmlElement representing the found member.
        /// </returns>
        public static XmlElement GetMemberByName( this XmlDocument xmlDocument, string memberName )
        {
            if( xmlDocument == null )
                return null;

            if( string.IsNullOrWhiteSpace( memberName ) )
                throw new ArgumentNullException( "memberName", "The parameter 'memberName' must not be null." );

            var docElement = xmlDocument["doc"];

            if( docElement == null )
                return null;

            var membersElement = docElement["members"];

            if( membersElement == null )
                return null;

            foreach( XmlElement member in membersElement )
            {
                if( member == null )
                    continue;

                if( member.Attributes["name"].Value == memberName )
                {
                    return member;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the summary text.
        /// </summary>
        /// <param name="xmlElement">XmlElement.</param>
        /// <returns>Summary text.</returns>
        public static string GetSummary( this XmlElement xmlElement )
        {
            var summary = xmlElement.GetNodeText( "summary" );

            return summary;
        }

        /// <summary>
        /// Gets the remarks text.
        /// </summary>
        /// <param name="xmlElement">XmlElement.</param>
        /// <returns>Remarks text.</returns>
        public static string GetRemarks( this XmlElement xmlElement )
        {
            var summary = xmlElement.GetNodeText( "remarks" );

            return summary;
        }

        /// <summary>
        /// Gets the description text for a parameter.
        /// </summary>
        /// <param name="xmlElement">XmlElement.</param>
        /// <param name="parameterName">Parameter name.</param>
        /// <returns>Description text.</returns>
        public static string GetParameterDescription( this XmlElement xmlElement, string parameterName )
        {
            var summary = xmlElement.GetNodeText( string.Format( "param[@name='{0}']", parameterName ) );

            return summary;
        }

        /// <summary>
        /// Gets the node text from the given XmlElement.
        /// </summary>
        /// <param name="xmlElement">XmlElement.</param>
        /// <param name="xpath">XPath to locate the node.</param>
        /// <returns>Node text.</returns>
        public static string GetNodeText( this XmlElement xmlElement, string xpath )
        {
            if( xmlElement == null )
                return null;

            if( string.IsNullOrWhiteSpace( xpath ) )
                return null;

            var summaryNode = xmlElement.SelectSingleNode( xpath );

            if( summaryNode == null )
                return null;

            var summary = FormatXmlInnerText( summaryNode.InnerText );

            return summary;
        }

        /// <summary>
        /// Formats XML inner text.
        /// </summary>
        /// <remarks>
        /// This method attempts to remove excessive whitespace yet maintain line breaks.
        /// </remarks>
        /// <param name="xmlInnerText">XML inner text to format.</param>
        /// <returns>Formatted text.</returns>
        public static string FormatXmlInnerText( string xmlInnerText )
        {
            if( string.IsNullOrWhiteSpace( xmlInnerText ) )
                return xmlInnerText;

            var lines = xmlInnerText.Trim().Replace( "\r", "" ).Split( new[] { "\n" }, StringSplitOptions.None );

            string formattedText = "";

            foreach( var line in lines )
            {
                if( formattedText.Length > 0 )
                    formattedText += "\n";

                formattedText += line.Trim();
            }

            return formattedText;
        }
    }
}