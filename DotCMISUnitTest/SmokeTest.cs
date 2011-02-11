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
using System.Collections.Generic;
using DotCMIS;
using DotCMIS.Client;
using DotCMIS.Client.Impl;
using DotCMIS.Enums;
using NUnit.Framework;
using System;
using DotCMIS.Data;
using DotCMIS.Data.Impl;
using System.Text;
using System.IO;
using DotCMIS.Exceptions;

namespace DotCMISUnitTest
{
    [TestFixture]
    class SmokeTest : TestFramework
    {
        [Test]
        public void SmokeTestSession()
        {
            Assert.NotNull(Session);
            Assert.NotNull(Session.Binding);
            Assert.NotNull(Session.RepositoryInfo);
            Assert.NotNull(Session.RepositoryInfo.Id);
            Assert.NotNull(Session.RepositoryInfo.RootFolderId);
            Assert.NotNull(Session.DefaultContext);
            Assert.NotNull(Session.ObjectFactory);

            Assert.AreEqual("test", Session.CreateObjectId("test").Id);
        }

        [Test]
        public void SmokeTestTypes()
        {
            // getTypeDefinition
            IObjectType documentType = Session.GetTypeDefinition("cmis:document");
            Assert.NotNull(documentType);
            Assert.True(documentType is DocumentType);
            Assert.AreEqual("cmis:document", documentType.Id);
            Assert.AreEqual(BaseTypeId.CmisDocument, documentType.BaseTypeId);
            Assert.True(documentType.IsBaseType);
            Assert.Null(documentType.ParentTypeId);
            Assert.NotNull(documentType.PropertyDefintions);
            Assert.True(documentType.PropertyDefintions.Count >= 9);

            IObjectType folderType = Session.GetTypeDefinition("cmis:folder");
            Assert.NotNull(folderType);
            Assert.True(folderType is FolderType);
            Assert.AreEqual("cmis:folder", folderType.Id);
            Assert.AreEqual(BaseTypeId.CmisFolder, folderType.BaseTypeId);
            Assert.True(folderType.IsBaseType);
            Assert.Null(folderType.ParentTypeId);
            Assert.NotNull(folderType.PropertyDefintions);
            Assert.True(folderType.PropertyDefintions.Count >= 9);

            // getTypeChildren
            Session.Clear();

            IItemEnumerable<IObjectType> children = Session.GetTypeChildren(null, true);
            Assert.NotNull(children);

            int count;
            count = 0;
            foreach (IObjectType type in children)
            {
                Assert.NotNull(type);
                Assert.NotNull(type.Id);
                Assert.True(type.IsBaseType);
                Assert.Null(type.ParentTypeId);
                Assert.NotNull(type.PropertyDefintions);

                Session.Clear();
                IObjectType type2 = Session.GetTypeDefinition(type.Id);
                AssertAreEqual(type, type2);

                Session.GetTypeChildren(type.Id, true);

                count++;
            }

            Assert.True(count >= 2);
            Assert.True(count <= 4);

            // getTypeDescendants
            Session.Clear();

            IList<ITree<IObjectType>> descendants = Session.GetTypeDescendants(null, -1, true);

            count = 0;
            foreach (ITree<IObjectType> tree in descendants)
            {
                Assert.NotNull(tree);
                Assert.NotNull(tree.Item);

                IObjectType type = tree.Item;
                Assert.NotNull(type);
                Assert.NotNull(type.Id);
                Assert.True(type.IsBaseType);
                Assert.Null(type.ParentTypeId);
                Assert.NotNull(type.PropertyDefintions);

                Session.Clear();
                IObjectType type2 = Session.GetTypeDefinition(type.Id);
                AssertAreEqual(type, type2);

                Session.GetTypeDescendants(type.Id, 2, true);

                count++;
            }

            Assert.True(count >= 2);
            Assert.True(count <= 4);
        }

