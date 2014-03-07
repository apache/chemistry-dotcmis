using NUnit.Framework;
using DotCMIS;
using DotCMIS.Client;
using DotCMIS.Client.Impl;
using DotCMIS.Enums;
using DotCMIS.Data;
using System.Collections.Generic;
using DotCMIS.Data.Impl;

namespace DotCMISUnitTest
{
    [TestFixture]
    class AclTest : TestFramework
    {
        [Test]
        public void TestGetAcl()
        {
            //IObjectId id = Session.CreateObjectId(RepositoryInfo.RootFolderId);
            //IAcl acl = Session.GetAcl(id, false);
            //Assert.NotNull(acl);

            string principalId = "admin";
            string permission = "cmis:write";
            Properties properties = new Properties();
            IDictionary<string, object> dictionaryProperties = new Dictionary<string, object>();
            dictionaryProperties.Add("cmis:objectTypeId", "cmis:folder");
            dictionaryProperties.Add("cmis:name", "ft2");

            IObjectId newId = Session.CreateObjectId(RepositoryInfo.RootFolderId);
            IObjectId newFolderId = Session.CreateFolder(dictionaryProperties, newId);
            ICmisObject newFolder = Session.GetObject(newFolderId, new OperationContext() { IncludeAcls = true });
            IAce ace = Session.ObjectFactory.CreateAce(principalId, new List<string>() { permission });

            List<IAce> aceList = new List<IAce>();

            aceList.Add(ace);


            IAcl acl1 = newFolder.AddAcl(aceList, null);
            Assert.NotNull(acl1);

            IAcl acl2 = newFolder.RemoveAcl(aceList, null);
            Assert.NotNull(acl2);

            Session.Delete(newFolderId);
        }
    }
}
