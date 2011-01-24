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
using System.Linq;
using System.Text;
using DotCMIS.CMISWebServicesReference;
using System.IO;
using DotCMIS.Exceptions;
using System.Xml;
using System.Globalization;
using System.Xml.Serialization;
using DotCMIS.Enums;

namespace DotCMIS.Binding.AtomPub
{
    internal class AtomWriter
    {
        public static XmlSerializer ObjectSerializer;
        public static XmlSerializer AclSerializer;

        static AtomWriter()
        {
            XmlRootAttribute objectXmlRoot = new XmlRootAttribute("object");
            objectXmlRoot.Namespace = AtomPubConstants.NamespaceRestAtom;
            ObjectSerializer = new XmlSerializer(typeof(cmisObjectType), objectXmlRoot);

            XmlRootAttribute aclXmlRoot = new XmlRootAttribute("acl");
            aclXmlRoot.Namespace = AtomPubConstants.NamespaceRestAtom;
            AclSerializer = new XmlSerializer(typeof(cmisAccessControlListType), objectXmlRoot);
        }

        public const string PrefixAtom = "atom";
        public const string PrefixCMIS = "cmis";
        public const string PrefixRestAtom = "cmisra";
    }

    internal class AtomEntryWriter
    {
        private const int BufferSize = 64 * 1024;

        private cmisObjectType cmisObject;
        private Stream stream;
        private string mediaType;

        public AtomEntryWriter(cmisObjectType cmisObject)
            : this(cmisObject, null, null)
        {
        }

        public AtomEntryWriter(cmisObjectType cmisObject, string mediaType, Stream stream)
        {
            if (cmisObject == null || cmisObject.properties == null)
            {
                throw new CmisInvalidArgumentException("Object and properties must not be null!");
            }

            if (stream != null && mediaType == null)
            {
                throw new CmisInvalidArgumentException("Media type must be set if a stream is present!");
            }

            this.cmisObject = cmisObject;
            this.mediaType = mediaType;
            this.stream = stream;
        }

        public void Write(Stream outStream)
        {
            using (XmlWriter writer = XmlWriter.Create(outStream))
            {
                // start doc
                writer.WriteStartDocument();

                // start entry
                writer.WriteStartElement(AtomWriter.PrefixAtom, AtomPubConstants.TagEntry, AtomPubConstants.NamespaceAtom);
                writer.WriteAttributeString("xmlns", AtomWriter.PrefixAtom, null, AtomPubConstants.NamespaceAtom);
                writer.WriteAttributeString("xmlns", AtomWriter.PrefixCMIS, null, AtomPubConstants.NamespaceCMIS);
                writer.WriteAttributeString("xmlns", AtomWriter.PrefixRestAtom, null, AtomPubConstants.NamespaceRestAtom);

                // atom:id
                writer.WriteStartElement(AtomWriter.PrefixAtom, AtomPubConstants.TagAtomId, AtomPubConstants.NamespaceAtom);
                writer.WriteString("urn:uuid:00000000-0000-0000-0000-00000000000");
                writer.WriteEndElement();

                // atom:title
                writer.WriteStartElement(AtomWriter.PrefixAtom, AtomPubConstants.TagAtomTitle, AtomPubConstants.NamespaceAtom);
                writer.WriteString(GetTitle());
                writer.WriteEndElement();

                // atom:updated
                writer.WriteStartElement(AtomWriter.PrefixAtom, AtomPubConstants.TagAtomUpdated, AtomPubConstants.NamespaceAtom);
                writer.WriteString(GetUpdated());
                writer.WriteEndElement();

                // content
                if (stream != null)
                {
                    writer.WriteStartElement(AtomWriter.PrefixRestAtom, AtomPubConstants.TagContent, AtomPubConstants.NamespaceRestAtom);

                    writer.WriteStartElement(AtomWriter.PrefixRestAtom, AtomPubConstants.TagContentMediatype, AtomPubConstants.NamespaceRestAtom);
                    writer.WriteString(mediaType);
                    writer.WriteEndElement();

                    writer.WriteStartElement(AtomWriter.PrefixRestAtom, AtomPubConstants.TagContentBase64, AtomPubConstants.NamespaceRestAtom);
                    WriteContent(writer);
                    writer.WriteEndElement();

                    writer.WriteEndElement();
                }

                // object
                AtomWriter.ObjectSerializer.Serialize(writer, cmisObject);

                // end entry
                writer.WriteEndElement();

                // end document
                writer.WriteEndDocument();

                writer.Flush();
            }
        }

        // ---- internal ----

        private string GetTitle()
        {
            string result = "";

            foreach (cmisProperty property in cmisObject.properties.Items)
            {
                if (PropertyIds.Name == property.propertyDefinitionId && property is cmisPropertyString)
                {
                    string[] values = ((cmisPropertyString)property).value;
                    if (values != null && values.Length > 0)
                    {
                        return values[0];
                    }
                }
            }

            return result;
        }

