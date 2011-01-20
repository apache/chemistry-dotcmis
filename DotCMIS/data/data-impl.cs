/*
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DotCMIS.Data.Extensions;
using DotCMIS.Enums;

namespace DotCMIS.Data
{
    public class RepositoryInfo : ExtensionsData, IRepositoryInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string VendorName { get; set; }
        public string ProductName { get; set; }
        public string ProductVersion { get; set; }
        public string RootFolderId { get; set; }
        public IRepositoryCapabilities Capabilities { get; set; }
        public IAclCapabilities AclCapabilities { get; set; }
        public string LatestChangeLogToken { get; set; }
        public string CmisVersionSupported { get; set; }
        public string ThinClientUri { get; set; }
        public bool? ChangesIncomplete { get; set; }
        public IList<BaseTypeId?> ChangesOnType { get; set; }
        public string PrincipalIdAnonymous { get; set; }
        public string PrincipalIdAnyone { get; set; }
    }

    public class RepositoryCapabilities : ExtensionsData, IRepositoryCapabilities
    {
        public CapabilityContentStreamUpdates? ContentStreamUpdatesCapability { get; set; }
        public CapabilityChanges? ChangesCapability { get; set; }
        public CapabilityRenditions? RenditionsCapability { get; set; }
        public bool? IsGetDescendantsSupported { get; set; }
        public bool? IsGetFolderTreeSupported { get; set; }
        public bool? IsMultifilingSupported { get; set; }
        public bool? IsUnfilingSupported { get; set; }
        public bool? IsVersionSpecificFilingSupported { get; set; }
        public bool? IsPwcSearchableSupported { get; set; }
        public bool? IsPwcUpdatableSupported { get; set; }
        public bool? IsAllVersionsSearchableSupported { get; set; }
        public CapabilityQuery? QueryCapability { get; set; }
        public CapabilityJoin? JoinCapability { get; set; }
        public CapabilityAcl? AclCapability { get; set; }
    }

    public class AclCapabilities : ExtensionsData, IAclCapabilities
    {
        public SupportedPermissions? SupportedPermissions { get; set; }
        public AclPropagation? AclPropagation { get; set; }
        public IList<IPermissionDefinition> Permissions { get; set; }
        public IDictionary<string, IPermissionMapping> PermissionMapping { get; set; }
    }

    public class PermissionDefinition : ExtensionsData, IPermissionDefinition
    {
        public string Id { get; set; }
        public string Description { get; set; }
    }

    public class PermissionMapping : ExtensionsData, IPermissionMapping
    {
        public string Key { get; set; }
        public IList<string> Permissions { get; set; }
    }

    public abstract class TypeDefinition : ExtensionsData, ITypeDefinition
    {
        private List<IPropertyDefinition> propertyDefintionList = new List<IPropertyDefinition>();
        private Dictionary<string, IPropertyDefinition> propertyDefintionDict = new Dictionary<string, IPropertyDefinition>();

        public string Id { get; set; }
        public string LocalName { get; set; }
        public string LocalNamespace { get; set; }
        public string DisplayName { get; set; }
        public string QueryName { get; set; }
        public string Description { get; set; }
        public BaseTypeId? BaseTypeId { get; set; }
        public string ParentTypeId { get; set; }
        public bool? IsCreatable { get; set; }
        public bool? IsFileable { get; set; }
        public bool? IsQueryable { get; set; }
        public bool? IsFulltextIndexed { get; set; }
        public bool? IsIncludedInSupertypeQuery { get; set; }
        public bool? IsControllablePolicy { get; set; }
        public bool? IsControllableAcl { get; set; }
        public IPropertyDefinition this[string propertyId]
        {
            get
            {
                IPropertyDefinition propertyDefinition = null;
                propertyDefintionDict.TryGetValue(propertyId, out propertyDefinition);
                return propertyDefinition;
            }
        }
        public IList<IPropertyDefinition> PropertyDefintions
        {
            get
            {
                return propertyDefintionList;
            }
        }

        public void AddPropertyDefinition(IPropertyDefinition propertyDefinition)
        {
            if (propertyDefinition == null || propertyDefinition.Id == null)
            {
                return;
            }

            propertyDefintionList.Add(propertyDefinition);
            propertyDefintionDict[propertyDefinition.Id] = propertyDefinition;
        }
    }

    public class DocumentTypeDefinition : TypeDefinition, IDocumentTypeDefinition
    {
        public bool? IsVersionable { get; set; }
        public ContentStreamAllowed? ContentStreamAllowed { get; set; }
    }

    public class FolderTypeDefinition : TypeDefinition, IFolderTypeDefinition
    {
    }

    public class PolicyTypeDefinition : TypeDefinition, IPolicyTypeDefinition
    {
    }

    public class RelationshipTypeDefinition : TypeDefinition, IRelationshipTypeDefinition
    {
        public IList<string> AllowedSourceTypeIds { get; set; }
        public IList<string> AllowedTargetTypeIds { get; set; }
    }

    public class TypeDefinitionList : ExtensionsData, ITypeDefinitionList
    {
        public IList<ITypeDefinition> List { get; set; }
        public bool? HasMoreItems { get; set; }
        public long? NumItems { get; set; }
    }

    public class TypeDefinitionContainer : ExtensionsData, ITypeDefinitionContainer
    {
        public ITypeDefinition TypeDefinition { get; set; }
        public IList<ITypeDefinitionContainer> Children { get; set; }
    }

    public abstract class PropertyDefinition : ExtensionsData, IPropertyDefinition
    {
        public string Id { get; set; }
        public string LocalName { get; set; }
        public string LocalNamespace { get; set; }
        public string DisplayName { get; set; }
        public string QueryName { get; set; }
        public string Description { get; set; }
        public PropertyType? PropertyType { get; set; }
        public Cardinality? Cardinality { get; set; }
        public Updatability? Updatability { get; set; }
        public bool? IsInherited { get; set; }
        public bool? IsRequired { get; set; }
        public bool? IsQueryable { get; set; }
        public bool? IsOrderable { get; set; }
        public bool? IsOpenChoice { get; set; }
    }

    public class Choice<T> : IChoice<T>
    {
        public string DisplayName { get; set; }
        public IList<T> Value { get; set; }
        public IList<IChoice<T>> Choices { get; set; }
    }

    public class PropertyBooleanDefinition : PropertyDefinition, IPropertyBooleanDefinition
    {
        public IList<bool> DefaultValue { get; set; }
        public IList<IChoice<bool>> Choices { get; set; }
    }

    public class PropertyDateTimeDefinition : PropertyDefinition, IPropertyDateTimeDefinition
    {
        public IList<DateTime> DefaultValue { get; set; }
        public IList<IChoice<DateTime>> Choices { get; set; }
        public DateTimeResolution? DateTimeResolution { get; set; }
    }

    public class PropertyDecimalDefinition : PropertyDefinition, IPropertyDecimalDefinition
    {
        public IList<decimal> DefaultValue { get; set; }
        public IList<IChoice<decimal>> Choices { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public DecimalPrecision? Precision { get; set; }
    }

    public class PropertyHtmlDefinition : PropertyDefinition, IPropertyHtmlDefinition
    {
        public IList<string> DefaultValue { get; set; }
        public IList<IChoice<string>> Choices { get; set; }
    }

    public class PropertyIdDefinition : PropertyDefinition, IPropertyIdDefinition
    {
        public IList<string> DefaultValue { get; set; }
        public IList<IChoice<string>> Choices { get; set; }
    }

    public class PropertyIntegerDefinition : PropertyDefinition, IPropertyIntegerDefinition
    {
        public IList<long> DefaultValue { get; set; }
        public IList<IChoice<long>> Choices { get; set; }
        public long? MinValue { get; set; }
        public long? MaxValue { get; set; }
    }

    public class PropertyStringDefinition : PropertyDefinition, IPropertyStringDefinition
    {
        public IList<string> DefaultValue { get; set; }
        public IList<IChoice<string>> Choices { get; set; }
        public long? MaxLength { get; set; }
    }

    public class PropertyUriDefinition : PropertyDefinition, IPropertyUriDefinition
    {
        public IList<string> DefaultValue { get; set; }
        public IList<IChoice<string>> Choices { get; set; }
    }

    public class ObjectData : ExtensionsData, IObjectData
    {
        public string Id
        {
            get
            {
                return GetFirstValue(PropertyIds.ObjectId) as string;
            }
        }
        public BaseTypeId? BaseTypeId
        {
            get
            {
                string baseTypeId = GetFirstValue(PropertyIds.BaseTypeId) as string;
                if (baseTypeId == null)
                {
                    return null;
                }

                return baseTypeId.GetCmisEnum<BaseTypeId>();
            }
        }
        public IProperties Properties { get; set; }
        public IAllowableActions AllowableActions { get; set; }
        public IList<IObjectData> Relationships { get; set; }
        public IChangeEventInfo ChangeEventInfo { get; set; }
        public IAcl Acl { get; set; }
        public bool? IsExactAcl { get; set; }
        public IPolicyIdList PolicyIds { get; set; }
        public IList<IRenditionData> Renditions { get; set; }

        private object GetFirstValue(string id)
        {
            if (Properties == null) { return null; }

            IPropertyData property = Properties[id];
            if (property == null)
            {
                return null;
            }

            return property.FirstObject;
        }
    }

    public class ObjectList : ExtensionsData, IObjectList
    {
        public IList<IObjectData> Objects { get; set; }
        public bool? HasMoreItems { get; set; }
        public long? NumItems { get; set; }
    }

    public class ObjectInFolderData : ExtensionsData, IObjectInFolderData
    {
        public IObjectData Object { get; set; }
        public string PathSegment { get; set; }
    }

    public class ObjectInFolderList : ExtensionsData, IObjectInFolderList
    {
        public IList<IObjectInFolderData> Objects { get; set; }
        public bool? HasMoreItems { get; set; }
        public long? NumItems { get; set; }
    }

    public class ObjectInFolderContainer : ExtensionsData, IObjectInFolderContainer
    {
        public IObjectInFolderData Object { get; set; }
        public IList<IObjectInFolderContainer> Children { get; set; }
    }

    public class ObjectParentData : ExtensionsData, IObjectParentData
    {
        public IObjectData Object { get; set; }
        public string RelativePathSegment { get; set; }
    }

    public class Properties : ExtensionsData, IProperties
    {
        private List<IPropertyData> propertyList = new List<IPropertyData>();
        private Dictionary<string, IPropertyData> propertyDict = new Dictionary<string, IPropertyData>();

        public IPropertyData this[string propertyId]
        {
            get
            {
                IPropertyData property = null;
                propertyDict.TryGetValue(propertyId, out property);
                return property;
            }
        }

        public IList<IPropertyData> PropertyList
        {
            get
            {
                return propertyList;
            }
        }

        public void AddProperty(IPropertyData property)
        {
            if (property == null)
            {
                return;
            }

            propertyList.Add(property);

            if (property.Id != null)
            {
                propertyDict[property.Id] = property;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (IPropertyData prop in propertyList)
            {
                if (sb.Length == 0) { sb.Append("["); }
                else { sb.Append(", "); }
                sb.Append(prop.ToString());
            }
            sb.Append("]");

            return sb.ToString();
        }
    }

    public abstract class PropertyData : ExtensionsData, IPropertyData
    {
        public string Id { get; set; }
        public string LocalName { get; set; }
        public string DisplayName { get; set; }
        public string QueryName { get; set; }

        public object FirstObject
        {
            get
            {
                if (this is PropertyBoolean)
                {
                    return ((PropertyBoolean)this).FirstValue;
                }
                else if (this is PropertyDateTime)
                {
                    return ((PropertyDateTime)this).FirstValue;
                }
                else if (this is PropertyDecimal)
                {
                    return ((PropertyDecimal)this).FirstValue;
                }
                else if (this is PropertyHtml)
                {
                    return ((PropertyHtml)this).FirstValue;
                }
                else if (this is PropertyId)
                {
                    return ((PropertyId)this).FirstValue;
                }
                else if (this is PropertyInteger)
                {
                    return ((PropertyInteger)this).FirstValue;
                }
                else if (this is PropertyString)
                {
                    return ((PropertyString)this).FirstValue;
                }
                else if (this is PropertyUri)
                {
                    return ((PropertyUri)this).FirstValue;
                }

                return null;
            }
        }

        public override string ToString()
        {
            return Id + ": " + FirstObject;
        }
    }

    public class PropertyBoolean : PropertyData, IPropertyBoolean
    {
        public IList<bool> Values { get; set; }
        public bool? FirstValue { get { return Values == null || Values.Count < 1 ? null : (bool?)Values[0]; } }
    }

    public class PropertyDateTime : PropertyData, IPropertyDateTime
    {
        public IList<DateTime> Values { get; set; }
        public DateTime? FirstValue { get { return Values == null || Values.Count < 1 ? null : (DateTime?)Values[0]; } }
    }

    public class PropertyDecimal : PropertyData, IPropertyDecimal
    {
        public IList<decimal> Values { get; set; }
        public decimal? FirstValue { get { return Values == null || Values.Count < 1 ? null : (decimal?)Values[0]; } }
    }

    public class PropertyHtml : PropertyData, IPropertyHtml
    {
        public IList<string> Values { get; set; }
        public string FirstValue { get { return Values == null || Values.Count < 1 ? null : Values[0]; } }
    }

    public class PropertyId : PropertyData, IPropertyId
    {
        public IList<string> Values { get; set; }
        public string FirstValue { get { return Values == null || Values.Count < 1 ? null : Values[0]; } }
    }

    public class PropertyInteger : PropertyData, IPropertyInteger
    {
        public IList<long> Values { get; set; }
        public long? FirstValue { get { return Values == null || Values.Count < 1 ? null : (long?)Values[0]; } }
    }

    public class PropertyString : PropertyData, IPropertyString
    {
        public IList<string> Values { get; set; }
        public string FirstValue { get { return Values == null || Values.Count < 1 ? null : Values[0]; } }
    }

    public class PropertyUri : PropertyData, IPropertyUri
    {
        public IList<string> Values { get; set; }
        public string FirstValue { get { return Values == null || Values.Count < 1 ? null : Values[0]; } }
    }

    public class Principal : ExtensionsData, IPrincipal
    {
        public string Id { get; set; }
    }

    public class Ace : ExtensionsData, IAce
    {
        public IPrincipal Principal { get; set; }
        public string PrincipalId { get { return Principal == null ? null : Principal.Id; } }
        public IList<string> Permissions { get; set; }
        public bool IsDirect { get; set; }
    }

    public class Acl : ExtensionsData, IAcl
    {
        public IList<IAce> Aces { get; set; }
        public bool? IsExact { get; set; }
    }

    public class ContentStream : ExtensionsData, IContentStream
    {
        public long? Length { get; set; }
        public string MimeType { get; set; }
        public string FileName { get; set; }
        public Stream Stream { get; set; }
    }

    public class AllowableActions : ExtensionsData, IAllowableActions
    {
        public HashSet<string> Actions { get; set; }
    }

    public class RenditionData : ExtensionsData, IRenditionData
    {
        public string StreamId { get; set; }
        public string MimeType { get; set; }
        public long? Length { get; set; }
        public string Kind { get; set; }
        public string Title { get; set; }
        public long? Height { get; set; }
        public long? Width { get; set; }
        public string RenditionDocumentId { get; set; }
    }

    public class ChangeEventInfo : ExtensionsData, IChangeEventInfo
    {
        public ChangeType ChangeType { get; set; }
        public DateTime ChangeTime { get; set; }
    }

    public class PolicyIdList : ExtensionsData, IPolicyIdList
    {
        public IList<string> PolicyIds { get; set; }
    }

    public class FailedToDeleteData : ExtensionsData, IFailedToDeleteData
    {
        public IList<string> Ids { get; set; }
    }
}