        [Test]
        public void SmokeTestRootFolder()
        {
            ICmisObject rootFolderObject = Session.GetRootFolder();

            Assert.NotNull(rootFolderObject);
            Assert.NotNull(rootFolderObject.Id);
            Assert.True(rootFolderObject is IFolder);

            IFolder rootFolder = (IFolder)rootFolderObject;

            Assert.AreEqual("/", rootFolder.Path);
            Assert.AreEqual(1, rootFolder.Paths.Count);

            Assert.NotNull(rootFolder.AllowableActions);
            Assert.True(rootFolder.AllowableActions.Actions.Contains(Actions.CanGetProperties));
            Assert.False(rootFolder.AllowableActions.Actions.Contains(Actions.CanGetFolderParent));

            IItemEnumerable<ICmisObject> children = rootFolder.GetChildren();
            Assert.NotNull(children);
            foreach (ICmisObject child in children)
            {
                Assert.NotNull(child);
                Assert.NotNull(child.Id);
                Assert.NotNull(child.Name);
                Console.WriteLine(child.Name + " (" + child.Id + ")");
            }
        }

        [Test]
        public void SmokeTestQuery()
        {
            IItemEnumerable<IQueryResult> qr = Session.Query("SELECT * FROM cmis:document", false);
            Assert.NotNull(qr);

            foreach (IQueryResult hit in qr)
            {
                Assert.NotNull(hit);
                Assert.NotNull(hit["cmis:objectId"]);
                Console.WriteLine(hit.GetPropertyValueById(PropertyIds.Name) + " (" + hit.GetPropertyValueById(PropertyIds.ObjectId) + ")");
            }
        }

        [Test]
        public void SmokeTestCreateDocument()
        {
            IFolder rootFolder = Session.GetRootFolder();

            IDictionary<string, object> properties = new Dictionary<string, object>();
            properties[PropertyIds.Name] = "test-smoke.txt";
            properties[PropertyIds.ObjectTypeId] = "cmis:document";

            byte[] content = UTF8Encoding.UTF8.GetBytes("Hello World!");

            ContentStream contentStream = new ContentStream();
            contentStream.FileName = properties[PropertyIds.Name] as string;
            contentStream.MimeType = "text/plain";
            contentStream.Length = content.Length;
            contentStream.Stream = new MemoryStream(content);

            IDocument doc = rootFolder.CreateDocument(properties, contentStream, null);

            // check doc
            Assert.NotNull(doc);
            Assert.NotNull(doc.Id);

            // check versions
            IList<IDocument> versions = doc.GetAllVersions();
            Assert.NotNull(versions);
            Assert.AreEqual(1, versions.Count);
            Assert.AreEqual(doc.Id, versions[0].Id);

            // check content
            IContentStream retrievedContentStream = doc.GetContentStream();
            Assert.NotNull(retrievedContentStream);
            Assert.NotNull(retrievedContentStream.Stream);

            doc.Delete(true);

            try
            {
                doc.Refresh();
                Assert.Fail("Document shouldn't exist anymore!");
            }
            catch (CmisObjectNotFoundException) { }
        }

        [Test]
        public void SmokeTestCreateFolder()
        {
            IFolder rootFolder = Session.GetRootFolder();

            IDictionary<string, object> properties = new Dictionary<string, object>();
            properties[PropertyIds.Name] = "test-smoke";
            properties[PropertyIds.ObjectTypeId] = "cmis:folder";

            IFolder folder = rootFolder.CreateFolder(properties);
            
            // check folder
            Assert.NotNull(folder);
            Assert.NotNull(folder.Id);

            // check children
            foreach (ICmisObject cmisObject in folder.GetChildren())
            {
                Assert.Fail("Folder shouldn't have children!");
            }

            folder.Delete(true);

            try
            {
                folder.Refresh();
                Assert.Fail("Folder shouldn't exist anymore!");
            }
            catch (CmisObjectNotFoundException) { }
        }
    }
}