        private string GetUpdated()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        }

        private void WriteContent(XmlWriter writer)
        {
            byte[] byteArray = null;

            if (stream is MemoryStream)
            {
                byteArray = ((MemoryStream)stream).ToArray();
            }
            else
            {
                MemoryStream memStream = new MemoryStream();
                byte[] buffer = new byte[BufferSize];
                int bytes;
                while ((bytes = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    memStream.Write(buffer, 0, bytes);
                }
                byteArray = memStream.ToArray();
            }

            writer.WriteBase64(byteArray, 0, byteArray.Length);
        }
    }

    internal class AtomQueryWriter
    {
        private string statement;
        private bool? searchAllVersions;
        private bool? includeAllowableActions;
        private IncludeRelationshipsFlag? includeRelationships;
        private string renditionFilter;
        private long? maxItems;
        private long? skipCount;

        public AtomQueryWriter(string statement, bool? searchAllVersions,
            bool? includeAllowableActions, IncludeRelationshipsFlag? includeRelationships, string renditionFilter,
            long? maxItems, long? skipCount)
        {
            this.statement = statement;
            this.searchAllVersions = searchAllVersions;
            this.includeAllowableActions = includeAllowableActions;
            this.includeRelationships = includeRelationships;
            this.renditionFilter = renditionFilter;
            this.maxItems = maxItems;
            this.skipCount = skipCount;
        }

        public void Write(Stream outStream)
        {
            using (XmlWriter writer = XmlWriter.Create(outStream))
            {
                // start doc
                writer.WriteStartDocument();

                // start query
                writer.WriteStartElement(AtomWriter.PrefixCMIS, AtomPubConstants.TagQuery, AtomPubConstants.NamespaceCMIS);
                writer.WriteAttributeString("xmlns", AtomWriter.PrefixAtom, null, AtomPubConstants.NamespaceAtom);
                writer.WriteAttributeString("xmlns", AtomWriter.PrefixCMIS, null, AtomPubConstants.NamespaceCMIS);
                writer.WriteAttributeString("xmlns", AtomWriter.PrefixRestAtom, null, AtomPubConstants.NamespaceRestAtom);

                // cmis:statement
                writer.WriteStartElement(AtomWriter.PrefixCMIS, AtomPubConstants.TagStatement, AtomPubConstants.NamespaceCMIS);
                writer.WriteString(statement);
                writer.WriteEndElement();

                // cmis:searchAllVersions
                if (searchAllVersions.HasValue)
                {
                    writer.WriteStartElement(AtomWriter.PrefixCMIS, AtomPubConstants.TagSearchAllVersions, AtomPubConstants.NamespaceCMIS);
                    writer.WriteString(searchAllVersions.Value ? "true" : "false");
                    writer.WriteEndElement();
                }

                // cmis:includeAllowableActions
                if (includeAllowableActions.HasValue)
                {
                    writer.WriteStartElement(AtomWriter.PrefixCMIS, AtomPubConstants.TagIncludeAllowableActions, AtomPubConstants.NamespaceCMIS);
                    writer.WriteString(includeAllowableActions.Value ? "true" : "false");
                    writer.WriteEndElement();
                }

                // cmis:includeRelationships
                if (includeRelationships.HasValue)
                {
                    writer.WriteStartElement(AtomWriter.PrefixCMIS, AtomPubConstants.TagIncludeRelationships, AtomPubConstants.NamespaceCMIS);
                    writer.WriteString(includeRelationships.GetCmisValue());
                    writer.WriteEndElement();
                }

                // cmis:renditionFilter
                if (renditionFilter != null)
                {
                    writer.WriteStartElement(AtomWriter.PrefixCMIS, AtomPubConstants.TagRenditionFilter, AtomPubConstants.NamespaceCMIS);
                    writer.WriteString(renditionFilter);
                    writer.WriteEndElement();
                }

                // cmis:maxItems
                if (maxItems.HasValue)
                {
                    writer.WriteStartElement(AtomWriter.PrefixCMIS, AtomPubConstants.TagMaxItems, AtomPubConstants.NamespaceCMIS);
                    writer.WriteString(maxItems.ToString());
                    writer.WriteEndElement();
                }

                // cmis:skipCount
                if (skipCount.HasValue)
                {
                    writer.WriteStartElement(AtomWriter.PrefixCMIS, AtomPubConstants.TagSkipCount, AtomPubConstants.NamespaceCMIS);
                    writer.WriteString(skipCount.ToString());
                    writer.WriteEndElement();
                }

                // end query
                writer.WriteEndElement();

                // end document
                writer.WriteEndDocument();

                writer.Flush();
            }
        }
    }
}
